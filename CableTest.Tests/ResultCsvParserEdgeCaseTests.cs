using CableTest.Core.Model;
using CableTest.Core.Results;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Granični slučajevi koje ćemo sigurno sresti u radu, jer aplikacija čita fajl koji drugi program
/// u istom trenutku pravi i dopisuje.
/// </summary>
/// <remarks>
/// Pravilo za sve njih: parser ne sme da baci izuzetak. Rezultat je prazan ili je uz njega
/// upozorenje — nadgledanje CSV-a mora da preživi svaki oblik fajla, jer se pokreće automatski
/// i operater nema kako da ga "popravi".
/// </remarks>
public class ResultCsvParserEdgeCaseTests
{
    private const string CrLf = "\r\n";

    private const string Header =
        "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit," + CrLf;

    [Fact]
    public void SamoZaglavljeKolona_BezIjednogRezultata()
    {
        CsvParseResult result = ResultCsvParser.ParseAll(Header);

        Assert.Empty(result.Runs);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void SamoZaglavljeFajlaIZaglavljeKolona_BezIjednogRezultata()
    {
        string csv =
            "File Name: M100W1" + CrLf +
            "Model:8761NK" + CrLf +
            "O/S:5KOHM" + CrLf +
            "-------------------------------" + CrLf +
            "001 O01-O02-O31-O32" + CrLf +
            "-------------------------------" + CrLf +
            Header;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Empty(result.Runs);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void PrazanFajl_BezIzuzetka()
    {
        CsvParseResult result = ResultCsvParser.ParseAll(string.Empty);

        Assert.Empty(result.Runs);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void PrazanFajlSaDiska_NulaBajtova_BezIzuzetka()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        File.WriteAllBytes(path, Array.Empty<byte>());

        try
        {
            string text = CsvTextReader.ReadAllText(path);
            CsvParseResult result = ResultCsvParser.ParseAll(text);

            Assert.Empty(result.Runs);
            Assert.Empty(result.Warnings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SamoPrviDeoZaglavljaFajla_UhvacenUTrenutkuKreiranja()
    {
        // CableConnector je upravo napravio fajl; upisano je tek nekoliko redova zaglavlja.
        string csv = "File Name: M100W1" + CrLf + "Model:8761NK" + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Empty(result.Runs);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void NedovrsenRedSaImenimaKolona_SeCekaDaSeDopuni()
    {
        // Red sa imenima kolona je uhvaćen usred upisa, bez preloma reda na kraju.
        var parser = new ResultCsvParser();
        CsvParseResult partial = parser.ReadNew("Seq.,Filename,Pass,Date,Ti");

        Assert.Empty(partial.Runs);
        Assert.Empty(partial.Warnings);
        Assert.False(parser.HasHeader);

        // Kad se red dopuni i stigne prvi rezultat, čita se normalno.
        CsvParseResult complete = parser.ReadNew(
            Header + "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS," + CrLf);

        Assert.Single(complete.Runs);
        Assert.Equal("M100W1", complete.Runs[0].SpecFileName);
    }

    [Fact]
    public void ZaglavljeBezIjednogRezultata_PaPrviRezultatKasnije()
    {
        var parser = new ResultCsvParser();

        Assert.Empty(parser.ReadNew(Header).Runs);
        Assert.True(parser.HasHeader);

        TestRun run = Assert.Single(
            parser.ReadNew(Header + "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS," + CrLf).Runs);

        Assert.Equal(1, run.Seq);
    }

    /// <summary>
    /// Red sa manje polja nego što zaglavlje najavljuje. Ovo nije greška u fajlu — stvarni fajl
    /// iz pogona izostavlja poslednju kolonu ("unit"). Polja koja nedostaju se čitaju kao prazna.
    /// </summary>
    [Fact]
    public void RedSaManjeKolonaNegoZaglavlje_CitaSeBezIzuzetka()
    {
        string csv = Header + "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš" + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);
        TestRun run = Assert.Single(result.Runs);

        Assert.Equal(1, run.Seq);
        Assert.Equal("M100W1", run.SpecFileName);
        Assert.True(run.Passed);
        Assert.Equal("Miloš", run.Operator);
        Assert.Equal(string.Empty, run.Barcode);
        Assert.Empty(run.Defects);
        Assert.False(result.HasWarnings);
    }

    /// <summary>Red kome fale i datum i ishod — čita se, ali uz upozorenja.</summary>
    [Fact]
    public void RedSaSamoDvaPolja_DajeUpozorenjaAliNePada()
    {
        string csv = Header + "1,M100W1" + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);
        TestRun run = Assert.Single(result.Runs);

        Assert.Equal(1, run.Seq);
        Assert.False(run.Passed);
        Assert.Equal(DateTime.MinValue, run.TestedAt);
        Assert.Empty(run.Defects);
        Assert.Contains(result.Warnings, w => w.Contains("ishod", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, w => w.Contains("datum", StringComparison.Ordinal));
    }

    /// <summary>
    /// Red sa više polja nego što zaglavlje najavljuje — skup kolona se promenio bez novog
    /// zaglavlja. Upozorenje mora da postoji, a vrednost iz nenajavljene kolone se ne gubi.
    /// </summary>
    [Fact]
    public void RedSaViseKolonaNegoZaglavlje_DajeUpozorenjeIneGubiGresku()
    {
        string csv = Header +
            "1,M100W1,fail,2026/09/07,11:44:11,,,Miloš,PASS,PASS,OHM,SHORT O01-O02,PASS," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);
        TestRun run = Assert.Single(result.Runs);

        Assert.Equal(1, run.Seq);
        Assert.Contains(result.Warnings, w => w.Contains("polja", StringComparison.Ordinal));

        TestDefect defect = Assert.Single(run.Defects);
        Assert.Equal(DefectKind.Short, defect.Kind);
        Assert.Equal("O01-O02", defect.Points);
    }

    /// <summary>Prazne kolone viška (fajl završava sa nekoliko zareza) nisu razlog za upozorenje.</summary>
    [Fact]
    public void RedSaPraznimKolonamaViska_NeDajeUpozorenje()
    {
        string csv = Header + "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS,OHM,,," + CrLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Single(result.Runs);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void SamoPrelomiReda_BezIzuzetka()
    {
        CsvParseResult result = ResultCsvParser.ParseAll(CrLf + CrLf + CrLf);

        Assert.Empty(result.Runs);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void RedSaSamimZarezima_NijeRezultatIneDajeUpozorenje()
    {
        // Prazan red iz CableConnector-a: sve kolone prazne.
        CsvParseResult result = ResultCsvParser.ParseAll(Header + ",,,,,,,,,,," + CrLf);

        Assert.Empty(result.Runs);
        Assert.Empty(result.Warnings);
    }
}
