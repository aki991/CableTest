using System.Collections.Concurrent;
using System.Text;
using CableTest.Core.Gateway;
using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Testovi nadgledanja CSV fajla. Svaki radi nad stvarnim fajlom u privremenom folderu, jer se
/// upravo ponašanje fajla (zauzet, nestao, zamenjen, upisan do pola) i proverava.
/// </summary>
public sealed class FileBasedTesterGatewayTests : IDisposable
{
    private const string CrLf = "\r\n";

    private const string Header =
        "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit,";

    /// <summary>Koliko se najduže čeka da rezultat stigne. Nadgledanje je asinhrono.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly string _folder;
    private readonly ConcurrentQueue<TestRunReceivedEventArgs> _received = new();
    private readonly List<FileBasedTesterGateway> _gateways = new();

    public FileBasedTesterGatewayTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTest_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        foreach (FileBasedTesterGateway gateway in _gateways)
        {
            gateway.Dispose();
        }

        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Privremeni folder; ako je nešto još zaključano, ostaje operativnom sistemu.
        }
    }

    // -------------------------------------------------------------------------------------
    // Nadgledanje
    // -------------------------------------------------------------------------------------

    [Fact]
    public void DopisivanjeRedova_DokJeNadgledanjeAktivno_StizeRezultat()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf);

        FileBasedTesterGateway gateway = Start(path);

        Append(path, Row(1, "pass", "11:44:11"));
        WaitForRuns(1);

        Append(path, Row(2, "fail", "11:47:34", "SHORT O01-O02;FAIL"));
        WaitForRuns(2);

        TestRunReceivedEventArgs[] events = _received.ToArray();
        Assert.Equal(new[] { 1, 2 }, events.Select(e => e.Run.Seq));
        Assert.True(events[0].Run.Passed);
        Assert.False(events[1].Run.Passed);
        Assert.Equal("O01-O02", Assert.Single(events[1].Run.Defects).Points);
        Assert.Equal(path, events[0].SourcePath);

        TesterGatewayState state = gateway.State;
        Assert.True(state.IsMonitoring);
        Assert.Equal(path, state.CurrentFilePath);
        Assert.Equal(2, state.ProcessedRunCount);
        Assert.NotNull(state.LastChangeAt);
        Assert.Null(state.LastError);
    }

    /// <summary>
    /// Tester je radio dok aplikacija nije bila upaljena; ti rezultati ne smeju da se izgube —
    /// ali ne smeju ni da se prikažu kao trenutni ishod. Zato stižu označeni kao backfill.
    /// </summary>
    [Fact]
    public void RedoviKojiSuVecUFajlu_StizuPriPokretanju_OznaceniKaoBackfill()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf + Row(1, "pass", "07:15:00") + Row(2, "pass", "07:19:00"));

        Start(path);
        WaitForRuns(2);

        TestRunReceivedEventArgs[] zateceni = _received.ToArray();
        Assert.Equal(new[] { 1, 2 }, zateceni.Select(e => e.Run.Seq));
        Assert.All(zateceni, e => Assert.True(e.IsBackfill));
        Assert.All(zateceni, e => Assert.False(e.IsLive));
    }

    /// <summary>
    /// Ista provera sa druge strane: sve što stigne posle pokretanja je uživo. Ovo je granica na
    /// koju se oslanja veliki prikaz ishoda — vidi TestRunReceivedEventArgs.IsBackfill.
    /// </summary>
    [Fact]
    public void RezultatiPoslePokretanja_StizuKaoUzivo()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf + Row(1, "pass", "07:15:00"));

        Start(path);
        WaitForRuns(1);

        Append(path, Row(2, "fail", "11:47:34", "SHORT O01-O02;FAIL"));
        WaitForRuns(2);

        TestRunReceivedEventArgs[] events = _received.ToArray();

        Assert.True(events[0].IsBackfill);      // zatečen u fajlu
        Assert.False(events[1].IsBackfill);     // stigao uživo
        Assert.True(events[1].IsLive);
    }

    /// <summary>
    /// Fajl koji se pojavi posle pokretanja nije zatečeni sadržaj — njegovi redovi su uživo.
    /// </summary>
    [Fact]
    public void FajlKojiSePojaviPoslePokretanja_DajeUzivoRezultate()
    {
        string path = CsvPath("jos-ga-nema.csv");

        Start(path);
        Write(path, Header + CrLf + Row(1, "pass", "11:44:11"));
        WaitForRuns(1);

        Assert.False(_received.ToArray()[0].IsBackfill);
    }

    /// <summary>Dnevna rotacija posle pokretanja: novi fajl donosi uživo rezultate.</summary>
    [Fact]
    public void ZamenjenFajlPoslePokretanja_DajeUzivoRezultate()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf + Row(1, "pass", "07:15:00") + Row(2, "pass", "07:19:00"));

        Start(path);
        WaitForRuns(2);

        File.Delete(path);
        Write(path, Header + CrLf + Row(1, "pass", "06:02:00", date: "2026/09/08"));
        WaitForRuns(3);

        Assert.False(_received.ToArray()[2].IsBackfill);
    }

    [Fact]
    public void DopisivanjePolovicnogReda_SeCekaDaSeDovrsi()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf);

        FileBasedTesterGateway gateway = Start(path);

        // Fajl uhvaćen usred upisa: red nema prelom na kraju.
        Append(path, "3,M100W1,pa");
        gateway.CheckNow();
        Thread.Sleep(150);
        Assert.Empty(_received);

        Append(path, "ss,2026/09/07,12:01:00,,,Miloš,PASS,PASS," + CrLf);
        WaitForRuns(1);

        TestRun run = _received.ToArray()[0].Run;
        Assert.Equal(3, run.Seq);
        Assert.True(run.Passed);
        Assert.Equal("M100W1", run.SpecFileName);
    }

    [Fact]
    public void ZamenaFajlaNovim_NaIstojPutanji_CitaSeOdPocetka()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf + Row(1, "pass", "07:15:00") + Row(2, "pass", "07:19:00"));

        FileBasedTesterGateway gateway = Start(path);
        WaitForRuns(2);

        // Dnevna rotacija: na istoj putanji je nov, kraći fajl sa drugim datumom.
        File.Delete(path);
        Write(path, Header + CrLf + Row(1, "pass", "06:02:00", date: "2026/09/08"));

        WaitForRuns(3);

        TestRunReceivedEventArgs[] events = _received.ToArray();
        Assert.Equal(new DateTime(2026, 9, 8, 6, 2, 0), events[2].Run.TestedAt);
        Assert.True(gateway.State.IsMonitoring);
    }

    [Fact]
    public void BrisanjeFajla_NeRusiNadgledanje_IFajlMozeDaSeVrati()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf + Row(1, "pass", "07:15:00"));

        FileBasedTesterGateway gateway = Start(path);
        WaitForRuns(1);

        File.Delete(path);
        WaitFor(() => gateway.State.CurrentFilePath is null, "fajl je obrisan");

        Assert.True(gateway.State.IsMonitoring);

        Write(path, Header + CrLf + Row(5, "pass", "08:30:00"));
        WaitForRuns(2);

        Assert.Equal(5, _received.ToArray()[1].Run.Seq);
    }

    [Fact]
    public void FajlKojiNePostojiNaPocetku_PaSePojavi()
    {
        string path = CsvPath("jos-ga-nema.csv");

        FileBasedTesterGateway gateway = Start(path);

        Assert.True(gateway.State.IsMonitoring);
        Assert.Null(gateway.State.CurrentFilePath);
        Assert.Empty(_received);

        Write(path, Header + CrLf + Row(1, "pass", "11:44:11"));
        WaitForRuns(1);

        Assert.Equal(path, gateway.State.CurrentFilePath);
    }

    [Fact]
    public void FolderKojiNePostojiNaPocetku_NeRusiPokretanje()
    {
        string missing = Path.Combine(_folder, "nema-ovog-foldera", "rezultati.csv");

        FileBasedTesterGateway gateway = Start(missing);

        Assert.True(gateway.State.IsMonitoring);
        Assert.Null(gateway.State.CurrentFilePath);

        Directory.CreateDirectory(Path.GetDirectoryName(missing)!);
        Write(missing, Header + CrLf + Row(1, "pass", "11:44:11"));

        WaitForRuns(1);
    }

    [Fact]
    public void FajlKojiCableConnectorDrziOtvoren_SeIpakCita()
    {
        string path = CsvPath("rezultati.csv");

        // Isti način otvaranja kakav ima CableConnector: drži fajl otvoren za upis.
        using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        WriteTo(writer, Header + CrLf);

        Start(path);

        WriteTo(writer, Row(1, "pass", "11:44:11"));
        WaitForRuns(1);

        Assert.Equal(1, _received.ToArray()[0].Run.Seq);
    }

    [Fact]
    public void PodesenFolder_PratiSeNajnovijiCsvUNjemu()
    {
        string stari = CsvPath("MERENJA-20260907.csv");
        Write(stari, Header + CrLf + Row(1, "pass", "07:15:00"));
        File.SetLastWriteTimeUtc(stari, DateTime.UtcNow.AddHours(-2));

        FileBasedTesterGateway gateway = Start(_folder);
        WaitForRuns(1);

        Assert.True(gateway.IsFolderMode());
        Assert.Equal(stari, gateway.State.CurrentFilePath);

        // Nov dan, nov fajl u istom folderu.
        string novi = CsvPath("MERENJA-20260908.csv");
        Write(novi, Header + CrLf + Row(1, "pass", "06:02:00", date: "2026/09/08"));

        WaitFor(() => gateway.State.CurrentFilePath == novi, "prelazak na noviji fajl");
        WaitForRuns(2);

        Assert.Equal(new DateTime(2026, 9, 8, 6, 2, 0), _received.ToArray()[1].Run.TestedAt);
    }

    /// <summary>
    /// Fajl je prepisan istim imenom pa je kraći nego ranije: parser ga čita od početka, a već
    /// prijavljeni rezultati se prepoznaju po prirodnom ključu i ne stižu po drugi put.
    /// </summary>
    [Fact]
    public void PrepisanFajl_VecPrijavljeniRezultatiNeStizuDvaput()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf + Row(1, "pass", "11:44:11") + Row(2, "pass", "11:50:00") + Row(3, "pass", "11:55:00"));

        FileBasedTesterGateway gateway = Start(path);
        WaitForRuns(3);

        // Isti fajl, prepisan i kraći — prva dva reda su ista.
        File.Delete(path);
        Write(path, Header + CrLf + Row(1, "pass", "11:44:11") + Row(2, "pass", "11:50:00"));

        WaitFor(() => gateway.State.SkippedDuplicateCount >= 2, "preskočena dva duplikata");
        Thread.Sleep(200);

        Assert.Equal(3, _received.Count);

        // Nov rezultat u prepisanom fajlu se normalno prijavljuje.
        Append(path, Row(4, "fail", "12:10:00", "OPEN O31-O32;FAIL"));
        WaitForRuns(4);

        Assert.Equal(new[] { 1, 2, 3, 4 }, _received.ToArray().Select(e => e.Run.Seq));
    }

    [Fact]
    public void StopMonitoring_ZaustavljaDogadjaje()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf);

        FileBasedTesterGateway gateway = Start(path);
        Append(path, Row(1, "pass", "11:44:11"));
        WaitForRuns(1);

        gateway.StopMonitoring();
        Assert.False(gateway.State.IsMonitoring);

        Append(path, Row(2, "pass", "11:50:00"));
        Thread.Sleep(300);
        gateway.CheckNow();
        Thread.Sleep(100);

        Assert.Single(_received);
    }

    [Fact]
    public void StopMonitoring_BezPokretanja_NePada()
    {
        var gateway = new FileBasedTesterGateway(new TesterGatewayOptions { ResultPath = CsvPath("x.csv") });
        _gateways.Add(gateway);

        gateway.StopMonitoring();
        gateway.CheckNow();

        Assert.False(gateway.State.IsMonitoring);
    }

    [Fact]
    public void PutanjaNijePodesena_DajeGreskuUStanjuAliNePada()
    {
        var gateway = new FileBasedTesterGateway(new TesterGatewayOptions
        {
            ResultPath = string.Empty,
            PollInterval = TimeSpan.FromMilliseconds(50)
        });
        _gateways.Add(gateway);

        gateway.StartMonitoring();

        Assert.True(gateway.State.IsMonitoring);
        Assert.NotNull(gateway.State.LastError);
        Assert.Contains("nije podešena", gateway.State.LastError!, StringComparison.Ordinal);
    }

    [Fact]
    public void NeprepoznatDatum_StizeKaoUpozorenjeUzRezultat()
    {
        string path = CsvPath("rezultati.csv");
        Write(path, Header + CrLf);

        FileBasedTesterGateway gateway = Start(path);
        Append(path, "1,M100W1,pass,07.09.2026,11:44:11,,,Miloš,PASS,PASS," + CrLf);

        WaitForRuns(1);

        Assert.NotEmpty(_received.ToArray()[0].Warnings);
        Assert.NotNull(gateway.State.LastWarning);
    }

    // -------------------------------------------------------------------------------------
    // Priprema testa (.c61)
    // -------------------------------------------------------------------------------------

    [Fact]
    public async Task PrepareTestAsync_UpisujeSpecFajlUSpecFolder()
    {
        string specFolder = Path.Combine(_folder, "spec");
        Directory.CreateDirectory(specFolder);

        var gateway = new FileBasedTesterGateway(new TesterGatewayOptions
        {
            SpecFolder = specFolder,
            TemplatePath = Path.Combine("templates", "MASTER.c61"),
            ResultPath = CsvPath("rezultati.csv")
        });
        _gateways.Add(gateway);

        var cable = new Cable { Code = "M100-W1", SpecFileName = "M100W1" };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02-O31-O32" });

        await gateway.PrepareTestAsync(cable, CancellationToken.None);

        string written = Path.Combine(specFolder, "M100W1.c61");
        Assert.True(File.Exists(written));

        string text = File.ReadAllText(written);
        Assert.Contains("OSNet=1", text, StringComparison.Ordinal);
        Assert.Contains("OSNet:O01-O02-O31-O32", text, StringComparison.Ordinal);
        Assert.Equal(written, gateway.State.LastPreparedSpecPath);
    }

    [Fact]
    public async Task PrepareTestAsync_SpecFolderNePostoji_JavljaGresku()
    {
        var gateway = new FileBasedTesterGateway(new TesterGatewayOptions
        {
            SpecFolder = Path.Combine(_folder, "nema-ga"),
            TemplatePath = Path.Combine("templates", "MASTER.c61"),
            ResultPath = CsvPath("rezultati.csv")
        });
        _gateways.Add(gateway);

        var cable = new Cable { SpecFileName = "M100W1" };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02" });

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => gateway.PrepareTestAsync(cable, CancellationToken.None));
    }

    [Fact]
    public async Task PrepareTestAsync_NeispravnoImeSpecFajla_SeOdbija()
    {
        var gateway = new FileBasedTesterGateway(new TesterGatewayOptions
        {
            SpecFolder = _folder,
            TemplatePath = Path.Combine("templates", "MASTER.c61"),
            ResultPath = CsvPath("rezultati.csv")
        });
        _gateways.Add(gateway);

        var cable = new Cable { SpecFileName = "m100 w1" };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02" });

        await Assert.ThrowsAsync<ArgumentException>(
            () => gateway.PrepareTestAsync(cable, CancellationToken.None));
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private FileBasedTesterGateway Start(string resultPath)
    {
        var gateway = new FileBasedTesterGateway(new TesterGatewayOptions
        {
            SpecFolder = _folder,
            ResultPath = resultPath,
            PollInterval = TimeSpan.FromMilliseconds(50)
        });

        _gateways.Add(gateway);
        gateway.TestRunReceived += (_, e) => _received.Enqueue(e);
        gateway.StartMonitoring();
        return gateway;
    }

    private string CsvPath(string name) => Path.Combine(_folder, name);

    private static string Row(int seq, string pass, string time, string osTest = "PASS", string date = "2026/09/07")
        => $"{seq},M100W1,{pass},{date},{time},,,Miloš,PASS,{osTest}," + CrLf;

    private static void Write(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        WriteTo(stream, text);
    }

    private static void Append(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        WriteTo(stream, text);
    }

    private static void WriteTo(Stream stream, string text)
    {
        byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private void WaitForRuns(int count) => WaitFor(() => _received.Count >= count, $"{count} rezultata");

    private static void WaitFor(Func<bool> condition, string what)
    {
        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(20);
        }

        Assert.Fail($"Isteklo je vreme čekanja na: {what}.");
    }
}
