namespace CableTest.Core.Model;

/// <summary>Jedan konektor testera (A..P) u mapi jednog kabla.</summary>
/// <param name="Letter">Slovo konektora.</param>
/// <param name="Used">Da li kabl koristi bar jednu ispitnu tačku tog konektora.</param>
/// <param name="PointCount">Koliko tačaka tog konektora kabl koristi.</param>
public sealed record PortUsage(char Letter, bool Used, int PointCount)
{
    /// <summary>Opis za oblačić iznad konektora.</summary>
    public string Description => Used
        ? $"Konektor {Letter} — kabl koristi {PointCount} ispitnih tačaka"
        : $"Konektor {Letter} — kabl ga ne koristi";
}

/// <summary>
/// Jedan red net liste: <b>jedan kraj žice</b>, tj. jedna tačka testera.
/// </summary>
/// <remarks>
/// <para>
/// Reč „net" ovde znači ono što znači operateru: jedan kraj provodnika. Žica sa dva kraja daje
/// dva neta, pa ih je uvek dvaput više nego žica.
/// </para>
/// <para>
/// U modelu i u .c61 fajlu <see cref="CableNet"/> znači nešto drugo — grupu međusobno spojenih
/// tačaka („C01-D01"), jednu po žici. To se NE menja: tester traži baš takvu net listu, a
/// program sa po jednom tačkom u netu ne bi ispitao ništa. Ovaj red je zato samo prikaz — jedan
/// <see cref="CableNet"/> daje onoliko redova koliko ima tačaka, a svi nose isti
/// <see cref="Connection"/> i isti ishod.
/// </para>
/// <para>
/// Kolona <see cref="Pins"/> postoji zbog zamke u oznakama: „C01" je <b>tačka</b> 01 konektora
/// C, a pinovi te tačke su „A01" i „B01" istog konektora. Bez te kolone operater vidi samo
/// „C01" i nema odakle da zna u koja dva pina adaptera ukrcava kraj žice; vidi
/// <see cref="TestPoint.Pins"/>.
/// </para>
/// </remarks>
/// <param name="Ordinal">Redni broj reda.</param>
/// <param name="Point">Tačka testera tog kraja, npr. „C01" — konektor C, tačka 01.</param>
/// <param name="Pins">Par pinova te tačke u istom konektoru, npr. „A01+B01"; „—" kad se ne zna.</param>
/// <param name="Wire">Oznaka žice kojoj kraj pripada, npr. „BR"; „—" kad se ne zna.</param>
/// <param name="Connection">Veza kojoj kraj pripada, npr. „C01-D01".</param>
/// <param name="Status">„PROŠAO", „PAO" ili „—" dok rezultata nema.</param>
/// <param name="Defect">Opis greške na toj vezi; prazno ako greške nema.</param>
public sealed record NetRow(
    int Ordinal,
    string Point,
    string Pins,
    string Wire,
    string Connection,
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

    /// <summary>
    /// Tekst oblačića: greška ako je ima, inače gde se kraj ukrcava i kojoj vezi pripada.
    /// </summary>
    public string Description
    {
        get
        {
            if (Defect.Length > 0)
            {
                return Defect;
            }

            return TestPoint.TryParse(Point, out TestPoint point)
                ? $"{point.Describe()} · veza {Connection}"
                : Connection;
        }
    }
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
                // Greška se ispisuje jezikom crteža — terminali i boja žice — a ne tačkama testera.
                string text = DefectTranslator.Describe(defect, runCable);

                foreach (string point in SplitPoints(defect.Points))
                {
                    faulty.TryAdd(point, text);
                }
            }
        }

        var rows = new List<NetRow>();
        int ordinal = 0;

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

            // Ishod tester javlja po VEZI, ne po kraju — oba kraja iste veze nose isto stanje.
            string status = !sameCable
                ? NetRow.Unknown
                : defect.Length > 0 ? NetRow.Failed : NetRow.Passed;

            if (points.Length == 0)
            {
                // Net bez ijedne tačke ne bi trebalo da postoji, ali ako se nađe u starijem
                // zapisu, mora da se vidi. Prećutno izostavljen red značio bi net koji niko ne
                // ispituje, a niko i ne primećuje da nedostaje.
                rows.Add(new NetRow(
                    ++ordinal, NetRow.Unknown, NetRow.Unknown, NetRow.Unknown, net.Points, status, defect));
                continue;
            }

            foreach (string point in points)
            {
                rows.Add(new NetRow(
                    ++ordinal, point, PinsOf(point), WireOf(cable, point), net.Points, status, defect));
            }
        }

        return rows;
    }

    /// <summary>
    /// Par pinova zadate tačke, npr. „A01+B01" za tačku „C01"; „—" kad oznaka nije ispravna.
    /// </summary>
    /// <remarks>
    /// Par ne bira niko — sledi iz same tačke: oba pina su u istom konektoru i nose njen broj.
    /// Zato ovde nema ni baze ni dodele, nego samo <see cref="TestPoint.Pins"/>.
    /// </remarks>
    public static string PinsOf(string? point)
        => TestPoint.TryParse(point, out TestPoint parsed) ? parsed.Pins : NetRow.Unknown;

    /// <summary>
    /// Oznaka žice čiji je kraj na zadatoj tački testera; „—" kad se ne zna.
    /// </summary>
    /// <remarks>
    /// Put je tačka → terminal → provodnik: priključna tabela kaže koji terminal ide na koju
    /// tačku, a ožičenje koji provodnik dodiruje taj terminal. Za starije kablove bez ožičenja
    /// nema šta da se nađe, pa ostaje „—".
    /// </remarks>
    private static string WireOf(Cable cable, string point)
    {
        CableTerminal? terminal = cable.FindTerminalByPoint(point);

        if (terminal is null)
        {
            return NetRow.Unknown;
        }

        CableWire? wire = cable.FindWireOfTerminal(terminal.Label);

        return string.IsNullOrWhiteSpace(wire?.Color) ? NetRow.Unknown : wire!.Color;
    }
}
