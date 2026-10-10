using System;
using CommandSystem;
using Swappy.Configs.Sync;

namespace Swappy.Configs.Commands;

/// <summary>
/// <c>cfgsync</c>: shows the applied commit, the layers, the files written and the warnings and
/// errors of the last synchronisation. Available in the Remote Admin and server consoles, and
/// requires the <see cref="PlayerPermissions.ServerConfigs"/> permission.
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class CfgSyncCommand : ICommand
{
    /// <inheritdoc/>
    public string Command => "cfgsync";

    /// <inheritdoc/>
    public string[] Aliases { get; } = [];

    /// <inheritdoc/>
    public string Description => "Shows the state of the configuration synchronisation (Swappy.Configs).";

    /// <inheritdoc/>
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.ServerConfigs, out response))
            return false;

        if (SwappyConfigs.Singleton is null)
        {
            response = $"{Bundle.SelfName} is not enabled.";
            return false;
        }

        response = ConfigSync.Describe(SwappyConfigs.Singleton.Config);
        return true;
    }
}
