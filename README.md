# browser-map

This is a terminal application you can set as your "default browser" in order to configure how links get opened in different browsers.

# Installation

Create a config file (described next) and build the application (described after configuration)

After setting up a configuration and building you can set your computers "default browser" to the built program using the settings of your desktop environment.


# Configuration

The application will read a config from a `browser-intercept.config` file in your "ApplicationData" directory. For me this is at `~/.config/browser-intercept.config`

Example `browser-intercept.config` to map all links except for twitter links to Firefox, and twitter links Brave:
```
|map|.*|/snap/bin/firefox
|map|^(www\.)?twitter\.com|/bin/brave-browser
```

The configuration format has a configuration on each line. The first character defines the seperator for that line. `|` is used here but if you need that character you can use any other character as the delimiter. The second value is what configuration action to take. The third value is a regex that defines which domains/hosts that configuration action applies to. All remaining values are parameters to the configuration action. Configuration actions override any settings specified previously.

The available actions are currently `map`, `exclude_query`, and `include_query`.

### Action: map

Maps domains to a specific browser by specifying a domain as a regex and a terminal command to execute to run the browser.

Format: `|map|domain regex|browser shell command`

Example (map google services to chromium): 
```
|map|^(www.\)?google.com|/bin/chromium
```

### Action: exclude_query

Specifies query parameters to exclude when opening links.

Format: `|exclude_query|domain regex|filter regex|filter regex|....`

Example (remove `s` and `t` tracker params from twitter links): 
```
|exclude_query|^(www\.)?twitter\.com|^s$|^t$
```

### Action: include_query

Specifies query parameters to include when opening links. (Overrides previous exclusions)

Format: `|include_query|domain regex|selection regex|selection regex|....`

Example (remove all but the `q` search term param from google searches): 
```
|exclude_query|^(www\.)?google\.com$|.*
|include_query|^(www\.)?google\.com$|^q$
```

# Building

Building the project requires the [.NET 6.0 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to be installed. Future versions will probably work.

(To disable telemetry create an environment variable `DOTNET_CLI_TELEMETRY_OPTOUT` with value `1`)

All commands should be ran from the directory containing `browser-map.csproj`



### Basic build for systems with the runtime installed, will output to `./bin/Debug/net6.0/`
```
dotnet build
```

### Run from source with an URL
```
dotnet run https://google.com/search?q=example+search
```

### Publish to a single file (or very few files) 

For linux (output to `./bin/Release/net6.0/linux-x64/publish/`):
```
dotnet publish -c Release --self-contained -r linux-x64
```

For windows (output to `./bin/Release/net6.0/win-x64/publish/`):
```
dotnet publish -c Release --self-contained -r win-x64
```

If you would like a smaller executable and don't mind the runtime needing to stay installed you can use `--no-self-contained` instead of `--self-contained`.
