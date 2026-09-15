namespace CableTest.Core.Configuration;

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

    /// <summary>Podrazumevana putanja rezultata.</summary>
    public const string DefaultResultPath = @"C:\Cable Linker8761";

    /// <summary>Kopija, da izmena u ekranu Podešavanja ne dira podešavanja koja su u upotrebi.</summary>
    public AppSettings Clone() => new()
    {
        SpecFolder = SpecFolder,
        ResultPath = ResultPath,
        OperatorName = OperatorName,
        SoundEnabled = SoundEnabled,
        DemoMode = DemoMode,
        TemplatePath = TemplatePath
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
