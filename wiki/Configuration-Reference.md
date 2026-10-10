# Configuration Reference

## Swappy

`LabAPI/configs/<port>/Swappy/config.yml`

```yml
access_token: null
rate_limit_minutes: 30
check_on_exit_idle: true
webhook_url: null
frozen_plugins: []
manual_configurations: []
```

| Field | Meaning |
|---|---|
| `access_token` | GitHub token, used for repositories marked private. |
| `rate_limit_minutes` | Minimum time between two automatic checks. `swappy force` ignores it. |
| `check_on_exit_idle` | Also check when the server leaves idle mode. If something was updated, the round restarts immediately. |
| `webhook_url` | Discord webhook notified on every update, changelog included. Empty = off. |
| `frozen_plugins` | Plugin names skipped by every check (case-insensitive). Filled by `swappy freeze` and `swappy rollback`. |
| `manual_configurations` | Plugins to update that don't declare a repository themselves. See below. |

### manual_configurations

| Field | Meaning |
|---|---|
| `plugin_name` | The plugin's `Name`, exact and case-sensitive. |
| `author` | GitHub owner of the repository. |
| `repository` | Repository name. |
| `is_private` | Authenticate with `access_token`. |
| `use_pre_release` | Also consider pre-releases. |

A plugin that is a [Managed Plugin](Managed-Plugins) uses its own declared
repository, and an entry here for it is ignored.

## Swappy.Configs

`LabAPI/configs/<port>/Swappy.Configs/config.yml`

```yml
server_id: rp1
repository_owner: MyOrg
repository_name: server-configs
ref: main
access_token: null
timeout_seconds: 10
strict_unknown_keys: false
secrets:
  db_password: hunter2
```

| Field | Meaning |
|---|---|
| `server_id` | This server's key in the repository's `servers.yml`. Empty = sync disabled. |
| `repository_owner`, `repository_name` | The config repository. Both required. |
| `ref` | Branch, tag or commit SHA to apply. |
| `access_token` | Required for a private repository, recommended otherwise (GitHub rate limit). |
| `timeout_seconds` | How long the server start may block while fetching. On timeout, files on disk are kept. |
| `strict_unknown_keys` | An unknown key in a plugin config blocks the sync instead of only warning. |
| `secrets` | Values available as `{{ secrets.<name> }}`. They stay on the server and are never committed. |

Changes to this file apply on the next server start.

## Commands

Swappy's commands live under `swappy` (aliases `swp`, `updater`) in the
Remote Admin console.

| Command | Permission | Effect |
|---|---|---|
| `swappy force [plugin...]` | `swappy.force` | Check now, ignoring the rate limit. Every plugin, or only the ones named. |
| `swappy freeze <plugin>` (alias `pin`) | `swappy.freeze` | Toggle the plugin in `frozen_plugins`. |
| `swappy rollback <plugin>` | `swappy.rollback` | Swap the DLL with its `.bak`, freeze the plugin, restart at the end of the round. |
| `cfgsync` | `ServerConfigs` | Swappy.Configs: applied commit, layers, files written, warnings, errors, pending restart. Also available in the server console. |

A `.bak` only exists once Swappy has updated a plugin. Running `rollback`
twice swaps back to the newer version, but the plugin stays frozen until you
`freeze` it again.
