using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using Octokit;
using Swappy.Configurations;
using Swappy.Helpers;

namespace Swappy.Managers;

public static class GithubManager
{
    private static Config Config => Entrypoint.Singleton.Config!;

    public static void UpdatePlugin(Plugin plugin, PluginConfig pluginConfig)
    {
        _ = Task.Run(() => UpdateAsync(plugin, pluginConfig));
    }

    private static async Task UpdateAsync(Plugin plugin, PluginConfig config)
    {
        try
        {
            if (!File.Exists(plugin.FilePath))
            {
                Logger.Error($"Plugin file for {plugin.Name} does not exist at {plugin.FilePath}");
                return;
            }

            GitHubClient client = new(new ProductHeaderValue("Swappy", Entrypoint.Singleton.Version.ToString()));
            if (!string.IsNullOrEmpty(config.AccessToken))
                client.Credentials = new Credentials(config.AccessToken);

            Release? release = await GetLatestReleaseAsync(client, config);
            if (release is null)
                return;

            if (!VersionHelper.TryParse(release.TagName, out Version parsedVersion))
            {
                Logger.Warn($"Plugin {plugin.Name} version: {release.TagName} doesn't match pattern.");
                return;
            }
            
            if (parsedVersion <= plugin.Version)
            {
                Logger.Info($"Plugin {plugin.Name} is already up to date (v{parsedVersion})");
                return;
            }

            ReleaseAsset? asset = release.Assets.FirstOrDefault(x => x.Name == $"{config.PluginName}.dll");
            if (asset is null)
            {
                Logger.Error($"No matching .dll found for {plugin.Name} in latest release");
                return;
            }

            Logger.Info($"Downloading release: {release.TagName} for Plugin: {plugin.Name}");
            
            IApiResponse<byte[]>? bytes = await client.Connection.GetRaw(new Uri(asset.BrowserDownloadUrl), new Dictionary<string, string>());
            File.WriteAllBytes(plugin.FilePath, bytes.Body);
            
            Logger.Info($"Plugin {plugin.Name} updated to v{parsedVersion}");

            if (config.DownloadDependencies)
            {
                ReleaseAsset? depAsset = release.Assets.FirstOrDefault(x => x.Name == "dependencies.zip");
                if (depAsset is not null)
                {
                    Logger.Info($"Downloading dependencies for Plugin: {plugin.Name}");
                    
                    IApiResponse<byte[]>? depBytes = await client.Connection.GetRaw(new Uri(depAsset.BrowserDownloadUrl), new Dictionary<string, string>());
                    string dependenciesPath = PathManager.Dependencies.FullName;
                    Directory.CreateDirectory(dependenciesPath);
                    string zipPath = Path.Combine(dependenciesPath, "dependencies.zip");
                    File.WriteAllBytes(zipPath, depBytes.Body);
                    ZipFile.ExtractToDirectory(zipPath, dependenciesPath);
                    File.Delete(zipPath);
                    
                    Logger.Info($"Dependencies extracted for Plugin: {plugin.Name}");
                }
            }
            


            if (config.SoftRestart)
                MainThreadDispatcher.Dispatch(() => ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private static async Task<Release?> GetLatestReleaseAsync(GitHubClient client, PluginConfig config)
    {
        IReadOnlyList<Release>? releases = await client.Repository.Release.GetAll(config.RepositoryOwner, config.RepositoryName);
        if (!releases.IsEmpty()) 
            return releases.FirstOrDefault(r => !r.Draft && !r.Prerelease);
        
        Logger.Error($"No releases found for {config.RepositoryOwner}/{config.RepositoryName}");
        return null;

    }


}