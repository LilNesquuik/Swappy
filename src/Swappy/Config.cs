using System.Collections.Generic;
using System.ComponentModel;
using Swappy.API.Configurations;

namespace Swappy;

/// <summary>
/// Swappy's configuration.
/// </summary>
public class Config
{
    /// <summary>
    /// Gets or sets the GitHub token used for repositories marked as private.
    /// </summary>
    [Description("A personal access token for GitHub. This is required to check for updates on private repositories.")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of minutes between two automatic update checks. A forced
    /// check ignores it.
    /// </summary>
    [Description("The rate limit in minutes for checking updates. Default is 30 minutes.")]
    public int RateLimitMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to check for updates when the server leaves idle
    /// mode. The round restarts right away when something was updated.
    /// </summary>
    [Description("Whether to check for updates when the server exits idle mode. Default is true.")]
    public bool CheckOnExitIdle { get; set; } = true;

    /// <summary>
    /// Gets or sets the Discord webhook notified of every update. Null or empty disables it.
    /// </summary>
    [Description("A Discord webhook URL to notify when a plugin is updated. Leave empty to disable notifications.")]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets the names of the plugins skipped by every update check, compared
    /// case-insensitively.
    /// </summary>
    [Description("Plugins excluded from update checks. Managed by the 'swappy freeze' command, but can also be edited manually.")]
    public List<string> FrozenPlugins { get; set; } = [];

    /// <summary>
    /// Gets or sets the repositories of the plugins that do not declare one themselves.
    /// </summary>
    [Description("Configuration for manually added plugins. These plugins will be added to the list of plugins to check for updates.")]
    public List<ManualConfiguration> ManualConfigurations { get; set; } = [];
}