using CableTest.Core.Results;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Spisak zapisa merene vrednosti koje očekujemo u koloni provodne otpornosti.
/// </summary>
/// <remarks>
/// <para>
/// Ovaj test ne tvrdi da znamo format — tvrdi šta trenutno pokrivamo. Izraz
/// <see cref="ResultCsvParser.MeasurementRegex"/> je napravljen na osnovu jednog stvarnog CSV
/// fajla sa dva reda u kome je bio uključen samo Open/Short test, dakle bez ijedne izmerene
/// vrednosti. Sve dole je pretpostavka o tome kako bi CableConnector mogao da piše vrednost.
/// </para>
/// <para>
/// <b>Kad stigne stvarni fajl sa uključenim Cond testom:</b> dodaj zapis iz njega kao jedan red
/// <c>[InlineData("...")]</c> u <see cref="MereneVrednosti"/>. Ako test padne, znači da izraz
/// ne pokriva taj oblik i dopunjuje se na jednom mestu —
/// <see cref="ResultCsvParser.MeasurementRegex"/>. Tako odmah vidimo šta ne pokrivamo, umesto
/// da se izmerena vrednost tiho upiše u istoriju kao lažna greška.
/// </para>
/// </remarks>
public class MeasurementPatternTests
{
    /// <summary>Svaki red je jedan zapis merene vrednosti koji NE sme da postane poruka o grešci.</summary>
    [Theory]
    [InlineData("0.12")]
    [InlineData("0.120")]
    [InlineData(".12")]
    [InlineData("0")]
    [InlineData("51.2m")]
    [InlineData("0.0512R")]
    [InlineData("1.23E-02")]
    [InlineData("12 mOhm")]
    [InlineData("12mΩ")]         // grcko Omega, U+03A9
    [InlineData("12mΩ")]         // znak za om, U+2126
    [InlineData("--")]                // ishod "nije mereno"
    [InlineData("")]                  // prazna celija
    [InlineData(" ")]                 // sam razmak
    // --- dopuni odavde, jedan red po novom zapisu iz stvarnog fajla ---
    [InlineData("1.234")]
    [InlineData("+0.12")]
    [InlineData("-0.12")]
    [InlineData("12.")]
    [InlineData("0.12 Ohm")]
    [InlineData("0.12ohm")]
    [InlineData("51.2 m")]
    [InlineData("1.23e+03")]
    [InlineData("100%")]
    [InlineData("0.5 kΩ")]
    [InlineData("  0.12  ")]          // razmaci oko vrednosti
    [InlineData("-")]
    [InlineData("---")]
    public void MerenaVrednost_NijePorukaOGresci(string value)
    {
        Assert.False(
            ResultCsvParser.IsDefectMessage(value),
            $"Zapis \"{value}\" je protumačen kao poruka o grešci, a treba da bude merena vrednost. " +
            "Dopuni ResultCsvParser.MeasurementRegex.");
    }

    /// <summary>
    /// Druga strana iste granice: poruke koje MORAJU ostati greška. Da se, šireći izraz za
    /// merene vrednosti, ne izgubi stvarna greška.
    /// </summary>
    [Theory]
    [InlineData("SHORT O01-O02")]
    [InlineData("OPEN O01-O31")]
    [InlineData("HV LEAKAGE O05")]
    [InlineData("COND NG O01-O02")]
    [InlineData("0.12 OHM TOO HIGH")]
    [InlineData("O01-O02")]
    [InlineData("ERROR")]
    public void PorukaOGresci_NijeMerenaVrednost(string value)
    {
        Assert.True(
            ResultCsvParser.IsDefectMessage(value),
            $"Zapis \"{value}\" je protumačen kao merena vrednost, a treba da bude greška.");
    }

    /// <summary>
    /// Isti spisak kroz ceo parser: vrednost u koloni merenja ne sme da se pojavi kao greška
    /// u pročitanom zapisu.
    /// </summary>
    [Theory]
    [InlineData("0.12")]
    [InlineData(".12")]
    [InlineData("51.2m")]
    [InlineData("1.23E-02")]
    [InlineData("12 mOhm")]
    [InlineData("--")]
    [InlineData("")]
    public void MerenaVrednostUKoloniCondValue_NeDajeGresku(string value)
    {
        const string crLf = "\r\n";
        string csv =
            "Seq.,Filename,Pass,Date,Time,Operater,O/S TEST,COND TEST,COND VALUE,unit," + crLf +
            $"1,M100-W1,pass,2026/09/07,11:44:11,Miloš,PASS,PASS,{value},OHM," + crLf;

        CsvParseResult result = ResultCsvParser.ParseAll(csv);

        Assert.Single(result.Runs);
        Assert.Empty(result.Runs[0].Defects);
    }
}
