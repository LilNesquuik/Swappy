using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Configuration;
using LabApi.Loader.Features.Yaml;
using YamlDotNet.RepresentationModel;

namespace Swappy.Configs.Sync;

/// <summary>
/// Fetch → build → validate → write, all or nothing. Runs on the main thread during this plugin's Enable,
/// i.e. before every other plugin reads its configuration.
/// </summary>
internal static class ConfigSync
{
    private const int KeptBackups = 5;

    public sealed class Report
    {
        public string? Sha;
        public List<string> Layers = [];
        public readonly List<string> Written = [];
        public readonly List<string> Warnings = [];
        public readonly List<string> Errors = [];
        public DateTime Time = DateTime.Now;
    }

    public sealed class ManagedFile
    {
        public string Target { get; set; } = string.Empty;
        public Bundle.Phase Phase { get; set; }
        /// <summary>Hash of the content we rendered.</summary>
        public string Rendered { get; set; } = string.Empty;
        /// <summary>Hash on disk once LabAPI rewrote it (recorded on PluginsEnabled), used to detect local edits.</summary>
        public string? Disk { get; set; }
    }

    public sealed class State
    {
        public string? AppliedSha { get; set; }
        public Dictionary<string, ManagedFile> Files { get; set; } = new();
    }

    public static Report? LastReport { get; private set; }

    public static bool RestartPending { get; set; }

    private static bool _succeeded;

    private static string WorkDir => Path.Combine(PathManager.LabApi.FullName, "swappy-configs", Server.Port.ToString());

    private static string StatePath => Path.Combine(WorkDir, "state.yml");

    private static string LocalDir => Path.Combine(WorkDir, "local");

    public static void Run(Config config)
    {
        Report report = new();
        LastReport = report;

        if (string.IsNullOrWhiteSpace(config.ServerId) || string.IsNullOrWhiteSpace(config.RepositoryOwner) || string.IsNullOrWhiteSpace(config.RepositoryName))
        {
            report.Warnings.Add("server_id / repository_owner / repository_name not set: synchronisation disabled");
            Log(report);
            return;
        }

        try
        {
            Sync(config, report);
        }
        catch (Exception e)
        {
            report.Errors.Add(e.ToString());
        }

        Log(report);
    }

    /// <summary>Called on PluginsEnabled: every plugin has read (and LabAPI rewritten) its config.</summary>
    public static void RecordDiskHashes()
    {
        if (!_succeeded)
            return;

        try
        {
            State state = LoadState();
            foreach (ManagedFile file in state.Files.Values.Where(f => f.Phase == Bundle.Phase.Now))
                file.Disk = File.Exists(file.Target) ? Bundle.Hash(File.ReadAllBytes(file.Target)) : null;
            SaveState(state);
        }
        catch (Exception e)
        {
            Logger.Error($"[{Bundle.SelfName}] Could not save the state after loading: {e}");
        }
    }

    public static string Describe(Config config)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Server: {config.ServerId} · repository: {config.RepositoryOwner}/{config.RepositoryName}@{config.Ref}");

        State state = LoadState();
        builder.AppendLine($"Applied commit: {state.AppliedSha ?? "none"} · managed files: {state.Files.Count}");

        Report? report = LastReport;
        if (report is not null)
        {
            builder.AppendLine($"Last sync: {report.Time:yyyy-MM-dd HH:mm:ss} · commit {report.Sha ?? "?"} · layers [{string.Join(", ", report.Layers)}]");
            builder.AppendLine($"Written ({report.Written.Count}): {string.Join(", ", report.Written)}");
            foreach (string warning in report.Warnings)
                builder.AppendLine("WARN  " + warning);
            foreach (string error in report.Errors)
                builder.AppendLine("ERROR " + error);
        }

