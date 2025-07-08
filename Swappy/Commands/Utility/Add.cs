using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommandSystem;
using LabApi.Features.Permissions;
using Swappy.Configurations;
using Swappy.Enums;

#if EXILED
using Exiled.Loader;
#endif

namespace Swappy.Commands.Utility;

public class Add : ICommand, IUsageProvider
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("swappy." + Command))
        {
            response = "You do not have permission to execute this command. Required: swappy."+Command;
            return false;
        }
        
        if (arguments.Count != 6)
        {
            response = "You do not have enough arguments. Expected: /swappy add <PluginName> <RepositoryOwner> <RepositoryName> <DownloadDependencies> <Cycle> <ScheduleSoftRestart>";
            return false;
        }
        
        if (Entrypoint.Singleton.Config!.TryGetConfig(arguments.At(0), out _))
        {
            response = $"Plugin {arguments.At(0)} is already configured.";
            return false;
        }
        
        PluginConfig pluginConfig = new PluginConfig
        {
            PluginName = arguments.At(0),
            RepositoryOwner = arguments.At(1),
            RepositoryName = arguments.At(2),
            DownloadDependencies = bool.Parse(arguments.At(3)),
            Cycle = (CycleType)Enum.Parse(typeof(CycleType), arguments.At(4)),
            ScheduleSoftRestart = bool.Parse(arguments.At(5))
        };
        
        Entrypoint.Singleton.Config.Configurations.Add(pluginConfig);

    #if EXILED
        File.WriteAllText(Entrypoint.Singleton.ConfigPath, Loader.Serializer.Serialize(Entrypoint.Singleton.Config));
    #else 
        Entrypoint.Singleton.SaveConfig();
    #endif
        response = $"Plugin {pluginConfig.PluginName} has been added to Swappy's configuration";
        return true;
    }

    public string Command => "add";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Adds a plugin to Swappy's configuration. The GitHub credential must be set manually in the config file.";
    public string[] Usage { get; } = {
        "PluginName",
        "RepositoryOwner",
        "RepositoryName",
        "DownloadDependencies (true/false)",
        "Cycle (OnStartup/EachRound/Never)",
        "ScheduleSoftRestart (true/false)"
    };
}