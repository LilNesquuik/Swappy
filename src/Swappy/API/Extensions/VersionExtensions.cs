using System;
using System.Linq;

namespace Swappy.API.Extensions;

/// <summary>
/// Extension methods for reading versions out of release tags.
/// </summary>
public static class VersionExtensions
{
    /// <summary>
    /// Parses a release tag into a version, keeping only its digits and dots, so that
    /// <c>v1.2.0</c> and <c>release-1.2.0</c> both read as <c>1.2.0</c>.
    /// </summary>
    /// <param name="tagName">The release tag.</param>
    /// <returns>The parsed version, or null when what remains is not a valid version.</returns>
    public static Version? ToVersion(this string tagName)
    {
        string versionString = new string(tagName.Where(c => char.IsDigit(c) || c == '.').ToArray());
        
        return Version.TryParse(versionString, out Version? version) ? version : null;
    }
}