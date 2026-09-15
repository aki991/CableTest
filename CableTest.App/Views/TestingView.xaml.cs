using System.Windows;
using System.Windows.Controls;

namespace CableTest.App.Views;

/// <summary>Prikaz glavnog ekrana. Sva logika je u <see cref="ViewModels.TestingViewModel"/>.</summary>
public partial class TestingView : UserControl
{
    public TestingView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Priprema polje za očitavanje barkoda.
    /// </summary>
    /// <remarks>
    /// Skener barkoda se prema računaru ponaša kao tastatura — očitanu oznaku „otkuca" u polje
    /// koje je u fokusu. Dugme zato ne čita ništa samo, nego postavlja fokus i obeležava zatečeni
    /// tekst, da ga sledeće očitavanje zameni.
    /// </remarks>
    private void OnBarkod(object sender, RoutedEventArgs e)
    {
        PoljePretrage.Focus();
        PoljePretrage.SelectAll();
    }
}
