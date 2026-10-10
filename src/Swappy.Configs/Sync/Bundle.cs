using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Swappy.Configs.Sync;

/// <summary>
/// Builds a server's files from the repository: layer selection, variables, merging and mapping to
/// paths on disk.
/// </summary>
/// <remarks>
/// Nothing here touches Unity or LabAPI, so it can be tested outside the game. Paths called
/// <em>logical</em> are relative to a source folder and use <c>/</c>, e.g.
/// <c>labapi/plugins/MyPlugin/config.yml</c>.
/// </remarks>
public static class Bundle
{
    /// <summary>
    /// The plugin's name, also used as its config folder and GitHub user agent.
    /// </summary>
    public const string SelfName = "Swappy.Configs";

    /// <summary>
    /// The file at the repository root that lists each server and its layers.
    /// </summary>
    public const string ServersFile = "servers.yml";

    /// <summary>
    /// The file, in any source folder, that defines the <c>{{ vars.* }}</c> variables.
    /// </summary>
    public const string VarsFile = "vars.yml";

    private static readonly Regex Template = new(@"\{\{\s*(server|vars|secrets|env)\.([A-Za-z0-9_.\-]+)\s*\}\}", RegexOptions.Compiled);

    // A bad push must never be able to disable or break the sync itself.
    private static readonly string[] ExcludedPrefixes = ["labapi/plugins/" + SelfName + "/"];
    private static readonly string[] ExcludedFiles = ["labapi/plugins/Swappy/properties.yml"];

    /// <summary>
    /// When a written file takes effect.
    /// </summary>
    public enum Phase
    {
        /// <summary>Read after Swappy.Configs, applies to the current boot.</summary>
        Now,

        /// <summary>Already read when Swappy.Configs runs, applies to the next boot.</summary>
        NextBoot,
    }

    /// <summary>
    /// The folders and files on disk that logical paths are mapped to.
    /// </summary>
    public sealed class Roots
    {
        /// <summary>Folder of this port's plugin configs, for <c>labapi/plugins/</c>.</summary>
        public string PluginConfigs = string.Empty;

        /// <summary>Folder of the global plugin configs, for <c>labapi/global/</c>.</summary>
        public string GlobalConfigs = string.Empty;

        /// <summary>LabAPI's permissions file, for <c>labapi/permissions.yml</c>.</summary>
        public string Permissions = string.Empty;

        /// <summary>LabAPI's own config file for this port, for <c>labapi/labapi.yml</c>.</summary>
        public string LabApiYml = string.Empty;

        /// <summary>The game's config folder for this port, for <c>game/</c>.</summary>
        public string Game = string.Empty;
    }

    /// <summary>
    /// The outcome of <see cref="Build"/>.
    /// </summary>
    public sealed class Result
    {
        /// <summary>The layers this server uses, in order.</summary>
        public readonly List<string> Layers = [];

        /// <summary>The final content of each file, by logical path.</summary>
        public readonly Dictionary<string, byte[]> Files = new(StringComparer.Ordinal);

        /// <summary>Every problem found. The result must not be applied unless this is empty.</summary>
        public readonly List<string> Errors = [];
    }

    /// <summary>
    /// Reads the layers of a server from <c>servers.yml</c>.
    /// </summary>
    /// <param name="serversYml">Content of <c>servers.yml</c>.</param>
    /// <param name="serverId">Key of the server.</param>
    /// <returns>The layer names in order, or an empty list when the server declares none.</returns>
    /// <exception cref="FormatException">The file is not a map, or <c>layers</c> is not a list of names.</exception>
    /// <exception cref="KeyNotFoundException">The server is not in the file.</exception>
    public static List<string> GetLayers(string serversYml, string serverId)
    {
        if (Parse(serversYml) is not YamlMappingNode root)
            throw new FormatException("servers.yml: the root must be a map");

        if (Child(root, serverId) is not YamlMappingNode server)
            throw new KeyNotFoundException($"servers.yml: server '{serverId}' not found");

        if (Child(server, "layers") is not YamlSequenceNode layers)
            return [];

        return layers.Children
            .Select(n => (n as YamlScalarNode)?.Value ?? throw new FormatException("servers.yml: 'layers' must be a list of names"))
            .ToList();
    }

