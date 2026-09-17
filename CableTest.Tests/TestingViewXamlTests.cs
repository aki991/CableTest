using System.Reflection;
using System.Text.RegularExpressions;
using CableTest.App.ViewModels;
using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Provere samog XAML-a: glavnog ekrana, okvira aplikacije i ostalih ekrana.
/// </summary>
/// <remarks>
/// <para>
/// Kompajler ove greške ne hvata: pogrešno ime resursa puca tek pri učitavanju prozora, a
/// pogrešno ime u <c>{Binding}</c> ne puca uopšte — polje jednostavno ostane prazno. U pogonu bi
/// to značilo ekran koji izgleda ispravno, a ne prikazuje ništa.
/// </para>
/// <para>
/// Testovi ne otvaraju prozor: jedni čitaju XAML kao tekst, drugi ga učitavaju na STA niti bez
/// prikazivanja.
/// </para>
/// </remarks>
public class TestingViewXamlTests
{
    private static readonly string ViewsFolder = Path.Combine(AppContext.BaseDirectory, "Views");

    private static readonly string XamlPath = Path.Combine(ViewsFolder, "TestingView.xaml");

    private static readonly string ShellXamlPath = Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml");

    /// <summary>
    /// Svojstva do kojih se stiže preko <c>RelativeSource</c> ili <c>ElementName</c>, a ne preko
    /// ViewModel-a. Regularni izraz ne razlikuje takve veze, pa se navode ovde.
    /// </summary>
    private static readonly string[] IzvanViewModela =
    {
        "Foreground",     // boja ikone prati boju dugmeta ili stavke spiska
        "WindowState",    // ikona „uvećaj" prati stanje prozora
        "SelectedIndex"   // izabrani ekran prati izabranu stavku bočnog menija
    };

    // -------------------------------------------------------------------------------------
    // Veze sa podacima
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// Svako <c>{Binding Nesto}</c> na glavnom ekranu mora da pokazuje na postojeće svojstvo —
    /// bilo ViewModel-a, bilo tipa reda koji se prikazuje u spisku ili tabeli.
    /// </summary>
    [Fact]
    public void SvakaVeza_PokazujeNaPostojeceSvojstvoViewModela()
    {
        HashSet<string> svojstva = Svojstva(
            typeof(TestingViewModel),
            typeof(PortUsage),
            typeof(NetRow),
            typeof(Cable),
            typeof(CableWire),
            typeof(Vehicle));

        Assert.Empty(NepoznateVeze(File.ReadAllText(XamlPath), svojstva));
    }

    /// <summary>Veze u okviru aplikacije moraju da pokazuju na <see cref="ShellViewModel"/>.</summary>
    [Fact]
    public void SvakaVezaOkvira_PokazujeNaPostojeceSvojstvoOkvira()
    {
        Assert.Empty(NepoznateVeze(File.ReadAllText(ShellXamlPath), Svojstva(typeof(ShellViewModel))));
    }

    /// <summary>
    /// Dvodelne putanje okvira (<c>Testing.StatusText</c>) moraju da se razreše i u drugom delu.
    /// </summary>
    /// <remarks>
    /// Provera korena nije dovoljna: <c>{Binding Testing.NemaOvoga}</c> ima ispravan koren, a
    /// u pogonu bi ostavilo prazno polje u traci stanja.
    /// </remarks>
    [Fact]
    public void DvodelneVezeOkvira_ImajuIDrugiDeo()
    {
        string xaml = File.ReadAllText(ShellXamlPath);
        var nedostaju = new List<string>();

        foreach (Match match in Regex.Matches(xaml, @"\{Binding\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)"))
        {
            PropertyInfo? koren = typeof(ShellViewModel).GetProperty(match.Groups[1].Value);
            if (koren is null)
            {
                continue;   // koren hvata test iznad
            }

            if (koren.PropertyType.GetProperty(match.Groups[2].Value) is null)
            {
                nedostaju.Add(match.Value);
            }
        }

        Assert.Empty(nedostaju);
    }

