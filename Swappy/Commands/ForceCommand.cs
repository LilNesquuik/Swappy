using System;
using CommandSystem;
using LabApi.Features.Permissions;
using Swappy.Managers;

namespace Swappy.Commands;

[CommandHandler(typeof(SwappyParentCommand))]
public sealed class ForceCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy.force"))
        {
            response = "You do not have permission to execute this command. Required: swappy.force";
            return false;
        }

        if (arguments.Count != 1)
        {
            response = "You do not have one argument. Expected: /swappy many Swappy.LabApi ProjectMER";
            return false;
        }

        _ = UpdateManager.CheckForUpdates();
        response = "Forced updated for all plugins.";
        return true;
    }

    public string Command => "force";
    public string[] Aliases { get; } = [];
    public string Description => "Update a list of plugins";
}