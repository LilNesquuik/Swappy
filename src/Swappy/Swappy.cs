using System;
using HarmonyLib;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.API.Features;
using Swappy.API.Interfaces;
using Swappy.Handlers;

namespace Swappy;

/// <summary>
/// The LabAPI plugin that keeps the other plugins up to date. It checks their repositories when
/// the server waits for players, at the end of each round and when the server leaves idle mode.
/// </summary>
/// <remarks>
/// Swappy is itself a <see cref="ManagedPlugin{TConfig}"/>, so it updates itself unless frozen.
/// </remarks>
public class Swappy : ManagedPlugin<Config>, ISwappyConfigurable
{
    /// <summary>
    /// Gets the running instance. Set in <see cref="Enable"/>, cleared in <see cref="Disable"/>.
    /// </summary>
    public static Swappy Singleton;

    /// <inheritdoc/>
    public override string Author => "LilNesquuik";

    /// <inheritdoc/>
    public override Version Version => new(2, 2, 0);

    /// <inheritdoc/>
    public override string Name => "Swappy";

    /// <inheritdoc/>
    public override string Description => "Your trusted companion for automatic plugin updates";

    /// <inheritdoc/>
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

    /// <inheritdoc/>
    public override LoadPriority Priority => LoadPriority.Lowest;

    /// <inheritdoc/>
    public override DependencyResource Repository { get; } = new GithubResource
    {
        Repository = "Swappy",
        Author = "LilNesquuik",
    };

    /// <summary>
    /// Gets the time the last update check completed, used to enforce
    /// <see cref="Config.RateLimitMinutes"/>.
    /// </summary>
    public DateTimeOffset LastUpdateCheck { get; internal set; }

    private Harmony _harmony;

    private ServerHandler _serverHandler;

    /// <summary>
    /// Applies the Harmony patches and subscribes to the server events that trigger update checks.
    /// </summary>
    public override void Enable()
    {
        Singleton = this;
        
        _serverHandler = new ServerHandler();
        
        _harmony = new Harmony("com.lilnesquuik.swappy");
        
        try
        {
            _harmony.PatchAll();   
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to patch: {e}");
        }   
        
        ServerEvents.WaitingForPlayers += _serverHandler.OnServerWaitingForPlayers;
        ServerEvents.RoundEnded += _serverHandler.OnServerRoundEnded;
    }
    
    /// <summary>
    /// Unsubscribes from the server events.
    /// </summary>
    public override void Disable()
    {
        ServerEvents.WaitingForPlayers -= _serverHandler.OnServerWaitingForPlayers;
        ServerEvents.RoundEnded -= _serverHandler.OnServerRoundEnded;
        
        _serverHandler = null!;
        
        Singleton = null!;
    }
}