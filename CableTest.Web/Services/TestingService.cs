using System.Globalization;
using System.Text;
using CableTest.Core.Configuration;
using CableTest.Core.Data;
using CableTest.Core.Gateway;
using CableTest.Core.Model;
using CableTest.Core.Reporting;

namespace CableTest.Web.Services;

/// <summary>Stanje velikog prikaza ishoda.</summary>
public enum OutcomeState
{
    /// <summary>Nijedan rezultat još nije stigao uživo.</summary>
    Waiting,

    /// <summary>Poslednji uživo rezultat je prošao.</summary>
    Pass,

    /// <summary>Poslednji uživo rezultat je pao.</summary>
    Fail
}

/// <summary>
/// Ekran „Ispitivanje" u web izdanju — isto ponašanje kao <c>TestingViewModel</c> iz WPF verzije,
/// samo bez WPF-a.
/// </summary>
/// <remarks>
/// <para>
/// Servis je <b>singleton</b>: tester je jedan, pa je i stanje jedno. Svaki pretraživač koji se
/// javi na localhost vidi isti ekran — otvoreno na dva monitora znači dva prikaza istog testa, a
/// ne dva nezavisna nadgledanja.
/// </para>
/// <para>
/// <b>Pravilo od koga sve zavisi:</b> veliki prikaz ishoda se pomera isključivo na rezultat sa
/// <see cref="TestRunReceivedEventArgs.IsBackfill"/> = <c>false</c>. Zatečeni rezultati idu u
/// istoriju, ali ne i na ekran.
/// </para>
/// <para>
/// Događaji gateway-a ovde stižu sa pozadinske niti (u ASP.NET-u nema
/// <see cref="SynchronizationContext"/>), pa prikaz na <see cref="Changed"/> mora da se osvežava
/// kroz <c>InvokeAsync</c> u komponenti.
/// </para>
/// </remarks>
public sealed class TestingService : IDisposable
{
    /// <summary>Tekst velikog prikaza dok nijedan rezultat nije stigao.</summary>
    public const string WaitingText = "ČEKA SE REZULTAT";

    /// <summary>Argument komandne linije kojim se pokreće demo režim.</summary>
    public const string DemoArgument = "--demo";

    /// <summary>Ime fajla baze u demo režimu.</summary>
    public const string DemoDatabaseFileName = "CableTest.demo.db";

    private readonly object _sync = new();

    private readonly ITesterGateway _gateway;
    private readonly IVehicleRepository _vehicles;
    private readonly ICableRepository _cables;
    private readonly ITestRunImporter _importer;
    private readonly ITestRunRepository _runs;
    private readonly AppSettings _settings;
    private readonly FakeTesterGateway? _fake;

    private Timer? _stateTimer;

    private IReadOnlyList<Vehicle> _vehicleList = Array.Empty<Vehicle>();
    private IReadOnlyList<Cable> _cableList = Array.Empty<Cable>();
    private IReadOnlyList<string> _netList = Array.Empty<string>();
    private IReadOnlyList<string> _defectList = Array.Empty<string>();

    private Vehicle? _selectedVehicle;
    private Cable? _selectedCable;
    private OutcomeState _outcome = OutcomeState.Waiting;
    private TestRun? _currentRun;
    private Cable? _currentRunCable;
    private string? _prepareMessage;
    private string? _prepareError;
    private string? _problemText;
    private bool _problemIsError;
    private string _statusText = "Nadgledanje nije pokrenuto.";
    private string _watchedFileText = "—";
    private string _lastChangeText = "—";
    private string _processedText = "0";
    private bool _isMonitoring;
    private int _backfillCount;
    private int _demoSeq;
    private int _todayTotal;
    private int _todayPassed;
    private int _todayFailed;
    private bool _started;
    private bool _disposed;

