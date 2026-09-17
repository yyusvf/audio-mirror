# Audio Mirror: modernes UI

Auftrag an Claude Code. Ziel ist eine Oberfläche im Stil von TagTuner:
dunkel, abgerundet, eigene Titelleiste, Akzentfarbe, ordentliche Abstände.

## Ausgangslage

.NET 8, WinForms, NAudio. Eine einzelne self-contained exe. Tray-Symbol,
globaler Hotkey, 13 Sprachen, Updater und Inno Setup sind vorhanden und
funktionieren.

Unangetastet bleibt alles unter `Audio/`. Der Mixer, die Resampler und die
Geräteerkennung haben nichts mit der Oberfläche zu tun und sind der Teil, der
schwer zu ersetzen wäre. Ebenso bleiben `Strings.cs`, `UpdateChecker.cs`,
`AppSettings.cs`, `Autostart.cs`, `SingleInstance.cs`.

Neu gebaut wird nur `Ui/`.

## Welche Technik

**Empfehlung: WPF.** Volle Kontrolle über das Aussehen, Mica und Dunkelmodus
über `DwmSetWindowAttribute`, Tray über `H.NotifyIcon.Wpf`, und
`PublishSingleFile` funktioniert weiterhin. Die exe bleibt damit klein und die
Auslieferung wie bisher.

**WinUI 3 nicht nehmen.** TagTuner ist damit gebaut und sieht gut aus, aber
für Audio Mirror passt es nicht:

- Kein `PublishSingleFile`. Self-contained sind es rund 220 MB statt weniger
  als 100. Für ein Tray-Werkzeug ist das unverhältnismäßig.
- Kein Tray-Symbol. Man braucht Win32-Aufrufe oder eine Fremdbibliothek.
- Der globale Hotkey braucht ebenfalls ein Fensterhandle über Umwege.

**Wenn wenig Risiko wichtiger ist als das Ergebnis:** WinForms behalten und
nur neu einfärben. Dunkle Farben, `DwmSetWindowAttribute` für dunkle
Titelleiste und abgerundete Ecken, eigene Zeichnung für Geräte- und
App-Zeilen. Das sind ein paar Tage statt einer Umschreibung, sieht aber
erkennbar nach WinForms aus.

## Was zu bauen ist

1. **Farben und Maße an einer Stelle.** Ein ResourceDictionary mit Akzent,
   Hintergründen, Textfarben, Eckenradien, Schriftgrößen. Keine Farbe direkt
   im Markup.
2. **Eigene Titelleiste.** Logo, Titel, rechts die Fenstertasten. Alles darin,
   was anklickbar sein soll, muss ein echtes Bedienelement sein. In TagTuner
   ließen sich Tabs nicht wechseln, weil sie `Border` mit `Tapped` waren und
   damit als Ziehfläche des Fensters galten.
3. **Gerätezeilen** als Vorlage statt zusammengesetzter Steuerelemente: Symbol,
   Name, Status, Lautstärke. Höhe um 56 px, abgerundete Auswahl, Hover.
4. **App-Mixer** in derselben Zeilenform.
5. **Einstellungen** als eigene Seite im Fenster, nicht als getrenntes
   Fenster. Zwei Spalten, solange Platz ist.
6. **Tray-Menü** im selben dunklen Stil.

## Worauf zu achten ist

- Die 13 Sprachen bleiben. `Strings.cs` hat englische Schlüssel und benannte
  Argumente, das Muster passt unverändert weiter.
- Fenstergröße und Position merken, wie jetzt.
- Hoher DPI: `PerMonitorV2` ist gesetzt und muss gesetzt bleiben.
- Nicht gegen das Framework arbeiten. Wenn eine Liste beim Klick von selbst
  etwas tut, ist das Hinzufügen einer zweiten Reaktion fast immer der Fehler.
- Jede Änderung einzeln bauen und starten. Bei UI-Arbeit sagt nur das laufende
  Fenster die Wahrheit.

## Reihenfolge

Farben und Titelleiste zuerst, dann die Gerätezeilen, dann Mixer und
Einstellungen, Tray zuletzt. Nach jedem Schritt startfähig bleiben.
