using System;
using System.Linq;

namespace Swappy.API.Extensions;

public static class VersionExtensions
{
    public static Version? ToVersion(this string tagName)
    {
        string versionString = new string(tagName.Where(c => char.IsDigit(c) || c == '.').ToArray());
        
        return Version.TryParse(versionString, out Version? version) ? version : null;
    }
}