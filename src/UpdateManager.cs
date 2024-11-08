using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin
    {
        public override string ModuleName => "Update Manager";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "0.0.1";

        private string _pluginPath = "";
        private List<Tuple<string, string, string>> _plugins = new();

        public override void Load(bool hotReload)
        {
            _pluginPath = Path.Combine(ModuleDirectory, $"../");
            // update plugin list
            getPluginList();
            // initialize configuration
            LoadConfig();
            UpdateConfig();
            SaveConfig();
            RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
            }
        }

        public override void Unload(bool hotReload)
        {
            // unregister listeners
            RemoveListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            Console.WriteLine(Localizer["core.unload"]);
        }

        private void OnServerHibernationUpdate(bool isHibernating)
        {
            if (!isHibernating) return;
            // update plugin list
            getPluginList();
            // initialize configuration
            LoadConfig();
            UpdateConfig();
            SaveConfig();
            // check for updates
            checkForUpdates();
        }

        private void getPluginList()
        {
            var directories = Directory.GetDirectories(_pluginPath);
            foreach (var dir in directories)
            {
                var pluginName = Path.GetFileName(dir);
                var pluginFilePath = Path.Combine(dir, $"{pluginName}.info");

                if (File.Exists(pluginFilePath))
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();
                    using var reader = new StreamReader(pluginFilePath);
                    var yamlObject = deserializer.Deserialize<Dictionary<string, string>>(reader);
                    var pluginVersion = yamlObject["version"];
                    var pluginRepoURL = yamlObject["repository"];
                    // add to plugin list
                    _plugins.Add(new Tuple<string, string, string>(pluginName, pluginVersion, pluginRepoURL));
                    Console.WriteLine(Localizer["plugin.found"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{pluginVersion}", pluginVersion));
                }
            }
        }

        private void checkForUpdates()
        {
            foreach (var (pluginName, pluginVersion, pluginRepoURL) in _plugins)
            {
                // TODO: update UpdateManager itself. Skip for now.
                if (pluginName == "UpdateManager") continue;
                // get plugin configuration
                var pluginConfig = Config.Plugins[pluginName];
                if (pluginConfig == null || !pluginConfig.Enabled) continue;
                // check github api /repos/{owner}/{repo}/releases/latest
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "CounterStrikeSharp");
                if (!string.IsNullOrEmpty(pluginConfig.GithubToken))
                    client.DefaultRequestHeaders.Add("Authorization", $"token {pluginConfig.GithubToken}");
                var repoPath = new Uri(pluginRepoURL).AbsolutePath.Trim('/');
                var response = client.GetAsync($"https://api.github.com/repos/{repoPath}/releases/latest").Result;
                Console.WriteLine(response);
            }
        }
    }
}
