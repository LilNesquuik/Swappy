using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
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
        List<Task> tasks = [];
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
                resource = repositoryPlugin.RepositoryConfiguration;

            tasks.Add(resource.Resolve(plugin.Version, assembly.GetName().Name!, plugin.FilePath));
        }
        
        if (tasks.Count == 0)
            return 0;
        
        await Task.WhenAll(tasks);
        
        ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
        
        return tasks.Count;
    }
}