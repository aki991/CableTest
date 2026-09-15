using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using CableTest.App.Mvvm;
using CableTest.App.Services;
using CableTest.Core.Configuration;
using CableTest.Core.Data;
using CableTest.Core.Gateway;
using CableTest.Core.Model;
using CableTest.Core.Reporting;
using System.Windows.Data;

namespace CableTest.App.ViewModels;

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
/// Glavni ekran — „Ispitivanje".
/// </summary>
/// <remarks>
/// <para>
/// <b>Pravilo od koga sve zavisi:</b> veliki prikaz ishoda se pomera isključivo na rezultat sa
/// <see cref="TestRunReceivedEventArgs.IsBackfill"/> = <c>false</c>. Rezultati zatečeni u fajlu
/// pri pokretanju idu u istoriju, ali ne i na ekran — inače bi operater ujutru video jučerašnji
/// PASS i pustio neispitan kabl dalje.
/// </para>
/// <para>
/// Sve što stigne upisuje se u istoriju pre nego što se bilo šta prikaže. Upis je sinhron, na
/// istoj niti: reč je o jednom redu u lokalnoj SQLite bazi, traje milisekundama, a rezultat koji
/// se prikaže a ne upiše bio bi gori problem od kratkog zastoja.
/// </para>
/// </remarks>
public sealed class TestingViewModel : ObservableObject, IDisposable
{
    /// <summary>Tekst velikog prikaza dok nijedan rezultat nije stigao.</summary>
    public const string WaitingText = "ČEKA SE REZULTAT";

    private readonly ITesterGateway _gateway;
    private readonly IVehicleRepository _vehicles;
    private readonly ICableRepository _cables;
    private readonly ITestRunImporter _importer;
    private readonly ISoundPlayer _sounds;
    private readonly IDocumentViewer _documents;
    private readonly AppSettings _settings;
    private readonly FakeTesterGateway? _fake;

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
    private string _cableFilter = string.Empty;
    private bool _isMonitoring;
    private int _backfillCount;
    private int _demoSeq;
    private bool _started;
    private bool _disposed;

    public TestingViewModel(
        ITesterGateway gateway,
        IVehicleRepository vehicles,
        ICableRepository cables,
        ITestRunImporter importer,
        ISoundPlayer sounds,
        IDocumentViewer documents,
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(vehicles);
        ArgumentNullException.ThrowIfNull(cables);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(sounds);
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(settings);

        _gateway = gateway;
        _vehicles = vehicles;
        _cables = cables;
        _importer = importer;
        _sounds = sounds;
        _documents = documents;
        _settings = settings;
        _fake = gateway as FakeTesterGateway;

        PrepareTestCommand = new AsyncRelayCommand(PrepareTestAsync, () => SelectedCable is not null, ShowPrepareError);
        MeasurementCardCommand = new RelayCommand(ShowMeasurementCard, () => HasResult);
        SimulatePassCommand = new RelayCommand(() => Simulate(passed: true), () => CanSimulate);
        SimulateFailCommand = new RelayCommand(() => Simulate(passed: false), () => CanSimulate);
    }

    // -----------------------------------------------------------------------------------
    // Izbor vozila i kabla
    // -----------------------------------------------------------------------------------

    public ObservableCollection<Vehicle> Vehicles { get; } = new();

    public ObservableCollection<Cable> Cables { get; } = new();

    /// <summary>
    /// Mapa pinova izabranog kabla — svih 16 portova testera (A..P), sa oznakom da li ih kabl
    /// koristi.
    /// </summary>
    public ObservableCollection<PortUsage> Ports { get; } = new();

    /// <summary>Tabela netova izabranog kabla, sa ishodom poslednjeg prikazanog rezultata.</summary>
    public ObservableCollection<NetRow> NetRows { get; } = new();

