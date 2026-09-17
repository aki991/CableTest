using CableTest.Core.Model;
using CableTest.Core.Results;
using CableTest.Core.Spec;

namespace CableTest.Core.Gateway;

/// <summary>
/// Veza sa testerom preko fajlova: .c61 se upisuje u spec folder, rezultati se čitaju iz CSV-a
/// koji piše CableConnector.
/// </summary>
/// <remarks>
/// <para>
/// Fajl nadgleda i <see cref="FileSystemWatcher"/> i tajmer. Watcher daje brzu reakciju, ali je
/// poznat po tome da propušta događaje (mrežni disk, bafer koji se prepuni, program koji piše u
/// privremeni fajl pa preimenuje), i po tome da isti upis prijavi više puta. Zato se na njega ne
/// oslanjamo sam: tajmer svejedno, na svake dve sekunde, uporedi veličinu i vreme izmene, a
/// događaji watcher-a se poništavaju kroz kratko odlaganje po putanji.
/// </para>
/// <para>
/// Fajl se ne čita čim se javi promena — tada je najčešće još otvoren za upis ili upisan do pola.
/// Čitanje ide kroz <see cref="StableFileReader"/>: pokušaji sa odlaganjem, i veličina koja se ne
/// menja u dva uzastopna očitavanja.
/// </para>
/// <para>
/// Sve što na disku može da pođe naopako ovde je očekivano stanje, ne izuzetak: fajl još ne
/// postoji kad aplikacija krene, fajl nestane, fajl bude zamenjen novim (dnevna rotacija po
/// datumu u imenu), folder ne postoji, fajl je zaključan. Nijedan od tih slučajeva ne ruši
/// nadgledanje — prijavi se kroz <see cref="GatewayError"/> i pokuša ponovo pri sledećoj proveri.
/// </para>
/// </remarks>
public sealed class FileBasedTesterGateway : ITesterGateway, IDisposable
{
    /// <summary>Koliko se dugo poništavaju ponovljeni događaji watcher-a za istu putanju.</summary>
    public static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(500);

    private readonly TesterGatewayOptions _options;

    /// <summary>Štiti polja stanja. Nikad se ne drži dok se okida događaj.</summary>
    private readonly object _sync = new();

    /// <summary>Serijalizuje provere; watcher i tajmer zovu <see cref="Poll"/> sa raznih niti.</summary>
    private readonly object _pollGate = new();

    private readonly ResultCsvParser _parser = new();
    private readonly StableFileReader _reader;
    private readonly Func<DateTime> _clock;

    /// <summary>Otisak sadržaja → fajl u kome je prvi put viđen.</summary>
    private readonly Dictionary<string, string> _seenHashes = new(StringComparer.Ordinal);

    /// <summary>Poništavanje ponovljenih događaja watcher-a za istu putanju.</summary>
    private readonly PathDebounce _debounce = new(DebounceWindow);

    /// <summary>Ko čeka rezultat kroz <see cref="WaitForResultAsync"/>.</summary>
    private readonly ResultWaitList _waiters = new();

    private FileSystemWatcher? _watcher;
    private Timer? _timer;

    private TesterState _state = TesterState.Idle;

    /// <summary>
    /// Rezultat koji je stigao dok je test bio u toku, a niko još nije počeo da čeka.
    /// </summary>
    /// <remarks>
    /// Mašina ume da odgovori pre nego što aplikacija stigne da se postavi na čekanje — pogotovo
    /// kad je fajl već bio upisan. Bez ovoga bi takav rezultat propao, a čekanje bi isteklo iako
    /// je test odavno gotov.
    /// </remarks>
    private TestRun? _pending;

    private bool _monitoring;
    private bool _disposed;
    private string? _currentFile;
    private long _lastLength = -1;
    private DateTime _lastWriteUtc = DateTime.MinValue;

    private DateTime? _lastChangeAt;
    private DateTime? _lastErrorAt;
    private string? _lastError;
    private string? _lastWarning;
    private string? _lastSpecPath;
    private int _runCount;
    private int _duplicateCount;

