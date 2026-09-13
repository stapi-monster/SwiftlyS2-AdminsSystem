using Admins.Core.Contract;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace Admins.Rcon.Commands;

public class RconCommands
{
    private readonly ISwiftlyCore Core;
    private IConfigurationManager? ConfigurationManager;

    public RconCommands(ISwiftlyCore core)
    {
        Core = core;
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        ConfigurationManager = configurationManager;
    }

    private string GetPrefix()
    {
        return ConfigurationManager?.GetCurrentConfiguration()?.Prefix ?? "[Admins]";
    }

    [Command("rcon", permission: "@admin/rcon")]
    [CommandAlias("sw_rcon")]
    [CommandAlias("css_rcon")]
    [CommandAlias("mm_rcon")]
    public void Command_Rcon(ICommandContext context)
    {
        if (context.Args.Length < 1)
        {
            if (context.IsSentByPlayer && context.Sender != null)
            {
                var localizer = Core.Translation.GetPlayerLocalizer(context.Sender);
                context.Reply(localizer["UsageRcon", GetPrefix(), context.CommandName]);
            }
            else
            {
                context.Reply($"[Admins.Rcon] Usage: {context.CommandName} <command>");
            }
            return;
        }

        var rconCommand = string.Join(" ", context.Args);
        Core.Engine.ExecuteCommand(rconCommand);

        string adminName = context.IsSentByPlayer && context.Sender != null ? context.Sender.Controller.PlayerName : "Console";
        Core.Logger.LogInformation($"[Admins.Rcon] Admin '{adminName}' executed RCON command: {rconCommand}");

        if (context.IsSentByPlayer && context.Sender != null)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(context.Sender);
            context.Reply(localizer["RconExecuted", GetPrefix(), adminName, rconCommand]);
        }
        else
        {
            context.Reply($"[Admins.Rcon] Executed RCON command: {rconCommand}");
        }
    }
}
