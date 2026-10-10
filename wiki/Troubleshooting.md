# Troubleshooting

## Swappy

| Symptom | Check |
|---|---|
| A plugin is never checked | `plugin_name` must match the plugin's `Name` exactly, including case. Also check it isn't in `frozen_plugins`. |
| `Update check skipped - last check was too recent` | Normal: `rate_limit_minutes` hasn't elapsed. Use `swappy force` to check now. |
| `No valid release found ... AlreadyUpToDate` | The tag's version isn't higher than the loaded plugin's `Version`. |
| A plugin updates again on every check | The released DLL's `Version` property wasn't bumped to match the tag, so Swappy still sees it as older. |
| `No valid release found ... NoValidReleaseFound` | No release tag contains a readable version, or the only newer one is a pre-release without `use_pre_release`. |
| `Asset 'X.dll' not found` | The asset must be named after the plugin's **assembly**, not its `Name`. |
| `No release found for X` | Wrong `author`/`repository`, or a private repository without `is_private: true` and a valid `access_token`. |
| `GitHub rate limit exceeded` | Unauthenticated calls are limited to 60 per hour per IP. Set `access_token`. |
| The new version breaks the server | `swappy rollback <plugin>`, then report the issue to the plugin author. |
| No Discord message | Check `webhook_url`. A failed send is logged as a warning, never as an error. |

## Swappy.Configs

| Symptom | Check |
|---|---|
| `synchronisation disabled` | `server_id`, `repository_owner` or `repository_name` is empty. |
| `servers.yml is missing` / `server 'x' not found` | `servers.yml` must sit at the repository root, and contain `server_id` as a top-level key. |
| `synchronisation cancelled, no file written` | At least one error below it. Fix them all: nothing is applied until the whole set is valid. |
| `unknown variable {{ ... }}` | Missing `vars.yml` key, `secrets` entry or environment variable. |
| `unknown path` / `extension '...' is not allowed` | See the table in [Config Sync](Config-Sync#where-files-go). |
| `no loaded plugin named 'X'` | The folder name must be the plugin's `Name`. The warning suggests the right case when only the case differs. |
| `unknown key 'x'` | The plugin's config class has no such key: typo, or a renamed setting. |
| `timed out after Ns` | GitHub is slow or unreachable. Raise `timeout_seconds`, or wait for the next start. |
| `modified locally since the last sync` | Someone edited a synced file on the server. It was backed up and overwritten. Move the change to `local/` or into the repository. |
| Another plugin read its old config | A plugin with load priority `0` may load before Swappy.Configs. A warning is logged at startup in that case. |
