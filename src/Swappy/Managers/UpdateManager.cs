using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.API.Configurations;
using Swappy.API.Features;
using Swappy.API.Interfaces;

namespace Swappy.Managers;

/// <summary>
/// Runs the update checks.
/// </summary>
public static class UpdateManager
{
    private static int _running;

    /// <summary>
    /// Checks every loaded plugin that implements <see cref="ISwappyConfigurable"/> or has a
    /// <see cref="ManualConfiguration"/>, and downloads the newer versions found.
    /// </summary>
    /// <param name="force">Ignore <see cref="Config.RateLimitMinutes"/>.</param>
    /// <param name="pluginNames">Only check these plugins, compared case-insensitively. Null checks them all.</param>
    /// <returns>The number of plugins updated. Zero when the check was skipped.</returns>
    /// <remarks>
    /// Frozen plugins are skipped. A check started while another one is running is skipped
    /// rather than queued.
    /// </remarks>
    public static async Task<int> CheckForUpdates(bool force = false, ICollection<string>? pluginNames = null)
    {
        if (!force && Swappy.Singleton.LastUpdateCheck + TimeSpan.FromMinutes(Swappy.Singleton.Config.RateLimitMinutes) > DateTimeOffset.UtcNow)
        {
            Logger.Warn("Update check skipped - last check was too recent");
            return 0;
        }

        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            Logger.Warn("Update check skipped - another check is already running");
            return 0;
        }

        try
        {
            List<Task<bool>> tasks = [];
            foreach ((Plugin plugin, Assembly assembly) in PluginLoader.Plugins)
            {
                if (pluginNames != null && !pluginNames.Contains(plugin.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (Swappy.Singleton.Config!.FrozenPlugins.Contains(plugin.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                DependencyResource resource;

                if (plugin is not ISwappyConfigurable repositoryPlugin)
                {
                    ManualConfiguration? manualConfiguration = Swappy.Singleton.Config!.ManualConfigurations.Find(x => x.PluginName == plugin.Name);
                    if (manualConfiguration is null)
                        continue;

                    resource = new GithubResource
                    {
                        Author = manualConfiguration.Author,
                        Repository = manualConfiguration.Repository,
                        UsePreRelease = manualConfiguration.UsePreRelease,
                        IsPrivate = manualConfiguration.IsPrivate,
                    };
                }
                else
                    resource = repositoryPlugin.Repository;

                tasks.Add(resource.Resolve(plugin.Version, assembly.GetName().Name!, plugin.FilePath));
            }

            bool[] results = await Task.WhenAll(tasks);

            Swappy.Singleton.LastUpdateCheck = DateTimeOffset.UtcNow;

            return results.Count(x => x);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}