    public TestingService(
        ITesterGateway gateway,
        IVehicleRepository vehicles,
        ICableRepository cables,
        ITestRunImporter importer,
        ITestRunRepository runs,
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(vehicles);
        ArgumentNullException.ThrowIfNull(cables);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(settings);

        _gateway = gateway;
        _vehicles = vehicles;
        _cables = cables;
        _importer = importer;
        _runs = runs;
        _settings = settings;
        _fake = gateway as FakeTesterGateway;
    }

    /// <summary>Okida se kad se bilo šta na ekranu promeni. Zove se sa pozadinske niti.</summary>
    public event Action? Changed;

    /// <summary>
    /// Okida se kad uživo rezultat stigne; <c>true</c> za PASS. Zvuk pušta pretraživač, jer
    /// server nema ni zvučnik ni operatera pored sebe.
    /// </summary>
    public event Action<bool>? ResultArrived;

    // -----------------------------------------------------------------------------------
    // Sastavljanje
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Pravi servis sa bazom, repozitorijumima i gateway-em — ono što u WPF verziji radi
    /// <c>App.Start</c>.
    /// </summary>
    public static TestingService Create(bool demo)
    {
        SettingsStore store = SettingsStore.Default();
        AppSettings settings = store.Load();

        if (demo)
        {
            settings.DemoMode = true;
        }

        CableTestDatabase database = OpenDatabase(settings.DemoMode);

        var cables = new SqliteCableRepository(database);
        var vehicles = new SqliteVehicleRepository(database);
        var runs = new SqliteTestRunRepository(database);
        var importer = new TestRunImporter(cables, runs);

        ITesterGateway gateway = settings.DemoMode
            ? new FakeTesterGateway()
            : new FileBasedTesterGateway(settings.ToGatewayOptions());

        return new TestingService(gateway, vehicles, cables, importer, runs, settings);
    }

    /// <summary>
    /// Otvara bazu i primenjuje migracije.
    /// </summary>
    /// <remarks>
    /// Demo režim radi nad <b>zasebnim fajlom baze</b>. Simulirani rezultati ne smeju da se nađu
    /// u stvarnoj istoriji testova — ona je dokaz da je kabl ispitan.
    /// </remarks>
    private static CableTestDatabase OpenDatabase(bool demo)
    {
        if (!demo)
        {
            return CableTestDatabase.OpenDefault();
        }

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CableTest");

        Directory.CreateDirectory(folder);
        return CableTestDatabase.OpenAndMigrate(Path.Combine(folder, DemoDatabaseFileName));
    }

    // -----------------------------------------------------------------------------------
    // Izbor vozila i kabla
    // -----------------------------------------------------------------------------------

    public IReadOnlyList<Vehicle> Vehicles => _vehicleList;

    public IReadOnlyList<Cable> Cables => _cableList;

    /// <summary>Net lista izabranog kabla, spremna za prikaz („1. O01-O02-O31-O32").</summary>
    public IReadOnlyList<string> Nets => _netList;

    public long? SelectedVehicleId => _selectedVehicle?.Id;

    public long? SelectedCableId => _selectedCable?.Id;

    public Cable? SelectedCable => _selectedCable;

    public bool HasSelectedCable => _selectedCable is not null;

    /// <summary>Menja izabrano vozilo i učitava njegove kablove.</summary>
    public void SelectVehicle(long? id)
    {
        lock (_sync)
        {
            Vehicle? vehicle = id is null ? null : _vehicleList.FirstOrDefault(v => v.Id == id.Value);
            if (ReferenceEquals(vehicle, _selectedVehicle))
            {
                return;
            }

            _selectedVehicle = vehicle;
            LoadCables();
        }

        RaiseChanged();
    }

    /// <summary>Menja izabrani kabl i učitava njegovu net listu.</summary>
    public void SelectCable(long? id)
    {
        lock (_sync)
        {
            Cable? cable = id is null ? null : _cableList.FirstOrDefault(c => c.Id == id.Value);
            if (ReferenceEquals(cable, _selectedCable))
            {
                return;
            }

            SetSelectedCable(cable);
        }

        RaiseChanged();
    }

