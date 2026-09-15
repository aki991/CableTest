using System.Windows;
using CableTest.App.Services;
using CableTest.App.ViewModels;
using CableTest.Core.Configuration;
using CableTest.Core.Data;
using CableTest.Core.Gateway;

namespace CableTest.App;

/// <summary>
/// Sastavljanje aplikacije: podešavanja, baza, gateway, ViewModel, prozor.
/// </summary>
/// <remarks>
/// Sve zavisnosti se prave ovde i predaju kroz konstruktore. Bez kontejnera — aplikacija ima
/// jedan glavni ekran i nekoliko servisa, pa se ovako na jednom mestu vidi šta je od čega
/// sastavljeno i šta se menja kad dođe <c>SerialTesterGateway</c>.
/// </remarks>
public partial class App : Application
{
    /// <summary>Argument komandne linije kojim se pokreće demo režim.</summary>
    public const string DemoArgument = "--demo";

    /// <summary>Ime fajla baze u demo režimu.</summary>
    public const string DemoDatabaseFileName = "CableTest.demo.db";

    private ITesterGateway? _gateway;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Start(e.Args);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Aplikacija nije mogla da se pokrene.\n\n" + ex.Message,
                "CableTest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        (_gateway as IDisposable)?.Dispose();
        base.OnExit(e);
    }

    private void Start(string[] args)
    {
        SettingsStore store = SettingsStore.Default();
        AppSettings settings = store.Load();

        // Demo se uključuje argumentom komandne linije ili prekidačem u podešavanjima.
        if (args.Any(a => string.Equals(a, DemoArgument, StringComparison.OrdinalIgnoreCase)))
        {
            settings.DemoMode = true;
        }

        CableTestDatabase database = OpenDatabase(settings.DemoMode);

        var cables = new SqliteCableRepository(database);
        var vehicles = new SqliteVehicleRepository(database);
        var runs = new SqliteTestRunRepository(database);
        var importer = new TestRunImporter(cables, runs);

        _gateway = settings.DemoMode
            ? new FakeTesterGateway()
            : new FileBasedTesterGateway(settings.ToGatewayOptions());

        ISoundPlayer sounds = new SwitchableSoundPlayer(new SystemSoundPlayer(), settings.SoundEnabled);

        var testing = new TestingViewModel(
            _gateway,
            vehicles,
            cables,
            importer,
            sounds,
            new BrowserDocumentViewer(),
            settings);

        var shell = new ShellViewModel(
            testing,
            new CatalogViewModel(vehicles, cables),
            new HistoryViewModel(runs, cables),
            settings);

        MainWindow = new MainWindow(shell);
        MainWindow.Show();
    }

    /// <summary>
    /// Otvara bazu i primenjuje migracije.
    /// </summary>
    /// <remarks>
    /// Demo režim radi nad <b>zasebnim fajlom baze</b>. Simulirani rezultati ne smeju da se nađu
    /// u stvarnoj istoriji testova — ona je dokaz da je kabl ispitan i mora da sadrži isključivo
    /// ono što je tester zaista izmerio.
    /// </remarks>
    private static CableTestDatabase OpenDatabase(bool demo)
    {
        if (!demo)
        {
            return CableTestDatabase.OpenDefault();
        }

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CableTest");

        Directory.CreateDirectory(folder);
        return CableTestDatabase.OpenAndMigrate(Path.Combine(folder, DemoDatabaseFileName));
    }
}
