# Managed Plugins

Plugin authors can declare their repository in code, so server owners have
nothing to configure. Reference the `Swappy` NuGet package and inherit from
`ManagedPlugin` instead of `Plugin`:

```xml
<PackageReference Include="Swappy" Version="2.1.0" />
```

```csharp
using Swappy.API.Features;

public sealed class MyPlugin : ManagedPlugin<Config>
{
    public override DependencyResource Repository { get; } = new GithubResource
    {
        Author = "MyGitHubUser",
        Repository = "MyPlugin",
        IsPrivate = false,
        UsePreRelease = false,
    };

    // Name, Version, Enable, Disable... as usual
}
```

Use `ManagedPlugin` when the plugin has no config. A plugin that already has
its own base class can implement `ISwappyConfigurable` instead.

Your plugin now needs Swappy at runtime: `Swappy.dll` must be installed on
the server.

Releases must follow the same rules as any other plugin: the version in
the tag, and an asset named after your assembly. See
[Getting Started](Getting-Started#3-publish-releases-swappy-can-read).

## Other sources

`GithubResource` is one implementation of `DependencyResource`. To update
from somewhere else, implement it yourself:

```csharp
public abstract Task<bool> Resolve(Version current, string fileName, string outputPath);
```

`fileName` is the assembly name, without `.dll`. `outputPath` is the
plugin's current DLL path. Write the new DLL there and return `true` when you
did. Swappy only counts the result: scheduling the restart is up to you.
