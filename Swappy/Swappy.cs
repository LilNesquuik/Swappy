using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using Swappy.API.Abstractions;
using Swappy.API.Interfaces;
using Swappy.Handlers;

namespace Swappy;

public class Swappy : Plugin<Config>, ISwappyConfigurable
{
    public static Swappy Singleton;
    
    public override string Author => "LilNesquuik";
    public override Version Version => new(2, 0, 0);
    public override string Name => "Swappy.LabApi";
    public override string Description => "Your trusted companion for automatic plugin updates";
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
    public override LoadPriority Priority => LoadPriority.Lowest;
    
    public DependencyResource RepositoryConfiguration { get; } = new GithubResource
    {
        Repository = "Swappy",
        Author = "LilNesquuik",
        FileName = "Swappy.LabApi",
        UsePreRelease = false
    };
    
    private ServerHandler _serverHandler;
    
    public override void Enable()
    {
        Singleton = this;
        
        _serverHandler = new ServerHandler();
        
        CustomHandlersManager.RegisterEventsHandler(_serverHandler);
    }
    
    public override void Disable()
    {
        CustomHandlersManager.UnregisterEventsHandler(_serverHandler);
        
        _serverHandler = null!;
        
        Singleton = null!;
    }
}