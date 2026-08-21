# Audio Mirror

Spiegelt den Windows-Systemton gleichzeitig auf mehrere Ausgabegeräte – z. B. Lautsprecher **und**
Bluetooth-Kopfhörer **und** USB-Headset zur selben Zeit. Kein Treiber, keine Installation.

## Herunterladen und starten

Die fertige Datei gibt es unter [Releases](../../releases). Herunterladen, Doppelklick, fertig –
eine einzelne, eigenständige Datei. Auf dem Zielrechner muss **kein** .NET installiert sein, sie
lässt sich beliebig verschieben, z. B. auf den Desktop.

| Datei | Für |
|---|---|
| `AudioMirror.exe` | Normale PCs (x64) – in aller Regel die richtige |
| `AudioMirror-arm64.exe` | Windows-on-ARM (Snapdragon-/Surface-Notebooks) |

Beim ersten Start meldet Windows „Der Computer wurde durch Windows geschützt“, weil die Datei nicht
kostenpflichtig signiert ist. Über *Weitere Informationen → Trotzdem ausführen* geht es weiter. Wer
das vermeiden möchte, baut sie sich mit zwei Befehlen selbst (siehe *Selbst bauen*).

## Voraussetzungen

- **Windows 11**, oder Windows 10 ab Build 20348. Die Aufnahme einzelner Anwendungen gibt es erst ab
  dieser Version; der komplette Geräteton läuft auch auf älterem Windows 10.
- Keine Treiberinstallation, keine Administratorrechte.

## Bedienung

**Haken setzen – fertig.** Es gibt keinen Start- oder Stoppknopf:

- Haken bei einem Gerät setzen → die Spiegelung auf dieses Gerät startet **sofort**.
- Haken entfernen → die Spiegelung auf dieses Gerät stoppt **sofort**.
- Die übrigen angehakten Geräte laufen dabei ununterbrochen weiter.

Der Lautstärkeregler pro Gerät wirkt nur auf die Spiegelung und ist unabhängig von der
Windows-Lautstärke. Das Fenster darf jederzeit geschlossen werden – das Programm läuft dann im
Infobereich weiter.

### Was gespiegelt wird: pro Gerät einzeln einstellbar

**Voreingestellt wird der komplette Ton des Quellgeräts gespiegelt** – alles, einschließlich
Windows-Systemklängen. Dafür muss nichts eingestellt werden.

Vor jedem Gerät sitzt ein Pfeil. Aufgeklappt erscheint darunter **eine Zeile je Anwendung, die
gerade Ton ausgibt** – dieselbe Liste, die auch der Windows-Lautstärkemixer für das Quellgerät
zeigt. Jede Zeile hat eine Checkbox und einen eigenen Lautstärkeregler.

Audio Mirror selbst taucht dort nicht auf: seine Audiositzungen *sind* die gespiegelte Ausgabe.
Würde man sie abgreifen, liefe der Ton im Kreis und schaukelte sich auf.

Solange alle Häkchen gesetzt und alle Regler auf 100 % stehen, bleibt es beim kompletten
Geräteton. **Erst wenn für ein Zielgerät eine Anwendung abgehakt oder leiser gestellt wird**,
schaltet die Erfassung für dieses Gerät auf gezielten Abgriff je Anwendung um: dann werden nur
die aktivierten Anwendungen einzeln erfasst und gemischt. Sind wieder alle aktiviert und auf
100 %, geht es automatisch zurück auf den kompletten Geräteton.

Der Unterschied ist in der Statuszeile des Geräts ablesbar: *„läuft – kompletter Ton"* gegenüber
*„läuft – 5 Anwendung(en)"*.

Das hat eine Konsequenz, die man kennen sollte: **im Anwendungsmodus fehlen die
Windows-Systemklänge**, weil sie zu keinem adressierbaren Prozess gehören und sich deshalb nicht
einzeln abgreifen lassen. Das passiert nur, wenn man selbst etwas abwählt – der Normalfall bleibt
vollständig.

