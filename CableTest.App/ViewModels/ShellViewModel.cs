using CableTest.App.Mvvm;
using CableTest.Core.Configuration;
using CableTest.Core.Model;

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

    public ShellViewModel(
        TestingViewModel testing,
        CatalogViewModel catalog,
        HistoryViewModel history,
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(testing);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(settings);

        Testing = testing;
        Catalog = catalog;
        History = history;
        _settings = settings;

        // Svaki rezultat koji uđe u istoriju menja i brojače u podnožju i spisak u Istoriji.
        Testing.RunImported += OnRunImported;
    }

    /// <summary>Glavni ekran — „Ispitivanje".</summary>
    public TestingViewModel Testing { get; }

    /// <summary>Katalog — ekrani „Vozila", „Kablovi" i „Test programi".</summary>
    public CatalogViewModel Catalog { get; }

    /// <summary>Ekran „Istorija" i brojači u podnožju.</summary>
    public HistoryViewModel History { get; }

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
    public string SettingsFileText => SettingsStore.Default().FilePath;

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
    }

    private void OnRunImported() => History.Load();
}
