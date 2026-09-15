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
/// privremeni fajl pa preimenuje), pa se na njega ne oslanjamo sam. Tajmer svejedno, na svake dve
/// sekunde, uporedi veličinu i vreme izmene. Ako se ne slažu sa zapamćenim, fajl se čita.
/// </para>
/// <para>
/// Sve što na disku može da pođe naopako ovde je očekivano stanje, ne izuzetak: fajl još ne
/// postoji kad aplikacija krene, fajl nestane, fajl bude zamenjen novim (dnevna rotacija po
/// datumu u imenu), folder ne postoji, fajl je u tom trenutku zaključan jer ga CableConnector
/// upravo upisuje. Nijedan od tih slučajeva ne ruši nadgledanje — zapiše se u
/// <see cref="State"/> i pokušava se ponovo pri sledećoj proveri.
/// </para>
/// </remarks>
public sealed class FileBasedTesterGateway : ITesterGateway, IDisposable
{
    private readonly TesterGatewayOptions _options;

    /// <summary>Štiti polja stanja. Nikad se ne drži dok se okida događaj.</summary>
    private readonly object _sync = new();

    /// <summary>Serijalizuje provere; watcher i tajmer zovu <see cref="Poll"/> sa raznih niti.</summary>
    private readonly object _pollGate = new();

    private readonly ResultCsvParser _parser = new();
    private readonly HashSet<TestRunKey> _seen = new();

    private FileSystemWatcher? _watcher;
    private Timer? _timer;
    private SynchronizationContext? _uiContext;

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

    public FileBasedTesterGateway(TesterGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public event EventHandler<TestRunReceivedEventArgs>? TestRunReceived;

    /// <summary>Podešavanja sa kojima gateway radi.</summary>
    public TesterGatewayOptions Options => _options;

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

    /// <summary>
    /// Generiše .c61 iz šablona i net liste kabla i upisuje ga u spec folder.
    /// Ništa drugo ne pokreće — dalje operater ručno radi u CableConnector-u.
    /// </summary>
    /// <exception cref="ArgumentException">Ime spec fajla kabla nije ispravno.</exception>
    /// <exception cref="FileNotFoundException">Šablon MASTER.c61 ne postoji.</exception>
    /// <exception cref="DirectoryNotFoundException">Spec folder ne postoji.</exception>
    public Task PrepareTestAsync(Cable cable, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cable);

        // Upis na disk ne sme da blokira GUI nit; greške stižu kroz Task, ne kroz State —
        // pripremu je pokrenuo operater i mora odmah da vidi šta nije uspelo.
        return Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();

                C61Generator generator = C61Generator.FromFile(ResolveTemplatePath());

                List<Net> nets = cable.Nets
                    .OrderBy(n => n.Ordinal)
                    .Select(n => n.ToNet())
                    .ToList();

                ct.ThrowIfCancellationRequested();

                string path = generator.WriteSpec(_options.SpecFolder, cable.SpecFileName, nets);

                lock (_sync)
                {
                    _lastSpecPath = path;
                }
            },
            ct);
    }

    /// <summary>
    /// Počinje da nadgleda CSV. Poziva se sa GUI niti — tada se hvata
    /// <see cref="SynchronizationContext"/> na kome će se okidati <see cref="TestRunReceived"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prvo čitanje se radi odmah, sinhrono, pa se prijavljuju i redovi koji su već u fajlu. Tako
    /// se pokupi i ono što je tester zapisao dok aplikacija nije radila. Da isti rezultat ne bi
    /// ušao u istoriju dvaput, svaki se prepoznaje po prirodnom ključu <see cref="TestRunKey"/> —
    /// ovde, i još jednom jedinstvenim indeksom u bazi.
    /// </para>
    /// <para>
    /// Ti rezultati nose <see cref="TestRunReceivedEventArgs.IsBackfill"/> = <c>true</c>. Razlika
    /// je bezbednosna, ne kozmetička: prikaz ishoda sme da se pomeri samo na rezultat koji je
    /// stigao uživo. Vidi <see cref="TestRunReceivedEventArgs.IsBackfill"/>.
    /// </para>
    /// </remarks>
    public void StartMonitoring()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            if (_monitoring)
            {
                return;
            }

            _uiContext = SynchronizationContext.Current;
            _monitoring = true;
            _currentFile = null;
            _lastLength = -1;
            _lastWriteUtc = DateTime.MinValue;
            _lastError = null;
            _lastErrorAt = null;
            _lastWarning = null;
            _parser.Reset();
            _seen.Clear();
        }

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
    }

    // ---------------------------------------------------------------------------------------
    // Nadgledanje
    // ---------------------------------------------------------------------------------------

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
            RecordError($"Nadgledanje foldera \"{folder}\" nije uspelo: {ex.Message}");
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e) => Poll(backfill: false);

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // Bafer se prepunio ili je disk nestao. Watcher se odbacuje i pravi ponovo pri sledećoj
        // proveri; tajmer u međuvremenu radi svoj posao, pa se ništa ne gubi.
        RecordError("Nadgledanje fajla je prekinuto: " + e.GetException().Message + " Pokušava se ponovo.");

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
            RecordError(ex.Message);
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
            RecordError($"Fajl \"{path}\" nije dostupan: {ex.Message}");
            return;
        }

        if (info.Length == _lastLength && info.LastWriteTimeUtc == _lastWriteUtc)
        {
            return; // ništa se nije promenilo
        }

        byte[] bytes;
        try
        {
            bytes = CsvTextReader.ReadAllBytes(path);
        }
        catch (FileNotFoundException)
        {
            ForgetCurrentFile();
            return;
        }
        catch (DirectoryNotFoundException)
        {
            ForgetCurrentFile();
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Očekivano: CableConnector u ovom trenutku upisuje. Čita se pri sledećoj proveri.
            RecordError($"Fajl \"{path}\" je trenutno zauzet: {ex.Message}");
            return;
        }

        string text = CsvTextReader.Decode(bytes);
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

    /// <summary>Prosleđuje pročitane rezultate, preskačući one koji su već stigli.</summary>
    private void Deliver(CsvParseResult parsed, string path, bool backfill)
    {
        var toRaise = new List<TestRun>();

        lock (_sync)
        {
            foreach (TestRun run in parsed.Runs)
            {
                if (!_seen.Add(TestRunKey.Of(run)))
                {
                    // Fajl je prepisan istim imenom pa pročitan od početka — isti rezultat je već
                    // prijavljen. Tiho se preskače; vidi TestRunKey.
                    _duplicateCount++;
                    continue;
                }

                toRaise.Add(run);
            }

            _runCount += toRaise.Count;
        }

        foreach (TestRun run in toRaise)
        {
            Raise(new TestRunReceivedEventArgs(run, path, parsed.Warnings, backfill));
        }
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
            RecordError("Putanja CSV fajla sa rezultatima nije podešena.");
            return null;
        }

        if (!IsFolderMode())
        {
            return File.Exists(configured) ? Path.GetFullPath(configured) : null;
        }

        if (!Directory.Exists(configured))
        {
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

    private void RecordError(string message)
    {
        lock (_sync)
        {
            _lastError = message;
            _lastErrorAt = DateTime.Now;
        }
    }
}
