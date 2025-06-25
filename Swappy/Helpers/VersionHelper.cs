using System;
using System.Text.RegularExpressions;

namespace Swappy.Helpers;

public static class VersionHelper
{
    // Thank you CHATGPT im dumb asf in Regex
    private static readonly Regex VersionRegex = new(
        @"(?:version|v)?\s*(\d+)\.(\d+)\.(\d+)(?:[-]?([0-9A-Za-z\-]+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public static bool TryParse(string input, out Version version)
    {
        var match = VersionRegex.Match(input);
        if (!match.Success)
        {
            version = null!;
            return false;
        }

        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int patch = int.Parse(match.Groups[3].Value);

        version = new Version(major, minor, patch);
        return true;
    }
}