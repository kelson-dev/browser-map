using System.Text.RegularExpressions;
using System.Web;
using CliWrap;

var config_path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/browser-intercept.config";
var (command, path) = FindConfiguredBrowserSelection(args);
if (command != "") // empty string indicates no browser was found given the configuration
    await Cli.Wrap(command).WithArguments(path).ExecuteAsync();

(string command, string uri) FindConfiguredBrowserSelection(IReadOnlyList<string> args)
{
    if (args.Count == 0) // if no args, don't
        return ("", "");
    if (!File.Exists(config_path)) // if no config, don't
        return ("", args[0]);
    
    var config_state = new ConfigState();
    
    var nav = new Uri(args[0]);
    using (var stream = File.OpenRead(config_path))
        using (var reader = new StreamReader(stream))
            while (reader.ReadLine() is { Length: > 0 } line)
                if (ConfigRow.TryParse(line, out var row))
                    row.Update(ref config_state, nav);

    var query_params = HttpUtility.ParseQueryString(nav.Query);
    var selected_query_params = HttpUtility.ParseQueryString("");
    foreach (var key in query_params.AllKeys!)
    {
        if (key is null)
            continue;
        var included = true;
        foreach (var (include, match) in config_state.QueryParamExclusions)
            if (match.IsMatch(key))
                included = include;
        if (included)
            selected_query_params.Add(key, query_params.Get(key));
    }

    var builder = new UriBuilder
    {
        Scheme = nav.Scheme,
        Host = nav.Host,
        Path = nav.AbsolutePath,
        Query = selected_query_params.ToString()
    };

    return (config_state.SelectedBrowser, builder.Uri.ToString());
}

enum ConfigAction
{
    /// <summary>
    /// Maps hosts ending in [domain] to [command]
    /// May be overridden by subsequent configurations.
    /// Example:
    /// |map|.*google.com|/dev/null
    /// </summary>
    map,
    /// <summary>
    /// Indicates that query params whose keys match the given patterns should be excluded for the specified domain.
    /// May be overridden by subsequent configurations.
    /// Example:
    /// |exclude_query|.*twitter.com|^s$|^t$
    /// </summary>
    exclude_query,
    /// <summary>
    /// Indicates that query params whose keys match the given patterns should be included for the specified domain, even if previously excluded.
    /// May be overridden by subsequent configurations.
    /// Example:
    /// |exclude_query|.*twitter.com|^s$|^t$
    /// </summary>
    include_query
}

/// <summary>
/// Represents a configuration applied to a specific url
/// </summary>
public struct ConfigState
{
    public string SelectedBrowser = "";
    public readonly List<(bool include, Regex match)> QueryParamExclusions;

    public ConfigState() => QueryParamExclusions = new();
}

record struct ConfigRow(char Delimiter, ConfigAction Action, Regex Domain, string[] Parameters)
{
    public void Update(ref ConfigState state, Uri uri)
    {
        if (!Domain.IsMatch(uri.Host))
            return;
        switch (Action)
        {
            case ConfigAction.map:
                foreach (var p in Parameters)
                {
                    if (!File.Exists(p)) continue;
                    state.SelectedBrowser = p;
                    return;
                }
                return;
            case ConfigAction.exclude_query:
                foreach (var p in Parameters)
                    state.QueryParamExclusions.Add((false, new Regex(p)));
                return;
            case ConfigAction.include_query:
                foreach (var p in Parameters)
                    state.QueryParamExclusions.Add((true, new Regex(p)));
                return;
            default:
                return;
        }
    }

    public static bool TryParse(string line, out ConfigRow row)
    {
        row = default;
        if (line.Length == 0)
            return false;
        var delimiter = line[0];
        var remaining = line.AsSpan()[1..];
        Queue<string> values = new();
        while (remaining.Length > 0)
        {
            var index = remaining.IndexOf(delimiter);
            var content = index < 0 ? remaining : remaining[..index];
            values.Enqueue(content.ToString());
            remaining = remaining[content.Length..].TrimStart(delimiter);
        }
        if (values.Count < 3)
            return false;

        if (!Enum.TryParse(values.Dequeue(), out ConfigAction action))
            return false;

        row = new(delimiter, action, new Regex(values.Dequeue()), values.ToArray());
        return true;
    }
};

