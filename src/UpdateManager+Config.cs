using System.IO.Enumeration;
using System.Text.Json;
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
        [JsonPropertyName("github_token")] public string GithubToken { get; set; } = new();
    }

    public class PluginConfig : BasePluginConfig
    {
        // disable update checks completely
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // disable update check for specific plugins
        [JsonPropertyName("plugins")] public Dictionary<string, PluginEntryConfig> Plugins { get; set; } = new();
    }

    public partial class UpdateManager : BasePlugin, IPluginConfig<PluginConfig>
    {
        public PluginConfig Config { get; set; } = null!;
        private MapConfig[] _currentMapConfigs = Array.Empty<MapConfig>();
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
            // iterate through all dices and add them to the configuration file
            foreach (var plugin in _plugins)
            {
                if (!Config.Plugins.ContainsKey(plugin[0]))
                {
                    Config.Plugins.Add(plugin[0], new PluginEntryConfig());
                }
            }
            // delete all keys that do not exist anymore
            foreach (var key in Config.Plugins.Keys)
            {
                if (!_plugins.Any(plugin => plugin[0] == key))
                {
                    Config.Plugins.Remove(key);
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
