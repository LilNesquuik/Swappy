# Swappy

Keep an SCP:SL server's LabAPI plugins and configuration in sync with GitHub:
publish a release or push a commit, and the server picks it up on its own.

## Pages

| Page | For |
|---|---|
| [Getting Started](Getting-Started) | Install, register a first plugin, check that it works |
| [Configuration Reference](Configuration-Reference) | Every config field, commands and permissions |
| [Config Sync](Config-Sync) | Repository layout, layers, variables and how files are applied |
| [Managed Plugins](Managed-Plugins) | Plugin authors: declare your own repository in code |
| [Troubleshooting](Troubleshooting) | Symptom → what to check |

## The two plugins

- **Swappy**: checks GitHub releases when the server waits for players,
  at the end of each round and when it leaves idle mode. When a newer
  version is found, it replaces the DLL, keeps the old one as `.bak` and
  restarts at the end of the round.
- **Swappy.Configs**: loads before every other plugin, fetches a config
  repository, merges the layers this server uses and writes the result.
  It validates everything before writing anything to disk.

They don't depend on each other. Install just the one you need.