    // -----------------------------------------------------------------------------------
    // Priprema testa
    // -----------------------------------------------------------------------------------

    /// <summary>Putanja upisanog .c61 fajla i uputstvo operateru; <c>null</c> dok se ne pripremi test.</summary>
    public string? PrepareMessage => _prepareMessage;

    public bool HasPrepareMessage => !string.IsNullOrEmpty(_prepareMessage);

    /// <summary>Zašto priprema nije uspela; <c>null</c> ako je sve u redu.</summary>
    public string? PrepareError => _prepareError;

    public bool HasPrepareError => !string.IsNullOrEmpty(_prepareError);

    /// <summary>
    /// Priprema test za izabrani kabl: generiše .c61 i upisuje ga u spec folder.
    /// Greška se ne guta — operater mora da vidi zašto fajl nije upisan.
    /// </summary>
    public async Task PrepareTestAsync(CancellationToken ct = default)
    {
        Cable? cable;

        lock (_sync)
        {
            cable = _selectedCable;
            _prepareError = null;
            _prepareMessage = null;
        }

        RaiseChanged();

        if (cable is null)
        {
            return;
        }

        // Priprema ne baca na očekivane ishode; sve što je pošlo naopako stoji u odgovoru.
        GatewayResult result = await _gateway.LoadProgramAsync(cable, ct).ConfigureAwait(false);

        lock (_sync)
        {
            if (result.IsOk)
            {
                string? path = _gateway.Diagnostics.LastPreparedSpecPath;

                _prepareMessage = IsDemoMode
                    ? "Demo režim: .c61 fajl nije upisan na disk. " +
                      "U stvarnom radu bi ovde stajala puna putanja upisanog fajla."
                    : $"Upisano: {path}\n" + _gateway.Capabilities.StartInstruction;
            }
            else
            {
                _prepareError = "Priprema testa nije uspela. " + result.Message;
            }
        }

        RaiseChanged();
    }

    // -----------------------------------------------------------------------------------
    // Veliki prikaz ishoda
    // -----------------------------------------------------------------------------------

    /// <summary>Stanje prikaza. Menja se samo na uživo rezultat.</summary>
    public OutcomeState Outcome => _outcome;

    /// <summary>Krupan tekst uz boju — boja sama nije dovoljna (daltonizam).</summary>
    public string OutcomeText => Outcome switch
    {
        OutcomeState.Pass => "PASS",
        OutcomeState.Fail => "FAIL",
        _ => WaitingText
    };

    public string OutcomeSubText => Outcome switch
    {
        OutcomeState.Pass => "KABL JE ISPRAVAN",
        OutcomeState.Fail => "KABL NIJE ISPRAVAN",
        _ => "Pokreni test na testeru."
    };

    public bool IsWaiting => Outcome == OutcomeState.Waiting;

    /// <summary>Da li je prikazan neki rezultat — od toga zavisi dugme „Merna karta".</summary>
    public bool HasResult => _currentRun is not null;

    public string ResultTimeText { get; private set; } = string.Empty;

    public string ResultCableText { get; private set; } = string.Empty;

    public string ResultOperatorText { get; private set; } = string.Empty;

    /// <summary>Greške poslednjeg rezultata, u obliku razumljivom operateru.</summary>
    public IReadOnlyList<string> ResultDefects => _defectList;

    // -----------------------------------------------------------------------------------
    // Traka stanja
    // -----------------------------------------------------------------------------------

    public bool IsMonitoring => _isMonitoring;

    public string StatusText => _statusText;

    public string WatchedFileText => _watchedFileText;

    public string LastChangeText => _lastChangeText;

    public string ProcessedText => _processedText;

    /// <summary>
    /// Greška ili upozorenje koje operater mora da vidi. Ćutanje o grešci je gore od greške:
    /// bez ovoga aplikacija izgleda kao da radi, a rezultati ne stižu.
    /// </summary>
    public string? ProblemText => _problemText;

