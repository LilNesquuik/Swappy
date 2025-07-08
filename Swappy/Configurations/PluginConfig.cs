using System;
using System.ComponentModel;
using Swappy.Enums;

namespace Swappy.Configurations;

[Serializable]
public class PluginConfig
{
    [Description("Name of the plugin (case sensitive)")]
    public string PluginName { get; set; }
    
    [Description("Owner of the GitHub repository")]
    public string? RepositoryOwner { get; set; }
    
    [Description("Name of the GitHub repository")] 
    public string? RepositoryName { get; set; }
    
    [Description("Optional GitHub access token for private repositories")]
    public string? AccessToken { get; set; }
    
    [Description("Whether to download and extract dependencies.zip from releases")]
    public bool DownloadDependencies { get; set; }
    
    [Description("Defines how often update checks are performed when loading the plugin")]
    public CycleType Cycle { get; set; }
    
    [Description("Whether to schedule a server restart after updating")]
    public bool ScheduleSoftRestart { get; set; }
}