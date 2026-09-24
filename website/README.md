# Website

Statische Seite für Audio Mirror, ohne Build-Schritt: `index.html`, `assets/`, `screenshots/`.

## Veröffentlichen mit Cloudflare Pages

1. Cloudflare Dashboard → **Workers & Pages** → **Create** → **Pages** → **Connect to Git**.
2. Repository `yyusvf/audio-mirror` wählen.
3. Einstellungen:
   - **Production branch:** `main`
   - **Framework preset:** None
   - **Build command:** leer lassen
   - **Build output directory:** `website`
4. **Save and Deploy.** Jeder Push auf `main` veröffentlicht die Seite neu.

Eigene Domain: im Pages-Projekt unter **Custom domains** eintragen.

`_headers` setzt Sicherheits-Header und das Caching für Cloudflare.

## Screenshots

Die Bilder in `screenshots/` sind vorläufig aus den XAML-Dateien nachgezeichnet. Echte Aufnahmen:

1. Unter Windows `website\make-screenshots.cmd` doppelklicken. Das baut die Debug-Fassung und
   zeichnet das Fenster mit `--render` in vier PNGs (Geräte und Einstellungen, jeweils hell und
   dunkel, auf Englisch). Es erscheint dabei kein Fenster.
2. Committen und pushen.

Die Seite zeigt automatisch das helle oder dunkle Bild, je nach Systemeinstellung des Besuchers.

## Downloads

Die Links zeigen auf das aktuelle GitHub-Release. `assets/site.js` fragt beim Laden die
GitHub-API ab und setzt Version, Dateigröße und Link des neuesten Releases ein; ohne JavaScript
greifen die Links im Markup.

## Symbole

`assets/fonts/Phosphor.woff2` enthält nur die Symbole, die die Seite nutzt (Subset, rund 3 KB).
Wer ein neues `ph-...`-Symbol einbaut, muss es in `assets/icons.css` eintragen und die Schrift
neu zuschneiden (`pyftsubset` aus fonttools, Quelle: npm-Paket `@phosphor-icons/web`).

## Cache

Nur die Schriften werden dauerhaft gecacht (`_headers`). HTML, CSS, JS und Bilder fragt der
Browser bei jedem Besuch neu an, Änderungen sind also sofort sichtbar. Die Endung `?v=2` an
CSS und JS in `index.html` ist nur nötig, wenn sich das Caching selbst ändert; bei normalen
Änderungen muss man sie nicht hochzählen.
