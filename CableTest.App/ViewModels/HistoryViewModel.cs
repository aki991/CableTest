using System.Collections.ObjectModel;
using System.Globalization;
using CableTest.App.Mvvm;
using CableTest.Core.Data;
using CableTest.Core.Model;

namespace CableTest.App.ViewModels;

/// <summary>Jedan izvršen test u spisku istorije.</summary>
/// <param name="TimeText">Vreme testa, spremno za prikaz.</param>
/// <param name="Seq">Redni broj iz kolone „Seq." u CSV-u.</param>
/// <param name="SpecFileName">Ime spec fajla iz CSV-a.</param>
/// <param name="CableCode">Oznaka kabla, ili „neprepoznat".</param>
/// <param name="Operator">Ime operatera.</param>
/// <param name="DefectCount">Broj prijavljenih grešaka.</param>
/// <param name="Passed">Da li je test prošao.</param>
/// <param name="RawRow">Sirov red iz CSV-a, za oblačić.</param>
public sealed record RunRow(
    string TimeText,
    int Seq,
    string SpecFileName,
    string CableCode,
    string Operator,
    int DefectCount,
    bool Passed,
    string RawRow)
{
    public string OutcomeText => Passed ? NetRow.Passed : NetRow.Failed;
}

/// <summary>
/// Istorija izvršenih testova.
/// </summary>
/// <remarks>
/// Ovde ulaze i zatečeni rezultati koji se ne prikazuju na glavnom ekranu: istorija je dokaz da
/// je kabl ispitan i mora da bude potpuna, za razliku od velikog prikaza ishoda, koji sme da
/// pokaže isključivo test izvršen pred operaterom.
/// </remarks>
public sealed class HistoryViewModel : ObservableObject
{
    /// <summary>Koliko se poslednjih zapisa prikazuje u spisku.</summary>
    public const int Count = 200;

    /// <summary>Koliko se poslednjih zapisa pregleda pri brojanju današnjih testova.</summary>
    private const int TodayLookback = 1000;

    private readonly ITestRunRepository _runs;
    private readonly ICableRepository _cables;

    private int _todayTotal;
    private int _todayPassed;
    private int _todayFailed;

    public HistoryViewModel(ITestRunRepository runs, ICableRepository cables)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(cables);

        _runs = runs;
        _cables = cables;
    }

    /// <summary>Poslednjih <see cref="Count"/> testova, najnoviji prvi.</summary>
    public ObservableCollection<RunRow> Rows { get; } = new();

    /// <summary>Koliko je testova danas u istoriji.</summary>
    public int TodayTotal
    {
        get => _todayTotal;
        private set => Set(ref _todayTotal, value);
    }

    /// <summary>Koliko je današnjih testova prošlo.</summary>
    public int TodayPassed
    {
        get => _todayPassed;
        private set => Set(ref _todayPassed, value);
    }

    /// <summary>Koliko je današnjih testova palo.</summary>
    public int TodayFailed
    {
        get => _todayFailed;
        private set => Set(ref _todayFailed, value);
    }

    /// <summary>Da li je spisak prazan — od toga zavisi poruka umesto tabele.</summary>
    public bool IsEmpty => Rows.Count == 0;

    /// <summary>
    /// Čita istoriju iz baze. Brojači se računaju iz baze, a ne iz pamćenja ekrana, da prežive
    /// ponovno pokretanje aplikacije usred smene.
    /// </summary>
    public void Load()
    {
        IReadOnlyList<Cable> cables = _cables.GetAll();

        Rows.Clear();

        foreach (TestRun run in _runs.GetRecent(Count))
        {
            string code = run.CableId is null
                ? "neprepoznat"
                : cables.FirstOrDefault(c => c.Id == run.CableId.Value)?.Code ?? "neprepoznat";

            Rows.Add(new RunRow(
                run.TestedAt == DateTime.MinValue
                    ? NetRow.Unknown
                    : run.TestedAt.ToString("dd.MM.yyyy. HH:mm:ss", CultureInfo.InvariantCulture),
                run.Seq,
                run.SpecFileName,
                code,
                string.IsNullOrWhiteSpace(run.Operator) ? NetRow.Unknown : run.Operator,
                run.Defects.Count,
                run.Passed,
                run.RawRow));
        }

        CountToday();
        Raise(nameof(IsEmpty));
    }

    private void CountToday()
    {
        DateTime today = DateTime.Today;
        int total = 0;
        int passed = 0;

        foreach (TestRun run in _runs.GetRecent(TodayLookback))
        {
            if (run.TestedAt.Date != today)
            {
                continue;
            }

            total++;

            if (run.Passed)
            {
                passed++;
            }
        }

        TodayTotal = total;
        TodayPassed = passed;
        TodayFailed = total - passed;
    }
}
