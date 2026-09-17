using CableTest.Core.Configuration;
using CableTest.Core.Model;
using CableTest.Core.Reporting;
using CableTest.Core.Results;
using Xunit;

namespace CableTest.Tests;

/// <summary>Podešavanja, provera putanja i merna karta.</summary>
public sealed class ConfigurationAndReportingTests : IDisposable
{
    private readonly string _folder;

    public ConfigurationAndReportingTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestCfg_" + Path.GetRandomFileName());
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

    // -----------------------------------------------------------------------------------
    // settings.json
    // -----------------------------------------------------------------------------------

    [Fact]
    public void Podesavanja_FajlNePostoji_DajePodrazumevaneVrednosti()
    {
        var store = new SettingsStore(Path.Combine(_folder, "settings.json"));

        AppSettings settings = store.Load();

        Assert.Null(store.LastError);
        Assert.Equal(@"C:\Cable Linker8761\spec", settings.SpecFolder);
        Assert.Equal(AppSettings.DefaultResultPath, settings.ResultPath);
        Assert.True(settings.SoundEnabled);
        Assert.False(settings.DemoMode);
        Assert.False(File.Exists(store.FilePath));  // fajl se ne pravi dok se nešto ne sačuva
    }

    [Fact]
    public void Podesavanja_SeCuvajuICitaju()
    {
        var store = new SettingsStore(Path.Combine(_folder, "settings.json"));

        var settings = new AppSettings
        {
            SpecFolder = @"D:\spec",
            ResultPath = @"D:\rezultati",
            OperatorName = "Miloš",
            SoundEnabled = false,
            DemoMode = true
        };

        Assert.True(store.Save(settings));

        AppSettings procitano = new SettingsStore(store.FilePath).Load();

        Assert.Equal(@"D:\spec", procitano.SpecFolder);
        Assert.Equal(@"D:\rezultati", procitano.ResultPath);
        Assert.Equal("Miloš", procitano.OperatorName);
        Assert.False(procitano.SoundEnabled);
        Assert.True(procitano.DemoMode);
    }

    [Fact]
    public void Podesavanja_OstecenFajl_NeRusiAplikaciju()
    {
        string path = Path.Combine(_folder, "settings.json");
        File.WriteAllText(path, "{ ovo nije ispravan json");

        var store = new SettingsStore(path);
        AppSettings settings = store.Load();

        Assert.NotNull(store.LastError);
        Assert.Equal(@"C:\Cable Linker8761\spec", settings.SpecFolder);
    }

    [Fact]
    public void Podesavanja_NepotpunFajl_DopunjavaPodrazumevanim()
    {
        string path = Path.Combine(_folder, "settings.json");
        File.WriteAllText(path, "{ \"OperatorName\": \"Miloš\" }");

        AppSettings settings = new SettingsStore(path).Load();

        Assert.Equal("Miloš", settings.OperatorName);
        Assert.Equal(AppSettings.DefaultResultPath, settings.ResultPath);
    }

