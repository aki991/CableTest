using CableTest.App.Services;
using CableTest.App.ViewModels;
using CableTest.Core.Configuration;
using CableTest.Core.Data;
using CableTest.Core.Gateway;
using CableTest.Core.Model;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Grupisanje kablova u spisku za izbor.
/// </summary>
/// <remarks>
/// Operater prvo bira grupu („Grupa 40"), pa tek kad je otvori i kabl iz nje. Grupa je deo
/// oznake pre prve crtice i ne stoji zasebno u bazi — vidi <see cref="Cable.Group"/>.
/// </remarks>
public sealed class GrupeKablovaTests : IDisposable
{
    private readonly string _folder;
    private readonly CableTestDatabase _database;
    private readonly SqliteCableRepository _cables;
    private readonly SqliteVehicleRepository _vehicles;
    private readonly SqliteTestRunRepository _runs;
    private readonly FakeTesterGateway _gateway = new();
    private readonly AppSettings _settings;
    private TestingViewModel? _viewModel;

    // Cable.SpecFileName je jedinstven u bazi, a migracija zaseje kablove vozila „Miloš Veliki"
    // („40W1-1" i ostale). Kablovi iz ovih testova zato nose svoj, nasumičan predznak.
    // Velikim slovima: SpecFileNameValidator ne prima mala.
    private readonly string _predznak =
        "G" + Path.GetRandomFileName().Replace(".", string.Empty, StringComparison.Ordinal)[..6].ToUpperInvariant();
    private int _redni;

