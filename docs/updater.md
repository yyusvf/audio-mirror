# Selbstaktualisierung — die Logik

Wie eine Desktop-Anwendung sich selbst aktualisiert: was sie prüft, wann sie fragt, was sie
speichert und woran es üblicherweise scheitert. Bewusst ohne Code und ohne Festlegung auf eine
Programmiersprache — die Beschreibung soll ausreichen, um das Ganze in einer beliebigen Umgebung
nachzubauen.

Der Kern in drei Sätzen: Die Anwendung fragt beim Start eine öffentliche Liste ihrer
Veröffentlichungen ab und vergleicht die Versionsnummer mit ihrer eigenen. Ist dort etwas Neueres,
lädt sie die Installationsdatei und startet sie — je nach Einstellung nach Rückfrage sichtbar oder
ohne jedes Zutun im Hintergrund. Anschließend beendet sie sich, lässt sich austauschen und startet
wieder.

Kein Server, kein Hintergrunddienst, keine Bibliothek.

---

## 1. Die vier Zuständigkeiten

Trenne diese vier Dinge sauber. Die Trennung ist nicht Formsache, sie entscheidet darüber, ob der
Ablauf später noch zu verstehen ist:

**Die Prüfstelle** spricht mit dem Netz. Sie holt die Liste der Veröffentlichungen, sucht die
neueste brauchbare heraus, lädt auf Verlangen die Installationsdatei herunter und startet sie. Sie
kennt keine Fenster, keine Dialoge, keine Einstellungen — sie bekommt gesagt, was sie tun soll.

**Die Einstellungsseite** zeigt die Wahlmöglichkeiten und den Knopf „Jetzt suchen", schreibt die
Einstellung weg und zeigt eine Statuszeile. Findet sie etwas, meldet sie das weiter und ist fertig.
Sie entscheidet nichts.

**Das Hauptfenster** entscheidet. Nur hier laufen die Fäden zusammen, denn nur das Hauptfenster
kann eine Frage stellen *und* die Anwendung beenden. Ob gefragt oder stillschweigend installiert
wird, steht hier.

**Der gespeicherte Zustand** sind vier Werte in der ganz normalen Einstellungsdatei. Kein eigener
Ablageort, keine eigene Datei.

---

## 2. Der gespeicherte Zustand

Das ist das Rückgrat. Vier Werte, mehr braucht es nicht:

| Wert | Inhalt | Wozu |
|---|---|---|
| **Verhalten** | automatisch / fragen / nie | Die Wahl des Nutzers |
| **Zeitpunkt der letzten Prüfung** | Datum und Uhrzeit in Weltzeit | Verhindert, dass bei jedem Start eine Anfrage rausgeht |
| **Abgelehnte Fassung** | Versionsnummer oder leer | Damit dieselbe Frage nicht ewig wiederkommt |
| **Zuletzt gelaufene Fassung** | Versionsnummer oder leer | Erkennt, dass gerade aktualisiert wurde |

Zwei Dinge unbedingt beachten:

- **Der Zeitpunkt gehört in Weltzeit**, nicht in Ortszeit. Sonst rechnet sich das Zeitfenster bei
  einer Zeitumstellung oder auf Reisen falsch.
- **Die abgelehnte Fassung wird als Versionsnummer gespeichert, nicht als Datum.** Wer eine Fassung
  ablehnt, will die nächste trotzdem angeboten bekommen — nicht erst nach Ablauf einer Frist.

---

## 3. Die drei Verhaltensweisen

**Automatisch installieren.** Wird etwas gefunden, geschieht alles ohne Rückfrage: herunterladen,
still installieren, Anwendung neu starten. Kein Dialog, kein Assistent, kein Abschlussbildschirm.

**Nur benachrichtigen** (sinnvolle Voreinstellung). Wird etwas gefunden, kommt eine Frage mit Ja und
Nein. Bei Ja läuft die Installation sichtbar ab — wer selbst zugestimmt hat, soll auch sehen, was
passiert.

**Nie nachsehen.** Und zwar wirklich nie: es geht **überhaupt keine Anfrage** nach außen. Nicht
„prüfen, aber nichts sagen". Das ist die einzige Einstellung, bei der jemand guten Gewissens
erwarten darf, dass die Anwendung stumm bleibt, und der einzige Weg, dieses Versprechen zu halten,
ist, gar nicht erst zu fragen. Die Prüfung muss also abbrechen, **bevor** irgendeine
Netzwerkverbindung entsteht.

