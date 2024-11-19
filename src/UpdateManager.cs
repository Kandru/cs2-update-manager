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
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
            }
            // register listeners
            RegisterListeners();
            // check on startup if enabled
            if (Config.CheckOnStartup) checkForUpdates();
        }

        public override void Unload(bool hotReload)
        {
            RemoveListeners();
            Console.WriteLine(Localizer["core.unload"]);
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
            checkForUpdates();
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
            checkForUpdates();
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
            Task.Run(async () =>
            {
                foreach (var (pluginName, pluginVersion, pluginRepoURL) in _plugins)
                {
                    // get plugin configuration
                    var pluginConfig = Config.Plugins[pluginName];
                    if (pluginConfig == null || !pluginConfig.Enabled) continue;
                    // check github api /repos/{owner}/{repo}/releases/latest
                    try
                    {
                        var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("User-Agent", "CounterStrikeSharp");
                        // check for plugin github token
                        if (!string.IsNullOrEmpty(pluginConfig.GithubToken))
                            client.DefaultRequestHeaders.Add("Authorization", $"token {pluginConfig.GithubToken}");
                        // check for global github token
                        else if (!string.IsNullOrEmpty(Config.GithubToken))
                            client.DefaultRequestHeaders.Add("Authorization", $"token {Config.GithubToken}");
                        // get latest release
                        var repoPath = new Uri(pluginRepoURL).AbsolutePath.Trim('/');
                        var response = await client.GetAsync($"https://api.github.com/repos/{repoPath}/releases/latest");
                        // check if response is successful
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine(Localizer["update.error"].Value
                                .Replace("{pluginName}", pluginName)
                                .Replace("{error}", response.ReasonPhrase));
                            continue;
                        }
                        // parse response
                        var responseString = await response.Content.ReadAsStringAsync();
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
                        // get asset list
                        var assetList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(assets?.ToString() ?? string.Empty);
                        // look for zip asset
                        var zipAsset = assetList?.FirstOrDefault(a =>
                        {
                            if (a == null) return false;
                            return a.TryGetValue("name", out var name) && name != null && name.ToString()!.EndsWith(".zip");
                        });
                        // check if zip asset was found
                        if (zipAsset == null || !zipAsset.TryGetValue("browser_download_url", out var browserDownloadUrl))
                        {
                            Console.WriteLine(Localizer["update.error"].Value
                                .Replace("{pluginName}", pluginName)
                                .Replace("{error}", "Download URL for .zip file not found in assets."));
                            continue;
                        }
                        // download zip
                        var downloadURL = browserDownloadUrl.ToString();
                        var downloadPath = Path.Combine(_pluginPath, $"{pluginName}.zip");
                        var downloadStream = await client.GetStreamAsync(downloadURL);
                        // save zip
                        using (var fileStream = File.Create(downloadPath))
                        {
                            await downloadStream.CopyToAsync(fileStream);
                        }
                        // extract zip
                        ZipFile.ExtractToDirectory(downloadPath, _pluginPath, true);
                        // remove zip
                        File.Delete(downloadPath);
                        Console.WriteLine(Localizer["update.success"].Value
                            .Replace("{pluginName}", pluginName)
                            .Replace("{pluginVersion}", pluginVersion)
                            .Replace("{latestVersion}", latestVersion));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(Localizer["update.error"].Value
                            .Replace("{pluginName}", pluginName)
                            .Replace("{error}", e.Message));
                    }
                }
            });
        }
    }
}
