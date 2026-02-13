using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using Octokit;
using Swappy.API.Extensions;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Swappy.API.Abstractions;

public abstract class DependencyResource
{
    public abstract Task Resolve(Version current);
}

public class GithubResource : DependencyResource
{
    public required string Repository { get; init; }
    public required string Author { get; init; }
    public required string FileName { get; init; }
    public bool IsPrivate { get; init; }
    public bool UsePreRelease { get; init; } 
    
    public Action<Version>? Callback { get; init; }
    
    public override async Task Resolve(Version current)
    {
        string token = string.Empty;
        if (IsPrivate && !string.IsNullOrEmpty(Swappy.Singleton.Config!.AccessToken))
            token = Swappy.Singleton.Config!.AccessToken!;
        
        try
        {
            ProductHeaderValue product = new ProductHeaderValue("Swappy", Swappy.Singleton.Version.ToString());

            GitHubClient client = !IsPrivate
                ? new GitHubClient(product)
                : new GitHubClient(product)
                {
                    Credentials = new Credentials(token)
                };

            IReadOnlyList<Release>? releases = await client.Repository.Release.GetAll(Author, Repository);
            Release? targetRelease = GetValid(current, releases, out GithubReason reason);
            if (targetRelease == null)
            {
                Logger.Warn($"No valid release found for {FileName}. Reason: {reason}");
                return;
            }

            Logger.Info($"Found release {targetRelease.TagName} for {FileName}");

            ReleaseAsset? asset = targetRelease.Assets.FirstOrDefault(a => a.Name.Equals($"{FileName}.dll", StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                Logger.Error($"Asset '{FileName}.dll' not found in release '{targetRelease.TagName}'.");
                return;
            }

            string outputDirectory = PathManager.Dependencies.FullName;
            Directory.CreateDirectory(outputDirectory);

            string outputPath = Path.Combine(outputDirectory, $"{FileName}.dll");

            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Swappy");

            if (!string.IsNullOrWhiteSpace(token))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpResponseMessage? response = await httpClient.GetAsync(asset.BrowserDownloadUrl);
            response.EnsureSuccessStatusCode();

            byte[]? bytes = await response.Content.ReadAsByteArrayAsync();

            if (bytes.Length == 0)
            {
                Logger.Error("Empty file downloaded.");
                return;
            }

            await File.WriteAllBytesAsync(outputPath, bytes);
            
            MainThreadDispatcher.Dispatch(() =>
            {
                Logger.Info($"Successfully downloaded {FileName}.dll from {Author}/{Repository} to: {targetRelease.TagName}");
                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to resolve GitHub resource: {ex.Message}");
            throw;
        }
        
        Callback?.Invoke(current);
    }

    public Release? GetValid(Version current, IReadOnlyList<Release> releases, out GithubReason reason)
    {
        reason = GithubReason.NoValidReleaseFound;
        
        foreach (Release release in releases)
        {
            if (release.Draft)
                continue;

            if (release.Prerelease && !UsePreRelease)
                continue;

            Version? tagVersion = release.TagName.ToVersion();
            if (tagVersion == null)
                continue;

            if (tagVersion <= current)
            {
                reason = GithubReason.AlreadyUpToDate;
                continue;
            }

            reason = GithubReason.Done;
            return release;
        }
        
        return null;
    }
    
    public enum GithubReason
    {
        Done,
        NoValidReleaseFound,
        AssetNotFound,
        EmptyFileDownloaded,
        AlreadyUpToDate,
        Other,
    }
    
    public override string ToString()
    {
        return $"GithubResource: {Author}/{Repository}";
    }
}