    public bool HasProblem => !string.IsNullOrEmpty(_problemText);

    /// <summary>Da li je <see cref="ProblemText"/> greška (crveno) ili upozorenje (žuto).</summary>
    public bool ProblemIsError => _problemIsError;

    /// <summary>Koliko je ranijih rezultata učitano u istoriju pri pokretanju.</summary>
    public int BackfillCount => _backfillCount;

    /// <summary>
    /// Obaveštenje da zatečeni rezultati nisu prikazani. Stoji da operateru bude jasno zašto je
    /// ekran prazan iako je fajl pun.
    /// </summary>
    public string BackfillNoticeText => BackfillCount == 0
        ? string.Empty
        : "Pri pokretanju je u istoriju upisano ranijih rezultata: " +
          $"{BackfillCount.ToString(CultureInfo.InvariantCulture)}. " +
          "Oni se ne prikazuju kao trenutni ishod.";

    public bool HasBackfillNotice => BackfillCount > 0;

    // -----------------------------------------------------------------------------------
    // Demo režim
    // -----------------------------------------------------------------------------------

    /// <summary>Da li aplikacija radi bez testera.</summary>
    public bool IsDemoMode => _settings.DemoMode;

    public string DemoBannerText =>
        "DEMO REŽIM — rezultati su simulirani, tester nije priključen. Ovo nisu stvarna merenja.";

    public bool CanSimulate => IsDemoMode && _fake is not null && _selectedCable is not null;

    /// <summary>Da li pretraživač pušta zvučni signal uz rezultat.</summary>
    public bool SoundEnabled => _settings.SoundEnabled;

    // -----------------------------------------------------------------------------------
    // Život ekrana
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Učitava podatke, proverava putanje i pokreće nadgledanje. Poziva se jednom, pri podizanju
    /// servera — nadgledanje ne sme da zavisi od toga da li je neki pretraživač otvoren.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            if (_started)
            {
                return;
            }

            _started = true;

