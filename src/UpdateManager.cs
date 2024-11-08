using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin
    {
        public override string ModuleName => "Update Manager";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "0.1.1";

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
            _plugins.Clear();
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
                // ignore UpdateManager
                if (pluginName == "UpdateManager") continue;
                // get plugin configuration
                var pluginConfig = Config.Plugins[pluginName];
                if (pluginConfig == null || !pluginConfig.Enabled) continue;
                // check github api /repos/{owner}/{repo}/releases/latest
                try {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "CounterStrikeSharp");
                if (!string.IsNullOrEmpty(pluginConfig.GithubToken))
                    client.DefaultRequestHeaders.Add("Authorization", $"token {pluginConfig.GithubToken}");
                var repoPath = new Uri(pluginRepoURL).AbsolutePath.Trim('/');
                var response = client.GetAsync($"https://api.github.com/repos/{repoPath}/releases/latest").Result;
                // check if response is successful
                if (!response.IsSuccessStatusCode) {
                    Console.WriteLine(Localizer["update.error"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{error}", response.ReasonPhrase));
                    continue;
                }
                // parse response
                var responseString = response.Content.ReadAsStringAsync().Result;
                // get download url for latest .zip
                var release = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);
                if (release == null)
                {
                    Console.WriteLine(Localizer["update.error"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{error}", "Release data not found."));
                    continue;
                }
                if (!release.TryGetValue("tag_name", out var tagName))
                {
                    Console.WriteLine(Localizer["update.error"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{error}", "Tag name not found in release data."));
                    continue;
                }
                var latestVersion = tagName.ToString();
                if (latestVersion == pluginVersion)
                {
                    Console.WriteLine(Localizer["update.notfound"].Value
                    .Replace("{pluginName}", pluginName)
                    .Replace("{pluginVersion}", pluginVersion));
                    continue;
                }
                Console.WriteLine(Localizer["update.available"].Value
                    .Replace("{pluginName}", pluginName)
                    .Replace("{pluginVersion}", pluginVersion)
                    .Replace("{latestVersion}", latestVersion));
                // download and update plugin
                if (!release.TryGetValue("assets", out var assets) || assets == null)
                {
                    Console.WriteLine(Localizer["update.error"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{error}", "Assets not found in release data."));
                    continue;
                }
                var assetList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(assets?.ToString() ?? string.Empty);
                var zipAsset = assetList?.FirstOrDefault(a =>
                {
                    if (a == null) return false;
                    return a.TryGetValue("name", out var name) && name?.ToString().EndsWith(".zip") == true;
                });
                if (zipAsset == null || !zipAsset.TryGetValue("browser_download_url", out var browserDownloadUrl))
                {
                    Console.WriteLine(Localizer["update.error"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{error}", "Download URL for .zip file not found in assets."));
                    continue;
                }
                var downloadURL = browserDownloadUrl.ToString();
                var downloadPath = Path.Combine(_pluginPath, $"{pluginName}.zip");
                var downloadStream = client.GetStreamAsync(downloadURL).Result;
                using (var fileStream = File.Create(downloadPath))
                {
                    downloadStream.CopyTo(fileStream);
                }
                // extract zip
                ZipFile.ExtractToDirectory(downloadPath, _pluginPath, true);
                // remove zip
                File.Delete(downloadPath);
                Console.WriteLine(Localizer["update.success"].Value
                    .Replace("{pluginName}", pluginName)
                    .Replace("{pluginVersion}", pluginVersion)
                    .Replace("{latestVersion}", latestVersion));
                } catch (Exception e) {
                    Console.WriteLine(Localizer["update.error"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{error}", e.Message));
                }
            }
        }
    }
}
