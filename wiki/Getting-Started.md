# Getting Started

## 1. Install

| File | Folder |
|---|---|
| `Swappy.dll` and/or `Swappy.Configs.dll` | `LabAPI/plugins/global/` |
| `Octokit.dll`, `0Harmony.dll` | `LabAPI/dependencies/global/` |

`0Harmony.dll` is only needed by Swappy. Use a port folder instead of
`global` to install on a single server. Restart the server: each plugin
generates its config in `LabAPI/configs/<port>/<plugin name>/`.

## 2. Register a plugin to update

Swappy updates itself out of the box. Plugins whose author made them a
[Managed Plugin](Managed-Plugins) are also picked up with no config. Any
other plugin is declared in Swappy's `config.yml`:

```yml
manual_configurations:
- plugin_name: MyPlugin      # exactly the plugin's Name, case-sensitive
  author: MyGitHubUser       # repository owner
  repository: MyPlugin
  is_private: false
  use_pre_release: false
```

For a private repository, set `is_private: true` and fill `access_token`
with a GitHub token that has read access to it.

## 3. Publish releases Swappy can read

On each GitHub release:

- the **tag** must contain the version: Swappy keeps only its digits and dots, so
  `v1.4.0`, `1.4.0` and `release-1.4.0` all read as `1.4.0`. It must be
  higher than the loaded plugin's `Version`.
- an **asset** must be named after the plugin's **assembly**:
  `MyPlugin.dll`, not the plugin's `Name`.

Drafts are always skipped. Pre-releases are only considered with
`use_pre_release: true`.

## 4. Check it works

From the Remote Admin console:

```text
swappy force MyPlugin
```

The server log shows `Found release ...` then `Successfully downloaded ...`,
or why nothing happened. When something was updated, the server restarts at
the end of the round. If the new version misbehaves, run `swappy rollback MyPlugin`.

To sync configuration files from a repository, see [Config Sync](Config-Sync).
