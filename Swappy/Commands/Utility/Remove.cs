using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommandSystem;
using LabApi.Features.Permissions;
using Swappy.Configurations;

#if EXILED
using Exiled.Loader;
#endif

namespace Swappy.Commands.Utility;

public class Remove : ICommand, IUsageProvider
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
        
        if (!Entrypoint.Singleton.Config!.TryGetConfig(arguments.At(0), out PluginConfig config))
        {
            response = $"Plugin {arguments.At(0)} not found in the Swappy configuration.";
            return false;
        }
        
        Entrypoint.Singleton.Config.Configurations.Remove(config);
        
    #if EXILED
        File.WriteAllText(Entrypoint.Singleton.ConfigPath, Loader.Serializer.Serialize(Entrypoint.Singleton.Config));
    #else 
        Entrypoint.Singleton.SaveConfig();
    #endif
        response = $"Plugin {arguments.At(0)} has been removed from the Swappy configuration.";
        return true;
    }

    public string Command => "remove";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Removes a plugin from the Swappy configuration. This does not uninstall the plugin, it just removes it from the Swappy config so it won't be updated anymore.";
    public string[] Usage => new[] { "PluginName" };
}