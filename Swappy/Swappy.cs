using System;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.API.Features;
using Swappy.API.Interfaces;
using Swappy.Handlers;

namespace Swappy;

public class Swappy : ManagedPlugin<Config>, ISwappyConfigurable
{
    public static Swappy Singleton;
    
    public override string Author => "LilNesquuik";
    public override Version Version => new(2, 0, 2);
    public override string Name => "Swappy";
    public override string Description => "Your trusted companion for automatic plugin updates";
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
    public override LoadPriority Priority => LoadPriority.Lowest;
    public override DependencyResource Repository { get; } = new GithubResource
    {
        Repository = "Swappy",
        Author = "LilNesquuik",
    };
    
    private ServerHandler _serverHandler;
    
    public override void Enable()
    {
        Singleton = this;
        
        _serverHandler = new ServerHandler();
        
        ServerEvents.WaitingForPlayers += _serverHandler.OnServerWaitingForPlayers;
        ServerEvents.RoundEnded += _serverHandler.OnServerRoundEnded;
    }
    
    public override void Disable()
    {
        ServerEvents.WaitingForPlayers -= _serverHandler.OnServerWaitingForPlayers;
        ServerEvents.RoundEnded -= _serverHandler.OnServerRoundEnded;
        
        _serverHandler = null!;
        
        Singleton = null!;
    }
}