using System;
using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.Configurations;
using Swappy.Enums;
using Swappy.Handlers;
using Swappy.Managers;

#if EXILED
using Exiled.API.Interfaces;
using Exiled.Loader;
using Exiled.API.Enums;
#endif

namespace Swappy;

#if EXILED
public class Entrypoint : Exiled.API.Features.Plugin<Config>
#else
public class Entrypoint : Plugin<Config>
#endif
{
    public static Entrypoint Singleton;
    
    public override string Author => "LilNesquuik";
    public override Version Version => new(1, 3, 0);
#if EXILED
    public override string Name => "Swappy.Exiled";
    public override PluginPriority Priority => PluginPriority.Lowest;
    public override bool IgnoreRequiredVersionCheck => true;
#else
    public override string Name => "Swappy.LabApi";
    public override string Description => "Your trusted companion for automatic plugin updates";
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
    public override LoadPriority Priority => LoadPriority.Lowest;
#endif

    private ServerHandler _serverHandler;

#if EXILED
    public override void OnEnabled()
#else
    public override void Enable()
#endif
    {
        Singleton = this;
        
        _serverHandler = new ServerHandler(Config!);
        
        if (!string.IsNullOrEmpty(Config!.Warning))
            Logger.Error(Config.Warning);
        
        CustomHandlersManager.RegisterEventsHandler(_serverHandler);
        
        foreach (PluginConfig pluginConfig in Config!.Configurations)
        {
            if (pluginConfig.Cycle is CycleType.Never or not (CycleType.OnStartup or CycleType.EachRound))
                continue;
        #if EXILED
            IPlugin<IConfig>? plugin = Loader.Plugins.FirstOrDefault(x => x.Name == pluginConfig.PluginName);
        #else 
            Plugin? plugin = PluginLoader.Plugins.Keys.FirstOrDefault(x => x.Name == pluginConfig.PluginName);
        #endif
            if (plugin is null)
                continue;

            GithubManager.UpdatePlugin(plugin, pluginConfig);
        }
    }
    
#if EXILED
    public override void OnDisabled()
#else
    public override void Disable()
#endif
    {
        CustomHandlersManager.UnregisterEventsHandler(_serverHandler);
        
        _serverHandler = null!;
        
        Singleton = null!;
    }
}