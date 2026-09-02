using System.Globalization;

namespace AudioMirror;

/// <summary>
/// Oberflächentexte in dreizehn Sprachen.
///
/// Englisch ist die Voreinstellung; jede andere Sprache erscheint nur, wenn Windows selbst darauf
/// eingestellt ist oder sie in den Einstellungen gewählt wurde. Maßgeblich ist die Anzeigesprache
/// (<see cref="CultureInfo.CurrentUICulture"/>), nicht das Regionsformat - jemand mit deutschem
/// Datumsformat, aber englischem Windows, erwartet Englisch.
///
/// Jeder Text steht mit allen Übersetzungen beieinander. Benannte Argumente sind hier der
/// entscheidende Kunstgriff: bei dreizehn Sprachen wäre eine Reihe gleichaussehender Zeichenketten
/// nicht mehr zu lesen, und eine vergessene Sprache fällt sofort auf, statt still zu verrutschen.
/// Fehlt eine Übersetzung, erscheint der englische Text.
/// </summary>
internal static class Strings
{
    /// <summary>Sprachen, für die Texte vorliegen. Alles andere landet bei Englisch.</summary>
    public static readonly string[] Supported =
        ["en", "de", "fr", "es", "it", "pt", "nl", "pl", "ru", "uk", "tr", "cs", "sv"];

    /// <summary>Eigenbezeichnungen für die Sprachauswahl - jede Sprache nennt sich selbst.
    /// Gleiche Reihenfolge wie <see cref="Supported"/>.</summary>
    public static readonly string[] SupportedNames =
        ["English", "Deutsch", "Français", "Español", "Italiano", "Português (Brasil)",
         "Nederlands", "Polski", "Русский", "Українська", "Türkçe", "Čeština", "Svenska"];

    private static string? forced;

    /// <summary>Kürzel der Sprache, in der die Oberfläche gerade erscheint.</summary>
    public static string Language => forced ?? Detect();

