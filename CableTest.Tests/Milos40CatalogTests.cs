using CableTest.Core.Data;
using CableTest.Core.Model;
using CableTest.Core.Spec;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Kablovi vozila „Miloš Veliki“ iz elektro dokumentacije (crtež 40_grupa_2_0.pdf, 11.2.2021).
/// </summary>
/// <remarks>
/// <para>
/// Radi nad pravom bazom u privremenom fajlu, sa primenjenim migracijama — dakle nad onim što
/// operater zaista dobije. Net lista se ne poredi sa onim što je negde upisano nego sa onim što
/// je <b>izvedeno</b> iz ožičenja i priključne tabele; upravo to izvođenje je ovde i predmet
/// provere, jer o njemu zavisi da li program koji ide u tester odgovara kablu na stolu.
/// </para>
/// <para>
/// Očekivani netovi su prepisani iz zadatka, ne iz koda — ako se izvođenje promeni, test to
/// mora da primeti.
/// </para>
/// </remarks>
public sealed class Milos40CatalogTests : IDisposable
{
    private readonly string _folder;
    private readonly CableTestDatabase _database;
    private readonly SqliteCableRepository _cables;
    private readonly SqliteVehicleRepository _vehicles;

    public Milos40CatalogTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestKatalog_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);

        _database = CableTestDatabase.OpenAndMigrate(Path.Combine(_folder, "test.db"));
        _cables = new SqliteCableRepository(_database);
        _vehicles = new SqliteVehicleRepository(_database);
    }

    public void Dispose()
    {
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

    // -------------------------------------------------------------------------------------
    // Sadržaj kataloga
    // -------------------------------------------------------------------------------------

    /// <summary>Nova baza ima jedno vozilo i dvanaest kablova sa crteža — ništa od demo sadržaja.</summary>
    [Fact]
    public void NovaBaza_ImaSamoVoziloMilosVelikiIDvanaestKablova()
    {
        Vehicle vehicle = Assert.Single(_vehicles.GetAll());
        Assert.Equal("Miloš Veliki", vehicle.Name);

        IReadOnlyList<Cable> cables = _cables.GetByVehicle(vehicle.Id);

        Assert.Equal(12, cables.Count);
        Assert.Equal(
            new[]
            {
                "40W1-1", "40W1-2", "40W1-3", "40W1-4", "40W1-5", "40W1-6",
                "40W2", "40W3", "40W4-1", "40W4-2", "40W4-3", "40W5"
            },
            cables.Select(c => c.SpecFileName).OrderBy(n => n, StringComparer.Ordinal));

        Assert.DoesNotContain(_cables.GetAll(), c => c.SpecFileName.StartsWith("M30-", StringComparison.Ordinal));
    }

    [Fact]
    public void SvakiKabl_NosiPodatkeSaCrteza()
    {
        Cable cable = Cable("40W1-4");

        Assert.Equal("=40-W1.4", cable.Designation);
        Assert.Equal("Wabco 2x1,5 mm²", cable.CableType);
        Assert.Equal(4.6m, cable.LengthM);
        Assert.Equal("40_grupa_2_0.pdf", cable.SourceDocument);
        Assert.Equal(4, cable.SourcePage);
        Assert.True(cable.IsActive);
        Assert.Contains("papučicu i pin postavljati na vozilu", cable.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SvaImenaSpecFajlova_ProlazeValidator()
    {
        foreach (Cable cable in _cables.GetAll())
        {
            Assert.Null(SpecFileNameValidator.Validate(cable.SpecFileName));
        }
    }

    /// <summary>Dok adapter nije napravljen, svaka dodela tačke je privremena.</summary>
    [Fact]
    public void SveDodeleTacaka_SuPrivremene()
    {
        foreach (Cable cable in _cables.GetAll())
        {
            Assert.True(cable.HasProvisionalPoints);
            Assert.All(cable.Terminals, t => Assert.True(t.IsProvisional));
        }
    }

    // -------------------------------------------------------------------------------------
    // Izvedena net lista
    // -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("40W1-1")]
    [InlineData("40W1-2")]
    [InlineData("40W1-3")]
    [InlineData("40W1-4")]
    [InlineData("40W1-5")]
    [InlineData("40W1-6")]
    public void Grupa1_DajeNetoveA01B01IA02B02(string spec)
        => Assert.Equal(new[] { "A01-B01", "A02-B02" }, Nets(spec));

    /// <summary>Kod W2–W4 je raspored boja obrnut, pa su i netovi obrnuti.</summary>
    [Theory]
    [InlineData("40W2")]
    [InlineData("40W3")]
    [InlineData("40W4-1")]
    [InlineData("40W4-2")]
    [InlineData("40W4-3")]
    public void Grupa2_DajeNetoveA02B01IA01B02(string spec)
        => Assert.Equal(new[] { "A02-B01", "A01-B02" }, Nets(spec));

    [Fact]
    public void W5_DajeSestNetova_SaLancemMostovaUJednom()
        => Assert.Equal(
            new[] { "C01-D01", "C02-D02", "C03-D03", "C04-D04", "C05-D05", "C06-D06-D07-D08-D09" },
            Nets("40W5"));

    /// <summary>Tri mosta nacrtana lančano daju jedan čvor sa pet tačaka, a ne tri čvora.</summary>
    [Fact]
    public void W5_LancaniMostovi_DajuJedanNetOdPetTacaka()
    {
        Net net = Cable("40W5").Nets.Single(n => n.Points.StartsWith("C06", StringComparison.Ordinal)).ToNet();

        Assert.Equal(5, net.Count);
        Assert.Equal(
            new[] { "C06", "D06", "D07", "D08", "D09" },
            net.Select(p => p.ToString()));
    }

    [Fact]
    public void W5_ImaDevetBuksniIDevetProvodnika()
    {
        Cable cable = Cable("40W5");

        Assert.Equal(9, cable.Terminals.Count(t => t.ContactType.StartsWith("buksna", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(9, cable.Wires.Count);
        Assert.Equal(3, cable.Wires.Count(w => w.LengthM == 0.05m));
    }

    [Fact]
    public void Provodnici_NoseBojuIPresekSaCrteza()
    {
        CableWire wire = Cable("40W2").Wires.Single(w => w.WireNo == 1);

        Assert.Equal("BR", wire.Color);
        Assert.Equal(0.75m, wire.CrossSectionMm2);
        Assert.Equal("DIN:2", wire.FromTerminal);
        Assert.Equal("10XB:16", wire.ToTerminal);
        Assert.Equal("braon", WireColor.Describe(wire.Color));
    }

    /// <summary>Neslaganje zaglavlja i tabele za W4.3 mora da ostane vidljivo u napomeni.</summary>
    [Fact]
    public void W43_NosiNapomenuONeslaganjuPinova()
    {
        Cable cable = Cable("40W4-3");

        Assert.Contains("10XC:05", cable.Terminals.Select(t => t.Label));
        Assert.Contains("10XB", cable.Notes, StringComparison.Ordinal);
        Assert.Contains("NEPOTVRĐENO", cable.Notes, StringComparison.Ordinal);
    }

    /// <summary>Pretpostavka o x1/x2 sa crteža mora da stoji uz kabl, ne samo u glavi onoga ko je unosio.</summary>
    [Fact]
    public void Grupa1_NosiNapomenuOPretpostavciX1X2()
    {
        Cable cable = Cable("40W1-1");

        Assert.Contains("x1", cable.Notes, StringComparison.Ordinal);
        Assert.Contains("x1", cable.FindTerminal("DIN:1")!.Notes, StringComparison.Ordinal);
        Assert.Contains("x2", cable.FindTerminal("DIN:2")!.Notes, StringComparison.Ordinal);
    }

    // -------------------------------------------------------------------------------------
    // .c61
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// Generisani .c61 sme da se razlikuje od šablona isključivo u bloku OSNet — sve ostalo je
    /// zaglavlje koje je snimio sam CableConnector.
    /// </summary>
    [Fact]
    public void W5_GenerisanSpec_SeOdSablonaRazlikujeSamoUOSNetBloku()
    {
        var generator = C61Generator.FromFile(Path.Combine(AppContext.BaseDirectory, "templates", "MASTER.c61"));

        string[] sablon = generator.TemplateText.Split(C61Generator.LineEnding);
        string[] spec = generator.Build(Cable("40W5").Nets.Select(n => n.ToNet())).Split(C61Generator.LineEnding);

        string[] bezNetova(string[] lines) => lines
            .Where(l => !l.StartsWith(C61Generator.NetCountPrefix, StringComparison.Ordinal)
                        && !l.StartsWith(C61Generator.NetLinePrefix, StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(bezNetova(sablon), bezNetova(spec));

        Assert.Equal(
            new[]
            {
                "OSNet=6",
                "OSNet:C01-D01",
                "OSNet:C02-D02",
                "OSNet:C03-D03",
                "OSNet:C04-D04",
                "OSNet:C05-D05",
                "OSNet:C06-D06-D07-D08-D09"
            },
            spec.Where(l => l.StartsWith("OSNet", StringComparison.Ordinal)));
    }

    [Fact]
    public void W11_GenerisanSpec_ImaDvaNeta()
    {
        var generator = C61Generator.FromFile(Path.Combine(AppContext.BaseDirectory, "templates", "MASTER.c61"));

        string[] spec = generator.Build(Cable("40W1-1").Nets.Select(n => n.ToNet()))
            .Split(C61Generator.LineEnding);

        Assert.Equal(
            new[] { "OSNet=2", "OSNet:A01-B01", "OSNet:A02-B02" },
            spec.Where(l => l.StartsWith("OSNet", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Kabl kome nedostaje tačka testera ne sme da dobije .c61: program koji ode u tester mora da
    /// odgovara kablu na stolu.
    /// </summary>
    [Fact]
    public void KablBezDodeljeneTacke_NeDajeSpecNegoPoruku()
    {
        Cable cable = Cable("40W2");
        cable.FindTerminal("10XB:16")!.TesterPoint = null;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => CableSpec.NetsFor(cable));

        Assert.Contains(
            "Kabl nije spreman za ispitivanje: terminal 10XB:16 nema tačku testera",
            error.Message,
            StringComparison.Ordinal);
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private Cable Cable(string spec)
        => _cables.GetBySpecFileName(spec) ?? throw new InvalidOperationException($"Nema kabla {spec}.");

    private string[] Nets(string spec)
        => Cable(spec).Nets.OrderBy(n => n.Ordinal).Select(n => n.Points).ToArray();
}
