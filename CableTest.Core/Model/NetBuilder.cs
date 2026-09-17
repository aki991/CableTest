using System.Globalization;

namespace CableTest.Core.Model;

/// <summary>Ishod izvođenja net liste iz ožičenja i priključne tabele.</summary>
/// <param name="Nets">Izvedeni netovi, poređani kako idu u .c61; prazno kad ima grešaka.</param>
/// <param name="Errors">Zašto kabl nije spreman za ispitivanje; prazno kad je sve u redu.</param>
public sealed record NetDerivation(IReadOnlyList<CableNet> Nets, IReadOnlyList<string> Errors)
{
    /// <summary>Da li se iz ožičenja može napraviti ispravna net lista.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Sve greške u jednom tekstu, red po red.</summary>
    public string ErrorText => string.Join("\n", Errors);

    /// <summary>Netovi u obliku koji traži generator .c61.</summary>
    public IReadOnlyList<Net> ToNets() => Nets.Select(n => n.ToNet()).ToArray();
}

/// <summary>
/// Izvođenje net liste iz ožičenja kabla i priključne tabele.
/// </summary>
/// <remarks>
/// <para>
/// <b>Zašto se net lista ne unosi ručno.</b> Crtež opisuje povezivanje sa strane vozila (pin 2
/// DIN konektora, pin <c>10XF:02</c>, papučica), a tester poznaje samo tačke <c>A01</c>–<c>P32</c>.
/// Između to dvoje stoji priključni pribor na stolu. Zato se čuvaju dve odvojene stvari —
/// ožičenje (<see cref="CableWire"/>) i priključna tabela (<see cref="CableTerminal.TesterPoint"/>)
/// — a net lista se iz njih računa. Kad se napravi pravi adapter, menja se samo priključna tabela.
/// </para>
/// <para>
/// Terminali spojeni provodnicima čine jedan čvor (union-find). Svaki čvor je jedan net: tačke
/// testera njegovih terminala. Redosled netova prati redni broj prve žice u čvoru, a redosled
/// tačaka unutar neta ide po slovu porta pa po broju — tako je .c61 uvek isti za isti kabl, bez
/// obzira na redosled redova u bazi.
/// </para>
/// <para>
/// Sve što bi dalo net listu kojoj se ne može verovati zaustavlja generisanje .c61 i vraća se kao
/// poruka operateru. Program koji se upiše u tester mora da odgovara kablu na stolu — pogrešna
/// net lista bi značila da tester prijavi „prošao" za kabl koji nije ispitan kako treba.
/// </para>
/// </remarks>
public static class NetBuilder
{
    /// <summary>Poruka kad terminal u čvoru nema dodeljenu tačku testera.</summary>
    public const string NotReadyPrefix = "Kabl nije spreman za ispitivanje: ";

    /// <summary>Izvodi net listu kabla iz njegovih terminala i provodnika.</summary>
    public static NetDerivation Derive(Cable cable)
    {
        ArgumentNullException.ThrowIfNull(cable);
        return Derive(cable.Terminals, cable.Wires);
    }

    /// <summary>Izvodi net listu iz zadatih terminala i provodnika.</summary>
    public static NetDerivation Derive(IReadOnlyList<CableTerminal> terminals, IReadOnlyList<CableWire> wires)
    {
        ArgumentNullException.ThrowIfNull(terminals);
        ArgumentNullException.ThrowIfNull(wires);

        var errors = new List<string>();

        // Terminali po oznaci; ista oznaka dvaput je greška u unosu, ne u ožičenju.
        var byLabel = new Dictionary<string, CableTerminal>(StringComparer.OrdinalIgnoreCase);
        foreach (CableTerminal terminal in terminals)
        {
            if (!byLabel.TryAdd(terminal.Label, terminal))
            {
                errors.Add($"Terminal \"{terminal.Label}\" je naveden dvaput.");
            }
        }

        CheckTesterPoints(terminals, errors);

        var union = new UnionFind(terminals.Count);
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < terminals.Count; i++)
        {
            index[terminals[i].Label] = i;
        }

        // Najmanji redni broj žice u čvoru — po njemu se netovi poređaju.
        var firstWire = new Dictionary<int, int>();

