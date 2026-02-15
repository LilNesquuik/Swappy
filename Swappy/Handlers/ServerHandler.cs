using System.Threading.Tasks;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.API.Interfaces;
using Swappy.Managers;

namespace Swappy.Handlers;

public class ServerHandler
{
    public void OnServerWaitingForPlayers()
    {
        _ = UpdateManager.CheckForUpdates();
    }

    public void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        _ = UpdateManager.CheckForUpdates();
    }
}