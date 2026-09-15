using System.Globalization;
using CableTest.Core.Model;
using CableTest.Core.Results;

namespace CableTest.Core.Gateway;

/// <summary>
/// Gateway bez testera i bez fajlova: rezultat se „primi" tako što ga neko programski pošalje.
/// </summary>
/// <remarks>
/// <para>
/// Postoji zato što se bez njega GUI ne može ni razviti ni testirati dok se ne sedne za sto pored
/// testera. Ponaša se isto kao <see cref="FileBasedTesterGateway"/>: rezultat se prijavljuje samo
/// dok je nadgledanje aktivno, na niti koja je pozvala <see cref="StartMonitoring"/>, i isti
/// rezultat se ne prijavljuje dvaput (vidi <see cref="TestRunKey"/>).
/// </para>
/// <para>
/// <see cref="PrepareTestAsync"/> ne dira disk — samo pamti kabl u <see cref="PreparedCables"/>,
/// pa test može da proveri da je priprema pozvana.
/// </para>
/// </remarks>
public sealed class FakeTesterGateway : ITesterGateway
{
    private readonly object _sync = new();
    private readonly List<Cable> _prepared = new();
    private readonly HashSet<TestRunKey> _seen = new();

    private SynchronizationContext? _uiContext;
    private bool _monitoring;
    private DateTime? _lastChangeAt;
    private int _runCount;
    private int _duplicateCount;
    private int _nextSeq = 1;

    /// <inheritdoc />
    public event EventHandler<TestRunReceivedEventArgs>? TestRunReceived;

    /// <summary>Kablovi za koje je pozvana <see cref="PrepareTestAsync"/>, redom.</summary>
    public IReadOnlyList<Cable> PreparedCables
    {
        get
        {
            lock (_sync)
            {
                return _prepared.ToArray();
            }
        }
    }

    /// <summary>
    /// Ako je postavljeno, <see cref="PrepareTestAsync"/> baca ovaj izuzetak — za proveru kako se
    /// GUI ponaša kad priprema ne uspe (nema šablona, spec folder ne postoji).
    /// </summary>
    public Exception? PrepareFailure { get; set; }

    /// <inheritdoc />
    public TesterGatewayState State
    {
        get
        {
            lock (_sync)
            {
                return new TesterGatewayState
                {
                    IsMonitoring = _monitoring,
                    WatchedPath = "(bez testera — lažni gateway)",
                    LastChangeAt = _lastChangeAt,
                    ProcessedRunCount = _runCount,
                    ProcessedLineCount = _runCount,
                    SkippedDuplicateCount = _duplicateCount,
                    LastPreparedSpecPath = _prepared.Count == 0
                        ? null
                        : _prepared[^1].SpecFileNameWithExtension
                };
            }
        }
    }

    /// <inheritdoc />
    public Task PrepareTestAsync(Cable cable, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cable);
        ct.ThrowIfCancellationRequested();

        if (PrepareFailure is not null)
        {
            return Task.FromException(PrepareFailure);
        }

