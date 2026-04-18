using System.Collections.Generic;
using System.ComponentModel;
using Swappy.API.Configurations;

namespace Swappy;

public class Config
{
    [Description("A personal access token for GitHub. This is required to check for updates on private repositories.")]
    public string? AccessToken { get; set; }
    
    [Description("The rate limit in minutes for checking updates. Default is 30 minutes.")]
    public int RateLimitMinutes { get; set; } = 30;
    
    [Description("Whether to check for updates when the server exits idle mode. Default is true.")]
    public bool CheckOnExitIdle { get; set; } = true;
    
    [Description("Configuration for manually added plugins. These plugins will be added to the list of plugins to check for updates.")]
    public List<ManualConfiguration> ManualConfigurations { get; set; } = [];
}