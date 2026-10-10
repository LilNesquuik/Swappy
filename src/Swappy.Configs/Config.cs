using System.Collections.Generic;
using System.ComponentModel;

namespace Swappy.Configs;

/// <summary>
/// Swappy.Configs' configuration. Changes apply on the next server start.
/// </summary>
public class Config
{
    /// <summary>
    /// Gets or sets this server's key in the repository's <c>servers.yml</c>. Empty disables the
    /// synchronisation.
    /// </summary>
    [Description("Key of this server in the repository's servers.yml (e.g. rp1). Empty = synchronisation disabled.")]
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub user or organization that owns the configuration repository.
    /// </summary>
    [Description("Owner of the configuration repository.")]
    public string RepositoryOwner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the configuration repository.
    /// </summary>
    [Description("Name of the configuration repository.")]
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch, tag or commit SHA to apply.
    /// </summary>
    [Description("Branch, tag or commit SHA to apply.")]
    public string Ref { get; set; } = "main";

    /// <summary>
    /// Gets or sets the GitHub token. Required for a private repository, and recommended
    /// otherwise because of GitHub's rate limit.
    /// </summary>
    [Description("GitHub personal access token. Required for private repositories, recommended otherwise (rate limit).")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets how many seconds the server start may block while fetching. On timeout, the
    /// files already on disk are kept.
    /// </summary>
    [Description("Maximum time in seconds the server start is blocked while fetching. On timeout, files on disk are kept.")]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether an unknown key in a plugin config blocks the
    /// synchronisation, instead of only being reported as a warning.
    /// </summary>
    [Description("If true, an unknown key in a plugin config blocks the synchronisation. If false, it is only a warning.")]
    public bool StrictUnknownKeys { get; set; }

    /// <summary>
    /// Gets or sets the values available in the repository as <c>{{ secrets.&lt;name&gt; }}</c>.
    /// They stay on the server and must never be committed.
    /// </summary>
    [Description("Local secrets usable in the repository as {{ secrets.name }}. Never commit them.")]
    public Dictionary<string, string> Secrets { get; set; } = new();
}
