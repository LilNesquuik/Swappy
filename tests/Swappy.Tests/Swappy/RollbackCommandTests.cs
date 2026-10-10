using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LabTesting;
using SwappyPlugin = Swappy.Swappy;

namespace Swappy.Tests.Swappy;

using Commands = LabTesting.Commands;

/// <summary>
/// Rolls back Swappy.Configs, the smallest plugin under test, with a hand-made backup.
/// </summary>
[Collection("Swappy")]
[Trait("plugin", "Swappy")]
public sealed class RollbackCommandTests : IAsyncLifetime
{
    private Plugin _target = null!;
    private byte[] _original = [];
    private List<string> _previousFrozen = [];
    private ServerStatic.NextRoundAction _previousAction;

    private string BackupPath => _target.FilePath + ".bak";

    private static List<string> Frozen => SwappyPlugin.Singleton.Config!.FrozenPlugins;

    public Task InitializeAsync()
    {
        _target = PluginLoader.Plugins.Keys.First(p => p.Name == "Swappy.Configs");
        _original = File.ReadAllBytes(_target.FilePath);
        _previousFrozen = [..Frozen];
        _previousAction = ServerStatic.StopNextRound;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        File.WriteAllBytes(_target.FilePath, _original);
        File.Delete(BackupPath);

        Frozen.Clear();
        Frozen.AddRange(_previousFrozen);
        SwappyPlugin.Singleton.SaveConfig();

        ServerStatic.StopNextRound = _previousAction;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes a .bak next to Swappy.Configs.dll whose content differs by one byte, runs 'swappy rollback',
    /// and checks that the two files were swapped byte for byte, that the plugin was added to
    /// frozen_plugins, and that a restart was scheduled for the end of the round.
    /// </summary>
    [Fact]
    public void Rollback_swaps_the_dll_with_its_backup()
    {
        byte[] backup = [.._original, 0x42];
        File.WriteAllBytes(BackupPath, backup);

        CommandResult result = Commands.Run("swappy", "rollback", _target.Name);

        Assert.True(result.Success, result.Response);
        Assert.True(File.ReadAllBytes(_target.FilePath).SequenceEqual(backup), "the backup should now be the plugin");
        Assert.True(File.ReadAllBytes(BackupPath).SequenceEqual(_original), "the old plugin should now be the backup");
        Assert.True(Frozen.Contains(_target.Name), "a rolled back plugin should be frozen");
        Assert.Equal(ServerStatic.NextRoundAction.Restart, ServerStatic.StopNextRound);
    }

    /// <summary>
    /// Makes sure no .bak exists, runs 'swappy rollback', and checks that it fails with 'No backup found'
    /// without touching the DLL or freezing the plugin.
    /// </summary>
    [Fact]
    public void Rollback_without_backup_changes_nothing()
    {
        File.Delete(BackupPath);

        CommandResult result = Commands.Run("swappy", "rollback", _target.Name);

        Assert.False(result.Success, result.Response);
        Assert.True(result.Response.StartsWith("No backup found"), result.Response);
        Assert.True(File.ReadAllBytes(_target.FilePath).SequenceEqual(_original), "the plugin must be untouched");
        Assert.False(Frozen.Contains(_target.Name), "nothing should be frozen");
    }

    /// <summary>
    /// Runs 'swappy rollback' on a name no loaded plugin has, and checks that it fails with
    /// 'No loaded plugin'.
    /// </summary>
    [Fact]
    public void Rollback_of_an_unknown_plugin_fails()
    {
        CommandResult result = Commands.Run("swappy", "rollback", "Swappy.Tests.Ghost");

        Assert.False(result.Success, result.Response);
        Assert.True(result.Response.StartsWith("No loaded plugin"), result.Response);
    }

    /// <summary>
    /// Writes a backup, runs 'swappy rollback' with the plugin name in upper case, and checks that the
    /// plugin is found anyway and frozen under its real name, not the one typed.
    /// </summary>
    [Fact]
    public void Rollback_matches_the_name_case_insensitively()
    {
        File.WriteAllBytes(BackupPath, _original);

        CommandResult result = Commands.Run("swappy", "rollback", _target.Name.ToUpperInvariant());

        Assert.True(result.Success, result.Response);
        Assert.True(Frozen.Contains(_target.Name, StringComparer.Ordinal), "the plugin's real name should be frozen");
    }
}
