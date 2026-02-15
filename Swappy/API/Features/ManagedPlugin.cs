using LabApi.Loader.Features.Plugins;

namespace Swappy.API.Features;

/// <summary>
/// Represents a plugin that is managed by Swappy.
/// </summary>
/// <remarks>
/// This plugin must provide a <see cref="Repository"/> resource for Swappy to check for updates.
/// </remarks>
public abstract class ManagedPlugin : Plugin
{
    public abstract DependencyResource Repository { get; }
}