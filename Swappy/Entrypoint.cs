using System;
using System.Threading.Tasks;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.Configurations;
using Swappy.Managers;

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
        
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
        {
            if (!Config.TryGetConfig(plugin.Name, out PluginConfig pluginConfig))
                continue;

            if (!pluginConfig.UpdateOnStartup)
                continue;

            GithubManager.UpdatePlugin(plugin, pluginConfig);
        }
    }

    public override void Disable()
    {
        Singleton = null;
    }
}