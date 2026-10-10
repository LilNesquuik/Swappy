using System.Collections.Generic;
using System.Threading.Tasks;
using LabTesting;
using PlayerRoles;
using SwappyPlugin = Swappy.Swappy;

namespace Swappy.Tests.Swappy;

using Commands = LabTesting.Commands;

[Collection("Swappy")]
[Trait("plugin", "Swappy")]
public sealed class FreezeCommandTests : IAsyncLifetime
{
    private const string Ghost = "Swappy.Tests.Ghost";

    private List<string> _previous = [];

    private static List<string> Frozen => SwappyPlugin.Singleton.Config!.FrozenPlugins;

    public Task InitializeAsync()
    {
        _previous = [..Frozen];
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Frozen.Clear();
        Frozen.AddRange(_previous);
        SwappyPlugin.Singleton.SaveConfig();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks that Fixtures/swappy.yml was deployed and loaded: Swappy must be in frozen_plugins,
    /// otherwise the test server could download a release over the build under test.
    /// </summary>
    [Fact]
    public void Fixture_config_freezes_Swappy()
    {
        Assert.True(Frozen.Contains("Swappy"), "Fixtures/swappy.yml should have been deployed");
    }

    /// <summary>
    /// Runs 'swappy freeze' twice on a plugin that is not loaded. The first run must succeed, add the
    /// name to frozen_plugins and warn that no loaded plugin has that name; the second run must remove it
    /// again. Also checks that no event handler threw in the meantime.
    /// </summary>
    [Fact]
    public async Task Freeze_toggles_the_plugin()
    {
        CommandResult frozen = Commands.Run("swappy", "freeze", Ghost);
        Assert.True(frozen.Success, frozen.Response);
        Assert.True(Frozen.Contains(Ghost), "first run should freeze");
        Assert.True(frozen.Response.Contains("no loaded plugin"), "an unknown name should be warned about");

        CommandResult unfrozen = Commands.Run("swappy", "freeze", Ghost);
        Assert.True(unfrozen.Success, unfrozen.Response);
        Assert.False(Frozen.Contains(Ghost), "second run should unfreeze");

        await Swallowed.AssertNone();
    }

    /// <summary>
    /// Freezes a name, then runs 'swappy freeze' again with the same name in upper case, and checks
    /// that the existing entry is matched case-insensitively and removed rather than added a second time.
    /// </summary>
    [Fact]
    public void Unfreeze_ignores_case()
    {
        Commands.Run("swappy", "freeze", Ghost);

        CommandResult result = Commands.Run("swappy", "freeze", Ghost.ToUpperInvariant());

        Assert.True(result.Success, result.Response);
        Assert.False(Frozen.Contains(Ghost), "the upper-case name should match the frozen entry");
    }

    /// <summary>
    /// Runs 'swappy freeze' with no argument and checks that it fails with the usage message.
    /// </summary>
    [Fact]
    public void Freeze_without_a_name_shows_usage()
    {
        CommandResult result = Commands.Run("swappy", "freeze");

        Assert.False(result.Success, result.Response);
        Assert.True(result.Response.StartsWith("Usage:"), result.Response);
    }

    /// <summary>
    /// Spawns a dummy player, who has no permission, runs 'swappy freeze' as that player, and checks
    /// that the command is refused and frozen_plugins is left unchanged.
    /// </summary>
    [Fact]
    public async Task Freeze_requires_permission()
    {
        TestPlayer player = await World.Spawn(RoleTypeId.ClassD, "NoPerms");

        CommandResult result = Commands.RunAs(player, "swappy", "freeze", Ghost);

        Assert.False(result.Success, result.Response);
        Assert.False(Frozen.Contains(Ghost), "a refused command must not change the config");
    }
}
