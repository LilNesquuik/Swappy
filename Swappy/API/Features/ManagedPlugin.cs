using LabApi.Loader.Features.Plugins;
using Swappy.API.Interfaces;

namespace Swappy.API.Features;

/// <summary>
/// Represents a plugin that is managed by Swappy.
/// </summary>
/// <remarks>
/// This plugin must provide a <see cref="Repository"/> resource for Swappy to check for updates.
/// </remarks>
public abstract class ManagedPlugin : Plugin, ISwappyConfigurable
{
    public abstract DependencyResource Repository { get; }
}

public abstract class ManagedPlugin<TConfig> : Plugin<TConfig>, ISwappyConfigurable where TConfig : class, new()
{
    public abstract DependencyResource Repository { get; }
}