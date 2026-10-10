# Swappy

Keep an SCP:SL server's LabAPI plugins and configuration up to date from
GitHub, without touching the server by hand. Two plugins, both on **net48**:

- **Swappy** downloads new releases of your plugins and restarts the
  server at the end of the round.
- **Swappy.Configs** syncs config files from a Git repository before any
  other plugin reads its own.

Both are optional and work independently. Drop `Swappy.dll` into
`LabAPI/plugins/global/` and `Octokit.dll` + `0Harmony.dll` into
`LabAPI/dependencies/global/`, then restart. Swappy updates itself
out of the box. Other plugins only need a few lines in its config:

```yml
manual_configurations:
- plugin_name: MyPlugin
  author: MyGitHubUser
  repository: MyPlugin
  is_private: false
  use_pre_release: false
```

**Full documentation: [the wiki](https://github.com/LilNesquuik/Swappy/wiki).**

- [Getting Started](https://github.com/LilNesquuik/Swappy/wiki/Getting-Started)
- [Configuration Reference](https://github.com/LilNesquuik/Swappy/wiki/Configuration-Reference)
- [Config Sync](https://github.com/LilNesquuik/Swappy/wiki/Config-Sync)
- [Managed Plugins](https://github.com/LilNesquuik/Swappy/wiki/Managed-Plugins)
- [Troubleshooting](https://github.com/LilNesquuik/Swappy/wiki/Troubleshooting)

> [!WARNING]
> Only point Swappy at **repositories you trust**. It installs whatever the
> latest release contains, and that code runs on your server.

Trusted by [Access Community](https://access-community.fr).

## Contributing to Swappy itself

The projects compile against the dedicated server's assemblies. Without
`SL_REFERENCES`, the build falls back to the default Steam install:

```powershell
$env:SL_REFERENCES = 'C:/path/to/server/SCPSL_Data/Managed'
dotnet build Swappy.slnx -c Release
```

Tests run inside a real dedicated server with
[LabTesting](https://github.com/LilNesquuik/LabTesting). The server installed
through Steam is found on its own:

```powershell
dotnet build tests/Swappy.Tests -c Release -t:LabTesting
```

## License

MIT.
