using CableTest.Core.Model;
using CableTest.Core.Spec;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Net lista onako kako je operater čita: <b>jedan red po kraju žice</b>.
/// </summary>
/// <remarks>
/// <para>
/// Reč „net" ima dva značenja i oba su u opticaju:
/// </para>
/// <list type="bullet">
/// <item>na ekranu — jedan KRAJ provodnika, pa žica sa dva kraja daje dva neta;</item>
/// <item>u modelu i u .c61 fajlu (<see cref="CableNet"/>) — grupa međusobno spojenih tačaka
/// („A01-B01"), jedna po žici.</item>
/// </list>
/// <para>
/// Ovi testovi drže oba značenja na mestu. Najvažniji je onaj poslednji: .c61 mora da ostane
/// nepromenjen. Program sa po jednom tačkom u netu tražio bi da su A01 i B01 razdvojeni i
/// oborio bi ispravan kabl.
/// </para>
/// </remarks>
public class NetListaTests
{
    // -----------------------------------------------------------------------------------
    // Redovi
    // -----------------------------------------------------------------------------------

    /// <summary>Dve žice → četiri reda, po jedan za svaki kraj.</summary>
    [Fact]
    public void DveZice_DajuCetiriReda()
    {
        IReadOnlyList<NetRow> rows = CableLayout.NetRows(DvozilniKabl(), run: null, runCable: null);

        Assert.Equal(4, rows.Count);
        Assert.Equal(new[] { "A01", "B01", "A02", "B02" }, rows.Select(r => r.Point));
        Assert.Equal(new[] { 1, 2, 3, 4 }, rows.Select(r => r.Ordinal));
    }

    /// <summary>Oba kraja iste žice nose istu vezu — po njoj se u tabeli vidi da idu u paru.</summary>
    [Fact]
    public void ObaKraja_NoseIstuVezu()
    {
        IReadOnlyList<NetRow> rows = CableLayout.NetRows(DvozilniKabl(), run: null, runCable: null);

        Assert.Equal("A01-B01", rows[0].Connection);
        Assert.Equal("A01-B01", rows[1].Connection);
        Assert.Equal("A02-B02", rows[2].Connection);
        Assert.Equal("A02-B02", rows[3].Connection);
    }

    /// <summary>Uz svaki kraj stoji žica kojoj pripada, pročitana kroz priključnu tabelu.</summary>
    [Fact]
    public void UzSvakiKraj_StojiNjegovaZica()
    {
        IReadOnlyList<NetRow> rows = CableLayout.NetRows(DvozilniKabl(), run: null, runCable: null);

        Assert.Equal(new[] { "BR", "BR", "PL", "PL" }, rows.Select(r => r.Wire));
    }

