﻿using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Config;

namespace UpdateManager
{

    public class PluginEntryConfig
    {
        // disable update check for specific plugins
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // github token for private repositories
        [JsonPropertyName("github_token")] public string GithubToken { get; set; } = "";
    }

    public class PluginConfig : BasePluginConfig
    {
        // disable update checks completely
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // github token for private repositories
        [JsonPropertyName("github_token")] public string GithubToken { get; set; } = "";
        // wait at least x minutes before checking again for updates
        [JsonPropertyName("min_check_interval")] public int MinCheckInterval { get; set; } = 60;
        // check for updates on server hibernation
        [JsonPropertyName("check_on_hibernation")] public bool CheckOnHibernation { get; set; } = true;
        // check for updates on server startup
        [JsonPropertyName("check_on_startup")] public bool CheckOnStartup { get; set; } = true;
        // check for updates on map start
        [JsonPropertyName("check_on_map_start")] public bool CheckOnMapStart { get; set; } = false;
        // check for updates on map end
        [JsonPropertyName("check_on_map_end")] public bool CheckOnMapEnd { get; set; } = false;
        // disable update check for specific plugins
        [JsonPropertyName("plugins")] public Dictionary<string, PluginEntryConfig> Plugins { get; set; } = new Dictionary<string, PluginEntryConfig>();
    }

    public partial class UpdateManager : BasePlugin, IPluginConfig<PluginConfig>
    {
        public PluginConfig Config { get; set; } = null!;
        private string _configPath = "";

        private void LoadConfig()
        {
            Config = ConfigManager.Load<PluginConfig>("UpdateManager");
            _configPath = Path.Combine(ModuleDirectory, $"../../configs/plugins/UpdateManager/UpdateManager.json");
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            Console.WriteLine(Localizer["config.loaded"]);
        }

        private void UpdateConfig()
        {
            // iterate through all plugins and add them to the configuration file
            foreach (var (pluginName, pluginVersion, pluginRepoURL) in _plugins)
            {
                if (!Config.Plugins.ContainsKey(pluginName))
                {
                    Config.Plugins.Add(pluginName, new PluginEntryConfig());
                }
            }
        }

        private void SaveConfig()
        {
            var jsonString = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, jsonString);
        }
    }
}