---

## 4. Was beim Start passiert

In dieser Reihenfolge:

1. **Wurde zwischendurch aktualisiert?** Die laufende Versionsnummer mit der zuletzt gemerkten
   vergleichen. Sind sie verschieden und war die gemerkte nicht leer, einmal kurz Bescheid geben:
   „Auf Fassung X aktualisiert." Dann die neue Nummer merken und die abgelehnte Fassung vergessen —
   die ist mit dem Wechsel erledigt.

   Die Prüfung auf „war nicht leer" unterdrückt den Hinweis bei der allerersten Installation, wo er
   Unsinn wäre.

2. **Darf überhaupt geprüft werden?** Nur wenn das Verhalten nicht auf „nie" steht *und* die letzte
   Prüfung lange genug her ist. Sonst hier aufhören.

3. **Nachsehen**, im Hintergrund, ohne die Anwendung aufzuhalten. Die Anwendung muss vom ersten
   Moment an bedienbar sein; die Prüfung darf nichts blockieren.

4. **Gibt es nichts Neueres**, ist die Sache still erledigt. Keine Meldung. Nur wer selbst gesucht
   hat, bekommt eine Antwort — „ist aktuell".

5. **Gibt es etwas Neueres**, entscheidet die Regelkette aus Abschnitt 6.

---

## 5. Wie geprüft wird

**Die Quelle** ist die öffentliche Liste der Veröffentlichungen deines Projekts — bei GitHub die
Releases-Schnittstelle. Frag bewusst die *Liste* ab, nicht den Eintrag „neueste": dann kannst du
selbst entscheiden, was als brauchbar gilt, statt dich darauf zu verlassen, was der Anbieter dafür
hält. Zehn Einträge reichen.

**Übergehen musst du** Entwürfe und Vorabfassungen. Sonst bekommen normale Nutzer Testfassungen
angeboten.

**Die Versionsnummer** wird zahlenweise verglichen, niemals als Text. Als Text wäre „1.10" kleiner
als „1.9" — ein Fehler, der erst nach zehn Veröffentlichungen auffällt und dann jeden betrifft.
Zerlege die Nummer in ihre Zahlen und vergleiche Stelle für Stelle.

Aus dem Namen der Veröffentlichung müssen dabei die üblichen Beigaben verschwinden: ein
vorangestelltes „v", ein angehängtes „-beta". Am robustesten fährst du, wenn du alles außer Ziffern
und Punkten wegwirfst.

**Deine eigene Versionsnummer** liest du aus der laufenden Programmdatei, nicht aus einer Konstante
im Quelltext. Sonst vergisst du sie eines Tages zu erhöhen und die Anwendung meldet ewig „aktuell".

**Die richtige Datei** in der Veröffentlichung erkennst du an einem **Bestandteil des Namens**, zum
Beispiel dem Wort „Setup" — nicht an einem vollständigen, festen Dateinamen. So kannst du später
eine Versionsnummer in den Dateinamen aufnehmen, ohne dass die Erkennung bricht.

> **Der Namensbestandteil ist eine Schnittstelle.** Bereits ausgelieferte Fassungen suchen nach dem
> Wort, das *sie* kennen. Fällt es aus dem Namen, findet jede ältere Fassung nichts mehr und kann
> sich nie wieder selbst aktualisieren. Einmal festlegen, nie ändern.

**Fehler werden verschluckt.** Kein Netz, Anbieter nicht erreichbar, unerwartete Antwort: das ist
kein Grund für eine Fehlermeldung. Eine Aktualisierungsprüfung ist eine Nebensache und darf
niemanden aufhalten, weil gerade das WLAN weg ist. Beim nächsten Start wird eben erneut
nachgesehen. Der einzige Fehler, der gemeldet werden darf, ist einer *nach* ausdrücklicher
Zustimmung zur Installation — dort wartet jemand auf ein Ergebnis.

**Zwei Kleinigkeiten, die sonst 403 zurückgeben:** Viele Schnittstellen verlangen eine
Programmkennung im Anfragekopf; ohne sie wirst du abgewiesen. Und ohne Anmeldung gilt meist eine
Obergrenze an Anfragen pro Stunde und IP-Adresse — bei einer Prüfung pro Tag völlig unkritisch,
aber der eigentliche Grund, warum es überhaupt ein Zeitfenster gibt.

