using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.Configurations;
using Swappy.Managers;

#if EXILED
using Exiled.API.Interfaces;
using Exiled.Loader;
#endif

namespace Swappy.Commands.Utility;

public class Install : ICommand
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

        foreach (string name in arguments)
        {
        #if EXILED
            IPlugin<IConfig>? plugin = Loader.Plugins.FirstOrDefault(x => x.Name == name);
        #else
            Plugin? plugin = PluginLoader.Plugins.Keys.FirstOrDefault(x => x.Name == name);
        #endif
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

        response = "Updated plugin: " + string.Join(", ", arguments);
        return true;
    }

    public string Command => "install";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Update a list of plugins";
}