using System.Windows;
using CableTest.Core.Configuration;

namespace CableTest.App.Services;

/// <summary>
/// Menja temu u toku rada, zamenom rečnika boja u <see cref="Application.Resources"/>.
/// </summary>
/// <remarks>
/// <para>
/// Boje aplikacije stoje u dva rečnika — <c>Themes\Tamna.xaml</c> i <c>Themes\Svetla.xaml</c> —
/// sa <b>istim skupom ključeva</b>. Promena teme je zamena jednog drugim; sve ostalo (oblici,
/// razmaci, ponašanje kontrola) ostaje netaknuto, jer stoji u <c>App.xaml</c>.
/// </para>
/// <para>
/// Zato XAML na boje upućuje isključivo sa <c>DynamicResource</c>. <c>StaticResource</c> se
/// razrešava jednom, pri učitavanju, i posle zamene bi ostao na starim bojama — ekran bi bio
/// pola svetao, pola taman.
/// </para>
/// <para>
/// Traži se i briše <b>tačno</b> rečnik teme, po putanji: <c>Application.Resources</c> može da
/// dobije i druge spojene rečnike, a oni ne smeju da nestanu uz promenu teme.
/// </para>
/// </remarks>
public static class ThemeService
{
    private const string DarkFile = "Tamna.xaml";
    private const string LightFile = "Svetla.xaml";

    /// <summary>Postavlja temu na rečnike aplikacije koja se izvršava.</summary>
    public static void Apply(AppTheme theme) => Apply(Application.Current?.Resources, theme);

    /// <summary>Postavlja temu na zadati rečnik; <c>null</c> se prećutno propušta.</summary>
    /// <remarks>
    /// Odvojeno od <see cref="Apply(AppTheme)"/> da može da se ispita bez pokrenute aplikacije.
    /// </remarks>
    public static void Apply(ResourceDictionary? resources, AppTheme theme)
    {
        if (resources is null)
        {
            return;
        }

        // Već je ta tema? Ponovno učitavanje bi bez potrebe preračunalo ceo ekran.
        if (resources.MergedDictionaries.Any(d => FileOf(d) == FileName(theme)))
        {
            return;
        }

        var replacement = new ResourceDictionary { Source = Source(theme) };

        // Prvo se doda nova pa se skloni stara: obrnutim redom bi između dva koraka postojao
        // trenutak bez ijedne boje, u kome svaki DynamicResource ostaje nerazrešen.
        resources.MergedDictionaries.Add(replacement);

        foreach (ResourceDictionary old in resources.MergedDictionaries
                     .Where(d => !ReferenceEquals(d, replacement) && FileOf(d) is not null)
                     .ToList())
        {
            resources.MergedDictionaries.Remove(old);
        }
    }

    /// <summary>Ime fajla rečnika jedne teme.</summary>
    public static string FileName(AppTheme theme) => theme == AppTheme.Light ? LightFile : DarkFile;

    /// <summary>
    /// Puna „pack" putanja rečnika jedne teme.
    /// </summary>
    /// <remarks>
    /// Namerno puna, a ne relativna („Themes/Tamna.xaml"): relativna se razrešava prema
    /// aplikaciji koja se izvršava, pa van pokrenutog WPF-a — recimo u testu — uopšte ne radi.
    /// Rečnik se traži imenom sklopa, iz koga je i ugrađen.
    /// </remarks>
    public static Uri Source(AppTheme theme) => new(
        "pack://application:,,,/CableTest;component/Themes/" + FileName(theme),
        UriKind.Absolute);

    /// <summary>
    /// Ime fajla rečnika ako je rečnik jedna od tema, inače <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Gleda se samo poslednji deo putanje: <c>App.xaml</c> spaja temu relativno
    /// („Themes/Tamna.xaml"), a <see cref="Apply(ResourceDictionary?, AppTheme)"/> punom „pack"
    /// putanjom — isti rečnik, dva zapisa. Ostali spojeni rečnici se ne diraju.
    /// </remarks>
    private static string? FileOf(ResourceDictionary dictionary)
    {
        if (dictionary.Source is not { } source)
        {
            return null;
        }

        string last = source.OriginalString.Split('/').Last();

        return string.Equals(last, DarkFile, StringComparison.OrdinalIgnoreCase) ? DarkFile
            : string.Equals(last, LightFile, StringComparison.OrdinalIgnoreCase) ? LightFile
            : null;
    }
}
