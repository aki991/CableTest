using CableTest.Core.Model;

namespace CableTest.Core.Results;

/// <summary>Rezultat jednog čitanja CSV-a: pročitani testovi i upozorenja koja nisu prekinula obradu.</summary>
public sealed class CsvParseResult
{
    public CsvParseResult(IReadOnlyList<TestRun> runs, IReadOnlyList<string> warnings)
    {
        Runs = runs;
        Warnings = warnings;
    }

    /// <summary>Testovi pročitani u ovom prolazu, redom kojim su u fajlu.</summary>
    public IReadOnlyList<TestRun> Runs { get; }

    /// <summary>
    /// Redovi koje parser nije razumeo. Obrada se zbog njih ne prekida — upozorenje se prikaže
    /// i zapiše, a ostali redovi se obrade normalno.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    public bool HasWarnings => Warnings.Count > 0;

    public static CsvParseResult Empty { get; } = new(Array.Empty<TestRun>(), Array.Empty<string>());
}
