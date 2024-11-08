# CounterstrikeSharp - Plugin Update Manager

[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-update-manager?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-update-manager/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-map-modifier](https://img.shields.io/github/issues/Kandru/cs2-update-manager)](https://github.com/Kandru/cs2-update-manager/issues)

The Plugin Update Manager is a plugin for Counter-Strike 2 designed to automatically update all your other plugins.

## Key Features

1. **Easy Management**
   - Automates plugin updates, saving time and effort. Just add one small file to your plugin to enable automatic updates.

2. **Effortless**
   - A server owner simply needs to add this plugin to their server and forget about outdated plugins.

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-map-modifier/releases/).
2. Move the "UpdateManager" folder to the `/addons/counterstrikesharp/configs/plugins/` directory.
3. Restart the server.

Updating is even easier: simply overwrite all plugin files and they will be reloaded automatically.

## Commands

There is currently one server-side command available for this plugin:

### update_plugin

This command triggers the update process for all installed plugins.

## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/UpdateManager/UpdateManager.json`.

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-update-manager.git
```

Go to the project directory

```bash
  cd cs2-update-manager
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

## FAQ

#### What is necessary to have my plugin updated automatically?

Release your plugin on GitHub using a public repository (or a private one if you're the only one that wants to use and update the plugin - and add the necessary GitHub token to the UpdateManager configuration file). Add an .info file to the plugin with the same name as your plugin folder (e.g., UpdateManager.info), which is a simple YAML file with two entries: version and repository. Version is simply the current version of your plugin, and repository is the URL to your GitHub repository. UpdateManager will check this URL automatically to determine updates.

#### The log file contains information about a rate limit exceeded.

GitHub has a rate limit without providing a GitHub token. The current rate limit is 60 requests per hour. If you host this plugin on a shared host, other people may be using it as well. Create a GitHub account and create a GitHub token with read access to all the repositories you want to be checked. The rate limits are much better than anonymous access.

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
- [@jmgraeffe](https://www.github.com/jmgraeffe)
