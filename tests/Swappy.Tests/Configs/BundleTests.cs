using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LabTesting;
using Swappy.Configs.Sync;
using YamlDotNet.RepresentationModel;

namespace Swappy.Tests.Configs;

[Collection("Swappy.Configs")]
[Trait("plugin", "Swappy.Configs")]
public sealed class BundleTests
{
    private const string PluginConfig = "labapi/plugins/MyPlugin/config.yml";

    private static readonly Dictionary<string, string> Lookup = new()
    {
        ["server.id"] = "rp1",
        ["secrets.token"] = "s3cr3t",
    };

    /// <summary>
    /// Reads the layers of 'rp1' from the 'Layers' repository's servers.yml, which also declares other
    /// servers, and checks that only rp1's layers are returned, in the declared order.
    /// </summary>
    [Fact]
    public void Layers_are_read_in_order()
    {
        List<string> layers = Bundle.GetLayers(ServersYml("Layers"), "rp1");

        Assert.Equal("base,rp", string.Join(",", layers));
    }

    /// <summary>
    /// Reads 'bare', declared in the 'Layers' repository without a 'layers' key, and checks that an
    /// empty list is returned instead of an error.
    /// </summary>
    [Fact]
    public void Server_without_layers_has_none()
    {
        Assert.Equal(0, Bundle.GetLayers(ServersYml("Layers"), "bare").Count);
    }

    /// <summary>
    /// Asks the 'Layers' repository's servers.yml for a server it does not declare and checks that
    /// GetLayers throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public void Unknown_server_throws()
    {
        Assert.Throws<KeyNotFoundException>(() => Bundle.GetLayers(ServersYml("Layers"), "ghost"));
    }

    /// <summary>
    /// Builds the 'Merge' repository, where the same plugin config comes from four sources: layers
    /// 'base' then 'rp', the server folder, and the local override. Checks that maps are merged deeply
    /// (b.d from base survives b.c from rp), that later sources win (local a=100), that keys only in
    /// one source are kept (e from the server), and that the list from rp replaces the one from base
    /// instead of being appended to it.
    /// </summary>
    [Fact]
    public void Yaml_is_merged_layer_by_layer_then_server_then_local()
    {
        Bundle.Result result = Build(Repository.Load("Merge"));

        AssertNoErrors(result);
        YamlMappingNode config = Map(result, PluginConfig);
        Assert.Equal("100", Scalar(config, "a"));
        Assert.Equal("20", Scalar(config, "b", "c"));
        Assert.Equal("3", Scalar(config, "b", "d"));
        Assert.Equal("5", Scalar(config, "e"));
        Assert.Equal(1, ((YamlSequenceNode)Bundle.Child(config, "list")!).Children.Count);
    }

    /// <summary>
    /// Builds the 'NonYaml' repository, where layers 'base' and 'rp' both provide the same .txt file,
    /// and checks that the output is byte for byte the file of the last layer, with no attempt to
    /// merge it.
    /// </summary>
    [Fact]
    public void Non_yaml_files_are_replaced_whole()
    {
        const string gameplay = "game/config_gameplay.txt";
        Repository repository = Repository.Load("NonYaml");

        Bundle.Result result = Build(repository);

        AssertNoErrors(result);
        Assert.True(result.Files[gameplay].SequenceEqual(repository.Files["layers/rp/" + gameplay]), Bundle.Text(result.Files[gameplay]));
    }

    /// <summary>
    /// Builds the 'Placeholders' repository, whose config uses each kind of placeholder. Checks that
    /// vars.yml from the server folder overrides the layer's (motd), that nested vars are reachable
    /// with a dotted path, that server.id and secrets come from the lookup, and that vars.yml itself is
    /// not part of the output.
    /// </summary>
    [Fact]
    public void Placeholders_are_replaced()
    {
        Bundle.Result result = Build(Repository.Load("Placeholders"));

        AssertNoErrors(result);
        YamlMappingNode config = Map(result, PluginConfig);
        Assert.Equal("server", Scalar(config, "motd"));
        Assert.Equal("deep", Scalar(config, "key"));
        Assert.Equal("rp1", Scalar(config, "id"));
        Assert.Equal("s3cr3t", Scalar(config, "token"));
        Assert.False(result.Files.ContainsKey("vars.yml"), "vars.yml is not written to disk");
    }

    /// <summary>
    /// Builds the 'UnknownPlaceholder' repository, whose config uses a vars placeholder that no
    /// vars.yml defines, and checks that an 'unknown variable' error naming the file is reported.
    /// </summary>
    [Fact]
    public void Unknown_placeholder_is_an_error()
    {
        Bundle.Result result = Build(Repository.Load("UnknownPlaceholder"));

        Assert.True(result.Errors.Any(e => e.Contains(PluginConfig) && e.Contains("unknown variable")), string.Join("\n", result.Errors));
    }

    /// <summary>
    /// Builds the 'InvalidYaml' repository, whose config has an unclosed list, and checks that an
    /// 'invalid YAML' error is reported.
    /// </summary>
    [Fact]
    public void Invalid_yaml_is_an_error()
    {
        Bundle.Result result = Build(Repository.Load("InvalidYaml"));

        Assert.True(result.Errors.Any(e => e.Contains("invalid YAML")), string.Join("\n", result.Errors));
    }

