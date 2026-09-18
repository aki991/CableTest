namespace CableTest.Core.Configuration;

/// <summary>Odakle aplikacija uzima rezultate.</summary>
public enum ResultSource
{
    /// <summary>Iz CSV fajla koji piše CableConnector. Jedini izvor za rad u pogonu.</summary>
    File,

    /// <summary>
    /// Iz pripremljenih fajlova, bez mašine. Isključivo za razvoj — vidi se samo kad je uključen
    /// razvojni prekidač, isti onaj koji otkriva stranu „CableConnector".
    /// </summary>
    Simulation
}

/// <summary>Izgled aplikacije.</summary>
public enum AppTheme
{
    /// <summary>
    /// Tamna („radionička") tema. Podrazumevana: ekran stoji iznad ispitnog stola i gleda se
    /// ceo radni dan, pa tamna podloga manje zamara i ne pravi odsjaj.
    /// </summary>
    Dark,

    /// <summary>
    /// Svetla tema. Za sto uz prozor ili pod jakim plafonskim svetlom, gde tamna podloga
    /// postaje ogledalo.
    /// </summary>
    Light
}

/// <summary>
/// Podešavanja aplikacije, onakva kakva stoje u <c>%APPDATA%\CableTest\settings.json</c>.
/// </summary>
/// <remarks>
/// Svojstva su namerno obična i sa podrazumevanim vrednostima: fajl sa podešavanjima može da
/// nedostaje, da bude star ili nepotpun, i ni u jednom od tih slučajeva aplikacija ne sme da
/// odbije da se pokrene.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>Folder u koji se upisuje generisani .c61.</summary>
    public string SpecFolder { get; set; } = Gateway.TesterGatewayOptions.DefaultSpecFolder;

    /// <summary>
    /// Putanja CSV fajla sa rezultatima, ili foldera u kome CableConnector pravi CSV fajlove.
    /// </summary>
    /// <remarks>
    /// PRETPOSTAVKA: podrazumevana vrednost je folder instalacije CableConnector-a, jer tačno
    /// mesto CSV fajla nije potvrđeno. Kad se potvrdi, menja se ovde. Dok putanja nije ispravna,
    /// traka stanja na glavnom ekranu to jasno kaže — aplikacija o tome ne ćuti.
    /// </remarks>
    public string ResultPath { get; set; } = DefaultResultPath;

    /// <summary>Ime operatera; upisuje se uz rezultate koje sama aplikacija napravi.</summary>
    public string OperatorName { get; set; } = Environment.UserName;

    /// <summary>Da li uz rezultat ide i zvučni signal.</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Demo režim: umesto testera radi <see cref="Gateway.FakeTesterGateway"/>, a na ekranu stoji
    /// vidna traka da ono što se vidi nije stvarni rezultat.
    /// </summary>
    public bool DemoMode { get; set; }

    /// <summary>Putanja šablona MASTER.c61; prazno znači <c>templates\MASTER.c61</c> pored .exe.</summary>
    public string TemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// Da li se u bočnom meniju vidi razvojni alat „CableConnector".
    /// </summary>
    /// <remarks>
    /// Podrazumevano isključen: ta strana upravlja tuđim programom preko UI Automation i nema šta
    /// da radi pred operaterom u pogonu. Uključuje se u Podešavanjima, dok se ispituje šta je od
    /// CableConnector-a uopšte dohvatljivo.
    /// </remarks>
    public bool ShowCableConnectorTool { get; set; }

    /// <summary>
    /// Izvor rezultata: fajl (pogon) ili simulacija (razvoj).
    /// </summary>
    /// <remarks>
    /// Simulacija se u Podešavanjima nudi <b>samo</b> kad je uključen <see cref="ShowCableConnectorTool"/>
    /// — isti razvojni prekidač koji otkriva stranu „CableConnector". Ako u fajlu podešavanja
    /// zatekne simulaciju bez tog prekidača, aplikacija se vraća na fajl: rezultat koji nije
    /// izmeren ne sme da se nađe pred operaterom slučajno.
    /// </remarks>
    public ResultSource ResultSource { get; set; } = ResultSource.File;

    /// <summary>Koliko se najduže čeka rezultat posle pokretanja testa, u sekundama.</summary>
    public int ResultTimeoutSeconds { get; set; } = DefaultResultTimeoutSeconds;

    /// <summary>Folder sa pripremljenim CSV fajlovima za simulaciju; prazno znači bez fajlova.</summary>
    public string SimulationFolder { get; set; } = string.Empty;

    /// <summary>
    /// Tamna ili svetla tema.
    /// </summary>
    /// <remarks>
    /// Menja se u Podešavanjima i važi odmah, bez ponovnog pokretanja — operater temu bira
    /// prema svetlu nad svojim stolom, a ne prema tome kad mu se da da ugasi program.
    /// </remarks>
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    /// <summary>Podrazumevana putanja rezultata.</summary>
    public const string DefaultResultPath = @"C:\Cable Linker8761";

    /// <summary>Podrazumevano čekanje na rezultat, u sekundama.</summary>
    public const int DefaultResultTimeoutSeconds = 120;

    /// <summary>
    /// Izvor rezultata sa kojim se zaista radi.
    /// </summary>
    /// <remarks>
    /// Simulacija važi samo uz uključen razvojni prekidač; bez njega je uvek fajl, bez obzira na
    /// to šta piše u <c>settings.json</c>.
    /// </remarks>
    public ResultSource EffectiveResultSource =>
        ResultSource == ResultSource.Simulation && ShowCableConnectorTool
            ? ResultSource.Simulation
            : ResultSource.File;

    /// <summary>Čekanje na rezultat, svedeno na razumne granice (1 s do 1 h).</summary>
    public TimeSpan ResultTimeout => TimeSpan.FromSeconds(Math.Clamp(ResultTimeoutSeconds, 1, 3600));

    /// <summary>Kopija, da izmena u ekranu Podešavanja ne dira podešavanja koja su u upotrebi.</summary>
    public AppSettings Clone() => new()
    {
        SpecFolder = SpecFolder,
        ResultPath = ResultPath,
        OperatorName = OperatorName,
        SoundEnabled = SoundEnabled,
        DemoMode = DemoMode,
        TemplatePath = TemplatePath,
        ShowCableConnectorTool = ShowCableConnectorTool,
        ResultSource = ResultSource,
        ResultTimeoutSeconds = ResultTimeoutSeconds,
        SimulationFolder = SimulationFolder,
        Theme = Theme
    };

    /// <summary>Podešavanja pretvorena u ono što gateway očekuje.</summary>
    public Gateway.TesterGatewayOptions ToGatewayOptions() => new()
    {
        SpecFolder = SpecFolder,
        ResultPath = ResultPath,
        TemplatePath = string.IsNullOrWhiteSpace(TemplatePath)
            ? Gateway.TesterGatewayOptions.DefaultTemplatePath
            : TemplatePath
    };
}