---

## 6. Wann gefragt wird — die Regelkette

Wurde etwas gefunden, wird der Reihe nach entschieden. Die erste zutreffende Regel gewinnt:

**Erstens: Steht das Verhalten auf „automatisch", wird ohne Rückfrage installiert.** Genau das sagt
die Einstellung zu. Wer sie gewählt hat, will nicht gefragt werden.

**Zweitens: Wurde genau diese Fassung schon einmal abgelehnt, geschieht nichts.** Sonst steht bei
jedem Start dieselbe Frage. Ausnahme: Wer gerade selbst auf „Jetzt suchen" gedrückt hat, bekommt
sie trotzdem angeboten — er hat schließlich ausdrücklich gefragt.

Dafür muss die Information, ob von Hand gesucht wurde, bis hierher durchgereicht werden. Das ist
der einzige Grund, warum die Prüfung überhaupt wissen muss, wer sie angestoßen hat.

**Drittens: Ist das Fenster gerade ausgeblendet, kommt kein Dialog.** Läuft die Anwendung still im
Infobereich — etwa weil sie beim Anmelden automatisch gestartet wurde —, wäre ein Dialog, der
unaufgefordert aus dem Nichts aufspringt, zudringlich. Stattdessen ein kurzer Hinweis am Symbol,
und der Fund wird gemerkt. Sobald jemand das Fenster öffnet, kommt die Frage dann doch.

**Viertens: sonst wird gefragt.** Ein schlichter Ja/Nein-Dialog, der drei Dinge nennt: welche
Fassung verfügbar ist, welche installiert ist, und dass sich die Anwendung für die Installation
beendet, die Einstellungen aber erhalten bleiben.

Bei „Nein" wird die Fassung als abgelehnt gemerkt. Bei „Ja" beginnt die Installation.

---

## 7. Das Zeitfenster

Zwischen zwei Prüfungen im Hintergrund liegt ein Mindestabstand — ein Tag ist ein guter Wert.

**Entscheidend ist, wer den Zeitpunkt fortschreibt: ausschließlich die Prüfung im Hintergrund,
niemals die Suche auf Knopfdruck.**

Ohne diese Unterscheidung passiert Folgendes: Jemand drückt einmal auf „Jetzt suchen". Damit ist
das Tagesfenster verbraucht. Beim nächsten Programmstart wird nicht geprüft, beim übernächsten
auch nicht, und wer die Anwendung mehrmals täglich benutzt, erlebt nie eine automatische Meldung.
Von außen sieht es so aus, als funktioniere die Prüfung nur, wenn man sie von Hand anstößt.

Genau dieser Fehler steckte in der Vorlage zu diesem Dokument und blieb lange unbemerkt, weil beide
Wege für sich genommen funktionierten.

---

## 8. Die Installation

**Schritt für Schritt:**

1. Gibt es zu dieser Veröffentlichung gar keine Installationsdatei, bleibt nur, die
   Veröffentlichungsseite im Browser zu öffnen. Das ist eine Rückfalllösung, kein normaler Weg.
2. Die Datei in den Zwischenablageordner des Systems herunterladen. **Nimm die Versionsnummer in
   den Dateinamen auf**, damit zwei Versuche sich nicht in die Quere kommen.
3. Währenddessen eine Statuszeile führen: „Fassung X wird geladen". Bei größeren Dateien sieht es
   sonst aus, als hinge die Anwendung.
4. Scheitert der Download, ebenfalls die Veröffentlichungsseite öffnen — hier wartet jemand auf ein
   Ergebnis, also darf es eine Meldung geben.
5. Das Installationsprogramm starten.
6. Die Anwendung beendet sich.

**Sichtbar oder still** ist der einzige Unterschied zwischen den beiden Wegen. Nach ausdrücklicher
Zustimmung läuft die Installation ganz normal mit Oberfläche ab. Bei „automatisch" läuft sie ohne
jede Anzeige durch, und danach startet die Anwendung von selbst wieder.

**Der Neustart muss den vorherigen Zustand wiederherstellen.** Lief die Anwendung still im
Infobereich, soll sie auch wieder still starten und nicht plötzlich ein Fenster aufreißen. Merke dir
also beim Start des Installationsprogramms, ob das Fenster sichtbar war, und gib diese Information
weiter.

