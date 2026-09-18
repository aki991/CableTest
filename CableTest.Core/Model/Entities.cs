namespace CableTest.Core.Model;

/// <summary>Vozilo za koje se radi kablovski set (tabela Vehicle).</summary>
public sealed class Vehicle
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public override string ToString() => Name;
}

/// <summary>
/// Jedan kabl (kablovski set) jednog vozila (tabela Cable).
/// </summary>
/// <remarks>
/// Model je: jedan kabl — jedan .c61 fajl, imenovan po <see cref="SpecFileName"/>.
/// Ograničenje od 500 test programa odnosi se na internu memoriju testera, ne na spec folder
/// na disku, a u tester se učitava samo program koji je trenutno potreban. Zato se kablovi ne
/// grupišu u zajednički fajl — time kolona "Filename" u CSV rezultatu jednoznačno određuje kabl.
/// </remarks>
public sealed class Cable
{
    public long Id { get; set; }
    public long VehicleId { get; set; }

    /// <summary>Oznaka kabla, npr. "M100-W1".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Grupa kojoj kabl pripada — deo oznake pre prve crtice, npr. "40" za "40-W1.1".
    /// </summary>
    /// <remarks>
    /// Grupa nije zasebno polje u bazi nego se čita iz oznake, jer to i jeste: oznaka na crtežu
    /// počinje brojem grupe. Zaseban podatak bi mogao da se razlikuje od oznake, a onda nijedan
    /// od ta dva ne bi bio pouzdan.
    /// <para>
    /// Oznaka bez crtice nema grupu i vraća se prazno — takav kabl se prikazuje odvojeno, a ne
    /// gura se u tuđu grupu.
    /// </para>
    /// </remarks>
    public string Group
    {
        get
        {
            int crtica = Code.IndexOf('-');
            return crtica <= 0 ? string.Empty : Code[..crtica].Trim();
        }
    }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Ime spec fajla bez ekstenzije, npr. "M100-W1". Proverava ga
    /// <see cref="Spec.SpecFileNameValidator"/>; ekstenziju dodaje generator.
    /// </summary>
    public string SpecFileName { get; set; } = string.Empty;

    /// <summary>Ime fajla na disku, npr. "M100-W1.c61".</summary>
    public string SpecFileNameWithExtension => SpecFileName + Spec.SpecFileNameValidator.Extension;

    /// <summary>Oznaka sa elektro crteža, npr. "=40-W1.1". Prazno za starije zapise.</summary>
    public string Designation { get; set; } = string.Empty;

    /// <summary>Tip kabla sa crteža, npr. "Wabco 2x1,5 mm²".</summary>
    public string CableType { get; set; } = string.Empty;

    /// <summary>Dužina kabla u metrima; 0 kad nije poznata.</summary>
    public decimal LengthM { get; set; }

    /// <summary>Napomene sa crteža, doslovno prepisane.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Iz kog dokumenta su podaci, npr. "40_grupa_2_0.pdf".</summary>
    public string SourceDocument { get; set; } = string.Empty;

    /// <summary>Strana dokumenta; 0 kad nije poznata.</summary>
    public int SourcePage { get; set; }

    /// <summary>
    /// Da li se kabl nudi za ispitivanje.
    /// </summary>
    /// <remarks>
    /// Kabl nad kojim je već nešto ispitano se ne briše ni kad izađe iz upotrebe — istorija je
    /// dokaz da je ispitan. Umesto brisanja se gasi, pa nestaje iz padajuće liste, a ostaje u
    /// istoriji i u mernim kartama.
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Net lista kabla, sortirana po <see cref="CableNet.Ordinal"/>.
    /// </summary>
    /// <remarks>
    /// Za kabl koji ima ožičenje (<see cref="Wires"/>) ovo je <b>izvedena</b> vrednost — računa je
    /// <see cref="NetBuilder"/> iz terminala i provodnika, i ne unosi se ručno. Za starije kablove
    /// bez ožičenja ostaje ono što je upisano u tabeli <c>Net</c>.
    /// </remarks>
    public List<CableNet> Nets { get; } = new();

