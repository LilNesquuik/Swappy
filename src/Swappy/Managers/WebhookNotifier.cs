using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LabApi.Features.Console;

namespace Swappy.Managers;

/// <summary>
/// Sends Discord webhook notifications when a plugin is updated.
/// </summary>
public static class WebhookNotifier
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    /// <summary>
    /// Notifies <see cref="Config.WebhookUrl"/> that a plugin was updated. Does nothing when no
    /// webhook is configured, and only logs a warning when the request fails.
    /// </summary>
    /// <param name="fileName">The plugin's assembly name.</param>
    /// <param name="oldVersion">The version that was replaced.</param>
    /// <param name="newTag">The tag of the release installed.</param>
    /// <param name="changelog">The release notes, truncated to fit in a Discord message.</param>
    public static async Task SendUpdate(string fileName, Version oldVersion, string newTag, string? changelog)
    {
        string? url = Swappy.Singleton.Config!.WebhookUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            string message = $"**{fileName}** updated: `{oldVersion}` → `{newTag}` (restart scheduled)";

            if (!string.IsNullOrWhiteSpace(changelog))
            {
                // ponytail: Discord caps content at 2000 chars, keep room for the header line
                const int maxChangelogLength = 1500;
                string body = changelog!.Length > maxChangelogLength
                    ? changelog.Substring(0, maxChangelogLength) + "…"
                    : changelog;

                message += $"\n{body}";
            }

            string json = "{\"content\":\"" + Escape(message) + "\"}";

            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await Client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                Logger.Warn($"Webhook notification failed with status {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Webhook notification failed: {ex.Message}");
        }
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", string.Empty)
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
