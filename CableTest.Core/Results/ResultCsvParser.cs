using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CableTest.Core.Model;

namespace CableTest.Core.Results;

/// <summary>
/// Parser CSV fajla sa rezultatima koji upisuje CableConnector.
/// </summary>
/// <remarks>
/// <para>
/// Fajl počinje zaglavljem koje se preskače do reda koji počinje sa "Seq.," — taj red daje imena
/// kolona. Broj kolona nije fiksan: uključivanjem dodatnih test stavki (npr. merena provodna
/// otpornost) pojavljuju se nove kolone, pa se sve čita po imenu kolone, nikad po rednom broju.
/// </para>
/// <para>
/// Parser pamti koliko je redova već obradio, pa se pri ponovnom čitanju istog fajla vraćaju samo
/// novi redovi. Nepotpun poslednji red (fajl je uhvaćen usred upisa) se ne obrađuje dok se ne
/// dopuni. Nepoznata poruka o grešci ne prekida obradu — čuva se kao sirov tekst.
/// </para>
/// </remarks>
public sealed class ResultCsvParser
{
    /// <summary>Red koji nosi imena kolona počinje ovako.</summary>
    public const string HeaderPrefix = "Seq.";

    private static readonly string[] SeqNames = { "Seq.", "Seq", "No", "No." };
    private static readonly string[] FileNameNames = { "Filename", "File Name", "FileName" };
    private static readonly string[] PassNames = { "Pass", "Result" };
    private static readonly string[] DateNames = { "Date" };
    private static readonly string[] TimeNames = { "Time" };
    private static readonly string[] BarcodeNames = { "Barcode1", "Barcode" };
    private static readonly string[] OperatorNames = { "Operater", "Operator" };