    /// <summary>Krajevi kabla, onako kako stoje na crtežu.</summary>
    public List<CableTerminal> Terminals { get; } = new();

    /// <summary>Provodnici kabla: žice i mostovi.</summary>
    public List<CableWire> Wires { get; } = new();

    /// <summary>Da li je ožičenje uneto — tada je ono izvor istine za net listu.</summary>
    public bool HasWiring => Wires.Count > 0 && Terminals.Count > 0;

    /// <summary>
    /// Da li je bar jedna dodela tačke testera privremena, tj. nije potvrđena na adapteru.
    /// </summary>
    public bool HasProvisionalPoints => Terminals.Any(t => t.IsProvisional && t.HasTesterPoint);

    /// <summary>Dužina u obliku za prikaz, npr. „4,5 m"; „—" kad nije poznata.</summary>
    public string LengthText => LengthM <= 0 ? "—" : Decimals.Format(LengthM) + " m";

    /// <summary>Izvor podataka za prikaz, npr. „40_grupa_2_0.pdf, strana 1".</summary>
    public string SourceText => string.IsNullOrWhiteSpace(SourceDocument)
        ? "—"
        : SourcePage > 0
            ? $"{SourceDocument}, strana {SourcePage.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : SourceDocument;

    /// <summary>Terminal po oznaci; <c>null</c> ako ga nema.</summary>
    public CableTerminal? FindTerminal(string? label)
        => string.IsNullOrWhiteSpace(label)
            ? null
            : Terminals.FirstOrDefault(t => string.Equals(t.Label, label.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Terminal koji je dodeljen zadatoj tački testera; <c>null</c> ako je nijedan nema.</summary>
    public CableTerminal? FindTerminalByPoint(string? testerPoint)
    {
        if (string.IsNullOrWhiteSpace(testerPoint))
        {
            return null;
        }

        string point = testerPoint.Trim();
        return Terminals.FirstOrDefault(t =>
            t.HasTesterPoint && string.Equals(t.TesterPoint, point, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Provodnik koji dodiruje zadati terminal; <c>null</c> ako ga nema.</summary>
    public CableWire? FindWireOfTerminal(string? label)
        => string.IsNullOrWhiteSpace(label)
            ? null
            : Wires.OrderBy(w => w.WireNo).FirstOrDefault(w =>
                string.Equals(w.FromTerminal, label.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(w.ToTerminal, label.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Da li vrednost kolone "Filename" iz CSV rezultata pripada ovom kablu.
    /// Poređenje je bez obzira na velika/mala slova i podnosi zapis sa ekstenzijom.
    /// </summary>
    public bool MatchesResultFileName(string? resultFileName)
        => string.Equals(NormalizeResultFileName(resultFileName), SpecFileName, StringComparison.OrdinalIgnoreCase)
           && SpecFileName.Length > 0;

    /// <summary>Nalazi kabl kome pripada rezultat; <c>null</c> ako nijedan ne odgovara.</summary>
    public static Cable? FindByResultFileName(IEnumerable<Cable> cables, string? resultFileName)
    {
        ArgumentNullException.ThrowIfNull(cables);
        return cables.FirstOrDefault(c => c.MatchesResultFileName(resultFileName));
    }

    /// <summary>Skida razmake i eventualnu ekstenziju sa vrednosti kolone "Filename".</summary>
    public static string NormalizeResultFileName(string? resultFileName)
    {
        if (string.IsNullOrWhiteSpace(resultFileName))
        {
            return string.Empty;
        }

        string name = resultFileName.Trim();
        if (name.EndsWith(Spec.SpecFileNameValidator.Extension, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^Spec.SpecFileNameValidator.Extension.Length];
        }

        return name;
    }

    public override string ToString() => Code;
}

/// <summary>
/// Jedan net kabla (tabela Net). U bazi se čuva kao tekst ("O01-O02-O31-O32"),
/// a <see cref="Net"/> je parsirani oblik.
/// </summary>
public sealed class CableNet
{
    public long Id { get; set; }
    public long CableId { get; set; }

    /// <summary>Redni broj neta u listi, počinje od 1.</summary>
    public int Ordinal { get; set; }

    /// <summary>Zapis neta kako ide u .c61 fajl.</summary>
    public string Points { get; set; } = string.Empty;

    /// <summary>Parsirani net. Baca <see cref="FormatException"/> ako je zapis u bazi neispravan.</summary>
    public Net ToNet() => Net.Parse(Points);

    public override string ToString() => Points;
}

/// <summary>Vrsta greške koju je tester prijavio.</summary>
public enum DefectKind
{
    /// <summary>Kratak spoj između tačaka koje ne bi smele da budu spojene.</summary>
    Short,

    /// <summary>Prekid — tačke koje bi trebalo da su spojene nisu.</summary>
    Open,

    /// <summary>Poruka koju parser ne prepoznaje; čuva se kao sirov tekst.</summary>
    Unknown
}

/// <summary>Jedan izvršen test, tj. jedan red iz CSV fajla CableConnector-a (tabela TestRun).</summary>
public sealed class TestRun
{
    public long Id { get; set; }

    /// <summary>
    /// Kabl kome rezultat pripada, pronađen poređenjem <see cref="SpecFileName"/> sa
    /// <see cref="Cable.SpecFileName"/>. <c>null</c> kada ime iz CSV-a ne odgovara nijednom kablu —
    /// takav rezultat se ipak čuva, jer je operater možda pokrenuo test iz drugog spec fajla
    /// i to treba da se vidi u istoriji.
    /// </summary>
    public long? CableId { get; set; }

    /// <summary>Da li je rezultat vezan za kabl iz baze.</summary>
    public bool IsRecognized => CableId.HasValue;

    /// <summary>Redni broj iz kolone "Seq." u CSV-u.</summary>
    public int Seq { get; set; }

    /// <summary>Vrednost kolone "Filename" iz CSV-a, bez ekstenzije.</summary>
    public string SpecFileName { get; set; } = string.Empty;

    /// <summary>true ako je kolona "Pass" imala vrednost "pass".</summary>
    public bool Passed { get; set; }

    public DateTime TestedAt { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    /// <summary>Ceo sirov red iz CSV-a, za kasniju proveru.</summary>
    public string RawRow { get; set; } = string.Empty;

    public List<TestDefect> Defects { get; } = new();

    public string OutcomeText => Passed ? "PASS" : "FAIL";
}

/// <summary>Jedna greška prijavljena u okviru jednog testa (tabela TestDefect).</summary>
public sealed class TestDefect
{
    public long Id { get; set; }
    public long TestRunId { get; set; }
    public DefectKind Kind { get; set; }

    /// <summary>Tačke iz poruke, npr. "O01-O02". Prazno ako ih poruka nema.</summary>
    public string Points { get; set; } = string.Empty;

    /// <summary>Sirova poruka iz CSV-a, npr. "SHORT O01-O02".</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>Opis greške na srpskom, za prikaz operateru.</summary>
    public string Describe()
    {
        string[] points = string.IsNullOrWhiteSpace(Points)
            ? Array.Empty<string>()
            : Points.Split(Net.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string pointList = points.Length switch
        {
            0 => string.Empty,
            1 => $"tački {points[0]}",
            2 => $"tačaka {points[0]} i {points[1]}",
            _ => $"tačaka {string.Join(", ", points[..^1])} i {points[^1]}"
        };

        return Kind switch
        {
            DefectKind.Short when points.Length >= 2 => $"Kratak spoj između {pointList}",
            DefectKind.Short => $"Kratak spoj na {pointList}",
            DefectKind.Open when points.Length >= 2 => $"Prekid između {pointList}",
            DefectKind.Open => $"Prekid na {pointList}",
            _ => string.IsNullOrWhiteSpace(RawText) ? "Nepoznata greška" : $"Nepoznata poruka testera: {RawText}"
        };
    }

    public override string ToString() => Describe();
}
