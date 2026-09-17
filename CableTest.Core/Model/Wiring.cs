using System.Globalization;

namespace CableTest.Core.Model;

/// <summary>Strana kabla na kojoj terminal stoji, kako je na crtežu.</summary>
public enum CableSide
{
    /// <summary>Strana A — na crtežu levo (npr. DIN konektor).</summary>
    A,

    /// <summary>Strana B — na crtežu desno (pinovi na vozilu, papučice, buksne).</summary>
    B
}

/// <summary>
/// Skraćenice boja žica sa crteža.
/// </summary>
/// <remarks>
/// Skraćenice se čuvaju onakve kakve stoje na crtežu — tako se zapis u bazi može bez prevođenja
/// uporediti sa dokumentacijom. Pun naziv postoji samo za prikaz operateru.
/// </remarks>
public static class WireColor
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BR"] = "braon",
        ["PL"] = "plava",
        ["BE"] = "bela",
        ["ZE"] = "zelena",
        ["ZU"] = "žuta",
        ["SV"] = "siva"
    };

    /// <summary>Pun naziv boje, ili sama skraćenica kad nije u legendi.</summary>
    public static string Describe(string? abbreviation)
    {
        if (string.IsNullOrWhiteSpace(abbreviation))
        {
            return "—";
        }

        string key = abbreviation.Trim();
        return Names.TryGetValue(key, out string? name) ? name : key;
    }

    /// <summary>Skraćenica i pun naziv, npr. „BR (braon)"; za nepoznatu boju samo skraćenica.</summary>
    public static string DescribeFull(string? abbreviation)
    {
        if (string.IsNullOrWhiteSpace(abbreviation))
        {
            return "—";
        }

        string key = abbreviation.Trim();
        return Names.TryGetValue(key, out string? name) ? $"{key} ({name})" : key;
    }
}

/// <summary>
/// Jedan kraj kabla: kontakt koji se na vozilu na nešto spaja (tabela CableTerminal).
/// </summary>
/// <remarks>
/// <para>
/// Terminal je prepis crteža i ne zna za tester: <see cref="Label"/> je oznaka sa dokumentacije
/// (<c>DIN:1</c>, <c>10XF:02</c>, <c>PAP 1,5/M6</c>), a <see cref="ContactType"/> vrsta kontakta.
/// </para>
/// <para>
/// <see cref="TesterPoint"/> je jedina veza sa ispitivanjem i pripada <b>priključnom priboru</b>,
/// ne kablu: kad se napravi pravi adapter, menja se samo ona, a ožičenje ostaje netaknuto. Dok
/// raspored nije potvrđen na adapteru, dodela nosi <see cref="IsProvisional"/>.
/// </para>
/// </remarks>
public sealed class CableTerminal
{
    public long Id { get; set; }

    public long CableId { get; set; }

    /// <summary>Oznaka terminala sa crteža; jedinstvena u okviru kabla.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Strana kabla.</summary>
    public CableSide Side { get; set; }

    /// <summary>Vrsta kontakta, npr. „DIN 72585 pin", „papučica 1,5/M6", „buksna 1,5/6,3".</summary>
    public string ContactType { get; set; } = string.Empty;

    /// <summary>Tačka testera na koju terminal ide preko adaptera, ili <c>null</c> ako nije dodeljena.</summary>
    public string? TesterPoint { get; set; }

    /// <summary>Da li je dodela tačke privremena — raspored još nije potvrđen na adapteru.</summary>
    public bool IsProvisional { get; set; }

    /// <summary>Napomena sa crteža.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Da li terminal ima dodeljenu tačku testera.</summary>
    public bool HasTesterPoint => !string.IsNullOrWhiteSpace(TesterPoint);

    /// <summary>Dodeljena tačka kao <see cref="Model.TestPoint"/>; <c>null</c> ako nije dodeljena ili nije ispravna.</summary>
    public TestPoint? Point
        => HasTesterPoint && Model.TestPoint.TryParse(TesterPoint, out TestPoint point) ? point : null;

    public override string ToString() => Label;
}

/// <summary>
/// Jedan provodnik kabla (tabela CableWire): žica ili most između dva terminala.
/// </summary>
/// <remarks>
/// Ovo je ožičenje — prepis crteža koji ne zavisi od testera. Iz njega se, zajedno sa
/// priključnom tabelom terminala, <b>izvodi</b> net lista; vidi <see cref="NetBuilder"/>.
/// </remarks>
public sealed class CableWire
{
    public long Id { get; set; }

    public long CableId { get; set; }

    /// <summary>Redni broj provodnika na crtežu, počinje od 1.</summary>
    public int WireNo { get; set; }

    /// <summary>Skraćenica boje sa crteža; za mostove „most".</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Presek u mm².</summary>
    public decimal CrossSectionMm2 { get; set; }

    /// <summary>Dužina u metrima kad je posebno data (mostovi); inače <c>null</c> — važi dužina kabla.</summary>
    public decimal? LengthM { get; set; }

    /// <summary>Oznaka terminala sa jednog kraja.</summary>
    public string FromTerminal { get; set; } = string.Empty;

    /// <summary>Oznaka terminala sa drugog kraja.</summary>
    public string ToTerminal { get; set; } = string.Empty;

    /// <summary>Napomena sa crteža.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Presek u obliku za prikaz, npr. „1,5 mm²".</summary>
    public string CrossSectionText => Decimals.Format(CrossSectionMm2) + " mm²";

    /// <summary>Dužina u obliku za prikaz, ili „—" kad nije posebno data.</summary>
    public string LengthText => LengthM is null ? "—" : Decimals.Format(LengthM.Value) + " m";

    /// <summary>Red tabele provodnika: „1. BR (braon) 1,5 mm²  DIN:1 → 10XF:02".</summary>
    public string Describe()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{WireNo}. {WireColor.DescribeFull(Color)} {CrossSectionText}  {FromTerminal} → {ToTerminal}");

    public override string ToString() => Describe();
}

/// <summary>Ispis decimalnih vrednosti sa zarezom, kako se piše na crtežu i kako operater čita.</summary>
public static class Decimals
{
    private static readonly CultureInfo Serbian = CultureInfo.GetCultureInfo("sr-Latn-RS");

    /// <summary>Broj bez suvišnih nula: 1,5 · 0,75 · 2 · 4,6.</summary>
    public static string Format(decimal value) => value.ToString("0.###", Serbian);
}