Jedes Zielgerät hat seine eigene Mischung: die Küchenlautsprecher dürfen nur Spotify bekommen,
während das Headset weiterhin alles hört.

**Lautstärken multiplizieren sich.** Der Regler am Gerät bleibt die Haupt-Lautstärke und wirkt auf
die fertige Mischung; der Regler an einer Anwendung wirkt nur auf diese. App 50 % × Haupt 50 %
ergibt also 25 % – die Haupt-Lautstärke skaliert damit weiterhin alles gemeinsam.

Sowohl der Haken als auch die Lautstärke werden je Anwendung **und** je Zielgerät dauerhaft
gespeichert, ebenso ob der Bereich aufgeklappt war. Der Schlüssel ist der Name der ausführbaren
Datei, nicht die Prozess-ID – dadurch findet eine neu gestartete Anwendung ihre Einstellung wieder,
und die Spiegelung nimmt sie automatisch mit dem gespeicherten Zustand wieder auf.

Noch zwei Hinweise:

- Eine Anwendung ist danach **doppelt** zu hören: normal über das Quellgerät und zusätzlich auf dem
  Spiegelziel. Das Programm spiegelt, es verlegt nicht. Soll der Ton das Quellgerät *verlassen*,
  ist die native Windows-Funktion *Lautstärkemix* der richtige Weg – beides lässt sich kombinieren.
- Im Anwendungsmodus wird der ganze Prozessbaum erfasst, damit auch Browser und Chat-Programme
  funktionieren, die ihren Ton in einem Hilfsprozess ausgeben.

### Quelle wählen

Oben steht eine Auswahlliste dafür, **von welchem Gerät** gespiegelt wird:

- **Windows-Standardgerät (…)** – die Voreinstellung. Die Quelle folgt dem jeweils aktuellen
  Windows-Standardgerät; in der Klammer steht, welches das gerade ist.
- **Ein konkretes Gerät** – bleibt dauerhaft die Quelle, auch wenn Windows den Standard wechselt.

Die Auswahl wird gespeichert und beim nächsten Start wiederhergestellt. Ist ein fest gewähltes
Quellgerät nicht verfügbar, pausiert die Spiegelung mit einer entsprechenden Meldung und wartet,
bis es zurück ist – es wird bewusst *nicht* still auf ein anderes Gerät ausgewichen. Das Gerät
bleibt so lange in der Liste sichtbar, damit die Auswahl nicht stillschweigend verfällt.

### Das Fenster ist so groß wie nötig

Die Fensterhöhe richtet sich nach dem Inhalt: bei zwei Geräten bleibt es flach, beim Aufklappen
einer Anwendungsliste wächst es mit und schrumpft beim Zuklappen wieder. Nach oben ist es auf gut
die halbe Bildschirmhöhe begrenzt – darüber scrollt die Liste, statt über den Bildschirm zu wachsen.

### Geräteliste: Verbunden und Getrennt

Die Liste ist in zwei Abschnitte mit Überschrift und Trennlinie geteilt:

- **Verbunden** – die gerade angeschlossenen Ausgabegeräte.
- **Getrennt** – nur Geräte, für die tatsächlich schon einmal etwas eingerichtet wurde (angehakt,
  in der Lautstärke verändert oder mit einer Anwendungsmischung versehen) und die gerade nicht
  angeschlossen sind.

Nie benutzte Buchsen tauchen also nicht auf. Ihr Zustand wird trotzdem mitgeführt – er ist nur so
lange unsichtbar, bis wirklich etwas daran eingestellt wurde. Der Abschnitt „Getrennt“ erscheint gar
nicht, solange es nichts darin zu zeigen gibt.

Die **Quellenauswahl** enthält ausschließlich angeschlossene Geräte: von einem nicht verbundenen
ließe sich ohnehin nichts abgreifen. Einzige Ausnahme ist ein fest gewähltes Gerät, das gerade fehlt
– das bleibt sichtbar, damit die Auswahl nicht stillschweigend auf „automatisch“ zurückfällt.

