using System;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.Configurations;
using Swappy.Managers;

#if EXILED
using Exiled.API.Interfaces;
using Exiled.Loader;
#endif

namespace Swappy;

public class Entrypoint : Plugin<Config>
{
    public static Entrypoint Singleton;
    
    public override string Name => "Swappy";
    public override string Description => "Your trusted companion for automatic plugin updates";
    public override string Author => "LilNesquuik";
    public override Version Version => new(1, 0, 2);
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
    public override LoadPriority Priority => LoadPriority.Lowest;

    public override void Enable()
    {
        Singleton = this;
        
        if (!string.IsNullOrEmpty(Config!.Warning))
            Logger.Error(Config.Warning);
        
        if (Config.Configurations.IsEmpty())
            return;
        
        ServerEvents.MapGenerated += OnMapGenerated;
    }

    public override void Disable()
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