using CounterStrikeSharp.API.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO.Compression;
using System.Text.Json;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin
    {
        public override string ModuleName => "Update Manager";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";

        private string _pluginPath = "";
        private List<Tuple<string, string, string>> _plugins = [];

        public override void Load(bool hotReload)
        {
            _pluginPath = Path.Combine(ModuleDirectory, $"../");
            // update plugin list
            getPluginList();
            // initialize configuration
            LoadConfig();
            UpdateConfig();
            SaveConfig();
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
            }
            // register listeners
            RegisterListeners();
            // check on startup if enabled
            if (Config.CheckOnStartup) UpdateAllPlugins(true);
        }

        public override void Unload(bool hotReload)
        {
            // stop the queue processing task
            cancellationToken.Cancel();
            RemoveListeners();
            Console.WriteLine(Localizer["core.unload"]);
        }

        public override void OnAllPluginsLoaded(bool isReload)
        {
            // Start the queue processing task
            Task.Run(() => ProcessUpdateQueueAsync(cancellationToken.Token));
        }

        private void RegisterListeners()
        {
            if (Config.CheckOnMapStart) RegisterListener<Listeners.OnMapStart>(OnMapStart);
            if (Config.CheckOnMapEnd) RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            if (Config.CheckOnHibernation) RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
        }

        private void RemoveListeners()
        {
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
        }

        private void OnMapStart(string mapName)
        {
            // update plugin list
            getPluginList();
            // initialize configuration
            LoadConfig();
            UpdateConfig();
            SaveConfig();
            // check for updates
            UpdateAllPlugins(true);
        }

        private void OnMapEnd()
        {
            // update plugin list
            getPluginList();
            // initialize configuration
            LoadConfig();
            UpdateConfig();
            SaveConfig();
            // check for updates
            UpdateAllPlugins(true);
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
            UpdateAllPlugins(true);
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
                    if (!yamlObject.TryGetValue("version", out var pluginVersion))
                    {
                        Console.WriteLine(Localizer["update.error"].Value
                            .Replace("{pluginName}", pluginName)
                            .Replace("{error}", "Version not found in .info file."));
                        continue;
                    }
                    if (!yamlObject.TryGetValue("repository", out var pluginRepoURL))
                    {
                        Console.WriteLine(Localizer["update.error"].Value
                            .Replace("{pluginName}", pluginName)
                            .Replace("{error}", "Repository-URL not found in .info file."));
                        continue;
                    }
                    // add to plugin list
                    _plugins.Add(new Tuple<string, string, string>(pluginName, pluginVersion, pluginRepoURL));
                    Console.WriteLine(Localizer["plugin.found"].Value
                        .Replace("{pluginName}", pluginName)
                        .Replace("{pluginVersion}", pluginVersion));
                }
            }
        }

        private async Task<bool> UpdatePluginOnGithub(string pluginName, bool applyUpdate)
        {
            // find plugin in list
            var plugin = _plugins.FirstOrDefault(p => p.Item1.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase));
            if (plugin == null) return false;
            // get plugin details
            var (name, version, repoURL) = plugin;
            // check for plugin configuration
            var pluginConfig = Config.Plugins[name];
            if (pluginConfig == null || !pluginConfig.Enabled) return false;
            // check for updates
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "CounterStrikeSharp");
                if (!string.IsNullOrEmpty(pluginConfig.GithubToken))
                    client.DefaultRequestHeaders.Add("Authorization", $"token {pluginConfig.GithubToken}");
                else if (!string.IsNullOrEmpty(Config.GithubToken))
                    client.DefaultRequestHeaders.Add("Authorization", $"token {Config.GithubToken}");

                var repoPath = new Uri(repoURL).AbsolutePath.Trim('/');
                var response = await client.GetAsync($"https://api.github.com/repos/{repoPath}/releases/latest");

                // error due to http error
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(Localizer["update.error"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{error}", response.ReasonPhrase));
                    return false;
                }
                // read response
                var responseString = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);
                // error due to missing data
                if (release == null || !release.TryGetValue("tag_name", out var tagName))
                {
                    Console.WriteLine(Localizer["update.error"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{error}", "Release data not found."));
                    return false;
                }
                // check for version differences
                var latestVersion = tagName.ToString();
                if (latestVersion == version)
                {
                    Console.WriteLine(Localizer["update.notfound"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{pluginVersion}", version));
                    return false;
                }
                // update available
                Console.WriteLine(Localizer["update.available"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{pluginVersion}", version)
                    .Replace("{latestVersion}", latestVersion));
                // stop if no update should be applied
                if (!applyUpdate) return true;
                // check for assets
                if (!release.TryGetValue("assets", out var assets) || assets == null)
                {
                    Console.WriteLine(Localizer["update.error"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{error}", "Assets not found in release data."));
                    return false;
                }
                // check for zip assets
                var assetList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(assets?.ToString() ?? string.Empty);
                var zipAsset = assetList?.FirstOrDefault(a =>
                {
                    if (a == null) return false;
                    return a.TryGetValue("name", out var assetName) && assetName != null && assetName.ToString()!.EndsWith(".zip");
                });
                // check if download url exists
                if (zipAsset == null || !zipAsset.TryGetValue("browser_download_url", out var browserDownloadUrl))
                {
                    Console.WriteLine(Localizer["update.error"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{error}", "Download URL for .zip file not found in assets."));
                    return false;
                }
                // download zip
                var downloadURL = browserDownloadUrl.ToString();
                var downloadPath = Path.Combine(_pluginPath, $"{name}.zip");
                var downloadStream = await client.GetStreamAsync(downloadURL);
                // save zip
                using (var fileStream = File.Create(downloadPath))
                {
                    await downloadStream.CopyToAsync(fileStream);
                }
                // extract zip
                ZipFile.ExtractToDirectory(downloadPath, _pluginPath, true);
                File.Delete(downloadPath);
                // indicate success
                Console.WriteLine(Localizer["update.success"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{pluginVersion}", version)
                    .Replace("{latestVersion}", latestVersion));

                return true;
            }
            catch (Exception e)
            {
                // error during update
                Console.WriteLine(Localizer["update.error"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{error}", e.Message));
                return false;
            }
        }

        private void UpdatePlugin(string pluginName, bool applyUpdate)
        {
            // find plugin in list
            var plugin = _plugins.FirstOrDefault(p => p.Item1.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase));
            if (plugin == null) return;
            // get plugin details
            var (name, version, repoURL) = plugin;
            // check if repoURL is github
            if (repoURL.Contains("github.com"))
                EnqueueUpdateTask(() => UpdatePluginOnGithub(name, applyUpdate));
            else
                Console.WriteLine(Localizer["update.error"].Value
                    .Replace("{pluginName}", name)
                    .Replace("{error}", "Only Github repositories are supported."));
        }

        private void UpdateAllPlugins(bool applyUpdate)
        {
            foreach (var plugin in _plugins)
            {
                UpdatePlugin(plugin.Item1, applyUpdate);
            }
        }
    }
}