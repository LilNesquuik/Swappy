using LabApi.Loader.Features.Plugins;
using Swappy.API.Interfaces;

namespace Swappy.API.Features;

/// <summary>
/// A plugin without configuration that declares where Swappy downloads its updates from.
/// </summary>
/// <seealso cref="ManagedPlugin{TConfig}"/>
public abstract class ManagedPlugin : Plugin, ISwappyConfigurable
{
    /// <inheritdoc/>
    public abstract DependencyResource Repository { get; }
}

/// <summary>
/// A plugin with a configuration of type <typeparamref name="TConfig"/> that declares where
/// Swappy downloads its updates from.
/// </summary>
/// <typeparam name="TConfig">The plugin's configuration type.</typeparam>
public abstract class ManagedPlugin<TConfig> : Plugin<TConfig>, ISwappyConfigurable where TConfig : class, new()
{
    /// <inheritdoc/>
    public abstract DependencyResource Repository { get; }
}