    /// <summary>Kolone sa podacima o testu; sve ostale kolone su test stavke i u njima se traže greške.</summary>
    private static readonly HashSet<string> MetaColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Seq.", "Seq", "No", "No.", "Filename", "File Name", "FileName", "Pass", "Result",
        "Date", "Time", "Lots", "Lot", "Barcode1", "Barcode", "Barcode2", "Operater", "Operator",
        "unit", "units"
    };

    /// <summary>Reči koje su ishod, a ne poruka o grešci.</summary>
    private static readonly HashSet<string> Verdicts = new(StringComparer.OrdinalIgnoreCase)
    {
        "PASS", "FAIL", "OK", "NG", "GOOD", "SKIP", "NONE", "-", "--", "---"
    };

    private static readonly (string Keyword, DefectKind Kind)[] DefectKeywords =
    {
        ("SHORT", DefectKind.Short),
        ("OPEN", DefectKind.Open)
    };

    /// <summary>
    /// Oblik merene vrednosti: broj sa opcionim znakom, decimalnim delom, eksponentom i jedinicom.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PRETPOSTAVKA, ne znanje. Ovaj izraz je napravljen na osnovu <b>jednog jedinog stvarnog CSV
    /// fajla sa dva reda</b>, u kome je bio uključen samo Open/Short test i u kome zato nema
    /// nijedne izmerene vrednosti. Kako tačno CableConnector zapisuje izmerenu provodnu otpornost
    /// (broj decimala, jedinicu, razmak pre jedinice, prefiks m/µ/k, oznaku Ω ili "Ohm",
    /// eksponentni zapis) nije potvrđeno — biće poznato tek kad stignu fajlovi sa uključenim
    /// Cond testom.
    /// </para>
    /// <para>
    /// Zato je izraz namerno na jednom mestu i namerno širok: bolje je propustiti vrednost koja
    /// liči na broj nego od nje napraviti lažnu grešku. Kad stigne stvarni fajl, dopunjuje se
    /// ovde i samo ovde, a <c>MeasurementPatternTests</c> nabraja varijante koje pokrivamo —
    /// nova varijanta se dodaje jednim redom u taj test.
    /// </para>
    /// <para>Delovi izraza:</para>
    /// <list type="bullet">
    ///   <item><description>znak: <c>+</c> ili <c>-</c>, opciono</description></item>
    ///   <item><description>broj: <c>0</c>, <c>0.12</c>, <c>12.</c> ili <c>.12</c> (bez celog dela)</description></item>
    ///   <item><description>eksponent: <c>E-02</c>, <c>e+3</c>, opciono</description></item>
    ///   <item><description>jedinica: do 8 znakova, npr. <c>m</c>, <c>R</c>, <c>mOhm</c>, <c>mΩ</c>, <c>%</c>, sa ili bez razmaka</description></item>
    /// </list>
    /// </remarks>
    public const string MeasurementRegex =
        @"^[+-]?(\d+([.]\d*)?|[.]\d+)([eE][+-]?\d+)?\s*[A-Za-z%/°µΩΩ]{0,8}$";

    /// <summary>Broj (sa opcionom jedinicom) — merena vrednost, ne poruka o grešci.</summary>
    private static readonly Regex MeasurementPattern = new(
        MeasurementRegex,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Da li vrednost ćelije izgleda kao merena vrednost (broj sa opcionom jedinicom),
    /// a ne kao poruka o grešci. Oblik je pretpostavka — vidi <see cref="MeasurementRegex"/>.
    /// </summary>
    public static bool IsMeasuredValue(string? text)
        => !string.IsNullOrWhiteSpace(text) && MeasurementPattern.IsMatch(text.Trim());

    /// <summary>
    /// Da li se vrednost ćelije tumači kao poruka o grešci. Prazna vrednost, ishod ("PASS",
    /// "--") i merena vrednost ("0.12", "51.2m") nisu greška; sve ostalo jeste, pa i poruka
    /// koju ne prepoznajemo — ona se čuva kao sirov tekst umesto da se tiho izgubi.
    /// </summary>
    public static bool IsDefectMessage(string? text)
    {
        string message = text?.Trim() ?? string.Empty;
        return message.Length > 0 && !Verdicts.Contains(message) && !IsMeasuredValue(message);
    }

    private static readonly string[] TimestampFormats =
    {
        "yyyy/MM/dd HH:mm:ss", "yyyy/M/d H:mm:ss", "yyyy/MM/dd HH:mm",
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd H:mm:ss"
    };

    private static readonly string[] DateFormats = { "yyyy/MM/dd", "yyyy/M/d", "yyyy-MM-dd" };

    private string[] _columns = Array.Empty<string>();
    private int _processedLines;

    /// <summary>Broj redova fajla koji su već obrađeni.</summary>
    public int ProcessedLineCount => _processedLines;

    /// <summary>Da li je red sa imenima kolona već pronađen.</summary>
    public bool HasHeader => _columns.Length > 0;

    /// <summary>Zaboravlja stanje — koristi se kada je fajl zamenjen novim (dnevna rotacija).</summary>
    public void Reset()
    {
        _columns = Array.Empty<string>();
        _processedLines = 0;
    }

    /// <summary>Pročita ceo CSV od početka. Za jednokratno čitanje.</summary>
    public static CsvParseResult ParseAll(string text) => new ResultCsvParser().ReadNew(text);

    /// <summary>
    /// Čita samo redove koji se pojavili od prethodnog poziva. Ako je fajl postao kraći
    /// (zamenjen novim), stanje se resetuje i fajl se čita od početka.
    /// </summary>
    public CsvParseResult ReadNew(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var runs = new List<TestRun>();
        var warnings = new List<string>();

        List<string> lines = SplitCompleteLines(text);

        if (lines.Count < _processedLines)
        {
            warnings.Add(
                $"CSV fajl je kraći nego ranije ({lines.Count.ToString(CultureInfo.InvariantCulture)} " +
                $"umesto {_processedLines.ToString(CultureInfo.InvariantCulture)} redova) — " +
                "izgleda da je zamenjen novim. Čita se od početka.");
            Reset();
        }

        for (int i = _processedLines; i < lines.Count; i++)
        {
            ProcessLine(lines[i], i + 1, runs, warnings);
        }

        _processedLines = lines.Count;
        return new CsvParseResult(runs, warnings);
    }

    /// <summary>
    /// Uklanja zapise koji se ponavljaju po prirodnom ključu <see cref="TestRunKey"/>
    /// (<c>SpecFileName</c> + <c>Seq</c> + <c>TestedAt</c>). Zadržava prvi zapis svakog ključa.
    /// </summary>
    /// <param name="runs">Pročitani zapisi, redom kojim su u fajlu.</param>
    /// <param name="warnings">
    /// Ako je zadato, za svaki preskočen zapis se dopisuje upozorenje. Preskakanje je tiho —
    /// ne prekida obradu — ali se mora videti, jer znači da je isti rezultat stigao dvaput.
    /// </param>
    /// <remarks>
    /// Potrebno je zato što parser, kada fajl postane kraći nego ranije, čita ceo fajl od početka
    /// (rukovanje dnevnom rotacijom). Ako je fajl prepisan istim imenom umesto zamenjen novim,
    /// ranije pročitani redovi dolaze po drugi put. Isto pravilo sprovodi i jedinstveni indeks
    /// nad <c>(SpecFileName, Seq, TestedAt)</c> u bazi.
    /// </remarks>
    public static List<TestRun> RemoveDuplicates(IEnumerable<TestRun> runs, ICollection<string>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(runs);

        var seen = new HashSet<TestRunKey>();
        var result = new List<TestRun>();

        foreach (TestRun run in runs)
        {
            if (run is null)
            {
                continue;
            }

            TestRunKey key = TestRunKey.Of(run);
            if (seen.Add(key))
            {
                result.Add(run);
                continue;
            }

            warnings?.Add($"Preskočen duplikat rezultata ({key}) — isti zapis je već pročitan.");
        }

        return result;
    }

    private void ProcessLine(string line, int lineNumber, List<TestRun> runs, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        string trimmed = line.TrimStart();

        // Red sa imenima kolona. Fajl ih može imati više puta ako su rezultati dopisivani
        // za različite spec fajlove — svaki put se preuzima novo zaglavlje.
        if (trimmed.StartsWith(HeaderPrefix + ",", StringComparison.OrdinalIgnoreCase))
        {
            _columns = SplitCsvLine(line).Select(c => c.Trim()).ToArray();
            return;
        }

        if (!HasHeader)
        {
            // Zaglavlje fajla ("File Name:", "Model:", crte, spisak netova) — preskače se.
            return;
        }

        string[] fields = SplitCsvLine(line);

        // Prva kolona mora biti redni broj; sve ostalo je neki drugi red u fajlu.
        string seqText = Field(fields, SeqNames).Trim();
        if (!int.TryParse(seqText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seq))
        {
            if (!IsSeparator(trimmed))
            {
                warnings.Add($"Red {lineNumber.ToString(CultureInfo.InvariantCulture)} nije prepoznat kao rezultat: {line.Trim()}");
            }

            return;
        }

        var run = new TestRun
        {
            Seq = seq,
            SpecFileName = Cable.NormalizeResultFileName(Field(fields, FileNameNames)),
            Operator = Field(fields, OperatorNames).Trim(),
            Barcode = Field(fields, BarcodeNames).Trim(),
            RawRow = line
        };

        string pass = Field(fields, PassNames).Trim();
        if (string.Equals(pass, "pass", StringComparison.OrdinalIgnoreCase))
        {
            run.Passed = true;
        }
        else if (string.Equals(pass, "fail", StringComparison.OrdinalIgnoreCase))
        {
            run.Passed = false;
        }
        else
        {
            run.Passed = false;
            warnings.Add(
                $"Red {lineNumber.ToString(CultureInfo.InvariantCulture)}: nepoznat ishod \"{pass}\" " +
                "u koloni Pass; upisano je FAIL.");
        }

        WarnOnExtraColumns(fields, lineNumber, warnings);

        run.TestedAt = ParseTimestamp(
            Field(fields, DateNames),
            Field(fields, TimeNames),
            lineNumber,
            warnings);

        foreach (TestDefect defect in ExtractDefects(fields))
        {
            run.Defects.Add(defect);
        }

        runs.Add(run);
    }

    /// <summary>
    /// Javlja kad red ima više popunjenih polja nego što zaglavlje najavljuje kolona.
    /// </summary>
    /// <remarks>
    /// Red sa <b>manje</b> polja je normalna pojava — stvarni fajl iz pogona izostavlja poslednju
    /// kolonu ("unit") — pa se za njega ne javlja ništa; polja koja nedostaju se čitaju kao prazna,
    /// a ako nedostaje nešto bitno (datum, ishod) upozorenje dolazi odatle. Red sa <b>više</b>
    /// popunjenih polja znači da se skup kolona promenio bez novog zaglavlja i to treba da se vidi.
    /// Vrednosti iz nenajavljenih kolona se svejedno pregledaju, da se poruka o grešci ne izgubi.
    /// </remarks>
    private void WarnOnExtraColumns(string[] fields, int lineNumber, List<string> warnings)
    {
        if (fields.Length <= _columns.Length)
        {
            return;
        }

        int filled = 0;
        for (int i = _columns.Length; i < fields.Length; i++)
        {
            if (fields[i].Trim().Length > 0)
            {
                filled++;
            }
        }

        if (filled == 0)
        {
            return;
        }

        warnings.Add(
            $"Red {lineNumber.ToString(CultureInfo.InvariantCulture)} ima " +
            $"{fields.Length.ToString(CultureInfo.InvariantCulture)} polja, a zaglavlje najavljuje " +
            $"{_columns.Length.ToString(CultureInfo.InvariantCulture)} kolona; " +
            $"{filled.ToString(CultureInfo.InvariantCulture)} nenajavljenih polja nije prazno.");
    }

    /// <summary>Traži greške u svim kolonama test stavki; imena kolona i njihov broj nisu fiksni.</summary>
    private IEnumerable<TestDefect> ExtractDefects(string[] fields)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            string columnName = i < _columns.Length ? _columns[i] : string.Empty;
            if (columnName.Length > 0 && MetaColumns.Contains(columnName))
            {
                continue;
            }

            foreach (string token in fields[i].Split(';'))
            {
                if (!IsDefectMessage(token))
                {
                    continue;
                }

                yield return CreateDefect(token);
            }
        }
    }

    /// <summary>Pravi grešku iz jedne poruke, npr. "SHORT O01-O02".</summary>
    public static TestDefect CreateDefect(string message)
    {
        string text = message.Trim();

        foreach ((string keyword, DefectKind kind) in DefectKeywords)
        {
            if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string rest = text[keyword.Length..];
            if (rest.Length > 0 && rest[0] is not (' ' or ':' or '=' or '\t'))
            {
                continue; // npr. "OPENING" nije "OPEN"
            }

            return new TestDefect
            {
                Kind = kind,
                Points = ExtractPoints(rest),
                RawText = text
            };
        }

        return new TestDefect
        {
            Kind = DefectKind.Unknown,
            Points = string.Empty,
            RawText = text
        };
    }

    /// <summary>Izvlači oznake tačaka iz ostatka poruke; vraća prazan tekst ako ih nema.</summary>
    private static string ExtractPoints(string rest)
    {
        string candidate = rest.Trim(' ', ':', '=', '\t');
        int end = candidate.IndexOfAny(new[] { ' ', '\t', ',', '(' });
        if (end >= 0)
        {
            candidate = candidate[..end];
        }

        if (candidate.Length == 0)
        {
            return string.Empty;
        }

        string[] parts = candidate.Split(Net.Separator, StringSplitOptions.RemoveEmptyEntries);
        var points = new List<TestPoint>(parts.Length);
        foreach (string part in parts)
        {
            if (!TestPoint.TryParse(part, out TestPoint point))
            {
                return string.Empty; // poruka ima prepoznat tip, ali tačke nisu čitljive — ostaje sirov tekst
            }

            points.Add(point);
        }

        return string.Join(Net.Separator, points);
    }

    private static DateTime ParseTimestamp(string dateText, string timeText, int lineNumber, List<string> warnings)
    {
        string date = dateText.Trim();
        string time = timeText.Trim();

        // InvariantCulture: na srpskoj kulturi separatori datuma i vremena su drugačiji,
        // pa bi lokalno parsiranje "2026/09/07 11:44:11" palo ili dalo pogrešan datum.
        if (DateTime.TryParseExact($"{date} {time}", TimestampFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime timestamp))
        {
            return timestamp;
        }

        if (DateTime.TryParseExact(date, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime parsedDate))
        {
            warnings.Add($"Red {lineNumber.ToString(CultureInfo.InvariantCulture)}: vreme \"{time}\" nije prepoznato.");
            return parsedDate;
        }

        warnings.Add(
            $"Red {lineNumber.ToString(CultureInfo.InvariantCulture)}: datum \"{date}\" i vreme \"{time}\" " +
            "nisu prepoznati (očekuje se yyyy/MM/dd i HH:mm:ss).");
        return DateTime.MinValue;
    }

    private string Field(string[] fields, string[] names)
    {
        foreach (string name in names)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (string.Equals(_columns[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return i < fields.Length ? fields[i] : string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private static bool IsSeparator(string line)
        => line.Length > 0 && line.TrimEnd().All(c => c is '-' or '=' or '*' or ',');

    /// <summary>
    /// Deli red fajla na redove teksta. Nepotpun poslednji red (bez preloma na kraju) se izostavlja
    /// jer ga CableConnector možda još upisuje.
    /// </summary>
    private static List<string> SplitCompleteLines(string text)
    {
        var lines = new List<string>();
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            int end = i;
            if (end > start && text[end - 1] == '\r')
            {
                end--;
            }

            lines.Add(text[start..end]);
            start = i + 1;
        }

        return lines;
    }

    /// <summary>
    /// Deli jedan red na polja. Podržava navodnike po RFC 4180, jer barkod ili ime mogu sadržati zarez.
    /// </summary>
    public static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