        builder.AppendLine($"Restart pending: {(RestartPending ? "yes" : "no")}");
        return builder.ToString();
    }

    private static void Sync(Config config, Report report)
    {
        // 1. Fetch. Task.Run + ConfigureAwait(false) everywhere: blocking on the Unity main thread must not deadlock.
        Task<(string Sha, Dictionary<string, byte[]> Files)> fetch = Task.Run(() => GitSource.FetchAsync(config));
        try
        {
            if (!fetch.Wait(TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds))))
            {
                report.Errors.Add($"timed out after {config.TimeoutSeconds}s, files on disk kept");
                return;
            }
        }
        catch (AggregateException e)
        {
            report.Errors.Add($"fetch failed, files on disk kept: {e.InnerException?.Message ?? e.Message}");
            return;
        }

        (string sha, Dictionary<string, byte[]> repo) = fetch.Result;
        report.Sha = sha;

        // 2. Build.
        Bundle.Result bundle = Bundle.Build(repo, config.ServerId, ReadLocal(), key => Lookup(key, config, sha));
        report.Layers = bundle.Layers;
        report.Errors.AddRange(bundle.Errors);

        // 3. Map + validate everything before touching the disk.
        Bundle.Roots roots = Roots();
        List<(string Logical, string Target, Bundle.Phase Phase, byte[] Content)> plan = [];
        foreach (KeyValuePair<string, byte[]> file in bundle.Files.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            string? error = Bundle.Map(file.Key, roots, out string target, out Bundle.Phase phase);
            if (error is not null)
            {
                report.Errors.Add($"{file.Key} : {error}");
                continue;
            }

            Validate(file.Key, file.Value, config, report);
            plan.Add((file.Key, target, phase, file.Value));
        }

        if (report.Errors.Count > 0)
        {
            report.Errors.Insert(0, "synchronisation cancelled, no file written");
            return;
        }

        // 4. Apply.
        State state = LoadState();
        string backupDir = Path.Combine(WorkDir, "backup", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        bool restart = false;

        foreach ((string logical, string target, Bundle.Phase phase, byte[] content) in plan)
        {
            string hash = Bundle.Hash(content);
            state.Files.TryGetValue(logical, out ManagedFile? known);
            string? disk = File.Exists(target) ? Bundle.Hash(File.ReadAllBytes(target)) : null;

            // ponytail: files read before us (Phase.NextBoot) are rewritten by LabAPI at every boot (PluginLoader.cs:223-224),
            // so local-edit detection only applies to Phase.Now files, otherwise it would restart in a loop.
            bool drift = phase == Bundle.Phase.Now && known?.Disk is not null && disk != known.Disk;
            bool write = known is null
                ? disk != hash
                : known.Rendered != hash || drift || disk is null;

            if (drift)
                report.Warnings.Add($"{logical}: modified locally since the last sync, backed up then overwritten");

            if (write)
            {
                Backup(target, logical, backupDir);
                WriteAtomic(target, content);
                report.Written.Add(logical);
                restart |= phase == Bundle.Phase.NextBoot;
            }

            state.Files[logical] = new ManagedFile
            {
                Target = target,
                Phase = phase,
                Rendered = hash,
                Disk = write ? null : known?.Disk,
            };
        }

        // ponytail: never delete. A file removed from the repo (or a layer removed from servers.yml) is just no longer managed.
        foreach (string gone in state.Files.Keys.Except(plan.Select(p => p.Logical)).ToList())
        {
            report.Warnings.Add($"{gone}: removed from the repository, left in place and no longer managed");
            state.Files.Remove(gone);
        }

        state.AppliedSha = sha;
        SaveState(state);
        PruneBackups();

        _succeeded = true;
        RestartPending = restart;
    }

    private static void Validate(string logical, byte[] content, Config config, Report report)
    {
        string[] parts = logical.Split('/');
        if (parts.Length < 4 || parts[0] != "labapi" || parts[1] != "plugins" || !Bundle.IsYaml(logical))
            return;

        string pluginName = parts[2];
        string fileName = string.Join("/", parts.Skip(3));

        Plugin? plugin = PluginLoader.Plugins.Keys.FirstOrDefault(p => p.Name == pluginName);
        if (plugin is null)
        {
            Plugin? caseMismatch = PluginLoader.Plugins.Keys.FirstOrDefault(p => string.Equals(p.Name, pluginName, StringComparison.OrdinalIgnoreCase));
            report.Warnings.Add(caseMismatch is null
                ? $"{logical}: no loaded plugin named '{pluginName}', orphan file"
                : $"{logical}: no plugin named '{pluginName}', did you mean '{caseMismatch.Name}'? (case)");
            return;
        }

        Type? type = fileName == "properties.yml" ? typeof(Properties) : ConfigTypeOf(plugin, fileName);
        if (type is null)
            return;

        string text = Bundle.Text(content);
        object? parsed;
        try
        {
            parsed = YamlConfigParser.Deserializer.Deserialize(text, type);
        }
        catch (Exception e)
        {
            report.Errors.Add($"{logical}: incompatible with {type.Name} ({e.Message})");
            return;
        }

        if (parsed is null)
            return;

        // The deserializer ignores unknown keys silently (YamlConfigParser.cs:19): compare with what the type can hold.
        List<string> unknown = [];
        CollectUnknownKeys(Bundle.Parse(text), Bundle.Parse(YamlConfigParser.Serializer.Serialize(parsed)), string.Empty, unknown);
        foreach (string key in unknown)
            (config.StrictUnknownKeys ? report.Errors : report.Warnings).Add($"{logical}: unknown key '{key}', ignored by {plugin.Name}");
    }

    /// <returns>TConfig when <paramref name="fileName"/> is the main config file of a Plugin&lt;TConfig&gt;.</returns>
    private static Type? ConfigTypeOf(Plugin plugin, string fileName)
    {
        for (Type? type = plugin.GetType(); type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Plugin<>))
                continue;

            string configFileName = type.GetProperty(nameof(Plugin<object>.ConfigFileName))?.GetValue(plugin) as string ?? string.Empty;
            if (!Bundle.IsYaml(configFileName))
                configFileName += ".yml"; // same rule as ConfigurationLoader.GetConfigPath

            return configFileName == fileName ? type.GetGenericArguments()[0] : null;
        }
        return null;
    }

    private static void CollectUnknownKeys(YamlNode? given, YamlNode? known, string path, List<string> unknown)
    {
        if (given is not YamlMappingNode givenMap || known is not YamlMappingNode knownMap)
            return;

        foreach (KeyValuePair<YamlNode, YamlNode> child in givenMap.Children)
        {
            if (child.Key is not YamlScalarNode key)
                continue;

            string keyPath = path.Length == 0 ? key.Value! : $"{path}.{key.Value}";
            YamlNode? match = Bundle.Child(knownMap, key.Value!);
            if (match is null)
                unknown.Add(keyPath);
            else
                CollectUnknownKeys(child.Value, match, keyPath, unknown);
        }
    }

    private static string? Lookup(string key, Config config, string sha)
    {
        int dot = key.IndexOf('.');
        string ns = key.Substring(0, dot);
        string name = key.Substring(dot + 1);

        return ns switch
        {
            "server" => name switch
            {
                "id" => config.ServerId,
                "port" => Server.Port.ToString(),
                "ref" => config.Ref,
                "sha" => sha,
                _ => null,
            },
            "secrets" => config.Secrets.TryGetValue(name, out string secret) ? secret : null,
            "env" => Environment.GetEnvironmentVariable(name),
            _ => null,
        };
    }

    private static Bundle.Roots Roots()
    {
        string port = Server.Port.ToString();
        return new Bundle.Roots
        {
            PluginConfigs = Path.Combine(PathManager.Configs.FullName, port),
            GlobalConfigs = Path.Combine(PathManager.Configs.FullName, "global"),
            Permissions = Path.Combine(PathManager.Configs.FullName, "permissions.yml"),
            LabApiYml = Path.Combine(PathManager.LabApi.FullName, $"LabApi-{port}.yml"),
            Game = GameConfigFolder(port),
        };
    }

    // ponytail: mirrors FileManager.GetAppFolder(serverConfig: true) (FileManager.cs:19-29) without calling it:
    // its RefreshAppFolder reads ConfigFile.HosterPolicy, which may trigger the game config load early (ConfigFile.cs:27).
    private static string GameConfigFolder(string port)
    {
        if (typeof(FileManager).GetField("_configfolder", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is string custom && custom.Length > 0)
            return custom;

        // Hoster policy (gamedir_for_configs) puts game configs in AppData/config, LabAPI in AppData/SCP Secret Laboratory (PathManager.cs:48-69).
        bool hosterPolicy = PathManager.AppData.FullName.TrimEnd(Path.DirectorySeparatorChar)
                            != new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName.TrimEnd(Path.DirectorySeparatorChar);
        string appFolder = hosterPolicy ? PathManager.AppData.FullName : PathManager.SecretLab.FullName;
        return Path.Combine(Path.Combine(appFolder, "config"), port);
    }

    private static Dictionary<string, byte[]> ReadLocal()
    {
        Directory.CreateDirectory(LocalDir);
        string root = Path.GetFullPath(LocalDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Directory.GetFiles(LocalDir, "*", SearchOption.AllDirectories)
            .ToDictionary(
                file => Path.GetFullPath(file).Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllBytes,
                StringComparer.Ordinal);
    }

    private static void WriteAtomic(string target, byte[] content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temp = target + ".swappy.tmp";
        File.WriteAllBytes(temp, content);

        if (!File.Exists(target))
        {
            File.Move(temp, target);
            return;
        }

        try
        {
            File.Replace(temp, target, null);
        }
        catch (PlatformNotSupportedException)
        {
            File.Copy(temp, target, overwrite: true);
            File.Delete(temp);
        }
    }

    private static void Backup(string target, string logical, string backupDir)
    {
        if (!File.Exists(target))
            return;

        string destination = Path.Combine(backupDir, logical.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(target, destination, overwrite: true);
    }

    private static void PruneBackups()
    {
        string root = Path.Combine(WorkDir, "backup");
        if (!Directory.Exists(root))
            return;

        foreach (string old in Directory.GetDirectories(root).OrderByDescending(d => d, StringComparer.Ordinal).Skip(KeptBackups))
            Directory.Delete(old, recursive: true);
    }

    private static State LoadState()
    {
        if (!File.Exists(StatePath))
            return new State();

        return YamlConfigParser.Deserializer.Deserialize<State>(File.ReadAllText(StatePath)) ?? new State();
    }

    private static void SaveState(State state)
    {
        WriteAtomic(StatePath, new UTF8Encoding(false).GetBytes(YamlConfigParser.Serializer.Serialize(state)));
    }

    private static void Log(Report report)
    {
        string prefix = $"[{Bundle.SelfName}]";
        foreach (string warning in report.Warnings)
            Logger.Warn($"{prefix} {warning}");
        foreach (string error in report.Errors)
            Logger.Error($"{prefix} {error}");

        if (report.Errors.Count == 0 && report.Sha is not null)
            Logger.Info($"{prefix} Commit {report.Sha} applied (layers [{string.Join(", ", report.Layers)}]), {report.Written.Count} file(s) written{(RestartPending ? ", restart required" : string.Empty)}");
    }
}