Getrennte Geräte behalten ihre gespeicherte Einstellung sichtbar und lassen sich vorab anhaken;
bespielt werden sie erst, wenn sie wieder da sind. Wird ein Gerät wieder angeschlossen, wandert es
automatisch nach oben in „Verbunden“ und die Spiegelung läuft mit dem gespeicherten Stand an.

Deaktivierte und gar nicht vorhandene Endpunkte bleiben bewusst außen vor – davon führt Windows
dutzende Karteileichen (alte HDMI-Ausgänge, virtuelle Kabel), die die Liste unbrauchbar machen würden.

Vor jedem Gerät steht **dasselbe Symbol, das auch die Windows-Soundeinstellungen zeigen**. Windows
hinterlegt zu jedem Endpunkt einen Symbolverweis (etwa `%windir%\system32\mmres.dll,-3010`), der
direkt geladen wird – in der passenden Größe angefordert, damit er bei hoher Anzeigeskalierung
scharf bleibt. Bringt ein Treiber ein eigenes Symbol mit, erscheint auch dieses.

Getrennte Geräte werden blasser dargestellt. Liefert Windows ausnahmsweise keinen brauchbaren
Verweis, zeichnet das Programm ersatzweise ein passendes Symbol zur Bauform (Kopfhörer,
Lautsprecher, Bildschirm, Digitalanschluss), abgeleitet aus der Eigenschaft *FormFactor*.

### Hotkey: alles auf einmal umschalten

Unten lässt sich eine frei wählbare Tastenkombination aufnehmen: ins Feld klicken, Kombination
drücken, fertig (Rücktaste löscht sie). Das Häkchen daneben schaltet den Hotkey ab, ohne die
Belegung zu verlieren; beides wird gespeichert.

Die Kombination wirkt **systemweit**, also auch aus einem Spiel oder einer Vollbildanwendung heraus:

- **Erstes Drücken** merkt sich den genauen Zustand – welche Geräte aktiv sind und wie die
  Anwendungen darin jeweils eingestellt sind – und schaltet dann alles ab.
- **Zweites Drücken** stellt genau diesen Stand wieder her, nicht einfach „alles an“. Geräte, die
  inzwischen weg sind, werden übersprungen und in der Rückmeldung mitgezählt.

Der gemerkte Zustand wird mitgespeichert und überdauert einen Programmneustart. Lehnt Windows die
Kombination ab, weil ein anderes Programm sie belegt, steht das in der Statuszeile.

### Symbol im Infobereich

Solange das Programm läuft, sitzt ein Symbol rechts unten im Infobereich (Systray).

- **Rechtsklick** öffnet ein Menü mit allen erkannten Ausgabegeräten; ist für ein Gerät eine
  einzelne Anwendung gewählt, steht sie hinter dem Gerätenamen. Die Haken dort sind
  dieselben wie im Hauptfenster und bleiben mit ihm synchron – ein Klick schaltet die Spiegelung
  für dieses Gerät sofort an oder aus, ganz ohne das Fenster zu öffnen. Darunter stehen
  *Fenster öffnen* und *Beenden*.
- **Linksklick oder Doppelklick** holt das Hauptfenster zurück.
- Der Kurztext beim Überfahren zeigt, auf wie viele Geräte gerade gespiegelt wird.

### Start von Hand öffnet das Fenster, Autostart nicht

- **Doppelklick auf die Datei** → das Fenster geht ganz normal auf.
- **Autostart beim Anmelden** → nur das Symbol im Infobereich, kein Fenster.

