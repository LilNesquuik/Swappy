namespace Swappy.API.Configurations;

/// <summary>
/// Declares the GitHub repository of a plugin that does not implement
/// <see cref="Interfaces.ISwappyConfigurable"/> itself.
/// </summary>
public class ManualConfiguration
{
    /// <summary>
    /// Gets the <c>Name</c> of the plugin to update. The match is exact and case-sensitive.
    /// </summary>
    public string PluginName { get; init; }

    /// <summary>
    /// Gets the GitHub user or organization that owns the repository.
    /// </summary>
    public string Author { get; init; }

    /// <summary>
    /// Gets the name of the repository.
    /// </summary>
    public string Repository { get; init; }

    /// <summary>
    /// Gets a value indicating whether the repository is private, in which case requests are
    /// authenticated with <see cref="Config.AccessToken"/>.
    /// </summary>
    public bool IsPrivate { get; init; }

    /// <summary>
    /// Gets a value indicating whether pre-releases are considered.
    /// </summary>
    public bool UsePreRelease { get; init; }
}