    // -------------------------------------------------------------------------------------
    // Traženi elementi
    // -------------------------------------------------------------------------------------

    /// <summary>Traženi elementi glavnog ekrana moraju zaista biti u XAML-u.</summary>
    [Theory]
    [InlineData("1. Izbor kabla")]
    [InlineData("2. Test program")]
    [InlineData("3. Rezultat")]
    [InlineData("Vozilo")]
    [InlineData("Oznaka kabla")]
    [InlineData("Konektor / mapa pinova")]
    [InlineData("Pripremi test program")]
    [InlineData("Pronađene greške")]
    [InlineData("Net lista")]
    [InlineData("Merna karta")]
    public void GlavniEkran_SadrziTrazeneElemente(string tekst)
        => Assert.Contains(tekst, File.ReadAllText(XamlPath), StringComparison.Ordinal);

    /// <summary>Bočni meni i traka stanja moraju imati sve stavke.</summary>
    [Theory]
    [InlineData("Ispitivanje")]
    [InlineData("Vozila")]
    [InlineData("Kablovi")]
    [InlineData("Test programi")]
    [InlineData("Istorija")]
    [InlineData("Podešavanja")]
    [InlineData("CableConnector")]
    [InlineData("Simuliraj PROŠAO")]
    [InlineData("Simuliraj PAO")]
    [InlineData("Testova danas:")]
    public void Okvir_SadrziTrazeneElemente(string tekst)
        => Assert.Contains(tekst, File.ReadAllText(ShellXamlPath), StringComparison.Ordinal);

    // -------------------------------------------------------------------------------------
    // Učitavanje
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// XAML svih ekrana se učitava, a svi <c>StaticResource</c> ključevi postoje. Pogrešan ključ
    /// ovde puca — isto kao što bi puclo pri pokretanju aplikacije.
    /// </summary>
    [Fact]
    public void Xaml_SeUcitavaISviResursiPostoje()
    {
        Exception? greska = null;

        // WPF elementi se prave samo na STA niti; nijedan prozor se ne prikazuje.
        var nit = new Thread(() =>
        {
            try
            {
                var app = new global::CableTest.App.App();
                app.InitializeComponent();   // učitava App.xaml, tj. sve resurse

                _ = new global::CableTest.App.Views.TestingView();
                _ = new global::CableTest.App.Views.VozilaView();
                _ = new global::CableTest.App.Views.KabloviView();
                _ = new global::CableTest.App.Views.TestProgramiView();
                _ = new global::CableTest.App.Views.IstorijaView();
                _ = new global::CableTest.App.Views.PodesavanjaView();
                _ = new global::CableTest.App.Views.CableConnectorView();
            }
            catch (Exception ex)
            {
                greska = ex;
            }
        });

        nit.SetApartmentState(ApartmentState.STA);
        nit.Start();
        nit.Join(TimeSpan.FromSeconds(30));

        Assert.Null(greska);
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private static HashSet<string> Svojstva(params Type[] tipovi)
    {
        var imena = new HashSet<string>(IzvanViewModela, StringComparer.Ordinal);

        foreach (Type tip in tipovi)
        {
            foreach (PropertyInfo property in tip.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                imena.Add(property.Name);
            }
        }

        return imena;
    }

    private static IReadOnlyList<string> NepoznateVeze(string xaml, HashSet<string> svojstva)
    {
        var nedostaju = new List<string>();

        foreach (Match match in Regex.Matches(xaml, @"\{Binding\s+([A-Za-z_][A-Za-z0-9_.]*)"))
        {
            // Putanja može imati tačku ("ResultDefects.Count"); proverava se prvi deo.
            string koren = match.Groups[1].Value.Split('.')[0];

            if (koren is "RelativeSource" or "Path" or "ElementName")
            {
                continue;
            }

            if (!svojstva.Contains(koren) && !nedostaju.Contains(koren))
            {
                nedostaju.Add(koren);
            }
        }

        return nedostaju;
    }
}