    public GrupeKablovaTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestGrupe_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);

        _database = CableTestDatabase.OpenAndMigrate(Path.Combine(_folder, "test.db"));
        _cables = new SqliteCableRepository(_database);
        _vehicles = new SqliteVehicleRepository(_database);
        _runs = new SqliteTestRunRepository(_database);

        _settings = new AppSettings
        {
            SpecFolder = _folder,
            ResultPath = Path.Combine(_folder, "rezultati.csv"),
            OperatorName = "Miloš"
        };
    }

    // -----------------------------------------------------------------------------------
    // Oznaka grupe
    // -----------------------------------------------------------------------------------

    [Theory]
    [InlineData("40-W1.1", "40")]
    [InlineData("40-W5", "40")]
    [InlineData("M100-W1", "M100")]
    [InlineData("G100-W2", "G100")]
    public void Grupa_JeDeoOznakePrePrveCrtice(string oznaka, string grupa)
        => Assert.Equal(grupa, new Cable { Code = oznaka }.Group);

    /// <summary>
    /// Oznaka bez crtice nema grupu. Takav kabl ne sme da se ugura u tuđu grupu, jer bi se onda
    /// u spisku našao pod zaglavljem kome ne pripada.
    /// </summary>
    [Theory]
    [InlineData("W1")]
    [InlineData("")]
    [InlineData("-W1")]
    public void Grupa_BezCrtice_JePrazna(string oznaka)
        => Assert.Equal(string.Empty, new Cable { Code = oznaka }.Group);

    // -----------------------------------------------------------------------------------
    // Spisak grupa
    // -----------------------------------------------------------------------------------

    [Fact]
    public void Grupe_SePraveIzOznakaKablova()
    {
        TestingViewModel vm = Start(DodajVozilo("Grupe A", "40-W1.1", "40-W2", "40-W5"));

        CableGroupViewModel grupa = Assert.Single(vm.CableGroups);

        Assert.Equal("40", grupa.Name);
        Assert.Equal("Grupa 40", grupa.Title);
        Assert.Equal(3, grupa.Count);
    }

    [Fact]
    public void Grupe_IduRedomPoOznaci()
    {
        TestingViewModel vm = Start(DodajVozilo("Grupe C", "70-W1", "40-W1", "50-W2"));

        Assert.Equal(new[] { "40", "50", "70" }, vm.CableGroups.Select(g => g.Name));
    }

    /// <summary>
    /// Grupe kreću zatvorene — to se i tražilo: prvo se vidi grupa, pa tek na klik kablovi iz nje.
    /// Vredi i kad je kabl iz grupe već izabran: izbor je podatak, otvorenost je samo prikaz.
    /// </summary>
    [Fact]
    public void Grupe_PriUcitavanju_SuZatvorene()
    {
        TestingViewModel vm = Start(DodajVozilo("Grupe A", "40-W1.1", "40-W2"));

        Assert.NotNull(vm.SelectedCable);
        Assert.All(vm.CableGroups, g => Assert.False(g.IsExpanded));
    }

    [Fact]
    public void PromenaVozila_ZatvaraGrupe()
    {
        Vehicle prvo = DodajVozilo("Grupe A", "40-W1.1", "40-W2");
        Vehicle drugo = DodajVozilo("Grupe B", "G100-W1");
        TestingViewModel vm = Start(prvo);

        vm.CableGroups[0].IsExpanded = true;
        vm.SelectedVehicle = vm.Vehicles.First(v => v.Id == drugo.Id);

        Assert.All(vm.CableGroups, g => Assert.False(g.IsExpanded));
    }

    // -----------------------------------------------------------------------------------
    // Pretraga
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Pretraga otvara grupe u kojima ima pogodaka. Bez toga bi izgledalo kao da pretraga ne
    /// nalazi ništa — pogodak bi bio iza zatvorenog zaglavlja.
    /// </summary>
    [Fact]
    public void Pretraga_OtvaraGrupuSaPogotkom()
    {
        TestingViewModel vm = Start(DodajVozilo("Grupe C", "40-W1.1", "50-W2"));

        vm.CableFilter = "40-W1";

        Assert.True(Grupa(vm, "40").IsExpanded);
        Assert.False(Grupa(vm, "50").IsExpanded);
    }

    [Fact]
    public void Pretraga_BezPogodaka_NeOtvaraNista()
    {
        TestingViewModel vm = Start(DodajVozilo("Grupe C", "40-W1.1", "50-W2"));

        vm.CableFilter = "nepostojeće";

        Assert.All(vm.CableGroups, g => Assert.False(g.IsExpanded));
    }

    [Fact]
    public void BrisanjePretrage_VracaGrupeNaZatvoreno()
    {
        TestingViewModel vm = Start(DodajVozilo("Grupe C", "40-W1.1", "50-W2"));

        vm.CableFilter = "40";
        Assert.True(Grupa(vm, "40").IsExpanded);

        vm.CableFilter = string.Empty;
        Assert.All(vm.CableGroups, g => Assert.False(g.IsExpanded));
    }

    /// <summary>Pretraga radi i po nazivu, ne samo po oznaci — grupa se i tada otvara.</summary>
    [Fact]
    public void Pretraga_PoNazivu_OtvaraGrupu()
    {
        TestingViewModel vm = Start(
            DodajVozilo("Grupe C", ("40-W1.1", "Fabrički Wabco kabl"), ("50-W2", "Snop FLRY")));

        vm.CableFilter = "Wabco";

        Assert.True(Grupa(vm, "40").IsExpanded);
        Assert.False(Grupa(vm, "50").IsExpanded);
    }

    // -----------------------------------------------------------------------------------
    // Broj kablova u zaglavlju
    // -----------------------------------------------------------------------------------

    /// <summary>Srpski ima tri oblika imenice uz broj; 11–14 idu na „kablova".</summary>
    [Theory]
    [InlineData(1, "1 kabl")]
    [InlineData(2, "2 kabla")]
    [InlineData(4, "4 kabla")]
    [InlineData(5, "5 kablova")]
    [InlineData(11, "11 kablova")]
    [InlineData(12, "12 kablova")]
    [InlineData(14, "14 kablova")]
    [InlineData(21, "21 kabl")]
    [InlineData(22, "22 kabla")]
    [InlineData(25, "25 kablova")]
    public void BrojKablova_JeNaSrpskom(int broj, string tekst)
        => Assert.Equal(tekst, new CableGroupViewModel("40") { Count = broj }.CountText);

    // -----------------------------------------------------------------------------------
    // Pomoćno
    // -----------------------------------------------------------------------------------

    private static CableGroupViewModel Grupa(TestingViewModel vm, string ime)
        => vm.CableGroups.Single(g => g.Name == ime);

    /// <summary>
    /// Pokreće ekran i prebacuje ga na zadato vozilo.
    /// </summary>
    /// <remarks>
    /// Vozilo se bira izričito: migracija zaseje „Miloš Veliki" sa dvanaest kablova, a ekran pri
    /// pokretanju uzima prvo vozilo iz kataloga. Bez ovog koraka bi testovi merili zasejane
    /// kablove umesto svojih.
    /// </remarks>
    private TestingViewModel Start(Vehicle vozilo)
    {
        _viewModel = new TestingViewModel(
            _gateway,
            _vehicles,
            _cables,
            new TestRunImporter(_cables, _runs),
            new SilentSoundPlayer(),
            new SilentDocumentViewer(),
            _settings);

        _viewModel.Start();
        _viewModel.SelectedVehicle = _viewModel.Vehicles.First(v => v.Id == vozilo.Id);

        return _viewModel;
    }

    private Vehicle DodajVozilo(string ime, params string[] oznake)
        => DodajVozilo(ime, oznake.Select(o => (o, "opis")).ToArray());

    private Vehicle DodajVozilo(string ime, params (string Code, string Description)[] kablovi)
    {
        var vehicle = new Vehicle { Name = ime };
        _vehicles.Add(vehicle);

        foreach ((string code, string description) in kablovi)
        {
            var cable = new Cable
            {
                VehicleId = vehicle.Id,
                Code = code,
                Description = description,
                SpecFileName = _predznak + (++_redni).ToString(System.Globalization.CultureInfo.InvariantCulture)
            };

            cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02" });
            _cables.Add(cable);
        }

        return vehicle;
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Privremeni folder nije bitan za ishod testa.
        }
    }

    /// <summary>Ne otvara ništa — nijedan test iz ove grupe ne dira mernu kartu.</summary>
    private sealed class SilentDocumentViewer : IDocumentViewer
    {
        public void Open(string path)
        {
        }
    }
}
