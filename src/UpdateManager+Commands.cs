using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin
    {
        [ConsoleCommand("um", "CS2 Update Manager")]
        [RequiresPermissions("@updatemanager/admin")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 2, usage: "[check/update] [all/<plugin_name>]")]
        public void CommandUpdatePlugins(CCSPlayerController player, CommandInfo command)
        {
            // arguments
            var commandType = command.GetArg(1);
            var pluginName = command.GetArg(2);
            bool applyUpdate;
            // check action
            switch (commandType.ToLower())
            {
                case "check":
                    applyUpdate = false;
                    break;
                case "update":
                    applyUpdate = true;
                    break;
                default:
                    command.ReplyToCommand("Invalid command. Usage: [check/update] [all/<plugin_name>]");
                    return;
            }
            // update plugin list
            getPluginList();
            // check pluginName
            if (pluginName == "all")
            {
                if (applyUpdate) command.ReplyToCommand(Localizer["command.update_all"]);
                else command.ReplyToCommand(Localizer["command.check_all"]);
                UpdateAllPlugins(applyUpdate).GetAwaiter().GetResult();
                return;
            }
            else if (_plugins.FirstOrDefault(x => x.Item1 == pluginName) != null)
            {
                if (applyUpdate) command.ReplyToCommand(Localizer["command.update"].Value.Replace("{plugin}", pluginName));
                else command.ReplyToCommand(Localizer["command.check"].Value.Replace("{plugin}", pluginName));
                UpdatePlugin(pluginName, applyUpdate).GetAwaiter().GetResult();
            }
            else
            {
                command.ReplyToCommand(Localizer["command.plugin_not_found"].Value.Replace("{plugin}", pluginName));
            }
        }
    }
}
