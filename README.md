# CounterstrikeSharp - Plugin Update Manager

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/bkuF8xKHUt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-update-manager?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-update-manager/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-map-modifier](https://img.shields.io/github/issues/Kandru/cs2-update-manager?color=darkgreen)](https://github.com/Kandru/cs2-update-manager/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

The Plugin Update Manager is a plugin for Counter-Strike 2 designed to automatically update all your other plugins.

![Example Usage GIF](https://github.com/Kandru/cs2-update-manager/blob/main/assets/update_plugins.gif?raw=true)

## Key Features

1. **Easy Management**
   - Automates plugin updates, saving time and effort. Just add one small file to your plugin to enable automatic updates.

2. **Effortless**
   - A server owner simply needs to add this plugin to their server and forget about outdated plugins.

## Plugin Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-update-manager/releases/).
2. Move the "UpdateManager" folder to the `/addons/counterstrikesharp/configs/plugins/` directory of your gameserver.
3. Restart the server.

## Plugin Update

Simply overwrite all plugin files and they will be reloaded automatically or just use the Update Manager itself for an easy automatic update by using the *update_plugin* command.

## Commands

There is currently one server-side command available to use via the command line for this plugin:

### um [check/update] [all/<plugin_name>]

This command triggers the update mechanism for all or one specific plugin. Examples:

```bash
# check all plugins for updates (but do not update)
um check all

# update all plugins (including "check all")
um update all

# check specific plugin e.g. UpdateManager itself
um check UpdateManager

# update specific plugin e.g. UpdateManager itself
um update UpdateManager
```

## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/UpdateManager/UpdateManager.json`.

```json
{
  "enabled": true,
  "github_token": "",
  "min_check_interval": 60,
  "check_on_hibernation": true,
  "check_on_startup": true,
  "check_on_map_start": false,
  "check_on_map_end": false,
  "plugins": {
    "MapModifiers": {
      "enabled": true,
      "github_token": ""
    },
    "UpdateManager": {
      "enabled": true,
      "github_token": ""
    }
  },
  "ConfigVersion": 1
}
```

You can either disable the complete UpdateManager by simply setting the *enable* boolean to *false* or disable single plugins from being updated. The *github_token* is necessary for private plugins: each Github user can create tokens for specific repositories with permissions for the *content*. Otherwise plugins which are not public could not be accessed.

**Hint:** you should add a *github_token* regardless whether a plugin is private or publically available. The GitHub API is rate limited to *60* requests per hour for guests. In case you host your gameserver via a gameserver provider I strongly recommend using a token to avoid hitting a constant rate limit. Same goes for private hosting with multiple servers.

## FAQ

#### What is necessary to have my plugin updated automatically?

Release your plugin on GitHub and create a Release with the version number as a tag. Important: the release file must be *.zip*-file containing a folder with the name of the plugin. Inside this folder should be an additional yaml-file named *<PluginName>.info*. The UpdateManager will automatically find this file and read it.

```yaml
version: 0.1.2
repository: https://github.com/Kandru/cs2-update-manager
```

This file simply contains the current version and a link to the github repository. Only GitHub is supported so far. Feel free to create an issue regarding other hosting options which will fit your needs.

#### The log file contains information about a rate limit exceeded.

GitHub has a rate limit without providing a GitHub token. The current rate limit is 60 requests per hour. If you host this plugin on a shared host, other people may be using it as well. Create a GitHub account and create a GitHub token with read access to all the repositories you want to be checked. The rate limits are much better than anonymous access.

#### How to know which plugins are compatible with this AutoUpdater?

There is a list of plugins [here](https://github.com/Kandru/cs2-update-manager/blob/main/COMPATIBLE_PLUGINS.md) (feel free to add yours via opening an issue or creating a pull-request). Compatible plugins should include the following badge on top of the README:

```
[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
```

#### When will update checks occure?

Currently update checks will start when the server is in hibernation mode. This event occures after all players have left the server. However, this behaviour can be disabled via a cvar. Therefore a manual update command is available from the server console.

#### Are updates secure?

The update mechanism is the same as manually downloading the *.zip*-file from GitHub and uploading it to the plugin folder of CounterstrikeSharp. A plugin developer should make sure the plugin does work flawlessly before releasing it.

#### Are plugins secure?

Be aware: as good as an automatic plugin update tool may be: you will be running unkown code on your gameserver. There might be plugins out there which will tamper with your gameserver or grab all data like rcon passwords and other stuff. Make sure you trust the developer of a plugin and may disable automated updates for plugins you do not trust.

#### How to automatically release new updates?

You can use the GitHub workflow provided in this repository. It will automatically listen for a change of the *Version.cs* file in the *src*-Folder and compile everything. Afterwards a new release including a *.info*-file will be published.

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

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
- [@jmgraeffe](https://www.github.com/jmgraeffe)
