using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Izvođenje net liste iz ožičenja i priključne tabele.
/// </summary>
/// <remarks>
/// Ovde se brani pravilo od koga zavisi ispravnost ispitivanja: program koji ode u tester mora da
/// odgovara kablu na stolu. Zato svaka sumnja — terminal bez tačke, ista tačka na dva terminala,
/// čvor sa jednom tačkom, žica koja pokazuje u prazno — zaustavlja izvođenje i vraća poruku
/// operateru, umesto da se .c61 upiše sa net listom kojoj se ne može verovati.
/// </remarks>
public sealed class NetBuilderTests
{
    [Fact]
    public void DveZice_DajuDvaNeta_RedomPoRednomBrojuZice()
    {
        Cable cable = Kabl(
            terminals: new[]
            {
                T("DIN:1", "A01"),
                T("DIN:2", "A02"),
                T("10XF:02", "B01"),
                T("PAP", "B02")
            },
            wires: new[]
            {
                W(1, "BR", "DIN:1", "10XF:02"),
                W(2, "PL", "DIN:2", "PAP")
            });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.True(derivation.IsValid);
        Assert.Equal(new[] { "A01-B01", "A02-B02" }, derivation.Nets.Select(n => n.Points));
        Assert.Equal(new[] { 1, 2 }, derivation.Nets.Select(n => n.Ordinal));
    }

    /// <summary>Tačke unutar neta idu po slovu porta, pa po broju — bez obzira na redosled unosa.</summary>
    [Fact]
    public void TackeUNetu_SuPoredjanePoPortuPaPoBroju()
    {
        Cable cable = Kabl(
            terminals: new[] { T("A", "D09"), T("B", "C06"), T("C", "D07") },
            wires: new[] { W(1, "PL", "A", "B"), W(2, "most", "B", "C") });

        Assert.Equal("C06-D07-D09", Assert.Single(NetBuilder.Derive(cable).Nets).Points);
    }

    /// <summary>Lančani mostovi: tri žice, jedan čvor.</summary>
    [Fact]
    public void LancaniMostovi_DajuJedanCvor()
    {
        Cable cable = Kabl(
            terminals: new[] { T("PAP", "C06"), T("B1", "D06"), T("B2", "D07"), T("B3", "D08"), T("B4", "D09") },
            wires: new[]
            {
                W(1, "PL", "PAP", "B1"),
                W(2, "most", "B1", "B2"),
                W(3, "most", "B2", "B3"),
                W(4, "most", "B3", "B4")
            });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.True(derivation.IsValid);
        Assert.Equal("C06-D06-D07-D08-D09", Assert.Single(derivation.Nets).Points);
    }

    // -------------------------------------------------------------------------------------
    // Validacija
    // -------------------------------------------------------------------------------------

    [Fact]
    public void TerminalBezTackeTestera_ZaustavljaIzvodjenje()
    {
        Cable cable = Kabl(
            terminals: new[] { T("DIN:1", "A01"), T("10XF:02", point: null) },
            wires: new[] { W(1, "BR", "DIN:1", "10XF:02") });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.False(derivation.IsValid);
        Assert.Empty(derivation.Nets);
        Assert.Contains(
            "Kabl nije spreman za ispitivanje: terminal 10XF:02 nema tačku testera",
            derivation.ErrorText,
            StringComparison.Ordinal);
    }

    [Fact]
    public void IstaTackaNaDvaTerminala_ZaustavljaIzvodjenje()
    {
        Cable cable = Kabl(
            terminals: new[] { T("DIN:1", "A01"), T("DIN:2", "A01"), T("PAP", "B02") },
            wires: new[] { W(1, "BR", "DIN:1", "PAP") });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.False(derivation.IsValid);
        Assert.Contains("A01", derivation.ErrorText, StringComparison.Ordinal);
        Assert.Contains("DIN:1", derivation.ErrorText, StringComparison.Ordinal);
        Assert.Contains("DIN:2", derivation.ErrorText, StringComparison.Ordinal);
    }

    /// <summary>Čvor sa jednom tačkom tester ne može da ispita — nema šta sa čim da poredi.</summary>
    [Fact]
    public void CvorSaJednomTackom_ZaustavljaIzvodjenje()
    {
        Cable cable = Kabl(
            terminals: new[] { T("A", "A01"), T("B", "A02"), T("C", "A03") },
            wires: new[] { W(1, "BR", "A", "B"), W(2, "PL", "C", "C") });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.False(derivation.IsValid);
        Assert.Contains("samo 1 tačku", derivation.ErrorText, StringComparison.Ordinal);
    }

    [Fact]
    public void ZicaKojaPokazujeNaNepostojeciTerminal_ZaustavljaIzvodjenje()
    {
        Cable cable = Kabl(
            terminals: new[] { T("DIN:1", "A01"), T("DIN:2", "A02") },
            wires: new[] { W(1, "BR", "DIN:1", "NEMA-GA") });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.False(derivation.IsValid);
        Assert.Contains("NEMA-GA", derivation.ErrorText, StringComparison.Ordinal);
        Assert.Contains("koga u kablu nema", derivation.ErrorText, StringComparison.Ordinal);
    }

    [Fact]
    public void NeispravnaTackaTestera_ZaustavljaIzvodjenje()
    {
        Cable cable = Kabl(
            terminals: new[] { T("DIN:1", "A01"), T("DIN:2", "Q99") },
            wires: new[] { W(1, "BR", "DIN:1", "DIN:2") });

        Assert.False(NetBuilder.Derive(cable).IsValid);
    }

    [Fact]
    public void IstaOznakaTerminalaDvaput_ZaustavljaIzvodjenje()
    {
        Cable cable = Kabl(
            terminals: new[] { T("DIN:1", "A01"), T("DIN:1", "A02") },
            wires: Array.Empty<CableWire>());

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.False(derivation.IsValid);
        Assert.Contains("naveden dvaput", derivation.ErrorText, StringComparison.Ordinal);
    }

    /// <summary>Terminal koji nijedna žica ne dodiruje nije greška — samo ne pravi net.</summary>
    [Fact]
    public void NepovezanTerminal_SeNeUracunavaUNetove()
    {
        Cable cable = Kabl(
            terminals: new[] { T("DIN:1", "A01"), T("DIN:2", "A02"), T("SLOBODAN", "A03") },
            wires: new[] { W(1, "BR", "DIN:1", "DIN:2") });

        NetDerivation derivation = NetBuilder.Derive(cable);

        Assert.True(derivation.IsValid);
        Assert.Equal("A01-A02", Assert.Single(derivation.Nets).Points);
    }

    [Fact]
    public void KablBezOzicenja_NemaIzvedenihNetova()
    {
        NetDerivation derivation = NetBuilder.Derive(
            Kabl(Array.Empty<CableTerminal>(), Array.Empty<CableWire>()));

        Assert.True(derivation.IsValid);
        Assert.Empty(derivation.Nets);
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private static Cable Kabl(IReadOnlyList<CableTerminal> terminals, IReadOnlyList<CableWire> wires)
    {
        var cable = new Cable { Id = 1, Code = "=40-W1.1", SpecFileName = "40W1-1" };
        cable.Terminals.AddRange(terminals);
        cable.Wires.AddRange(wires);
        return cable;
    }

    private static CableTerminal T(string label, string? point)
        => new() { CableId = 1, Label = label, TesterPoint = point, IsProvisional = true };

    private static CableWire W(int no, string color, string from, string to)
        => new() { CableId = 1, WireNo = no, Color = color, CrossSectionMm2 = 1.5m, FromTerminal = from, ToTerminal = to };
}
