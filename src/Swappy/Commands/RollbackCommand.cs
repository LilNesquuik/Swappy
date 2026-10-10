using System;
using System.IO;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;

namespace Swappy.Commands;

/// <summary>
/// <c>swappy rollback &lt;plugin&gt;</c>: swaps the plugin's DLL with the <c>.bak</c> kept by the
/// last update, freezes the plugin and restarts the server at the end of the round. Requires
/// <c>swappy.rollback</c>.
/// </summary>
/// <remarks>
/// The files are swapped rather than overwritten, so running it again restores the newer
/// version. The plugin stays frozen until <see cref="FreezeCommand"/> is run.
/// </remarks>
[CommandHandler(typeof(SwappyParentCommand))]
public sealed class RollbackCommand : ICommand
{
    /// <inheritdoc/>
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy.rollback"))
        {
            response = "You do not have permission to execute this command. Required: swappy.rollback";
            return false;
        }

        if (arguments.Count != 1)
        {
            response = "Usage: swappy rollback <PluginName>";
            return false;
        }

        string name = arguments.First();
        Plugin? plugin = PluginLoader.Plugins.Keys.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (plugin == null)
        {
            response = $"No loaded plugin is named '{name}'.";
            return false;
        }

        string backupPath = plugin.FilePath + ".bak";
        if (!File.Exists(backupPath))
        {
            response = $"No backup found for {plugin.Name}. A backup is only created when Swappy updates a plugin.";
            return false;
        }

        try
        {
            string tempPath = plugin.FilePath + ".tmp";
            File.Delete(tempPath);
            File.Move(plugin.FilePath, tempPath);
            File.Move(backupPath, plugin.FilePath);
            File.Move(tempPath, backupPath);
        }
        catch (Exception ex)
        {
            response = $"Rollback failed: {ex.Message}";
            return false;
        }

        if (!Swappy.Singleton.Config!.FrozenPlugins.Contains(plugin.Name, StringComparer.OrdinalIgnoreCase))
        {
            Swappy.Singleton.Config!.FrozenPlugins.Add(plugin.Name);
            Swappy.Singleton.SaveConfig();
        }

        ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;

        response = $"Rolled back {plugin.Name}. The plugin was frozen to prevent re-updating (use 'swappy freeze {plugin.Name}' to unfreeze). Restart scheduled for next round.";
        return true;
    }

    /// <inheritdoc/>
    public string Command => "rollback";

    /// <inheritdoc/>
    public string[] Aliases { get; } = [];

    /// <inheritdoc/>
    public string Description => "Swap a plugin's dll with its .bak backup, freeze it and schedule a restart";
}
