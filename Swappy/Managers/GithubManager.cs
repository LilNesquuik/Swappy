using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using Octokit;
using Swappy.Configurations;
using Swappy.Helpers;

#if EXILED
using Exiled.API.Interfaces;
using Exiled.Loader;
#endif

namespace Swappy.Managers;

public static class GithubManager
{
    private static Config Config => Entrypoint.Singleton.Config!;

    public static void UpdatePlugin(
    #if EXILED
        IPlugin<IConfig> plugin,
    #else
        Plugin plugin,
    #endif
        PluginConfig pluginConfig)
    {
        _ = Task.Run(() => UpdateAsync(plugin, pluginConfig));
    }

    private static async Task UpdateAsync(
    #if EXILED
        IPlugin<IConfig> plugin,
    #else
        Plugin plugin,
    #endif
        PluginConfig config)
    {
        try
        {
            GitHubClient client = new(new ProductHeaderValue("Swappy", Entrypoint.Singleton.Version.ToString()));
            if (!string.IsNullOrEmpty(config.AccessToken))
                client.Credentials = new Credentials(config.AccessToken);

            Release? release = await GetLatestReleaseAsync(client, config);
            if (release is null)
                return;

            if (!VersionHelper.TryParse(release.TagName, out Version parsedVersion))
            {
                Logger.Warn($"[{plugin.Name}] Version: {release.TagName} doesn't match pattern");
                return;
            }
            
            if (parsedVersion <= plugin.Version)
            {
                Logger.Info($"[{plugin.Name}] Already up to date (v{parsedVersion})");
                return;
            }

            ReleaseAsset? asset = release.Assets.FirstOrDefault(x => x.Name == $"{config.PluginName}.dll");
            if (asset is null)
            {
                Logger.Error($"[{plugin.Name}] No matching .dll found in latest release. Expected: {config.PluginName}.dll");
                return;
            }

            Logger.Info($"[{plugin.Name}] Downloading latest release: {release.TagName}");
            
            IApiResponse<byte[]>? bytes = await client.Connection.GetRaw(new Uri(asset.BrowserDownloadUrl), new Dictionary<string, string>());
        #if EXILED
            File.WriteAllBytes(plugin.GetPath(), bytes.Body);
        #else
            File.WriteAllBytes(plugin.FilePath, bytes.Body);
        #endif
            Logger.Info($"[{plugin.Name}] Successfully updated to v{parsedVersion}");

            if (config.DownloadDependencies)
            {
                ReleaseAsset? dependenciesAsset = release.Assets.FirstOrDefault(x => x.Name == "dependencies.zip");
                if (dependenciesAsset is not null)
                    _ = Task.Run(async () => await DownloadDependencies(client, plugin, dependenciesAsset));
                else
                {
                    Logger.Warn($"[{plugin.Name}] Dependencies don't exist");
                    return;
                }
            }
            
            if (config.ScheduleSoftRestart)
                MainThreadDispatcher.Dispatch(() => ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private static async Task DownloadDependencies(
        GitHubClient client, 
    #if EXILED
        IPlugin<IConfig> plugin,
    #else
        Plugin plugin,
    #endif
        ReleaseAsset asset)
    {
        Logger.Info($"[{plugin.Name}] Downloading dependencies");
                
        IApiResponse<byte[]>? depBytes = await client.Connection.GetRaw(new Uri(asset.BrowserDownloadUrl), new Dictionary<string, string>());
        
        Logger.Debug($"[{plugin.Name}] Dependencies downloaded", Config.Debug);
        
    #if EXILED
        string dependenciesPath = Path.Combine(Exiled.API.Features.Paths.Dependencies, Server.Port.ToString());
    #else
        string dependenciesPath = Path.Combine(PathManager.Dependencies.FullName, Server.Port.ToString());
    #endif
        string zipPath = Path.Combine(dependenciesPath, "dependencies.zip");
        
        File.WriteAllBytes(zipPath, depBytes.Body);
        
        Logger.Debug($"[{plugin.Name}] Dependencies zip file created", Config.Debug);
                
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            Logger.Debug($"[{plugin.Name}] Verifying dependencies integrity", Config.Debug);
                    
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string destinationPath = Path.Combine(dependenciesPath, entry.FullName);

                if (!entry.Name.EndsWith(".dll"))
                {
                    Logger.Debug($"[{plugin.Name}] Skipping non-dll file: {entry.Name}", Config.Debug);
                    continue;
                }
                        
                if (File.Exists(destinationPath))
                {
                    Logger.Debug($"[{plugin.Name}] Deleted old dependencies found in zip: {entry.Name}");
                    File.Delete(destinationPath);
                }
                
                Logger.Debug($"[{plugin.Name}] Extracting file: {entry.Name}", Config.Debug);
                entry.ExtractToFile(destinationPath);
            }
        }
        
        File.Delete(zipPath);
                
        Logger.Info($"[{plugin.Name}] Dependencies successfully extracted");
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