using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.API.Interfaces;

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

        List<Plugin> plugins = [];
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
        {
            if (plugin is not ISwappyConfigurable repositoryPlugin)
                continue;

            plugins.Add(plugin);
            _ = Task.Run(async () => await repositoryPlugin.RepositoryConfiguration.Resolve(plugin.Version));
        }

        response = "Forced updated: " + string.Join(", ", plugins.Select(p => p.Name));
        return true;
    }

    public string Command => "force";
    public string[] Aliases { get; } = [];
    public string Description => "Update a list of plugins";
}