### Der Wettlauf, an dem es scheitert

Das ist der unangenehmste Teil und der Grund, warum stille Aktualisierungen oft wortlos versagen:

> **Ein Installationsprogramm prüft gleich beim Start, ob die Anwendung noch läuft**, und weigert
> sich sonst weiterzumachen. Im normalen Betrieb erscheint dann „Bitte schließen Sie zuerst …".
> Läuft es aber ohne Oberfläche, wird eine solche Rückfrage üblicherweise automatisch mit
> *Abbrechen* beantwortet — **die Installation bricht ab, ohne dass irgendwer etwas merkt.**

Die Anwendung zu starten und sich sofort danach zu beenden reicht nicht sicher: es ist ein
Wettlauf, und beim stillen Durchlauf ist das Installationsprogramm schneller, weil es keine
Oberfläche aufbauen muss.

**Die Lösung:** Starte nicht das Installationsprogramm direkt, sondern einen kleinen
Zwischenprozess, der ein paar Sekunden wartet und *dann* das Installationsprogramm aufruft. Dieser
Zwischenprozess lebt weiter, während die Anwendung verschwindet. Zwei Sekunden reichen bequem.

Achte darauf, dass der Zwischenprozess kein sichtbares Fenster aufmacht und wirklich unabhängig
weiterläuft, wenn die Anwendung endet — beides ist je nach Umgebung eine eigene Einstellung.

---

## 9. Was das Installationsprogramm können muss

Drei Dinge, unabhängig davon, welches du verwendest:

**Es muss ohne Oberfläche laufen können** und dabei auch keine Rückfragen stellen. Meist gibt es
dafür Schalter wie „völlig still", „Meldungen unterdrücken" und „nicht neu starten".

**Es muss die Anwendung nach der Installation wieder starten können — ohne Zutun.** Der übliche
Weg, ein Häkchen „Anwendung jetzt starten" auf der Abschlussseite, hilft nicht: bei stiller
Installation gibt es keine Abschlussseite. Du brauchst also einen zweiten, unabhängigen Startbefehl,
der nur dann greift, wenn die Anwendung selbst ihn angefordert hat. Praktisch löst man das über
einen selbst erfundenen Schalter in der Befehlszeile, den das Installationsprogramm abfragt.

**Es muss beim Aktualisieren nicht alles neu fragen.** Zielordner, Startmenüeintrag und
Zusatzoptionen stehen bereits fest; sie erneut vorzulegen kostet nur Klicks. Überspringe diese
Seiten, wenn die Anwendung schon installiert ist — aber nur, wenn dein Installationsprogramm die
früheren Antworten auch wirklich übernimmt, sonst fallen sie auf die Voreinstellungen zurück.

**Zwei Fallen dabei:**

Das Erkennen „ist bereits installiert" darf **nur einmal, ganz am Anfang** ausgewertet und dann
gemerkt werden. Viele Installationsprogramme legen ihren eigenen Deinstallations-Eintrag *während*
des Vorgangs an — wer später erneut nachsieht, hält auch eine Erstinstallation für eine
Aktualisierung.

Alles, was zwingend passieren muss — Vorbedingungen prüfen, Laufzeitumgebungen nachinstallieren —
gehört an eine Stelle, die **auch im stillen Betrieb** durchlaufen wird. Hängst du es an eine
Assistentenseite, läuft es bei einer stillen Installation nie.

### Einstellungen, die der Anwendung gehören

Wenn deine Anwendung eine Einstellung sowohl im Installationsprogramm anbietet als auch selbst
verwaltet — der Klassiker ist „mit Windows starten" —, dann **darf das Installationsprogramm sie nur
bei der Erstinstallation setzen.**

Sonst passiert das hier: Beim ersten Mal wird das Häkchen gesetzt. Später schaltet der Nutzer die
Einstellung in der Anwendung ab. Die nächste Aktualisierung erinnert sich an das Häkchen von damals,
schreibt den Eintrag neu — und der abgeschaltete Autostart ist stillschweigend wieder da. Nach der
Erstinstallation gehört die Einstellung der Anwendung, dem Installationsprogramm nicht mehr.

---

## 10. Fallstricke auf einen Blick

