using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;

namespace Swappy.Commands;

/// <summary>
/// <c>swappy freeze &lt;plugin&gt;</c>: adds the plugin to <see cref="Config.FrozenPlugins"/>, or
/// removes it if it is already there. Requires <c>swappy.freeze</c>.
/// </summary>
[CommandHandler(typeof(SwappyParentCommand))]
public sealed class FreezeCommand : ICommand
{
    /// <inheritdoc/>
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy.freeze"))
        {
            response = "You do not have permission to execute this command. Required: swappy.freeze";
            return false;
        }

        if (arguments.Count != 1)
        {
            response = "Usage: swappy freeze <PluginName>";
            return false;
        }

        string name = arguments.First();
        List<string> frozen = Swappy.Singleton.Config!.FrozenPlugins;

        string? existing = frozen.Find(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            frozen.Remove(existing);
            Swappy.Singleton.SaveConfig();

            response = $"{existing} unfrozen - it will be checked for updates again.";
            return true;
        }

        frozen.Add(name);
        Swappy.Singleton.SaveConfig();

        response = $"{name} frozen - it will be skipped by update checks. Run the command again to unfreeze.";

        if (!PluginLoader.Plugins.Keys.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            response += $" Warning: no loaded plugin is named '{name}'.";

        return true;
    }

    /// <inheritdoc/>
    public string Command => "freeze";

    /// <inheritdoc/>
    public string[] Aliases { get; } = ["pin"];

    /// <inheritdoc/>
    public string Description => "Toggle a plugin's exclusion from update checks";
}
