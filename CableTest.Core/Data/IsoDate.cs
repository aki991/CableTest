using System.Globalization;

namespace CableTest.Core.Data;

/// <summary>
/// Pretvaranje datuma između baze i modela. Jedino mesto na kome se to radi.
/// </summary>
/// <remarks>
/// <para>
/// U bazi je datum ISO 8601 tekst u UTC: <c>2026-09-07T09:44:11.123Z</c>. UTC zato što se vreme
/// poredi i sortira, a lokalno vreme dvaput godišnje skoči — u noći prelaska na zimsko računanje
/// isti lokalni sat se ponovi, pa bi dva testa izgledala kao da su u obrnutom redosledu. Tekst
/// zato što je čitljiv u svakom pregledaču SQLite baze i što se sortira isto kao vreme.
/// </para>
/// <para>
/// U modelu je datum lokalno vreme, jer ga operater tako vidi i jer ga CSV tako i piše —
/// CableConnector zapisuje vreme mašine, bez oznake zone. Zato se datum bez zone
/// (<see cref="DateTimeKind.Unspecified"/>) tumači kao lokalni.
/// </para>
/// </remarks>
public static class IsoDate
{
    /// <summary>Oblik u kome se datum upisuje u bazu.</summary>
    public const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <summary>Zapis za nepoznat datum (<see cref="DateTime.MinValue"/>).</summary>
    /// <remarks>
    /// Nastaje kada CSV ima datum koji parser ne prepoznaje. Zapis se svejedno čuva, pa mora da
    /// ima svoj oblik u bazi — i mora da se vrati kao <see cref="DateTime.MinValue"/>, da se
    /// prirodni ključ zapisa ne promeni prolaskom kroz bazu.
    /// </remarks>
    public const string UnknownValue = "0001-01-01T00:00:00.000Z";

    private static readonly string[] AcceptedFormats =
    {
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ss.fffffffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-dd HH:mm:ss.fffZ",
        "yyyy-MM-dd HH:mm:ss"
    };

    /// <summary>Datum iz modela u zapis za bazu.</summary>
    public static string ToStorage(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return UnknownValue;
        }

        DateTime utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };

        return utc.ToString(Format, CultureInfo.InvariantCulture);
    }

    /// <summary>Trenutno vreme u zapisu za bazu.</summary>
    public static string Now() => DateTime.UtcNow.ToString(Format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Zapis iz baze u lokalno vreme. Neprepoznat zapis daje <see cref="DateTime.MinValue"/> —
    /// baza se ne sme srušiti zbog jednog neispravnog datuma.
    /// </summary>
    public static DateTime FromStorage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == UnknownValue)
        {
            return DateTime.MinValue;
        }

        if (DateTime.TryParseExact(text, AcceptedFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out utc))
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        }

        return DateTime.MinValue;
    }
}
