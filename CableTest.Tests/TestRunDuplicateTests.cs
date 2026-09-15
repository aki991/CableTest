using CableTest.Core.Model;
using CableTest.Core.Results;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Zaštita od duplikata po prirodnom ključu <c>SpecFileName</c> + <c>Seq</c> + <c>TestedAt</c>.
/// </summary>
/// <remarks>
/// Rizik je stvaran: parser, kad fajl postane kraći nego ranije, čita ga od početka. Ako
/// CableConnector prepiše fajl istim imenom umesto da napravi novi, isti redovi dolaze dvaput.
/// Istorija testova mora biti tačna.
/// </remarks>
public class TestRunDuplicateTests
{
    private const string CrLf = "\r\n";

    private static readonly DateTime Trenutak = new(2026, 9, 7, 11, 44, 11);

    [Fact]
    public void RemoveDuplicates_ZadrzavaPrviZapisIstogKljuca()
    {
        var runs = new[]
        {
            Run("M100W1", 1, Trenutak, "prvi"),
            Run("M100W1", 1, Trenutak, "drugi"),
            Run("M100W1", 2, Trenutak.AddMinutes(3), "treci")
        };

        var warnings = new List<string>();
        List<TestRun> unique = ResultCsvParser.RemoveDuplicates(runs, warnings);

        Assert.Equal(2, unique.Count);
        Assert.Equal("prvi", unique[0].Barcode);
        Assert.Equal("treci", unique[1].Barcode);
        Assert.Single(warnings);
        Assert.Contains("duplikat", warnings[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveDuplicates_RazlicitoVremeNijeDuplikat()
    {
        var runs = new[]
        {
            Run("M100W1", 1, Trenutak),
            Run("M100W1", 1, Trenutak.AddSeconds(1))
        };

        Assert.Equal(2, ResultCsvParser.RemoveDuplicates(runs).Count);
    }

    [Fact]
    public void RemoveDuplicates_RazlicitSpecFajlNijeDuplikat()
    {
        var runs = new[]
        {
            Run("M100W1", 1, Trenutak),
            Run("M100W2", 1, Trenutak)
        };

        Assert.Equal(2, ResultCsvParser.RemoveDuplicates(runs).Count);
    }

    [Fact]
    public void RemoveDuplicates_ImeSpecFajlaSePorediBezObziraNaVelikaSlovaIEkstenziju()
    {
        var runs = new[]
        {
            Run("M100W1", 1, Trenutak),
            Run("m100w1", 1, Trenutak),
            Run("M100W1.c61", 1, Trenutak)
        };

        Assert.Single(ResultCsvParser.RemoveDuplicates(runs));
    }

    [Fact]
    public void RemoveDuplicates_BezUpozorenjaRadiIsto()
    {
        var runs = new[] { Run("M100W1", 1, Trenutak), Run("M100W1", 1, Trenutak) };

        Assert.Single(ResultCsvParser.RemoveDuplicates(runs));
    }

    [Fact]
    public void RemoveDuplicates_PraznaListaDajePraznuListu()
        => Assert.Empty(ResultCsvParser.RemoveDuplicates(Array.Empty<TestRun>()));

    /// <summary>Slučaj zbog koga ovo postoji: isti fajl pročitan dvaput jer je prepisan istim imenom.</summary>
    [Fact]
    public void PonovoProcitanPrepisanFajl_DajeDuplikateKojeRemoveDuplicatesUklanja()
    {
        string csv =
            "Seq.,Filename,Pass,Date,Time,Operater,O/S TEST," + CrLf +
            "1,M100W1,pass,2026/09/07,11:44:11,Miloš,PASS," + CrLf +
            "2,M100W1,fail,2026/09/07,11:47:34,Miloš,SHORT O01-O02;FAIL," + CrLf;

        var parser = new ResultCsvParser();
        List<TestRun> all = new(parser.ReadNew(csv).Runs);

        // CableConnector je prepisao fajl istim imenom: isti sadržaj, kraći od zapamćenog stanja
        // nije — ali parser je resetovan rotacijom i čita sve ponovo.
        parser.Reset();
        all.AddRange(parser.ReadNew(csv).Runs);

        Assert.Equal(4, all.Count);

        var warnings = new List<string>();
        List<TestRun> unique = ResultCsvParser.RemoveDuplicates(all, warnings);

        Assert.Equal(2, unique.Count);
        Assert.Equal(2, warnings.Count);
        Assert.Equal(new[] { 1, 2 }, unique.Select(r => r.Seq));
    }

    [Fact]
    public void TestRunKey_JednakZaIstiZapis()
    {
        Assert.Equal(TestRunKey.Of(Run("M100W1", 1, Trenutak)), TestRunKey.Of("m100w1.c61", 1, Trenutak));
        Assert.NotEqual(TestRunKey.Of(Run("M100W1", 1, Trenutak)), TestRunKey.Of("M100W1", 2, Trenutak));
    }

    [Fact]
    public void TestRunKey_OpisSadrziSveTriVrednosti()
    {
        string text = TestRunKey.Of("M100W1", 7, Trenutak).ToString();

        Assert.Contains("M100W1", text, StringComparison.Ordinal);
        Assert.Contains("7", text, StringComparison.Ordinal);
        Assert.Contains("2026-09-07 11:44:11", text, StringComparison.Ordinal);
    }

    private static TestRun Run(string specFileName, int seq, DateTime testedAt, string barcode = "")
        => new()
        {
            SpecFileName = specFileName,
            Seq = seq,
            TestedAt = testedAt,
            Barcode = barcode
        };
}
