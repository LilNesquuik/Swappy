# Swappy - Plugin Updater

**Swappy** is a lightweight companion plugin that automatically updates the plugins you choose based on a simple `.yml` configuration. It checks for updates from public (or private) GitHub repositories and downloads the latest releases when the server starts.

---

## 🛠️ Features

- Auto-updates selected plugins at server startup
- Supports both public and private GitHub repositories
- Optional dependency downloading
- Option to schedule a soft restart after updates
- Self-updating by default (can be disabled)

---

## 📦 Configuration

By default, Swappy includes itself in the update list

| Field                  | Description                                                    |
| ---------------------- | -------------------------------------------------------------- |
| `PluginName`           | Name of the plugin (used for logging and matching)             |
| `RepositoryOwner`      | GitHub username or organization owning the repo                |
| `RepositoryName`       | Name of the GitHub repository                                  |
| `AccessToken`          | (Optional) GitHub token for private repo access                |
| `DownloadDependencies` | Whether to also download `.deps` or extra files in the release |
| `UpdateOnStartup`      | If `true`, checks for updates at every server launch           |
| `ScheduleSoftRestart`  | If `true`, schedules a soft restart at the end of the round    |


```yml
# List of plugins to auto-update
plugins:
  - pluginName: Swappy
    repositoryOwner: LilNesquuik
    repositoryName: Swappy
    accessToken: null             # Optional: GitHub token for private repos
    downloadDependencies: true    # Download additional files if present in the release
    updateOnStartup: true         # Check for updates on server startup
    scheduleSoftRestart: true     # Schedule a soft restart after update if needed
```
