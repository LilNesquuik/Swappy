using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using Octokit;
using Swappy.API.Extensions;
using Swappy.Managers;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Swappy.API.Features;

/// <summary>
/// Downloads a plugin's new versions from the releases of a GitHub repository.
/// </summary>
/// <remarks>
/// A release is picked when it is not a draft, its tag reads as a version higher than the loaded
/// one (see <see cref="Extensions.VersionExtensions.ToVersion"/>), and it has an asset named
/// <c>&lt;assembly name&gt;.dll</c>.
/// </remarks>
public class GithubResource : DependencyResource
{
    /// <summary>
    /// Gets the name of the repository (e.g. <c>Swappy</c>).
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Gets the GitHub user or organization that owns the repository (e.g. <c>LilNesquuik</c>).
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Gets a value indicating whether the repository is private, in which case requests are
    /// authenticated with <see cref="Config.AccessToken"/>.
    /// </summary>
    /// <remarks>
    /// A private repository without a token cannot be read: the check fails and logs an error.
    /// </remarks>
    public bool IsPrivate { get; init; }

    /// <summary>
    /// Gets a value indicating whether pre-releases are considered.
    /// </summary>
    public bool UsePreRelease { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// The previous DLL is kept next to the new one as <c>.bak</c>, for
    /// <see cref="Commands.RollbackCommand"/>. On success, the webhook is notified and the server
    /// restarts at the end of the round.
    /// </remarks>
    public override async Task<bool> Resolve(Version current, string fileName, string outputPath)
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

            // ponytail: GetLatest = 1 lighter API call; only prereleases need the full listing
            IReadOnlyList<Release> releases = UsePreRelease
                ? await client.Repository.Release.GetAll(Author, Repository)
                : [await client.Repository.Release.GetLatest(Author, Repository)];

            Release? targetRelease = GetValid(current, releases, out GithubReason reason);
            if (targetRelease == null)
            {
                Logger.Warn($"No valid release found for {fileName}. Reason: {reason}");
                return false;
            }

            Logger.Info($"Found release {targetRelease.TagName} for {fileName}");

            ReleaseAsset? asset = targetRelease.Assets.FirstOrDefault(a => a.Name.Equals($"{fileName}.dll", StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                Logger.Error($"Asset '{fileName}.dll' not found in release '{targetRelease.TagName}'.");
                return false;
            }

            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Swappy");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Octet));

            if (!string.IsNullOrWhiteSpace(token))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpResponseMessage? response = await httpClient.GetAsync(IsPrivate ? asset.Url : asset.BrowserDownloadUrl);
            response.EnsureSuccessStatusCode();

            byte[]? bytes = await response.Content.ReadAsByteArrayAsync();

            if (bytes.Length == 0)
            {
                Logger.Error("Empty file downloaded.");
                return false;
            }

            string tempPath = outputPath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, bytes);

            if (File.Exists(outputPath))
                File.Replace(tempPath, outputPath, outputPath + ".bak");
            else
                File.Move(tempPath, outputPath);

            _ = WebhookNotifier.SendUpdate(fileName, current, targetRelease.TagName, targetRelease.Body);

            MainThreadDispatcher.Dispatch(() =>
            {
                Logger.Info($"Successfully downloaded {fileName}.dll from {Author}/{Repository} to: {targetRelease.TagName}");
                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
            });

            return true;
        }
        catch (RateLimitExceededException ex)
        {
            Logger.Error($"GitHub rate limit exceeded while checking {fileName}. Resets at {ex.Reset:u}.");
            return false;
        }
        catch (NotFoundException)
        {
            Logger.Warn($"No release found for {fileName} ({Author}/{Repository}).");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to resolve GitHub resource: {ex.Message}");
            return false;
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
    
    /// <summary>
    /// Why a release was or was not selected, as written in the log.
    /// </summary>
    public enum GithubReason
    {
        /// <summary>A newer release was found.</summary>
        Done,

        /// <summary>No release is usable: drafts, skipped pre-releases, or tags without a version.</summary>
        NoValidReleaseFound,

        /// <summary>The release has no asset named after the assembly.</summary>
        AssetNotFound,

        /// <summary>The downloaded asset was empty.</summary>
        EmptyFileDownloaded,

        /// <summary>The newest usable release is not higher than the loaded version.</summary>
        AlreadyUpToDate,

        /// <summary>Any other failure.</summary>
        Other,
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"GithubResource: {Author}/{Repository}";
    }
}