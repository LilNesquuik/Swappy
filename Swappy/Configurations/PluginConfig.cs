using System;

namespace Swappy.Configurations;

[Serializable]
public class PluginConfig
{
    public string PluginName { get; set; }
    public string? RepositoryOwner { get; set; }
    public string? RepositoryName { get; set; }
    public string? AccessToken { get; set; }
    public bool DownloadDependencies { get; set; }
    public bool UpdateOnStartup { get; set; }
    public bool SoftRestart { get; set; }
}