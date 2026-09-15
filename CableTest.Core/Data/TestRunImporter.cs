using CableTest.Core.Model;

namespace CableTest.Core.Data;

/// <summary>Ishod pokušaja upisa jednog rezultata u istoriju.</summary>
/// <param name="Run">Zapis, sa popunjenim <see cref="TestRun.CableId"/> ako je kabl prepoznat.</param>
/// <param name="Cable">Kabl kome rezultat pripada, ili <c>null</c>.</param>
/// <param name="Stored"><c>true</c> ako je zapis upisan, <c>false</c> ako je preskočen kao duplikat.</param>
/// <param name="Warnings">Upozorenja nastala pri upisu.</param>
public sealed record TestRunImportResult(
    TestRun Run,
    Cable? Cable,
    bool Stored,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Da li je rezultat vezan za kabl iz baze.</summary>
    public bool IsRecognized => Cable is not null;
}

/// <summary>Upis rezultata koji je stigao sa testera u istoriju.</summary>
public interface ITestRunImporter
{
    /// <summary>
    /// Vezuje rezultat za kabl po imenu spec fajla i upisuje ga. Zapis koji već postoji po
    /// prirodnom ključu se preskače, uz upozorenje.
    /// </summary>
    TestRunImportResult Import(TestRun run);
}

/// <summary>
/// Spaja pročitan rezultat sa kablom iz baze i upisuje ga u istoriju.
/// </summary>
/// <remarks>
/// Postoji da se ovaj korak ne bi našao u ViewModel-u: veza rezultata sa kablom ide isključivo
/// preko kolone „Filename" iz CSV-a i mora da radi isto bez obzira na to ko upisuje — glavni
/// ekran, kasniji uvoz starih fajlova ili serijska veza iz sledeće faze.
/// </remarks>
public sealed class TestRunImporter : ITestRunImporter
{
    private readonly ICableRepository _cables;
    private readonly ITestRunRepository _runs;

    public TestRunImporter(ICableRepository cables, ITestRunRepository runs)
    {
        ArgumentNullException.ThrowIfNull(cables);
        ArgumentNullException.ThrowIfNull(runs);
        _cables = cables;
        _runs = runs;
    }

    /// <inheritdoc />
    public TestRunImportResult Import(TestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var warnings = new List<string>();

        Cable? cable = _cables.GetBySpecFileName(run.SpecFileName);
        run.CableId = cable?.Id;

        if (cable is null)
        {
            // Rezultat se ipak čuva: operater je možda pokrenuo test iz spec fajla koji nije u
            // bazi, i upravo to treba da se vidi u istoriji, a ne da se tiho izgubi.
            warnings.Add(
                $"Rezultat iz spec fajla \"{run.SpecFileName}\" ne pripada nijednom kablu iz baze. " +
                "Sačuvan je kao neprepoznat.");
        }

        bool stored = _runs.Add(run, warnings);
        return new TestRunImportResult(run, cable, stored, warnings);
    }
}
