using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using NorthwoodLib.Pools;
using Swappy.Commands.Utility;

namespace Swappy.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SwappyParentCommand : ParentCommand
{
    public SwappyParentCommand() => LoadGeneratedCommands();
    
    public sealed override void LoadGeneratedCommands()
    {
        RegisterCommand(new Many());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
        stringBuilder.AppendLine();
        stringBuilder.Append("Valid subcommands:");
        
        foreach (ICommand command in AllCommands)
        {
            if (sender.HasAnyPermission($"swappy.{command.Command}"))
            {
                stringBuilder.Append($"\n\n<color=purple><b>- {command.Command} ({string.Join(", ", command.Aliases)})</b></color>\n" +
                                     $"<color=white>{command.Description}</color>");
            }
        }

        response = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
        return false;
    }

    public override string Command => "swappy";
    public override string[] Aliases => Array.Empty<string>();
    public override string Description => "Swappy Parent command";
}