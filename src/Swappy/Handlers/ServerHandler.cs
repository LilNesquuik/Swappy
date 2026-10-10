using LabApi.Events.Arguments.ServerEvents;
using Swappy.Managers;

namespace Swappy.Handlers;

/// <summary>
/// Starts the automatic update checks from server events.
/// </summary>
public class ServerHandler
{
    /// <summary>
    /// Checks for updates while the server waits for players.
    /// </summary>
    public void OnServerWaitingForPlayers()
    {
        _ = UpdateManager.CheckForUpdates();
    }

    /// <summary>
    /// Checks for updates at the end of each round.
    /// </summary>
    public void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        _ = UpdateManager.CheckForUpdates();
    }
}