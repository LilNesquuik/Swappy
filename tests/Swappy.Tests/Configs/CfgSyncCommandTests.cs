using System.Threading.Tasks;
using LabTesting;
using PlayerRoles;
using Swappy.Configs;

namespace Swappy.Tests.Configs;

using Commands = LabTesting.Commands;

/// <summary>
/// The test server leaves <c>server_id</c> empty, so the synchronisation is disabled and never
/// reaches GitHub.
/// </summary>
[Collection("Swappy.Configs")]
[Trait("plugin", "Swappy.Configs")]
public sealed class CfgSyncCommandTests
{
    /// <summary>
    /// Checks that Swappy.Configs loaded and set its singleton before the tests run.
    /// </summary>
    [Fact]
    public void Plugin_is_enabled()
    {
        Assert.NotNull(SwappyConfigs.Singleton, "Swappy.Configs should load before the tests run");
    }

    /// <summary>
    /// Runs 'cfgsync' as the server and checks the report of a server with no server_id: no applied
    /// commit, the 'synchronisation disabled' warning, and no pending restart. Also checks that no event
    /// handler threw.
    /// </summary>
    [Fact]
    public async Task Cfgsync_reports_a_disabled_synchronisation()
    {
        CommandResult result = Commands.Run("cfgsync");

        Assert.True(result.Success, result.Response);
        Assert.True(result.Response.Contains("Applied commit: none"), result.Response);
        Assert.True(result.Response.Contains("synchronisation disabled"), result.Response);
        Assert.True(result.Response.Contains("Restart pending: no"), result.Response);

        await Swallowed.AssertNone();
    }

    /// <summary>
    /// Checks that 'cfgsync' is registered in the server console, not only in Remote Admin.
    /// </summary>
    [Fact]
    public void Cfgsync_is_available_in_the_server_console()
    {
        Assert.True(Commands.TryResolve("cfgsync", Where.GameConsole, out _), "cfgsync should be registered in the server console");
    }

    /// <summary>
    /// Spawns a dummy player, who lacks the ServerConfigs permission, runs 'cfgsync' as that player,
    /// and checks that the command is refused.
    /// </summary>
    [Fact]
    public async Task Cfgsync_requires_permission()
    {
        TestPlayer player = await World.Spawn(RoleTypeId.ClassD, "NoPerms");

        CommandResult result = Commands.RunAs(player, "cfgsync");

        Assert.False(result.Success, result.Response);
    }
}
