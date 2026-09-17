namespace CableTest.Core.Model;

/// <summary>
/// Prevod greške sa jezika testera na jezik crteža.
/// </summary>
/// <remarks>
/// <para>
/// Tester prijavljuje „SHORT A02-B01" — tačke svog priključnog polja. Operater pred sobom ima
/// kabl sa crteža, na kome tih oznaka nema: tamo stoje <c>DIN:2</c>, <c>10XB:16</c> i boja žice.
/// Zato se tačke prevode nazad kroz priključnu tabelu, a sama tačka ostaje u zagradi — po njoj se
/// poruka može uporediti sa onim što piše u CSV-u testera.
/// </para>
/// <para>
/// Tačka koje u priključnoj tabeli nema ostaje prikazana onakva kakva je: izmišljanje prevoda za
/// nju bi operatera poslalo da traži kvar na pogrešnom kraju kabla.
/// </para>
/// </remarks>
public static class DefectTranslator
{
    /// <summary>Crtica između tačaka u zagradi; duža od one u zapisu neta, da se razlikuje.</summary>
    private const string PointSeparator = "–";

    /// <summary>
    /// Greška u obliku „Prekid: DIN:2 → 10XB:16, žica BR (A02–B01)".
    /// </summary>
    /// <remarks>
    /// Kad kabl nije poznat ili nema priključnu tabelu, vraća se opis koji zna sam
    /// <see cref="TestDefect"/> — bolje išta razumljivo nego prazno.
    /// </remarks>
    public static string Describe(TestDefect defect, Cable? cable)
    {
        ArgumentNullException.ThrowIfNull(defect);

        string[] points = string.IsNullOrWhiteSpace(defect.Points)
            ? Array.Empty<string>()
            : defect.Points.Split(Net.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (cable is null || points.Length == 0 || !cable.HasWiring)
        {
            return defect.Describe();
        }

        var terminals = new List<CableTerminal?>(points.Length);
        bool anyKnown = false;

        foreach (string point in points)
        {
            CableTerminal? terminal = cable.FindTerminalByPoint(point);
            terminals.Add(terminal);
            anyKnown |= terminal is not null;
        }

        if (!anyKnown)
        {
            // Nijedna tačka nije u priključnoj tabeli — greška je verovatno sa drugog kabla.
            return defect.Describe();
        }

        string kind = defect.Kind switch
        {
            DefectKind.Short => "Kratak spoj",
            DefectKind.Open => "Prekid",
            _ => string.IsNullOrWhiteSpace(defect.RawText) ? "Nepoznata greška" : defect.RawText
        };

        string labels = string.Join(
            " → ",
            terminals.Select((t, i) => t?.Label ?? points[i]));

        string wire = DescribeWire(cable, terminals);
        string raw = string.Join(PointSeparator, points);

        return $"{kind}: {labels}{wire} ({raw})";
    }

    /// <summary>Sve greške jednog rezultata, prevedene.</summary>
    public static IReadOnlyList<string> DescribeAll(TestRun run, Cable? cable)
    {
        ArgumentNullException.ThrowIfNull(run);
        return run.Defects.Select(d => Describe(d, cable)).ToArray();
    }

    /// <summary>
    /// „, žica BR" kad oba kraja greške leže na istom provodniku; inače prazno.
    /// </summary>
    /// <remarks>
    /// Kod prekida su to dva kraja iste žice, pa se boja zna. Kod kratkog spoja su najčešće dve
    /// različite žice i tada se boja namerno ne pominje — rekli bismo nešto što ne znamo.
    /// </remarks>
    private static string DescribeWire(Cable cable, IReadOnlyList<CableTerminal?> terminals)
    {
        if (terminals.Count != 2 || terminals[0] is null || terminals[1] is null)
        {
            return string.Empty;
        }

        string from = terminals[0]!.Label;
        string to = terminals[1]!.Label;

        CableWire? wire = cable.Wires.FirstOrDefault(w =>
            (string.Equals(w.FromTerminal, from, StringComparison.OrdinalIgnoreCase)
             && string.Equals(w.ToTerminal, to, StringComparison.OrdinalIgnoreCase))
            || (string.Equals(w.FromTerminal, to, StringComparison.OrdinalIgnoreCase)
                && string.Equals(w.ToTerminal, from, StringComparison.OrdinalIgnoreCase)));

        return wire is null ? string.Empty : $", žica {wire.Color}";
    }
}
