using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using Swappy.Managers;

namespace Swappy.Commands;

/// <summary>
/// <c>swappy force [plugin...]</c>: checks for updates now, ignoring the rate limit. Without
/// arguments every plugin is checked. Requires <c>swappy.force</c>.
/// </summary>
[CommandHandler(typeof(SwappyParentCommand))]
public sealed class ForceCommand : ICommand
{
    /// <inheritdoc/>
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy.force"))
        {
            response = "You do not have permission to execute this command. Required: swappy.force";
            return false;
        }

        string[]? targets = arguments.Count == 0 ? null : arguments.ToArray();

        _ = UpdateManager.CheckForUpdates(force: true, targets);

        response = targets == null
            ? "Forced update check started for all plugins."
            : $"Forced update check started for: {string.Join(", ", targets)}";
        return true;
    }

    /// <inheritdoc/>
    public string Command => "force";

    /// <inheritdoc/>
    public string[] Aliases { get; } = [];

    /// <inheritdoc/>
    public string Description => "Force an update check for all plugins, or only the ones given as arguments";
}