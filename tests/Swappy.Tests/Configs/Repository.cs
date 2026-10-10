using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Swappy.Configs.Sync;

namespace Swappy.Tests.Configs;

/// <summary>
/// A fake configuration repository, read from the files embedded under
/// <c>Configs/Repositories/&lt;name&gt;/</c>.
/// </summary>
internal sealed class Repository
{
    private static readonly Assembly Assembly = typeof(Repository).Assembly;

    private Repository(string name)
    {
        string prefix = name + "/";
        Dictionary<string, byte[]> files = Assembly.GetManifestResourceNames()
            .Where(resource => resource.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(resource => resource.Substring(prefix.Length), Read, StringComparer.Ordinal);

        if (files.Count == 0)
            throw new ArgumentException($"no embedded repository named '{name}'", nameof(name));

        const string localPrefix = "local/";
        Files = files.Where(f => !f.Key.StartsWith(localPrefix, StringComparison.Ordinal)).ToDictionary(f => f.Key, f => f.Value, StringComparer.Ordinal);
        Local = files.Where(f => f.Key.StartsWith(localPrefix, StringComparison.Ordinal)).ToDictionary(f => f.Key.Substring(localPrefix.Length), f => f.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the repository's files, by path from its root. Everything except <c>local/</c>.
    /// </summary>
    public Dictionary<string, byte[]> Files { get; }

    /// <summary>
    /// Gets the files of <c>local/</c>, which stand for the server's local override folder, by path
    /// relative to it.
    /// </summary>
    public Dictionary<string, byte[]> Local { get; }

    /// <summary>
    /// Loads the embedded repository <c>Configs/Repositories/&lt;name&gt;/</c>.
    /// </summary>
    public static Repository Load(string name) => new(name);

    /// <summary>
    /// Gets the content of one of the repository's files, as text.
    /// </summary>
    public string Text(string path) => Bundle.Text(Files[path]);

    private static byte[] Read(string resource)
    {
        using Stream stream = Assembly.GetManifestResourceStream(resource)!;
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
