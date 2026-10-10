using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace Swappy.Configs.Sync;

/// <summary>
/// Downloads only what this server needs: servers.yml, its layers and its servers/&lt;id&gt;/ folder.
/// </summary>
internal static class GitSource
{
    public static async Task<(string Sha, Dictionary<string, byte[]> Files)> FetchAsync(Config config)
    {
        GitHubClient client = new(new ProductHeaderValue(Bundle.SelfName));
        if (!string.IsNullOrWhiteSpace(config.AccessToken))
            client.Credentials = new Credentials(config.AccessToken);

        string owner = config.RepositoryOwner;
        string name = config.RepositoryName;

        string sha = await client.Repository.Commit.GetSha1(owner, name, config.Ref).ConfigureAwait(false);
        TreeResponse tree = await client.Git.Tree.GetRecursive(owner, name, sha).ConfigureAwait(false);
        if (tree.Truncated)
            throw new InvalidOperationException("Git tree truncated by GitHub (repository too large)");

        Dictionary<string, string> blobs = tree.Tree
            .Where(item => item.Type == TreeType.Blob)
            .ToDictionary(item => item.Path, item => item.Sha, StringComparer.Ordinal);

        if (!blobs.TryGetValue(Bundle.ServersFile, out string serversSha))
            throw new InvalidOperationException($"{Bundle.ServersFile} is missing from the repository root");

        byte[] servers = await GetBlob(client, owner, name, serversSha).ConfigureAwait(false);
        string[] prefixes = Bundle.GetLayers(Bundle.Text(servers), config.ServerId)
            .Select(layer => $"layers/{layer}/")
            .Append($"servers/{config.ServerId}/")
            .ToArray();

        KeyValuePair<string, string>[] wanted = blobs
            .Where(blob => prefixes.Any(prefix => blob.Key.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();
        byte[][] contents = await Task.WhenAll(wanted.Select(blob => GetBlob(client, owner, name, blob.Value))).ConfigureAwait(false);

        Dictionary<string, byte[]> files = new(StringComparer.Ordinal) { [Bundle.ServersFile] = servers };
        for (int i = 0; i < wanted.Length; i++)
            files[wanted[i].Key] = contents[i];

        return (sha, files);
    }

    private static async Task<byte[]> GetBlob(GitHubClient client, string owner, string name, string sha)
    {
        Blob blob = await client.Git.Blob.Get(owner, name, sha).ConfigureAwait(false);
        return blob.Encoding == EncodingType.Base64
            ? Convert.FromBase64String(blob.Content)
            : Encoding.UTF8.GetBytes(blob.Content);
    }
}
