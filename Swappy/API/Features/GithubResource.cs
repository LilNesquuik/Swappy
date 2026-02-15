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

namespace Swappy.API.Features;

/// <summary>
/// Represents a GitHub resource. It can be used to check for updates and download new versions of plugins from GitHub releases.
/// </summary>
public class GithubResource : DependencyResource
{
    /// <summary>
    /// The name of the GitHub repository (e.g. "LilNesquuik").
    /// </summary>
    public required string Repository { get; init; }
    
    /// <summary>
    /// The name of the Github repository author (e.g. "Swappy").
    /// </summary>
    public required string Author { get; init; }
    
    /// <summary>
    /// Indicates whether the repository is private.
    /// If true, the resolver will use the access token provided in Swappy's config to authenticate with GitHub and access private repositories.
    /// If false, it will access public repositories without authentication.
    /// Note: If the repository is private and no access token is provided, the resolver will fail to access the repository and log an error.
    /// </summary>
    public bool IsPrivate { get; init; }
    
    /// <summary>
    /// Indicates whether pre-release versions should be considered when checking for updates.
    /// </summary>
    public bool UsePreRelease { get; init; } 
    
    public override async Task Resolve(Version current, string fileName)
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
                Logger.Warn($"No valid release found for {fileName}. Reason: {reason}");
                return;
            }

            Logger.Info($"Found release {targetRelease.TagName} for {fileName}");

            ReleaseAsset? asset = targetRelease.Assets.FirstOrDefault(a => a.Name.Equals($"{fileName}.dll", StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                Logger.Error($"Asset '{fileName}.dll' not found in release '{targetRelease.TagName}'.");
                return;
            }

            string outputDirectory = PathManager.Dependencies.FullName;
            Directory.CreateDirectory(outputDirectory);

            string outputPath = Path.Combine(outputDirectory, $"{fileName}.dll");

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
                Logger.Info($"Successfully downloaded {fileName}.dll from {Author}/{Repository} to: {targetRelease.TagName}");
                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to resolve GitHub resource: {ex.Message}");
            throw;
        }
    }

    private Release? GetValid(Version current, IReadOnlyList<Release> releases, out GithubReason reason)
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