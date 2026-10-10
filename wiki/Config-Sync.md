# Config Sync

Swappy.Configs loads before every other plugin. It fetches one commit of a
GitHub repository, builds this server's files from it and writes them. Then
LabAPI loads the other plugins and they read their config as usual.

## Repository layout

```text
servers.yml
layers/
  base/
    vars.yml
    labapi/plugins/MyPlugin/config.yml
  rp/
    labapi/plugins/MyPlugin/config.yml
servers/
  rp1/
    vars.yml
    labapi/permissions.yml
```

`servers.yml` lists each server and the layers it uses, in order:

```yml
rp1:
  layers: [base, rp]
event:
  layers: [base]
```

A server's files are built from, in order:

1. each layer listed in `layers`, under `layers/<name>/`
2. `servers/<server_id>/`
3. the local override folder, `LabAPI/swappy-configs/<port>/local/` on the
   server itself, never committed

Swappy.Configs only downloads `servers.yml`, the layers this server uses and its own
`servers/<id>/` folder. Files and folders starting with `.` are ignored.

## Merging

When several sources contain the same path:

- **YAML files** are merged deeply: maps are combined key by key, lists and
  scalars are replaced by the later source.
- **Other files** are replaced whole by the last source.

So a layer only needs the keys it changes:

```yml
# layers/rp/labapi/plugins/MyPlugin/config.yml
max_players: 40
```

## Variables

Any file can use `{{ ... }}` placeholders:

| Placeholder | Value |
|---|---|
| `{{ vars.a.b }}` | Key `a.b` of the merged `vars.yml` files, same order as above |
| `{{ secrets.name }}` | `secrets` in Swappy.Configs' own config |
| `{{ env.NAME }}` | Environment variable of the server process |
| `{{ server.id }}`, `{{ server.port }}`, `{{ server.ref }}`, `{{ server.sha }}` | This server, and the commit being applied |

An unknown placeholder is an error, so nothing is written.

## Where files go

| Repository path | Written to | Applies |
|---|---|---|
| `labapi/plugins/<Plugin>/<file>` | `LabAPI/configs/<port>/<Plugin>/<file>` | this boot |
| `labapi/global/<Plugin>/<file>` | `LabAPI/configs/global/<Plugin>/<file>` | this boot |
| `labapi/permissions.yml` | `LabAPI/configs/permissions.yml` | this boot |
| `labapi/labapi.yml` | `LabAPI/LabApi-<port>.yml` | next boot |
| `game/<file>.txt` | the game's config folder for this port | next boot |

Only `.yml`, `.yaml` and `.txt` files are accepted. Any other path is an error.
Swappy.Configs never writes `labapi/plugins/Swappy.Configs/` or
`labapi/plugins/Swappy/properties.yml`, so a bad push cannot turn the sync off.

"Next boot" files were already read when Swappy.Configs ran. If one of them
changes, the server restarts: right away if nobody is connected, otherwise at
the end of the round.

## Safety

- **All or nothing.** Every file is mapped and validated first. On any
  error, nothing is written and the files already on disk stay in use.
- **Plugin configs are checked** against the plugin's config class. A value
  of the wrong type is an error. An unknown key is a warning, or an error
  with `strict_unknown_keys`. A folder that matches no loaded plugin is a
  warning.
- **Fetch failure or timeout** keeps the files on disk.
- **Backups.** Every file that is overwritten is copied first to
  `LabAPI/swappy-configs/<port>/backup/<timestamp>/`. The last 5 syncs are kept.
- **Local edits** to a synced file are detected. Swappy.Configs logs them,
  backs them up, then overwrites them. To keep a local change, put it in
  the `local/` folder.
- **Nothing is deleted.** A file removed from the repository stays on
  disk and is simply no longer synced.

Run `cfgsync` in the console to see the result of the last sync.
