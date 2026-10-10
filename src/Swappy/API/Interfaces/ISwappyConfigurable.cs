using Swappy.API.Features;

namespace Swappy.API.Interfaces;

/// <summary>
/// Implemented by a plugin that declares where Swappy downloads its updates from. Prefer
/// inheriting from <see cref="ManagedPlugin"/>; use this interface when the plugin already has
/// another base class.
/// </summary>
public interface ISwappyConfigurable
{
    /// <summary>
    /// Gets the resource the plugin's new versions are downloaded from.
    /// </summary>
    public DependencyResource Repository { get; }
}
