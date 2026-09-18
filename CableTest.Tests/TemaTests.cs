using System.Text.RegularExpressions;
using System.Windows;
using CableTest.App.Services;
using CableTest.Core.Configuration;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Provere tema: da su uparene i da nijedna boja ne visi u prazno.
/// </summary>
/// <remarks>
/// <para>
/// Ove greške ne hvata ni kompajler ni učitavanje prozora. Boje se u XAML-u traže preko
/// <c>DynamicResource</c>, a takva veza koja se ne razreši <b>ne puca</b> — element jednostavno
/// ostane bez boje. U pogonu bi to bio nevidljiv tekst ili beli pravougaonik usred ekrana, i to
/// tek u jednoj od dve teme, pa bi lako prošlo neopaženo do isporuke.
/// </para>
/// <para>
/// Odatle i pravilo koje se ovde proverava: svaki ključ iz <c>Themes\Tamna.xaml</c> mora da
/// postoji i u <c>Themes\Svetla.xaml</c>, i obrnuto.
/// </para>
/// </remarks>
public class TemaTests
{
    private static readonly string XamlFolder = Path.Combine(AppContext.BaseDirectory, "Xaml");

    // -------------------------------------------------------------------------------------
    // Uparenost
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// Obe teme nose isti skup ključeva. Ključ koji postoji samo u jednoj znači da posle promene
    /// teme neki deo ekrana ostaje bez boje.
    /// </summary>
    [Fact]
    public void Teme_ImajuIsteKljuceve()
    {
        (ResourceDictionary tamna, ResourceDictionary svetla) = UcitajTeme();

        string[] samoTamna = Kljucevi(tamna).Except(Kljucevi(svetla)).OrderBy(k => k).ToArray();
        string[] samoSvetla = Kljucevi(svetla).Except(Kljucevi(tamna)).OrderBy(k => k).ToArray();

        Assert.Empty(samoTamna);
        Assert.Empty(samoSvetla);
    }

    /// <summary>Tema nije prazna — da provera uparenosti ne bi prošla nad dva prazna rečnika.</summary>
    [Fact]
    public void Teme_NisuPrazne()
    {
        (ResourceDictionary tamna, _) = UcitajTeme();
        Assert.True(tamna.Count > 30, $"Tamna tema ima samo {tamna.Count} ključeva.");
    }

    // -------------------------------------------------------------------------------------
    // Upotreba u XAML-u
    // -------------------------------------------------------------------------------------

    /// <summary>Svaki <c>DynamicResource</c> iz XAML-a postoji u obe teme.</summary>
    [Fact]
    public void SvakiDynamicResource_PostojiUObeTeme()
    {
        (ResourceDictionary tamna, ResourceDictionary svetla) = UcitajTeme();

        var nedostaju = new List<string>();

        foreach ((string fajl, string xaml) in SavXaml())
        {
            foreach (string kljuc in Kljucevi(xaml, "DynamicResource"))
            {
                if (!tamna.Contains(kljuc) || !svetla.Contains(kljuc))
                {
                    nedostaju.Add($"{fajl}: {kljuc}");
                }
            }
        }

        Assert.Empty(nedostaju);
    }

    /// <summary>
    /// Nijedna boja teme se ne traži preko <c>StaticResource</c>.
    /// </summary>
    /// <remarks>
    /// <c>StaticResource</c> se razrešava jednom, pri učitavanju. Posle promene teme ostao bi na
    /// staroj boji, pa bi ekran bio pola svetao, pola taman — i to samo na mestima koja je neko
    /// slučajno napisao tako.
    /// </remarks>
    [Fact]
    public void Boje_SeNeTrazePrekoStaticResource()
    {
        (ResourceDictionary tamna, _) = UcitajTeme();

        var pogresne = new List<string>();

        foreach ((string fajl, string xaml) in SavXaml())
        {
            foreach (string kljuc in Kljucevi(xaml, "StaticResource"))
            {
                if (tamna.Contains(kljuc))
                {
                    pogresne.Add($"{fajl}: {kljuc}");
                }
            }
        }

        Assert.Empty(pogresne);
    }

