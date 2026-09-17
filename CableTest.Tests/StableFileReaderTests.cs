using System.Text;
using CableTest.Core.Gateway;
using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Čitanje fajla koji neko drugi upravo upisuje.
/// </summary>
/// <remarks>
/// Radi nad stvarnim fajlovima u privremenom folderu, jer se upravo ponašanje fajla proverava:
/// zaključan, upisan do pola, nestao. Odlaganja su skraćena da testovi ne bi trajali sekundama —
/// red pokušaja je isti, samo kraći.
/// </remarks>
public sealed class StableFileReaderTests : IDisposable
{
    private static readonly int[] KratkaOdlaganja = { 5, 10, 20, 40 };

    private readonly string _folder;

    public StableFileReaderTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestRead_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Privremeni folder.
        }
    }

    [Fact]
    public async Task MiranFajl_SeCitaIzDrugogOcitavanja()
    {
        string path = Write("mirno.csv", "jedan,dva\r\n");

        StableReadResult result = await Reader().ReadAsync(path);

        Assert.True(result.IsOk);
        Assert.Equal("jedan,dva\r\n", Encoding.UTF8.GetString(result.Bytes!));

        // Prvo očitavanje samo zapamti veličinu; čita se tek kad se dva očitavanja slože.
        Assert.Equal(2, result.Attempts);
    }

    [Fact]
    public async Task FajlaNema_VracaMissing()
    {
        StableReadResult result = await Reader().ReadAsync(Path.Combine(_folder, "nema-ga.csv"));

        Assert.Equal(StableReadStatus.Missing, result.Status);
        Assert.Null(result.Bytes);
    }

    /// <summary>Fajl koji raste za sve vreme pokušaja ostaje za sledeći prolaz.</summary>
    [Fact]
    public async Task FajlKojiStalnoRaste_NeProglasavaSeGotovim()
    {
        string path = Write("raste.csv", "1");

        using var pisac = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

        var reader = new StableFileReader(
            KratkaOdlaganja,
            async (ms, ct) =>
            {
                // Između dva očitavanja fajl svaki put poraste — kao kad upis još traje.
                byte[] bytes = Encoding.UTF8.GetBytes("x");
                await pisac.WriteAsync(bytes, ct);
                await pisac.FlushAsync(ct);
                await Task.Delay(ms, ct);
            });

        StableReadResult result = await reader.ReadAsync(path);

        Assert.Equal(StableReadStatus.Growing, result.Status);
        Assert.Null(result.Bytes);
    }

    /// <summary>Zaključan fajl se ne čita, ali se ni ne proglašava greškom programa.</summary>
    [Fact]
    public async Task ZakljucanFajl_VracaLocked()
    {
        string path = Write("zakljucan.csv", "jedan,dva\r\n");

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            StableReadResult result = await Reader().ReadAsync(path);

            Assert.Equal(StableReadStatus.Locked, result.Status);
            Assert.NotNull(result.Error);
            Assert.Equal(Reader().MaxAttempts, result.Attempts);
        }
    }

    /// <summary>Kad se fajl oslobodi između dva pokušaja, čitanje uspeva bez ijednog izuzetka napolje.</summary>
    [Fact]
    public async Task FajlKojiSeOslobodi_SeIpakProcita()
    {
        string path = Write("oslobadja-se.csv", "jedan,dva\r\n");

        var brava = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        int pokusaj = 0;

        var reader = new StableFileReader(
            KratkaOdlaganja,
            async (ms, ct) =>
            {
                if (++pokusaj == 2)
                {
                    await brava.DisposeAsync();
                }

                await Task.Delay(ms, ct);
            });

        StableReadResult result = await reader.ReadAsync(path);

        Assert.True(result.IsOk);
        Assert.Equal("jedan,dva\r\n", Encoding.UTF8.GetString(result.Bytes!));
    }

    [Fact]
    public async Task Otkazivanje_PrekidaCitanje()
    {
        string path = Write("otkazivanje.csv", "jedan,dva\r\n");

        using var brava = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        using var cts = new CancellationTokenSource();

        var reader = new StableFileReader(
            KratkaOdlaganja,
            (ms, ct) =>
            {
                cts.Cancel();
                return Task.Delay(ms, ct);
            });

        StableReadResult result = await reader.ReadAsync(path, cts.Token);

        Assert.Equal(StableReadStatus.Cancelled, result.Status);
    }

    // -------------------------------------------------------------------------------------
    // Otisak sadržaja
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Otisak_IstogRedaJeIsti_ARazlicitogRazlicit()
    {
        string prvi = ResultHash.OfText("1,M100W1,pass,2026/09/07,11:44:11");
        string isti = ResultHash.OfText("1,M100W1,pass,2026/09/07,11:44:11");
        string drugi = ResultHash.OfText("2,M100W1,pass,2026/09/07,11:50:00");

        Assert.Equal(prvi, isti);
        Assert.NotEqual(prvi, drugi);
        Assert.Equal(64, prvi.Length);   // SHA-256 u heksadekadnom obliku
    }

    [Fact]
    public void Otisak_RezultataSeRacunaNadSirovimRedom()
    {
        var run = new TestRun { Seq = 1, SpecFileName = "M100W1", RawRow = "1,M100W1,pass" };
        var isti = new TestRun { Seq = 9, SpecFileName = "drugo", RawRow = "1,M100W1,pass" };

        Assert.Equal(ResultHash.Of(run), ResultHash.Of(isti));
    }

    /// <summary>Rezultat bez sirovog reda se prepoznaje po onome što ga određuje.</summary>
    [Fact]
    public void Otisak_BezSirovogReda_KoristiPodatkeRezultata()
    {
        var prvi = new TestRun { Seq = 1, SpecFileName = "M100W1", TestedAt = new DateTime(2026, 9, 7, 11, 44, 11) };
        var isti = new TestRun { Seq = 1, SpecFileName = "M100W1", TestedAt = new DateTime(2026, 9, 7, 11, 44, 11) };
        var drugi = new TestRun { Seq = 2, SpecFileName = "M100W1", TestedAt = new DateTime(2026, 9, 7, 11, 44, 11) };

        Assert.Equal(ResultHash.Of(prvi), ResultHash.Of(isti));
        Assert.NotEqual(ResultHash.Of(prvi), ResultHash.Of(drugi));
    }

    // -------------------------------------------------------------------------------------
    // Poništavanje ponovljenih događaja
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// Jedan upis podigne više događaja watcher-a; prolazi prvi, ostali u prozoru otpadaju.
    /// </summary>
    [Fact]
    public void Debounce_PustaPrviDogadjaj_ASledeceUProzoruOdbacuje()
    {
        var debounce = new PathDebounce(TimeSpan.FromMilliseconds(500));
        var pocetak = new DateTime(2026, 9, 7, 11, 44, 11, DateTimeKind.Utc);

        Assert.True(debounce.ShouldHandle(@"C:\rez\merenja.csv", pocetak));
        Assert.False(debounce.ShouldHandle(@"C:\rez\merenja.csv", pocetak.AddMilliseconds(100)));
        Assert.False(debounce.ShouldHandle(@"C:\rez\merenja.csv", pocetak.AddMilliseconds(499)));

        // Posle prozora se ponovo reaguje — sledeći upis nije isti upis.
        Assert.True(debounce.ShouldHandle(@"C:\rez\merenja.csv", pocetak.AddMilliseconds(500)));
    }

    [Fact]
    public void Debounce_RazliciteputanjeSeNePotiru()
    {
        var debounce = new PathDebounce(TimeSpan.FromMilliseconds(500));
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11, DateTimeKind.Utc);

        Assert.True(debounce.ShouldHandle(@"C:\rez\prvi.csv", trenutak));
        Assert.True(debounce.ShouldHandle(@"C:\rez\drugi.csv", trenutak));
    }

    [Fact]
    public void Debounce_Clear_ZaboravljaZapamceno()
    {
        var debounce = new PathDebounce(TimeSpan.FromMilliseconds(500));
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11, DateTimeKind.Utc);

        Assert.True(debounce.ShouldHandle(@"C:\rez\merenja.csv", trenutak));

        debounce.Clear();

        Assert.True(debounce.ShouldHandle(@"C:\rez\merenja.csv", trenutak));
    }

    /// <summary>Podrazumevani prozor je pola sekunde — toliko traje jedan upis sa više događaja.</summary>
    [Fact]
    public void Debounce_PodrazumevaniProzorJePolaSekunde()
        => Assert.Equal(TimeSpan.FromMilliseconds(500), FileBasedTesterGateway.DebounceWindow);

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private static StableFileReader Reader() => new(KratkaOdlaganja);

    private string Write(string name, string text)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }
}
