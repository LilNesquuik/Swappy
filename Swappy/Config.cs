using System.Collections.Generic;
using System.ComponentModel;
using Swappy.API.Configurations;

namespace Swappy;

public class Config
{
    public string? AccessToken { get; set; }
    
    [Description("Configuration for manually added plugins. These plugins will be added to the list of plugins to check for updates.")]
    public List<ManualConfiguration> ManualConfigurations { get; set; } = [];
}