    // -------------------------------------------------------------------------------------
    // Promena teme
    // -------------------------------------------------------------------------------------

    /// <summary>Promena teme zamenjuje rečnik boja, a ne dodaje drugi pored njega.</summary>
    [Fact]
    public void PromenaTeme_ZamenjujeRecnikBoja()
    {
        NaStaNiti(() =>
        {
            var resources = new ResourceDictionary();

            ThemeService.Apply(resources, AppTheme.Dark);
            Assert.Single(resources.MergedDictionaries);

            ThemeService.Apply(resources, AppTheme.Light);
            Assert.Single(resources.MergedDictionaries);
            Assert.EndsWith("Svetla.xaml", resources.MergedDictionaries[0].Source!.OriginalString,
                StringComparison.Ordinal);

            ThemeService.Apply(resources, AppTheme.Dark);
            Assert.Single(resources.MergedDictionaries);
            Assert.EndsWith("Tamna.xaml", resources.MergedDictionaries[0].Source!.OriginalString,
                StringComparison.Ordinal);
        });
    }

    /// <summary>Rečnici koji nisu tema ostaju netaknuti kad se tema promeni.</summary>
    [Fact]
    public void PromenaTeme_NeDiraOstaleRecnike()
    {
        NaStaNiti(() =>
        {
            var tudji = new ResourceDictionary();
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(tudji);

            ThemeService.Apply(resources, AppTheme.Dark);
            ThemeService.Apply(resources, AppTheme.Light);

            Assert.Contains(resources.MergedDictionaries, d => ReferenceEquals(d, tudji));
            Assert.Equal(2, resources.MergedDictionaries.Count);
        });
    }

    /// <summary>Ista tema dvaput ne menja ništa — nema razloga da se ekran preračunava.</summary>
    [Fact]
    public void PromenaTeme_NaIstuTemu_NeRadiNista()
    {
        NaStaNiti(() =>
        {
            var resources = new ResourceDictionary();

            ThemeService.Apply(resources, AppTheme.Dark);
            ResourceDictionary prvi = resources.MergedDictionaries[0];

            ThemeService.Apply(resources, AppTheme.Dark);

            Assert.Single(resources.MergedDictionaries);
            Assert.Same(prvi, resources.MergedDictionaries[0]);
        });
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private static IEnumerable<(string Fajl, string Xaml)> SavXaml()
        => Directory.EnumerateFiles(XamlFolder, "*.xaml")
            .Select(p => (Path.GetFileName(p), File.ReadAllText(p)));

    private static IEnumerable<string> Kljucevi(string xaml, string vrsta)
        => Regex.Matches(xaml, @"\{" + vrsta + @"\s+([A-Za-z0-9_]+)\}")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal);

    private static IEnumerable<string> Kljucevi(ResourceDictionary dictionary)
        => dictionary.Keys.Cast<object>().Select(k => k.ToString()!);

    private static (ResourceDictionary Tamna, ResourceDictionary Svetla) UcitajTeme()
    {
        ResourceDictionary? tamna = null;
        ResourceDictionary? svetla = null;

        NaStaNiti(() =>
        {
            tamna = Ucitaj(AppTheme.Dark);
            svetla = Ucitaj(AppTheme.Light);
        });

        Assert.NotNull(tamna);
        Assert.NotNull(svetla);
        return (tamna!, svetla!);
    }

    // Rečnici su ugrađeni u CableTest.exe, a putanja je ista ona kojom ih menja i sama
    // aplikacija — proverava se ono što se zaista isporučuje.
    private static ResourceDictionary Ucitaj(AppTheme theme)
        => new() { Source = ThemeService.Source(theme) };

    /// <summary>
    /// Pokreće radnju na WPF niti. Rečnici se prave samo tamo, i to na istoj niti na kojoj se i
    /// čitaju; nijedan prozor se ne otvara.
    /// </summary>
    private static void NaStaNiti(Action radnja)
    {
        WpfHost.Izvrsi(radnja);
    }
}
