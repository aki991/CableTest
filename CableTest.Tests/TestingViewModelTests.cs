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
/// Testovi glavnog ekrana. Nijedan ne otvara prozor — ViewModel ne dira WPF tipove.
/// </summary>
/// <remarks>
/// Rade nad pravom SQLite bazom u privremenom fajlu i nad <see cref="FakeTesterGateway"/>, pa se
/// proverava ceo lanac: rezultat stigne kroz gateway, prođe kroz parser, veže se za kabl, upiše u
/// istoriju i tek onda se pojavi na ekranu.
/// </remarks>
public sealed class TestingViewModelTests : IDisposable
{
    private readonly string _folder;
    private readonly CableTestDatabase _database;
    private readonly SqliteCableRepository _cables;
    private readonly SqliteVehicleRepository _vehicles;
    private readonly SqliteTestRunRepository _runs;
    private readonly FakeTesterGateway _gateway = new();
    private readonly CountingSoundPlayer _sounds = new();
    private readonly RecordingDocumentViewer _documents = new();
    private readonly AppSettings _settings;
    private TestingViewModel? _viewModel;

    public TestingViewModelTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestVm_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);

        _database = CableTestDatabase.OpenAndMigrate(Path.Combine(_folder, "test.db"));
        _cables = new SqliteCableRepository(_database);
        _vehicles = new SqliteVehicleRepository(_database);
        _runs = new SqliteTestRunRepository(_database);

        _settings = new AppSettings
        {
            SpecFolder = _folder,
            ResultPath = Path.Combine(_folder, "rezultati.csv"),
            OperatorName = "Miloš",
            SoundEnabled = true
        };
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
            // Privremeni folder.
        }
    }

    // -----------------------------------------------------------------------------------
    // Veliki prikaz ishoda
    // -----------------------------------------------------------------------------------

    [Fact]
    public void PriPokretanju_EkranStojiPrazanSaTekstomCekaSeRezultat()
    {
        TestingViewModel vm = Start();

        Assert.Equal(OutcomeState.Waiting, vm.Outcome);
        Assert.Equal("ČEKA SE REZULTAT", vm.OutcomeText);
        Assert.True(vm.IsWaiting);
        Assert.False(vm.HasResult);
        Assert.False(vm.MeasurementCardCommand.CanExecute(null));
    }

    [Fact]
    public void UzivoRezultat_MenjaPrikaz()
    {
        TestingViewModel vm = Start();

        _gateway.ReceivePass(Kabl());

        Assert.Equal(OutcomeState.Pass, vm.Outcome);
        Assert.Equal("PROŠAO", vm.OutcomeText);
        Assert.Equal("KABL JE ISPRAVAN", vm.OutcomeSubText);
        Assert.True(vm.HasResult);
        Assert.Contains("M30-W1", vm.ResultCableText, StringComparison.Ordinal);
        Assert.Equal("Miloš", vm.ResultOperatorText);
        Assert.True(vm.MeasurementCardCommand.CanExecute(null));
    }

    /// <summary>
    /// Ovo je bezbednosno pravilo, ne kozmetika: zatečeni rezultat na ekranu bi operatera naveo
    /// da pusti neispitan kabl dalje.
    /// </summary>
    [Fact]
    public void BackfillRezultat_NeMenjaPrikaz()
    {
        TestingViewModel vm = Start();

        _gateway.ReceiveBackfillPass(Kabl());

        Assert.Equal(OutcomeState.Waiting, vm.Outcome);
        Assert.Equal("ČEKA SE REZULTAT", vm.OutcomeText);
        Assert.False(vm.HasResult);
        Assert.False(vm.MeasurementCardCommand.CanExecute(null));

        // Ali jeste upisan u istoriju i operater vidi da se to dogodilo.
        Assert.Equal(1, _runs.Count());
        Assert.Equal(1, vm.BackfillCount);
        Assert.True(vm.HasBackfillNotice);
    }

    [Fact]
    public void BackfillPaUzivo_PrikazujeSamoUzivoRezultat()
    {
        TestingViewModel vm = Start();

        _gateway.ReceiveBackfillPass(Kabl());
        _gateway.ReceiveFail(Kabl(), "SHORT O01-O02");

        Assert.Equal(OutcomeState.Fail, vm.Outcome);
        Assert.Equal("PAO", vm.OutcomeText);
        Assert.Equal("Kratak spoj između tačaka O01 i O02", Assert.Single(vm.ResultDefects));
        Assert.Equal(2, _runs.Count());
    }

    [Fact]
    public void BackfillRezultat_NePustaZvuk()
    {
        Start();

        _gateway.ReceiveBackfillPass(Kabl());

        Assert.Equal(0, _sounds.PassCount);
        Assert.Equal(0, _sounds.FailCount);
    }

    [Fact]
    public void UzivoRezultat_PustaZvukRazlicitZaPassIFail()
    {
        Start();

        _gateway.ReceivePass(Kabl());
        Assert.Equal(1, _sounds.PassCount);
        Assert.Equal(0, _sounds.FailCount);

        _gateway.ReceiveFail(Kabl(), "SHORT O01-O02");
        Assert.Equal(1, _sounds.PassCount);
        Assert.Equal(1, _sounds.FailCount);
    }

    [Fact]
    public void FailRezultat_PrikazujeGreskeUCitljivomObliku()
    {
        TestingViewModel vm = Start();

        _gateway.ReceiveFail(Kabl(), "SHORT O01-O02", "OPEN O31");

        Assert.Equal(OutcomeState.Fail, vm.Outcome);
        Assert.Equal("KABL NIJE ISPRAVAN", vm.OutcomeSubText);
        Assert.Equal(
            new[] { "Kratak spoj između tačaka O01 i O02", "Prekid na tački O31" },
            vm.ResultDefects);
    }

    [Fact]
    public void NeprepoznatKabl_SePrikazujeIUpisuje()
    {
        TestingViewModel vm = Start();

        // Test pokrenut iz spec fajla koji nije u bazi.
        _gateway.ReceiveCsvLine("1,PROBA1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS,");

        Assert.Equal(OutcomeState.Pass, vm.Outcome);
        Assert.Contains("PROBA1", vm.ResultCableText, StringComparison.Ordinal);
        Assert.Contains("nije u bazi", vm.ResultCableText, StringComparison.Ordinal);

        TestRun upisan = Assert.Single(_runs.GetRecent(10));
        Assert.False(upisan.IsRecognized);
    }

    // -----------------------------------------------------------------------------------
    // Izbor vozila i kabla
    // -----------------------------------------------------------------------------------

    [Fact]
    public void IzborVozila_MenjaListuKablova()
    {
        Vehicle drugo = DodajVozilo("Golf", ("G100-W1", "G100W1"), ("G100-W2", "G100W2"));
        TestingViewModel vm = Start();

        // Vozilo "Miloš Veliki" iz kataloga ima dvadeset kablova, M30-W1 … M30-W20.
        vm.SelectedVehicle = vm.Vehicles.First(v => v.Name == "Miloš Veliki");
        Assert.Equal(20, vm.Cables.Count);
        Assert.Equal("M30-W1", vm.Cables.First().Code);
        Assert.Equal("M30-W20", vm.Cables.Last().Code);

        vm.SelectedVehicle = vm.Vehicles.First(v => v.Id == drugo.Id);
        Assert.Equal(new[] { "G100-W1", "G100-W2" }, vm.Cables.Select(c => c.Code));
        Assert.Equal("G100-W1", vm.SelectedCable!.Code);
    }

    [Fact]
    public void IzborKabla_PrikazujeNjegovuNetListu()
    {
        TestingViewModel vm = Start();

        // Pri pokretanju se bira prvo vozilo iz kataloga i njegov prvi kabl.
        Assert.Equal("M30-W1", vm.SelectedCable!.Code);
        Assert.Equal(vm.SelectedCable.Nets.Count, vm.Nets.Count);
        Assert.StartsWith("1. ", vm.Nets.First(), StringComparison.Ordinal);
        Assert.EndsWith(vm.SelectedCable.Nets[0].Points, vm.Nets.First(), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------------------
    // Priprema testa
    // -----------------------------------------------------------------------------------

    [Fact]
    public void PripremiTest_BezIzabranogKabla_JeOnemoguceno()
    {
        TestingViewModel vm = Start();

        vm.SelectedVehicle = null;

        Assert.Null(vm.SelectedCable);
        Assert.False(vm.PrepareTestCommand.CanExecute(null));
        Assert.False(vm.HasSelectedCable);
    }

    [Fact]
    public void PripremiTest_SaIzabranimKablom_JeDostupno()
    {
        TestingViewModel vm = Start();

        Assert.NotNull(vm.SelectedCable);
        Assert.True(vm.PrepareTestCommand.CanExecute(null));
    }

    [Fact]
    public async Task PripremiTest_PrikazujePutanjuIUputstvo()
    {
        TestingViewModel vm = Start();

        await vm.PrepareTestCommand.ExecuteAsync();

        Assert.True(vm.HasPrepareMessage);
        Assert.False(vm.HasPrepareError);
        Assert.Contains("Download", vm.PrepareMessage!, StringComparison.Ordinal);
        Assert.Single(_gateway.PreparedCables);
    }

    [Fact]
    public async Task PripremiTest_NeuspelaPriprema_PrikazujeGreskuINeRusiAplikaciju()
    {
        _gateway.PrepareFailure = new DirectoryNotFoundException(
            "Spec folder \"C:\\Cable Linker8761\\spec\" ne postoji.");

        TestingViewModel vm = Start();

        await vm.PrepareTestCommand.ExecuteAsync();

        Assert.True(vm.HasPrepareError);
        Assert.Contains("ne postoji", vm.PrepareError!, StringComparison.Ordinal);
        Assert.False(vm.HasPrepareMessage);

        // Ekran i dalje radi.
        _gateway.ReceivePass(Kabl());
        Assert.Equal(OutcomeState.Pass, vm.Outcome);
    }

    // -----------------------------------------------------------------------------------
    // Merna karta
    // -----------------------------------------------------------------------------------

    [Fact]
    public void MernaKarta_BezRezultata_JeOnemogucena()
    {
        TestingViewModel vm = Start();

        Assert.False(vm.MeasurementCardCommand.CanExecute(null));

        vm.MeasurementCardCommand.Execute(null);
        Assert.Null(_documents.LastOpened);
    }

    [Fact]
    public void MernaKarta_SaRezultatom_OtvaraDokument()
    {
        TestingViewModel vm = Start();
        _gateway.ReceiveFail(Kabl(), "SHORT O01-O02");

        vm.MeasurementCardCommand.Execute(null);

        Assert.NotNull(_documents.LastOpened);
        Assert.EndsWith(".html", _documents.LastOpened!, StringComparison.OrdinalIgnoreCase);

        string html = File.ReadAllText(_documents.LastOpened!);
        Assert.Contains("Merna karta", html, StringComparison.Ordinal);
        Assert.Contains("NEISPRAVAN", html, StringComparison.Ordinal);
        Assert.Contains("Kratak spoj", html, StringComparison.Ordinal);
        Assert.Contains("Miloš", html, StringComparison.Ordinal);

        File.Delete(_documents.LastOpened!);
    }

    // -----------------------------------------------------------------------------------
    // Traka stanja i upozorenja
    // -----------------------------------------------------------------------------------

    [Fact]
    public void OneDrivePutanja_DajeUpozorenje()
    {
        _settings.SpecFolder = @"C:\Users\Andreja\OneDrive\spec";

        TestingViewModel vm = Start();

        Assert.True(vm.HasProblem);
        Assert.Contains("OneDrive", vm.ProblemText!, StringComparison.Ordinal);
        Assert.Contains("Could not find file", vm.ProblemText!, StringComparison.Ordinal);
    }

    [Fact]
    public void SpecFolderKojiNePostoji_DajeGresku()
    {
        _settings.SpecFolder = Path.Combine(_folder, "nema-ovog-foldera");

        TestingViewModel vm = Start();

        Assert.True(vm.HasProblem);
        Assert.True(vm.ProblemIsError);
        Assert.Contains("ne postoji", vm.ProblemText!, StringComparison.Ordinal);
    }

    [Fact]
    public void IspravnaPodesavanja_NeDajuProblem()
    {
        File.WriteAllText(_settings.ResultPath, "Seq.,Filename,Pass,Date,Time," + Environment.NewLine);

        TestingViewModel vm = Start();

        Assert.False(vm.HasProblem);
    }

    [Fact]
    public void CsvFajlKojiJosNePostoji_DajeUpozorenjeAliNeGresku()
    {
        // Fajl se pojavljuje tek kad CableConnector prvi put upiše rezultat — to je upozorenje,
        // ne greška, i nadgledanje ga čeka.
        TestingViewModel vm = Start();

        Assert.True(vm.HasProblem);
        Assert.False(vm.ProblemIsError);
        Assert.Contains("čeka", vm.ProblemText!, StringComparison.Ordinal);
    }

    [Fact]
    public void TrakaStanja_PokazujeDaJeNadgledanjeAktivno()
    {
        TestingViewModel vm = Start();

        Assert.True(vm.IsMonitoring);
        Assert.Equal("0", vm.ProcessedText);

        _gateway.ReceivePass(Kabl());
        vm.RefreshState();

        Assert.Equal("1", vm.ProcessedText);
        Assert.NotEqual("—", vm.LastChangeText);
    }

    // -----------------------------------------------------------------------------------
    // Demo režim
    // -----------------------------------------------------------------------------------

    [Fact]
    public void DemoRezim_IskljucenPodrazumevano()
    {
        TestingViewModel vm = Start();

        Assert.False(vm.IsDemoMode);
        Assert.False(vm.CanSimulate);
        Assert.False(vm.SimulatePassCommand.CanExecute(null));
    }

    [Fact]
    public void DemoRezim_ImaVidnoOznacenuTraku()
    {
        _settings.DemoMode = true;
        TestingViewModel vm = Start();

        Assert.True(vm.IsDemoMode);
        Assert.Contains("DEMO", vm.DemoBannerText, StringComparison.Ordinal);
        Assert.Contains("nisu stvarna merenja", vm.DemoBannerText, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoRezim_SimulirajPass_UbacujeUzivoRezultat()
    {
        _settings.DemoMode = true;
        TestingViewModel vm = Start();

        Assert.True(vm.SimulatePassCommand.CanExecute(null));
        vm.SimulatePassCommand.Execute(null);

        Assert.Equal(OutcomeState.Pass, vm.Outcome);
        Assert.True(vm.HasResult);
        Assert.Equal(1, _sounds.PassCount);

        TestRun upisan = Assert.Single(_runs.GetRecent(10));
        Assert.True(upisan.Passed);
        Assert.True(upisan.IsRecognized);
        Assert.Equal("Miloš", upisan.Operator);
    }

    [Fact]
    public void DemoRezim_SimulirajFail_DajeGreskeSaTackamaIzNetListe()
    {
        _settings.DemoMode = true;
        TestingViewModel vm = Start();

        vm.SimulateFailCommand.Execute(null);

        Assert.Equal(OutcomeState.Fail, vm.Outcome);
        Assert.Equal(1, _sounds.FailCount);

        // Greške se prave od tačaka iz net liste izabranog kabla, a ne od izmišljenih oznaka.
        string prvaTacka = vm.SelectedCable!.Nets[0].Points.Split('-')[0];
        Assert.Contains(vm.ResultDefects, d => d.Contains(prvaTacka, StringComparison.Ordinal));
        Assert.All(vm.ResultDefects, d => Assert.DoesNotContain("Nepoznata", d, StringComparison.Ordinal));
    }

    [Fact]
    public void DemoRezim_DvaPutaZaredom_DajeDvaRezultata()
    {
        _settings.DemoMode = true;
        TestingViewModel vm = Start();

        vm.SimulatePassCommand.Execute(null);
        vm.SimulateFailCommand.Execute(null);

        Assert.Equal(2, _runs.Count());
        Assert.Equal(OutcomeState.Fail, vm.Outcome);
    }

    // -----------------------------------------------------------------------------------
    // Pomoćno
    // -----------------------------------------------------------------------------------

    private TestingViewModel Start()
    {
        _viewModel = new TestingViewModel(
            _gateway,
            _vehicles,
            _cables,
            new TestRunImporter(_cables, _runs),
            _sounds,
            _documents,
            _settings);

        _viewModel.Start();
        return _viewModel;
    }

    private Cable Kabl() => _cables.GetBySpecFileName("M30-W1")!;

    private Vehicle DodajVozilo(string ime, params (string Code, string Spec)[] kablovi)
    {
        var vehicle = new Vehicle { Name = ime };
        _vehicles.Add(vehicle);

        foreach ((string code, string spec) in kablovi)
        {
            var cable = new Cable { VehicleId = vehicle.Id, Code = code, SpecFileName = spec };
            cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02" });
            _cables.Add(cable);
        }

        return vehicle;
    }

    private sealed class CountingSoundPlayer : ISoundPlayer
    {
        public int PassCount { get; private set; }

        public int FailCount { get; private set; }

        public void PlayPass() => PassCount++;

        public void PlayFail() => FailCount++;
    }

    private sealed class RecordingDocumentViewer : IDocumentViewer
    {
        public string? LastOpened { get; private set; }

        public void Open(string path) => LastOpened = path;
    }
}
