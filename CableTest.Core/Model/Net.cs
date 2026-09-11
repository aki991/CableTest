using System.Collections;
using System.Globalization;
using System.Text;

namespace CableTest.Core.Model;

/// <summary>
/// Net (čvor) — skup ispitnih tačaka koje kabl međusobno spaja.
/// Zapisuje se kao tačke razdvojene crticom: "O01-O02-O31-O32".
/// Tačke ne moraju biti sa istog porta.
/// </summary>
/// <remarks>
/// Kod četverožičnog merenja jedan pin konektora je ožičen na dve ispitne tačke
/// (npr. O01 i O31), pa se u netu pojavljuju obe — za tester je to i dalje jedan čvor.
/// </remarks>
public sealed class Net : IReadOnlyList<TestPoint>, IEquatable<Net>
{
    public const char Separator = '-';
    public const int MinimumPoints = 2;

    private readonly TestPoint[] _points;

    public Net(IEnumerable<TestPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        _points = points.ToArray();

        if (_points.Length < MinimumPoints)
        {
            throw new ArgumentException(
                $"Net mora imati najmanje {MinimumPoints.ToString(CultureInfo.InvariantCulture)} tačke.",
                nameof(points));
        }

        var seen = new HashSet<int>();
        foreach (TestPoint p in _points)
        {
            if (!seen.Add(p.Index))
            {
                throw new ArgumentException($"Tačka {p} se u istom netu pojavljuje dva puta.", nameof(points));
            }
        }
    }

    public TestPoint this[int index] => _points[index];

    public int Count => _points.Length;

    public static Net Parse(string? text)
    {
        if (!TryParse(text, out Net? net, out string? error))
        {
            throw new FormatException(error);
        }

        return net!;
    }

    public static bool TryParse(string? text, out Net? net)
        => TryParse(text, out net, out _);

    /// <summary>
    /// Parsira net iz zapisa "O01-O02-O31-O32". Sve greške vraća kao poruku razumljivu operateru.
    /// </summary>
    public static bool TryParse(string? text, out Net? net, out string? error)
    {
        net = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Net je prazan. Očekivan oblik je npr. \"O01-O02-O31-O32\".";
            return false;
        }

        string[] parts = text.Trim().Split(Separator, StringSplitOptions.TrimEntries);
        if (parts.Length < MinimumPoints)
        {
            error = $"Net \"{text.Trim()}\" ima samo {parts.Length.ToString(CultureInfo.InvariantCulture)} tačku. " +
                    $"Potrebne su najmanje {MinimumPoints.ToString(CultureInfo.InvariantCulture)}, razdvojene crticom.";
            return false;
        }

        var points = new List<TestPoint>(parts.Length);
        var seen = new HashSet<int>();
        foreach (string part in parts)
        {
            if (!TestPoint.TryParse(part, out TestPoint point, out string? pointError))
            {
                error = pointError;
                return false;
            }

            if (!seen.Add(point.Index))
            {
                error = $"Tačka {point} se u netu \"{text.Trim()}\" pojavljuje dva puta.";
                return false;
            }

            points.Add(point);
        }

        net = new Net(points);
        error = null;
        return true;
    }

    /// <summary>Zapis neta onako kako ide u .c61 fajl: "O01-O02-O31-O32".</summary>
    public override string ToString()
    {
        var sb = new StringBuilder(Count * 4);
        for (int i = 0; i < _points.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(Separator);
            }

            sb.Append(_points[i].ToString());
        }

        return sb.ToString();
    }

    public bool Equals(Net? other)
        => other is not null && _points.AsSpan().SequenceEqual(other._points);

    public override bool Equals(object? obj) => Equals(obj as Net);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (TestPoint p in _points)
        {
            hash.Add(p.Index);
        }

        return hash.ToHashCode();
    }

    public IEnumerator<TestPoint> GetEnumerator() => ((IEnumerable<TestPoint>)_points).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _points.GetEnumerator();
}
