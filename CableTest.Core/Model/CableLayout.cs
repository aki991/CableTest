namespace CableTest.Core.Model;

/// <summary>Jedan port testera (A..P) u mapi pinova jednog kabla.</summary>
/// <param name="Letter">Slovo porta.</param>
/// <param name="Used">Da li kabl koristi bar jednu ispitnu tačku tog porta.</param>
/// <param name="PointCount">Koliko tačaka tog porta kabl koristi.</param>
public sealed record PortUsage(char Letter, bool Used, int PointCount)
{
    /// <summary>Opis za oblačić iznad pina.</summary>
    public string Description => Used
        ? $"Port {Letter} — kabl koristi {PointCount} ispitnih tačaka"
        : $"Port {Letter} — kabl ga ne koristi";
}

/// <summary>Jedan red tabele netova: odakle, dokle i sa kakvim ishodom.</summary>
/// <param name="Ordinal">Redni broj neta.</param>
/// <param name="From">Prva tačka neta.</param>
/// <param name="To">Poslednja tačka neta.</param>
/// <param name="Points">Ceo zapis neta.</param>
/// <param name="PointCount">Broj tačaka u netu.</param>
/// <param name="Status">„PROŠAO", „PAO" ili „—" dok rezultata nema.</param>
/// <param name="Defect">Opis greške na tom netu; prazno ako greške nema.</param>
public sealed record NetRow(
    int Ordinal,
    string From,
    string To,
    string Points,
    int PointCount,
    string Status,
    string Defect)
{
    /// <summary>Stanje neta dok rezultata nema.</summary>
    public const string Unknown = "—";

    /// <summary>Net na kome tester nije prijavio grešku.</summary>
    public const string Passed = "PROŠAO";

    /// <summary>Net na kome je tester prijavio grešku.</summary>
    public const string Failed = "PAO";

    public bool IsPassed => Status == Passed;

    public bool IsFailed => Status == Failed;

    /// <summary>Tekst oblačića: greška ako je ima, inače ceo zapis neta.</summary>
    public string Description => Defect.Length > 0 ? Defect : Points;
}

/// <summary>
/// Izvedeni podaci o kablu koje prikaz traži, a u bazi ne stoje: mapa portova, broj ispitnih
/// tačaka i tabela netova sa ishodom.
/// </summary>
/// <remarks>
/// Stoji u <c>Core</c>, a ne u prikazu, jer je reč o pravilima samog uređaja — koji port kabl
/// koristi i koja tačka pripada kom netu — a ne o rasporedu na ekranu.
/// </remarks>
public static class CableLayout
{
    /// <summary>Deli zapis neta („A01-I01") na oznake tačaka.</summary>
    public static string[] SplitPoints(string? points)
        => string.IsNullOrWhiteSpace(points)
            ? Array.Empty<string>()
            : points.Split(Net.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Mapa pinova kabla: svih 16 portova testera (A..P), sa oznakom da li ih kabl koristi.
    /// Portovi se ne izmišljaju — čitaju se iz net liste.
    /// </summary>
    public static IReadOnlyList<PortUsage> Ports(Cable? cable)
    {
        var counts = new int[TestPoint.PortCount];

        if (cable is not null)
        {
            foreach (CableNet net in cable.Nets)
            {
                foreach (string point in SplitPoints(net.Points))
                {
                    if (TestPoint.TryParse(point, out TestPoint parsed))
                    {
                        counts[parsed.Port - TestPoint.FirstPort]++;
                    }
                }
            }
        }

        var ports = new PortUsage[TestPoint.PortCount];

        for (int i = 0; i < ports.Length; i++)
        {
            ports[i] = new PortUsage((char)(TestPoint.FirstPort + i), counts[i] > 0, counts[i]);
        }

        return ports;
    }

    /// <summary>Koliko ispitnih tačaka kabl koristi.</summary>
    public static int CountPoints(Cable? cable)
    {
        if (cable is null)
        {
            return 0;
        }

        int count = 0;

        foreach (CableNet net in cable.Nets)
        {
            count += SplitPoints(net.Points).Length;
        }

        return count;
    }

    /// <summary>Koliko portova kabl koristi.</summary>
    public static int CountPorts(Cable? cable)
    {
        int count = 0;

        foreach (PortUsage port in Ports(cable))
        {
            if (port.Used)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Slova portova koje kabl koristi, npr. „A, B, C"; „—" ako ih nema.</summary>
    public static string PortLetters(Cable? cable)
    {
        string letters = string.Join(", ", Ports(cable).Where(p => p.Used).Select(p => p.Letter.ToString()));
        return letters.Length == 0 ? NetRow.Unknown : letters;
    }

    /// <summary>
    /// Tabela netova kabla, sa ishodom ako je zadati rezultat baš tog kabla.
    /// </summary>
    /// <param name="cable">Kabl čija se net lista prikazuje.</param>
    /// <param name="run">Poslednji prikazani rezultat, ili <c>null</c>.</param>
    /// <param name="runCable">Kabl kome taj rezultat pripada, ili <c>null</c>.</param>
    /// <remarks>
    /// Kolone su isključivo ono što aplikacija zaista zna: tačke neta i da li je tester nad njim
    /// prijavio grešku. Izmerene vrednosti otpora se ne prikazuju zato što ih CSV izveštaj
    /// CableConnector-a nema — izmišljen broj bio bi gori od prazne kolone.
    /// </remarks>
    public static IReadOnlyList<NetRow> NetRows(Cable? cable, TestRun? run, Cable? runCable)
    {
        if (cable is null)
        {
            return Array.Empty<NetRow>();
        }

        // Ishod se upisuje uz netove samo kad je prikazani rezultat baš tog kabla.
        bool sameCable = run is not null && runCable is not null && runCable.Id == cable.Id;

        // Tačka -> opis greške koju je tester prijavio nad njom.
        var faulty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (sameCable)
        {
            foreach (TestDefect defect in run!.Defects)
            {
                foreach (string point in SplitPoints(defect.Points))
                {
                    faulty.TryAdd(point, defect.Describe());
                }
            }
        }

        var rows = new List<NetRow>(cable.Nets.Count);

        foreach (CableNet net in cable.Nets.OrderBy(n => n.Ordinal))
        {
            string[] points = SplitPoints(net.Points);
            string defect = string.Empty;

            foreach (string point in points)
            {
                if (faulty.TryGetValue(point, out string? text))
                {
                    defect = text;
                    break;
                }
            }

            string status = !sameCable
                ? NetRow.Unknown
                : defect.Length > 0 ? NetRow.Failed : NetRow.Passed;

            rows.Add(new NetRow(
                net.Ordinal,
                points.Length > 0 ? points[0] : NetRow.Unknown,
                points.Length > 1 ? points[^1] : NetRow.Unknown,
                net.Points,
                points.Length,
                status,
                defect));
        }

        return rows;
    }
}
