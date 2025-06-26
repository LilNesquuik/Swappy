# Swappy - Plugin Updater

**Swappy** is a lightweight companion plugin that automatically updates the plugins you choose based on a simple `.yml` configuration. It checks for updates from public (or private) GitHub repositories and downloads the latest releases when the server starts.

> [!WARNING]
> Only configure **trusted repositories**! This plugin automatically installs the latest available releases. A **malicious repository** could publish an update containing **harmful code**, potentially leading to **security breaches** or **unwanted actions** on your server without your knowledge.

---

## 🛠️ Features

- Auto-updates selected plugins at server startup
- Supports both public and private GitHub repositories
- Optional dependency downloading
- Option to schedule a soft restart after updates
- Self-updating by default (can be disabled)
- LabApi & Exiled Support

> [!IMPORTANT]
> You must include **one .dll per framework** in the plugins folder! The **LabApi version** will only update **LabApi plugins**, and the **Exiled version** will only update **Exiled plugins**. If you place a plugin from a different framework in the plugins folder, it simply **won’t be loaded**.

---

## 📦 Configuration

| Field                  | Description                                                                       |
| ---------------------- | --------------------------------------------------------------------------------- |
| `PluginName`           | Must exactly match the plugin’s `Name` property (e.g., `Plugin.Name`)             |
| `RepositoryOwner`      | GitHub username or organization owning the repo                                   |
| `RepositoryName`       | Name of the GitHub repository                                                     |
| `AccessToken`          | (Optional) GitHub token for private repo access                                   |
| `DownloadDependencies` | Whether to also download `dependencies.zip` or extra files in the release         |
| `UpdateOnStartup`      | If `true`, checks for updates at every server launch                              |
| `ScheduleSoftRestart`  | If `true`, schedules a soft restart at the end of the round                       |

By default, Swappy includes itself in the update list, but you’re free to remove it from the configuration if you prefer not to auto-update it

```yml
plugins:
  - plugin_name: Swappy.LabApi
    repository_owner: LilNesquuik
    repository_name: Swappy
    access_token: null
    download_dependencies: true
    update_on_startup: true
    schedule_soft_restart: true
```
