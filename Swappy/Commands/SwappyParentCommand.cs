using System;
using CommandSystem;
using LabApi.Loader.Features.Commands.Extensions;

namespace Swappy.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SwappyParentCommand : ParentCommand
{
    public override string Command => "swappy";
    public override string[] Aliases => Array.Empty<string>();
    public override string Description => "Swappy Parent command";
    
    public override void LoadGeneratedCommands() { }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        return CommandExtensions.ListSubCommands(this, arguments, out response);
    }
}