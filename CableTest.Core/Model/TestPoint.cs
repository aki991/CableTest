using System.Globalization;

namespace CableTest.Core.Model;

/// <summary>
/// Jedna ispitna tačka Microtest 8761NK testera.
/// Tester ima 16 portova (slova A..P), svaki sa 32 tačke (1..32) — ukupno 512.
/// Tekstualna oznaka je uvek slovo + dvocifren broj: "A01", "O31", "P32".
/// </summary>
public readonly struct TestPoint : IEquatable<TestPoint>, IComparable<TestPoint>
{
    public const int PortCount = 16;          // A..P
    public const int PointsPerPort = 32;      // 01..32
    public const int TotalPoints = PortCount * PointsPerPort; // 512

    public const char FirstPort = 'A';
    public const char LastPort = 'P';

    /// <summary>Slovo porta, uvek veliko, u opsegu A..P.</summary>
    public char Port { get; }

    /// <summary>Redni broj tačke unutar porta, 1..32.</summary>
    public int Number { get; }

    public TestPoint(char port, int number)
    {
        char normalized = char.ToUpperInvariant(port);
        if (normalized < FirstPort || normalized > LastPort)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port), port,
                $"Neispravan port '{port}'. Dozvoljena su slova {FirstPort}..{LastPort}.");
        }

        if (number < 1 || number > PointsPerPort)
        {
            throw new ArgumentOutOfRangeException(
                nameof(number), number,
                $"Neispravan broj tačke {number.ToString(CultureInfo.InvariantCulture)}. " +
                $"Dozvoljen opseg je 1..{PointsPerPort.ToString(CultureInfo.InvariantCulture)}.");
        }

        Port = normalized;
        Number = number;
    }

    /// <summary>Redni broj tačke u celom testeru, 0..511. Koristi se za brzu proveru duplikata.</summary>
    public int Index => (Port - FirstPort) * PointsPerPort + (Number - 1);

    /// <summary>
    /// Parsira oznaku tačke. Prihvata mala slova i jednocifren broj ("o1"),
    /// ali rezultat se uvek ispisuje kao veliko slovo + dvocifren broj ("O01").
    /// </summary>
    public static TestPoint Parse(string? text)
    {
        if (!TryParse(text, out TestPoint point, out string? error))
        {
            throw new FormatException(error);
        }

        return point;
    }

    public static bool TryParse(string? text, out TestPoint point)
        => TryParse(text, out point, out _);

    /// <summary>
    /// Parsira oznaku tačke i, u slučaju greške, vraća poruku razumljivu operateru.
    /// </summary>
    public static bool TryParse(string? text, out TestPoint point, out string? error)
    {
        point = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Oznaka tačke je prazna. Očekivan oblik je slovo A..P i broj 01..32, npr. \"O01\".";
            return false;
        }

        string raw = text.Trim();

        char port = char.ToUpperInvariant(raw[0]);
        if (port < FirstPort || port > LastPort)
        {
            error = $"Neispravna oznaka tačke \"{raw}\": port '{raw[0]}' nije u opsegu {FirstPort}..{LastPort}.";
            return false;
        }

        string digits = raw[1..];
        if (digits.Length is < 1 or > 2)
        {
            error = $"Neispravna oznaka tačke \"{raw}\": broj tačke mora imati jednu ili dve cifre, npr. \"O01\".";
            return false;
        }

        foreach (char c in digits)
        {
            if (c is < '0' or > '9')
            {
                error = $"Neispravna oznaka tačke \"{raw}\": \"{digits}\" nije broj.";
                return false;
            }
        }

        // InvariantCulture: na srpskom rasporedu parsiranje brojeva ne sme da zavisi od lokalnih pravila.
        int number = int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
        if (number is < 1 or > PointsPerPort)
        {
            error = $"Neispravna oznaka tačke \"{raw}\": broj {number.ToString(CultureInfo.InvariantCulture)} " +
                    $"nije u opsegu 1..{PointsPerPort.ToString(CultureInfo.InvariantCulture)}.";
            return false;
        }

        point = new TestPoint(port, number);
        error = null;
        return true;
    }

    public override string ToString()
        => string.Concat(Port, Number.ToString("D2", CultureInfo.InvariantCulture));

    public bool Equals(TestPoint other) => Port == other.Port && Number == other.Number;

    public override bool Equals(object? obj) => obj is TestPoint other && Equals(other);

    public override int GetHashCode() => Index;

    public int CompareTo(TestPoint other) => Index.CompareTo(other.Index);

    public static bool operator ==(TestPoint left, TestPoint right) => left.Equals(right);

    public static bool operator !=(TestPoint left, TestPoint right) => !left.Equals(right);
}
