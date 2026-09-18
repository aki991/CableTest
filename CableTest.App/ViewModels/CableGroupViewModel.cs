using System.Globalization;
using System.Windows.Data;
using CableTest.App.Mvvm;

namespace CableTest.App.ViewModels;

/// <summary>
/// Jedna grupa kablova u spisku za izbor — zaglavlje koje se otvara i zatvara.
/// </summary>
/// <remarks>
/// <para>
/// Grupa je deo oznake pre prve crtice (<see cref="Core.Model.Cable.Group"/>); kod vozila
/// „Miloš Veliki" svi kablovi su grupa „40". Operater prvo bira grupu, pa tek onda kabl iz nje.
/// </para>
/// <para>
/// Ovaj objekat je <b>ključ grupisanja</b> u <see cref="System.ComponentModel.ICollectionView"/>,
/// a ne samo podatak za prikaz. Zato je i jedan isti primerak po grupi: pogled se preračunava
/// pri svakom pritisku tastera u pretrazi, a stanje „otvoreno/zatvoreno" mora to da preživi.
/// Da je ključ obična niska, zaglavlja bi se pravila iznova i grupe bi se zatvarale usred
/// kucanja.
/// </para>
/// </remarks>
public sealed class CableGroupViewModel : ObservableObject
{
    private bool _isExpanded;
    private int _count;

    public CableGroupViewModel(string name) => Name = name ?? string.Empty;

    /// <summary>Oznaka grupe, npr. „40"; prazno za kablove bez grupe.</summary>
    public string Name { get; }

    /// <summary>Naslov zaglavlja, npr. „Grupa 40".</summary>
    public string Title => string.IsNullOrWhiteSpace(Name) ? "Bez grupe" : "Grupa " + Name;

    /// <summary>Da li su kablovi grupe prikazani.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    /// <summary>Koliko kablova grupa ima.</summary>
    public int Count
    {
        get => _count;
        set
        {
            if (Set(ref _count, value))
            {
                Raise(nameof(CountText));
            }
        }
    }

    /// <summary>Broj kablova ispisan rečima, npr. „12 kablova".</summary>
    public string CountText => _count + " " + Imenica(_count);

    public override string ToString() => Title;

    /// <summary>
    /// Oblik imenice „kabl" uz broj.
    /// </summary>
    /// <remarks>
    /// Srpski ima tri oblika, a pravilo se vrti po poslednje dve cifre: 1 → „kabl", 2–4 → „kabla",
    /// ostalo → „kablova". Brojevi 11–14 idu na „kablova", iako se završavaju na 1–4.
    /// </remarks>
    private static string Imenica(int broj)
    {
        int poslednjeDve = Math.Abs(broj) % 100;
        int poslednja = Math.Abs(broj) % 10;

        if (poslednjeDve is >= 11 and <= 14)
        {
            return "kablova";
        }

        return poslednja switch
        {
            1 => "kabl",
            2 or 3 or 4 => "kabla",
            _ => "kablova"
        };
    }
}

/// <summary>
/// Pretvara oznaku grupe iz kabla u <see cref="CableGroupViewModel"/> koji je ključ grupisanja.
/// </summary>
/// <remarks>
/// Postoji da model ostane čist: <see cref="Core.Model.Cable"/> zna samo svoju oznaku grupe kao
/// nisku, a stanje prikaza („otvoreno/zatvoreno") živi u sloju ekrana. Grupisanje preko ovog
/// pretvarača spaja to dvoje bez dodavanja polja za prikaz u model.
/// </remarks>
public sealed class CableGroupKeyConverter : IValueConverter
{
    private readonly Func<string, CableGroupViewModel> _lookup;

    public CableGroupKeyConverter(Func<string, CableGroupViewModel> lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        _lookup = lookup;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => _lookup(value as string ?? string.Empty);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Grupa se ne upisuje nazad u kabl.");
}
