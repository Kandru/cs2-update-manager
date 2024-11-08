using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin
    {
        [ConsoleCommand("update_plugins", "Updates all supported plugins")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void CommandUpdatePlugins(CCSPlayerController player, CommandInfo command)
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
    }
}
