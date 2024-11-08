using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Update Manager";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "0.0.1";

        private string UpdateManagerUrl = "https://github.com/Kandru/cs2-update-manager";
        private string _pluginPath = Path.Combine(ModuleDirectory, $"../");
        private List<string, string, string> _plugins = new();

        public override void Load(bool hotReload)
        {
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

        private void OnServerHibernationUpdate(string mapName)
        {
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
                var pluginFilePath = Path.Combine(dir, $"{pluginName}.cs");

                if (File.Exists(pluginFilePath))
                {
                    var lines = File.ReadAllLines(pluginFilePath);
                    string url = null;
                    string version = null;

                    foreach (var line in lines)
                    {
                        if (line.Contains("private string UpdateManagerUrl"))
                        {
                            var startIndex = line.IndexOf("\"") + 1;
                            var endIndex = line.LastIndexOf("\"");
                            url = line.Substring(startIndex, endIndex - startIndex);
                        }
                        else if (line.Contains("public override string ModuleVersion"))
                        {
                            var startIndex = line.IndexOf("\"") + 1;
                            var endIndex = line.LastIndexOf("\"");
                            version = line.Substring(startIndex, endIndex - startIndex);
                        }

                        if (url != null && version != null && url.Contains("https://github.com"))
                        {
                            _plugins.Add(pluginName, version, url);
                            break;
                        }
                    }
                }
            }
        }

        private void checkForUpdates()
        {
            foreach (string (pluginName, pluginVersion, pluginRepoURL) in _plugins)
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