        lock (_sync)
        {
            _prepared.Add(cable);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void StartMonitoring()
    {
        lock (_sync)
        {
            if (_monitoring)
            {
                return;
            }

            _uiContext = SynchronizationContext.Current;
            _monitoring = true;
        }
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (_sync)
        {
            _monitoring = false;
        }
    }

    /// <summary>
    /// „Stigao" je rezultat. Dok nadgledanje nije pokrenuto, ništa se ne dešava — isto kao kod
    /// rada preko fajlova.
    /// </summary>
    /// <param name="run">Rezultat.</param>
    /// <param name="isBackfill">
    /// <c>true</c> označava rezultat zatečen u fajlu pri pokretanju; prikaz ishoda na njega ne sme
    /// da reaguje. Vidi <see cref="TestRunReceivedEventArgs.IsBackfill"/>.
    /// </param>
    /// <returns><c>true</c> ako je rezultat prijavljen, <c>false</c> ako je preskočen.</returns>
    public bool Receive(TestRun run, bool isBackfill = false)
    {
        ArgumentNullException.ThrowIfNull(run);

        lock (_sync)
        {
            if (!_monitoring)
            {
                return false;
            }

            if (!_seen.Add(TestRunKey.Of(run)))
            {
                _duplicateCount++;
                return false;
            }

            _runCount++;
            _lastChangeAt = DateTime.Now;
        }

        Raise(new TestRunReceivedEventArgs(run, sourcePath: null, warnings: null, isBackfill));
        return true;
    }

    /// <summary>
    /// „Stigao" je rezultat zapisan kao red CSV-a, sa podrazumevanim zaglavljem. Prolazi kroz
    /// pravi parser, pa se ovako proverava i ponašanje na neobičnom redu.
    /// </summary>
    public IReadOnlyList<TestRun> ReceiveCsvLine(string csvLine, bool isBackfill = false)
    {
        ArgumentNullException.ThrowIfNull(csvLine);

        CsvParseResult parsed = ResultCsvParser.ParseAll(DefaultHeader + "\r\n" + csvLine.TrimEnd('\r', '\n') + "\r\n");

        var delivered = new List<TestRun>();
        foreach (TestRun run in parsed.Runs)
        {
            if (Receive(run, isBackfill))
            {
                delivered.Add(run);
            }
        }

        return delivered;
    }

    /// <summary>Prolazan rezultat za zadati kabl; redni broj se sam povećava.</summary>
    public TestRun ReceivePass(Cable cable, string @operator = "Miloš", bool isBackfill = false)
    {
        ArgumentNullException.ThrowIfNull(cable);
        TestRun run = BuildRun(cable.SpecFileName, passed: true, @operator);
        Receive(run, isBackfill);
        return run;
    }

    /// <summary>
    /// Rezultat zatečen u fajlu pri pokretanju — za proveru da prikaz ishoda na njega ne reaguje.
    /// </summary>
    public TestRun ReceiveBackfillPass(Cable cable, string @operator = "Miloš")
        => ReceivePass(cable, @operator, isBackfill: true);

    /// <summary>Rezultat sa greškama, npr. <c>ReceiveFail(kabl, "SHORT O01-O02", "OPEN O31-O32")</c>.</summary>
    public TestRun ReceiveFail(Cable cable, params string[] defectMessages)

    {
        ArgumentNullException.ThrowIfNull(cable);
        ArgumentNullException.ThrowIfNull(defectMessages);

        TestRun run = BuildRun(cable.SpecFileName, passed: false, "Miloš");
        foreach (string message in defectMessages)
        {
            run.Defects.Add(ResultCsvParser.CreateDefect(message));
        }

        Receive(run);
        return run;
    }

    /// <summary>Zaglavlje koje <see cref="ReceiveCsvLine"/> podrazumeva; isto kao u stvarnom fajlu.</summary>
    public const string DefaultHeader = "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit,";

    private TestRun BuildRun(string specFileName, bool passed, string @operator)
    {
        int seq;
        lock (_sync)
        {
            seq = _nextSeq++;
        }

        DateTime now = DateTime.Now;
        var run = new TestRun
        {
            Seq = seq,
            SpecFileName = specFileName,
            Passed = passed,
            TestedAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second),
            Operator = @operator
        };

        run.RawRow = string.Join(
            ",",
            seq.ToString(CultureInfo.InvariantCulture),
            specFileName,
            passed ? "pass" : "fail",
            run.TestedAt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
            run.TestedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty,
            @operator);

        return run;
    }

    private void Raise(TestRunReceivedEventArgs args)
    {
        EventHandler<TestRunReceivedEventArgs>? handler = TestRunReceived;
        if (handler is null)
        {
            return;
        }

        SynchronizationContext? context = _uiContext;

        // Nema GUI niti (testovi, konzola), ili smo već na njoj — poziva se odmah, da redosled
        // bude očigledan. Inače se prebacuje na nit koja je pokrenula nadgledanje.
        if (context is null || ReferenceEquals(SynchronizationContext.Current, context))
        {
            handler(this, args);
            return;
        }

        context.Post(_ => handler(this, args), null);
    }
}