| Fallstrick | Was passiert | Gegenmittel |
|---|---|---|
| Manuelle Suche schreibt denselben Zeitpunkt fort | Automatische Prüfung feuert nie | Nur Hintergrundprüfungen stempeln |
| Installationsprogramm bricht ab, weil die Anwendung noch läuft | Stille Aktualisierung scheitert wortlos | Zwischenprozess mit einigen Sekunden Vorlauf |
| Vorbedingungen an einer Assistentenseite | Werden bei stiller Installation übersprungen | An eine Stelle hängen, die immer läuft |
| „Ist installiert?" mehrfach ausgewertet | Erstinstallation gilt als Aktualisierung | Einmal auswerten, Ergebnis merken |
| Installationsprogramm setzt gemeinsame Einstellungen neu | Abgeschaltetes kommt zurück | Nur bei Erstinstallation setzen |
| Versionen als Text verglichen | „1.10" gilt als kleiner als „1.9" | Zahlenweise vergleichen |
| Eigene Version aus einer Konstante | Wird vergessen, Meldung „ist aktuell" für immer | Aus der laufenden Programmdatei lesen |
| Dateiname der Installationsdatei geändert | Alle älteren Fassungen finden nichts mehr | Namensbestandteil festlegen und behalten |
| Fehlende Programmkennung im Anfragekopf | Anbieter weist die Anfrage ab | Kennung immer mitschicken |
| Dialog bei ausgeblendetem Fenster | Springt unaufgefordert auf | Hinweis am Symbol, Frage beim Öffnen |
| Ablehnung nicht gemerkt | Dieselbe Frage bei jedem Start | Abgelehnte Fassung speichern |
| Ortszeit statt Weltzeit | Zeitfenster rechnet sich falsch | Durchgehend Weltzeit |

Zwei Dinge, die beim Veröffentlichen auffallen und nichts mit der Anwendung zu tun haben: Manche
Anbieter — GitHub etwa — **sortieren die Dateien einer Veröffentlichung alphabetisch**, nicht nach
Reihenfolge des Hochladens. Soll die Installationsdatei oben stehen, muss ihr Name danach gebaut
sein, etwa mit der Fassung vorn. Und die automatisch angehängten Quelltext-Archive lassen sich nicht
abschalten.

---

## 11. Was du entscheiden musst

- **Woher die Liste der Veröffentlichungen kommt.** Bei einem nicht öffentlichen Ablageort brauchst
  du eine Zugangsberechtigung — und die gehört nicht in eine ausgelieferte Anwendung. Dann ist eine
  schlichte Datei auf einem Webserver der ehrlichere Weg.
- **Welcher Namensbestandteil die Installationsdatei kennzeichnet.** Einmal, für immer.
- **Wie lang das Zeitfenster ist.** Ein Tag ist ein guter Ausgangswert.
- **Ob „automatisch" die Voreinstellung sein soll.** Ich würde „fragen" voreinstellen: eine
  Anwendung, die sich beim ersten Start ungefragt selbst austauscht, überrascht Leute.
- **Wie ein stiller Start aussieht**, falls deine Anwendung das kennt — davon hängt ab, wie sie
  nach der Aktualisierung zurückkommt.

---

## 12. Grenzen dieses Entwurfs

Sag diese offen dazu, wenn du das Verfahren woanders einsetzt:

- **Keine Signaturprüfung.** Heruntergeladen wird über eine verschlüsselte Verbindung vom Anbieter,
  mehr nicht. Das vertraut auf dessen Zertifikat und darauf, dass niemand das Projektkonto
  übernimmt. Wer mehr will, prüft eine Signatur oder eine Prüfsumme aus einer **zweiten** Quelle —
  eine Prüfsumme aus derselben Veröffentlichung schützt gegen nichts, denn wer die Datei tauschen
  kann, tauscht die Prüfsumme gleich mit.
- **Kein Fortsetzen.** Ein abgebrochener Download beginnt von vorn.
- **Kein Fortschrittsbalken**, nur eine Statuszeile.
- **Kein Zurück.** Läuft die neue Fassung nicht, hilft nur, die vorige von Hand zu installieren.
- **Nur ein Zeitpunkt**, der Programmstart. Wer die Anwendung wochenlang durchlaufen lässt, erfährt
  nie etwas. Ein wiederkehrender Zeitgeber wäre leicht ergänzt.