        foreach (CableWire wire in wires.OrderBy(w => w.WireNo))
        {
            bool fromOk = index.TryGetValue(wire.FromTerminal, out int from);
            bool toOk = index.TryGetValue(wire.ToTerminal, out int to);

            if (!fromOk || !toOk)
            {
                string missing = !fromOk ? wire.FromTerminal : wire.ToTerminal;
                errors.Add(
                    $"Provodnik {wire.WireNo.ToString(CultureInfo.InvariantCulture)} pokazuje na terminal " +
                    $"\"{missing}\", koga u kablu nema.");
                continue;
            }

            union.Union(from, to);
        }

        if (errors.Count != 0)
        {
            return new NetDerivation(Array.Empty<CableNet>(), errors);
        }

        // Tek kad su svi spojevi napravljeni zna se kom čvoru koja žica pripada.
        foreach (CableWire wire in wires.OrderBy(w => w.WireNo))
        {
            int root = union.Find(index[wire.FromTerminal]);

            if (!firstWire.ContainsKey(root))
            {
                firstWire[root] = wire.WireNo;
            }
        }

        var nodes = new Dictionary<int, List<CableTerminal>>();

        for (int i = 0; i < terminals.Count; i++)
        {
            int root = union.Find(i);

            if (!nodes.TryGetValue(root, out List<CableTerminal>? group))
            {
                group = new List<CableTerminal>();
                nodes[root] = group;
            }

            group.Add(terminals[i]);
        }

        var nets = new List<CableNet>();
        int ordinal = 0;

        foreach ((int root, List<CableTerminal> group) in nodes
                     .Where(n => n.Value.Count > 1 || firstWire.ContainsKey(n.Key))
                     .OrderBy(n => firstWire.TryGetValue(n.Key, out int no) ? no : int.MaxValue))
        {
            var points = new List<TestPoint>();

            foreach (CableTerminal terminal in group)
            {
                if (!terminal.HasTesterPoint)
                {
                    errors.Add($"{NotReadyPrefix}terminal {terminal.Label} nema tačku testera.");
                    continue;
                }

                if (!TestPoint.TryParse(terminal.TesterPoint, out TestPoint point, out string? pointError))
                {
                    errors.Add($"Terminal {terminal.Label}: {pointError}");
                    continue;
                }

                points.Add(point);
            }

            if (errors.Count != 0)
            {
                continue;
            }

            if (points.Count < Net.MinimumPoints)
            {
                errors.Add(
                    $"Čvor sa terminalom {group[0].Label} ima samo " +
                    $"{points.Count.ToString(CultureInfo.InvariantCulture)} tačku testera; " +
                    "za ispitivanje su potrebne najmanje dve.");
                continue;
            }

            // Redosled tačaka: po slovu porta, pa po broju.
            points.Sort();

            ordinal++;
            nets.Add(new CableNet
            {
                CableId = group[0].CableId,
                Ordinal = ordinal,
                Points = new Net(points).ToString()
            });

            _ = root;
        }

        return errors.Count == 0
            ? new NetDerivation(nets, Array.Empty<string>())
            : new NetDerivation(Array.Empty<CableNet>(), errors);
    }

    /// <summary>
    /// Ista tačka testera ne sme da stoji uz dva terminala istog kabla — tester bi ih video kao
    /// spojene i prijavio kratak spoj koga na kablu nema.
    /// </summary>
    private static void CheckTesterPoints(IReadOnlyList<CableTerminal> terminals, List<string> errors)
    {
        var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (CableTerminal terminal in terminals)
        {
            if (!terminal.HasTesterPoint)
            {
                continue;
            }

            string point = terminal.TesterPoint!.Trim().ToUpperInvariant();

            if (used.TryGetValue(point, out string? first))
            {
                errors.Add(
                    $"Tačka testera {point} je dodeljena i terminalu {first} i terminalu {terminal.Label}. " +
                    "Jedna tačka može pripadati samo jednom terminalu.");
                continue;
            }

            used[point] = terminal.Label;
        }
    }

    /// <summary>Spajanje skupova; terminali spojeni žicama čine jedan čvor.</summary>
    private sealed class UnionFind
    {
        private readonly int[] _parent;

        public UnionFind(int count)
        {
            _parent = new int[count];
            for (int i = 0; i < count; i++)
            {
                _parent[i] = i;
            }
        }

        public int Find(int item)
        {
            while (_parent[item] != item)
            {
                // Skraćivanje puta: sledeće traženje ide direktno na koren.
                _parent[item] = _parent[_parent[item]];
                item = _parent[item];
            }

            return item;
        }

        public void Union(int left, int right)
        {
            int a = Find(left);
            int b = Find(right);

            if (a != b)
            {
                _parent[b] = a;
            }
        }
    }
}