    /// <summary>
    /// Builds a server's files: its layers in order, then <c>servers/&lt;id&gt;/</c>, then the
    /// local overrides. YAML files present in several sources are merged with
    /// <see cref="Merge"/>; any other file is replaced whole. Placeholders are then replaced.
    /// </summary>
    /// <param name="repo">Repository paths to content: <c>servers.yml</c>, <c>layers/*</c> and <c>servers/&lt;id&gt;/*</c>.</param>
    /// <param name="serverId">Key of this server in <c>servers.yml</c>.</param>
    /// <param name="local">Logical paths to content of the local override folder, applied last.</param>
    /// <param name="lookup">Resolves <c>server.x</c>, <c>secrets.x</c> and <c>env.x</c>; returns null when unknown.</param>
    /// <returns>The files and every error found. Never throws for bad input.</returns>
    public static Result Build(IReadOnlyDictionary<string, byte[]> repo, string serverId, IReadOnlyDictionary<string, byte[]> local, Func<string, string?> lookup)
    {
        Result result = new();

        if (!repo.TryGetValue(ServersFile, out byte[] servers))
        {
            result.Errors.Add("servers.yml is missing from the repository");
            return result;
        }

        try
        {
            result.Layers.AddRange(GetLayers(Text(servers), serverId));
        }
        catch (Exception e)
        {
            result.Errors.Add(e.Message);
            return result;
        }

        List<Dictionary<string, byte[]>> sources = [];
        foreach (string layer in result.Layers)
        {
            Dictionary<string, byte[]> files = SubTree(repo, $"layers/{layer}/");
            if (files.Count == 0)
                result.Errors.Add($"layer '{layer}' is empty or missing");
            sources.Add(files);
        }
        sources.Add(SubTree(repo, $"servers/{serverId}/"));
        sources.Add(local.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal));

        YamlNode? vars = null;
        foreach (Dictionary<string, byte[]> source in sources)
        {
            if (!source.TryGetValue(VarsFile, out byte[] content))
                continue;
            try
            {
                vars = Merge(vars, Parse(Text(content)));
            }
            catch (Exception e)
            {
                result.Errors.Add($"{VarsFile}: invalid YAML ({e.Message})");
            }
        }
        Dictionary<string, string> flatVars = new(StringComparer.Ordinal);
        Flatten(vars, string.Empty, flatVars);

        Dictionary<string, List<byte[]>> contributions = new(StringComparer.Ordinal);
        foreach (Dictionary<string, byte[]> source in sources)
        {
            foreach (KeyValuePair<string, byte[]> file in source.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (file.Key == VarsFile || IsHidden(file.Key))
                    continue;

                if (!contributions.TryGetValue(file.Key, out List<byte[]> list))
                    contributions[file.Key] = list = [];
                list.Add(Render(file.Key, file.Value, flatVars, lookup, result.Errors));
            }
        }

        foreach (KeyValuePair<string, List<byte[]>> entry in contributions)
        {
            string path = entry.Key;
            List<byte[]> parts = entry.Value;

            // Single contribution or non-YAML file: whole-file replace, bytes kept as-is.
            if (!IsYaml(path) || parts.Count == 1)
            {
                byte[] last = parts[parts.Count - 1];
                if (IsYaml(path) && !TryParse(Text(last), out _, out string? error))
                    result.Errors.Add($"{path}: invalid YAML ({error})");
                result.Files[path] = last;
                continue;
            }

            try
            {
                YamlNode? merged = parts.Aggregate<byte[], YamlNode?>(null, (acc, part) => Merge(acc, Parse(Text(part))));
                result.Files[path] = Utf8(merged is null ? string.Empty : Emit(merged));
            }
            catch (Exception e)
            {
                result.Errors.Add($"{path}: invalid YAML ({e.Message})");
            }
        }

