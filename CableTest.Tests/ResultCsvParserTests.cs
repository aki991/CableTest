using System.Globalization;
using CableTest.Core.Model;
using CableTest.Core.Results;
using Xunit;

namespace CableTest.Tests;

public class ResultCsvParserTests
{
    private const string CrLf = "\r\n";

    /// <summary>
    /// Stvarni fajl iz pogona: dva testa, drugi sa tri kratka spoja.
    /// </summary>
    /// <remarks>Isti uzorak koriste i testovi veze sa testerom; vidi <see cref="CsvSamples"/>.</remarks>
    private const string RealSample = CsvSamples.RealFile;

    [Fact]
    public void ParseAll_CitaObaRedaIzStvarnogFajla()
    {
        CsvParseResult result = ResultCsvParser.ParseAll(RealSample);

        Assert.Equal(2, result.Runs.Count);
        Assert.False(result.HasWarnings);

        TestRun first = result.Runs[0];
        Assert.Equal(1, first.Seq);
        Assert.Equal("PROBA1", first.SpecFileName);
        Assert.True(first.Passed);
        Assert.Equal("Andreja", first.Operator);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 44, 11), first.TestedAt);
        Assert.Empty(first.Defects);
        Assert.StartsWith("1,PROBA1,pass,", first.RawRow, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAll_IzvlaciKratkeSpojeveITacke()
    {
        TestRun run = ResultCsvParser.ParseAll(RealSample).Runs[1];

        Assert.False(run.Passed);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 47, 34), run.TestedAt);
        Assert.Equal(3, run.Defects.Count);
        Assert.All(run.Defects, d => Assert.Equal(DefectKind.Short, d.Kind));
        Assert.Equal(new[] { "O01-O02", "O01-O31", "O01-O32" }, run.Defects.Select(d => d.Points));
        Assert.Equal("SHORT O01-O02", run.Defects[0].RawText);
        Assert.Equal("Kratak spoj između tačaka O01 i O02", run.Defects[0].Describe());
    }

    [Fact]
    public void Parse_PrepoznajeOpen()
    {
        string csv = Header() + "1,PROBA1,fail,2026/09/07,11:44:11,,,Andreja,FAIL,OPEN O01-O31;FAIL," + CrLf;

        TestDefect defect = Assert.Single(ResultCsvParser.ParseAll(csv).Runs[0].Defects);

        Assert.Equal(DefectKind.Open, defect.Kind);
        Assert.Equal("O01-O31", defect.Points);
        Assert.Equal("Prekid između tačaka O01 i O31", defect.Describe());
    }

    [Fact]
    public void Parse_NepoznataPorukaSeCuvaKaoSirovTekst()
    {
        string csv = Header() + "1,PROBA1,fail,2026/09/07,11:44:11,,,Andreja,FAIL,HV LEAKAGE O05;FAIL," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);
        TestDefect defect = Assert.Single(result.Runs[0].Defects);

        Assert.Equal(DefectKind.Unknown, defect.Kind);
        Assert.Equal("HV LEAKAGE O05", defect.RawText);
        Assert.Equal(string.Empty, defect.Points);
        Assert.Contains("HV LEAKAGE O05", defect.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_PrepoznatTipSaNecitljivimTackama_OstajeSirovTekst()
    {
        string csv = Header() + "1,PROBA1,fail,2026/09/07,11:44:11,,,Andreja,FAIL,SHORT Q99-XYZ;FAIL," + CrLf;

        TestDefect defect = Assert.Single(ResultCsvParser.ParseAll(csv).Runs[0].Defects);

        Assert.Equal(DefectKind.Short, defect.Kind);
        Assert.Equal(string.Empty, defect.Points);
        Assert.Equal("SHORT Q99-XYZ", defect.RawText);
    }

    [Fact]
    public void Parse_DodatneKoloneNePomerajuCitanje()
    {
        // Uključena dodatna test stavka: kolone COND TEST i merena provodna otpornost.
        string csv =
            "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,COND TEST,COND VALUE,unit," + CrLf +
            "1,M100-W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS,PASS,0.12,OHM," + CrLf +
            "2,M100-W1,fail,2026/09/07,11:47:34,,,Miloš,FAIL,SHORT O01-O02;FAIL,PASS,0.13,OHM," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Equal(2, result.Runs.Count);
        Assert.False(result.HasWarnings);
        Assert.Equal("Miloš", result.Runs[0].Operator);
        Assert.Equal("M100-W1", result.Runs[0].SpecFileName);
        Assert.Empty(result.Runs[0].Defects);  // merena vrednost 0.12 nije greška

        TestDefect defect = Assert.Single(result.Runs[1].Defects);
        Assert.Equal(DefectKind.Short, defect.Kind);
        Assert.Equal("O01-O02", defect.Points);
    }

    [Fact]
    public void Parse_KoloneUDrugomRedosledu_SeCitajuPoImenu()
    {
        string csv =
            "Seq.,Pass,Operater,Filename,Barcode1,Date,Time,O/S TEST," + CrLf +
            "7,pass,Miloš,M100-W1,ABC-123,2026/09/07,11:44:11,PASS," + CrLf;

        TestRun run = Assert.Single(ResultCsvParser.ParseAll(csv).Runs);

        Assert.Equal(7, run.Seq);
        Assert.True(run.Passed);
        Assert.Equal("Miloš", run.Operator);
        Assert.Equal("M100-W1", run.SpecFileName);
        Assert.Equal("ABC-123", run.Barcode);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 44, 11), run.TestedAt);
    }

    [Fact]
    public void ReadNew_VracaSamoNoveRedove()
    {
        var parser = new ResultCsvParser();

        Assert.Equal(2, parser.ReadNew(RealSample).Runs.Count);
        Assert.Empty(parser.ReadNew(RealSample).Runs);

        string grown = RealSample + "3,PROBA1,pass,2026/09/07,12:01:00,,,Andreja,PASS,PASS," + CrLf;
        TestRun run = Assert.Single(parser.ReadNew(grown).Runs);

        Assert.Equal(3, run.Seq);
    }

    [Fact]
    public void ReadNew_NepotpunPoslednjiRedSeCekaDaSeDopuni()
    {
        var parser = new ResultCsvParser();

        // Fajl uhvaćen usred upisa — poslednji red nema prelom.
        string partial = RealSample + "3,PROBA1,pa";
        Assert.Equal(2, parser.ReadNew(partial).Runs.Count);

        string complete = RealSample + "3,PROBA1,pass,2026/09/07,12:01:00,,,Andreja,PASS,PASS," + CrLf;
        TestRun run = Assert.Single(parser.ReadNew(complete).Runs);

        Assert.Equal(3, run.Seq);
        Assert.True(run.Passed);
    }

    [Fact]
    public void ReadNew_ZamenjenFajl_SeCitaOdPocetkaUzUpozorenje()
    {
        var parser = new ResultCsvParser();
        parser.ReadNew(RealSample);

        // Dnevna rotacija: na istoj putanji je nov, kraći fajl.
        string fresh = Header() + "1,PROBA1,pass,2026/09/08,07:15:00,,,Andreja,PASS,PASS," + CrLf;
        CsvParseResult result = parser.ReadNew(fresh);

        Assert.Single(result.Runs);
        Assert.Contains(result.Warnings, w => w.Contains("zamenjen", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadNew_DrugoZaglavljeUIstomFajlu_MenjaKolone()
    {
        string csv =
            RealSample +
            "Seq.,Filename,Pass,Date,Time,Operater,O/S TEST," + CrLf +
            "1,M100-W1,fail,2026/09/07,13:00:00,Miloš,SHORT A01-A02;FAIL," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Equal(3, result.Runs.Count);
        Assert.Equal("M100-W1", result.Runs[2].SpecFileName);
        Assert.Equal("Miloš", result.Runs[2].Operator);
        Assert.Equal("A01-A02", Assert.Single(result.Runs[2].Defects).Points);
    }

    [Fact]
    public void Parse_NeispravanDatum_DajeUpozorenjeAliNePrekidaObradu()
    {
        string csv =
            Header() +
            "1,PROBA1,pass,07.09.2026,11:44:11,,,Andreja,PASS,PASS," + CrLf +
            "2,PROBA1,pass,2026/09/07,11:45:00,,,Andreja,PASS,PASS," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Equal(2, result.Runs.Count);
        Assert.Equal(DateTime.MinValue, result.Runs[0].TestedAt);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 45, 0), result.Runs[1].TestedAt);
        Assert.Contains(result.Warnings, w => w.Contains("datum", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_NepoznatIshodUKoloniPass_DajeUpozorenjeIFail()
    {
        string csv = Header() + "1,PROBA1,ABORT,2026/09/07,11:44:11,,,Andreja,FAIL,FAIL," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.False(Assert.Single(result.Runs).Passed);
        Assert.Contains(result.Warnings, w => w.Contains("ABORT", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_RedKojiNijeRezultat_DajeUpozorenjeAliNePada()
    {
        string csv = Header() + "ovo nije rezultat" + CrLf + "1,PROBA1,pass,2026/09/07,11:44:11,,,Andreja,PASS,PASS," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Single(result.Runs);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Parse_PraznoIliSamoZaglavlje_NeDajeRezultate()
    {
        Assert.Empty(ResultCsvParser.ParseAll(string.Empty).Runs);
        Assert.Empty(ResultCsvParser.ParseAll("File Name: PROBA1" + CrLf + "Model:8761NK" + CrLf).Runs);
        Assert.Empty(ResultCsvParser.ParseAll(Header()).Runs);
    }

    [Fact]
    public void SplitCsvLine_PodrzavaNavodnikeIZarezUPolju()
    {
        string[] fields = ResultCsvParser.SplitCsvLine("1,\"PROBA,1\",pass,\"kaže \"\"ok\"\"\",");

        Assert.Equal(new[] { "1", "PROBA,1", "pass", "kaže \"ok\"", string.Empty }, fields);
    }

    [Fact]
    public void Parse_NaSrpskojKulturi_CitaDatumIVremeIsto()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sr-RS");

            CsvParseResult result = ResultCsvParser.ParseAll(RealSample);

            Assert.Equal(new DateTime(2026, 9, 7, 11, 44, 11), result.Runs[0].TestedAt);
            Assert.Equal(new DateTime(2026, 9, 7, 11, 47, 34), result.Runs[1].TestedAt);
            Assert.Equal(1, result.Runs[0].Seq);
            Assert.False(result.HasWarnings);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Parse_RezultatSeVezujeZaKablPrekoImenaSpecFajla()
    {
        var cables = new[]
        {
            new Cable { Id = 5, Code = "M100-W1", SpecFileName = "M100-W1" }
        };

        TestRun[] runs = ResultCsvParser.ParseAll(RealSample).Runs.ToArray();
        foreach (TestRun run in runs)
        {
            run.CableId = Cable.FindByResultFileName(cables, run.SpecFileName)?.Id;
        }

        // "PROBA1" nije nijedan kabl iz baze — rezultat se čuva kao neprepoznat.
        Assert.All(runs, r => Assert.False(r.IsRecognized));
    }

    private static string Header()
        => "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit," + CrLf;
}
