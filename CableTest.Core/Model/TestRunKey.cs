using System.Globalization;

namespace CableTest.Core.Model;

/// <summary>
/// Prirodni ključ jednog izvršenog testa: ime spec fajla, redni broj i vreme testa.
/// </summary>
/// <remarks>
/// <para>
/// Zapis nema svoj identifikator u CSV-u, pa je ovo jedino po čemu se dva reda razlikuju.
/// Ključ postoji zbog jednog konkretnog rizika: parser, kada fajl postane kraći nego ranije,
/// zaključuje da je zamenjen novim i čita ga od početka. To je ispravno kod dnevne rotacije,
/// ali ako CableConnector iz bilo kog razloga prepiše fajl istim imenom, svi redovi se čitaju
/// ponovo. Bez ovog ključa u bazi bi nastali duplikati, a istorija testova mora biti tačna —
/// ona je smisao celog projekta.
/// </para>
/// <para>
/// Isti ključ se sprovodi na dva mesta: <see cref="Results.ResultCsvParser.RemoveDuplicates"/>
/// ga primenjuje na listu pročitanih zapisa, a u bazi nad njim stoji jedinstveni indeks
/// <c>(SpecFileName, Seq, TestedAt)</c> u tabeli <c>TestRun</c>.
/// </para>
/// <para>
/// <b>Poznato ograničenje — prelazak na zimsko računanje vremena.</b> CableConnector u CSV
/// zapisuje lokalno vreme mašine, bez oznake zone, a u bazi se datum čuva u UTC. U noći prelaska
/// na zimsko vreme lokalni sat se ponovi (npr. 02:30 se javlja dvaput), pa se oba puta tumači kao
/// isto lokalno vreme. Ako bi u tom ponovljenom satu dva različita testa imala isti
/// <c>SpecFileName</c> i isti <c>Seq</c>, dobili bi isti ključ i drugi bi bio tiho preskočen.
/// </para>
/// <para>
/// Ponašanje se namerno ne menja: verovatnoća je zanemarljiva (jedan sat godišnje, uz to noću,
/// uz isti redni broj testa), a svako rešenje — čuvanje pomaka zone ili sirovog lokalnog zapisa —
/// zakomplikovalo bi ključ koji mora da ostane isti na sva tri mesta gde se sprovodi. Ovaj
/// komentar postoji da se za godinu dana ne traži uzrok.
/// </para>
/// <para>
/// Ime spec fajla se poredi bez obzira na velika/mala slova (tester ga piše velikim slovima,
/// ali se u CSV-u pojavljivalo i drugačije), pa se u ključu čuva u velikim slovima.
/// </para>
/// </remarks>
public readonly record struct TestRunKey(string SpecFileName, int Seq, DateTime TestedAt)
{
    /// <summary>Ključ jednog zapisa.</summary>
    public static TestRunKey Of(TestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return new TestRunKey(Normalize(run.SpecFileName), run.Seq, run.TestedAt);
    }

    /// <summary>Ključ od pojedinačnih vrednosti, npr. pri upitu nad bazom.</summary>
    public static TestRunKey Of(string? specFileName, int seq, DateTime testedAt)
        => new(Normalize(specFileName), seq, testedAt);

    /// <summary>
    /// Ime spec fajla u obliku u kome se poredi i upisuje u bazu: bez ekstenzije i razmaka,
    /// velikim slovima.
    /// </summary>
    public static string Normalize(string? specFileName)
        => Cable.NormalizeResultFileName(specFileName).ToUpperInvariant();

    /// <summary>Opis ključa za upozorenja i dnevnik.</summary>
    public override string ToString()
        => $"{SpecFileName}/{Seq.ToString(CultureInfo.InvariantCulture)}/" +
           TestedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