Dazwischen liegt ein Fall, der wie ein Doppelklick aussieht, aber keiner ist: Windows holt
Programme nach der Anmeldung teils von sich aus wieder hoch (Einstellung *„Apps nach der Anmeldung
neu starten"*), wenn sie beim Herunterfahren liefen – und übergibt dabei kein Argument. Genau
dadurch stand das Fenster nach einem Neustart offen.

Deshalb merkt sich das Programm, wenn **Windows** es beendet (Abmelden oder Herunterfahren). Ist
diese Markierung beim nächsten Start da, war das die Wiederherstellung, und es bleibt im
Infobereich. Die Markierung wird dabei sofort verbraucht, sodass jeder weitere Start wieder ganz
normal ein Fenster öffnet. Zusätzlich hinterlegt das Programm bei Windows die Befehlszeile, die
für so einen Fall gelten soll.

**Es läuft immer nur eine Instanz.** Startet man die Datei erneut, während das Programm schon läuft,
holt der zweite Start das vorhandene Fenster nach vorn und beendet sich selbst. Ohne das ergäben
zwei Instanzen doppelten Ton und zwei Symbole im Infobereich.

### Minimieren schließt nicht

Sowohl **Minimieren** als auch das **X** blenden nur das Fenster aus. Das Programm läuft im
Infobereich weiter, die Spiegelung ebenso – ohne Unterbrechung. Zurück kommt das Fenster über
Doppelklick auf das Symbol oder *Fenster öffnen* im Menü.

Der **Schließen**-Knopf unten rechts tut dasselbe wie das X: ausblenden, nicht beenden.
Vollständig beendet wird ausschließlich über **Beenden** im Tray-Menü. Beim ersten Ausblenden
weist eine kurze Sprechblase einmalig darauf hin, damit das Fenster nicht als „verschwunden“ gilt.

### Merkt sich alles von selbst

Welche Geräte angehakt sind, wird dauerhaft gespeichert – anhand der stabilen Geräte-ID von Windows,
nicht anhand des Anzeigenamens. Daraus folgt:

- **Beim nächsten Programmstart** werden die zuletzt angehakten Geräte automatisch erkannt und
  sofort wieder bespielt, ohne einen einzigen Klick.
- **Wird ein Gerät ab- und wieder angesteckt**, ist sein Haken automatisch wieder gesetzt und die
  Spiegelung dorthin läuft von selbst wieder an.
- **Die Geräteliste aktualisiert sich im Hintergrund**, sobald ein Gerät ein- oder ausgesteckt bzw.
  verbunden oder getrennt wird. Es gibt keinen Aktualisieren-Knopf, weil keiner nötig ist.

### Mit Windows starten

Ein einzelner Schalter, **standardmäßig aus**: trägt das Programm für den aktuellen Benutzer in den
Windows-Autostart ein (`HKCU\...\CurrentVersion\Run`, keine Administratorrechte nötig). Beim
Anmelden startet es direkt in den Infobereich – ganz ohne Fenster – und bespielt die gemerkten
Geräte sofort.

### Wichtig: das Quellgerät ist kein Zielgerät

Das aktuell als Quelle dienende Gerät ist in der Liste ausgegraut und mit *(Quelle)* markiert. Es gibt den Ton
ohnehin schon aus – ganz normal über Windows, ohne Umweg und ohne Zusatzlatenz. Es zusätzlich als
Ziel anzukreuzen wäre nicht nur überflüssig, sondern würde eine Rückkopplung erzeugen: die
Loopback-Aufnahme würde die eigene Ausgabe wieder mit aufnehmen und immer weiter aufschaukeln.
Deshalb ist es bewusst gesperrt.

Wechselt das Windows-Standardgerät, hängt sich die Spiegelung automatisch auf die neue Quelle um.
Der gespeicherte Haken eines Geräts bleibt dabei erhalten, auch solange es selbst die Quelle ist –
wird es später wieder zum normalen Gerät, wird es automatisch wieder bespielt.

*(Die Design-Spezifikation hielt in Abschnitt 4 fest, das Quellgerät könne auch eines der
Wiedergabegeräte sein. Das trifft aus dem genannten Grund nicht zu – das Ergebnis ist dasselbe,
denn die Quelle spielt ja bereits.)*

### Puffer / Latenz

Die Einstellung unten steuert den Kompromiss zwischen Latenz und Stabilität. Gemessen auf diesem
Rechner (USB-DAC als Quelle, HDMI-Monitor als Ziel, 20 s pro Messung):

| Einstellung | Zusatzlatenz (min / Mittel / max) | Aussetzer |
|-------------|-----------------------------------|-----------|
| 50 ms       | 41 / 51 / 62 ms                   | keine     |
| **30 ms**   | **24 / 32 / 45 ms**               | **keine** |
| 20 ms       | 16 / 24 / 40 ms                   | keine     |
| 15 ms       | 15 / 23 / 35 ms                   | keine     |

Die Erklärung dazu erscheint als Kurzinfo, wenn man über *Puffer / Latenz* fährt – sie belegt
keinen dauerhaften Platz mehr. Voreinstellung ist 30 ms und trifft das Ziel der Spezifikation
(20–40 ms). Wer es knapper mag, kann
auf 20 oder 15 ms gehen; bei Knacksern wieder erhöhen. Eine Änderung baut die Audiokette neu auf,
was kurz hörbar ist – die Haken bleiben davon unberührt.

**Bluetooth:** Bluetooth-Kopfhörer haben systembedingt 100–200 ms Eigenlatenz, die kein Programm
umgehen kann. Sie laufen dadurch hörbar hinter den Lautsprechern her. Für Lippensynchronität beim
Spielen oder Filmen ist ein Kabel- oder Funk-Headset die bessere Wahl.

## Für Streamer: was aufgenommen wird

Audio Mirror gibt Ton wie jedes andere Programm aus. Ob eine Aufnahme diese Ausgabe *zusätzlich*
mitschneidet, hängt davon ab, wie das Aufnahmeprogramm den Ton holt – nachgemessen:

- **Gerätebezogene Aufnahme** (OBS „Desktop-Audio“, das ein Ausgabegerät aufnimmt): Die Spiegelung
  landet auf einem *anderen* Gerät und taucht dort nicht auf. Gemessen lag das Quellgerät während
  laufender Spiegelung sogar unter seiner eigenen Nulllinie – **kein Übersprechen**. Das ist der
  übliche Streaming-Aufbau, und dort gibt es nichts zu beachten.
- **Anwendungsbezogene Aufnahme, die „alle Programme“ erfasst** (so arbeitet etwa Discords
  Bildschirmfreigabe mit Desktop-Ton): Dort erscheint Audio Mirror als weiteres Programm und wird
  mit aufgenommen – zusätzlich zum Original. Das ergibt den Ton doppelt, um die Pufferzeit
  versetzt, und bei Sprachprogrammen eine Rückkopplung: die anderen hören sich selbst.

Zwei Regeln, damit das nicht passiert:

1. **Nimm ein Gerät auf, nicht „alle Programme“.** Oder wähle gezielt die Anwendung, um die es geht
   (das Spiel), statt pauschal den Desktop-Ton.
2. **Sprachprogramme von der Spiegelung ausnehmen.** Discord, TeamSpeak & Co. beim Zielgerät
   abhaken – dann kann deren Ton gar nicht erst zurücklaufen.

Sich der Aufnahme zu entziehen ist technisch nicht möglich: Ich habe alle neun Windows-
Stream-Kategorien durchgemessen, jede wird vom Prozess-Loopback mit vollem Pegel erfasst. Was aus
den Lautsprechern kommt, kann ein Aufnahmeprogramm auch mitschneiden.

## Kompatibilität

Das Programm versucht durchgehend, sich an das anzupassen, was es vorfindet, statt ein bestimmtes
Setup vorauszusetzen.

**Audioformate.** Quelle und Ziel dürfen völlig unterschiedliche Formate haben; alles dazwischen
wird umgerechnet:

- *Abtastraten* – 44,1 / 48 / 96 kHz und beliebige Mischungen daraus (getestet).
- *Kanäle* – Mono, Stereo, 5.1 und 7.1, in beide Richtungen. Stereo auf ein 5.1-Gerät landet korrekt
  auf den vorderen Kanälen; ein Mehrkanalsignal auf ein Stereogerät wird nach ITU-R BS.775
  eingefaltet (Center und Surrounds mit −3 dB, LFE weggelassen).
- *Bittiefen* – Windows liefert im Shared Mode praktisch immer 32-Bit-Float, es gibt aber Treiber
  mit ganzzahligen Formaten. 16, 24 und 32 Bit PCM werden ebenfalls bedient.

**Formatverhandlung.** Statt das Mix-Format des Geräts einfach vorauszusetzen, wird eine Reihe
zunehmend konservativer Formate durchprobiert und zuletzt Windows' eigener Gegenvorschlag
übernommen. Treiber, die ihr eigenes Mix-Format ablehnen, führen so nicht zum Ausfall.

**Puffergrößen.** Kleine Puffer sind für die Latenz entscheidend, werden aber nicht von jedem
Treiber akzeptiert. Aufnahme wie Wiedergabe probieren daher schrittweise größere Puffer, bis einer
angenommen wird, statt mit einer COM-Fehlermeldung auszusteigen.

**Automatische Wiederverbindung.** Fällt ein Zielgerät aus – Bluetooth trennt sich, Headset kurz
abgezogen, Gerät vorübergehend exklusiv belegt – wird alle 2 Sekunden erneut verbunden, sobald
Windows es wieder als aktiv meldet. Die übrigen Geräte laufen dabei ununterbrochen weiter.

**Anzeigeskalierung.** Der Fensteraufbau leitet alle Größen aus der Schrifthöhe ab und nutzt keine
festen Pixelpositionen. Lange Texte werden gekürzt statt das Fenster aufzuweiten, und die
Mindestbreite wird aus dem tatsächlichen Platzbedarf berechnet – damit bei 125 %, 150 % und 200 %
Skalierung nichts über den Rand hinausragt.

**Gleichnamige Geräte.** Mehrere identische Adapter (zwei gleiche USB-Headsets, mehrere
HDMI-Ausgänge) bekommen eine laufende Nummer, damit sie unterscheidbar bleiben.

**Anwendungsaufnahme.** Der Ton einzelner Anwendungen wird über die Prozess-Loopback-Aktivierung
von Windows abgegriffen (`ActivateAudioInterfaceAsync`). Die gibt es erst ab Windows 10 Build 20348
bzw. Windows 11 – auf älteren Systemen bleibt der gesamte Systemton nutzbar, die Auswahl einer
einzelnen Anwendung meldet dort einen Fehler in der betroffenen Zeile.

**Architekturen.** x64 und ARM64 als jeweils eigenständige Datei. Windows 11 gibt es nicht mehr als
32-Bit-Variante, deshalb entfällt x86.

## Was das Programm nicht anfasst

Die native Windows-Funktion *Einstellungen → System → Sound → Lautstärkemix* (welche App auf welches
Gerät ausgibt) bleibt unverändert nutzbar und wird nicht verändert.

## Einstellungen

Gespeichert unter `%APPDATA%\AudioMirror\settings.json`. Löschen setzt alles zurück. Der
Windows-Autostart steht separat in der Registry und wird über den Schalter im Programm entfernt.

## Selbst bauen

Benötigt das .NET 8 SDK:

```bash
dotnet publish AudioMirror.csproj -c Release -o dist
```

ARM64-Fassung:

```bash
dotnet publish AudioMirror.csproj -c Release -r win-arm64 --self-contained true -o dist/arm64
```

## Technischer Aufbau

| Datei | Zweck |
|---|---|
| `Audio/MirrorEngine.cs` | Aufnahme am Standardgerät, Zielabgleich, Geräteereignisse, Wiederverbindung |
| `Audio/DeviceOutput.cs` | Ein Zielgerät: Ringpuffer, Lautstärke, Fehlerbehandlung, Neuverbinden |
| `Audio/LowLatencyLoopbackCapture.cs` | Loopback-Aufnahme des Systemtons mit kleinem Puffer |
| `Audio/ProcessLoopbackCapture.cs` | Aufnahme einer einzelnen Anwendung (Prozess-Loopback) |
| `Audio/AudioAppEnumerator.cs` | Anwendungen mit Ton finden, Schlüssel zu Prozess auflösen |
| `Audio/AppMixerSampleProvider.cs` | Mischt mehrere Tonströme zu einem Gerätesignal |
| `Ui/AppMixRow.cs` | Eine Anwendungszeile im aufgeklappten Bereich |
| `Audio/OutputFormatNegotiator.cs` | Sucht ein Format, das das Gerät wirklich akzeptiert |
| `Audio/SampleToTargetProvider.cs` | Ausgabe in Float bzw. 16/24/32 Bit PCM, mit Begrenzung |
| `Audio/AdaptiveResampler.cs` | Drift-Kompensation zwischen den Geräteclocks |
| `Audio/ChannelMapSampleProvider.cs` | Kanalanpassung Mono/Stereo/5.1/7.1 über Mischmatrix |
| `Audio/TimerResolution.cs` | 1-ms-Timerauflösung während der Spiegelung |
| `Autostart.cs` | Windows-Autostart-Eintrag im Benutzerzweig |
| `Ui/TrayController.cs` | Symbol im Infobereich, Kontextmenü, zur Laufzeit gezeichnetes Icon |
| `Ui/MainForm.cs`, `Ui/DeviceRow.cs` | Oberfläche |

Die Zielmenge wird der Engine **deklarativ** übergeben (`SetTargets`): sie gleicht selbst ab, welche
Geräte hinzukommen und welche wegfallen. Unveränderte Geräte behalten dabei ihre laufende
Ausgabeinstanz und werden nicht angetastet – deshalb unterbricht das Umschalten eines Geräts die
anderen nicht.

Vier Punkte, die den Unterschied zwischen „läuft“ und „läuft zuverlässig“ ausmachen:

- **Taktdrift:** Quell- und Zielgerät laufen auf verschiedenen Quarzen. Ohne Ausgleich läuft der
  Puffer über Minuten leer (Aussetzer) oder voll (wachsende Latenz). `AdaptiveResampler` gleicht das
  über eine minimale, unhörbare Anpassung der Abspielrate aus (max. ±1 %).
- **Geregelt wird das Puffer-*Minimum*,** nicht der Mittelwert. Ein im Mittel gut gefüllter Puffer,
  der in den Tälern auf null fällt, knackt trotzdem.
- **Timerauflösung:** Der Aufnahmethread wartet mit `Thread.Sleep`. Bei Windows-Standardauflösung von
  15,6 ms kommen die Samples in unregelmäßigen Schüben – das allein sprengt das Latenzziel.
- **Kein Vorauslesen:** Der Resampler holt pro Aufruf exakt so viele Frames, wie das Gerät gerade
  anfordert. Feste Blockgrößen leeren den Ringpuffer schubweise und lassen die Latenz schwanken.

## Getestet

Auf diesem Rechner (Windows 11, USB-DAC + HDMI-Monitor) geprüft: Geräteerkennung, sofortiges
Starten und Stoppen per Haken, automatisches Wiederaufnehmen der gemerkten Geräte beim
Programmstart, Latenz und Aussetzerfreiheit über je 20 s pro Puffereinstellung, Rechenprobe des
Resamplers über alle Verhältnisse und Blockgrößen, Kanalmatrix für Mono/Stereo/5.1/7.1,
Formatverhandlung, wiederholtes Neuverbinden eines Geräts, schnelles Hin- und Herschalten,
Speichern/Laden der Einstellungen sowie Setzen und Entfernen des Windows-Autostarts.

Das Fensterlayout wurde bei Startgröße und bei erzwungener Mindestgröße geprüft: kein Element ragt
über den Fensterrand hinaus.

Zum Umschalten der Betriebsart geprüft: ohne Abwahl läuft genau eine Aufnahme, nämlich der
komplette Geräteton; nach dem Abwählen einer Anwendung laufen stattdessen sechs Einzelaufnahmen;
nach dem Wiederaktivieren wieder nur der Geräteton – auch nach mehrfachem Hin- und Herschalten und
über einen Programmneustart hinweg (dort im Anwendungsmodus wiederhergestellt).

Zur Anwendungsspiegelung geprüft: Trennschärfe der Aufnahme (der Testton eines Prozesses wird mit
vollem Pegel erfasst, ein stiller Fremdprozess liefert exakt null), Auflisten der Anwendungen mit
Ton, Mischen mehrerer Anwendungen auf ein Gerät, Zu- und Abschalten einzelner Anwendungen ohne
Unterbrechung der übrigen, das Zusammenspiel der Lautstärken (nachgerechnet: 0,5 × 0,5 = 0,25 und
0,25 × 0,5 = 0,125), Verhalten bei einer nicht laufenden Anwendung samt selbsttätigem Nachfassen
sowie Speichern und Wiederherstellen von An/Aus, Lautstärke und Aufklappzustand über einen
Programmneustart hinweg.

Zur Quellenwahl geprüft: Aufbau der Liste mit dem Standardgerät samt Namen als Vorauswahl, Festlegen eines
konkreten Geräts, das dadurch als Ziel gesperrt wird, sowie Speichern und Wiederherstellen der
Auswahl über einen Programmneustart.

Zur Geräteliste geprüft: die Aufteilung in „Verbunden“ und „Getrennt“ mit den richtigen Geräten in
den richtigen Abschnitten, das Auslesen der Bauform (Bildschirm für den HDMI-Monitor, Kopfhörer für
den USB-DAC, Lautsprecher für die Realtek-Ausgänge) sowie das Laden der Windows-Symbole: alle vier
Geräte liefern ihr echtes Symbol aus `mmres.dll` statt der gezeichneten Ersatzdarstellung, und die
ausgegraute Fassung liegt erwartungsgemäß bei rund 40 % Deckkraft.

Zum Hotkey geprüft: Aufnahme und Wiedereinlesen der Kombination, das Umschalten aus dem Hintergrund
heraus (ausdrücklich mit einem anderen Fenster im Vordergrund), das Wiederherstellen des vorherigen
Zustands beim zweiten Drücken sowie das Ablegen des gemerkten Zustands in der Einstellungsdatei.

Zum Startverhalten geprüft: Start von Hand öffnet das Fenster, Autostart bleibt im Infobereich,
ein Start nach einer von Windows ausgelösten Beendigung bleibt ebenfalls zu und verbraucht dabei
die Markierung, der darauffolgende Start von Hand öffnet wieder normal. Außerdem: ein zweiter Start
von Hand holt das vorhandene Fenster, statt eine zweite Instanz zu erzeugen; ein zweiter stiller
Start bleibt stumm.

Zum Infobereich geprüft: Aufbau des Menüs samt Haken und gesperrtem Quellgerät, die
Umschalt-Ereignisse, Minimieren und beide Schließen-Varianten (Klick aufs X wie auch ein von außen
gesendetes Schließen) – dabei bleibt das Fenster ausgeblendet, der Prozess am Leben und die
Spiegelung ununterbrochen –, das Umschalten eines Geräts im ausgeblendeten Zustand sowie der
vollständige Programmabschluss über *Beenden*.

Abtastraten- und Kanalumsetzung wurden gegen das echte Gerät gefahren, indem der Engine synthetische
Aufnahmeformate untergeschoben wurden (44,1 / 48 / 96 kHz, Mono, 5.1, 7.1) – alle ohne Aussetzer.

Nicht praktisch geprüft, weil dafür Hardware nötig ist, die hier fehlt: **mehrere Zielgeräte
gleichzeitig** (dieser Rechner hat neben der Quelle nur ein Ausgabegerät – die Abgleichlogik dafür
ist aber gesondert getestet), die ARM64-Fassung (baut, mangels ARM-Gerät nie gestartet), echte
Bluetooth-Ziele, das Wechseln des Standardgeräts im laufenden Betrieb sowie das physische Abziehen
eines Geräts. Die Pfade sind implementiert und fehlergesichert, aber ungetestet.