    /// <summary>
    /// Tekst iz polja za pretragu kablova. Spisak se sužava u mestu, kroz podrazumevani pogled
    /// nad <see cref="Cables"/>, pa izbor kabla i dalje radi bez posebnog vezivanja.
    /// </summary>
    public string CableFilter
    {
        get => _cableFilter;
        set
        {
            if (Set(ref _cableFilter, value))
            {
                CollectionViewSource.GetDefaultView(Cables).Refresh();
            }
        }
    }

    /// <summary>Net lista izabranog kabla, spremna za prikaz („1. O01-O02-O31-O32").</summary>
    public ObservableCollection<string> Nets { get; } = new();

    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            if (Set(ref _selectedVehicle, value))
            {
                LoadCables();
            }
        }
    }

    public Cable? SelectedCable
    {
        get => _selectedCable;
        set
        {
            if (!Set(ref _selectedCable, value))
            {
                return;
            }

            LoadNets();
            LoadPorts();
            LoadNetRows();
            PrepareMessage = null;
            PrepareError = null;
            PrepareTestCommand.RaiseCanExecuteChanged();
            SimulatePassCommand.RaiseCanExecuteChanged();
            SimulateFailCommand.RaiseCanExecuteChanged();
            RaiseAll(
                nameof(HasSelectedCable),
                nameof(CanSimulate),
                nameof(SelectedCableCode),
                nameof(SelectedCableName),
                nameof(SelectedNetCount),
                nameof(SelectedPointCount),
                nameof(SelectedPortCount),
                nameof(SelectedSpecFileText));
        }
    }

    public bool HasSelectedCable => SelectedCable is not null;

    /// <summary>Oznaka izabranog kabla; „—“ ako kabl nije izabran.</summary>
    public string SelectedCableCode => SelectedCable?.Code ?? NetRow.Unknown;

    /// <summary>Naziv izabranog kabla; „—“ ako ga nema.</summary>
    public string SelectedCableName => string.IsNullOrWhiteSpace(SelectedCable?.Description)
        ? NetRow.Unknown
        : SelectedCable!.Description;

    /// <summary>Broj netova izabranog kabla.</summary>
    public int SelectedNetCount => SelectedCable?.Nets.Count ?? 0;

    /// <summary>Broj ispitnih tačaka izabranog kabla.</summary>
    public int SelectedPointCount => CableLayout.CountPoints(SelectedCable);

    /// <summary>Broj portova (konektora) koje izabrani kabl koristi.</summary>
    public int SelectedPortCount => CableLayout.CountPorts(SelectedCable);

    /// <summary>Ime test programa izabranog kabla, sa ekstenzijom.</summary>
    public string SelectedSpecFileText => SelectedCable?.SpecFileNameWithExtension ?? NetRow.Unknown;

    // -----------------------------------------------------------------------------------
    // Priprema testa
    // -----------------------------------------------------------------------------------

    public AsyncRelayCommand PrepareTestCommand { get; }

    /// <summary>Putanja upisanog .c61 fajla i uputstvo operateru; <c>null</c> dok se ne pripremi test.</summary>
    public string? PrepareMessage
    {
        get => _prepareMessage;
        private set
        {
            if (Set(ref _prepareMessage, value))
            {
                Raise(nameof(HasPrepareMessage));
            }
        }
    }

    public bool HasPrepareMessage => !string.IsNullOrEmpty(PrepareMessage);

    /// <summary>Zašto priprema nije uspela; <c>null</c> ako je sve u redu.</summary>
    public string? PrepareError
    {
        get => _prepareError;
        private set
        {
            if (Set(ref _prepareError, value))
            {
                Raise(nameof(HasPrepareError));
            }
        }
    }

    public bool HasPrepareError => !string.IsNullOrEmpty(PrepareError);

    // -----------------------------------------------------------------------------------
    // Veliki prikaz ishoda
    // -----------------------------------------------------------------------------------

    /// <summary>Stanje prikaza. Menja se samo na uživo rezultat.</summary>
    public OutcomeState Outcome
    {
        get => _outcome;
        private set
        {
            if (Set(ref _outcome, value))
            {
                RaiseAll(nameof(OutcomeText), nameof(OutcomeSubText), nameof(HasResult), nameof(IsWaiting));
                MeasurementCardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Krupan tekst uz boju — boja sama nije dovoljna (daltonizam).</summary>
    /// <summary>
    /// Krupan tekst uz boju — boja sama nije dovoljna (daltonizam). Na srpskom, kao i ceo ekran;
    /// „PASS“ i „FAIL“ ostaju samo u CSV-u testera i u imenima kolona koje on piše.
    /// </summary>
    public string OutcomeText => Outcome switch
    {
        OutcomeState.Pass => NetRow.Passed,
        OutcomeState.Fail => NetRow.Failed,
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
    public ObservableCollection<string> ResultDefects { get; } = new();

    public RelayCommand MeasurementCardCommand { get; }

    // -----------------------------------------------------------------------------------
    // Traka stanja
    // -----------------------------------------------------------------------------------

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (Set(ref _isMonitoring, value))
            {
                RaiseAll(nameof(ConnectionText), nameof(ConnectionOk));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public string WatchedFileText
    {
        get => _watchedFileText;
        private set => Set(ref _watchedFileText, value);
    }

    public string LastChangeText
    {
        get => _lastChangeText;
        private set => Set(ref _lastChangeText, value);
    }

    public string ProcessedText
    {
        get => _processedText;
        private set => Set(ref _processedText, value);
    }

    /// <summary>
    /// Greška ili upozorenje koje operater mora da vidi. Ćutanje o grešci je gore od greške:
    /// bez ovoga aplikacija izgleda kao da radi, a rezultati ne stižu.
    /// </summary>
    public string? ProblemText
    {
        get => _problemText;
        private set
        {
            if (Set(ref _problemText, value))
            {
                Raise(nameof(HasProblem));
            }
        }
    }

    public bool HasProblem => !string.IsNullOrEmpty(ProblemText);

    /// <summary>Da li je <see cref="ProblemText"/> greška (crveno) ili upozorenje (žuto).</summary>
    public bool ProblemIsError
    {
        get => _problemIsError;
        private set => Set(ref _problemIsError, value);
    }

    /// <summary>Koliko je ranijih rezultata učitano u istoriju pri pokretanju.</summary>
    public int BackfillCount
    {
        get => _backfillCount;
        private set
        {
            if (Set(ref _backfillCount, value))
            {
                RaiseAll(nameof(BackfillNoticeText), nameof(HasBackfillNotice));
            }
        }
    }

    /// <summary>
    /// Obaveštenje da zatečeni rezultati nisu prikazani. Stoji da operateru bude jasno zašto je
    /// ekran prazan iako je fajl pun.
    /// </summary>
    public string BackfillNoticeText => BackfillCount == 0
        ? string.Empty
        : $"Pri pokretanju je u istoriju upisano ranijih rezultata: " +
          $"{BackfillCount.ToString(CultureInfo.InvariantCulture)}. " +
          "Oni se ne prikazuju kao trenutni ishod.";

    public bool HasBackfillNotice => BackfillCount > 0;

    // -----------------------------------------------------------------------------------
    // Demo režim
    // -----------------------------------------------------------------------------------

    /// <summary>Tekst u zaglavlju pored tackice stanja veze.</summary>
    public string ConnectionText => IsDemoMode
        ? "Demo režim"
        : IsMonitoring ? "Tester povezan" : "Tester nije povezan";

    /// <summary>Da li je veza sa testerom u redu — od toga zavisi boja tačkice u zaglavlju.</summary>
    public bool ConnectionOk => IsDemoMode || IsMonitoring;

    /// <summary>Ime operatera onako kako stoji u zaglavlju.</summary>
    public string OperatorDisplayName => OperatorName();

    /// <summary>Da li aplikacija radi bez testera.</summary>
    public bool IsDemoMode => _settings.DemoMode;

    public string DemoBannerText =>
        "DEMO REŽIM — rezultati su simulirani, tester nije priključen. Ovo nisu stvarna merenja.";

    public bool CanSimulate => IsDemoMode && _fake is not null && SelectedCable is not null;

    public RelayCommand SimulatePassCommand { get; }

    public RelayCommand SimulateFailCommand { get; }

    // -----------------------------------------------------------------------------------
    // Život ekrana
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Okida se kad rezultat uđe u istoriju — i zatečeni i uživo. Okvir aplikacije na to osvežava
    /// brojace u podnožju i spisak u Istoriji.
    /// </summary>
    public event Action? RunImported;

    /// <summary>
    /// Učitava podatke, proverava putanje i pokreće nadgledanje. Poziva se sa GUI niti — tada
    /// gateway hvata <see cref="SynchronizationContext"/> na kome će okidati događaje.
    /// </summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;

        // Filtar spiska kablova: prazan tekst pušta sve.
        CollectionViewSource.GetDefaultView(Cables).Filter = MatchesFilter;

        // Mapa pinova ima svih 16 portova i kad kabl nije izabran — prazan okvir bi izgledao
        // kao da ekran nije do kraja učitan.
        LoadPorts();
        LoadNetRows();

        LoadVehicles();
        CheckPaths();

        _gateway.TestRunReceived += OnTestRunReceived;
        _gateway.StartMonitoring();

        RefreshState();
    }

    /// <summary>Osvežava traku stanja. Prikaz zove periodično, jer se stanje menja i bez događaja.</summary>
    public void RefreshState()
    {
        TesterGatewayState state = _gateway.State;

        IsMonitoring = state.IsMonitoring;
        StatusText = state.IsMonitoring ? "Nadgledanje je aktivno." : "Nadgledanje nije pokrenuto.";
        WatchedFileText = state.CurrentFilePath ?? state.WatchedPath ?? "(putanja nije podešena)";

        LastChangeText = state.LastChangeAt is null
            ? "—"
            : state.LastChangeAt.Value.ToString("dd.MM.yyyy. HH:mm:ss", CultureInfo.InvariantCulture);

        ProcessedText = state.ProcessedRunCount.ToString(CultureInfo.InvariantCulture);

        UpdateProblem(state);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gateway.TestRunReceived -= OnTestRunReceived;
        _gateway.StopMonitoring();
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
            BackfillCount++;
            RunImported?.Invoke();
            RefreshState();
            return;
        }

        ShowResult(imported.Run, imported.Cable);
        RunImported?.Invoke();

        if (imported.Run.Passed)
        {
            _sounds.PlayPass();
        }
        else
        {
            _sounds.PlayFail();
        }

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

        ResultDefects.Clear();
        foreach (TestDefect defect in run.Defects)
        {
            ResultDefects.Add(defect.Describe());
        }

        RaiseAll(nameof(ResultTimeText), nameof(ResultCableText), nameof(ResultOperatorText), nameof(HasResult));

        // Ishod se upisuje i uz netove izabranog kabla, ako je rezultat baš njegov.
        LoadNetRows();

        Outcome = run.Passed ? OutcomeState.Pass : OutcomeState.Fail;
        MeasurementCardCommand.RaiseCanExecuteChanged();
    }

    // -----------------------------------------------------------------------------------
    // Radnje
    // -----------------------------------------------------------------------------------

    private async Task PrepareTestAsync()
    {
        Cable? cable = SelectedCable;
        if (cable is null)
        {
            return;
        }

        PrepareError = null;
        PrepareMessage = null;

        await _gateway.PrepareTestAsync(cable, CancellationToken.None).ConfigureAwait(true);

        string? path = _gateway.State.LastPreparedSpecPath;

        PrepareMessage = IsDemoMode
            ? "Demo režim: .c61 fajl nije upisan na disk. " +
              "U stvarnom radu bi ovde stajala puna putanja upisanog fajla."
            : $"Upisano: {path}\n" +
              "U CableConnector-u izaberi ovaj spec, pritisni Download, pa pokreni test.";
    }

    private void ShowPrepareError(Exception ex)
        => PrepareError = "Priprema testa nije uspela. " + ex.Message;

    private void ShowMeasurementCard()
    {
        TestRun? run = _currentRun;
        if (run is null)
        {
            return;
        }

        try
        {
            Cable? cable = _currentRunCable;
            Vehicle? vehicle = cable is null ? null : _vehicles.GetById(cable.VehicleId);

            string path = MeasurementCard.WriteToFile(new MeasurementCardData
            {
                Run = run,
                Cable = cable,
                Vehicle = vehicle,
                Note = IsDemoMode ? "DEMO REŽIM — rezultat je simuliran, ovo nije stvarno merenje." : null
            });

            _documents.Open(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ShowProblem("Merna karta nije otvorena: " + ex.Message, isError: true);
        }
    }

    /// <summary>
    /// Ubacuje simulirani rezultat kroz pravi parser, kao uživo. Dostupno samo u demo režimu.
    /// </summary>
    private void Simulate(bool passed)
    {
        if (_fake is null || SelectedCable is null)
        {
            return;
        }

        DateTime now = DateTime.Now;
        string osTest = passed ? "PASS" : string.Join(';', BuildDemoDefects(SelectedCable)) + ";FAIL";

        string line = string.Join(
            ',',
            (++_demoSeq).ToString(CultureInfo.InvariantCulture),
            SelectedCable.SpecFileName,
            passed ? "pass" : "fail",
            now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
            now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty,
            OperatorName(),
            passed ? "PASS" : "FAIL",
            osTest,
            string.Empty);

        _fake.ReceiveCsvLine(line);
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
        Vehicles.Clear();
        foreach (Vehicle vehicle in _vehicles.GetAll())
        {
            Vehicles.Add(vehicle);
        }

        SelectedVehicle = Vehicles.FirstOrDefault();
    }

    private void LoadCables()
    {
        Cables.Clear();

        if (SelectedVehicle is not null)
        {
            foreach (Cable cable in _cables.GetByVehicle(SelectedVehicle.Id))
            {
                Cables.Add(cable);
            }
        }

        SelectedCable = Cables.FirstOrDefault();
    }

    /// <summary>Da li kabl odgovara tekstu iz polja za pretragu.</summary>
    private bool MatchesFilter(object item)
    {
        if (string.IsNullOrWhiteSpace(_cableFilter))
        {
            return true;
        }

        if (item is not Cable cable)
        {
            return false;
        }

        string trazeno = _cableFilter.Trim();

        return cable.Code.Contains(trazeno, StringComparison.OrdinalIgnoreCase)
               || cable.Description.Contains(trazeno, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadPorts()
    {
        Ports.Clear();

        foreach (PortUsage port in CableLayout.Ports(SelectedCable))
        {
            Ports.Add(port);
        }
    }

    private void LoadNetRows()
    {
        NetRows.Clear();

        foreach (NetRow row in CableLayout.NetRows(SelectedCable, _currentRun, _currentRunCable))
        {
            NetRows.Add(row);
        }
    }

    private void LoadNets()
    {
        Nets.Clear();

        if (SelectedCable is null)
        {
            return;
        }

        foreach (CableNet net in SelectedCable.Nets.OrderBy(n => n.Ordinal))
        {
            Nets.Add($"{net.Ordinal.ToString(CultureInfo.InvariantCulture)}. {net.Points}");
        }
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
    private void UpdateProblem(TesterGatewayState state)
    {
        if (state.LastError is not null)
        {
            ShowProblem(state.LastError, isError: true);
            return;
        }

        if (state.LastWarning is not null)
        {
            ShowProblem(state.LastWarning, isError: false);
        }
    }

    private void ShowProblem(string text, bool isError)
    {
        ProblemText = text;
        ProblemIsError = isError;
    }
}
