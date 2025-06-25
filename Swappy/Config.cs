using System;
using System.Collections.Generic;
using System.ComponentModel;
using Swappy.Configurations;
using Utils.NonAllocLINQ;

namespace Swappy;

public class Config
{
    public bool Debug { get; set; }

    [Description("Leave this blank after you have read and acknowledged the warning message")]
    public string Warning { get; set; } =
        "Swappy automatically downloads and replaces plugin .dll files from the latest releases of the repositories you configure. " +
        "Make sure you trust the repositories you enable AutoUpdate on. Swappy does not verify the integrity or source of the code — if a repository gets compromised, malicious code could be deployed directly to your server.";

    [Description("List of plugins configurations for auto-update. The plugin itself is configured to auto-update by default, but can be removed from this list if desired.")]
    public List<PluginConfig> Configurations { get; set; } = new()
    {
        new PluginConfig
        {
            PluginName = "Swappy",
            RepositoryOwner = "LilNesquuik",
            RepositoryName = "Swappy",
            AccessToken = null,
            UpdateOnStartup = true,
            SoftRestart = true
        }
    };
    
    public bool TryGetConfig(string pluginName, out PluginConfig config)
    {
        return Configurations.TryGetFirst(c => c.PluginName == pluginName, out config);
    }
}