            LoadVehicles();
            CheckPaths();
        }

        RefreshToday();

        _gateway.ResultReceived += OnTestRunReceived;
        _gateway.StartMonitoring();

        RefreshState();

        // Stanje nadgledanja se menja i kad nijedan rezultat ne stigne (fajl nestane, folder se
        // pojavi, greška prođe), pa traka stanja mora sama da se osvežava.
        _stateTimer = new Timer(_ => RefreshState(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Osvežava traku stanja. <see cref="Changed"/> se okida samo kad se nešto zaista promenilo —
    /// inače bi se svaki otvoren pretraživač ponovo iscrtavao svake sekunde bez razloga.
    /// </summary>
    public void RefreshState()
    {
        bool changed;

        lock (_sync)
        {
            TesterGatewayState state = _gateway.Diagnostics;

            string status = state.IsMonitoring ? "Nadgledanje je aktivno." : "Nadgledanje nije pokrenuto.";
            string watched = state.CurrentFilePath ?? state.WatchedPath ?? "(putanja nije podešena)";

            string lastChange = state.LastChangeAt is null
                ? "—"
                : state.LastChangeAt.Value.ToString("dd.MM.yyyy. HH:mm:ss", CultureInfo.InvariantCulture);

            string processed = state.ProcessedRunCount.ToString(CultureInfo.InvariantCulture);

            changed = _isMonitoring != state.IsMonitoring
                      || !string.Equals(_statusText, status, StringComparison.Ordinal)
                      || !string.Equals(_watchedFileText, watched, StringComparison.Ordinal)
                      || !string.Equals(_lastChangeText, lastChange, StringComparison.Ordinal)
                      || !string.Equals(_processedText, processed, StringComparison.Ordinal);

            _isMonitoring = state.IsMonitoring;
            _statusText = status;
            _watchedFileText = watched;
            _lastChangeText = lastChange;
            _processedText = processed;

            changed |= UpdateProblem(state);
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stateTimer?.Dispose();
        _gateway.ResultReceived -= OnTestRunReceived;
        _gateway.StopMonitoring();
        (_gateway as IDisposable)?.Dispose();
    }

    // -----------------------------------------------------------------------------------
    // Dolazak rezultata
    // -----------------------------------------------------------------------------------

    private void OnTestRunReceived(object? sender, TestRunReceivedEventArgs e)
    {
        // Prvo istorija, pa prikaz. I zatečeni i uživo rezultati idu u bazu bez razlike.
        TestRunImportResult imported = _importer.Import(e.Run);

        if (e.IsBackfill)
        {
            // NE dira prikaz ishoda. Vidi TestRunReceivedEventArgs.IsBackfill.
            lock (_sync)
            {
                _backfillCount++;
            }

            RefreshToday();
            RaiseChanged();
            RefreshState();
            return;
        }

        lock (_sync)
        {
            ShowResult(imported.Run, imported.Cable);
        }

        RefreshToday();
        RaiseChanged();
        ResultArrived?.Invoke(imported.Run.Passed);
        RefreshState();
    }

    private void ShowResult(TestRun run, Cable? cable)
    {
        _currentRun = run;
        _currentRunCable = cable;

        ResultTimeText = run.TestedAt == DateTime.MinValue
            ? "(vreme nije prepoznato u CSV-u)"
            : run.TestedAt.ToString("dd.MM.yyyy. HH:mm:ss", CultureInfo.InvariantCulture);

        ResultCableText = cable is null
            ? $"{run.SpecFileName} — kabl nije u bazi"
            : $"{cable.Code} ({run.SpecFileName})";

        ResultOperatorText = string.IsNullOrWhiteSpace(run.Operator) ? "—" : run.Operator;

        _defectList = run.Defects.Select(d => d.Describe()).ToArray();

        _outcome = run.Passed ? OutcomeState.Pass : OutcomeState.Fail;
    }

    // -----------------------------------------------------------------------------------
    // Merna karta
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Merna karta trenutnog rezultata kao HTML; <c>null</c> ako nijedan rezultat nije prikazan.
    /// </summary>
    /// <remarks>
    /// Desktop verzija upisuje fajl i otvara ga u podrazumevanom pregledaču. Ovde je pregledač
    /// već otvoren, pa se karta služi kao stranica — štampa i „Sačuvaj kao PDF" rade isto.
    /// </remarks>
    public string? BuildMeasurementCardHtml()
    {
        TestRun? run;
        Cable? cable;

        lock (_sync)
        {
            run = _currentRun;
            cable = _currentRunCable;
        }

        if (run is null)
        {
            return null;
        }

        Vehicle? vehicle = cable is null ? null : _vehicles.GetById(cable.VehicleId);

        return MeasurementCard.BuildHtml(new MeasurementCardData
        {
            Run = run,
            Cable = cable,
            Vehicle = vehicle,
            Note = IsDemoMode ? "DEMO REŽIM — rezultat je simuliran, ovo nije stvarno merenje." : null
        });
    }

    // -----------------------------------------------------------------------------------
    // Demo
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Ubacuje simulirani rezultat kroz pravi parser, kao uživo. Dostupno samo u demo režimu.
    /// </summary>
    public void Simulate(bool passed)
    {
        FakeTesterGateway? fake = _fake;
        string line;

        lock (_sync)
        {
            if (fake is null || _selectedCable is null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            string osTest = passed ? "PASS" : string.Join(';', BuildDemoDefects(_selectedCable)) + ";FAIL";

            line = string.Join(
                ',',
                (++_demoSeq).ToString(CultureInfo.InvariantCulture),
                _selectedCable.SpecFileName,
                passed ? "pass" : "fail",
                now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                string.Empty,
                string.Empty,
                OperatorName(),
                passed ? "PASS" : "FAIL",
                osTest,
                string.Empty);
        }

        fake.ReceiveCsvLine(line);
    }

    /// <summary>Greške za demo — prave oznake tačaka iz net liste izabranog kabla.</summary>
    private static IReadOnlyList<string> BuildDemoDefects(Cable cable)
    {
        var messages = new List<string>();

        foreach (CableNet net in cable.Nets.OrderBy(n => n.Ordinal))
        {
            string[] points = net.Points.Split(
                Net.Separator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (points.Length >= 2)
            {
                messages.Add($"SHORT {points[0]}-{points[1]}");
            }

            if (points.Length >= 4)
            {
                messages.Add($"OPEN {points[^2]}-{points[^1]}");
            }

            if (messages.Count > 0)
            {
                break;
            }
        }

        if (messages.Count == 0)
        {
            messages.Add("SHORT O01-O02");
        }

        return messages;
    }

    private string OperatorName()
        => string.IsNullOrWhiteSpace(_settings.OperatorName) ? Environment.UserName : _settings.OperatorName;

    // -----------------------------------------------------------------------------------
    // Učitavanje
    // -----------------------------------------------------------------------------------

    private void LoadVehicles()
    {
        _vehicleList = _vehicles.GetAll();
        _selectedVehicle = _vehicleList.FirstOrDefault();
        LoadCables();
    }

    private void LoadCables()
    {
        _cableList = _selectedVehicle is null
            ? Array.Empty<Cable>()
            : _cables.GetByVehicle(_selectedVehicle.Id);

        SetSelectedCable(_cableList.FirstOrDefault());
    }

    private void SetSelectedCable(Cable? cable)
    {
        _selectedCable = cable;
        _prepareMessage = null;
        _prepareError = null;
        LoadNets();
    }

    private void LoadNets()
    {
        _netList = _selectedCable is null
            ? Array.Empty<string>()
            : _selectedCable.Nets
                .OrderBy(n => n.Ordinal)
                .Select(n => $"{n.Ordinal.ToString(CultureInfo.InvariantCulture)}. {n.Points}")
                .ToArray();
    }

    // -----------------------------------------------------------------------------------
    // Problemi
    // -----------------------------------------------------------------------------------

    private void CheckPaths()
    {
        IReadOnlyList<PathIssue> issues = PathCheck.CheckAll(_settings);
        if (issues.Count == 0)
        {
            return;
        }

        bool hasError = issues.Any(i => i.IsError);
        var text = new StringBuilder();

        foreach (PathIssue issue in issues)
        {
            if (text.Length > 0)
            {
                text.Append('\n');
            }

            text.Append(issue.Message);
        }

        ShowProblem(text.ToString(), hasError);
    }

    /// <summary>
    /// Problem iz podešavanja ostaje prikazan dok ga ne potisne noviji problem iz nadgledanja.
    /// </summary>
    private bool UpdateProblem(TesterGatewayState state)
    {
        if (state.LastError is not null)
        {
            return ShowProblem(state.LastError, isError: true);
        }

        if (state.LastWarning is not null)
        {
            return ShowProblem(state.LastWarning, isError: false);
        }

        return false;
    }

    private bool ShowProblem(string text, bool isError)
    {
        if (string.Equals(_problemText, text, StringComparison.Ordinal) && _problemIsError == isError)
        {
            return false;
        }

        _problemText = text;
        _problemIsError = isError;
        return true;
    }

    // -----------------------------------------------------------------------------------
    // Katalog, mapa portova i brojači — podaci koje traži novi raspored ekrana
    // -----------------------------------------------------------------------------------

    /// <summary>Podešavanja u upotrebi — ekran „Podešavanja“ ih samo prikazuje.</summary>
    public AppSettings Settings => _settings;

    /// <summary>Ime operatera onako kako stoji u zaglavlju.</summary>
    public string OperatorDisplayName => OperatorName();

    /// <summary>Sva vozila iz baze.</summary>
    public IReadOnlyList<Vehicle> AllVehicles => _vehicles.GetAll();

    /// <summary>Svi kablovi iz baze, sa učitanim netovima.</summary>
    public IReadOnlyList<Cable> AllCables => _cables.GetAll();

    /// <summary>Poslednjih <paramref name="count"/> testova iz istorije, najnoviji prvi.</summary>
    public IReadOnlyList<TestRun> RecentRuns(int count) => _runs.GetRecent(count);

    /// <summary>Vozilo po ključu; <c>null</c> ako ga nema.</summary>
    public Vehicle? VehicleById(long id) => _vehicles.GetById(id);

    /// <summary>Izabrano vozilo.</summary>
    public Vehicle? SelectedVehicle => _selectedVehicle;

    /// <summary>Tekst u zaglavlju pored zelene tačke.</summary>
    public string ConnectionText => IsDemoMode
        ? "Demo režim"
        : _isMonitoring ? "Tester povezan" : "Tester nije povezan";

    /// <summary>Da li je veza sa testerom u redu — od toga zavisi boja tačke u zaglavlju.</summary>
    public bool ConnectionOk => IsDemoMode || _isMonitoring;

    /// <summary>Broj netova izabranog kabla.</summary>
    public int SelectedNetCount => _selectedCable?.Nets.Count ?? 0;

    /// <summary>Broj ispitnih tačaka izabranog kabla.</summary>
    public int SelectedPointCount => CableLayout.CountPoints(_selectedCable);

    /// <summary>
    /// Mapa pinova izabranog kabla: svih 16 portova testera (A..P), sa oznakom da li ih kabl
    /// koristi.
    /// </summary>
    public IReadOnlyList<PortUsage> Ports => CableLayout.Ports(_selectedCable);

    /// <summary>Broj portova (konektora) koje izabrani kabl koristi.</summary>
    public int SelectedPortCount => CableLayout.CountPorts(_selectedCable);

    /// <summary>Ime test programa izabranog kabla, sa ekstenzijom.</summary>
    public string SelectedSpecFileText => _selectedCable is null
        ? "—"
        : _selectedCable.SpecFileNameWithExtension;

    /// <summary>
    /// Tabela netova izabranog kabla sa ishodom poslednjeg prikazanog rezultata.
    /// </summary>
    /// <remarks>
    /// Kolone su isključivo ono što aplikacija zaista zna: tačke neta i da li je tester nad njim
    /// prijavio grešku. Izmerene vrednosti otpora se ne prikazuju zato što ih CSV izveštaj
    /// CableConnector-a nema — izmišljen broj bio bi gori od prazne kolone.
    /// </remarks>
    public IReadOnlyList<NetRow> NetRows
    {
        get
        {
            lock (_sync)
            {
                return CableLayout.NetRows(_selectedCable, _currentRun, _currentRunCable);
            }
        }
    }

    /// <summary>Koliko je testova danas u istoriji.</summary>
    public int TodayTotal => _todayTotal;

    /// <summary>Koliko je današnjih testova prošlo.</summary>
    public int TodayPassed => _todayPassed;

    /// <summary>Koliko je današnjih testova palo.</summary>
    public int TodayFailed => _todayFailed;

    /// <summary>Koliko se poslednjih zapisa pregleda pri brojanju današnjih testova.</summary>
    private const int TodayLookback = 1000;

    /// <summary>
    /// Prebrojava današnje testove iz istorije. Broji se iz baze, a ne iz pamćenja ekrana, da
    /// brojač preživi ponovno pokretanje servera usred smene.
    /// </summary>
    private void RefreshToday()
    {
        DateTime today = DateTime.Today;
        int total = 0;
        int passed = 0;

        try
        {
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
        }
        catch (Exception)
        {
            // Brojač u podnožju nije razlog da ekran stane.
            return;
        }

        _todayTotal = total;
        _todayPassed = passed;
        _todayFailed = total - passed;
    }

    private void RaiseChanged() => Changed?.Invoke();
}
