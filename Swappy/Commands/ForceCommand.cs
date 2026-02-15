using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.API.Interfaces;
using Swappy.Managers;

namespace Swappy.Commands;

[CommandHandler(typeof(SwappyParentCommand))]
public class ForceCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy." + Command))
        {
            response = "You do not have permission to execute this command. Required: swappy."+Command;
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