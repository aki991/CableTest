using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Provera polja za unos: da kucani tekst kreće tačno tamo gde stoji i uputstvo.
/// </summary>
/// <remarks>
/// <para>
/// Polje za pretragu kablova ima levo uvlačenje 37 tačaka, da tekst ne ulazi pod ikonu lupe.
/// <see cref="TextBox"/> svoj <c>Padding</c> <b>sam</b> nanosi na tekst, unutar
/// <c>PART_ContentHost</c>-a. Kad bi ga šablon dodao još jednom, kao <c>Margin</c>, uvlačenje bi
/// se nanelo dvaput: uputstvo bi stajalo na 37, a kucanje počinjalo na 75 — pored uputstva
/// umesto preko njega.
/// </para>
/// <para>
/// To se ne vidi ni pri prevođenju ni pri učitavanju prozora, nego tek kad neko kuca u polje.
/// Zato se ovde meri.
/// </para>
/// </remarks>
public class PoljePretrageTests
{
    /// <summary>Levo uvlačenje polja za pretragu; toliko zauzima ikona lupe.</summary>
    private const double Uvlacenje = 37;

    [Fact]
    public void Kucanje_PocinjeNaIstomMestuGdeIUputstvo()
    {
        (double kursorX, double uputstvoX) = Izmeri();

        // Dozvoljena je tačka-dve razlike: kursor je tanka crta i crta se na ivici slova.
        Assert.True(
            Math.Abs(kursorX - uputstvoX) <= 2,
            $"Kucanje počinje na {kursorX}, a uputstvo stoji na {uputstvoX} — uvlačenje se nanosi dvaput.");
    }

    [Fact]
    public void Uputstvo_StojiNaZadatomUvlacenju()
    {
        (_, double uputstvoX) = Izmeri();

        // Uz uvlačenje ide i 1 tačka ivice polja.
        Assert.True(
            Math.Abs(uputstvoX - (Uvlacenje + 1)) <= 2,
            $"Uputstvo stoji na {uputstvoX}, a očekuje se oko {Uvlacenje + 1}.");
    }

    /// <summary>Gde počinje kucani tekst, a gde uputstvo — mereno na stvarnom šablonu polja.</summary>
    private static (double KursorX, double UputstvoX) Izmeri()
    {
        (double kursorX, double uputstvoX) = WpfHost.Izvrsi(() =>
        {
            var polje = new TextBox
            {
                Style = (Style)WpfHost.Resursi[typeof(TextBox)],
                Tag = "Pretraga po oznaci ili nazivu…",
                Padding = new Thickness(Uvlacenje, 9, 11, 9),
                Text = string.Empty
            };

            var koren = new Border { Child = polje, Width = 480, Height = 60 };
            koren.Measure(new Size(480, 60));
            koren.Arrange(new Rect(0, 0, 480, 60));
            koren.UpdateLayout();

            // U šablonu je samo jedan TextBlock — uputstvo za prazno polje.
            TextBlock? uputstvo = Nadji<TextBlock>(polje);

            return (
                polje.GetRectFromCharacterIndex(0).X,
                uputstvo is null
                    ? double.NaN
                    : uputstvo.TransformToAncestor(polje).Transform(default).X);
        });

        Assert.False(double.IsNaN(kursorX), "Položaj kursora nije izmeren.");
        Assert.False(double.IsNaN(uputstvoX), "Uputstvo nije nađeno u šablonu polja.");

        return (kursorX, uputstvoX);
    }

    private static T? Nadji<T>(DependencyObject koren) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(koren); i++)
        {
            DependencyObject dete = VisualTreeHelper.GetChild(koren, i);

            if (dete is T pogodak)
            {
                return pogodak;
            }

            if (Nadji<T>(dete) is { } dublje)
            {
                return dublje;
            }
        }

        return null;
    }
}
