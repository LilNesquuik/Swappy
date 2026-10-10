using System;
using CommandSystem;
using LabApi.Loader.Features.Commands.Extensions;

namespace Swappy.Commands;

/// <summary>
/// The <c>swappy</c> Remote Admin command, which groups Swappy's subcommands. Run without
/// arguments, it lists them.
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SwappyParentCommand : ParentCommand
{
    /// <inheritdoc/>
    public override string Command => "swappy";

    /// <inheritdoc/>
    public override string[] Aliases { get; } = ["swp", "updater"];

    /// <inheritdoc/>
    public override string Description => "Swappy Parent command";

    /// <inheritdoc/>
    public override void LoadGeneratedCommands() { }

    /// <inheritdoc/>
    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        return CommandExtensions.ListSubCommands(this, arguments, out response);
    }
}