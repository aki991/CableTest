using System.Globalization;
using System.Text;
using CableTest.Core.Model;

namespace CableTest.Core.Spec;

/// <summary>
/// Generiše .c61 test program za CableConnector V3.12.17.
/// </summary>
/// <remarks>
/// Zaglavlje se NIKADA ne generiše programski. Polazi se od šablona (templates\MASTER.c61)
/// koji je snimljen samim CableConnector-om, pa su sve vrednosti u njemu garantovano iz
/// dozvoljenog skupa. Menja se isključivo blok "OSNet=" / "OSNet:", sve ostalo ostaje
/// bajt po bajt nepromenjeno. Ako se u zaglavlje upiše vrednost koju CableConnector nema
/// u svojim padajućim listama, on se ruši sa "Index was outside the bounds of the array".
/// </remarks>
public sealed class C61Generator
{
    /// <summary>Prelom reda u .c61 fajlu. Nikada <see cref="Environment.NewLine"/>.</summary>
    public const string LineEnding = "\r\n";

    public const string NetCountPrefix = "OSNet=";
    public const string NetLinePrefix = "OSNet:";
    public const string EmptyNetMarker = "EMPTY";
    public const string FileExtension = SpecFileNameValidator.Extension;

    private readonly string _templateText;

    public C61Generator(string templateText)
    {
        ArgumentNullException.ThrowIfNull(templateText);
        EnsureAscii(templateText, "Šablon");

        if (IndexOfNetCountLine(SplitLines(templateText)) < 0)
        {
            throw new InvalidDataException(
                $"Šablon nije ispravan: nije pronađen red koji počinje sa \"{NetCountPrefix}\". " +
                "Očekuje se .c61 fajl snimljen iz CableConnector-a.");
        }

        _templateText = templateText;
    }

    /// <summary>Učitava šablon sa diska. Fajl mora biti čist ASCII, kakav CableConnector i piše.</summary>
    public static C61Generator FromFile(string templatePath)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException(
                $"Šablon \"{templatePath}\" ne postoji. Očekuje se fajl templates\\MASTER.c61 " +
                "snimljen iz CableConnector-a sa stvarnim proizvodnim parametrima.",
                templatePath);
        }

        byte[] bytes = File.ReadAllBytes(templatePath);
        foreach (byte b in bytes)
        {
            if (b > 127)
            {
                throw new InvalidDataException(
                    $"Šablon \"{templatePath}\" sadrži znak koji nije ASCII. " +
                    "CableConnector piše i čita čist ASCII, pa šablon mora biti takav.");
            }
        }

        return new C61Generator(Encoding.ASCII.GetString(bytes));
    }

    /// <summary>Sadržaj šablona, neizmenjen.</summary>
    public string TemplateText => _templateText;

    /// <summary>
    /// Pravi sadržaj .c61 fajla za zadatu net listu. Vraća tekst sa CRLF prelomima.
    /// </summary>
    public string Build(IEnumerable<Net> nets)
    {
        ArgumentNullException.ThrowIfNull(nets);

        List<Net> netList = nets.ToList();
        ValidateNets(netList);

        string[] lines = SplitLines(_templateText);
        int start = IndexOfNetCountLine(lines);

        // Blok se prostire od reda "OSNet=" kroz sve uzastopne redove "OSNet:".
        int end = start + 1;
        while (end < lines.Length && lines[end].StartsWith(NetLinePrefix, StringComparison.Ordinal))
        {
            end++;
        }

        var result = new List<string>(lines.Length + netList.Count);
        result.AddRange(lines[..start]);

        // InvariantCulture: fajl je razdvojen zarezima, lokalni separatori ga ruše.
        result.Add(NetCountPrefix + netList.Count.ToString(CultureInfo.InvariantCulture));

        if (netList.Count == 0)
        {
            result.Add(NetLinePrefix + EmptyNetMarker);
        }
        else
        {
            foreach (Net net in netList)
            {
                result.Add(NetLinePrefix + net.ToString());
            }
        }

        result.AddRange(lines[end..]);

        string text = string.Join(LineEnding, result);
        EnsureAscii(text, "Generisani .c61 sadržaj");
        return text;
    }

    /// <summary>
    /// Upisuje .c61 fajl u ciljni folder. Vraća punu putanju upisanog fajla.
    /// Upis je uvek ASCII, bez BOM-a, sa CRLF prelomima.
    /// Jedan kabl — jedan fajl, imenovan po <see cref="Model.Cable.SpecFileName"/>.
    /// </summary>
    public string WriteSpec(string targetFolder, string specFileName, IEnumerable<Net> nets)
    {
        if (string.IsNullOrWhiteSpace(targetFolder))
        {
            throw new ArgumentException("Putanja spec foldera nije zadata.", nameof(targetFolder));
        }

        string fileName = BuildSpecFileName(specFileName);

        if (!Directory.Exists(targetFolder))
        {
            throw new DirectoryNotFoundException(
                $"Spec folder \"{targetFolder}\" ne postoji. Proveri putanju u Podešavanjima.");
        }

        string fullPath = Path.Combine(targetFolder, fileName);

        string text = Build(nets);
        File.WriteAllBytes(fullPath, Encoding.ASCII.GetBytes(text));
        return fullPath;
    }

    /// <summary>
    /// Od imena spec fajla pravi ime fajla na disku: proverava ime i dodaje nastavak .c61.
    /// Neispravno ime se odbija, ne "popravlja" — tiha izmena imena raskida vezu rezultata sa kablom.
    /// </summary>
    public static string BuildSpecFileName(string? specFileName)
    {
        SpecFileNameValidator.EnsureValid(specFileName, nameof(specFileName));
        return specFileName + FileExtension;
    }

    /// <summary>
    /// Proverava net listu pre generisanja: broj tačaka i da se ista tačka ne pojavljuje u dva neta.
    /// </summary>
    public static void ValidateNets(IReadOnlyList<Net> nets)
    {
        ArgumentNullException.ThrowIfNull(nets);

        var owner = new Dictionary<int, int>();
        for (int i = 0; i < nets.Count; i++)
        {
            Net net = nets[i] ?? throw new ArgumentException(
                $"Net broj {(i + 1).ToString(CultureInfo.InvariantCulture)} nije zadat.", nameof(nets));

            foreach (TestPoint point in net)
            {
                if (owner.TryGetValue(point.Index, out int firstNet))
                {
                    throw new InvalidOperationException(
                        $"Tačka {point} se pojavljuje u netu " +
                        $"{(firstNet + 1).ToString(CultureInfo.InvariantCulture)} i u netu " +
                        $"{(i + 1).ToString(CultureInfo.InvariantCulture)}. " +
                        "Jedna ispitna tačka može pripadati samo jednom netu.");
                }

                owner[point.Index] = i;
            }
        }

        if (owner.Count > TestPoint.TotalPoints)
        {
            throw new InvalidOperationException(
                $"Net lista koristi {owner.Count.ToString(CultureInfo.InvariantCulture)} tačaka, " +
                $"a tester ima {TestPoint.TotalPoints.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private static string[] SplitLines(string text) => text.Split(LineEnding);

    private static int IndexOfNetCountLine(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith(NetCountPrefix, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static void EnsureAscii(string text, string what)
    {
        foreach (char c in text)
        {
            if (c > 127)
            {
                throw new InvalidDataException(
                    $"{what} sadrži znak '{c}' koji nije ASCII. .c61 fajl mora biti čist ASCII.");
            }
        }
    }
}
