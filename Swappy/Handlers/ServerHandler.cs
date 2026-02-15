using LabApi.Events.Arguments.ServerEvents;
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