        return result;
    }

    /// <summary>
    /// Maps a logical path to the file it is written to, and tells when it takes effect.
    /// </summary>
    /// <param name="logical">The logical path.</param>
    /// <param name="roots">The folders and files on disk.</param>
    /// <param name="target">Full path on disk, empty on error.</param>
    /// <param name="phase">When the file takes effect.</param>
    /// <returns>An error message, or null when <paramref name="target"/> is valid.</returns>
    /// <remarks>
    /// Rejects unknown roots, extensions other than <c>.yml</c>, <c>.yaml</c> and <c>.txt</c>,
    /// paths that leave their root, and the files that control the synchronisation itself.
    /// </remarks>
    public static string? Map(string logical, Roots roots, out string target, out Phase phase)
    {
        target = string.Empty;
        phase = Phase.Now;

        if (ExcludedPrefixes.Any(p => logical.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || ExcludedFiles.Any(f => logical.Equals(f, StringComparison.OrdinalIgnoreCase)))
            return "excluded (protects the synchronisation itself)";

        if (logical == "labapi/permissions.yml")
        {
            target = roots.Permissions;
            return null;
        }

        if (logical == "labapi/labapi.yml")
        {
            target = roots.LabApiYml;
            phase = Phase.NextBoot;
            return null;
        }

        string extension = Path.GetExtension(logical).ToLowerInvariant();
        string root;
        string rest;
        if (TryStrip(logical, "labapi/plugins/", out rest))
        {
            root = roots.PluginConfigs;
        }
        else if (TryStrip(logical, "labapi/global/", out rest))
        {
            root = roots.GlobalConfigs;
        }
        else if (TryStrip(logical, "game/", out rest))
        {
            root = roots.Game;
            phase = Phase.NextBoot;
            if (extension != ".txt")
                return "only .txt files are accepted under game/";
        }
        else
        {
            return "unknown path (expected: labapi/plugins/, labapi/global/, labapi/permissions.yml, labapi/labapi.yml, game/)";
        }

        if (extension is not (".yml" or ".yaml" or ".txt"))
            return $"extension '{extension}' is not allowed";

        if (phase == Phase.Now && rest.IndexOf('/') < 0)
            return "expected: <PluginName>/<file>";

        string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(Path.Combine(rootFull, rest));
        if (!full.StartsWith(rootFull, StringComparison.Ordinal))
            return "leaves the target folder";

        target = full;
        return null;
    }

    /// <summary>
    /// Computes the SHA-256 of a file's content.
    /// </summary>
    /// <returns>The hash as lowercase hexadecimal.</returns>
    public static string Hash(byte[] content)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(content)).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// Decodes a file's content as UTF-8, without its byte order mark.
    /// </summary>
    public static string Text(byte[] content)
    {
        string text = Encoding.UTF8.GetString(content);
        return text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text;
    }

    /// <summary>
    /// Gets a value indicating whether the path has a <c>.yml</c> or <c>.yaml</c> extension.
    /// </summary>
    public static bool IsYaml(string path)
    {
        return path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses YAML without throwing.
    /// </summary>
    /// <param name="text">The YAML text.</param>
    /// <param name="node">The root node, or null for an empty document or on failure.</param>
    /// <param name="error">The parser's message on failure, null otherwise.</param>
    /// <returns>True when the text is valid YAML.</returns>
    public static bool TryParse(string text, out YamlNode? node, out string? error)
    {
        try
        {
            node = Parse(text);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            node = null;
            error = e.Message;
            return false;
        }
    }

    /// <summary>
    /// Parses the first document of a YAML text.
    /// </summary>
    /// <returns>The root node, or null for an empty document.</returns>
    /// <exception cref="YamlDotNet.Core.YamlException">The text is not valid YAML.</exception>
    public static YamlNode? Parse(string text)
    {
        YamlStream stream = new();
        stream.Load(new StringReader(text));
        return stream.Documents.Count == 0 ? null : stream.Documents[0].RootNode;
    }

    /// <summary>
    /// Gets the value of a scalar key in a map.
    /// </summary>
    /// <returns>The value, or null when the key is absent.</returns>
    public static YamlNode? Child(YamlMappingNode map, string key)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> child in map.Children)
        {
            if (child.Key is YamlScalarNode scalar && scalar.Value == key)
                return child.Value;
        }
        return null;
    }

    /// <summary>
    /// Merges two YAML nodes. Maps are merged deeply; lists and scalars are replaced; an empty
    /// override changes nothing.
    /// </summary>
    /// <param name="baseNode">The earlier source.</param>
    /// <param name="overrideNode">The later source, which wins on conflicts.</param>
    /// <returns>A new node. Neither argument is modified.</returns>
    public static YamlNode? Merge(YamlNode? baseNode, YamlNode? overrideNode)
    {
        if (overrideNode is null)
            return baseNode;
        if (baseNode is not YamlMappingNode baseMap || overrideNode is not YamlMappingNode overrideMap)
            return overrideNode;

        YamlMappingNode result = new();
        foreach (KeyValuePair<YamlNode, YamlNode> child in baseMap.Children)
            result.Add(child.Key, child.Value);

        foreach (KeyValuePair<YamlNode, YamlNode> child in overrideMap.Children)
        {
            YamlNode? existingKey = result.Children.Keys.FirstOrDefault(k => SameKey(k, child.Key));
            if (existingKey is null)
                result.Add(child.Key, child.Value);
            else
                result.Children[existingKey] = Merge(result.Children[existingKey], child.Value)!;
        }
        return result;
    }

    private static bool SameKey(YamlNode a, YamlNode b)
    {
        return a is YamlScalarNode sa && b is YamlScalarNode sb ? sa.Value == sb.Value : a.Equals(b);
    }

    private static string Emit(YamlNode node)
    {
        StringWriter writer = new();
        new YamlStream(new YamlDocument(node)).Save(writer, assignAnchors: false);
        string text = writer.ToString().TrimEnd();
        if (text.EndsWith("..."))
            text = text.Substring(0, text.Length - 3).TrimEnd();
        return text + "\n";
    }

    private static void Flatten(YamlNode? node, string prefix, Dictionary<string, string> output)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                output[prefix] = scalar.Value ?? string.Empty;
                break;
            case YamlMappingNode map:
                foreach (KeyValuePair<YamlNode, YamlNode> child in map.Children)
                {
                    if (child.Key is YamlScalarNode key)
                        Flatten(child.Value, prefix.Length == 0 ? key.Value! : $"{prefix}.{key.Value}", output);
                }
                break;
        }
    }

    private static byte[] Render(string path, byte[] content, Dictionary<string, string> vars, Func<string, string?> lookup, List<string> errors)
    {
        string text = Text(content);
        if (!Template.IsMatch(text))
            return content;

        return Utf8(Template.Replace(text, match =>
        {
            string ns = match.Groups[1].Value;
            string key = match.Groups[2].Value;
            string? value = ns == "vars"
                ? vars.TryGetValue(key, out string v) ? v : null
                : lookup($"{ns}.{key}");

            if (value is not null)
                return value;

            errors.Add($"{path}: unknown variable {{{{ {ns}.{key} }}}}");
            return match.Value;
        }));
    }

    private static Dictionary<string, byte[]> SubTree(IReadOnlyDictionary<string, byte[]> repo, string prefix)
    {
        return repo
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(kv => kv.Key.Substring(prefix.Length), kv => kv.Value, StringComparer.Ordinal);
    }

    private static bool TryStrip(string value, string prefix, out string rest)
    {
        bool match = value.StartsWith(prefix, StringComparison.Ordinal);
        rest = match ? value.Substring(prefix.Length) : string.Empty;
        return match;
    }

    private static bool IsHidden(string path)
    {
        return path.Split('/').Any(segment => segment.StartsWith("."));
    }

    private static byte[] Utf8(string text)
    {
        return new UTF8Encoding(false).GetBytes(text);
    }
}
