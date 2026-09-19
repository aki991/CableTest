using CableTest.Core.Data;
using CableTest.Core.Model;
using CableTest.Core.Spec;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Kablovi vozila „Miloš Veliki“ iz migracije 005 — grupe M26, M40, M52, M53, M77, M90 i M96.
/// </summary>
/// <remarks>
/// <para>
/// Podaci su prepisani sa 55 strana crteža, i to programom, a ne rukom. Ovi testovi paze na ono
/// što prepis može da pokvari tiho: da kabl uopšte postoji, da mu se iz ožičenja može izvesti
/// net lista, i da ime spec fajla prolazi kroz <see cref="SpecFileNameValidator"/>. Kabl kome
/// se net lista ne izvodi ne može da se ispita, a to se inače vidi tek kad operater stane pred
/// tester.
/// </para>
/// <para>
/// Baza se svaki put pravi iz migracija, u privremenom fajlu — isto onako kako nastaje i kod
/// operatera pri prvom pokretanju.
/// </para>
/// </remarks>
public sealed class GrupeMilosVelikiTests : IDisposable
{
    private readonly string _folder;
    private readonly CableTestDatabase _database;
    private readonly SqliteCableRepository _cables;
    private readonly SqliteVehicleRepository _vehicles;

    public GrupeMilosVelikiTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestGrupe005_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);

        _database = CableTestDatabase.OpenAndMigrate(Path.Combine(_folder, "test.db"));
        _cables = new SqliteCableRepository(_database);
        _vehicles = new SqliteVehicleRepository(_database);
    }

    /// <summary>Očekivani broj kablova po grupi, prema broju strana u crtežima.</summary>
    public static TheoryData<string, int> Grupe => new()
    {
        { "M26", 3 },
        { "M40", 14 },
        { "M52", 2 },
        { "M53", 5 },
        { "M77", 19 },
        { "M90", 9 },
        { "M96", 3 },
    };

    [Theory]
    [MemberData(nameof(Grupe))]
    public void Grupa_ImaOnolikoKablovaKolikoCrtezImaStrana(string grupa, int kablova)
        => Assert.Equal(kablova, Kablovi().Count(c => c.Group == grupa));

    [Fact]
    public void Ukupno_JePedesetPetKablova() => Assert.Equal(55, Kablovi().Count);

    /// <summary>
    /// Stara grupa 40, prepisana sa crteža 40_grupa_2_0.pdf, više se ne nudi.
    /// </summary>
    /// <remarks>
    /// Na novom crtežu isti kablovi idu u konektor XM7 umesto na pin 10XF i papučicu — to nije
    /// preimenovanje nego drugi kabl. Da je ostao i stari, operater bi u spisku imao dva „W1.1“
    /// i nikakvog načina da izabere pravi.
    /// </remarks>
    [Fact]
    public void StaraGrupa40_SeViseNeNudi()
    {
        Assert.DoesNotContain(Kablovi(), c => c.Group == "40");
        Assert.DoesNotContain(Kablovi(), c => c.SpecFileName.StartsWith("40W", StringComparison.Ordinal));
    }

    [Fact]
    public void SvakiKabl_ImaIspravnoImeSpecFajla()
    {
        string[] lose = Kablovi()
            .Where(c => !SpecFileNameValidator.IsValid(c.SpecFileName))
            .Select(c => $"{c.Code}: {c.SpecFileName}")
            .ToArray();

        Assert.Empty(lose);
    }

    [Fact]
    public void ImenaSpecFajlova_SuJedinstvena()
    {
        string[] ponovljena = Kablovi()
            .GroupBy(c => c.SpecFileName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        Assert.Empty(ponovljena);
    }

    // -----------------------------------------------------------------------------------
    // Ožičenje
    // -----------------------------------------------------------------------------------

    [Fact]
    public void SvakiKabl_ImaOzicenje()
    {
        string[] bez = Kablovi().Where(c => !c.HasWiring).Select(c => c.Code).ToArray();
        Assert.Empty(bez);
    }

    /// <summary>Svaki kraj svakog provodnika mora da pokazuje na postojeći terminal.</summary>
    [Fact]
    public void SvakaZica_SpajaPostojeceTerminale()
    {
        var lose = new List<string>();

        foreach (Cable c in Kablovi())
        {
            foreach (CableWire w in c.Wires)
            {
                if (c.FindTerminal(w.FromTerminal) is null)
                {
                    lose.Add($"{c.Code}: žica {w.WireNo} polazi sa nepoznatog „{w.FromTerminal}“");
                }

                if (c.FindTerminal(w.ToTerminal) is null)
                {
                    lose.Add($"{c.Code}: žica {w.WireNo} ide na nepoznat „{w.ToTerminal}“");
                }
            }
        }

        Assert.Empty(lose);
    }

    /// <summary>
    /// Iz ožičenja i priključne tabele mora da se izvede net lista — inače kabl ne može da se
    /// ispita. Ovo je jedina provera koja stvarno kaže da li je prepis upotrebljiv.
    /// </summary>
    [Fact]
    public void SvakomKablu_SeIzvodiNetLista()
    {
        string[] lose = Kablovi()
            .Select(c => new { c.Code, Izvod = NetBuilder.Derive(c) })
            .Where(x => !x.Izvod.IsValid)
            .Select(x => $"{x.Code}: {x.Izvod.ErrorText}")
            .ToArray();

        Assert.Empty(lose);
    }

    /// <summary>Netova ima tačno koliko i žica: svaka žica je jedna veza između dve tačke.</summary>
    [Fact]
    public void BrojNetova_OdgovaraBrojuZica()
    {
        string[] lose = Kablovi()
            .Select(c => new { c.Code, Zica = c.Wires.Count, Neta = NetBuilder.Derive(c).Nets.Count })
            .Where(x => x.Zica != x.Neta)
            .Select(x => $"{x.Code}: {x.Zica} žica, {x.Neta} netova")
            .ToArray();

        Assert.Empty(lose);
    }

    /// <summary>
    /// Sve dodele tačaka testera su privremene — adapter još nije napravljen.
    /// </summary>
    /// <remarks>
    /// Ekran zbog ovoga iznad test programa piše da priključna tabela nije potvrđena. Kad bi
    /// dodela bila označena kao konačna, operater bi poverovao rasporedu koji niko nije proverio.
    /// </remarks>
    [Fact]
    public void SveTackeTestera_SuPrivremene()
    {
        Assert.All(Kablovi(), c => Assert.True(c.HasProvisionalPoints, c.Code));
        Assert.All(Kablovi(), c => Assert.All(c.Terminals, t => Assert.True(t.HasTesterPoint, c.Code + "/" + t.Label)));
    }

    // -----------------------------------------------------------------------------------
    // Pojedini kablovi, provereni uz sam crtež
    // -----------------------------------------------------------------------------------

    /// <summary>=M40-W1.1, strana 2: DIN 72585 pin 1 (BR) i pin 2 (PL) u konektor XM7, pinovi A i B.</summary>
    [Fact]
    public void M40W11_JeSaCrteza()
    {
        Cable c = Kabl("M40-W1.1");

        Assert.Equal("=M40-W1.1", c.Designation);
        Assert.Equal("Wabco 2x1,5mm2", c.CableType);
        Assert.Equal(3.5m, c.LengthM);
        Assert.Equal(2, c.Wires.Count);

        Assert.Equal(("BR", "1", "A"), Zica(c, 1));
        Assert.Equal(("PL", "2", "B"), Zica(c, 2));
    }

    /// <summary>=M40-W6, strana 14 — najveći kabl u dokumentaciji: 31 provodnik.</summary>
    [Fact]
    public void M40W6_ImaTridesetJednuZicu()
    {
        Cable c = Kabl("M40-W6");

        Assert.Equal(31, c.Wires.Count);
        Assert.Equal(62, c.Terminals.Count);
        Assert.Equal(31, NetBuilder.Derive(c).Nets.Count);
    }

    /// <summary>
    /// =M52-W1 je jedini kabl kod koga se boje leve i desne tabele ne poklapaju po redu, pa su
    /// krajevi spojeni po boji. To mora da piše uz kabl, da se ne shvati kao potvrđen podatak.
    /// </summary>
    [Fact]
    public void M52W1_NosiNapomenuOSpajanjuPoBoji()
    {
        Cable c = Kabl("M52-W1");

        Assert.Contains("NEPOTVRĐENO", c.Notes, StringComparison.Ordinal);
        Assert.Equal(5, c.Wires.Count);
    }

    /// <summary>
    /// =M96-W1 ima istu oznaku priključka na oba kraja („50/m12“), pa drugi kraj nosi crticu.
    /// Bez toga bi dva terminala istog kabla imala istu oznaku, što baza ne prima.
    /// </summary>
    [Fact]
    public void M96W1_RazlikujeKrajeveIsteOznake()
    {
        Cable c = Kabl("M96-W1");

        Assert.Equal(2, c.Terminals.Count);
        Assert.Equal(new[] { "50/m12", "50/m12'" }, c.Terminals.Select(t => t.Label));
    }

    /// <summary>Kod =M90-W2A crtež oznaku piše bez „M“; šifra je ipak u grupi M90.</summary>
    [Fact]
    public void M90W2A_ZadrzavaDoslovnuOznakuSaCrteza()
    {
        Cable c = Kabl("M90-W2A");

        Assert.Equal("M90", c.Group);
        Assert.Equal("=90-W2A", c.Designation);
    }

    // -----------------------------------------------------------------------------------
    // Test program
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Generisani .c61 sme da se razlikuje od šablona isključivo u bloku OSNet — sve ostalo je
    /// zaglavlje koje je snimio sam CableConnector.
    /// </summary>
    /// <remarks>
    /// Vrednost u zaglavlju koju CableConnector nema u svojim padajućim listama ruši ga sa
    /// „Index was outside the bounds of the array“, pa se zaglavlje ne dira.
    /// </remarks>
    [Fact]
    public void GenerisanSpec_SeOdSablonaRazlikujeSamoUOSNetBloku()
    {
        var generator = C61Generator.FromFile(Path.Combine(AppContext.BaseDirectory, "templates", "MASTER.c61"));

        string[] sablon = generator.TemplateText.Split(C61Generator.LineEnding);
        string[] spec = generator.Build(Kabl("M40-W6").Nets.Select(n => n.ToNet()))
            .Split(C61Generator.LineEnding);

        static string[] BezNetova(string[] redovi) => redovi
            .Where(l => !l.StartsWith(C61Generator.NetCountPrefix, StringComparison.Ordinal)
                        && !l.StartsWith(C61Generator.NetLinePrefix, StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(BezNetova(sablon), BezNetova(spec));
    }

    /// <summary>=M40-W1.1 daje dva neta: po jedan za svaku žicu.</summary>
    [Fact]
    public void M40W11_GenerisanSpec_ImaDvaNeta()
    {
        var generator = C61Generator.FromFile(Path.Combine(AppContext.BaseDirectory, "templates", "MASTER.c61"));

        string[] spec = generator.Build(Kabl("M40-W1.1").Nets.Select(n => n.ToNet()))
            .Split(C61Generator.LineEnding);

        Assert.Equal(
            new[] { "OSNet=2", "OSNet:C01-D01", "OSNet:C02-D02" },
            spec.Where(l => l.StartsWith("OSNet", StringComparison.Ordinal)));
    }

    /// <summary>
    /// =M40-W1.1 je prvi kabl prebačen na tačke konektora C i D (migracija 006).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Strana A ide u konektor C, strana B u konektor D, a svaka tačka se na adapteru ukrcava u
    /// oba pina svog para. Dok su oba kraja stajala na konektorima A i B, net je izgledao
    /// „A01-B01" — isti niz znakova kao par pinova jedne tačke. Ko ga pročita kao par, ukrca oba
    /// kraja žice u jedan konektor: žica nije ispitana, a tester javlja „pass".
    /// </para>
    /// <para>
    /// Ostali kablovi iz migracije 005 su još na starim tačkama — raspored se prvo proverava na
    /// ovom kablu.
    /// </para>
    /// </remarks>
    [Fact]
    public void M40W11_KrajeviSuNaKonektorimaCiD()
    {
        Cable c = Kabl("M40-W1.1");

        Assert.Equal("C01", c.FindTerminal("1")!.TesterPoint);
        Assert.Equal("C02", c.FindTerminal("2")!.TesterPoint);
        Assert.Equal("D01", c.FindTerminal("A")!.TesterPoint);
        Assert.Equal("D02", c.FindTerminal("B")!.TesterPoint);

        // Svaka žica spaja dva RAZLIČITA konektora — inače kabl nije ni ispitan.
        foreach (CableWire w in c.Wires)
        {
            TestPoint od = TestPoint.Parse(c.FindTerminal(w.FromTerminal)!.TesterPoint);
            TestPoint doo = TestPoint.Parse(c.FindTerminal(w.ToTerminal)!.TesterPoint);

            Assert.NotEqual(od.Connector, doo.Connector);
        }
    }

    /// <summary>Pinovi tačke se ne unose — čitaju se iz same tačke, u njenom konektoru.</summary>
    [Fact]
    public void M40W11_SvakaTacka_ImaSvojParPinova()
    {
        Cable c = Kabl("M40-W1.1");

        Assert.Equal("A01+B01", TestPoint.Parse(c.FindTerminal("1")!.TesterPoint).Pins);
        Assert.Equal("A01+B01", TestPoint.Parse(c.FindTerminal("A")!.TesterPoint).Pins);
        Assert.Equal("A02+B02", TestPoint.Parse(c.FindTerminal("2")!.TesterPoint).Pins);
        Assert.Equal("A02+B02", TestPoint.Parse(c.FindTerminal("B")!.TesterPoint).Pins);
    }

    /// <summary>
    /// Kabl kome nedostaje tačka testera ne sme da dobije .c61: program koji ode u tester mora
    /// da odgovara kablu na stolu.
    /// </summary>
    [Fact]
    public void KablBezDodeljeneTacke_NeDajeSpecNegoPoruku()
    {
        Cable c = Kabl("M40-W1.1");
        c.Terminals[0].TesterPoint = null;

        InvalidOperationException greska =
            Assert.Throws<InvalidOperationException>(() => CableSpec.NetsFor(c));

        Assert.Contains(NetBuilder.NotReadyPrefix, greska.Message, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------------------
    // Pomoćno
    // -----------------------------------------------------------------------------------

    private static (string Boja, string Od, string Do) Zica(Cable c, int broj)
    {
        CableWire w = c.Wires.Single(x => x.WireNo == broj);
        return (w.Color, w.FromTerminal, w.ToTerminal);
    }

    private Cable Kabl(string code) => Kablovi().Single(c => c.Code == code);

    private IReadOnlyList<Cable> Kablovi()
    {
        Vehicle vozilo = _vehicles.GetAll().Single(v => v.Name == "Miloš Veliki");
        return _cables.GetByVehicle(vozilo.Id).ToList();
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
            // Privremeni folder nije bitan za ishod testa.
        }
    }
}
