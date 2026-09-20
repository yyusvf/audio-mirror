# Sprachsystem

Wie Audio Mirror dreizehn Sprachen hält — zum Übernehmen in TagTuner.

## Der Kern in einem Satz

Eine einzige Datei, eine Hilfsmethode mit **benannten Argumenten** je Sprache, und jeder Text
steht mit allen Übersetzungen auf einem Fleck. Kein Ressourcensystem, keine `.resx`, keine
Satellitenassemblys, kein Build-Schritt.

Der ganze Grund für den Aufbau: bei dreizehn Sprachen ist eine Reihe gleichaussehender
Zeichenketten nicht mehr zu lesen. Benannte Argumente machen jede Zeile selbsterklärend, und
eine vergessene Sprache fällt beim Lesen sofort auf, statt still zu verrutschen.

## Das Gerüst

```csharp
internal static class Strings
{
    public static readonly string[] Supported =
        ["en", "de", "fr", "es", "it", "pt", "nl", "pl", "ru", "uk", "tr", "cs", "sv"];

    // Eigenbezeichnungen für die Auswahlliste - jede Sprache nennt sich selbst.
    // Gleiche Reihenfolge wie Supported, der Index ist die Verbindung.
    public static readonly string[] SupportedNames =
        ["English", "Deutsch", "Français", "Español", "Italiano", "Português (Brasil)",
         "Nederlands", "Polski", "Русский", "Українська", "Türkçe", "Čeština", "Svenska"];

    private static string? forced;

    public static string Language => forced ?? Detect();

    /// Ein bekanntes Kürzel erzwingt die Sprache, alles andere folgt Windows.
    /// Muss vor dem ersten Textzugriff aufgerufen werden.
    public static void Configure(string? language)
    {
        string? code = language?.Trim().ToLowerInvariant();
        forced = code != null && Array.IndexOf(Supported, code) >= 0 ? code : null;
    }

    private static string Detect()
    {
        try
        {
            string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return Array.IndexOf(Supported, code) >= 0 ? code : "en";
        }
        catch
        {
            return "en";
        }
    }

    private static string T(
        string en,
        string? de = null, string? fr = null, string? es = null, string? it = null,
        string? pt = null, string? nl = null, string? pl = null, string? ru = null,
        string? uk = null, string? tr = null, string? cs = null, string? sv = null) =>
        Language switch
        {
            "de" => de ?? en,
            "fr" => fr ?? en,
            // ... je Kürzel eine Zeile ...
            _ => en,
        };
}
```

Zwei Entscheidungen darin, die zählen:

- **`CurrentUICulture`, nicht `CurrentCulture`.** Maßgeblich ist die Anzeigesprache, nicht das
  Regionsformat. Wer deutsches Datumsformat auf englischem Windows fährt, erwartet Englisch.
- **`?? en` in jedem Zweig.** Eine fehlende Übersetzung fällt auf Englisch zurück, statt leer
  zu bleiben. Damit lässt sich eine neue Sprache nach und nach füllen, ohne dass etwas bricht.

## So sieht ein Text aus

```csharp
public static string Source => T("Source",
    de: "Quelle", fr: "Source", es: "Origen", it: "Sorgente", pt: "Origem", nl: "Bron",
    pl: "Źródło", ru: "Источник", uk: "Джерело", tr: "Kaynak", cs: "Zdroj", sv: "Källa");
```

Eigenschaft, kein Feld — `=>` statt `=`. Wichtig: ein Feld würde beim ersten Zugriff einmal
ausgewertet und bliebe dann auf dieser Sprache stehen.

Produktnamen bleiben unübersetzt:

```csharp
public static string AppTitle => "Audio Mirror";
```

## Texte mit Werten

Methode statt Eigenschaft, interpolierte Zeichenketten in jeder Sprache:

```csharp
public static string HotkeyAssignedTo(string combination, string owner) => T(
    $"\"{combination}\" is already assigned to {owner}.",
    de: $"„{combination}“ ist bereits für {owner} vergeben.",
    fr: $"« {combination} » est déjà attribué à {owner}.",
    ru: $"«{combination}» уже назначено для {owner}.");
```

Das trägt die **Anführungszeichen der jeweiligen Sprache** mit — deutsche unten/oben,
französische Guillemets mit geschütztem Leerzeichen, niederländische einfache. Mit einem
Platzhalter-Schema in einer Ressourcendatei wäre das nicht zu machen.

Zahlenformate stehen ebenfalls je Sprache drin (`{ms:0}`, `мс` statt `ms`).

## Anbindung

1. **Vor dem ersten Text** die Sprache setzen, ganz am Anfang von `Main`:
   ```csharp
   Strings.Configure(AppSettings.Load().Language);
   ```
2. **Auswahlliste**: `SupportedNames` anzeigen, der gewählte Index zeigt auf `Supported`.
   In der Einstellungsdatei steht das Kürzel, nicht der Index — sonst verschiebt sich alles,
   sobald eine Sprache dazukommt. Leer bedeutet „Windows folgen".
3. **Nach dem Wechsel Neustart**: die Texte sind bereits gesetzt. Audio Mirror zeigt dafür
   einen Hinweis an. Wer das vermeiden will, braucht Bindungen, die sich neu auswerten lassen.

## Regeln, die sich bewährt haben

- **Eine Datei, nach Bereichen sortiert**, mit Kommentarblöcken als Trenner. Bei ~100 Texten
  und 1150 Zeilen ist das noch übersichtlich.
- **Englisch ist das Original**, alles andere hängt daran. Der erste Parameter ist
  verpflichtend.
- **Nie zusammensetzen.** Kein `Text1 + " " + Text2` — die Wortstellung ist nicht in allen
  Sprachen dieselbe. Lieber ein Satz mit Platzhaltern.
- **Kein Text im Markup.** Beschriftungen kommen über Bindungen aus dem Ansichtsmodell, das
  sie aus `Strings` holt. Sonst ist die Sprache an einer Stelle fest verdrahtet.

## Grenzen

- **Keine echten Pluralformen.** Audio Mirror weicht aus: `"{count} Gerät(e)"` oder im
  Polnischen/Russischen `"Wyciszono urządzenia: {count}"` — eine Form, die für jede Zahl
  stimmt. Slawische Sprachen haben drei Pluralformen; wer die braucht, kommt um
  `PluralRules` oder ICU nicht herum.
- **Alle dreizehn Fassungen werden gebaut.** Bei einem Text mit Werten baut C# alle
  interpolierten Zeichenketten, bevor `T` eine auswählt — zwölf davon landen im Müll. Für
  Oberflächentexte ohne Belang, in einer Schleife wäre es keine gute Idee.
- **Keine Rechts-nach-links-Sprachen.** Arabisch oder Hebräisch bräuchten zusätzlich
  `FlowDirection` am Fenster.
