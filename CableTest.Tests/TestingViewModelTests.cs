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
        Assert.Equal(TestingViewModel.WaitingText, vm.OutcomeText);
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
        Assert.Contains("M40-W1.1", vm.ResultCableText, StringComparison.Ordinal);
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
        Assert.Equal(TestingViewModel.WaitingText, vm.OutcomeText);
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

        // Vozilo „Miloš Veliki“ ima dvanaest kablova sa crteža, redom po stranama crteža.
        vm.SelectedVehicle = vm.Vehicles.First(v => v.Name == "Miloš Veliki");
        Assert.Equal(55, vm.Cables.Count);
        Assert.Contains(vm.Cables, c => c.Code == "M40-W1.1");
        Assert.Contains(vm.Cables, c => c.Code == "M96-W2.2");
        Assert.Equal(7, vm.Cables.Select(c => c.Group).Distinct().Count());

        vm.SelectedVehicle = vm.Vehicles.First(v => v.Id == drugo.Id);
        Assert.Equal(new[] { "G100-W1", "G100-W2" }, vm.Cables.Select(c => c.Code));
        Assert.Equal("G100-W1", vm.SelectedCable!.Code);
    }

    [Fact]
    public void IzborKabla_PrikazujeNjegovuNetListu()
    {
        TestingViewModel vm = Start();

        // Pri pokretanju se bira prvo vozilo iz kataloga i njegov prvi kabl.
        Assert.Equal("M26-W1", vm.SelectedCable!.Code);
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

        // Uputstvo dolazi iz Capabilities, pa se poruka menja zajedno sa implementacijom.
        Assert.Contains(vm.StartInstruction, vm.PrepareMessage!, StringComparison.Ordinal);
        Assert.Single(_gateway.PreparedCables);
        Assert.Equal(TesterState.ProgramLoaded, vm.TesterState);
    }

    /// <summary>
    /// Lažni gateway ume sam da pokrene test, pa ekran nudi dugme; rad preko fajlova ne ume, pa
    /// umesto dugmeta stoji uputstvo da se pritisne START na mašini.
    /// </summary>
    [Fact]
    public void DugmePokreniTest_PostojiSamoKadGatewayUmeDaPokreneTest()
    {
        TestingViewModel vm = Start();

        Assert.True(vm.CanShowStartButton);
        Assert.False(vm.ShowStartInstruction);
        Assert.False(vm.CanStartTest);   // program još nije pripremljen
    }

    [Fact]
    public async Task PokreniTest_PoslePripreme_DovodiRezultatIzSimulacije()
    {
        _gateway.StartDelay = TimeSpan.Zero;
        _settings.ResultTimeoutSeconds = 5;

        TestingViewModel vm = Start();

        await vm.PrepareTestCommand.ExecuteAsync();
        Assert.True(vm.CanStartTest);

        await vm.StartTestCommand.ExecuteAsync();

        // Rezultat stiže sa pozadinske niti i prebacuje se na nit prikaza, pa se sačeka da se
        // taj posao izvrši — u pogonu to radi WPF dispečer, ovde nit testa.
        await Sacekaj(() => vm.Outcome == OutcomeState.Pass);

        Assert.True(vm.HasResult);
        Assert.False(vm.IsWaitingForResult);
    }

    /// <summary>Čeka da se izvrši posao prebačen na nit prikaza; najduže sekundu.</summary>
    private static async Task Sacekaj(Func<bool> uslov)
    {
        for (int i = 0; i < 100 && !uslov(); i++)
        {
            await Task.Delay(10);
        }

        Assert.True(uslov(), "Prikaz se nije osvežio u zadatom vremenu.");
    }

    /// <summary>Dok se čeka rezultat, Panel 3 to i pokazuje; prekid vraća ekran na čekanje.</summary>
    [Fact]
    public async Task PokreniTest_PrekidCekanja_VracaEkranNaCekanje()
    {
        _gateway.Scenario = FakeScenario.Timeout;
        _gateway.StartDelay = TimeSpan.Zero;
        _settings.ResultTimeoutSeconds = 3600;

        TestingViewModel vm = Start();

        await vm.PrepareTestCommand.ExecuteAsync();

        Task pokretanje = vm.StartTestCommand.ExecuteAsync();

        Assert.True(vm.IsWaitingForResult);
        Assert.Equal(OutcomeState.Running, vm.Outcome);

        vm.CancelWaitCommand.Execute(null);
        await pokretanje;

        Assert.False(vm.IsWaitingForResult);
        Assert.Equal(OutcomeState.Waiting, vm.Outcome);
    }

    /// <summary>
    /// Rezultat koji ne stigne nije „prošao" — Panel 3 mora jasno da kaže da kabl nije ispitan.
    /// </summary>
    [Fact]
    public async Task PokreniTest_KadRezultatNeStigne_PrikazujeIstekVremena()
    {
        _gateway.Scenario = FakeScenario.Timeout;
        _gateway.StartDelay = TimeSpan.Zero;
        _settings.ResultTimeoutSeconds = 1;

        TestingViewModel vm = Start();

        await vm.PrepareTestCommand.ExecuteAsync();
        await vm.StartTestCommand.ExecuteAsync();

        Assert.Equal(OutcomeState.TimedOut, vm.Outcome);
        Assert.Equal("NEMA REZULTATA", vm.OutcomeText);
        Assert.False(vm.HasResult);
        Assert.True(vm.HasProblem);
        Assert.Equal(TesterState.Failed, vm.TesterState);
    }

    /// <summary>Kod FAIL se vidi koja mreža nije prošla i šta joj je.</summary>
    [Fact]
    public void PaoRezultat_PrikazujeMrezeKojeNisuProsle()
    {
        TestingViewModel vm = Start();

        // Ishod se upisuje uz netove samo kad je prikazani rezultat baš izabranog kabla, pa se
        // kabl bira izričito — katalog ima 55 kablova i prvi u spisku nije ovaj.
        vm.SelectedCable = vm.Cables.First(c => c.SpecFileName == "M40W1-1");

        // Tačke pripadaju izabranom kablu: C01 i C02 su njegova prva dva terminala.
        _gateway.ReceiveFail(Kabl(), "SHORT C01-C02");

        Assert.Equal(OutcomeState.Fail, vm.Outcome);
        Assert.True(vm.HasFailedNets);

        // Greška se upisuje uz svaku mrežu koja dodiruje pogođene tačke.
        Assert.All(vm.FailedNetRows, red => Assert.Contains("Kratak spoj", red.Defect, StringComparison.Ordinal));
        Assert.All(vm.FailedNetRows, red => Assert.Equal(NetRow.Failed, red.Status));

        Assert.Contains(
            vm.FailedNetRows[0].Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
            vm.FailedNetsSummary,
            StringComparison.Ordinal);

        // Mreže koje nisu pogođene ostaju van spiska.
        Assert.True(vm.FailedNetRows.Count <= vm.NetRows.Count);
    }

    /// <summary>
    /// Greška se operateru pokazuje jezikom crteža: terminal, boja žice, pa tačka testera u
    /// zagradi. Tačka ostaje da bi se poruka mogla uporediti sa CSV-om testera.
    /// </summary>
    [Fact]
    public void FailRezultat_PrevodiTackeTesteraUTerminaleIBojuZice()
    {
        TestingViewModel vm = Start();

        vm.SelectedCable = vm.Cables.First(c => c.SpecFileName == "M40W2");
        _gateway.ReceiveFail(vm.SelectedCable!, "OPEN A02-B01");

        // Kod =M40-W2 su A02 i B01 krajevi iste žice: pin 1 konektora DIN 72585 i pin P u XM6.
        Assert.Equal("Prekid: 1 → P (A02–B01)", Assert.Single(vm.ResultDefects));
    }

    /// <summary>Tačka koje nema u priključnoj tabeli ostaje prikazana onakva kakva je.</summary>
    [Fact]
    public void FailRezultat_NepoznataTacka_OstajeKakvaJe()
    {
        TestingViewModel vm = Start();

        _gateway.ReceiveFail(Kabl(), "SHORT P31-P32");

        Assert.Equal("Kratak spoj između tačaka P31 i P32", Assert.Single(vm.ResultDefects));
    }

    // -----------------------------------------------------------------------------------
    // Podaci sa crteža
    // -----------------------------------------------------------------------------------

    [Fact]
    public void IzborKabla_PrikazujeOznakuTipDuzinuIProvodnike()
    {
        TestingViewModel vm = Start();

        vm.SelectedCable = vm.Cables.First(c => c.SpecFileName == "M40W5");

        Assert.Equal("=M40-W5", vm.SelectedDesignationText);
        Assert.Equal("Snop FLRY 9 x 0,75mm2", vm.SelectedCableTypeText);
        Assert.Equal("0,8 m", vm.SelectedLengthText);
        Assert.Equal("Gr.40 Miloš Veliki  Prelazni.pdf, strana 13", vm.SelectedSourceText);

        Assert.True(vm.HasWires);
        Assert.Equal(12, vm.Wires.Count);
        Assert.Equal("BR", vm.Wires[0].Color);
        Assert.Equal("0,75 mm²", vm.Wires[0].CrossSectionText);
        Assert.Equal("1", vm.Wires[0].FromTerminal);
        Assert.Equal("B09", vm.Wires[0].ToTerminal);

        // Ispod tabele provodnika stoje izvedene veze — ono što zaista ide u tester.
        Assert.Equal(12, vm.Nets.Count);
        Assert.Equal("12. A12-B12", vm.Nets[^1]);
    }

    /// <summary>
    /// Demo simulacija radi nad izvedenim netovima i daje poruke u istom obliku kao pravi tester:
    /// prekid unutar neta, kratak spoj između dva neta.
    /// </summary>
    [Fact]
    public void DemoSimulacija_RadiNadIzvedenimNetovima()
    {
        _settings.DemoMode = true;
        TestingViewModel vm = Start();

        vm.SelectedCable = vm.Cables.First(c => c.SpecFileName == "M40W2");
        Assert.True(vm.CanSimulate);

        vm.SimulateFailCommand.Execute(null);

        Assert.Equal(OutcomeState.Fail, vm.Outcome);
        Assert.Equal(
            new[]
            {
                "Prekid: 2 → P, žica BR (A01–B01)",
                "Kratak spoj: 2 → 1 (A01–A02)"
            },
            vm.ResultDefects);
    }

    /// <summary>Dok adapter nije napravljen, operater mora da vidi da je raspored privremen.</summary>
    [Fact]
    public void IzborKabla_SaPrivremenomPrikljucnomTabelom_PrikazujeUpozorenje()
    {
        TestingViewModel vm = Start();

        Assert.True(vm.HasProvisionalPoints);
        Assert.Contains("privremena", vm.ProvisionalNoticeText, StringComparison.Ordinal);
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

    private Cable Kabl() => _cables.GetBySpecFileName("M40W1-1")!;

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
