using System;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.Configurations;
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
    public override Version Version => new(1, 1, 0);
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

#if EXILED
    public override void OnEnabled()
#else
    public override void Enable()
#endif
    {
        Singleton = this;
        
        if (!string.IsNullOrEmpty(Config!.Warning))
            Logger.Error(Config.Warning);
        
        if (Config.Configurations.IsEmpty())
            return;
        
        ServerEvents.MapGenerated += OnMapGenerated;
    }
    
#if EXILED
    public override void OnDisabled()
#else
    public override void Disable()
#endif
    {
        ServerEvents.MapGenerated -= OnMapGenerated;
        
        Singleton = null;
    }

    private void OnMapGenerated(MapGeneratedEventArgs ev)
    {
#if EXILED
        foreach (IPlugin<IConfig> plugin in Loader.Plugins)
#else
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
#endif
        {
            if (!Config!.TryGetConfig(plugin.Name, out PluginConfig pluginConfig))
                continue;

            if (!pluginConfig.UpdateOnStartup)
                continue;

            GithubManager.UpdatePlugin(plugin, pluginConfig);
        }
        
        ServerEvents.MapGenerated -= OnMapGenerated;
    }
}