    public FileBasedTesterGateway(TesterGatewayOptions options, StableFileReader? reader = null, Func<DateTime>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _reader = reader ?? new StableFileReader();
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    /// <inheritdoc />
    public event EventHandler<TestRunReceivedEventArgs>? ResultReceived;

    /// <inheritdoc />
    public event EventHandler<TesterStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<GatewayErrorEventArgs>? GatewayError;

    /// <summary>Podešavanja sa kojima gateway radi.</summary>
    public TesterGatewayOptions Options => _options;

    /// <inheritdoc />
    public TesterState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Rad preko fajlova ne može sam da pokrene test: aplikacija samo ostavlja .c61 na disku, a
    /// START na mašini pritiska operater. To nije nedostatak nego granica ovog načina razmene —
    /// zato ekran umesto dugmeta mora da pokaže uputstvo.
    /// </remarks>
    public TesterCapabilities Capabilities { get; } = new(
        CanStartTest: false,
        StartInstruction:
            "U CableConnector-u izaberi pripremljeni spec, pritisni Download, pa pritisni START na testeru.",
        Description: "Rezultati se čitaju iz CSV fajla koji piše CableConnector.");

    /// <inheritdoc />
    public TesterGatewayState Diagnostics
    {
        get
        {
            lock (_sync)
            {
                return new TesterGatewayState
                {
                    IsMonitoring = _monitoring,
                    WatchedPath = string.IsNullOrWhiteSpace(_options.ResultPath) ? null : _options.ResultPath,
                    CurrentFilePath = _currentFile,
                    LastChangeAt = _lastChangeAt,
                    ProcessedLineCount = _parser.ProcessedLineCount,
                    ProcessedRunCount = _runCount,
                    SkippedDuplicateCount = _duplicateCount,
                    LastError = _lastError,
                    LastErrorAt = _lastErrorAt,
                    LastWarning = _lastWarning,
                    LastPreparedSpecPath = _lastSpecPath
                };
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // Operacije
    // ---------------------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<GatewayResult> LoadProgramAsync(Cable cable, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cable);

        string blocked = Guard(TesterOperation.LoadProgram);
        if (blocked.Length != 0)
        {
            return GatewayResult.InvalidState(blocked);
        }

        try
        {
            await PrepareTestAsync(cable, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return GatewayResult.Cancelled("Priprema test programa je otkazana.");
        }
        catch (Exception ex)
        {
            // Priprema je radnja koju je pokrenuo operater: ishod ide njemu, a ne u tišinu.
            MoveTo(TesterOperation.Fail, "priprema test programa nije uspela");
            RaiseError("Priprema test programa nije uspela: " + ex.Message, ex);
            return GatewayResult.Failed("Priprema test programa nije uspela: " + ex.Message, ex);
        }

        lock (_sync)
        {
            // Nov program, nov test: rezultat prethodnog više ne važi.
            _pending = null;
        }

        MoveTo(TesterOperation.LoadProgram, "test program je pripremljen");
        return GatewayResult.Ok();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uvek <see cref="GatewayStatus.NotSupported"/>: kod rada preko fajlova aplikacija nema čime
    /// da pokrene test. Nije izuzetak — ekran na osnovu <see cref="Capabilities"/> i ne nudi dugme.
    /// </remarks>
    public Task<GatewayResult> StartTestAsync(CancellationToken ct)
    {
        // Ni otkazan token ovde ne pravi izuzetak: odgovor je isti bez obzira na sve.
        _ = ct;

        return Task.FromResult(GatewayResult.NotSupported(
            "Rad preko fajlova ne može sam da pokrene test. " + Capabilities.StartInstruction));
    }

    /// <inheritdoc />
    public async Task<GatewayResult> WaitForResultAsync(TimeSpan timeout, CancellationToken ct)
    {
        // Prvo zatečeni rezultat: ako je stigao dok se čekanje tek postavljalo, nema se šta čekati.
        lock (_sync)
        {
            if (_pending is not null)
            {
                TestRun arrived = _pending;
                _pending = null;
                return GatewayResult.Ok(arrived);
            }
        }

        string blocked = Guard(TesterOperation.WaitForResult);
        if (blocked.Length != 0)
        {
            return GatewayResult.InvalidState(blocked);
        }

        TaskCompletionSource<TestRun> waiter = _waiters.Add();

        MoveTo(TesterOperation.WaitForResult, "čeka se rezultat");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(timeout);
        }

        // Otkazivanje mora da prekine čekanje odmah, a ne da ga pusti do isteka roka.
        using CancellationTokenRegistration registration = cts.Token.Register(() => waiter.TrySetCanceled());

        try
        {
            TestRun run = await waiter.Task.ConfigureAwait(false);
            return GatewayResult.Ok(run);
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested)
            {
                // Odustao je pozivalac; test na mašini time nije prekinut, pa stanje ostaje.
                return GatewayResult.Cancelled("Čekanje na rezultat je otkazano.");
            }

            string message = $"Rezultat nije stigao u zadatom vremenu ({timeout.TotalSeconds:0} s).";
            MoveTo(TesterOperation.Fail, "isteklo je vreme čekanja na rezultat");
            RaiseError(message, error: null, isWarning: true);
            return GatewayResult.Timeout(message);
        }
        finally
        {
            _waiters.Remove(waiter);
        }
    }

    /// <summary>
    /// Generiše .c61 iz šablona i net liste kabla i upisuje ga u spec folder.
    /// </summary>
    /// <remarks>
    /// Sirova radnja, bez mašine stanja: baca ono što pođe naopako. Ugovor iz
    /// <see cref="ITesterGateway"/> ide kroz <see cref="LoadProgramAsync"/>, koja ovo obavija u
    /// <see cref="GatewayResult"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">Ime spec fajla kabla nije ispravno.</exception>
    /// <exception cref="FileNotFoundException">Šablon MASTER.c61 ne postoji.</exception>
    /// <exception cref="DirectoryNotFoundException">Spec folder ne postoji.</exception>
    public Task PrepareTestAsync(Cable cable, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cable);

        // Upis na disk ne sme da blokira GUI nit.
        return Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();

                C61Generator generator = C61Generator.FromFile(ResolveTemplatePath());

                // Net lista se izvodi iz ožičenja i priključne tabele; kabl koji nije spreman za
                // ispitivanje ovde staje, pre nego što se ijedan bajt upiše u spec folder.
                IReadOnlyList<Net> nets = CableSpec.NetsFor(cable);

                ct.ThrowIfCancellationRequested();

                string path = generator.WriteSpec(_options.SpecFolder, cable.SpecFileName, nets);

                lock (_sync)
                {
                    _lastSpecPath = path;
                }
            },
            ct);
    }

