using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Swappy.API.Configurations;
using Swappy.API.Features;
using Swappy.API.Interfaces;

namespace Swappy.Managers;

public static class UpdateManager
{
    /// <summary>
    /// Checks for updates for all plugins that implement <see cref="ISwappyConfigurable"/> or have a manual configuration in Swappy's config.
    /// schedules a server restart if any updates are found.
    /// </summary>
    /// <returns>the number of plugins that will be checked</returns>
    public static async Task<int> CheckForUpdates()
    {
        if (Swappy.Singleton.LastUpdateCheck + TimeSpan.FromMinutes(Swappy.Singleton.Config.RateLimitMinutes) > DateTimeOffset.UtcNow)
        {
            Logger.Warn("Update check skipped - last check was too recent");
            return 0;
        }
        
        List<Task<bool>> tasks = [];
        foreach ((Plugin plugin, Assembly assembly) in PluginLoader.Plugins)
        {
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
}