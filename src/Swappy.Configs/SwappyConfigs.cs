using System;
using System.Linq;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.Configs.Sync;

namespace Swappy.Configs;

/// <summary>
/// The LabAPI plugin that synchronises configuration files from a GitHub repository. It loads
/// before every other plugin, so they read the synchronised files.
/// </summary>
/// <remarks>
/// Files that were already read when it runs (<see cref="Bundle.Phase.NextBoot"/>) only apply
/// after a restart. When one of them changes, the server restarts right away if it is empty, or
/// at the end of the round otherwise.
/// </remarks>
public class SwappyConfigs : Plugin<Config>
{
    /// <summary>
    /// Gets the running instance. Set in <see cref="Enable"/>, cleared in <see cref="Disable"/>.
    /// </summary>
    public static SwappyConfigs Singleton = null!;

    /// <inheritdoc/>
    public override string Name => Bundle.SelfName;

    /// <inheritdoc/>
    public override string Description => "Synchronises configuration files from a Git repository before other plugins load";

    /// <inheritdoc/>
    public override string Author => "LilNesquuik";

    /// <inheritdoc/>
    public override Version Version => new(2, 2, 0);

    /// <inheritdoc/>
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

    /// <inheritdoc/>
    /// <remarks>
    /// Deliberately below <see cref="LoadPriority.Highest"/>, the lowest named value: LabAPI sorts
    /// on the underlying number, so 0 loads before every other plugin.
    /// </remarks>
    public override LoadPriority Priority => 0;

    /// <summary>
    /// Runs the synchronisation, blocking the server start for at most
    /// <see cref="Config.TimeoutSeconds"/>. Warns about any other plugin with the same priority.
    /// </summary>
    public override void Enable()
    {
        Singleton = this;

        ServerEvents.PluginsEnabled += OnPluginsEnabled;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;

        foreach (Plugin other in PluginLoader.Plugins.Keys.Where(p => p != this && p.Priority == Priority))
            Logger.Warn($"[{Name}] {other.Name} also has priority 0: it may read its config before the synchronisation");

        ConfigSync.Run(Config);
    }

    /// <summary>
    /// Unsubscribes from the server events.
    /// </summary>
    public override void Disable()
    {
        ServerEvents.PluginsEnabled -= OnPluginsEnabled;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;

        Singleton = null!;
    }

    private static void OnPluginsEnabled()
    {
        ConfigSync.RecordDiskHashes();
    }

    private static void OnWaitingForPlayers()
    {
        if (!ConfigSync.RestartPending)
            return;

        ConfigSync.RestartPending = false;

        if (Player.ConnectionsCount == 0)
        {
            Logger.Info($"[{Bundle.SelfName}] Files read at startup changed, restarting now (server empty)");
            Server.Restart();
            return;
        }

        Logger.Info($"[{Bundle.SelfName}] Files read at startup changed, restarting at the end of the round");
        ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
    }
}