    [Fact]
    public void Podesavanja_PodrazumevanaPutanjaJeUAppData()
    {
        Assert.EndsWith(
            Path.Combine("CableTest", "settings.json"),
            SettingsStore.DefaultFilePath,
            StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------------------
    // Provera putanja
    // -----------------------------------------------------------------------------------

    [Fact]
    public void OneDrivePutanja_DajeUpozorenjeSaObjasnjenjem()
    {
        IReadOnlyList<PathIssue> issues = PathCheck.CheckSpecFolder(@"C:\Users\Andreja\OneDrive\spec");

        // Putanja se pominje i u poruci da folder ne postoji, pa se bira po objašnjenju.
        PathIssue oneDrive = Assert.Single(
            issues,
            i => i.Message.Contains("Could not find file", StringComparison.Ordinal));
        Assert.True(oneDrive.IsWarning);
        Assert.Contains("Could not find file", oneDrive.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(@"C:\Users\Andreja\OneDrive\spec", true)]
    [InlineData(@"C:\Users\Andreja\onedrive - firma\spec", true)]
    [InlineData(@"C:\Cable Linker8761\spec", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsInOneDrive_PrepoznajePutanju(string? path, bool ocekivano)
        => Assert.Equal(ocekivano, PathCheck.IsInOneDrive(path));

    [Fact]
    public void SpecFolder_KojiPostojiIUpisivJe_NemaNalaza()
        => Assert.Empty(PathCheck.CheckSpecFolder(_folder));

    [Fact]
    public void SpecFolder_KojiNePostoji_DajeGresku()
    {
        PathIssue issue = Assert.Single(PathCheck.CheckSpecFolder(Path.Combine(_folder, "nema-ga")));

        Assert.True(issue.IsError);
        Assert.Contains("ne postoji", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SpecFolder_KojiNijePodesen_DajeGresku()
    {
        PathIssue issue = Assert.Single(PathCheck.CheckSpecFolder(" "));

        Assert.True(issue.IsError);
    }

    [Fact]
    public void PutanjaRezultata_PostojeciFajl_NemaNalaza()
    {
        string path = Path.Combine(_folder, "rezultati.csv");
        File.WriteAllText(path, "Seq.,Filename,");

        Assert.Empty(PathCheck.CheckResultPath(path));
    }

    [Fact]
    public void PutanjaRezultata_FolderBezCsvFajlova_DajeUpozorenje()
    {
        PathIssue issue = Assert.Single(PathCheck.CheckResultPath(_folder));

        Assert.True(issue.IsWarning);
        Assert.Contains("nema nijednog .csv", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PutanjaRezultata_KojaNijePodesena_DajeGresku()
    {
        PathIssue issue = Assert.Single(PathCheck.CheckResultPath(null));

        Assert.True(issue.IsError);
        Assert.Contains("neće stizati", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sablon_PoredIzvrsnogFajla_JePronadjenIIspravan()
    {
        PathIssue issue = PathCheck.CheckTemplate(null);

        Assert.Equal(PathIssueSeverity.Ok, issue.Severity);
        Assert.Contains("pronađen i ispravan", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sablon_KojiNePostoji_DajeGresku()
    {
        PathIssue issue = PathCheck.CheckTemplate(Path.Combine(_folder, "NEMA.c61"));

        Assert.True(issue.IsError);
        Assert.Contains("nije pronađen", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sablon_KojiNijeIspravan_DajeGresku()
    {
        string path = Path.Combine(_folder, "LOS.c61");
        File.WriteAllText(path, "ovo nije c61 fajl");

        PathIssue issue = PathCheck.CheckTemplate(path);

        Assert.True(issue.IsError);
        Assert.Contains("nije ispravan", issue.Message, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------------------
    // Merna karta
    // -----------------------------------------------------------------------------------

    [Fact]
    public void MernaKarta_SadrziSveTrazenePodatke()
    {
        string html = MeasurementCard.BuildHtml(Karta());

        Assert.Contains("Miloš", html, StringComparison.Ordinal);            // vozilo i operater
        Assert.Contains("M100-W1", html, StringComparison.Ordinal);          // oznaka kabla
        Assert.Contains("Kabl motorskog prostora", html, StringComparison.Ordinal);  // opis
        Assert.Contains("O01-O02-O31-O32", html, StringComparison.Ordinal);  // net lista
        Assert.Contains("07.09.2026. 11:47:34", html, StringComparison.Ordinal);
        Assert.Contains("NEISPRAVAN", html, StringComparison.Ordinal);       // ukupan ishod
        Assert.Contains("Kratak spoj između tačaka O01 i O02", html, StringComparison.Ordinal);
        Assert.Contains("Kontrolor", html, StringComparison.Ordinal);        // mesto za potpis
    }

    /// <summary>
    /// Karta ide u arhivu i čita se kasnije, kad se adapter i dokumentacija možda već promene.
    /// Zato na njoj moraju da stoje oznaka sa crteža, tip, dužina, izvor dokumentacije i tabela
    /// provodnika — i napomena da je priključna tabela bila privremena.
    /// </summary>
    [Fact]
    public void MernaKarta_SadrziPodatkeSaCrtezaITabeluProvodnika()
    {
        // HTML kodira sve što nije ASCII (mm², →, ž), pa se poredi nad dekodiranim tekstom.
        string text = System.Net.WebUtility.HtmlDecode(MeasurementCard.BuildHtml(KartaSaOzicenjem()));

        Assert.Contains("=40-W2", text, StringComparison.Ordinal);
        Assert.Contains("Snop FLRY 2x0,75 mm²", text, StringComparison.Ordinal);
        Assert.Contains("5,9 m", text, StringComparison.Ordinal);
        Assert.Contains("40_grupa_2_0.pdf, strana 7", text, StringComparison.Ordinal);

        Assert.Contains("Provodnici", text, StringComparison.Ordinal);
        Assert.Contains("BR (braon)", text, StringComparison.Ordinal);
        Assert.Contains("0,75 mm²", text, StringComparison.Ordinal);
        Assert.Contains("10XB:16", text, StringComparison.Ordinal);

        // Greška je prevedena na jezik crteža, uz tačku testera u zagradi.
        Assert.Contains("Prekid: DIN:2 → 10XB:16, žica BR (A02–B01)", text, StringComparison.Ordinal);

        Assert.Contains("Priključna tabela je privremena", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_BezPrivremenihDodela_NemaNapomenuOAdapteru()
    {
        MeasurementCardData data = KartaSaOzicenjem();

        foreach (CableTerminal terminal in data.Cable!.Terminals)
        {
            terminal.IsProvisional = false;
        }

        Assert.DoesNotContain(
            "Priključna tabela je privremena",
            MeasurementCard.BuildHtml(data),
            StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_ImaStilZaStampuA4()
    {
        string html = MeasurementCard.BuildHtml(Karta());

        Assert.Contains("@page { size: A4;", html, StringComparison.Ordinal);
        Assert.Contains("@media print", html, StringComparison.Ordinal);
        Assert.Contains(".stampaj { display: none; }", html, StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_ProlazanTest_NemaSpiskaGresaka()
    {
        MeasurementCardData data = Karta();
        data.Run.Passed = true;
        data.Run.Defects.Clear();

        string html = MeasurementCard.BuildHtml(data);

        Assert.Contains("ISPRAVAN", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Pronađene greške", html, StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_TekstIzCsvSeEskejpuje()
    {
        MeasurementCardData data = Karta();
        data.Run.RawRow = "2,<script>alert(1)</script>,fail";

        string html = MeasurementCard.BuildHtml(data);

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_DemoNapomenaSeVidi()
    {
        MeasurementCardData data = Karta();
        var sa = new MeasurementCardData
        {
            Run = data.Run,
            Cable = data.Cable,
            Vehicle = data.Vehicle,
            Note = "DEMO REŽIM — rezultat je simuliran."
        };

        Assert.Contains("DEMO REŽIM", MeasurementCard.BuildHtml(sa), StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_SeUpisujeUFajlSaImenomKojeSadrziKablIVreme()
    {
        string path = MeasurementCard.WriteToFile(Karta(), _folder);

        Assert.True(File.Exists(path));
        Assert.Contains("M100W1", Path.GetFileName(path), StringComparison.Ordinal);
        Assert.Contains("20260907", Path.GetFileName(path), StringComparison.Ordinal);
        Assert.EndsWith(".html", path, StringComparison.Ordinal);
        Assert.Contains("Merna karta", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void MernaKarta_NeprepoznatDatum_SeJasnoOznacava()
    {
        MeasurementCardData data = Karta();
        data.Run.TestedAt = DateTime.MinValue;

        Assert.Contains("vreme nije prepoznato", MeasurementCard.BuildHtml(data), StringComparison.Ordinal);
    }

    /// <summary>Merna karta kabla sa crteža: =40-W2, sa ožičenjem i privremenom priključnom tabelom.</summary>
    private static MeasurementCardData KartaSaOzicenjem()
    {
        var cable = new Cable
        {
            Id = 2,
            Code = "40-W2",
            Description = "Snop FLRY, DIN 72585 pod uglom",
            SpecFileName = "40W2",
            Designation = "=40-W2",
            CableType = "Snop FLRY 2x0,75 mm²",
            LengthM = 5.9m,
            SourceDocument = "40_grupa_2_0.pdf",
            SourcePage = 7
        };

        cable.Terminals.Add(new CableTerminal
        {
            Label = "DIN:2", Side = CableSide.A, ContactType = "DIN 72585 pin",
            TesterPoint = "A02", IsProvisional = true
        });
        cable.Terminals.Add(new CableTerminal
        {
            Label = "10XB:16", Side = CableSide.B, ContactType = "pin 10XB (postavlja se na vozilu)",
            TesterPoint = "B01", IsProvisional = true
        });

        cable.Wires.Add(new CableWire
        {
            WireNo = 1, Color = "BR", CrossSectionMm2 = 0.75m,
            FromTerminal = "DIN:2", ToTerminal = "10XB:16"
        });

        var run = new TestRun
        {
            Seq = 3,
            SpecFileName = "40W2",
            Passed = false,
            TestedAt = new DateTime(2026, 9, 7, 11, 47, 34),
            Operator = "Miloš"
        };

        run.Defects.Add(ResultCsvParser.CreateDefect("OPEN A02-B01"));

        return new MeasurementCardData
        {
            Run = run,
            Cable = cable,
            Vehicle = new Vehicle { Id = 1, Name = "Miloš Veliki" }
        };
    }

    private static MeasurementCardData Karta()
    {
        var cable = new Cable
        {
            Id = 1,
            Code = "M100-W1",
            Description = "Kabl motorskog prostora",
            SpecFileName = "M100W1"
        };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02-O31-O32" });

        var run = new TestRun
        {
            Seq = 2,
            SpecFileName = "M100W1",
            Passed = false,
            TestedAt = new DateTime(2026, 9, 7, 11, 47, 34),
            Operator = "Miloš",
            RawRow = "2,M100W1,fail,2026/09/07,11:47:34"
        };
        run.Defects.Add(ResultCsvParser.CreateDefect("SHORT O01-O02"));

        return new MeasurementCardData
        {
            Run = run,
            Cable = cable,
            Vehicle = new Vehicle { Id = 1, Name = "Miloš" }
        };
    }
}
