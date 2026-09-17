using CableTest.App.Mvvm;
using CableTest.Core.Configuration;

namespace CableTest.App.ViewModels;

/// <summary>
/// Okvir aplikacije: zaglavlje, bočni meni i traka stanja u podnožju, plus ekrani koje meni bira.
/// </summary>
/// <remarks>
/// Prozor ima jedan DataContext — ovaj. Svaki ekran je jedno svojstvo, pa se u XAML-u vezuje kao
/// <c>{Binding Testing.…}</c> ili <c>{Binding History.…}</c> i odmah se vidi čiji je koji podatak.
/// </remarks>
public sealed class ShellViewModel : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _store;

    private string? _settingsError;

    public ShellViewModel(
        TestingViewModel testing,
        CatalogViewModel catalog,
        HistoryViewModel history,
        CableConnectorViewModel cableConnector,
        AppSettings settings,
        SettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(testing);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(cableConnector);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(store);

        Testing = testing;
        Catalog = catalog;
        History = history;
        CableConnector = cableConnector;
        _settings = settings;
        _store = store;

        // Svaki rezultat koji uđe u istoriju menja i brojače u podnožju i spisak u Istoriji.
        Testing.RunImported += OnRunImported;
    }

    /// <summary>Glavni ekran — „Ispitivanje".</summary>
    public TestingViewModel Testing { get; }

    /// <summary>Katalog — ekrani „Vozila", „Kablovi" i „Test programi".</summary>
    public CatalogViewModel Catalog { get; }

    /// <summary>Ekran „Istorija" i brojači u podnožju.</summary>
    public HistoryViewModel History { get; }

    /// <summary>Razvojni alat „CableConnector"; u meniju ga nema dok se ne uključi u Podešavanjima.</summary>
    public CableConnectorViewModel CableConnector { get; }

    /// <summary>
    /// Da li se u bočnom meniju vidi stavka „CableConnector".
    /// </summary>
    /// <remarks>
    /// Menja se odmah — bez ponovnog pokretanja — i odmah se upisuje u <c>settings.json</c>, jer
    /// je to prekidač koji se pali i gasi usred rada.
    /// </remarks>
    public bool ShowCableConnector
    {
        get => _settings.ShowCableConnectorTool;
        set
        {
            if (_settings.ShowCableConnectorTool == value)
            {
                return;
            }

            _settings.ShowCableConnectorTool = value;
            Raise();

            // Izbor izvora rezultata visi o istom prekidaču; kad se ugasi, važi fajl.
            RaiseAll(nameof(ShowResultSourceChoice), nameof(ResultSourceText));

            SaveSettings();
        }
    }

    private void SaveSettings()
        => SettingsError = _store.Save(_settings) ? null : _store.LastError;

    /// <summary>
    /// Da li se u Podešavanjima uopšte nudi izbor izvora rezultata.
    /// </summary>
    /// <remarks>
    /// Simulacija je razvojna stvar i vezana je za isti prekidač koji otkriva stranu
    /// „CableConnector": dok je on isključen, izbora nema i radi se sa fajlom. Tako simulacija ne
    /// može da se uključi slučajno, a rezultat koji nije izmeren ne može da se nađe pred
    /// operaterom.
    /// </remarks>
    public bool ShowResultSourceChoice => ShowCableConnector;

    /// <summary>Izvori rezultata koji se nude.</summary>
    public IReadOnlyList<ResultSource> ResultSources { get; } = new[] { ResultSource.File, ResultSource.Simulation };

    /// <summary>
    /// Izabrani izvor rezultata; važi od sledećeg pokretanja, kao i ostale putanje.
    /// </summary>
    public ResultSource ResultSource
    {
        get => _settings.ResultSource;
        set
        {
            if (_settings.ResultSource == value)
            {
                return;
            }

            _settings.ResultSource = value;
            Raise();
            Raise(nameof(ResultSourceText));
            SaveSettings();
        }
    }

    /// <summary>Izvor rezultata sa kojim aplikacija zaista radi, rečeno operateru.</summary>
    public string ResultSourceText => _settings.EffectiveResultSource == ResultSource.Simulation
        ? "simulacija — rezultati su pripremljeni unapred i nisu stvarna merenja"
        : "fajl koji piše CableConnector";

    /// <summary>Koliko se čeka rezultat posle pokretanja testa.</summary>
    public string ResultTimeoutText =>
        $"{_settings.ResultTimeout.TotalSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture)} s";

    /// <summary>Poruka ako podešavanja nisu sačuvana, ili <c>null</c>.</summary>
    public string? SettingsError
    {
        get => _settingsError;
        private set
        {
            if (Set(ref _settingsError, value))
            {
                Raise(nameof(HasSettingsError));
            }
        }
    }

    /// <summary>Da li poslednji upis podešavanja nije uspeo.</summary>
    public bool HasSettingsError => SettingsError is not null;

    // -----------------------------------------------------------------------------------
    // Podešavanja (ekran „Podešavanja" ih samo prikazuje)
    // -----------------------------------------------------------------------------------

    /// <summary>Folder u koji se upisuje generisani .c61.</summary>
    public string SpecFolder => _settings.SpecFolder;

    /// <summary>Putanja CSV fajla ili foldera sa rezultatima.</summary>
    public string ResultPath => _settings.ResultPath;

    /// <summary>Putanja šablona MASTER.c61, ili napomena da se uzima onaj pored programa.</summary>
    public string TemplatePathText => string.IsNullOrWhiteSpace(_settings.TemplatePath)
        ? @"templates\MASTER.c61 (pored programa)"
        : _settings.TemplatePath;

    /// <summary>Ime operatera.</summary>
    public string OperatorName => Testing.OperatorDisplayName;

    /// <summary>Da li uz rezultat ide zvučni signal.</summary>
    public string SoundText => _settings.SoundEnabled ? "uključen" : "isključen";

    /// <summary>Stanje demo režima, u obliku razumljivom operateru.</summary>
    public string DemoText => _settings.DemoMode
        ? "uključen — rezultati su simulirani"
        : "isključen";

    /// <summary>Gde se podešavanja menjaju.</summary>
    public string SettingsFileText => _store.FilePath;

    // -----------------------------------------------------------------------------------
    // Život okvira
    // -----------------------------------------------------------------------------------

    /// <summary>Pokreće glavni ekran i učitava ono što ostali ekrani prikazuju.</summary>
    public void Start()
    {
        Testing.Start();
        Catalog.Load();
        History.Load();
    }

    public void Dispose()
    {
        Testing.RunImported -= OnRunImported;
        Testing.Dispose();
        CableConnector.Dispose();
    }

    private void OnRunImported() => History.Load();
}
