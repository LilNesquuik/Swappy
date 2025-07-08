using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using Octokit;
using Swappy.Configurations;
using Swappy.Helpers;
using ProductHeaderValue = Octokit.ProductHeaderValue;

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
                client.Credentials = new Credentials(config.AccessToken, AuthenticationType.Bearer);

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
            
            if (Config.FullDebug)
                foreach (ReleaseAsset releaseAsset in release.Assets)
                    Logger.Debug($"Asset: {releaseAsset.Name}, Size: {releaseAsset.Size} bytes, Url: {releaseAsset.Url}");

            ReleaseAsset? asset = release.Assets.FirstOrDefault(x => x.Name == $"{config.PluginName}.dll");
            if (asset is null)
            {
                Logger.Error($"[{plugin.Name}] No matching .dll found in latest release. Expected: {config.PluginName}.dll");
                return;
            }

            Logger.Info($"[{plugin.Name}] Downloading latest release: {release.TagName}, url: {asset.Url}");

        #if EXILED
            string pluginPath = plugin.GetPath();
        #else
            string pluginPath = plugin.FilePath;
        #endif
            
            if (!await DownloadAsync(plugin.Name, asset.Url, pluginPath))
            {
                Logger.Error($"[{plugin.Name}] Failed to download plugin asset");
                return;
            }
            
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
                MainThreadDispatcher.Dispatch(() =>
                {
                    Logger.Info($"[{plugin.Name}] Scheduling server restart after update");
                    
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                });
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
        
    #if EXILED
        string dependenciesPath = Path.Combine(Exiled.API.Features.Paths.Dependencies, Server.Port.ToString());
    #else
        string dependenciesPath = Path.Combine(PathManager.Dependencies.FullName, Server.Port.ToString());
    #endif
        string zipPath = Path.Combine(dependenciesPath, "dependencies.zip");
        
        if (!await DownloadAsync(plugin.Name, asset.Url, zipPath))
        {
            Logger.Error($"[{plugin.Name}] Failed to download dependencies from {asset.Url}");
            return;
        }
        
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
        
        Logger.Debug($"Found {releases.Count} releases for {config.RepositoryOwner}/{config.RepositoryName}", Config.Debug);
        if (Config.FullDebug)
            foreach (Release release in releases)
                Logger.Debug($"Release: {release.TagName}, Draft: {release.Draft}, Prerelease: {release.Prerelease}", Config.Debug);
        
        if (!releases.IsEmpty()) 
            return releases.FirstOrDefault(r => !r.Draft && !r.Prerelease);
        
        Logger.Error($"No releases found for {config.RepositoryOwner}/{config.RepositoryName}");
        return null;

    }

    private static async Task<bool> DownloadAsync(string name, string url, string destination)
    {
        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Swappy/1.0");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Octet));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Config.Configurations.FirstOrDefault(c => c.PluginName == name)?.AccessToken ?? string.Empty);
        
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            
            File.WriteAllBytes(destination, content);
            Logger.Debug($"[{name}] Successfully downloaded from {url} to {destination}", Config.Debug);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"[{name}] Failed to download from {url}: {ex.Message}");
            return false;
        }
    }
}