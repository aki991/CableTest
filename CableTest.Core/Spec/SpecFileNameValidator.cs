using System.Globalization;

namespace CableTest.Core.Spec;

/// <summary>
/// Provera imena spec fajla (<see cref="Model.Cable.SpecFileName"/>), bez ekstenzije.
/// </summary>
/// <remarks>
/// Uređaj i CableConnector rade sa kratkim imenima — u priručniku se vide "CRD-A", "BURN-1",
/// "1REVEN-D", a Learn generiše imena tipa "09071126". Ako je ime predugo ili sadrži znak koji
/// uređaj ne podržava, tester ga može tiho odseći i time raskinuti vezu između rezultata u CSV-u
/// i kabla u bazi. Zato se neispravno ime odbija odmah, pri unosu.
/// </remarks>
public static class SpecFileNameValidator
{
    /// <summary>
    /// Najveća dozvoljena dužina imena spec fajla, bez ekstenzije.
    /// </summary>
    /// <remarks>
    /// PRETPOSTAVKA: granica nije dokumentovana u priručniku i treba je potvrditi kod proizvođača
    /// ili ispitivanjem na uređaju. Ovde je namerno na jednom mestu da se lako promeni.
    /// </remarks>
    public const int MaxLength = 8;

    /// <summary>Ekstenziju dodaje generator; ime se u bazi čuva bez nje.</summary>
    public const string Extension = ".c61";

    private const string AllowedDescription =
        "Dozvoljena su velika slova A-Z (bez dijakritike), cifre 0-9 i crtica.";

    /// <summary>
    /// Proverava ime spec fajla. Vraća <c>null</c> ako je ime ispravno,
    /// odnosno poruku o grešci razumljivu operateru ako nije.
    /// </summary>
    public static string? Validate(string? specFileName)
    {
        if (string.IsNullOrEmpty(specFileName))
        {
            return $"Ime spec fajla nije uneto. {AllowedDescription} Najviše {Length(MaxLength)}.";
        }

        if (specFileName.Length > MaxLength)
        {
            return $"Ime spec fajla \"{specFileName}\" ima {Length(specFileName.Length)}, " +
                   $"a dozvoljeno je najviše {Length(MaxLength)}.";
        }

        foreach (char c in specFileName)
        {
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')
            {
                continue;
            }

            return DescribeInvalidCharacter(specFileName, c);
        }

        return null;
    }

    /// <summary>Da li je ime spec fajla ispravno.</summary>
    public static bool IsValid(string? specFileName) => Validate(specFileName) is null;

    /// <summary>Baca <see cref="ArgumentException"/> sa porukom o grešci ako ime nije ispravno.</summary>
    public static void EnsureValid(string? specFileName, string? paramName = null)
    {
        string? error = Validate(specFileName);
        if (error is not null)
        {
            throw new ArgumentException(error, paramName ?? nameof(specFileName));
        }
    }

    private static string DescribeInvalidCharacter(string specFileName, char c)
    {
        string prefix = $"Ime spec fajla \"{specFileName}\" ";

        if (c == '.')
        {
            return prefix + $"sadrži tačku. Ekstenziju {Extension} dodaje program, ime se piše bez nje.";
        }

        if (c == ' ')
        {
            return prefix + $"sadrži razmak. {AllowedDescription}";
        }

        if (char.IsWhiteSpace(c))
        {
            return prefix + $"sadrži prazan znak. {AllowedDescription}";
        }

        if (c is >= 'a' and <= 'z')
        {
            return prefix + $"sadrži malo slovo '{c}'. Dozvoljena su samo velika slova A-Z.";
        }

        if (char.IsLetter(c))
        {
            return prefix + $"sadrži slovo '{c}' koje nije osnovno latinično. {AllowedDescription}";
        }

        return prefix + $"sadrži znak '{c}' koji nije dozvoljen. {AllowedDescription}";
    }

    private static string Length(int count)
    {
        string number = count.ToString(CultureInfo.InvariantCulture);
        int last = count % 10;
        int lastTwo = count % 100;

        if (lastTwo is >= 11 and <= 14)
        {
            return $"{number} znakova";
        }

        return last switch
        {
            1 => $"{number} znak",
            2 or 3 or 4 => $"{number} znaka",
            _ => $"{number} znakova"
        };
    }
}
