using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using NorthwoodLib.Pools;
using Swappy.Configurations;

#if EXILED
using Exiled.API.Interfaces;
using Exiled.Loader;
#endif

namespace Swappy.Commands.Utility;

public class Plugins : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy." + Command))
        {
            response = "You do not have permission to execute this command. Required: swappy."+Command;
            return false;
        }
        
        StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
        stringBuilder.AppendLine("<color=purple><b>Plugins:</b></color>");
        
    #if EXILED
        foreach (IPlugin<IConfig> plugin in Loader.Plugins)
    #else
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
    #endif 
        {
            stringBuilder.AppendLine($"・Name: <color=purple><b>{plugin.Name}</b></color>");
            stringBuilder.AppendLine($"・Version: <color=white>{plugin.Version}</color>");
            stringBuilder.AppendLine($"・Author: <color=white>{plugin.Author}</color>");

            if (Entrypoint.Singleton.Config!.TryGetConfig(plugin.Name, out PluginConfig config))
            {
                stringBuilder.AppendLine($"・Repository: <color=white>{config.RepositoryName}</color>");
                stringBuilder.AppendLine($"・Owner: <color=white>{config.RepositoryOwner}</color>");
                stringBuilder.AppendLine($"・Cycle: <color=white>{config.Cycle}</color>");
                stringBuilder.AppendLine($"・Schedule Soft Restart: <color=white>{(config.ScheduleSoftRestart ? "Enabled" : "Disabled")}</color>");
            }
            
            stringBuilder.AppendLine();
        }
        
        response = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
        return true;
    }

    public string Command => "plugins";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Get list of plugins";
}