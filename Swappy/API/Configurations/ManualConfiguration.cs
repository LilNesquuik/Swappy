namespace Swappy.API.Configurations;

public class ManualConfiguration
{
    public string PluginName { get; init; }
    public string Author { get; init; }
    public string Repository { get; init; }
    public bool IsPrivate { get; init; }
    public bool UsePreRelease { get; init; }
}