    /// <summary>Bez ožičenja se žica ne može znati; red i dalje postoji, sa „—".</summary>
    [Fact]
    public void BezOzicenja_ZicaOstajeNepoznata()
    {
        var kabl = new Cable { Id = 7, Code = "40-W9" };
        kabl.Nets.Add(new CableNet { Ordinal = 1, Points = "A01-B01" });

        IReadOnlyList<NetRow> rows = CableLayout.NetRows(kabl, run: null, runCable: null);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(NetRow.Unknown, r.Wire));
    }

    /// <summary>
    /// Net bez ijedne tačke ne bi trebalo da postoji, ali ako se nađe u starijem zapisu, mora da
    /// se vidi — prećutno izostavljen red bio bi net koji niko ne ispituje i niko ne primećuje.
    /// </summary>
    [Fact]
    public void NetBezTacaka_IpakDajeRed()
    {
        var kabl = new Cable { Id = 8, Code = "40-W8" };
        kabl.Nets.Add(new CableNet { Ordinal = 1, Points = string.Empty });

        NetRow red = Assert.Single(CableLayout.NetRows(kabl, run: null, runCable: null));

        Assert.Equal(NetRow.Unknown, red.Point);
    }

    // -----------------------------------------------------------------------------------
    // Brojevi
    // -----------------------------------------------------------------------------------

    /// <summary>Krajeva je dvaput više nego žica; veza je tačno onoliko koliko ima žica.</summary>
    [Fact]
    public void Brojevi_KrajevaJeDvaputViseNegoVeza()
    {
        Cable kabl = DvozilniKabl();

        Assert.Equal(4, CableLayout.CountPoints(kabl));   // „Broj netova" na ekranu
        Assert.Equal(2, kabl.Nets.Count);                 // „Broj ispitnih tačaka" na ekranu
    }

    // -----------------------------------------------------------------------------------
    // Ishod
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Tester javlja ishod po VEZI, ne po kraju, pa oba kraja iste žice nose isto stanje.
    /// </summary>
    [Fact]
    public void GreskaNaVezi_ObaraObaNjenaKraja()
    {
        Cable kabl = DvozilniKabl();

        var run = new TestRun { Passed = false };
        run.Defects.Add(new TestDefect { Points = "A01-B01", Kind = DefectKind.Open });

        IReadOnlyList<NetRow> rows = CableLayout.NetRows(kabl, run, kabl);

        Assert.Equal(NetRow.Failed, rows[0].Status);   // A01
        Assert.Equal(NetRow.Failed, rows[1].Status);   // B01
        Assert.Equal(NetRow.Passed, rows[2].Status);   // A02
        Assert.Equal(NetRow.Passed, rows[3].Status);   // B02
    }

    [Fact]
    public void BezRezultata_SvakiKrajJeBezStanja()
    {
        IReadOnlyList<NetRow> rows = CableLayout.NetRows(DvozilniKabl(), run: null, runCable: null);

        Assert.All(rows, r => Assert.Equal(NetRow.Unknown, r.Status));
    }

    // -----------------------------------------------------------------------------------
    // Mapa pinova
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// <see cref="CableLayout.Ports"/> i dalje vraća svih šesnaest portova — ekran sam bira
    /// koje prikazuje. Kad bi filtriranje bilo ovde, „Broj portova" bi izgubio osnovu za
    /// poređenje.
    /// </summary>
    [Fact]
    public void MapaPinova_ZnaSvihSesnaestPortova_AliDvaSuUUpotrebi()
    {
        IReadOnlyList<PortUsage> ports = CableLayout.Ports(DvozilniKabl());

        Assert.Equal(16, ports.Count);
        Assert.Equal(new[] { 'A', 'B' }, ports.Where(p => p.Used).Select(p => p.Letter));
        Assert.Equal(2, CableLayout.CountPorts(DvozilniKabl()));
    }

    /// <summary>Uz iskorišćen port stoji koliko njegovih tačaka kabl dodiruje.</summary>
    [Fact]
    public void IskorisceniPort_ZnaKolikoTacakaNosi()
    {
        PortUsage[] korisceni = CableLayout.Ports(DvozilniKabl()).Where(p => p.Used).ToArray();

        Assert.All(korisceni, p => Assert.Equal(2, p.PointCount));
    }

    // -----------------------------------------------------------------------------------
    // Test program
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Prikaz po krajevima NE sme da se prelije u .c61.
    /// </summary>
    /// <remarks>
    /// Tester traži jedan <c>OSNet:</c> red po vezi, sa obe njene tačke. Kad bi se upisala po
    /// jedna tačka u netu, tester bi tražio da su A01 i B01 razdvojeni — i oborio bi ispravan
    /// kabl. Zato ovde stoji broj 2, iako se na ekranu vide četiri neta.
    /// </remarks>
    [Fact]
    public void TestProgram_ImaJedanNetPoVezi_ANePoKraju()
    {
        Cable kabl = DvozilniKabl();

        string sadrzaj = new C61Generator(Sablon).Build(kabl.Nets.Select(n => n.ToNet()).ToArray());

        Assert.Contains(C61Generator.NetCountPrefix + "2", sadrzaj, StringComparison.Ordinal);
        Assert.Contains(C61Generator.NetLinePrefix + "A01-B01", sadrzaj, StringComparison.Ordinal);
        Assert.Contains(C61Generator.NetLinePrefix + "A02-B02", sadrzaj, StringComparison.Ordinal);

        // Četiri neta na ekranu, dva u fajlu.
        Assert.Equal(4, CableLayout.NetRows(kabl, null, null).Count);
    }

    // -----------------------------------------------------------------------------------
    // Pomoćno
    // -----------------------------------------------------------------------------------

    private static string Sablon =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "templates", "MASTER.c61"));

    /// <summary>
    /// Kabl sa dve žice: braon DIN:1 → 10XF:02 i plava DIN:2 → papučica, sa pripadajućom
    /// priključnom tabelom.
    /// </summary>
    private static Cable DvozilniKabl()
    {
        var kabl = new Cable { Id = 1, Code = "40-W1.1", SpecFileName = "40W1-1" };

        kabl.Terminals.Add(new CableTerminal { Label = "DIN:1", TesterPoint = "A01" });
        kabl.Terminals.Add(new CableTerminal { Label = "10XF:02", TesterPoint = "B01" });
        kabl.Terminals.Add(new CableTerminal { Label = "DIN:2", TesterPoint = "A02" });
        kabl.Terminals.Add(new CableTerminal { Label = "PAP", TesterPoint = "B02" });

        kabl.Wires.Add(new CableWire { WireNo = 1, Color = "BR", FromTerminal = "DIN:1", ToTerminal = "10XF:02" });
        kabl.Wires.Add(new CableWire { WireNo = 2, Color = "PL", FromTerminal = "DIN:2", ToTerminal = "PAP" });

        kabl.Nets.Add(new CableNet { Ordinal = 1, Points = "A01-B01" });
        kabl.Nets.Add(new CableNet { Ordinal = 2, Points = "A02-B02" });

        return kabl;
    }
}
