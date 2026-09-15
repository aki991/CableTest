namespace CableTest.Core.Gateway;

/// <summary>Podešavanja rada preko fajlova; sve što <see cref="FileBasedTesterGateway"/> zna o disku.</summary>
public sealed class TesterGatewayOptions
{
    /// <summary>Podrazumevani spec folder CableConnector-a.</summary>
    public const string DefaultSpecFolder = @"C:\Cable Linker8761\spec";

    /// <summary>Podrazumevano ime šablona, pored izvršnog fajla.</summary>
    public const string DefaultTemplatePath = @"templates\MASTER.c61";

    /// <summary>Na koliko se proverava veličina i vreme izmene praćenog fajla.</summary>
    /// <remarks>
    /// <see cref="System.IO.FileSystemWatcher"/> je poznat po tome da propušta događaje — kod
    /// mrežnih diskova, kod programa koji pišu preko privremenog fajla pa preimenuju, i kad
    /// događaja bude više nego što stane u bafer. Zato se ne oslanjamo samo na njega: tajmer
    /// svejedno proverava fajl. Watcher je tu da reakcija bude brza, tajmer da ne izostane.
    /// </remarks>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);

    /// <summary>Folder u koji se upisuje generisani .c61.</summary>
    public string SpecFolder { get; set; } = DefaultSpecFolder;

    /// <summary>Putanja šablona MASTER.c61.</summary>
    public string TemplatePath { get; set; } = DefaultTemplatePath;

    /// <summary>
    /// Putanja CSV fajla sa rezultatima, ili foldera u kome CableConnector pravi CSV fajlove.
    /// Ako je folder, prati se najnoviji CSV u njemu (dnevna rotacija po datumu u imenu).
    /// </summary>
    public string ResultPath { get; set; } = string.Empty;

    /// <summary>Period provere fajla tajmerom.</summary>
    public TimeSpan PollInterval { get; set; } = DefaultPollInterval;

    /// <summary>Maska po kojoj se u folderu traži najnoviji fajl sa rezultatima.</summary>
    public string ResultFilePattern { get; set; } = "*.csv";
}