    /// <summary>
    /// Legt die Sprache fest. Ein bekanntes Kürzel erzwingt sie, alles andere folgt Windows.
    /// Muss vor dem ersten Textzugriff aufgerufen werden.
    /// </summary>
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
            // Ohne ermittelbare Anzeigesprache bleibt es bei Englisch.
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
            "es" => es ?? en,
            "it" => it ?? en,
            "pt" => pt ?? en,
            "nl" => nl ?? en,
            "pl" => pl ?? en,
            "ru" => ru ?? en,
            "uk" => uk ?? en,
            "tr" => tr ?? en,
            "cs" => cs ?? en,
            "sv" => sv ?? en,
            _ => en,
        };

    // Fenster und Kopfbereich
    public static string AppTitle => "Audio Mirror";

    public static string Source => T("Source",
        de: "Quelle", fr: "Source", es: "Origen", it: "Sorgente", pt: "Origem", nl: "Bron",
        pl: "Źródło", ru: "Источник", uk: "Джерело", tr: "Kaynak", cs: "Zdroj", sv: "Källa");

    public static string TargetDevices => T("Output devices",
        de: "Zielgeräte", fr: "Périphériques de sortie", es: "Dispositivos de salida",
        it: "Dispositivi di uscita", pt: "Dispositivos de saída", nl: "Uitvoerapparaten",
        pl: "Urządzenia wyjściowe", ru: "Устройства вывода", uk: "Пристрої виведення",
        tr: "Çıkış aygıtları", cs: "Výstupní zařízení", sv: "Uppspelningsenheter");

    public static string Connected => T("Connected",
        de: "Verbunden", fr: "Connectés", es: "Conectados", it: "Collegati", pt: "Conectados",
        nl: "Verbonden", pl: "Podłączone", ru: "Подключены", uk: "Підключені", tr: "Bağlı",
        cs: "Připojená", sv: "Anslutna");

    public static string Disconnected => T("Disconnected",
        de: "Getrennt", fr: "Déconnectés", es: "Desconectados", it: "Scollegati",
        pt: "Desconectados", nl: "Niet verbonden", pl: "Odłączone", ru: "Отключены",
        uk: "Відключені", tr: "Bağlı değil", cs: "Odpojená", sv: "Frånkopplade");

    public static string Close => T("Close",
        de: "Schließen", fr: "Fermer", es: "Cerrar", it: "Chiudi", pt: "Fechar", nl: "Sluiten",
        pl: "Zamknij", ru: "Закрыть", uk: "Закрити", tr: "Kapat", cs: "Zavřít", sv: "Stäng");

    public static string Ready => T("Ready.",
        de: "Bereit.", fr: "Prêt.", es: "Listo.", it: "Pronto.", pt: "Pronto.", nl: "Gereed.",
        pl: "Gotowe.", ru: "Готово.", uk: "Готово.", tr: "Hazır.", cs: "Připraveno.", sv: "Klar.");

    public static string WindowsDefaultDevice(string current) => T(
        $"Windows default device ({current})",
        de: $"Windows-Standardgerät ({current})",
        fr: $"Périphérique par défaut de Windows ({current})",
        es: $"Dispositivo predeterminado de Windows ({current})",
        it: $"Dispositivo predefinito di Windows ({current})",
        pt: $"Dispositivo padrão do Windows ({current})",
        nl: $"Standaardapparaat van Windows ({current})",
        pl: $"Domyślne urządzenie systemu Windows ({current})",
        ru: $"Устройство Windows по умолчанию ({current})",
        uk: $"Пристрій Windows за замовчуванням ({current})",
        tr: $"Windows varsayılan aygıtı ({current})",
        cs: $"Výchozí zařízení systému Windows ({current})",
        sv: $"Windows standardenhet ({current})");

    public static string NoDeviceAvailable => T("none available",
        de: "keines vorhanden", fr: "aucun disponible", es: "ninguno disponible",
        it: "nessuno disponibile", pt: "nenhum disponível", nl: "geen beschikbaar",
        pl: "brak dostępnych", ru: "нет доступных", uk: "немає доступних", tr: "yok",
        cs: "žádné k dispozici", sv: "ingen tillgänglig");

    public static string LastChosenUnavailable => T("Last selected device (unavailable)",
        de: "Zuletzt gewähltes Gerät (nicht verfügbar)",
        fr: "Dernier périphérique choisi (indisponible)",
        es: "Último dispositivo elegido (no disponible)",
        it: "Ultimo dispositivo scelto (non disponibile)",
        pt: "Último dispositivo escolhido (indisponível)",
        nl: "Laatst gekozen apparaat (niet beschikbaar)",
        pl: "Ostatnio wybrane urządzenie (niedostępne)",
        ru: "Последнее выбранное устройство (недоступно)",
        uk: "Останній вибраний пристрій (недоступний)",
        tr: "Son seçilen aygıt (kullanılamıyor)",
        cs: "Naposledy zvolené zařízení (nedostupné)",
        sv: "Senast valda enhet (inte tillgänglig)");

    // Puffer
    public static string BufferLabel => T("Buffer / latency:",
        de: "Puffer / Latenz:", fr: "Tampon / latence :", es: "Búfer / latencia:",
        it: "Buffer / latenza:", pt: "Buffer / latência:", nl: "Buffer / latentie:",
        pl: "Bufor / opóźnienie:", ru: "Буфер / задержка:", uk: "Буфер / затримка:",
        tr: "Arabellek / gecikme:", cs: "Buffer / latence:", sv: "Buffert / latens:");

    public static string Milliseconds => "ms";

    public static string BufferTip => T(
        "Smaller = less latency, larger = more headroom against dropouts."
        + Environment.NewLine + "Default is 30 ms. Increase it if you hear crackling.",
        de: "Kleiner = weniger Latenz, größer = mehr Reserve gegen Aussetzer."
        + Environment.NewLine + "Voreinstellung 30 ms. Bei Knacksern erhöhen.",
        fr: "Plus petit = moins de latence, plus grand = plus de marge contre les coupures."
        + Environment.NewLine + "Valeur par défaut : 30 ms. À augmenter en cas de craquements.",
        es: "Menor = menos latencia, mayor = más margen frente a los cortes."
        + Environment.NewLine + "Valor predeterminado: 30 ms. Auméntalo si oyes chasquidos.",
        it: "Più piccolo = meno latenza, più grande = più margine contro le interruzioni."
        + Environment.NewLine + "Valore predefinito: 30 ms. Aumentalo se senti crepitii.",
        pt: "Menor = menos latência, maior = mais margem contra falhas."
        + Environment.NewLine + "Padrão: 30 ms. Aumente se ouvir estalos.",
        nl: "Kleiner = minder latentie, groter = meer marge tegen onderbrekingen."
        + Environment.NewLine + "Standaard 30 ms. Verhoog dit bij geknetter.",
        pl: "Mniej = mniejsze opóźnienie, więcej = większy zapas przeciw przerwom."
        + Environment.NewLine + "Domyślnie 30 ms. Zwiększ przy trzaskach.",
        ru: "Меньше — ниже задержка, больше — надёжнее против пропусков звука."
        + Environment.NewLine + "По умолчанию 30 мс. При треске увеличьте.",
        uk: "Менше — менша затримка, більше — більший запас проти пропусків звуку."
        + Environment.NewLine + "За замовчуванням 30 мс. За тріску збільште.",
        tr: "Küçük = daha az gecikme, büyük = kesintilere karşı daha fazla pay."
        + Environment.NewLine + "Varsayılan 30 ms. Çıtırtı duyarsanız artırın.",
        cs: "Méně = nižší latence, více = větší rezerva proti výpadkům."
        + Environment.NewLine + "Výchozí hodnota je 30 ms. Při praskání ji zvyšte.",
        sv: "Mindre = lägre latens, större = mer marginal mot avbrott."
        + Environment.NewLine + "Standard är 30 ms. Öka vid knaster.");

    // Hotkey
    public static string ToggleAllLabel => T("Toggle everything:",
        de: "Alles umschalten:", fr: "Tout basculer :", es: "Alternar todo:",
        it: "Attiva/disattiva tutto:", pt: "Alternar tudo:", nl: "Alles omschakelen:",
        pl: "Przełącz wszystko:", ru: "Переключить всё:", uk: "Перемкнути все:",
        tr: "Tümünü değiştir:", cs: "Přepnout vše:", sv: "Växla allt:");

    public static string HotkeyEnabled => T("Hotkey enabled",
        de: "Hotkey aktiviert", fr: "Raccourci activé", es: "Atajo activado",
        it: "Scorciatoia attiva", pt: "Atalho ativado", nl: "Sneltoets ingeschakeld",
        pl: "Skrót włączony", ru: "Сочетание включено", uk: "Комбінацію ввімкнено",
        tr: "Kısayol etkin", cs: "Klávesová zkratka zapnuta", sv: "Snabbtangent aktiverad");

    public static string PressCombination => T("Press a combination …",
        de: "Kombination drücken …", fr: "Appuyez sur une combinaison …",
        es: "Pulsa una combinación …", it: "Premi una combinazione …",
        pt: "Pressione uma combinação …", nl: "Druk op een combinatie …",
        pl: "Naciśnij kombinację …", ru: "Нажмите сочетание клавиш …",
        uk: "Натисніть комбінацію …", tr: "Bir tuş birleşimine basın …",
        cs: "Stiskněte kombinaci …", sv: "Tryck på en kombination …");

    public static string NoHotkey => T("none",
        de: "keine", fr: "aucun", es: "ninguno", it: "nessuna", pt: "nenhum", nl: "geen",
        pl: "brak", ru: "нет", uk: "немає", tr: "yok", cs: "žádná", sv: "ingen");

    public static string HotkeyTaken => T("That combination is already used by another program.",
        de: "Die Tastenkombination ist bereits von einem anderen Programm belegt.",
        fr: "Cette combinaison est déjà utilisée par un autre programme.",
        es: "Esa combinación ya la usa otro programa.",
        it: "Questa combinazione è già usata da un altro programma.",
        pt: "Essa combinação já é usada por outro programa.",
        nl: "Die combinatie wordt al door een ander programma gebruikt.",
        pl: "Ta kombinacja jest już używana przez inny program.",
        ru: "Это сочетание клавиш уже занято другой программой.",
        uk: "Ця комбінація вже зайнята іншою програмою.",
        tr: "Bu tuş birleşimi başka bir program tarafından kullanılıyor.",
        cs: "Tuto kombinaci už používá jiný program.",
        sv: "Kombinationen används redan av ett annat program.");

    public static string HotkeyAssignedTo(string combination, string owner) => T(
        $"\"{combination}\" is already assigned to {owner}.",
        de: $"„{combination}“ ist bereits für {owner} vergeben.",
        fr: $"« {combination} » est déjà attribué à {owner}.",
        es: $"«{combination}» ya está asignado a {owner}.",
        it: $"«{combination}» è già assegnato a {owner}.",
        pt: $"\"{combination}\" já está atribuído a {owner}.",
        nl: $"\u2018{combination}\u2019 is al toegewezen aan {owner}.",
        pl: $"„{combination}” jest już przypisane do {owner}.",
        ru: $"«{combination}» уже назначено для {owner}.",
        uk: $"«{combination}» уже призначено для {owner}.",
        tr: $"\"{combination}\" zaten {owner} için atanmış.",
        cs: $"„{combination}“ je již přiřazeno k {owner}.",
        sv: $"”{combination}” är redan tilldelat {owner}.");

    public static string MirroringOff => T("Mirroring off",
        de: "Spiegelung aus", fr: "Duplication désactivée", es: "Duplicación desactivada",
        it: "Duplicazione disattivata", pt: "Duplicação desativada", nl: "Spiegelen uit",
        pl: "Powielanie wyłączone", ru: "Дублирование выключено", uk: "Дублювання вимкнено",
        tr: "Yansıtma kapalı", cs: "Zrcadlení vypnuto", sv: "Spegling av");

    public static string MirroringOn => T("Mirroring on",
        de: "Spiegelung an", fr: "Duplication activée", es: "Duplicación activada",
        it: "Duplicazione attivata", pt: "Duplicação ativada", nl: "Spiegelen aan",
        pl: "Powielanie włączone", ru: "Дублирование включено", uk: "Дублювання ввімкнено",
        tr: "Yansıtma açık", cs: "Zrcadlení zapnuto", sv: "Spegling på");

    public static string MutedDevices(int count) => T(
        $"{count} device(s) muted. Press again to restore them.",
        de: $"{count} Gerät(e) stummgeschaltet. Erneut drücken stellt sie wieder her.",
        fr: $"{count} périphérique(s) coupé(s). Appuyez à nouveau pour les rétablir.",
        es: $"{count} dispositivo(s) silenciado(s). Pulsa de nuevo para restaurarlos.",
        it: $"{count} dispositivo/i disattivato/i. Premi di nuovo per ripristinarli.",
        pt: $"{count} dispositivo(s) sem som. Pressione novamente para restaurá-los.",
        nl: $"{count} apparaat/apparaten gedempt. Druk nogmaals om ze te herstellen.",
        pl: $"Wyciszono urządzenia: {count}. Naciśnij ponownie, aby je przywrócić.",
        ru: $"Отключён звук на устройствах: {count}. Нажмите ещё раз, чтобы вернуть.",
        uk: $"Вимкнено звук на пристроях: {count}. Натисніть ще раз, щоб повернути.",
        tr: $"{count} aygıt sessize alındı. Geri almak için yeniden basın.",
        cs: $"Ztlumeno zařízení: {count}. Dalším stiskem je obnovíte.",
        sv: $"{count} enhet(er) tystade. Tryck igen för att återställa dem.");

    public static string RestoredDevices(int restored) => T(
        $"{restored} device(s) restored.",
        de: $"{restored} Gerät(e) wiederhergestellt.",
        fr: $"{restored} périphérique(s) rétabli(s).",
        es: $"{restored} dispositivo(s) restaurado(s).",
        it: $"{restored} dispositivo/i ripristinato/i.",
        pt: $"{restored} dispositivo(s) restaurado(s).",
        nl: $"{restored} apparaat/apparaten hersteld.",
        pl: $"Przywrócono urządzenia: {restored}.",
        ru: $"Восстановлено устройств: {restored}.",
        uk: $"Відновлено пристроїв: {restored}.",
        tr: $"{restored} aygıt geri alındı.",
        cs: $"Obnoveno zařízení: {restored}.",
        sv: $"{restored} enhet(er) återställda.");

    public static string RestoredDevicesPartly(int restored, int skipped) => T(
        $"{restored} device(s) restored, {skipped} no longer available.",
        de: $"{restored} Gerät(e) wiederhergestellt, {skipped} nicht mehr verfügbar.",
        fr: $"{restored} périphérique(s) rétabli(s), {skipped} ne sont plus disponibles.",
        es: $"{restored} dispositivo(s) restaurado(s), {skipped} ya no están disponibles.",
        it: $"{restored} dispositivo/i ripristinato/i, {skipped} non più disponibili.",
        pt: $"{restored} dispositivo(s) restaurado(s), {skipped} já não estão disponíveis.",
        nl: $"{restored} apparaat/apparaten hersteld, {skipped} niet meer beschikbaar.",
        pl: $"Przywrócono urządzenia: {restored}, niedostępne: {skipped}.",
        ru: $"Восстановлено устройств: {restored}, больше недоступно: {skipped}.",
        uk: $"Відновлено пристроїв: {restored}, більше недоступно: {skipped}.",
        tr: $"{restored} aygıt geri alındı, {skipped} artık kullanılamıyor.",
        cs: $"Obnoveno zařízení: {restored}, již nedostupných: {skipped}.",
        sv: $"{restored} enhet(er) återställda, {skipped} är inte längre tillgängliga.");

    public static string NoRememberedState => T("Nothing remembered yet - tick a device first.",
        de: "Kein gemerkter Zustand vorhanden – erst etwas anhaken.",
        fr: "Rien n'a encore été mémorisé - cochez d'abord un périphérique.",
        es: "Todavía no hay nada guardado: marca primero un dispositivo.",
        it: "Non c'è ancora nulla in memoria: seleziona prima un dispositivo.",
        pt: "Ainda não há nada memorizado: marque primeiro um dispositivo.",
        nl: "Er is nog niets onthouden - vink eerst een apparaat aan.",
        pl: "Nic jeszcze nie zapamiętano - najpierw zaznacz urządzenie.",
        ru: "Пока нечего восстанавливать - сначала отметьте устройство.",
        uk: "Поки нічого відновлювати - спочатку позначте пристрій.",
        tr: "Henüz kayıtlı bir durum yok - önce bir aygıt işaretleyin.",
        cs: "Zatím není co obnovit - nejprve zaškrtněte zařízení.",
        sv: "Inget är sparat ännu - kryssa först i en enhet.");

    // Autostart
    public static string StartWithWindows => T("Start with Windows (in the notification area)",
        de: "Mit Windows starten (startet im Infobereich)",
        fr: "Démarrer avec Windows (dans la zone de notification)",
        es: "Iniciar con Windows (en el área de notificación)",
        it: "Avvia con Windows (nell'area di notifica)",
        pt: "Iniciar com o Windows (na área de notificação)",
        nl: "Met Windows starten (in het systeemvak)",
        pl: "Uruchamiaj z systemem Windows (w obszarze powiadomień)",
        ru: "Запускать вместе с Windows (в области уведомлений)",
        uk: "Запускати разом із Windows (в області сповіщень)",
        tr: "Windows ile başlat (bildirim alanında)",
        cs: "Spouštět se systémem Windows (v oznamovací oblasti)",
        sv: "Starta med Windows (i meddelandefältet)");

    /// <summary>Kurzfassung für das Menü im Infobereich - dort ist der Zusatz nur im Weg.</summary>
    public static string StartWithWindowsShort => T("Start with Windows",
        de: "Mit Windows starten", fr: "Démarrer avec Windows", es: "Iniciar con Windows",
        it: "Avvia con Windows", pt: "Iniciar com o Windows", nl: "Met Windows starten",
        pl: "Uruchamiaj z systemem Windows", ru: "Запускать вместе с Windows",
        uk: "Запускати разом із Windows", tr: "Windows ile başlat",
        cs: "Spouštět se systémem Windows", sv: "Starta med Windows");

    // Infobereich
    public static string OpenWindow => T("Open window",
        de: "Fenster öffnen", fr: "Ouvrir la fenêtre", es: "Abrir ventana",
        it: "Apri finestra", pt: "Abrir janela", nl: "Venster openen", pl: "Otwórz okno",
        ru: "Открыть окно", uk: "Відкрити вікно", tr: "Pencereyi aç", cs: "Otevřít okno",
        sv: "Öppna fönstret");

    public static string Exit => T("Exit",
        de: "Beenden", fr: "Quitter", es: "Salir", it: "Esci", pt: "Sair", nl: "Afsluiten",
        pl: "Zakończ", ru: "Выход", uk: "Вихід", tr: "Çıkış", cs: "Ukončit", sv: "Avsluta");

    public static string NoOutputDevices => T("No output devices found",
        de: "Keine Ausgabegeräte gefunden", fr: "Aucun périphérique de sortie trouvé",
        es: "No se han encontrado dispositivos de salida",
        it: "Nessun dispositivo di uscita trovato",
        pt: "Nenhum dispositivo de saída encontrado",
        nl: "Geen uitvoerapparaten gevonden", pl: "Nie znaleziono urządzeń wyjściowych",
        ru: "Устройства вывода не найдены", uk: "Пристроїв виведення не знайдено",
        tr: "Çıkış aygıtı bulunamadı", cs: "Nebyla nalezena žádná výstupní zařízení",
        sv: "Inga uppspelningsenheter hittades");

    public static string SourceSuffix => T("  (source)",
        de: "  (Quelle)", fr: "  (source)", es: "  (origen)", it: "  (sorgente)",
        pt: "  (origem)", nl: "  (bron)", pl: "  (źródło)", ru: "  (источник)",
        uk: "  (джерело)", tr: "  (kaynak)", cs: "  (zdroj)", sv: "  (källa)");

    public static string SourceShort => T("source",
        de: "Quelle", fr: "source", es: "origen", it: "sorgente", pt: "origem", nl: "bron",
        pl: "źródło", ru: "источник", uk: "джерело", tr: "kaynak", cs: "zdroj", sv: "källa");

    public static string NoAppPlaying => T("No application is playing sound.",
        de: "Zurzeit gibt keine Anwendung Ton aus.",
        fr: "Aucune application ne joue de son actuellement.",
        es: "Ninguna aplicación está reproduciendo sonido.",
        it: "Nessuna applicazione sta riproducendo audio.",
        pt: "Nenhuma aplicação está a reproduzir som.",
        nl: "Er speelt momenteel geen toepassing geluid af.",
        pl: "Żadna aplikacja nie odtwarza teraz dźwięku.",
        ru: "Сейчас ни одно приложение не воспроизводит звук.",
        uk: "Зараз жодна програма не відтворює звук.",
        tr: "Şu anda hiçbir uygulama ses çalmıyor.",
        cs: "Žádná aplikace teď nepřehrává zvuk.",
        sv: "Inget program spelar upp ljud just nu.");

    public static string TrayNoMirroring => T("Audio Mirror – not mirroring",
        de: "Audio Mirror – keine Spiegelung", fr: "Audio Mirror – aucune duplication",
        es: "Audio Mirror – sin duplicación", it: "Audio Mirror – nessuna duplicazione",
        pt: "Audio Mirror – sem duplicação", nl: "Audio Mirror – geen spiegeling",
        pl: "Audio Mirror – brak powielania", ru: "Audio Mirror – дублирование выключено",
        uk: "Audio Mirror – дублювання вимкнено", tr: "Audio Mirror – yansıtma yok",
        cs: "Audio Mirror – žádné zrcadlení", sv: "Audio Mirror – ingen spegling");

    public static string TrayMirroring(int count) => T(
        $"Audio Mirror – mirroring to {count} device(s)",
        de: $"Audio Mirror – spiegelt auf {count} Gerät(e)",
        fr: $"Audio Mirror – duplication vers {count} périphérique(s)",
        es: $"Audio Mirror – duplicando en {count} dispositivo(s)",
        it: $"Audio Mirror – duplicazione su {count} dispositivo/i",
        pt: $"Audio Mirror – duplicando em {count} dispositivo(s)",
        nl: $"Audio Mirror – spiegelt naar {count} apparaat/apparaten",
        pl: $"Audio Mirror – powielanie na urządzenia: {count}",
        ru: $"Audio Mirror – дублирование на устройства: {count}",
        uk: $"Audio Mirror – дублювання на пристрої: {count}",
        tr: $"Audio Mirror – {count} aygıta yansıtılıyor",
        cs: $"Audio Mirror – zrcadlení na zařízení: {count}",
        sv: $"Audio Mirror – speglar till {count} enhet(er)");

    public static string StillRunningTitle => T("Audio Mirror keeps running",
        de: "Audio Mirror läuft weiter", fr: "Audio Mirror continue de fonctionner",
        es: "Audio Mirror sigue en marcha", it: "Audio Mirror continua a funzionare",
        pt: "O Audio Mirror continua a funcionar", nl: "Audio Mirror blijft actief",
        pl: "Audio Mirror działa dalej", ru: "Audio Mirror продолжает работать",
        uk: "Audio Mirror продовжує працювати", tr: "Audio Mirror çalışmaya devam ediyor",
        cs: "Audio Mirror běží dál", sv: "Audio Mirror fortsätter köras");

    public static string StillRunningBody => T(
        "The window is only hidden, mirroring continues in the background. "
        + "Double-click the icon to bring it back; \"Exit\" closes the program.",
        de: "Das Fenster ist nur ausgeblendet, die Spiegelung läuft im Hintergrund weiter. "
        + "Doppelklick auf das Symbol holt es zurück, „Beenden“ schließt das Programm.",
        fr: "La fenêtre est seulement masquée, la duplication continue en arrière-plan. "
        + "Double-cliquez sur l'icône pour la rouvrir ; « Quitter » ferme le programme.",
        es: "La ventana solo está oculta, la duplicación sigue en segundo plano. "
        + "Haz doble clic en el icono para recuperarla; «Salir» cierra el programa.",
        it: "La finestra è solo nascosta, la duplicazione continua in secondo piano. "
        + "Fai doppio clic sull'icona per riaprirla; «Esci» chiude il programma.",
        pt: "A janela está apenas oculta, a duplicação continua em segundo plano. "
        + "Faça duplo clique no ícone para a recuperar; \"Sair\" fecha o programa.",
        nl: "Het venster is alleen verborgen, het spiegelen gaat op de achtergrond door. "
        + "Dubbelklik op het pictogram om het terug te halen; \u2018Afsluiten\u2019 sluit het programma.",
        pl: "Okno jest tylko ukryte, powielanie działa dalej w tle. "
        + "Kliknij dwukrotnie ikonę, aby je przywrócić; „Zakończ” zamyka program.",
        ru: "Окно лишь скрыто, дублирование продолжается в фоне. "
        + "Двойной щелчок по значку вернёт его; «Выход» закрывает программу.",
        uk: "Вікно лише сховано, дублювання триває у фоні. "
        + "Подвійний клац по значку поверне його; «Вихід» закриває програму.",
        tr: "Pencere yalnızca gizlendi, yansıtma arka planda sürüyor. "
        + "Geri getirmek için simgeye çift tıklayın; \"Çıkış\" programı kapatır.",
        cs: "Okno je jen skryté, zrcadlení běží dál na pozadí. "
        + "Dvojklikem na ikonu je vrátíte; „Ukončit“ program zavře.",
        sv: "Fönstret är bara dolt, speglingen fortsätter i bakgrunden. "
        + "Dubbelklicka på ikonen för att ta fram det; ”Avsluta” stänger programmet.");

    // Statuszeile
    public static string NothingTicked => T("No output device ticked.",
        de: "Kein Zielgerät angehakt.", fr: "Aucun périphérique de sortie coché.",
        es: "Ningún dispositivo de salida marcado.",
        it: "Nessun dispositivo di uscita selezionato.",
        pt: "Nenhum dispositivo de saída marcado.",
        nl: "Geen uitvoerapparaat aangevinkt.", pl: "Nie zaznaczono urządzenia wyjściowego.",
        ru: "Устройство вывода не отмечено.", uk: "Пристрій виведення не позначено.",
        tr: "İşaretli çıkış aygıtı yok.", cs: "Není zaškrtnuto žádné výstupní zařízení.",
        sv: "Ingen uppspelningsenhet ikryssad.");

    public static string MirroringOnDevices(int running) => T(
        $"Mirroring to {running} device(s).",
        de: $"Spiegelung läuft auf {running} Gerät(en).",
        fr: $"Duplication vers {running} périphérique(s).",
        es: $"Duplicando en {running} dispositivo(s).",
        it: $"Duplicazione su {running} dispositivo/i.",
        pt: $"Duplicando em {running} dispositivo(s).",
        nl: $"Spiegelt naar {running} apparaat/apparaten.",
        pl: $"Powielanie na urządzenia: {running}.",
        ru: $"Дублирование на устройства: {running}.",
        uk: $"Дублювання на пристрої: {running}.",
        tr: $"{running} aygıta yansıtılıyor.",
        cs: $"Zrcadlení na zařízení: {running}.",
        sv: $"Speglar till {running} enhet(er).");

    public static string StillWaiting(int failed) => T(
        $" {failed} still waiting.",
        de: $" {failed} wartet noch.",
        fr: $" {failed} en attente.",
        es: $" {failed} en espera.",
        it: $" {failed} in attesa.",
        pt: $" {failed} em espera.",
        nl: $" {failed} wacht nog.",
        pl: $" Oczekuje: {failed}.",
        ru: $" Ожидают: {failed}.",
        uk: $" Очікують: {failed}.",
        tr: $" {failed} bekliyor.",
        cs: $" Čeká: {failed}.",
        sv: $" {failed} väntar.");

    public static string WaitingFor(string reason, int selected) => T(
        $"Waiting for {reason} ({selected} device(s) ticked).",
        de: $"Wartet auf {reason} ({selected} Gerät(e) angehakt).",
        fr: $"En attente de {reason} ({selected} périphérique(s) coché(s)).",
        es: $"Esperando a {reason} ({selected} dispositivo(s) marcado(s)).",
        it: $"In attesa di {reason} ({selected} dispositivo/i selezionato/i).",
        pt: $"À espera de {reason} ({selected} dispositivo(s) marcado(s)).",
        nl: $"Wacht op {reason} ({selected} apparaat/apparaten aangevinkt).",
        pl: $"Oczekiwanie na {reason} (zaznaczone urządzenia: {selected}).",
        ru: $"Ожидание: {reason} (отмечено устройств: {selected}).",
        uk: $"Очікування: {reason} (позначено пристроїв: {selected}).",
        tr: $"{reason} bekleniyor ({selected} aygıt işaretli).",
        cs: $"Čeká se na {reason} (zaškrtnuto zařízení: {selected}).",
        sv: $"Väntar på {reason} ({selected} enhet(er) ikryssade).");

    public static string DeviceOrApp => T("device or application",
        de: "Gerät bzw. Anwendung", fr: "un périphérique ou une application",
        es: "un dispositivo o una aplicación", it: "un dispositivo o un'applicazione",
        pt: "um dispositivo ou aplicação", nl: "een apparaat of toepassing",
        pl: "urządzenie lub aplikację", ru: "устройство или приложение",
        uk: "пристрій або програму", tr: "aygıt veya uygulama",
        cs: "zařízení nebo aplikaci", sv: "en enhet eller ett program");

    public static string SourceUnavailable => T(
        "The selected source device is unavailable - mirroring is paused until it returns "
        + "or another source is chosen.",
        de: "Das gewählte Quellgerät ist nicht verfügbar – die Spiegelung pausiert, "
        + "bis es zurück ist oder eine andere Quelle gewählt wird.",
        fr: "Le périphérique source choisi est indisponible - la duplication est en pause "
        + "jusqu'à son retour ou jusqu'au choix d'une autre source.",
        es: "El dispositivo de origen elegido no está disponible: la duplicación se detiene "
        + "hasta que vuelva o se elija otro origen.",
        it: "Il dispositivo sorgente scelto non è disponibile: la duplicazione è in pausa "
        + "finché non torna o non si sceglie un'altra sorgente.",
        pt: "O dispositivo de origem escolhido não está disponível: a duplicação fica em pausa "
        + "até que volte ou seja escolhida outra origem.",
        nl: "Het gekozen bronapparaat is niet beschikbaar - het spiegelen pauzeert "
        + "tot het terug is of een andere bron wordt gekozen.",
        pl: "Wybrane urządzenie źródłowe jest niedostępne - powielanie jest wstrzymane, "
        + "dopóki nie wróci lub nie zostanie wybrane inne źródło.",
        ru: "Выбранное устройство-источник недоступно - дублирование приостановлено, "
        + "пока оно не вернётся или не будет выбран другой источник.",
        uk: "Вибраний пристрій-джерело недоступний - дублювання призупинено, "
        + "доки він не повернеться або не буде вибрано інше джерело.",
        tr: "Seçilen kaynak aygıt kullanılamıyor - geri gelene ya da başka bir kaynak "
        + "seçilene kadar yansıtma duraklatıldı.",
        cs: "Zvolené zdrojové zařízení není dostupné - zrcadlení je pozastaveno, "
        + "dokud se nevrátí nebo dokud nezvolíte jiný zdroj.",
        sv: "Den valda källenheten är inte tillgänglig - speglingen är pausad "
        + "tills den kommer tillbaka eller en annan källa väljs.");

    public static string RetryRunning => T(" – retrying",
        de: " – neuer Versuch läuft", fr: " – nouvelle tentative", es: " – reintentando",
        it: " – nuovo tentativo", pt: " – a tentar de novo", nl: " – nieuwe poging",
        pl: " – ponawianie", ru: " – повторная попытка", uk: " – повторна спроба",
        tr: " – yeniden deneniyor", cs: " – nový pokus", sv: " – nytt försök");

    public static string NotConnected => T("not connected – setting is kept",
        de: "nicht verbunden – Einstellung bleibt gespeichert",
        fr: "non connecté – le réglage est conservé",
        es: "no conectado – se conserva el ajuste",
        it: "non collegato – l'impostazione viene mantenuta",
        pt: "não conectado – a definição é mantida",
        nl: "niet verbonden – instelling blijft bewaard",
        pl: "niepodłączone – ustawienie zostaje zachowane",
        ru: "не подключено – настройка сохраняется",
        uk: "не підключено – налаштування збережено",
        tr: "bağlı değil – ayar korunuyor",
        cs: "nepřipojeno – nastavení zůstává uloženo",
        sv: "inte ansluten – inställningen behålls");

    public static string NoAppSelected => T("no application selected",
        de: "keine Anwendung ausgewählt", fr: "aucune application sélectionnée",
        es: "ninguna aplicación seleccionada", it: "nessuna applicazione selezionata",
        pt: "nenhuma aplicação selecionada", nl: "geen toepassing geselecteerd",
        pl: "nie wybrano aplikacji", ru: "приложение не выбрано",
        uk: "програму не вибрано", tr: "uygulama seçilmedi",
        cs: "není vybrána aplikace", sv: "inget program valt");

    public static string RunningWholeSound(double ms) => T(
        $"running – full device sound, about {ms:0} ms",
        de: $"läuft – kompletter Ton, ca. {ms:0} ms",
        fr: $"actif – son complet du périphérique, environ {ms:0} ms",
        es: $"activo – sonido completo del dispositivo, unos {ms:0} ms",
        it: $"attivo – audio completo del dispositivo, circa {ms:0} ms",
        pt: $"ativo – som completo do dispositivo, cerca de {ms:0} ms",
        nl: $"actief – volledig apparaatgeluid, ongeveer {ms:0} ms",
        pl: $"działa – pełny dźwięk urządzenia, około {ms:0} ms",
        ru: $"работает – весь звук устройства, около {ms:0} мс",
        uk: $"працює – весь звук пристрою, близько {ms:0} мс",
        tr: $"çalışıyor – aygıtın tüm sesi, yaklaşık {ms:0} ms",
        cs: $"běží – kompletní zvuk zařízení, asi {ms:0} ms",
        sv: $"aktiv – hela enhetens ljud, cirka {ms:0} ms");

    public static string RunningApps(int count, double ms) => T(
        $"running – {count} application(s), about {ms:0} ms",
        de: $"läuft – {count} Anwendung(en), ca. {ms:0} ms",
        fr: $"actif – {count} application(s), environ {ms:0} ms",
        es: $"activo – {count} aplicación(es), unos {ms:0} ms",
        it: $"attivo – {count} applicazione/i, circa {ms:0} ms",
        pt: $"ativo – {count} aplicação(ões), cerca de {ms:0} ms",
        nl: $"actief – {count} toepassing(en), ongeveer {ms:0} ms",
        pl: $"działa – aplikacje: {count}, około {ms:0} ms",
        ru: $"работает – приложений: {count}, около {ms:0} мс",
        uk: $"працює – програм: {count}, близько {ms:0} мс",
        tr: $"çalışıyor – {count} uygulama, yaklaşık {ms:0} ms",
        cs: $"běží – aplikací: {count}, asi {ms:0} ms",
        sv: $"aktiv – {count} program, cirka {ms:0} ms");

    // Fehlermeldungen
    public static string UnexpectedError(string message) => T(
        $"Unexpected error:\r\n\r\n{message}\r\n\r\nMirroring has stopped. Please restart the program.",
        de: $"Unerwarteter Fehler:\r\n\r\n{message}\r\n\r\nDie Spiegelung wurde gestoppt. Bitte das Programm neu starten.",
        fr: $"Erreur inattendue :\r\n\r\n{message}\r\n\r\nLa duplication s'est arrêtée. Veuillez redémarrer le programme.",
        es: $"Error inesperado:\r\n\r\n{message}\r\n\r\nLa duplicación se ha detenido. Reinicia el programa.",
        it: $"Errore imprevisto:\r\n\r\n{message}\r\n\r\nLa duplicazione si è fermata. Riavvia il programma.",
        pt: $"Erro inesperado:\r\n\r\n{message}\r\n\r\nA duplicação parou. Reinicie o programa.",
        nl: $"Onverwachte fout:\r\n\r\n{message}\r\n\r\nHet spiegelen is gestopt. Start het programma opnieuw.",
        pl: $"Nieoczekiwany błąd:\r\n\r\n{message}\r\n\r\nPowielanie zostało zatrzymane. Uruchom program ponownie.",
        ru: $"Непредвиденная ошибка:\r\n\r\n{message}\r\n\r\nДублирование остановлено. Перезапустите программу.",
        uk: $"Неочікувана помилка:\r\n\r\n{message}\r\n\r\nДублювання зупинено. Перезапустіть програму.",
        tr: $"Beklenmeyen hata:\r\n\r\n{message}\r\n\r\nYansıtma durdu. Lütfen programı yeniden başlatın.",
        cs: $"Neočekávaná chyba:\r\n\r\n{message}\r\n\r\nZrcadlení se zastavilo. Restartujte prosím program.",
        sv: $"Oväntat fel:\r\n\r\n{message}\r\n\r\nSpeglingen har stoppats. Starta om programmet.");

    public static string Unknown => T("Unknown",
        de: "Unbekannt", fr: "Inconnu", es: "Desconocido", it: "Sconosciuto", pt: "Desconhecido",
        nl: "Onbekend", pl: "Nieznany", ru: "Неизвестно", uk: "Невідомо", tr: "Bilinmiyor",
        cs: "Neznámé", sv: "Okänt");

    public static string UnknownDevice => T("Unknown device",
        de: "Unbekanntes Gerät", fr: "Périphérique inconnu", es: "Dispositivo desconocido",
        it: "Dispositivo sconosciuto", pt: "Dispositivo desconhecido", nl: "Onbekend apparaat",
        pl: "Nieznane urządzenie", ru: "Неизвестное устройство", uk: "Невідомий пристрій",
        tr: "Bilinmeyen aygıt", cs: "Neznámé zařízení", sv: "Okänd enhet");

    public static string UnknownError => T("Unknown error.",
        de: "Unbekannter Fehler.", fr: "Erreur inconnue.", es: "Error desconocido.",
        it: "Errore sconosciuto.", pt: "Erro desconhecido.", nl: "Onbekende fout.",
        pl: "Nieznany błąd.", ru: "Неизвестная ошибка.", uk: "Невідома помилка.",
        tr: "Bilinmeyen hata.", cs: "Neznámá chyba.", sv: "Okänt fel.");

    public static string DeviceExclusive => T("Device is used exclusively by another application.",
        de: "Gerät ist exklusiv von einer anderen Anwendung belegt.",
        fr: "Le périphérique est utilisé en mode exclusif par une autre application.",
        es: "Otra aplicación está usando el dispositivo en modo exclusivo.",
        it: "Il dispositivo è usato in modo esclusivo da un'altra applicazione.",
        pt: "O dispositivo está a ser usado em modo exclusivo por outra aplicação.",
        nl: "Het apparaat wordt exclusief door een andere toepassing gebruikt.",
        pl: "Urządzenie jest używane wyłącznie przez inną aplikację.",
        ru: "Устройство занято другим приложением в монопольном режиме.",
        uk: "Пристрій зайнято іншою програмою в монопольному режимі.",
        tr: "Aygıt başka bir uygulama tarafından özel modda kullanılıyor.",
        cs: "Zařízení výhradně používá jiná aplikace.",
        sv: "Enheten används exklusivt av ett annat program.");

    public static string DeviceGone => T("Device is no longer available (disconnected or disabled).",
        de: "Gerät ist nicht mehr verfügbar (getrennt oder deaktiviert).",
        fr: "Le périphérique n'est plus disponible (débranché ou désactivé).",
        es: "El dispositivo ya no está disponible (desconectado o deshabilitado).",
        it: "Il dispositivo non è più disponibile (scollegato o disattivato).",
        pt: "O dispositivo já não está disponível (desconectado ou desativado).",
        nl: "Het apparaat is niet meer beschikbaar (losgekoppeld of uitgeschakeld).",
        pl: "Urządzenie nie jest już dostępne (odłączone lub wyłączone).",
        ru: "Устройство больше недоступно (отключено или выключено).",
        uk: "Пристрій більше недоступний (від'єднано або вимкнено).",
        tr: "Aygıt artık kullanılamıyor (çıkarıldı veya devre dışı).",
        cs: "Zařízení už není dostupné (odpojeno nebo zakázáno).",
        sv: "Enheten är inte längre tillgänglig (frånkopplad eller inaktiverad).");

    public static string FormatUnsupported => T("This device does not support the audio format.",
        de: "Audioformat wird von diesem Gerät nicht unterstützt.",
        fr: "Ce périphérique ne prend pas en charge le format audio.",
        es: "Este dispositivo no admite el formato de audio.",
        it: "Questo dispositivo non supporta il formato audio.",
        pt: "Este dispositivo não suporta o formato de áudio.",
        nl: "Dit apparaat ondersteunt de audio-indeling niet.",
        pl: "To urządzenie nie obsługuje tego formatu dźwięku.",
        ru: "Устройство не поддерживает этот аудиоформат.",
        uk: "Пристрій не підтримує цей аудіоформат.",
        tr: "Bu aygıt ses biçimini desteklemiyor.",
        cs: "Toto zařízení nepodporuje daný formát zvuku.",
        sv: "Enheten stöder inte ljudformatet.");

    public static string DeviceInUse => T("Device is already in use by this application.",
        de: "Gerät wird bereits von dieser Anwendung verwendet.",
        fr: "Le périphérique est déjà utilisé par cette application.",
        es: "Esta aplicación ya está usando el dispositivo.",
        it: "Il dispositivo è già in uso da questa applicazione.",
        pt: "O dispositivo já está a ser usado por esta aplicação.",
        nl: "Het apparaat wordt al door deze toepassing gebruikt.",
        pl: "Urządzenie jest już używane przez tę aplikację.",
        ru: "Устройство уже используется этим приложением.",
        uk: "Пристрій уже використовується цією програмою.",
        tr: "Aygıt zaten bu uygulama tarafından kullanılıyor.",
        cs: "Zařízení už tato aplikace používá.",
        sv: "Enheten används redan av det här programmet.");

    public static string AudioServiceDown => T("The Windows audio service is not running.",
        de: "Windows-Audiodienst läuft nicht.",
        fr: "Le service audio de Windows n'est pas démarré.",
        es: "El servicio de audio de Windows no se está ejecutando.",
        it: "Il servizio audio di Windows non è in esecuzione.",
        pt: "O serviço de áudio do Windows não está em execução.",
        nl: "De audioservice van Windows draait niet.",
        pl: "Usługa audio systemu Windows nie działa.",
        ru: "Служба звука Windows не запущена.",
        uk: "Службу звуку Windows не запущено.",
        tr: "Windows ses hizmeti çalışmıyor.",
        cs: "Zvuková služba systému Windows neběží.",
        sv: "Windows ljudtjänst körs inte.");

    public static string AccessDenied => T("Access to the device was denied.",
        de: "Zugriff auf das Gerät verweigert.", fr: "L'accès au périphérique a été refusé.",
        es: "Se ha denegado el acceso al dispositivo.",
        it: "Accesso al dispositivo negato.", pt: "O acesso ao dispositivo foi negado.",
        nl: "Toegang tot het apparaat is geweigerd.", pl: "Odmówiono dostępu do urządzenia.",
        ru: "Доступ к устройству запрещён.", uk: "Доступ до пристрою заборонено.",
        tr: "Aygıta erişim reddedildi.", cs: "Přístup k zařízení byl odepřen.",
        sv: "Åtkomst till enheten nekades.");

    public static string PlaybackStopped => T("Playback was stopped by the system.",
        de: "Wiedergabe wurde vom System beendet.",
        fr: "La lecture a été arrêtée par le système.",
        es: "El sistema ha detenido la reproducción.",
        it: "La riproduzione è stata interrotta dal sistema.",
        pt: "A reprodução foi parada pelo sistema.",
        nl: "Het afspelen is door het systeem gestopt.",
        pl: "System zatrzymał odtwarzanie.",
        ru: "Воспроизведение остановлено системой.",
        uk: "Відтворення зупинено системою.",
        tr: "Oynatma sistem tarafından durduruldu.",
        cs: "Přehrávání zastavil systém.",
        sv: "Uppspelningen stoppades av systemet.");

    public static string AppNotRunning => T("Application is not running.",
        de: "Anwendung läuft nicht.", fr: "L'application n'est pas en cours d'exécution.",
        es: "La aplicación no se está ejecutando.", it: "L'applicazione non è in esecuzione.",
        pt: "A aplicação não está em execução.", nl: "De toepassing draait niet.",
        pl: "Aplikacja nie działa.", ru: "Приложение не запущено.",
        uk: "Програму не запущено.", tr: "Uygulama çalışmıyor.",
        cs: "Aplikace neběží.", sv: "Programmet körs inte.");

    public static string AppSilent => T("Application is not playing any sound right now.",
        de: "Anwendung gibt gerade keinen Ton aus.",
        fr: "L'application ne joue aucun son actuellement.",
        es: "La aplicación no está reproduciendo sonido ahora mismo.",
        it: "L'applicazione al momento non riproduce audio.",
        pt: "A aplicação não está a reproduzir som neste momento.",
        nl: "De toepassing speelt op dit moment geen geluid af.",
        pl: "Aplikacja nie odtwarza teraz dźwięku.",
        ru: "Сейчас приложение не воспроизводит звук.",
        uk: "Зараз програма не відтворює звук.",
        tr: "Uygulama şu anda ses çalmıyor.",
        cs: "Aplikace teď nepřehrává zvuk.",
        sv: "Programmet spelar inte upp något ljud just nu.");

    public static string CaptureFailed(string message) => T(
        "Sound cannot be captured: " + message,
        de: "Ton nicht abgreifbar: " + message,
        fr: "Impossible de capturer le son : " + message,
        es: "No se puede capturar el sonido: " + message,
        it: "Impossibile catturare l'audio: " + message,
        pt: "Não é possível capturar o som: " + message,
        nl: "Geluid kan niet worden opgevangen: " + message,
        pl: "Nie można przechwycić dźwięku: " + message,
        ru: "Не удаётся захватить звук: " + message,
        uk: "Не вдається захопити звук: " + message,
        tr: "Ses yakalanamıyor: " + message,
        cs: "Zvuk nelze zachytit: " + message,
        sv: "Ljudet kan inte fångas: " + message);

    public static string NoSourceDevice => T("No source device available.",
        de: "Kein Quellgerät verfügbar.", fr: "Aucun périphérique source disponible.",
        es: "No hay ningún dispositivo de origen disponible.",
        it: "Nessun dispositivo sorgente disponibile.",
        pt: "Nenhum dispositivo de origem disponível.",
        nl: "Geen bronapparaat beschikbaar.", pl: "Brak dostępnego urządzenia źródłowego.",
        ru: "Нет доступного устройства-источника.",
        uk: "Немає доступного пристрою-джерела.", tr: "Kullanılabilir kaynak aygıt yok.",
        cs: "Není dostupné žádné zdrojové zařízení.", sv: "Ingen källenhet tillgänglig.");

    public static string SourceCaptureFailed(string message) => T(
        "Could not start capturing the source device: " + message,
        de: "Die Aufnahme am Quellgerät konnte nicht gestartet werden: " + message,
        fr: "Impossible de démarrer la capture du périphérique source : " + message,
        es: "No se ha podido iniciar la captura del dispositivo de origen: " + message,
        it: "Impossibile avviare la cattura dal dispositivo sorgente: " + message,
        pt: "Não foi possível iniciar a captura do dispositivo de origem: " + message,
        nl: "Het opvangen van het bronapparaat kon niet worden gestart: " + message,
        pl: "Nie udało się rozpocząć przechwytywania urządzenia źródłowego: " + message,
        ru: "Не удалось начать захват с устройства-источника: " + message,
        uk: "Не вдалося почати захоплення з пристрою-джерела: " + message,
        tr: "Kaynak aygıttan yakalama başlatılamadı: " + message,
        cs: "Nepodařilo se spustit zachytávání ze zdrojového zařízení: " + message,
        sv: "Det gick inte att starta inspelningen från källenheten: " + message);

    public static string SourceEnded => T("The audio source was stopped.",
        de: "Die Tonquelle wurde beendet.", fr: "La source audio a été arrêtée.",
        es: "La fuente de audio se ha detenido.", it: "La sorgente audio è stata interrotta.",
        pt: "A fonte de áudio foi parada.", nl: "De geluidsbron is gestopt.",
        pl: "Źródło dźwięku zostało zatrzymane.", ru: "Источник звука остановлен.",
        uk: "Джерело звуку зупинено.", tr: "Ses kaynağı durduruldu.",
        cs: "Zdroj zvuku byl zastaven.", sv: "Ljudkällan stoppades.");

    // Autostart-Fehler
    public static string ExecutablePathUnknown => T("The program path could not be determined.",
        de: "Der Programmpfad konnte nicht ermittelt werden.",
        fr: "Le chemin du programme n'a pas pu être déterminé.",
        es: "No se ha podido determinar la ruta del programa.",
        it: "Non è stato possibile determinare il percorso del programma.",
        pt: "Não foi possível determinar o caminho do programa.",
        nl: "Het pad van het programma kon niet worden bepaald.",
        pl: "Nie udało się ustalić ścieżki programu.",
        ru: "Не удалось определить путь к программе.",
        uk: "Не вдалося визначити шлях до програми.",
        tr: "Program yolu belirlenemedi.",
        cs: "Cestu k programu se nepodařilo zjistit.",
        sv: "Programmets sökväg kunde inte fastställas.");

    public static string RunKeyUnavailable => T("The startup key in the registry is not accessible.",
        de: "Der Autostart-Schlüssel in der Registry ist nicht zugänglich.",
        fr: "La clé de démarrage dans le registre est inaccessible.",
        es: "No se puede acceder a la clave de inicio del registro.",
        it: "La chiave di avvio nel registro non è accessibile.",
        pt: "A chave de arranque no registo não está acessível.",
        nl: "De opstartsleutel in het register is niet toegankelijk.",
        pl: "Klucz autostartu w rejestrze jest niedostępny.",
        ru: "Раздел автозапуска в реестре недоступен.",
        uk: "Розділ автозапуску в реєстрі недоступний.",
        tr: "Kayıt defterindeki başlangıç anahtarına erişilemiyor.",
        cs: "Klíč po spuštění v registru není přístupný.",
        sv: "Startnyckeln i registret är inte åtkomlig.");

    public static string AutostartDenied => T(
        "Not allowed to change the startup entry (possibly blocked by a group policy).",
        de: "Keine Berechtigung, den Autostart zu ändern (evtl. durch eine Gruppenrichtlinie gesperrt).",
        fr: "Modification de l'entrée de démarrage non autorisée (peut-être bloquée par une stratégie de groupe).",
        es: "No se permite cambiar la entrada de inicio (quizá bloqueada por una directiva de grupo).",
        it: "Non è consentito modificare la voce di avvio (forse bloccata da criteri di gruppo).",
        pt: "Não é permitido alterar a entrada de arranque (talvez bloqueada por uma política de grupo).",
        nl: "Wijzigen van de opstartvermelding is niet toegestaan (mogelijk geblokkeerd door groepsbeleid).",
        pl: "Brak uprawnień do zmiany wpisu autostartu (być może blokuje go zasada grupy).",
        ru: "Нет прав на изменение записи автозапуска (возможно, запрещено групповой политикой).",
        uk: "Немає прав на зміну запису автозапуску (можливо, заборонено груповою політикою).",
        tr: "Başlangıç girdisi değiştirilemiyor (grup ilkesiyle engellenmiş olabilir).",
        cs: "Není oprávnění změnit položku po spuštění (možná ji blokují zásady skupiny).",
        sv: "Det är inte tillåtet att ändra startposten (kan vara blockerat av en grupprincip).");

    public static string AutostartFailed(string message) => T(
        "Could not change the startup entry: " + message,
        de: "Autostart konnte nicht geändert werden: " + message,
        fr: "Impossible de modifier l'entrée de démarrage : " + message,
        es: "No se ha podido cambiar la entrada de inicio: " + message,
        it: "Impossibile modificare la voce di avvio: " + message,
        pt: "Não foi possível alterar a entrada de arranque: " + message,
        nl: "De opstartvermelding kon niet worden gewijzigd: " + message,
        pl: "Nie udało się zmienić wpisu autostartu: " + message,
        ru: "Не удалось изменить запись автозапуска: " + message,
        uk: "Не вдалося змінити запис автозапуску: " + message,
        tr: "Başlangıç girdisi değiştirilemedi: " + message,
        cs: "Položku po spuštění se nepodařilo změnit: " + message,
        sv: "Startposten kunde inte ändras: " + message);

    // Einstellungen
    public static string TabDevices => T("Devices",
        de: "Geräte", fr: "Périphériques", es: "Dispositivos", it: "Dispositivi",
        pt: "Dispositivos", nl: "Apparaten", pl: "Urządzenia", ru: "Устройства",
        uk: "Пристрої", tr: "Aygıtlar", cs: "Zařízení", sv: "Enheter");

    public static string TabSettings => T("Settings",
        de: "Einstellungen", fr: "Paramètres", es: "Ajustes", it: "Impostazioni",
        pt: "Definições", nl: "Instellingen", pl: "Ustawienia", ru: "Параметры",
        uk: "Параметри", tr: "Ayarlar", cs: "Nastavení", sv: "Inställningar");

    public static string BasicSettings => T("Basic settings",
        de: "Allgemein", fr: "Général", es: "General", it: "Generale", pt: "Geral",
        nl: "Algemeen", pl: "Ogólne", ru: "Общие", uk: "Загальні", tr: "Genel",
        cs: "Obecné", sv: "Allmänt");

    public static string AudioSettings => T("Audio",
        de: "Ton", fr: "Audio", es: "Audio", it: "Audio", pt: "Áudio", nl: "Geluid",
        pl: "Dźwięk", ru: "Звук", uk: "Звук", tr: "Ses", cs: "Zvuk", sv: "Ljud");

    public static string UpdateSettings => T("Updates",
        de: "Aktualisierungen", fr: "Mises à jour", es: "Actualizaciones",
        it: "Aggiornamenti", pt: "Atualizações", nl: "Updates", pl: "Aktualizacje",
        ru: "Обновления", uk: "Оновлення", tr: "Güncellemeler", cs: "Aktualizace",
        sv: "Uppdateringar");

    public static string LanguageLabel => T("Language",
        de: "Sprache", fr: "Langue", es: "Idioma", it: "Lingua", pt: "Idioma", nl: "Taal",
        pl: "Język", ru: "Язык", uk: "Мова", tr: "Dil", cs: "Jazyk", sv: "Språk");

    public static string DoubleClickLabel => T("Double-click action",
        de: "Doppelklick", fr: "Double-clic", es: "Doble clic", it: "Doppio clic",
        pt: "Duplo clique", nl: "Dubbelklik", pl: "Dwukliknięcie", ru: "Двойной щелчок",
        uk: "Подвійний клац", tr: "Çift tıklama", cs: "Dvojklik", sv: "Dubbelklick");

    public static string ActionOpenWindow => T("Open window",
        de: "Fenster öffnen", fr: "Ouvrir la fenêtre", es: "Abrir ventana",
        it: "Apri finestra", pt: "Abrir janela", nl: "Venster openen", pl: "Otwórz okno",
        ru: "Открыть окно", uk: "Відкрити вікно", tr: "Pencereyi aç", cs: "Otevřít okno",
        sv: "Öppna fönstret");

    public static string ActionToggle => T("Toggle mirroring",
        de: "Spiegelung umschalten", fr: "Basculer la duplication",
        es: "Alternar la duplicación", it: "Attiva/disattiva la duplicazione",
        pt: "Alternar a duplicação", nl: "Spiegelen omschakelen",
        pl: "Przełącz powielanie", ru: "Переключить дублирование",
        uk: "Перемкнути дублювання", tr: "Yansıtmayı değiştir",
        cs: "Přepnout zrcadlení", sv: "Växla spegling");

    public static string ActionNothing => T("Do nothing",
        de: "Nichts tun", fr: "Ne rien faire", es: "No hacer nada", it: "Non fare nulla",
        pt: "Não fazer nada", nl: "Niets doen", pl: "Nic nie rób", ru: "Ничего не делать",
        uk: "Нічого не робити", tr: "Hiçbir şey yapma", cs: "Nedělat nic",
        sv: "Gör ingenting");

    public static string RestartForLanguage => T("The language changes after restarting the program.",
        de: "Die Sprache wird nach einem Neustart des Programms übernommen.",
        fr: "La langue change après le redémarrage du programme.",
        es: "El idioma cambia después de reiniciar el programa.",
        it: "La lingua cambia dopo il riavvio del programma.",
        pt: "O idioma muda depois de reiniciar o programa.",
        nl: "De taal verandert na het opnieuw starten van het programma.",
        pl: "Język zmieni się po ponownym uruchomieniu programu.",
        ru: "Язык изменится после перезапуска программы.",
        uk: "Мова зміниться після перезапуску програми.",
        tr: "Dil, program yeniden başlatıldıktan sonra değişir.",
        cs: "Jazyk se změní po restartu programu.",
        sv: "Språket ändras efter omstart av programmet.");

    public static string UpdateAutomatic => T("Install updates automatically",
        de: "Aktualisierungen automatisch installieren",
        fr: "Installer les mises à jour automatiquement",
        es: "Instalar las actualizaciones automáticamente",
        it: "Installa gli aggiornamenti automaticamente",
        pt: "Instalar atualizações automaticamente",
        nl: "Updates automatisch installeren",
        pl: "Instaluj aktualizacje automatycznie",
        ru: "Устанавливать обновления автоматически",
        uk: "Встановлювати оновлення автоматично",
        tr: "Güncellemeleri otomatik yükle",
        cs: "Instalovat aktualizace automaticky",
        sv: "Installera uppdateringar automatiskt");

    public static string UpdateNotify => T("Notify me when updates are available",
        de: "Nur benachrichtigen", fr: "Me prévenir seulement", es: "Solo avisarme",
        it: "Avvisami soltanto", pt: "Apenas avisar", nl: "Alleen melden",
        pl: "Tylko powiadamiaj", ru: "Только уведомлять", uk: "Лише сповіщати",
        tr: "Yalnızca bildir", cs: "Pouze upozornit", sv: "Meddela bara");

    public static string UpdateNever => T("Never check for updates",
        de: "Nie nach Aktualisierungen suchen",
        fr: "Ne jamais rechercher de mises à jour",
        es: "No buscar actualizaciones nunca",
        it: "Non cercare mai aggiornamenti",
        pt: "Nunca procurar atualizações",
        nl: "Nooit op updates controleren",
        pl: "Nigdy nie sprawdzaj aktualizacji",
        ru: "Никогда не проверять обновления",
        uk: "Ніколи не перевіряти оновлення",
        tr: "Güncellemeleri hiç arama",
        cs: "Nikdy nekontrolovat aktualizace",
        sv: "Sök aldrig efter uppdateringar");

    public static string CheckNow => T("Check now",
        de: "Jetzt suchen", fr: "Rechercher maintenant", es: "Buscar ahora",
        it: "Cerca ora", pt: "Procurar agora", nl: "Nu controleren", pl: "Sprawdź teraz",
        ru: "Проверить сейчас", uk: "Перевірити зараз", tr: "Şimdi ara",
        cs: "Zkontrolovat nyní", sv: "Sök nu");

    public static string CheckingUpdates => T("Checking …",
        de: "Suche läuft …", fr: "Recherche en cours …", es: "Buscando …",
        it: "Ricerca in corso …", pt: "A procurar …", nl: "Bezig met controleren …",
        pl: "Sprawdzanie …", ru: "Проверка …", uk: "Перевірка …", tr: "Aranıyor …",
        cs: "Kontroluje se …", sv: "Söker …");

    public static string UpToDate => T("Audio Mirror is up to date.",
        de: "Audio Mirror ist aktuell.", fr: "Audio Mirror est à jour.",
        es: "Audio Mirror está actualizado.", it: "Audio Mirror è aggiornato.",
        pt: "O Audio Mirror está atualizado.", nl: "Audio Mirror is up-to-date.",
        pl: "Audio Mirror jest aktualny.", ru: "Audio Mirror обновлён.",
        uk: "Audio Mirror оновлено.", tr: "Audio Mirror güncel.",
        cs: "Audio Mirror je aktuální.", sv: "Audio Mirror är uppdaterad.");

    public static string UpdateAvailable(string version) => T(
        $"Version {version} is available.",
        de: $"Fassung {version} ist verfügbar.",
        fr: $"La version {version} est disponible.",
        es: $"La versión {version} está disponible.",
        it: $"La versione {version} è disponibile.",
        pt: $"A versão {version} está disponível.",
        nl: $"Versie {version} is beschikbaar.",
        pl: $"Dostępna jest wersja {version}.",
        ru: $"Доступна версия {version}.",
        uk: $"Доступна версія {version}.",
        tr: $"Sürüm {version} mevcut.",
        cs: $"Je dostupná verze {version}.",
        sv: $"Version {version} finns tillgänglig.");

    public static string UpdateDownloading(string version) => T(
        $"Downloading version {version} …",
        de: $"Fassung {version} wird geladen …",
        fr: $"Téléchargement de la version {version} …",
        es: $"Descargando la versión {version} …",
        it: $"Download della versione {version} …",
        pt: $"A transferir a versão {version} …",
        nl: $"Versie {version} wordt gedownload …",
        pl: $"Pobieranie wersji {version} …",
        ru: $"Загрузка версии {version} …",
        uk: $"Завантаження версії {version} …",
        tr: $"Sürüm {version} indiriliyor …",
        cs: $"Stahuje se verze {version} …",
        sv: $"Hämtar version {version} …");

    public static string UpdatePrompt(string version, string current) => T(
        $"Audio Mirror {version} is available - you have {current}."
        + Environment.NewLine + Environment.NewLine
        + "Download and install it now? Audio Mirror closes for the installation "
        + "and your settings are kept.",
        de: $"Audio Mirror {version} ist verfügbar - installiert ist {current}."
        + Environment.NewLine + Environment.NewLine
        + "Jetzt herunterladen und installieren? Audio Mirror wird dafür beendet, "
        + "die Einstellungen bleiben erhalten.",
        fr: $"Audio Mirror {version} est disponible - vous avez {current}."
        + Environment.NewLine + Environment.NewLine
        + "Le télécharger et l'installer maintenant ? Audio Mirror se ferme pour "
        + "l'installation et vos réglages sont conservés.",
        es: $"Audio Mirror {version} está disponible; tienes la {current}."
        + Environment.NewLine + Environment.NewLine
        + "¿Descargarla e instalarla ahora? Audio Mirror se cerrará para la instalación "
        + "y tus ajustes se conservan.",
        it: $"Audio Mirror {version} è disponibile - hai la {current}."
        + Environment.NewLine + Environment.NewLine
        + "Scaricarla e installarla ora? Audio Mirror si chiude per l'installazione "
        + "e le impostazioni vengono mantenute.",
        pt: $"O Audio Mirror {version} está disponível - tem a {current}."
        + Environment.NewLine + Environment.NewLine
        + "Transferir e instalar agora? O Audio Mirror fecha para a instalação "
        + "e as suas definições são mantidas.",
        nl: $"Audio Mirror {version} is beschikbaar - u hebt {current}."
        + Environment.NewLine + Environment.NewLine
        + "Nu downloaden en installeren? Audio Mirror sluit voor de installatie "
        + "en uw instellingen blijven behouden.",
        pl: $"Audio Mirror {version} jest dostępny - masz {current}."
        + Environment.NewLine + Environment.NewLine
        + "Pobrać i zainstalować teraz? Audio Mirror zamknie się na czas instalacji, "
        + "a ustawienia zostaną zachowane.",
        ru: $"Доступен Audio Mirror {version} - установлена {current}."
        + Environment.NewLine + Environment.NewLine
        + "Скачать и установить сейчас? Audio Mirror закроется на время установки, "
        + "настройки сохранятся.",
        uk: $"Доступний Audio Mirror {version} - встановлено {current}."
        + Environment.NewLine + Environment.NewLine
        + "Завантажити та встановити зараз? Audio Mirror закриється на час встановлення, "
        + "налаштування збережуться.",
        tr: $"Audio Mirror {version} mevcut - sizde {current} var."
        + Environment.NewLine + Environment.NewLine
        + "Şimdi indirilip kurulsun mu? Audio Mirror kurulum için kapanır, "
        + "ayarlarınız korunur.",
        cs: $"Je dostupný Audio Mirror {version} - máte {current}."
        + Environment.NewLine + Environment.NewLine
        + "Stáhnout a nainstalovat nyní? Audio Mirror se kvůli instalaci zavře "
        + "a nastavení zůstanou zachována.",
        sv: $"Audio Mirror {version} finns tillgänglig - du har {current}."
        + Environment.NewLine + Environment.NewLine
        + "Vill du hämta och installera den nu? Audio Mirror stängs under installationen "
        + "och dina inställningar behålls.");

    public static string UpdateStarting => T("Starting the installation …",
        de: "Installation wird gestartet …", fr: "Démarrage de l'installation …",
        es: "Iniciando la instalación …", it: "Avvio dell'installazione …",
        pt: "A iniciar a instalação …", nl: "Installatie wordt gestart …",
        pl: "Uruchamianie instalacji …", ru: "Запуск установки …",
        uk: "Запуск встановлення …", tr: "Kurulum başlatılıyor …",
        cs: "Spouští se instalace …", sv: "Startar installationen …");

    public static string UpdateNoSetup => T("This release has no installer - opening the download page.",
        de: "Zu dieser Fassung gibt es kein Setup - die Download-Seite wird geöffnet.",
        fr: "Cette version n'a pas de programme d'installation - la page de téléchargement s'ouvre.",
        es: "Esta versión no tiene instalador; se abrirá la página de descarga.",
        it: "Questa versione non ha un programma di installazione: si apre la pagina di download.",
        pt: "Esta versão não tem instalador - a página de transferência será aberta.",
        nl: "Deze versie heeft geen installatieprogramma - de downloadpagina wordt geopend.",
        pl: "Ta wersja nie ma instalatora - zostanie otwarta strona pobierania.",
        ru: "У этой версии нет установщика - откроется страница загрузки.",
        uk: "У цієї версії немає інсталятора - відкриється сторінка завантаження.",
        tr: "Bu sürümün kurulum programı yok - indirme sayfası açılıyor.",
        cs: "Tato verze nemá instalátor - otevře se stránka ke stažení.",
        sv: "Den här versionen saknar installationsprogram - hämtningssidan öppnas.");

    public static string UpdateDownloadFailed => T("The download failed. Opening the release page instead.",
        de: "Der Download ist fehlgeschlagen. Stattdessen wird die Release-Seite geöffnet.",
        fr: "Le téléchargement a échoué. La page de la version s'ouvre à la place.",
        es: "La descarga ha fallado. Se abrirá la página de la versión.",
        it: "Il download non è riuscito. Si apre invece la pagina della versione.",
        pt: "A transferência falhou. Será aberta a página da versão.",
        nl: "De download is mislukt. In plaats daarvan wordt de releasepagina geopend.",
        pl: "Pobieranie nie powiodło się. Zamiast tego otwarta zostanie strona wydania.",
        ru: "Загрузка не удалась. Вместо этого откроется страница выпуска.",
        uk: "Завантаження не вдалося. Замість цього відкриється сторінка випуску.",
        tr: "İndirme başarısız oldu. Bunun yerine sürüm sayfası açılıyor.",
        cs: "Stahování selhalo. Místo toho se otevře stránka vydání.",
        sv: "Hämtningen misslyckades. Släppsidan öppnas i stället.");

    public static string UpdatedTo(string version) => T(
        $"Updated to version {version}.",
        de: $"Auf Fassung {version} aktualisiert.",
        fr: $"Mis à jour vers la version {version}.",
        es: $"Actualizado a la versión {version}.",
        it: $"Aggiornato alla versione {version}.",
        pt: $"Atualizado para a versão {version}.",
        nl: $"Bijgewerkt naar versie {version}.",
        pl: $"Zaktualizowano do wersji {version}.",
        ru: $"Обновлено до версии {version}.",
        uk: $"Оновлено до версії {version}.",
        tr: $"Sürüm {version} sürümüne güncellendi.",
        cs: $"Aktualizováno na verzi {version}.",
        sv: $"Uppdaterad till version {version}.");

    public static string CurrentVersion(string version) => T(
        $"Installed version: {version}",
        de: $"Installierte Fassung: {version}",
        fr: $"Version installée : {version}",
        es: $"Versión instalada: {version}",
        it: $"Versione installata: {version}",
        pt: $"Versão instalada: {version}",
        nl: $"Geïnstalleerde versie: {version}",
        pl: $"Zainstalowana wersja: {version}",
        ru: $"Установленная версия: {version}",
        uk: $"Встановлена версія: {version}",
        tr: $"Yüklü sürüm: {version}",
        cs: $"Nainstalovaná verze: {version}",
        sv: $"Installerad version: {version}");

    // Tastennamen für die Hotkey-Anzeige
    public static string KeyControl => T("Ctrl",
        de: "Strg", fr: "Ctrl", es: "Ctrl", it: "Ctrl", pt: "Ctrl", nl: "Ctrl", pl: "Ctrl",
        ru: "Ctrl", uk: "Ctrl", tr: "Ctrl", cs: "Ctrl", sv: "Ctrl");

    public static string KeyShift => T("Shift",
        de: "Umschalt", fr: "Maj", es: "Mayús", it: "Maiusc", pt: "Shift", nl: "Shift",
        pl: "Shift", ru: "Shift", uk: "Shift", tr: "Shift", cs: "Shift", sv: "Skift");

    public static string KeyAlt => "Alt";

    public static string KeySpace => T("Space",
        de: "Leertaste", fr: "Espace", es: "Espacio", it: "Spazio", pt: "Espaço",
        nl: "Spatie", pl: "Spacja", ru: "Пробел", uk: "Пробіл", tr: "Boşluk",
        cs: "Mezerník", sv: "Blanksteg");

    public static string KeyPageUp => T("Page up",
        de: "Bild auf", fr: "Page préc.", es: "Re Pág", it: "Pag su", pt: "Page Up",
        nl: "Page Up", pl: "Page Up", ru: "Page Up", uk: "Page Up", tr: "Page Up",
        cs: "Page Up", sv: "Page Up");

    public static string KeyPageDown => T("Page down",
        de: "Bild ab", fr: "Page suiv.", es: "Av Pág", it: "Pag giù", pt: "Page Down",
        nl: "Page Down", pl: "Page Down", ru: "Page Down", uk: "Page Down", tr: "Page Down",
        cs: "Page Down", sv: "Page Down");

    public static string KeyNumPad => "Num ";
}
