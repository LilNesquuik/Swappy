using System.Threading.Tasks;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.API.Interfaces;

namespace Swappy.Handlers;

public class ServerHandler : CustomEventsHandler
{
    public override void OnServerWaitingForPlayers()
    {
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
        {
            if (plugin is not ISwappyConfigurable repositoryPlugin)
                continue;

            _ = Task.Run(async () => await repositoryPlugin.RepositoryConfiguration.Resolve(plugin.Version));
        }
    }

    public override void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
        {
            if (plugin is not ISwappyConfigurable repositoryPlugin)
                continue;

            _ = Task.Run(async () => await repositoryPlugin.RepositoryConfiguration.Resolve(plugin.Version));
        }
    }
}