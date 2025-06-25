using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.Configurations;
using Swappy.Managers;

namespace Swappy.Commands.Utility;

public class Many : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasAnyPermission($"swappy." + Command))
        {
            response = "You do not have permission to execute this command. Required: swappy."+Command;
            return false;
        }

        if (arguments.Count != 1)
        {
            response = "You do not have one argument. Expected: Swappy|ProjectMER|Exiled.Loader";
            return false;
        }

        string[] names = arguments.At(0).Split('|');

        foreach (string name in names)
        {
            Plugin? plugin = PluginLoader.Plugins.Keys.FirstOrDefault(x => x.Name == name);
            if (plugin is null)
            {
                response = "Could not find plugin: " + name;
                return false;
            }

            if (!Entrypoint.Singleton.Config!.TryGetConfig(plugin.Name, out PluginConfig pluginConfig))
            {
                response = "Could not find instructions for plugin: " + name;
                return false;
            }
            
            GithubManager.UpdatePlugin(plugin, pluginConfig);
        }

        response = "Update plugin: " + string.Join(", ", names);
        return true;
    }

    public string Command => "many";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Update a list of plugins";
}