using CableTest.Core.Gateway;
using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Lažni gateway — njime se razvija i proverava GUI dok se ne sedne za sto pored testera.
/// Mora da se ponaša kao pravi: bez pokrenutog nadgledanja ništa ne stiže, a isti rezultat
/// ne stiže dvaput.
/// </summary>
public class FakeTesterGatewayTests
{
    private static Cable Kabl()
    {
        var cable = new Cable { Id = 1, Code = "M100-W1", SpecFileName = "M100W1" };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02-O31-O32" });
        return cable;
    }

    [Fact]
    public void Receive_DokJeNadgledanjeAktivno_OkidaDogadjaj()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.ResultReceived += (_, e) => received.Add(e.Run);

        gateway.StartMonitoring();
        TestRun run = gateway.ReceivePass(Kabl());

        Assert.Same(run, Assert.Single(received));
        Assert.True(run.Passed);
        Assert.Equal("M100W1", run.SpecFileName);
        Assert.Equal(1, gateway.Diagnostics.ProcessedRunCount);
    }

    [Fact]
    public void Receive_BezPokrenutogNadgledanja_NeOkidaNista()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.ResultReceived += (_, e) => received.Add(e.Run);

        gateway.ReceivePass(Kabl());

        Assert.Empty(received);
        Assert.False(gateway.Diagnostics.IsMonitoring);
    }

    [Fact]
    public void StopMonitoring_ZaustavljaDogadjaje()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.ResultReceived += (_, e) => received.Add(e.Run);

        gateway.StartMonitoring();
        gateway.ReceivePass(Kabl());
        gateway.StopMonitoring();
        gateway.ReceivePass(Kabl());

        Assert.Single(received);
    }

    [Fact]
    public void ReceiveFail_PravIGreskeNaKojeGuiMozeDaSeVezuje()
    {
        var gateway = new FakeTesterGateway();
        gateway.StartMonitoring();

        TestRun run = gateway.ReceiveFail(Kabl(), "SHORT O01-O02", "OPEN O31-O32");

        Assert.False(run.Passed);
        Assert.Equal(2, run.Defects.Count);
        Assert.Equal(DefectKind.Short, run.Defects[0].Kind);
        Assert.Equal("O01-O02", run.Defects[0].Points);
        Assert.Equal(DefectKind.Open, run.Defects[1].Kind);
        Assert.Equal("Prekid između tačaka O31 i O32", run.Defects[1].Describe());
    }

    [Fact]
    public void ReceiveCsvLine_ProlaziKrozPraviParser()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.ResultReceived += (_, e) => received.Add(e.Run);
        gateway.StartMonitoring();

        gateway.ReceiveCsvLine("1,M100W1,fail,2026/09/07,11:47:34,,,Miloš,FAIL,SHORT O01-O02;FAIL,");

        TestRun run = Assert.Single(received);
        Assert.Equal(1, run.Seq);
        Assert.False(run.Passed);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 47, 34), run.TestedAt);
        Assert.Equal("Miloš", run.Operator);
        Assert.Equal("O01-O02", Assert.Single(run.Defects).Points);
    }

    [Fact]
    public void IstiRezultatDvaput_SePrijavljujeJednom()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.ResultReceived += (_, e) => received.Add(e.Run);
        gateway.StartMonitoring();

        const string line = "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS,";
        gateway.ReceiveCsvLine(line);
        gateway.ReceiveCsvLine(line);

        Assert.Single(received);
        Assert.Equal(1, gateway.Diagnostics.SkippedDuplicateCount);
    }

    [Fact]
    public void Receive_PodrazumevanoJeUzivo()
    {
        var gateway = new FakeTesterGateway();
        TestRunReceivedEventArgs? primljeno = null;
        gateway.ResultReceived += (_, e) => primljeno = e;
        gateway.StartMonitoring();

        gateway.ReceivePass(Kabl());

        Assert.NotNull(primljeno);
        Assert.False(primljeno!.IsBackfill);
        Assert.True(primljeno.IsLive);
    }

    [Fact]
    public void ReceiveBackfillPass_JeOznacenKaoBackfill()
    {
        var gateway = new FakeTesterGateway();
        TestRunReceivedEventArgs? primljeno = null;
        gateway.ResultReceived += (_, e) => primljeno = e;
        gateway.StartMonitoring();

        gateway.ReceiveBackfillPass(Kabl());

        Assert.NotNull(primljeno);
        Assert.True(primljeno!.IsBackfill);
        Assert.False(primljeno.IsLive);
    }

    [Fact]
    public async Task PrepareTestAsync_PamtiKablBezDiranjaDiska()
    {
        var gateway = new FakeTesterGateway();
        Cable cable = Kabl();

        await gateway.PrepareTestAsync(cable, CancellationToken.None);

        Assert.Same(cable, Assert.Single(gateway.PreparedCables));
        Assert.Equal("M100W1.c61", gateway.Diagnostics.LastPreparedSpecPath);
    }

    [Fact]
    public async Task PrepareFailure_SePrenosiPozivaocu()
    {
        var gateway = new FakeTesterGateway
        {
            PrepareFailure = new DirectoryNotFoundException("Spec folder ne postoji.")
        };

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => gateway.PrepareTestAsync(Kabl(), CancellationToken.None));

        Assert.Empty(gateway.PreparedCables);
    }

    /// <summary>Ostatak aplikacije radi sa interfejsom — ovo je provera da je to moguće.</summary>
    [Fact]
    public void RadiKrozInterfejs()
    {
        ITesterGateway gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.ResultReceived += (_, e) => received.Add(e.Run);

        gateway.StartMonitoring();
        Assert.True(gateway.Diagnostics.IsMonitoring);

        ((FakeTesterGateway)gateway).ReceivePass(Kabl());

        Assert.Single(received);
    }

    // -------------------------------------------------------------------------------------
    // Scenariji nad pripremljenim CSV fajlovima
    // -------------------------------------------------------------------------------------

    /// <summary>Lažni tester ume sam da pokrene test — po tome se i razlikuje od rada preko fajlova.</summary>
    [Fact]
    public void Capabilities_KazuDaUmeDaPokreneTest()
        => Assert.True(new FakeTesterGateway().Capabilities.CanStartTest);

    /// <summary>Uzorak je stvarni fajl iz pogona, isti onaj nad kojim se ispituje parser.</summary>
    [Fact]
    public async Task Scenario_Pass_CitaStvarniFajlIzPogona()
    {
        using var folder = new SampleFolder();
        folder.Write("01-stvarni.csv", CsvSamples.RealFile);

        await using var gateway = folder.Gateway(FakeScenario.Pass);
        var primljeni = new List<TestRunReceivedEventArgs>();
        gateway.ResultReceived += (_, e) => primljeni.Add(e);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway);

        Assert.True(result.IsOk);

        // Fajl ima dva reda; čekanje se vraća na prvi, a oba stižu kroz događaj.
        Assert.Equal(1, result.Run!.Seq);
        Assert.True(result.Run.Passed);
        Assert.Equal(2, primljeni.Count);
        Assert.Equal(3, primljeni[1].Run.Defects.Count);
    }

    [Fact]
    public async Task Scenario_Pass_DajeRezultatIzPripremljenogFajla()
    {
        using var folder = new SampleFolder();
        folder.Write("01-prolaz.csv", CsvSamples.PassFile);

        await using var gateway = folder.Gateway(FakeScenario.Pass);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway);

        Assert.True(result.IsOk);
        Assert.True(result.Run!.Passed);
        Assert.Equal(1, result.Run.Seq);
        Assert.Equal(TesterState.Completed, gateway.State);
    }

    [Fact]
    public async Task Scenario_Fail_DajeRezultatSaGreskama()
    {
        using var folder = new SampleFolder();
        folder.Write("01-pad.csv", CsvSamples.FailFile);

        await using var gateway = folder.Gateway(FakeScenario.Fail);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway);

        Assert.True(result.IsOk);
        Assert.False(result.Run!.Passed);
        Assert.Equal("O01-O02", Assert.Single(result.Run.Defects).Points);
    }

    /// <summary>Rezultat koji ne stigne mora da završi kao istek vremena, a ne kao „prošao".</summary>
    [Fact]
    public async Task Scenario_Timeout_NikadNeDajeRezultat()
    {
        using var folder = new SampleFolder();
        folder.Write("01-prolaz.csv", CsvSamples.PassFile);

        await using var gateway = folder.Gateway(FakeScenario.Timeout);
        gateway.StartMonitoring();

        await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);
        await gateway.StartTestAsync(CancellationToken.None);

        GatewayResult result = await gateway.WaitForResultAsync(
            TimeSpan.FromMilliseconds(200),
            CancellationToken.None);

        Assert.Equal(GatewayStatus.Timeout, result.Status);
        Assert.Equal(TesterState.Failed, gateway.State);
    }

    /// <summary>Neispravan sadržaj ne ruši ništa: nijedan rezultat, ali stiže objašnjenje.</summary>
    [Fact]
    public async Task Scenario_Corrupt_NeDajeRezultatAliJavljaProblem()
    {
        using var folder = new SampleFolder();

        await using var gateway = folder.Gateway(FakeScenario.Corrupt);
        var greske = new List<GatewayErrorEventArgs>();
        gateway.GatewayError += (_, e) => greske.Add(e);
        gateway.StartMonitoring();

        await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);
        await gateway.StartTestAsync(CancellationToken.None);

        GatewayResult result = await gateway.WaitForResultAsync(
            TimeSpan.FromMilliseconds(300),
            CancellationToken.None);

        Assert.Equal(GatewayStatus.Timeout, result.Status);
        Assert.NotEmpty(greske);
    }

    /// <summary>Zaključan fajl se pročita čim se oslobodi — isto kao u pogonu.</summary>
    [Fact]
    public async Task Scenario_Locked_DajeRezultatKadSeFajlOslobodi()
    {
        using var folder = new SampleFolder();
        folder.Write("01-prolaz.csv", CsvSamples.PassFile);

        await using var gateway = folder.Gateway(FakeScenario.Locked);
        gateway.LockDuration = TimeSpan.FromMilliseconds(200);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway, TimeSpan.FromSeconds(10));

        Assert.True(result.IsOk);
        Assert.Equal(1, result.Run!.Seq);
    }

    /// <summary>
    /// Isti fajl dvaput: drugo čitanje je ponovljeno čitanje istog izvora i preskače se, a broj
    /// preskočenih se vidi u stanju.
    /// </summary>
    [Fact]
    public async Task Scenario_Duplicate_PrijavljujeRezultatSamoJednom()
    {
        using var folder = new SampleFolder();
        folder.Write("01-prolaz.csv", CsvSamples.PassFile);

        await using var gateway = folder.Gateway(FakeScenario.Duplicate);
        var primljeni = new List<TestRunReceivedEventArgs>();
        gateway.ResultReceived += (_, e) => primljeni.Add(e);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway);

        Assert.True(result.IsOk);

        // Drugo čitanje istog fajla se preskače; čeka se da prođe i ono.
        await Task.Delay(200);

        Assert.Single(primljeni);
        Assert.Equal(1, gateway.Diagnostics.SkippedDuplicateCount);
    }

    /// <summary>Bez foldera sa uzorcima rezultat se pravi u pamćenju — tako radi demo režim.</summary>
    [Fact]
    public async Task BezFoldera_ScenarioIpakDajeRezultat()
    {
        await using var gateway = new FakeTesterGateway(startDelay: TimeSpan.Zero);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway);

        Assert.True(result.IsOk);
        Assert.True(result.Run!.Passed);
    }

    [Fact]
    public async Task BezFoldera_ScenarioFail_DajeRezultatSaGreskom()
    {
        await using var gateway = new FakeTesterGateway(scenario: FakeScenario.Fail, startDelay: TimeSpan.Zero);
        gateway.StartMonitoring();

        GatewayResult result = await RunAsync(gateway);

        Assert.True(result.IsOk);
        Assert.False(result.Run!.Passed);
        Assert.NotEmpty(result.Run.Defects);
    }

    /// <summary>Fajlovi se uzimaju redom po imenu, pa se niz vrti ukrug.</summary>
    [Fact]
    public void NextSample_UzimaFajloveRedomPaIzPocetka()
    {
        using var folder = new SampleFolder();
        folder.Write("01-prvi.csv", CsvSamples.PassFile);
        folder.Write("02-drugi.csv", CsvSamples.PassFile);

        var gateway = new FakeTesterGateway(folder.Path);

        Assert.EndsWith("01-prvi.csv", gateway.NextSample(), StringComparison.Ordinal);
        Assert.EndsWith("02-drugi.csv", gateway.NextSample(), StringComparison.Ordinal);
        Assert.EndsWith("01-prvi.csv", gateway.NextSample(), StringComparison.Ordinal);
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    /// <summary>Pripremi program, pokreni test i sačekaj rezultat — ceo tok jednim pozivom.</summary>
    private static async Task<GatewayResult> RunAsync(FakeTesterGateway gateway, TimeSpan? timeout = null)
    {
        Assert.True((await gateway.LoadProgramAsync(Kabl(), CancellationToken.None)).IsOk);
        Assert.True((await gateway.StartTestAsync(CancellationToken.None)).IsOk);

        return await gateway.WaitForResultAsync(timeout ?? TimeSpan.FromSeconds(5), CancellationToken.None);
    }


    /// <summary>Privremeni folder sa pripremljenim CSV fajlovima.</summary>
    private sealed class SampleFolder : IDisposable
    {
        public SampleFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CableTestFake_" + System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string name, string text)
            => File.WriteAllText(System.IO.Path.Combine(Path, name), text);

        /// <summary>Gateway nad ovim folderom, sa kratkim odlaganjima da testovi ne čekaju.</summary>
        public FakeTesterGateway Gateway(FakeScenario scenario)
            => new(Path, scenario, TimeSpan.Zero, new StableFileReader(new[] { 10, 20, 40, 80 }));

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Privremeni folder.
            }
        }
    }
}
