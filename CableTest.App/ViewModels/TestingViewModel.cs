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
    Fail,

    /// <summary>Test je pokrenut i rezultat se čeka.</summary>
    Running,

    /// <summary>Rezultat nije stigao u zadatom vremenu.</summary>
    TimedOut,

    /// <summary>Radnja nad testerom nije uspela.</summary>
    Error
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
    /// <summary>
    /// Tekst velikog prikaza dok nijedan rezultat nije stigao.
    /// </summary>
    /// <remarks>
    /// Namerno kratak: ovaj tekst se prikazuje u najkrupnijem slogu na ekranu, a duža rečenica
    /// se u toj veličini smanjivala da stane i time gubila smisao — krupan tekst uz boju postoji
    /// da bi se ishod video sa dva koraka. Šta se tačno čeka piše ispod, u podnaslovu.
    /// </remarks>
    public const string WaitingText = "REZULTAT";

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

    /// <summary>Nit na kojoj se sme dirati prikaz; gateway okida događaje sa pozadinskih niti.</summary>
    private SynchronizationContext? _ui;

    private CancellationTokenSource? _waiting;
    private TesterState _testerState = TesterState.Idle;
    private string _testerStateText = "Test nije pripremljen.";

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
        StartTestCommand = new AsyncRelayCommand(StartTestAsync, () => CanStartTest, ShowPrepareError);
        CancelWaitCommand = new RelayCommand(CancelWait, () => IsWaitingForResult);
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
    /// Grupe kablova izabranog vozila, redom po oznaci grupe.
    /// </summary>
    /// <remarks>
    /// Spisak kablova se prikazuje grupisano: operater prvo vidi „Grupa 40", pa tek kad je otvori
    /// i kablove iz nje. Grupisanje radi pogled nad <see cref="Cables"/>, a ova zbirka drži
    /// zaglavlja — ista ona koja su i ključevi grupisanja, pa se stanje „otvoreno/zatvoreno"
    /// ne gubi kad se pogled preračuna zbog pretrage.
    /// </remarks>
    public ObservableCollection<CableGroupViewModel> CableGroups { get; } = new();

    private readonly Dictionary<string, CableGroupViewModel> _cableGroups =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Mapa pinova izabranog kabla — <b>samo portovi koje kabl koristi</b>.
    /// </summary>
    /// <remarks>
    /// Tester ima šesnaest portova (A..P), a kabl retko dodiruje više od dva. Prikaz svih
    /// šesnaest značio je da operater svaki put traži ona dva među četrnaest praznih; ovako
    /// piše samo ono što se zaista priključuje.
    /// </remarks>
    public ObservableCollection<PortUsage> Ports { get; } = new();

    /// <summary>Da li izabrani kabl uopšte koristi neki port.</summary>
    public bool HasPorts => Ports.Count > 0;

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
                OpenGroupsWithMatches();
            }
        }
    }

    /// <summary>Net lista izabranog kabla, spremna za prikaz („1. O01-O02-O31-O32").</summary>
    /// <remarks>Za kabl sa ožičenjem je ovo <b>izvedena</b> lista — vidi <see cref="NetBuilder"/>.</remarks>
    public ObservableCollection<string> Nets { get; } = new();

    /// <summary>Provodnici izabranog kabla, redom sa crteža.</summary>
    public ObservableCollection<CableWire> Wires { get; } = new();

    /// <summary>Da li izabrani kabl uopšte ima uneto ožičenje.</summary>
    public bool HasWires => Wires.Count > 0;

    /// <summary>Oznaka izabranog kabla sa elektro crteža; „—" kad je nema.</summary>
    public string SelectedDesignationText => string.IsNullOrWhiteSpace(SelectedCable?.Designation)
        ? NetRow.Unknown
        : SelectedCable!.Designation;

    /// <summary>Tip izabranog kabla sa crteža.</summary>
    public string SelectedCableTypeText => string.IsNullOrWhiteSpace(SelectedCable?.CableType)
        ? NetRow.Unknown
        : SelectedCable!.CableType;

    /// <summary>Dužina izabranog kabla.</summary>
    public string SelectedLengthText => SelectedCable?.LengthText ?? NetRow.Unknown;

    /// <summary>Iz kog dokumenta i sa koje strane su podaci o kablu.</summary>
    public string SelectedSourceText => SelectedCable?.SourceText ?? NetRow.Unknown;

    /// <summary>Napomene sa crteža za izabrani kabl.</summary>
    public string SelectedNotesText => SelectedCable?.Notes ?? string.Empty;

    /// <summary>Da li izabrani kabl ima napomene.</summary>
    public bool HasSelectedNotes => !string.IsNullOrWhiteSpace(SelectedNotesText);

    /// <summary>
    /// Da li je priključna tabela izabranog kabla privremena.
    /// </summary>
    /// <remarks>
    /// Dok adapter nije napravljen, tačke testera su izabrane „na papiru". Operater to mora da
    /// vidi na ekranu: ako se kabl ispituje po rasporedu koji ne odgovara priboru na stolu,
    /// rezultat ne znači ništa.
    /// </remarks>
    public bool HasProvisionalPoints => SelectedCable?.HasProvisionalPoints ?? false;

    /// <summary>Tekst žute trake o privremenoj priključnoj tabeli.</summary>
    public string ProvisionalNoticeText =>
        "Priključna tabela je privremena — raspored tačaka nije potvrđen na adapteru.";

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
            LoadWires();
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
                nameof(SelectedEndCount),
                nameof(SelectedConnectionCount),
                nameof(SelectedPortCount),
                nameof(SelectedSpecFileText),
                nameof(SelectedDesignationText),
                nameof(SelectedCableTypeText),
                nameof(SelectedLengthText),
                nameof(SelectedSourceText),
                nameof(SelectedNotesText),
                nameof(HasSelectedNotes),
                nameof(HasWires),
                nameof(HasProvisionalPoints));
        }
    }

    public bool HasSelectedCable => SelectedCable is not null;

    /// <summary>Oznaka izabranog kabla; „—“ ako kabl nije izabran.</summary>
    public string SelectedCableCode => SelectedCable?.Code ?? NetRow.Unknown;

    /// <summary>Naziv izabranog kabla; „—“ ako ga nema.</summary>
    public string SelectedCableName => string.IsNullOrWhiteSpace(SelectedCable?.Description)
        ? NetRow.Unknown
        : SelectedCable!.Description;

    /// <summary>
    /// Broj krajeva žica — na ekranu „Broj netova".
    /// </summary>
    /// <remarks>
    /// Operater „netom" zove jedan KRAJ provodnika: žica sa dva kraja daje dva neta, pa ih je
    /// uvek dvaput više nego žica. U modelu i u .c61 fajlu <see cref="Core.Model.CableNet"/>
    /// znači grupu spojenih tačaka („A01-B01"), jednu po žici — otuda dva različita broja.
    /// Svojstva zato nose imena po tome ŠTA broje, a ne po reči „net".
    /// </remarks>
    public int SelectedEndCount => CableLayout.CountPoints(SelectedCable);

    /// <summary>
    /// Broj veza između dve tačke testera („A01-B01") — na ekranu „Broj ispitnih tačaka".
    /// </summary>
    /// <remarks>Toliko redova ima i net lista koja ide u tester, po jedan <c>OSNet:</c>.</remarks>
    public int SelectedConnectionCount => SelectedCable?.Nets.Count ?? 0;

    /// <summary>Broj portova (konektora) koje izabrani kabl koristi.</summary>
    public int SelectedPortCount => CableLayout.CountPorts(SelectedCable);

    /// <summary>Ime test programa izabranog kabla, sa ekstenzijom.</summary>
    public string SelectedSpecFileText => SelectedCable?.SpecFileNameWithExtension ?? NetRow.Unknown;

    // -----------------------------------------------------------------------------------
    // Priprema testa
    // -----------------------------------------------------------------------------------

    public AsyncRelayCommand PrepareTestCommand { get; }

    /// <summary>Pokreće test; vidljivo samo kad gateway to ume.</summary>
    public AsyncRelayCommand StartTestCommand { get; }

    /// <summary>Prekida čekanje na rezultat. Test na mašini se time ne zaustavlja.</summary>
    public RelayCommand CancelWaitCommand { get; }

    /// <summary>Da li ova implementacija ume sama da pokrene test.</summary>
    public bool CanShowStartButton => _gateway.Capabilities.CanStartTest;

    /// <summary>Uputstvo operateru kad aplikacija ne može sama da pokrene test.</summary>
    public string StartInstruction => _gateway.Capabilities.StartInstruction;

    /// <summary>Da li se uputstvo („pritisnite START na mašini") uopšte prikazuje.</summary>
    public bool ShowStartInstruction => !CanShowStartButton;

    /// <summary>Da li se dugme „Pokreni test" sme pritisnuti.</summary>
    public bool CanStartTest =>
        CanShowStartButton && TesterState == TesterState.ProgramLoaded && !IsWaitingForResult;

    /// <summary>Faza u kojoj je veza sa testerom.</summary>
    public TesterState TesterState
    {
        get => _testerState;
        private set
        {
            if (Set(ref _testerState, value))
            {
                RaiseAll(nameof(CanStartTest));
                StartTestCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Stanje veze rečeno operateru — stoji uz Panel 3.</summary>
    public string TesterStateText
    {
        get => _testerStateText;
        private set => Set(ref _testerStateText, value);
    }

    /// <summary>Da li se upravo čeka rezultat.</summary>
    public bool IsWaitingForResult => _waiting is not null;

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
                RaiseAll(
                    nameof(OutcomeText),
                    nameof(OutcomeSubText),
                    nameof(OutcomeErrorText),
                    nameof(HasResult),
                    nameof(IsWaiting),
                    nameof(IsOutcomeUnresolved));

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
        OutcomeState.Running => "TEST U TOKU",
        OutcomeState.TimedOut => "NEMA REZULTATA",
        OutcomeState.Error => "GREŠKA",
        _ => WaitingText
    };

    public string OutcomeSubText => Outcome switch
    {
        OutcomeState.Pass => "KABL JE ISPRAVAN",
        OutcomeState.Fail => "KABL NIJE ISPRAVAN",
        OutcomeState.Running => "Čeka se rezultat sa testera.",
        OutcomeState.TimedOut => "Rezultat nije stigao u zadatom vremenu. Kabl NIJE ispitan.",
        OutcomeState.Error => OutcomeErrorText,
        _ => "Pokreni test na testeru."
    };

    /// <summary>Zašto je Panel 3 u grešci; prazno kad greške nema.</summary>
    public string OutcomeErrorText { get; private set; } = "Veza sa testerom je javila grešku.";

    public bool IsWaiting => Outcome == OutcomeState.Waiting;

    /// <summary>Da li Panel 3 pokazuje nešto što nije ishod merenja (čekanje, istek, greška).</summary>
    public bool IsOutcomeUnresolved => Outcome is OutcomeState.Running or OutcomeState.TimedOut or OutcomeState.Error;

    /// <summary>Da li je prikazan neki rezultat — od toga zavisi dugme „Merna karta".</summary>
    public bool HasResult => _currentRun is not null;

    public string ResultTimeText { get; private set; } = string.Empty;

    public string ResultCableText { get; private set; } = string.Empty;

    public string ResultOperatorText { get; private set; } = string.Empty;

    /// <summary>Greške poslednjeg rezultata, u obliku razumljivom operateru.</summary>
    public ObservableCollection<string> ResultDefects { get; } = new();

    /// <summary>
    /// Mreže koje nisu prošle, sa vrstom greške na svakoj.
    /// </summary>
    /// <remarks>
    /// „PAO" sam po sebi operateru ne kaže šta da radi. Ovde stoji koja mreža nije prošla i šta
    /// joj je — kratak spoj ili prekid, i između kojih tačaka.
    /// </remarks>
    public ObservableCollection<NetRow> FailedNetRows { get; } = new();

    /// <summary>Da li ima mreža koje nisu prošle.</summary>
    public bool HasFailedNets => FailedNetRows.Count > 0;

    /// <summary>Kratak spisak mreža koje nisu prošle, npr. „Nisu prošle mreže: 3, 7".</summary>
    public string FailedNetsSummary => FailedNetRows.Count == 0
        ? string.Empty
        : "Nisu prošle mreže: " + string.Join(
            ", ",
            FailedNetRows.Select(r => r.Ordinal.ToString(CultureInfo.InvariantCulture)));

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
        ICollectionView pogledKablova = CollectionViewSource.GetDefaultView(Cables);
        pogledKablova.Filter = MatchesFilter;

        // Grupisanje po oznaci grupe iz šifre kabla („40-W1.1" → „40"). Ključ grupe nije niska
        // nego zaglavlje iz CableGroups, da otvorena grupa ostane otvorena i posle preračunavanja.
        pogledKablova.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(Cable.Group), new CableGroupKeyConverter(GroupFor)));

        // Mapa pinova ima svih 16 portova i kad kabl nije izabran — prazan okvir bi izgledao
        // kao da ekran nije do kraja učitan.
        LoadPorts();
        LoadNetRows();

        LoadVehicles();
        CheckPaths();

        // Gateway okida događaje sa pozadinskih niti i ne zna da GUI postoji; ovde se hvata nit
        // na kojoj se prikaz sme dirati, i na nju se svaki događaj prebacuje.
        _ui = SynchronizationContext.Current;

        _gateway.ResultReceived += OnResultReceived;
        _gateway.StateChanged += OnStateChanged;
        _gateway.GatewayError += OnGatewayError;
        _gateway.StartMonitoring();

        RefreshState();
    }

    /// <summary>Izvršava posao na GUI niti, bez obzira na to sa koje niti je događaj stigao.</summary>
    private void OnUiThread(Action work)
    {
        SynchronizationContext? ui = _ui;

        if (ui is null || ReferenceEquals(SynchronizationContext.Current, ui))
        {
            // Testovi i konzola nemaju GUI nit; tada se radi odmah, pa je redosled očigledan.
            work();
            return;
        }

        ui.Post(_ => work(), null);
    }

    /// <summary>Osvežava traku stanja. Prikaz zove periodično, jer se stanje menja i bez događaja.</summary>
    public void RefreshState()
    {
        TesterGatewayState state = _gateway.Diagnostics;

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

        _waiting?.Cancel();
        _waiting?.Dispose();
        _waiting = null;

        _gateway.ResultReceived -= OnResultReceived;
        _gateway.StateChanged -= OnStateChanged;
        _gateway.GatewayError -= OnGatewayError;
        _gateway.StopMonitoring();
    }

    // -----------------------------------------------------------------------------------
    // Dolazak rezultata
    // -----------------------------------------------------------------------------------

    private void OnResultReceived(object? sender, TestRunReceivedEventArgs e) => OnUiThread(() => HandleResult(e));

    private void OnStateChanged(object? sender, TesterStateChangedEventArgs e) => OnUiThread(() =>
    {
        TesterState = e.Current;
        TesterStateText = DescribeTesterState(e.Current, e.Reason);
    });

    /// <summary>
    /// Greška iz gateway-a: operater je vidi, a aplikacija nastavlja da radi.
    /// </summary>
    private void OnGatewayError(object? sender, GatewayErrorEventArgs e) => OnUiThread(() =>
    {
        ShowProblem(e.Message, !e.IsWarning);

        if (!e.IsWarning && IsWaitingForResult)
        {
            ShowError(e.Message);
        }
    });

    private static string DescribeTesterState(TesterState state, string reason) => state switch
    {
        Core.Gateway.TesterState.Idle => "Test nije pripremljen.",
        Core.Gateway.TesterState.ProgramLoaded => "Test program je pripremljen.",
        Core.Gateway.TesterState.WaitingForResult => "Čeka se rezultat.",
        Core.Gateway.TesterState.Completed => "Rezultat je stigao.",
        Core.Gateway.TesterState.Failed => string.IsNullOrWhiteSpace(reason) ? "Greška." : "Greška: " + reason,
        _ => reason
    };

    private void HandleResult(TestRunReceivedEventArgs e)
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

        if (e.IsDuplicate)
        {
            // Gateway ne odbacuje duplikat, nego ga označava; operater mora da zna da je isti
            // sadržaj već viđen u drugom fajlu.
            ShowProblem(
                $"Isti rezultat je već viđen u fajlu \"{e.DuplicateOfPath}\". Proveri da li je test zaista ponovljen.",
                isError: false);
        }

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

        // Greške se ispisuju jezikom crteža: terminal, boja žice, pa tačka testera u zagradi.
        ResultDefects.Clear();
        foreach (string defect in DefectTranslator.DescribeAll(run, cable))
        {
            ResultDefects.Add(defect);
        }

        RaiseAll(nameof(ResultTimeText), nameof(ResultCableText), nameof(ResultOperatorText), nameof(HasResult));

        // Ishod se upisuje i uz netove izabranog kabla, ako je rezultat baš njegov.
        LoadNetRows();
        LoadFailedNetRows();

        Outcome = run.Passed ? OutcomeState.Pass : OutcomeState.Fail;
        MeasurementCardCommand.RaiseCanExecuteChanged();
    }

    // -----------------------------------------------------------------------------------
    // Radnje
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Priprema test program i, kad aplikacija ne može sama da pokrene test, odmah počinje da
    /// čeka rezultat.
    /// </summary>
    /// <remarks>
    /// Kod rada preko fajlova START pritiska operater na mašini, pa se čekanje pokreće odmah po
    /// pripremi — inače bi Panel 3 ćutao dok se ne desi nešto, a operater ne bi znao ni da li se
    /// išta čeka ni dokle se čeka.
    /// </remarks>
    private async Task PrepareTestAsync()
    {
        Cable? cable = SelectedCable;
        if (cable is null)
        {
            return;
        }

        PrepareError = null;
        PrepareMessage = null;

        GatewayResult result = await _gateway.LoadProgramAsync(cable, CancellationToken.None).ConfigureAwait(true);

        if (!result.IsOk)
        {
            PrepareError = result.Message;
            ShowError(result.Message);
            return;
        }

        string? path = _gateway.Diagnostics.LastPreparedSpecPath;

        PrepareMessage = IsDemoMode
            ? "Demo režim: .c61 fajl nije upisan na disk. " +
              "U stvarnom radu bi ovde stajala puna putanja upisanog fajla."
            : $"Upisano: {path}\n" + StartInstruction;

        if (!CanShowStartButton)
        {
            // Test pokreće operater na mašini; od ovog trenutka se čeka rezultat.
            Outcome = OutcomeState.Running;
            await WaitForResultAsync().ConfigureAwait(true);
        }
    }

    /// <summary>Pokreće test kad gateway to ume, pa čeka rezultat.</summary>
    private async Task StartTestAsync()
    {
        // Prikaz se pomera pre pokretanja: rezultat ume da stigne pre nego što se poziv vrati, a
        // „test u toku" ne sme da prebriše ishod koji je već prikazan.
        Outcome = OutcomeState.Running;

        GatewayResult started = await _gateway.StartTestAsync(CancellationToken.None).ConfigureAwait(true);

        if (!started.IsOk)
        {
            // NotSupported nije greška u programu nego granica implementacije — zato poruka, a
            // ne izuzetak. Vidi ITesterGateway.StartTestAsync.
            PrepareError = started.Message;
            ShowError(started.Message);
            return;
        }

        await WaitForResultAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Čeka rezultat i vodi Panel 3 kroz čekanje, istek vremena i grešku.
    /// </summary>
    private async Task WaitForResultAsync()
    {
        CancelWait();

        var cts = new CancellationTokenSource();
        _waiting = cts;

        // Ovde se prikaz namerno ne dira: „test u toku" postavlja onaj ko test pokreće, pre nego
        // što rezultat uopšte može da stigne.
        RaiseAll(nameof(IsWaitingForResult), nameof(CanStartTest));
        CancelWaitCommand.RaiseCanExecuteChanged();
        StartTestCommand.RaiseCanExecuteChanged();

        try
        {
            GatewayResult result = await _gateway
                .WaitForResultAsync(_settings.ResultTimeout, cts.Token)
                .ConfigureAwait(true);

            switch (result.Status)
            {
                case GatewayStatus.Ok:
                    // Sam rezultat je već stigao kroz ResultReceived i tamo je i prikazan.
                    break;

                case GatewayStatus.Timeout:
                    Outcome = OutcomeState.TimedOut;
                    ShowProblem(result.Message, isError: true);
                    break;

                case GatewayStatus.Cancelled:
                    // Operater je odustao od čekanja; ekran se vraća na početak, bez ishoda.
                    if (Outcome == OutcomeState.Running)
                    {
                        Outcome = OutcomeState.Waiting;
                    }

                    break;

                default:
                    ShowError(result.Message);
                    break;
            }
        }
        finally
        {
            _waiting = null;
            cts.Dispose();

            RaiseAll(nameof(IsWaitingForResult), nameof(CanStartTest));
            CancelWaitCommand.RaiseCanExecuteChanged();
            StartTestCommand.RaiseCanExecuteChanged();
        }
    }

    private void CancelWait()
    {
        CancellationTokenSource? waiting = _waiting;

        if (waiting is null)
        {
            return;
        }

        // Prekida se samo čekanje u aplikaciji; mašina i dalje radi svoje.
        waiting.Cancel();
    }

    /// <summary>Panel 3 prelazi u grešku, a poruka ide i u traku problema.</summary>
    private void ShowError(string message)
    {
        OutcomeErrorText = message;
        Outcome = OutcomeState.Error;
        ShowProblem(message, isError: true);
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

    /// <summary>
    /// Greške za demo — prave oznake tačaka iz <b>izvedene</b> net liste izabranog kabla.
    /// </summary>
    /// <remarks>
    /// Greške moraju da liče na stvarne, inače demo ne vredi ništa: prekid je unutar jednog neta
    /// (žica koja je pukla), a kratak spoj je između dva različita neta (dve žice koje se dodiruju).
    /// Obrnuto — „kratak spoj" između dve tačke istog neta — tester nikad ne bi prijavio, jer te
    /// tačke i treba da budu spojene.
    /// </remarks>
    private static IReadOnlyList<string> BuildDemoDefects(Cable cable)
    {
        string[][] nets = cable.Nets
            .OrderBy(n => n.Ordinal)
            .Select(n => n.Points.Split(
                Net.Separator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(p => p.Length >= 2)
            .ToArray();

        if (nets.Length == 0)
        {
            return new[] { "SHORT O01-O02" };
        }

        var messages = new List<string> { $"OPEN {nets[0][0]}-{nets[0][^1]}" };

        if (nets.Length >= 2)
        {
            messages.Add($"SHORT {nets[0][0]}-{nets[1][0]}");
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

        LoadCableGroups();

        SelectedCable = Cables.FirstOrDefault();
    }

    /// <summary>
    /// Pravi zaglavlja grupa za kablove izabranog vozila.
    /// </summary>
    /// <remarks>
    /// Grupe kreću <b>zatvorene</b>, i onda kad je kabl iz njih već izabran. Izbor kabla i
    /// otvorenost grupe su dve različite stvari: prvi je podatak sa kojim se radi, druga je samo
    /// to koliko se spiska vidi. Kad bi izbor sam otvarao grupu, spisak bi se pri svakoj promeni
    /// vozila zatekao otvoren, a upravo se tražilo suprotno.
    /// </remarks>
    private void LoadCableGroups()
    {
        CableGroups.Clear();

        foreach (IGrouping<string, Cable> grupa in Cables
                     .GroupBy(c => c.Group, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            CableGroupViewModel zaglavlje = GroupFor(grupa.Key);
            zaglavlje.Count = grupa.Count();
            zaglavlje.IsExpanded = false;

            CableGroups.Add(zaglavlje);
        }
    }

    /// <summary>Zaglavlje jedne grupe; pravi se pri prvom traženju i posle se samo nalazi.</summary>
    /// <remarks>
    /// Isti primerak po oznaci grupe — zato što je taj primerak ključ grupisanja u pogledu, pa
    /// dva objekta za istu grupu značila bi i dva zaglavlja u spisku.
    /// </remarks>
    private CableGroupViewModel GroupFor(string name)
    {
        if (!_cableGroups.TryGetValue(name, out CableGroupViewModel? group))
        {
            group = new CableGroupViewModel(name);
            _cableGroups[name] = group;
        }

        return group;
    }

    /// <summary>
    /// Otvara grupe u kojima pretraga ima pogodaka, a ostale zatvara.
    /// </summary>
    /// <remarks>
    /// Bez ovoga bi pretraga izgledala kao da ništa ne nalazi: pogodak bi bio u grupi koja je
    /// zatvorena, pa se ne bi video. Kad se pretraga obriše, sve se vraća na zatvoreno.
    /// </remarks>
    private void OpenGroupsWithMatches()
    {
        bool trazi = !string.IsNullOrWhiteSpace(_cableFilter);

        foreach (CableGroupViewModel grupa in CableGroups)
        {
            grupa.IsExpanded = trazi && Cables.Any(
                c => string.Equals(c.Group, grupa.Name, StringComparison.OrdinalIgnoreCase)
                     && MatchesFilter(c));
        }
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

        foreach (PortUsage port in CableLayout.Ports(SelectedCable).Where(p => p.Used))
        {
            Ports.Add(port);
        }

        Raise(nameof(HasPorts));
    }

    private void LoadNetRows()
    {
        NetRows.Clear();

        foreach (NetRow row in CableLayout.NetRows(SelectedCable, _currentRun, _currentRunCable))
        {
            NetRows.Add(row);
        }
    }

    /// <summary>Iz tabele netova izdvaja one koji nisu prošli — to je ono što se gleda kod FAIL.</summary>
    private void LoadFailedNetRows()
    {
        FailedNetRows.Clear();

        foreach (NetRow row in NetRows.Where(r => r.IsFailed))
        {
            FailedNetRows.Add(row);
        }

        RaiseAll(nameof(HasFailedNets), nameof(FailedNetsSummary));
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

    private void LoadWires()
    {
        Wires.Clear();

        if (SelectedCable is null)
        {
            return;
        }

        foreach (CableWire wire in SelectedCable.Wires.OrderBy(w => w.WireNo))
        {
            Wires.Add(wire);
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