    /// <summary>
    /// Builds from an empty repository and checks that the missing servers.yml is reported as an error
    /// rather than thrown.
    /// </summary>
    [Fact]
    public void Missing_servers_yml_is_an_error()
    {
        Bundle.Result result = Bundle.Build(new Dictionary<string, byte[]>(), "rp1", new Dictionary<string, byte[]>(), _ => null);

        Assert.True(result.Errors.Any(e => e.Contains("servers.yml")), string.Join("\n", result.Errors));
    }

    /// <summary>
    /// Builds the 'MissingLayer' repository, whose servers.yml declares a layer 'ghost' that has no
    /// folder, and checks that an error names it.
    /// </summary>
    [Fact]
    public void Missing_layer_is_an_error()
    {
        Bundle.Result result = Build(Repository.Load("MissingLayer"));

        Assert.True(result.Errors.Any(e => e.Contains("'ghost'")), string.Join("\n", result.Errors));
    }

    /// <summary>
    /// Builds the 'HiddenFile' repository, whose only layer file is named '.draft.yml', and checks
    /// that it is skipped: no error and no file in the output.
    /// </summary>
    [Fact]
    public void Hidden_files_are_ignored()
    {
        Repository repository = Repository.Load("HiddenFile");
        Assert.True(repository.Files.ContainsKey("layers/base/labapi/plugins/MyPlugin/.draft.yml"), "the hidden file should be embedded");

        Bundle.Result result = Build(repository);

        AssertNoErrors(result);
        Assert.Equal(0, result.Files.Count);
    }

    /// <summary>
    /// Maps each kind of accepted logical path and checks that there is no error, that the phase is the
    /// expected one (files LabAPI or the game already read apply on the next boot), and that the target
    /// is an absolute path.
    /// </summary>
    [Theory]
    [InlineData("labapi/plugins/MyPlugin/config.yml", Bundle.Phase.Now)]
    [InlineData("labapi/global/MyPlugin/config.yaml", Bundle.Phase.Now)]
    [InlineData("labapi/permissions.yml", Bundle.Phase.Now)]
    [InlineData("labapi/labapi.yml", Bundle.Phase.NextBoot)]
    [InlineData("game/config_gameplay.txt", Bundle.Phase.NextBoot)]
    public void Accepted_paths_map_inside_their_root(string logical, Bundle.Phase expected)
    {
        Bundle.Roots roots = Roots();

        string? error = Bundle.Map(logical, roots, out string target, out Bundle.Phase phase);

        Assert.True(error is null, error);
        Assert.Equal(expected, phase);
        Assert.True(Path.IsPathRooted(target), target);
    }

    /// <summary>
    /// Maps logical paths that must be refused (unknown root, missing plugin folder, disallowed extension,
    /// non-.txt under game/, path escaping its root, and the files that control the synchronisation
    /// itself) and checks that each returns an error and an empty target.
    /// </summary>
    [Theory]
    [InlineData("other/config.yml")]
    [InlineData("labapi/plugins/config.yml")]
    [InlineData("labapi/plugins/MyPlugin/MyPlugin.dll")]
    [InlineData("game/config_gameplay.yml")]
    [InlineData("labapi/plugins/MyPlugin/../../../escape.yml")]
    [InlineData("labapi/plugins/Swappy.Configs/config.yml")]
    [InlineData("labapi/plugins/Swappy/properties.yml")]
    public void Rejected_paths_are_reported(string logical)
    {
        string? error = Bundle.Map(logical, Roots(), out string target, out _);

        Assert.NotNull(error, $"{logical} should be rejected");
        Assert.Equal(string.Empty, target);
    }

    /// <summary>
    /// Merges the two maps of the 'MergeInputs' repository, which share the key a.b, and checks that
    /// the base node still holds its original value, so that merging never mutates a source shared
    /// with another file.
    /// </summary>
    [Fact]
    public void Merge_leaves_its_inputs_untouched()
    {
        Repository inputs = Repository.Load("MergeInputs");
        YamlNode baseNode = Bundle.Parse(inputs.Text("base.yml"))!;
        YamlNode overrideNode = Bundle.Parse(inputs.Text("override.yml"))!;

        Bundle.Merge(baseNode, overrideNode);

        Assert.Equal("1", Scalar((YamlMappingNode)baseNode, "a", "b"));
    }

    private static Bundle.Result Build(Repository repository)
    {
        return Bundle.Build(repository.Files, "rp1", repository.Local, key => Lookup.TryGetValue(key, out string value) ? value : null);
    }

    private static string ServersYml(string repository)
    {
        return Repository.Load(repository).Text(Bundle.ServersFile);
    }

    private static Bundle.Roots Roots()
    {
        string root = Path.Combine(Path.GetTempPath(), "swappy-tests");
        return new Bundle.Roots
        {
            PluginConfigs = Path.Combine(root, "configs", "7790"),
            GlobalConfigs = Path.Combine(root, "configs", "global"),
            Permissions = Path.Combine(root, "configs", "permissions.yml"),
            LabApiYml = Path.Combine(root, "LabApi-7790.yml"),
            Game = Path.Combine(root, "config", "7790"),
        };
    }

    private static void AssertNoErrors(Bundle.Result result)
    {
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors));
    }

    private static YamlMappingNode Map(Bundle.Result result, string logical)
    {
        Assert.True(result.Files.ContainsKey(logical), $"{logical} should be built");
        return (YamlMappingNode)Bundle.Parse(Bundle.Text(result.Files[logical]))!;
    }

    private static string? Scalar(YamlMappingNode map, params string[] path)
    {
        YamlNode? node = map;
        foreach (string key in path)
            node = node is YamlMappingNode mapping ? Bundle.Child(mapping, key) : null;
        return (node as YamlScalarNode)?.Value;
    }
}
