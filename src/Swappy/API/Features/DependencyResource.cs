using System;
using System.Threading.Tasks;

namespace Swappy.API.Features;

/// <summary>
/// A place a plugin's new versions are downloaded from.
/// </summary>
/// <seealso cref="GithubResource"/>
public abstract class DependencyResource
{
    /// <summary>
    /// Looks for a version newer than <paramref name="current"/> and, if there is one, writes it
    /// over <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="current">The version of the plugin currently loaded.</param>
    /// <param name="fileName">The plugin's assembly name, without the <c>.dll</c> extension.</param>
    /// <param name="outputPath">Full path of the plugin's DLL.</param>
    /// <returns>True when a new version was written.</returns>
    /// <remarks>
    /// Called from a background thread. Swappy only counts the results: scheduling a restart so
    /// that the new DLL is loaded is up to the implementation.
    /// </remarks>
    public abstract Task<bool> Resolve(Version current, string fileName, string outputPath);
}