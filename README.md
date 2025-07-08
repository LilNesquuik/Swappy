# Swappy - SCP:SL Plugin Updater

**Swappy** is a lightweight companion plugin that automatically updates the plugins you choose based on a simple `.yml` configuration. It checks for updates from public (or private) GitHub repositories and downloads the latest releases when the server starts.

---

## 🏓 Installation
- Download the version of the plugin that matches your framework (**LabApi** or **Exiled**).
- Place the `.dll` file in your server's `plugins` folder.
- Put `Octokit.dll` in your dependencies folder.
- Restart your server to apply the changes.

> [!IMPORTANT]
> You must include **one .dll per framework** in the plugins folder! The **LabApi version** will only update **LabApi plugins**, and the **Exiled version** will only update **Exiled plugins**. If you place a plugin from a different framework in the plugins folder, the plugin will be downloaded into the wrong folder and simply **won’t be loaded**.

---

## 🛠️ Features

- 🚀 Automatically updates selected plugins at server startup
- 🔒 Supports both public and private GitHub repositories
- 📦 Optional dependency downloading
- ⏰ Configurable update frequency: each round or on server startup
- ♻️ Self-updating enabled by default (can be disabled)
- 🧩 Compatible with LabApi & Exiled frameworks

---

## 📦 Configuration

- By default, Swappy includes itself in the update list, but you’re free to remove it from the configuration if you prefer not to auto-update it

| Field                  | Description                                                                          |
|------------------------|--------------------------------------------------------------------------------------|
| `PluginName`           | Must exactly match the plugin’s `Name` property (e.g., `Plugin.Name`)                |
| `RepositoryOwner`      | GitHub username or organization owning the repo                                      |
| `RepositoryName`       | Name of the GitHub repository                                                        |
| `AccessToken`          | (Optional) GitHub token for private repo access                                      |
| `DownloadDependencies` | Whether to also download `dependencies.zip` or extra files in the release            |
| `Cycle`                | Update check frequency: `EachRound` (every round) or `OnStartup` (on server startup) |
| `ScheduleSoftRestart`  | If `true`, schedules a soft restart at the end of the round                          |

```yml
plugins:
  - plugin_name: Swappy.LabApi
    repository_owner: LilNesquuik
    repository_name: Swappy
    access_token: null
    download_dependencies: true
    cycle: OnStartup
    schedule_soft_restart: true
```

> [!WARNING]
> Only configure **trusted repositories**! This plugin automatically installs the latest available releases. A **malicious repository** could publish an update containing **harmful code**, potentially leading to **security breaches** or **unwanted actions** on your server without your knowledge.
