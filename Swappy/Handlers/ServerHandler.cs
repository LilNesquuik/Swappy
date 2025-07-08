using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.Configurations;
using Swappy.Enums;
using Swappy.Managers;

#if EXILED
using Exiled.API.Interfaces;
using Exiled.Loader;
#endif

namespace Swappy.Handlers;

public class ServerHandler : CustomEventsHandler
{
    private readonly Config _config;
    public ServerHandler(Config config)
    {
        _config = config;
    }

    public override void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        if (_config.Configurations.IsEmpty())
            return;
        
        Logger.Debug("Checking for plugin updates...");

        foreach (PluginConfig pluginConfig in _config.Configurations)
        {
            if (pluginConfig.Cycle is CycleType.Never or not CycleType.EachRound)
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
}