using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AudioMirror;

/// <summary>Wie mit neuen Fassungen verfahren wird.</summary>
internal enum UpdateMode
{
    /// <summary>Herunterladen und das Setup starten.</summary>
    Automatic = 0,

    /// <summary>Nur Bescheid geben.</summary>
    Notify = 1,

    /// <summary>Gar nicht nachsehen - es geht dann keinerlei Anfrage nach außen.</summary>
    Never = 2,
}

internal sealed record UpdateInfo(string Version, string PageUrl, string? SetupUrl);

/// <summary>
/// Sieht bei GitHub nach, ob es eine neuere Fassung gibt.
///
/// Bewusst schlicht: eine Anfrage an die Releases-Schnittstelle, Vergleich der Versionsnummer,
/// fertig. Es werden keinerlei Daten über den Rechner mitgeschickt, und bei
/// <see cref="UpdateMode.Never"/> unterbleibt die Anfrage vollständig.
/// </summary>
internal static class UpdateChecker
{
    private const string LatestUrl = "https://api.github.com/repos/yyusvf/audio-mirror/releases?per_page=10";

    /// <summary>Höchstens einmal am Tag nachsehen - öfter bringt nichts.</summary>
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromDays(1);

    public static Version CurrentVersion { get; } = ParseVersion(
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0");

    /// <summary>Ob nach der eingestellten Häufigkeit erneut nachgesehen werden soll.</summary>
    public static bool ShouldCheck(UpdateMode mode, DateTime lastCheckUtc) =>
        mode != UpdateMode.Never && DateTime.UtcNow - lastCheckUtc >= MinimumInterval;

    /// <summary>Sucht die neueste Fassung. Liefert <c>null</c>, wenn es nichts Neueres gibt.</summary>
    public static async Task<UpdateInfo?> FindNewerAsync(bool includeBeta, CancellationToken token = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AudioMirror", CurrentVersion.ToString()));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            string json = await client.GetStringAsync(LatestUrl, token).ConfigureAwait(false);
            using JsonDocument document = JsonDocument.Parse(json);

            UpdateInfo? best = null;
            Version bestVersion = CurrentVersion;

            foreach (JsonElement release in document.RootElement.EnumerateArray())
            {
                if (release.TryGetProperty("draft", out JsonElement draft) && draft.GetBoolean())
                {
                    continue;
                }
                bool prerelease = release.TryGetProperty("prerelease", out JsonElement pre) && pre.GetBoolean();
                if (prerelease && !includeBeta)
                {
                    continue;
                }

                string tag = release.TryGetProperty("tag_name", out JsonElement t) ? t.GetString() ?? "" : "";
                Version version = ParseVersion(tag);
                if (version <= bestVersion)
                {
                    continue;
                }

                string page = release.TryGetProperty("html_url", out JsonElement h) ? h.GetString() ?? "" : "";
                string? setup = null;
                if (release.TryGetProperty("assets", out JsonElement assets))
                {
                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string name = asset.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "";
                        if (name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                        {
                            setup = asset.TryGetProperty("browser_download_url", out JsonElement u) ? u.GetString() : null;
                            break;
                        }
                    }
                }

                bestVersion = version;
                best = new UpdateInfo(version.ToString(), page, setup);
            }

            return best;
        }
        catch
        {
            // Kein Netz, GitHub nicht erreichbar, unerwartete Antwort - kein Grund für eine
            // Fehlermeldung. Beim nächsten Start wird erneut nachgesehen.
            return null;
        }
    }

    /// <summary>Lädt das Setup herunter und startet es. Gibt bei Misserfolg false zurück.</summary>
    public static async Task<bool> DownloadAndRunAsync(UpdateInfo update, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(update.SetupUrl))
        {
            return false;
        }

        try
        {
            string target = Path.Combine(Path.GetTempPath(), $"AudioMirror-Setup-{update.Version}.exe");

            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AudioMirror", CurrentVersion.ToString()));
                await using Stream source = await client.GetStreamAsync(update.SetupUrl, token).ConfigureAwait(false);
                await using var file = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(file, token).ConfigureAwait(false);
            }

            // Sichtbar starten, nicht still: eine Installation soll niemanden überraschen.
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void OpenPage(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Kein Browser verfügbar - unkritisch.
        }
    }

    /// <summary>Wandelt "v1.2.3" oder "1.2" in eine vergleichbare Version.</summary>
    private static Version ParseVersion(string text)
    {
        string cleaned = new(text.Where(c => char.IsDigit(c) || c == '.').ToArray());
        cleaned = cleaned.Trim('.');
        return Version.TryParse(cleaned.Contains('.') ? cleaned : cleaned + ".0", out Version? version)
            ? version
            : new Version(0, 0);
    }
}
