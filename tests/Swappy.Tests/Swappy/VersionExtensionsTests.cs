using LabTesting;
using Swappy.API.Extensions;

namespace Swappy.Tests.Swappy;

[Collection("Swappy")]
[Trait("plugin", "Swappy")]
public sealed class VersionExtensionsTests
{
    /// <summary>
    /// Parses each tag with ToVersion and checks that only the digits and dots are kept: a 'v' or
    /// 'release-' prefix and a '-beta' suffix are dropped, and the result equals the expected version.
    /// </summary>
    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("release-2.0", "2.0")]
    [InlineData("1.4.0-beta", "1.4.0")]
    public void Tag_reads_as_its_version(string tag, string expected)
    {
        Assert.Equal(expected, tag.ToVersion()?.ToString());
    }

    /// <summary>
    /// Parses tags that hold no usable version (no digits, empty, or a single number that
    /// System.Version rejects) and checks that ToVersion returns null instead of throwing.
    /// </summary>
    [Theory]
    [InlineData("latest")]
    [InlineData("")]
    [InlineData("1")]
    public void Tag_without_a_version_reads_as_null(string tag)
    {
        Assert.True(tag.ToVersion() is null, $"'{tag}' should not parse");
    }
}