    // ---------------------------------------------------------------------------------------
    // Nadgledanje
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Počinje da nadgleda CSV.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prvo čitanje se radi odmah, sinhrono, pa se prijavljuju i redovi koji su već u fajlu. Tako
    /// se pokupi i ono što je tester zapisao dok aplikacija nije radila.
    /// </para>
    /// <para>
    /// Ti rezultati nose <see cref="TestRunReceivedEventArgs.IsBackfill"/> = <c>true</c>. Razlika
    /// je bezbednosna, ne kozmetička: prikaz ishoda sme da se pomeri samo na rezultat koji je
    /// stigao uživo. Vidi <see cref="TestRunReceivedEventArgs.IsBackfill"/>.
    /// </para>
    /// </remarks>
    public void StartMonitoring()
    {
        if (_disposed)
        {
            RaiseError("Nadgledanje se ne može pokrenuti: veza sa testerom je zatvorena.");
            return;
        }

        lock (_sync)
        {
            if (_monitoring)
            {
                return;
            }

            _monitoring = true;
            _currentFile = null;
            _lastLength = -1;
            _lastWriteUtc = DateTime.MinValue;
            _lastError = null;
            _lastErrorAt = null;
            _lastWarning = null;
            _parser.Reset();
            _seenHashes.Clear();
        }

        _debounce.Clear();

        TryCreateWatcher();

        // Zatečeni sadržaj fajla — ide u istoriju, ali označen kao backfill.
        Poll(backfill: true);

        TimeSpan interval = _options.PollInterval > TimeSpan.Zero
            ? _options.PollInterval
            : TesterGatewayOptions.DefaultPollInterval;

        _timer = new Timer(_ => Poll(backfill: false), null, interval, interval);
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        Timer? timer;
        FileSystemWatcher? watcher;

        lock (_sync)
        {
            if (!_monitoring)
            {
                return;
            }

            _monitoring = false;
            timer = _timer;
            watcher = _watcher;
            _timer = null;
            _watcher = null;
        }

        timer?.Dispose();

        if (watcher is not null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    /// <summary>
    /// Odmah proverava fajl, ne čekajući tajmer. Za dugme „Osveži" u GUI-ju i za testove.
    /// </summary>
    public void CheckNow() => Poll(backfill: false);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopMonitoring();
        _waiters.CancelAll();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void TryCreateWatcher()
    {
        string? folder = WatchedFolder();
        if (folder is null || !Directory.Exists(folder))
        {
            // Folder još ne postoji; tajmer nastavlja da proverava i watcher se pravi kasnije.
            return;
        }

        try
        {
            var watcher = new FileSystemWatcher(folder)
            {
                Filter = IsFolderMode() ? _options.ResultFilePattern : Path.GetFileName(_options.ResultPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                IncludeSubdirectories = false
            };

            watcher.Changed += OnFileSystemEvent;
            watcher.Created += OnFileSystemEvent;
            watcher.Deleted += OnFileSystemEvent;
            watcher.Renamed += OnFileSystemEvent;
            watcher.Error += OnWatcherError;
            watcher.EnableRaisingEvents = true;

            lock (_sync)
            {
                if (!_monitoring)
                {
                    watcher.Dispose();
                    return;
                }

                _watcher?.Dispose();
                _watcher = watcher;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            RaiseError($"Nadgledanje foldera \"{folder}\" nije uspelo: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Događaj watcher-a; ponovljeni događaji za istu putanju u kratkom roku se poništavaju.
    /// </summary>
    /// <remarks>
    /// Jedan upis ume da podigne i tri-četiri događaja (veličina, vreme izmene, pa još jednom).
    /// Bez ovoga bi se za svaki od njih pokretalo čitanje sa odlaganjima — posao bez ikakvog
    /// dobitka. Tajmer i dalje radi svoje, pa se ništa ne gubi ako se događaj poništi.
    /// </remarks>
    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        if (!_debounce.ShouldHandle(e.FullPath, _clock()))
        {
            return;
        }

        Poll(backfill: false);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // Bafer se prepunio ili je disk nestao. Watcher se odbacuje i pravi ponovo pri sledećoj
        // proveri; tajmer u međuvremenu radi svoj posao, pa se ništa ne gubi.
        RaiseError("Nadgledanje fajla je prekinuto: " + e.GetException().Message + " Pokušava se ponovo.",
            e.GetException(), isWarning: true);

        FileSystemWatcher? watcher;
        lock (_sync)
        {
            watcher = _watcher;
            _watcher = null;
        }

        watcher?.Dispose();
    }

    private void Poll(bool backfill)
    {
        // Ako je provera već u toku, ova se preskače — sledeća će pokupiti sve što je stiglo.
        if (!Monitor.TryEnter(_pollGate))
        {
            return;
        }

        try
        {
            lock (_sync)
            {
                if (!_monitoring)
                {
                    return;
                }
            }

            if (_watcher is null)
            {
                TryCreateWatcher();
            }

            PollCore(backfill);
        }
        catch (Exception ex)
        {
            // Nadgledanje radi u pozadini; izuzetak odavde nema ko da uhvati i srušio bi aplikaciju.
            RaiseError(ex.Message, ex);
        }
        finally
        {
            Monitor.Exit(_pollGate);
        }
    }

    private void PollCore(bool backfill)
    {
        string? path = ResolveCurrentFile();

        if (path is null)
        {
            ForgetCurrentFile();
            return;
        }

        if (!string.Equals(path, _currentFile, StringComparison.OrdinalIgnoreCase))
        {
            // Drugi fajl nego do sada: dnevna rotacija, ili se fajl konačno pojavio.
            SwitchTo(path);
        }

        FileInfo info;
        try
        {
            info = new FileInfo(path);
            if (!info.Exists)
            {
                ForgetCurrentFile();
                return;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RaiseError($"Fajl \"{path}\" nije dostupan: {ex.Message}", ex);
            return;
        }

        if (info.Length == _lastLength && info.LastWriteTimeUtc == _lastWriteUtc)
        {
            return; // ništa se nije promenilo
        }

        // Čitanje sa pokušajima; zaključan fajl i fajl koji još raste ostaju za sledeći prolaz.
        // Čeka se ovde, na tajmerskoj niti — GUI nit nikad ne prolazi ovuda, a poll gate sprečava
        // da se dve provere preklope.
        StableReadResult read = _reader.ReadAsync(path).GetAwaiter().GetResult();

        if (!read.IsOk)
        {
            switch (read.Status)
            {
                case StableReadStatus.Missing:
                    ForgetCurrentFile();
                    return;

                default:
                    // Ni veličina ni vreme izmene se ne pamte — sledeći prolaz pokušava ponovo.
                    RaiseError($"Fajl \"{path}\" još nije spreman za čitanje: {read.Message}", read.Error, isWarning: true);
                    return;
            }
        }

        string text = CsvTextReader.Decode(read.Bytes!);
        CsvParseResult parsed = _parser.ReadNew(text);

        lock (_sync)
        {
            _lastLength = info.Length;
            _lastWriteUtc = info.LastWriteTimeUtc;
            _lastChangeAt = DateTime.Now;
            _lastError = null;
            _lastErrorAt = null;

            if (parsed.HasWarnings)
            {
                _lastWarning = parsed.Warnings[^1];
            }
        }

        Deliver(parsed, path, backfill);
    }

    /// <summary>
    /// Prosleđuje pročitane rezultate, preskačući ponovljena čitanja istog fajla.
    /// </summary>
    /// <remarks>
    /// Prepoznavanje ide po SHA-256 sirovog reda: isti otisak iz istog fajla je isti red pročitan
    /// po drugi put (fajl je prepisan pa čitan od početka) i ćutke se preskače. Isti otisak iz
    /// drugog fajla se <b>ne odbacuje</b> — prijavljuje se označen, a odluku donosi sloj iznad.
    /// </remarks>
    private void Deliver(CsvParseResult parsed, string path, bool backfill)
    {
        var toRaise = new List<(TestRun Run, string? DuplicateOf)>();

        lock (_sync)
        {
            foreach (TestRun run in parsed.Runs)
            {
                string hash = ResultHash.Of(run);

                if (_seenHashes.TryGetValue(hash, out string? firstSeenIn))
                {
                    if (string.Equals(firstSeenIn, path, StringComparison.OrdinalIgnoreCase))
                    {
                        _duplicateCount++;
                        continue;
                    }

                    toRaise.Add((run, firstSeenIn));
                    continue;
                }

                _seenHashes[hash] = path;
                toRaise.Add((run, null));
            }

            _runCount += toRaise.Count;
        }

        foreach ((TestRun run, string? duplicateOf) in toRaise)
        {
            if (duplicateOf is not null)
            {
                RaiseError(
                    $"Isti rezultat je već viđen u fajlu \"{duplicateOf}\": red se prosleđuje označen kao duplikat.",
                    error: null,
                    isWarning: true);
            }

            var args = new TestRunReceivedEventArgs(run, path, parsed.Warnings, backfill, duplicateOf);

            Raise(args);

            if (!backfill)
            {
                CompleteWaiters(run);
            }
        }
    }

    private void Raise(TestRunReceivedEventArgs args)
    {
        // Bez prebacivanja na GUI nit: gateway ne zna da GUI postoji. Vidi ITesterGateway.
        ResultReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Predaje rezultat onome ko ga čeka, ili ga ostavlja po strani ako se još niko nije postavio.
    /// </summary>
    private void CompleteWaiters(TestRun run)
    {
        if (_waiters.HasWaiters)
        {
            // Prvo stanje, pa buđenje: onaj ko je čekao mora da zatekne Completed, a ne stanje
            // koje se tek sprema da se promeni.
            MoveTo(TesterOperation.ReceiveResult, "rezultat je stigao");
            _waiters.CompleteAll(run);
            return;
        }

        lock (_sync)
        {
            if (_state != TesterState.WaitingForResult)
            {
                // Niko nije ni tražio ovaj rezultat (operater je pritisnuo START sam, ili se čita
                // zatečeni sadržaj) — ide samo u istoriju, kroz ResultReceived.
                return;
            }

            _pending = run;
        }

        MoveTo(TesterOperation.ReceiveResult, "rezultat je stigao pre nego što se počelo čekati");
    }

    /// <summary>Prelazak na drugi fajl: pamćenje dokle se stiglo više ne važi.</summary>
    private void SwitchTo(string path)
    {
        lock (_sync)
        {
            _currentFile = path;
            _lastLength = -1;
            _lastWriteUtc = DateTime.MinValue;
            _parser.Reset();
        }
    }

    /// <summary>Fajl je nestao ili ga još nema; čeka se da se pojavi.</summary>
    private void ForgetCurrentFile()
    {
        lock (_sync)
        {
            if (_currentFile is null && _lastLength < 0)
            {
                return;
            }

            _currentFile = null;
            _lastLength = -1;
            _lastWriteUtc = DateTime.MinValue;
            _lastChangeAt = DateTime.Now;
            _parser.Reset();
        }
    }

    // ---------------------------------------------------------------------------------------
    // Stanje
    // ---------------------------------------------------------------------------------------

    /// <summary>Prazan tekst ako je radnja dozvoljena, inače objašnjenje zašto nije.</summary>
    private string Guard(TesterOperation operation)
    {
        lock (_sync)
        {
            return TesterStateMachine.IsAllowed(_state, operation)
                ? string.Empty
                : TesterStateMachine.Explain(_state, operation);
        }
    }

    private void MoveTo(TesterOperation operation, string reason)
    {
        TesterState previous;
        TesterState next;

        lock (_sync)
        {
            previous = _state;
            next = TesterStateMachine.Next(previous, operation);

            if (next == previous)
            {
                return;
            }

            _state = next;
        }

        StateChanged?.Invoke(this, new TesterStateChangedEventArgs(previous, next, reason));
    }

    /// <summary>
    /// Prijavljuje grešku ili upozorenje.
    /// </summary>
    /// <remarks>
    /// Isto upozorenje uzastopno (folder koji ne postoji, fajl koji je i dalje zaključan) prijavi
    /// se <b>jednom</b>: provera se ponavlja svake dve sekunde, a operateru ista rečenica stotinu
    /// puta ne govori ništa novo. Stanje se svejedno osvežava, pa traka stanja ostaje tačna.
    /// </remarks>
    private void RaiseError(string message, Exception? error = null, bool isWarning = false)
    {
        bool repeated;

        lock (_sync)
        {
            if (isWarning)
            {
                repeated = string.Equals(_lastWarning, message, StringComparison.Ordinal);
                _lastWarning = message;
            }
            else
            {
                repeated = string.Equals(_lastError, message, StringComparison.Ordinal);
                _lastError = message;
                _lastErrorAt = DateTime.Now;
            }
        }

        if (repeated)
        {
            return;
        }

        GatewayError?.Invoke(this, new GatewayErrorEventArgs(message, error, isWarning));
    }

    // ---------------------------------------------------------------------------------------
    // Putanje
    // ---------------------------------------------------------------------------------------

    /// <summary>Da li je podešena putanja foldera, a ne konkretnog fajla.</summary>
    public bool IsFolderMode()
    {
        string configured = _options.ResultPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return false;
        }

        return Directory.Exists(configured) || Path.GetExtension(configured).Length == 0;
    }

    /// <summary>
    /// Fajl koji se trenutno prati: podešeni fajl, ili najnoviji CSV u podešenom folderu.
    /// <c>null</c> ako ga nema.
    /// </summary>
    /// <remarks>
    /// U folderu se bira po vremenu izmene, a pri istom vremenu po imenu unazad — imena su
    /// tipično sa datumom, pa je veće ime noviji dan.
    /// </remarks>
    public string? ResolveCurrentFile()
    {
        string configured = _options.ResultPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            RaiseError("Putanja CSV fajla sa rezultatima nije podešena.");
            return null;
        }

        if (!IsFolderMode())
        {
            return File.Exists(configured) ? Path.GetFullPath(configured) : null;
        }

        if (!Directory.Exists(configured))
        {
            // Folder je nestao ili ga još nema: prijavi se i nastavlja se sa pokušajima.
            RaiseError($"Folder sa rezultatima \"{configured}\" ne postoji; pokušava se ponovo.", error: null, isWarning: true);
            return null;
        }

        string? newest = null;
        DateTime newestWrite = DateTime.MinValue;

        foreach (string candidate in Directory.EnumerateFiles(configured, _options.ResultFilePattern))
        {
            DateTime write;
            try
            {
                write = File.GetLastWriteTimeUtc(candidate);
            }
            catch (IOException)
            {
                continue;
            }

            if (newest is null
                || write > newestWrite
                || (write == newestWrite && string.CompareOrdinal(candidate, newest) > 0))
            {
                newest = candidate;
                newestWrite = write;
            }
        }

        return newest is null ? null : Path.GetFullPath(newest);
    }

    private string? WatchedFolder()
    {
        string configured = _options.ResultPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        if (IsFolderMode())
        {
            return configured;
        }

        string? folder = Path.GetDirectoryName(Path.GetFullPath(configured));
        return string.IsNullOrEmpty(folder) ? null : folder;
    }

    private string ResolveTemplatePath()
    {
        string configured = _options.TemplatePath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = TesterGatewayOptions.DefaultTemplatePath;
        }

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
    }
}
