# Swappy - SCP:SL Plugin Updater

**Swappy** is a lightweight companion plugin that automatically updates the plugins you choose based on a simple `.yml` configuration. It checks for updates from public (or private) GitHub repositories and downloads the latest releases when the server starts.

## 🤝 Trusted By
- [`Access Community`](https://access-community.fr)
*Wanna be part of the list? [Contact me](https://discord.com/users/542790005219655687) and let's collaborate!*  

---

## 🛠️ Features

- 📅 Flexible update cycles: check for updates on server startup or every round
- 🔒 Supports both public and private GitHub repositories
- 📦 Optional dependency downloading
- ♻️ Self-updating enabled by default (can be disabled)
- 🧩 Compatible with LabApi & Exiled frameworks
- 🔍 Commands for manual updates and plugin management
- 📜 Simple YAML configuration for easy setup

---

## 🏓 Installation
- Download the version of the plugin that matches your framework (**LabApi** or **Exiled**).
- Place the `.dll` file in your server's `plugins` folder.
- Put [`Octokit.dll`](https://github.com/LilNesquuik/Swappy/releases/download/1.3.0/dependencies.zip) in your dependencies folder.
- Restart your server to apply the changes.

> [!IMPORTANT]
> You must include **one .dll per framework** in the plugins folder! The **LabApi version** will only update **LabApi plugins**, and the **Exiled version** will only update **Exiled plugins**. If you place a plugin from a different framework in the plugins folder, the plugin will be downloaded into the wrong folder and simply **won’t be loaded**.

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

--- 

## 💻 Commands

- Swappy provides a few commands to manage updates and view plugin information. You can use these commands in the server console or in-game if you have the necessary permissions.

| Command                  | Description                                  |
|--------------------------|----------------------------------------------|
| `swappy`/`swappy_exiled` | Parent command to access Swappy features     |
| `swappy install`         | Manually checks for updates and applies them |
| `swappy plugins`         | Lists all plugins configured for updates     |
| `swappy add`             | Adds a new plugin to the update list         |
| `swappy remove`          | Removes a plugin from the update list        |
