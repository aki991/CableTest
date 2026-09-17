using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CableTest.App.Mvvm;
using CableTest.App.Services;
using CableTest.App.UiAutomation;

namespace CableTest.App.ViewModels;

/// <summary>Jedan čvor UIA drveta u prikazu.</summary>
public sealed class UiaNodeViewModel : ObservableObject
{
    private bool _isExpanded;
    private bool _isMatch;
    private bool _isSelected;

    public UiaNodeViewModel(UiaTreeNodeInfo node, CableConnectorViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(owner);

        Source = node;
        Element = node.Element;
        TruncatedByDepth = node.TruncatedByDepth;

        foreach (UiaTreeNodeInfo child in node.Children)
        {
            var childNode = new UiaNodeViewModel(child, owner) { Parent = this };
            Children.Add(childNode);
            VisibleChildren.Add(childNode);
        }

        CopyNodeCommand = new RelayCommand(() => owner.CopyNodeData(this));
        CopyBranchCommand = new RelayCommand(() => owner.CopyNodeBranch(this));

        // Prva dva nivoa su otvorena: korenski prozor i ono što je odmah u njemu.
        _isExpanded = node.Depth < 2;
    }

    /// <summary>Snimak čvora iz koga je prikaz napravljen.</summary>
    public UiaTreeNodeInfo Source { get; }

    /// <summary>Roditelj u drvetu, ili <c>null</c> za koren.</summary>
    public UiaNodeViewModel? Parent { get; private init; }

    /// <summary>Podaci o elementu.</summary>
    public UiaElementInfo Element { get; }

    /// <summary>Sva deca čvora, bez obzira na pretragu.</summary>
    public ObservableCollection<UiaNodeViewModel> Children { get; } = new();

    /// <summary>Deca koja se posle pretrage zaista prikazuju.</summary>
    public ObservableCollection<UiaNodeViewModel> VisibleChildren { get; } = new();

    /// <summary>Da li je obilazak ovde stao zbog granice dubine.</summary>
    public bool TruncatedByDepth { get; }

    /// <summary>Kopira sve podatke o čvoru u ostavu.</summary>
    public RelayCommand CopyNodeCommand { get; }

    /// <summary>Kopira čvor sa svom decom u ostavu.</summary>
    public RelayCommand CopyBranchCommand { get; }

    /// <summary>Da li je čvor rasklopljen.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    /// <summary>Da li se čvor poklapa sa traženim u pretrazi.</summary>
    public bool IsMatch
    {
        get => _isMatch;
        private set => Set(ref _isMatch, value);
    }

    /// <summary>
    /// Da li je čvor izabran u drvetu.
    /// </summary>
    /// <remarks>
    /// Veza ide u oba smera: prikaz javlja šta je operater kliknuo, a strana ovim označava čvor
    /// koji je uhvaćen tasterom F8.
    /// </remarks>
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>Prvi red: tip kontrole i ime.</summary>
    public string Header => $"{Element.ControlType} — {Element.DisplayName}";

    /// <summary>Drugi red: sve ostalo što se o čvoru prikazuje.</summary>
    public string Details => string.Create(
        CultureInfo.InvariantCulture,
        $"AutomationId=\"{Element.AutomationId}\"  ClassName=\"{Element.ClassName}\"  " +
        $"FrameworkId=\"{Element.FrameworkId}\"  HWND={Element.NativeWindowHandleText}  " +
        $"IsEnabled={Element.IsEnabled}  IsOffscreen={Element.IsOffscreen}  {Element.BoundsText}");

    /// <summary>Treći red: podržani obrasci.</summary>
    public string PatternsText => $"Obrasci: {Element.PatternsText}"
                                  + (TruncatedByDepth ? "   •   ima još dece ispod granice dubine" : string.Empty);

    /// <summary>
    /// Primenjuje ishod pretrage na čvor i svu decu ispod njega.
    /// </summary>
    /// <remarks>
    /// Čvor u kome ima pogotka se otvara: pogodak zakopan u sklopljenoj grani ne bi se video.
    /// Vraća da li u grani ima ijednog pogotka.
    /// </remarks>
    public bool ApplyFilter(UiaTreeFilterResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        IsMatch = result.Matched.Contains(Element.Id);
        bool anyMatch = IsMatch;

        var wanted = new List<UiaNodeViewModel>(Children.Count);

        foreach (UiaNodeViewModel child in Children)
        {
            anyMatch |= child.ApplyFilter(result);

            if (result.Visible.Contains(child.Element.Id))
            {
                wanted.Add(child);
            }
        }

        // Spisak se dira samo kad se zaista promenio. Inače bi svaki pritisak na taster iznova
        // pravio redove drveta i rasuo i izbor i rasklopljene grane.
        if (!VisibleChildren.SequenceEqual(wanted))
        {
            VisibleChildren.Clear();

            foreach (UiaNodeViewModel child in wanted)
            {
                VisibleChildren.Add(child);
            }
        }

        if (anyMatch)
        {
            IsExpanded = true;
        }

        return anyMatch;
    }

    /// <summary>
    /// Traži čvor sa zadatim RuntimeId-em i otvara sve grane iznad njega.
    /// </summary>
    /// <remarks>
    /// Grane se otvaraju pri povratku iz rekurzije, tako da se otvori tačno put do pogotka, a ne
    /// celo drvo. Vraća pronađeni čvor, ili <c>null</c> ako ga u ovom drvetu nema.
    /// </remarks>
    public UiaNodeViewModel? RevealByRuntimeId(string? runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
        {
            return null;
        }

        if (string.Equals(Element.RuntimeId, runtimeId, StringComparison.Ordinal))
        {
            return this;
        }

        foreach (UiaNodeViewModel child in Children)
        {
            UiaNodeViewModel? hit = child.RevealByRuntimeId(runtimeId);

            if (hit is not null)
            {
                IsExpanded = true;
                return hit;
            }
        }

        return null;
    }

    /// <summary>Da li se čvor posle poslednje pretrage uopšte vidi u drvetu.</summary>
    public bool IsShown()
    {
        for (UiaNodeViewModel node = this; node.Parent is not null; node = node.Parent)
        {
            if (!node.Parent.VisibleChildren.Contains(node))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Rasklapa ili sklapa čvor i svu decu ispod njega.</summary>
    public void SetExpanded(bool expanded)
    {
        IsExpanded = expanded;

        foreach (UiaNodeViewModel child in Children)
        {
            child.SetExpanded(expanded);
        }
    }
}

/// <summary>
/// Jedan nivo u lancu roditelja: prozor je prvi, izabrani čvor poslednji.
/// </summary>
/// <remarks>
/// Uvlačenje ide razmacima, a ne marginom: ceo panel je jednoprostoran, pa se nivoi poravnavaju
/// isto kao u logu i u tekstu koji se kopira.
/// </remarks>
public sealed class UiaChainItemViewModel
{
    public UiaChainItemViewModel(int level, UiaElementInfo element)
    {
        ArgumentNullException.ThrowIfNull(element);

        Level = level;
        Element = element;
    }

    /// <summary>Koji je ovo nivo; prozor je 0.</summary>
    public int Level { get; }

    /// <summary>Podaci o elementu na tom nivou.</summary>
    public UiaElementInfo Element { get; }

    /// <summary>Prvi red: tip i ime, uvučeno po nivou.</summary>
    public string Header => new string(' ', Level * 2) + UiaTreeText.Head(Element);

    /// <summary>Drugi red: ostali podaci, uvučeni pod prvi.</summary>
    public string Details => new string(' ', (Level * 2) + 2) + UiaTreeText.Data(Element);
}

/// <summary>Jedna kontrola sa desne strane — dugme, stavka menija, kartica ili polje.</summary>
public sealed class UiaActionViewModel : ObservableObject
{
    private string _value = string.Empty;
    private string _status = "—";
    private UiaSearchReport? _discovery;

    public UiaActionViewModel(UiaControlTarget target, CableConnectorViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(owner);

        Target = target;

        RunCommand = new AsyncRelayCommand(
            () => owner.RunTargetAsync(this),
            () => owner.CanAct,
            owner.ReportCommandError);

        ReadCommand = new AsyncRelayCommand(
            () => owner.ReadTargetAsync(this),
            () => owner.CanRead,
            owner.ReportCommandError);

        WriteCommand = new AsyncRelayCommand(
            () => owner.WriteTargetAsync(this),
            () => owner.CanAct,
            owner.ReportCommandError);
    }

    /// <summary>Koja se kontrola traži.</summary>
    public UiaControlTarget Target { get; }

    /// <summary>Natpis, onakav kakav stoji u CableConnector-u.</summary>
    public string Name => Target.Name;

    /// <summary>Da li se kontrola i čita i upisuje — tada dobija polje za vrednost.</summary>
    public bool IsField => Target.Kind == UiaControlKind.Field;

    /// <summary>Da li pokretanje traži potvrdu.</summary>
    public bool NeedsConfirmation => Target.NeedsConfirmation;

    /// <summary>Vrednost za upis, odnosno poslednja pročitana.</summary>
    public string Value
    {
        get => _value;
        set => Set(ref _value, value);
    }

    /// <summary>Ishod poslednjeg pokušaja nad ovom kontrolom, u kratkom obliku.</summary>
    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    /// <summary>Pokreće kontrolu.</summary>
    public AsyncRelayCommand RunCommand { get; }

    /// <summary>Čita vrednost polja.</summary>
    public AsyncRelayCommand ReadCommand { get; }

    /// <summary>Upisuje vrednost u polje.</summary>
    public AsyncRelayCommand WriteCommand { get; }

    /// <summary>Da li je „Otkrij sve“ već tražilo ovu kontrolu.</summary>
    public bool HasDiscovery => _discovery is not null;

    /// <summary>Da li je „Otkrij sve“ pronašlo ovu kontrolu.</summary>
    public bool DiscoveryFound => _discovery?.Found ?? false;

    /// <summary>Prvi red ishoda otkrivanja: pronađeno i čime, ili nije pronađeno.</summary>
    public string DiscoveryHeader => _discovery is null
        ? string.Empty
        : _discovery.Found
            ? $"pronađeno — strategija: {_discovery.Strategy}"
            : "nije pronađeno";

    /// <summary>Sve što je o pronađenoj kontroli otkriveno, red po red.</summary>
    public string DiscoveryDetails
    {
        get
        {
            if (_discovery?.Element is not UiaElementInfo element)
            {
                return string.Empty;
            }

            return string.Create(
                CultureInfo.InvariantCulture,
                $"ControlType={element.ControlType}   AutomationId=\"{element.AutomationId}\"\n" +
                $"Name=\"{element.Name}\"   ClassName=\"{element.ClassName}\"\n" +
                $"FrameworkId=\"{element.FrameworkId}\"   IsEnabled={element.IsEnabled}\n" +
                $"Obrasci: {element.PatternsText}");
        }
    }

    /// <summary>Da li se ispisuju najbliža imena — samo kad kontrola nije pronađena.</summary>
    public bool HasNearest => HasDiscovery && !DiscoveryFound;

    /// <summary>Najbliža viđena imena — objašnjenje zašto kontrola nije pronađena.</summary>
    public string DiscoveryNearest
    {
        get
        {
            if (_discovery is null || _discovery.Found)
            {
                return string.Empty;
            }

            return _discovery.Nearest.Count == 0
                ? "u prozoru nije viđeno nijedno ime"
                : $"najbliža imena: {string.Join(" | ", _discovery.Nearest)}";
        }
    }

    /// <summary>Upisuje ishod pored same kontrole, da se vidi bez čitanja loga.</summary>
    public void SetStatus(string text) => Status = text;

    /// <summary>Prima ishod jedne pretrage iz prolaza „Otkrij sve“.</summary>
    public void ApplyDiscovery(UiaSearchReport? report)
    {
        _discovery = report;

        RaiseAll(
            nameof(HasDiscovery),
            nameof(DiscoveryFound),
            nameof(DiscoveryHeader),
            nameof(DiscoveryDetails),
            nameof(HasNearest),
            nameof(DiscoveryNearest));
    }

    /// <summary>Javlja prikazu da se dostupnost komandi promenila.</summary>
    public void RefreshCommands()
    {
        RunCommand.RaiseCanExecuteChanged();
        ReadCommand.RaiseCanExecuteChanged();
        WriteCommand.RaiseCanExecuteChanged();
    }
}

/// <summary>Skupina kontrola u desnom panelu.</summary>
public sealed class UiaActionGroupViewModel
{
    public UiaActionGroupViewModel(UiaControlGroup group, CableConnectorViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(owner);

        Title = group.Title;

        foreach (UiaControlTarget target in group.Targets)
        {
            Actions.Add(new UiaActionViewModel(target, owner));
        }
    }

    /// <summary>Naslov skupine.</summary>
    public string Title { get; }

    /// <summary>Kontrole u skupini.</summary>
    public ObservableCollection<UiaActionViewModel> Actions { get; } = new();
}

/// <summary>
/// Razvojna strana „CableConnector" — ispitivanje šta je od Microtest CableConnector-a
/// dohvatljivo preko Windows UI Automation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ovo je laboratorija, ne pogon.</b> Strana ne otvara COM port, ne razgovara sa testerom i
/// nije povezana ni sa jednim <c>ITesterGateway</c>-em. Jedini joj je posao da pronađe kontrole
/// tuđeg programa, zapiše čime su pronađene i šta podržavaju, i da to izveze u JSON — tek kasnije
/// će se po tom zapisu praviti pravi gateway.
/// </para>
/// <para>
/// Zato je „Samo čitanje" podrazumevano uključeno, fizički klik je iza posebnog prekidača, a
/// START i Download traže potvrdu i kad je sve odblokirano: iza ta dva dugmeta stoji stvarni
/// hardver sa visokim naponom.
/// </para>
/// <para>
/// Sav rad sa UIA ide kroz <see cref="UiaWorker"/> — zasebnu nit sa rokom po pozivu. Ako
/// CableConnector nije pokrenut ili ne odgovara, strana to napiše i nastavi da radi; ništa se ne
/// baca dalje.
/// </para>
/// </remarks>
public sealed class CableConnectorViewModel : ObservableObject, IDisposable
{
    /// <summary>Podrazumevana granica dubine obilaska drveta.</summary>
    public const int DefaultDepth = 6;

    /// <summary>Najveća granica dubine koja se može zadati; veći broj se svodi na nju.</summary>
    public const int DepthLimit = 20;

    /// <summary>Ime fajla u koji se izvozi mapa.</summary>
    public const string MapFileName = "cableconnector-map.json";

    private readonly IConfirmation _confirmation;
    private readonly UiaControlMap _map = new();
    private readonly CableConnectorProbe _probe = new();
    private readonly StringBuilder _log = new();

    private UiaWorker _worker = new();

    private UiaWindowInfo _main = UiaWindowInfo.NotFound(UiaWindowKind.Main, "još nije traženo");
    private UiaWindowInfo _edit = UiaWindowInfo.NotFound(UiaWindowKind.Edit, "još nije traženo");

    private UiaWindowKind _selectedWindow = UiaWindowKind.Main;
    private UiaNodeViewModel? _selectedNode;
    private string _nodeValue = string.Empty;
    private string _versionText = "—";
    private bool _versionOk = true;
    private bool _connected;
    private bool _busy;
    private bool _readOnly = true;
    private bool _allowPhysicalClick;
    private int _maxDepth = DefaultDepth;
    private bool _unlimitedDepth;
    private int _timeoutSeconds = 5;
    private bool _disposed;

    // Drvo: snimak iz koga se prikaz pravi, koren prikaza i ono što se u njemu traži.
    private UiaTreeNodeInfo? _treeRoot;
    private UiaNodeViewModel? _rootNode;
    private UiaViewKind _selectedView = UiaViewKind.Raw;
    private string _chainTitle = "Lanac roditelja";
    private string _searchText = string.Empty;
    private string _selectedControlType = UiaTreeSearch.AllControlTypes;
    private string _filterModeName = UiaTreeSearch.HighlightModeName;
    private string _treeCountText = UiaTreeSearch.CountText(0, 0);

    // „Izvezi mapu“ se otvara tek kad „Otkrij sve“ prođe kroz ceo katalog.
    private bool _discoveryDone;

    public CableConnectorViewModel(IConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        _confirmation = confirmation;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsBusy, ReportCommandError);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy, ReportCommandError);
        DiscoverAllCommand = new AsyncRelayCommand(DiscoverAllAsync, () => IsConnected && !IsBusy, ReportCommandError);
        ExportMapCommand = new RelayCommand(ExportMap, () => _discoveryDone && _map.Count > 0);
        CopyLogCommand = new RelayCommand(CopyLog);
        ClearLogCommand = new RelayCommand(ClearLog);

        ExpandAllCommand = new RelayCommand(() => SetAllExpanded(true));
        CollapseAllCommand = new RelayCommand(() => SetAllExpanded(false));

        // Hvatanje elementa je čitanje i ništa drugo — zato ne pita ni za „Samo čitanje“ ni za to
        // da li su prozori CableConnector-a uopšte pronađeni.
        CaptureElementCommand = new AsyncRelayCommand(CaptureElementAsync, () => !IsBusy, ReportCommandError);

        InvokeNodeCommand = new AsyncRelayCommand(InvokeNodeAsync, () => CanAct && SelectedNode is not null, ReportCommandError);
        PhysicalClickNodeCommand = new AsyncRelayCommand(
            PhysicalClickNodeAsync,
            () => CanAct && AllowPhysicalClick && SelectedNode is not null,
            ReportCommandError);
        ReadNodeValueCommand = new AsyncRelayCommand(ReadNodeValueAsync, () => CanRead && SelectedNode is not null, ReportCommandError);
        WriteNodeValueCommand = new AsyncRelayCommand(WriteNodeValueAsync, () => CanAct && SelectedNode is not null, ReportCommandError);

        foreach (UiaControlGroup group in CableConnectorControls.Groups)
        {
            ActionGroups.Add(new UiaActionGroupViewModel(group, this));
        }

        Append("Strana je otvorena. Ništa još nije traženo — pritisnite „Poveži“.");
        Append($"Očekivana verzija CableConnector-a: {CableConnectorVersion.Expected}.");
    }

    // -----------------------------------------------------------------------------------
    // Gornja traka — stanje oba prozora
    // -----------------------------------------------------------------------------------

    /// <summary>Stanje glavnog prozora.</summary>
    public UiaWindowInfo Main
    {
        get => _main;
        private set
        {
            if (Set(ref _main, value))
            {
                RaiseAll(
                    nameof(MainStatusText),
                    nameof(MainPidText),
                    nameof(MainTitleText),
                    nameof(MainClassText),
                    nameof(MainFrameworkText),
                    nameof(MainNote));
            }
        }
    }

    /// <summary>Stanje prozorčeta „Edit".</summary>
    public UiaWindowInfo Edit
    {
        get => _edit;
        private set
        {
            if (Set(ref _edit, value))
            {
                RaiseAll(
                    nameof(EditStatusText),
                    nameof(EditPidText),
                    nameof(EditTitleText),
                    nameof(EditClassText),
                    nameof(EditFrameworkText),
                    nameof(EditNote));
            }
        }
    }

    public string MainStatusText => Main.Found ? "pronađen" : "nije pronađen";

    public string MainPidText => Main.Found ? Main.ProcessId.ToString(CultureInfo.InvariantCulture) : "—";

    public string MainTitleText => Main.Title;

    public string MainClassText => Main.ClassName;

    public string MainFrameworkText => Main.FrameworkId;

    public string MainNote => Main.Note;

    public string EditStatusText => Edit.Found ? "pronađen" : "nije pronađen";

    public string EditPidText => Edit.Found ? Edit.ProcessId.ToString(CultureInfo.InvariantCulture) : "—";

    public string EditTitleText => Edit.Title;

    public string EditClassText => Edit.ClassName;

    public string EditFrameworkText => Edit.FrameworkId;

    public string EditNote => Edit.Note;

    /// <summary>Verzija pročitana iz naslova glavnog prozora.</summary>
    public string VersionText
    {
        get => _versionText;
        private set => Set(ref _versionText, value);
    }

    /// <summary>Da li je verzija ona na kojoj je strana rađena.</summary>
    public bool VersionIsExpected
    {
        get => _versionOk;
        private set
        {
            if (Set(ref _versionOk, value))
            {
                Raise(nameof(VersionWarningText));
            }
        }
    }

    /// <summary>Upozorenje kad verzija nije očekivana.</summary>
    public string VersionWarningText =>
        $"Očekuje se {CableConnectorVersion.Expected}; natpisi dugmadi i raspored kartica mogu biti drugačiji.";

    // -----------------------------------------------------------------------------------
    // Stanje rada
    // -----------------------------------------------------------------------------------

    /// <summary>Da li je bar jedan prozor pronađen.</summary>
    public bool IsConnected
    {
        get => _connected;
        private set
        {
            if (Set(ref _connected, value))
            {
                RefreshCommands();
            }
        }
    }

    /// <summary>Da li je neki poziv u toku.</summary>
    public bool IsBusy
    {
        get => _busy;
        private set
        {
            if (Set(ref _busy, value))
            {
                RefreshCommands();
            }
        }
    }

    /// <summary>
    /// „Samo čitanje" — dok je uključeno, radi se isključivo obilazak drveta i čitanje vrednosti.
    /// </summary>
    public bool ReadOnlyMode
    {
        get => _readOnly;
        set
        {
            if (Set(ref _readOnly, value))
            {
                Append(value
                    ? "„Samo čitanje“ je UKLJUČENO — nijedna radnja nad kontrolama nije moguća."
                    : "„Samo čitanje“ je ISKLJUČENO — radnje nad kontrolama su moguće.");
                RefreshCommands();
            }
        }
    }

    /// <summary>„Dozvoli fizički klik" — poslednja mogućnost kad nijedan obrazac ne radi.</summary>
    public bool AllowPhysicalClick
    {
        get => _allowPhysicalClick;
        set
        {
            if (Set(ref _allowPhysicalClick, value))
            {
                Append(value
                    ? "„Dozvoli fizički klik“ je UKLJUČENO — klik mišem se koristi samo ako Invoke i Legacy ne prođu."
                    : "„Dozvoli fizički klik“ je ISKLJUČENO.");
                RefreshCommands();
            }
        }
    }

    /// <summary>Granica dubine obilaska drveta; važi dok <see cref="UnlimitedDepth"/> nije uključeno.</summary>
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            int clamped = Math.Clamp(value, 1, DepthLimit);

            if (Set(ref _maxDepth, clamped) && IsConnected && !UnlimitedDepth)
            {
                _ = RebuildTreeAsync();
            }
        }
    }

    /// <summary>
    /// „Bez ograničenja" — drvo se obilazi do kraja, koliko god da je duboko.
    /// </summary>
    /// <remarks>
    /// Svaki čvor je poziv u tuđi proces, pa obilazak bez granice ume da potraje i da istekne rok.
    /// Zato je ovo izbor operatera, a ne podrazumevano stanje.
    /// </remarks>
    public bool UnlimitedDepth
    {
        get => _unlimitedDepth;
        set
        {
            if (!Set(ref _unlimitedDepth, value))
            {
                return;
            }

            Raise(nameof(DepthIsLimited));

            Append(value
                ? "Dubina: BEZ OGRANIČENJA — drvo se obilazi do kraja; obilazak može da potraje."
                : $"Dubina: ograničena na {MaxDepth}.");

            if (IsConnected)
            {
                _ = RebuildTreeAsync();
            }
        }
    }

    /// <summary>Da li granica dubine uopšte važi — polje za dubinu se tada i koristi.</summary>
    public bool DepthIsLimited => !UnlimitedDepth;

    /// <summary>Dubina sa kojom se obilazak zaista pokreće.</summary>
    private int EffectiveDepth => UnlimitedDepth ? int.MaxValue : MaxDepth;

    /// <summary>Rok za jedan poziv prema UIA, u sekundama.</summary>
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => Set(ref _timeoutSeconds, Math.Clamp(value, 1, 60));
    }

    /// <summary>Da li su radnje nad kontrolama uopšte moguće.</summary>
    public bool CanAct => IsConnected && !IsBusy && !ReadOnlyMode;

    /// <summary>Da li je čitanje moguće — dozvoljeno je i u režimu „Samo čitanje".</summary>
    public bool CanRead => IsConnected && !IsBusy;

    // -----------------------------------------------------------------------------------
    // Levi panel — drvo
    // -----------------------------------------------------------------------------------

    /// <summary>Prozori koji se mogu obilaziti.</summary>
    public IReadOnlyList<UiaWindowKind> Windows { get; } = new[] { UiaWindowKind.Main, UiaWindowKind.Edit };

    /// <summary>Prozor čije se drvo obilazi.</summary>
    public UiaWindowKind SelectedWindow
    {
        get => _selectedWindow;
        set
        {
            if (Set(ref _selectedWindow, value) && IsConnected)
            {
                _ = RebuildTreeAsync();
            }
        }
    }

    /// <summary>Pogledi na drvo koji se mogu izabrati.</summary>
    public IReadOnlyList<UiaViewKind> Views { get; } = new[] { UiaViewKind.Raw, UiaViewKind.Control, UiaViewKind.Content };

    /// <summary>
    /// Pogled u kome se drvo obilazi; podrazumevano <see cref="UiaViewKind.Raw"/>.
    /// </summary>
    /// <remarks>
    /// Raw je podrazumevan zato što je ovo alat za otkrivanje: u Control pogledu WPF <c>Grid</c>,
    /// <c>Border</c> i <c>Panel</c> jednostavno nema, pa bi izgledalo kao da kontrola visi u
    /// vazduhu — ili kao da je uopšte nema.
    /// </remarks>
    public UiaViewKind SelectedView
    {
        get => _selectedView;
        set
        {
            if (Set(ref _selectedView, value) && IsConnected)
            {
                _ = RebuildTreeAsync();
            }
        }
    }

    /// <summary>Koren obilaska; prazno kad prozor nije pronađen ili kad pretraga nema pogotka.</summary>
    public ObservableCollection<UiaNodeViewModel> TreeNodes { get; } = new();

    // -----------------------------------------------------------------------------------
    // Lanac roditelja
    // -----------------------------------------------------------------------------------

    /// <summary>Lanac od prozora do izabranog, odnosno uhvaćenog elementa.</summary>
    public ObservableCollection<UiaChainItemViewModel> Chain { get; } = new();

    /// <summary>Naslov panela sa lancem — odakle je lanac došao.</summary>
    public string ChainTitle
    {
        get => _chainTitle;
        private set => Set(ref _chainTitle, value);
    }

    /// <summary>Da li lanac uopšte ima šta da prikaže.</summary>
    public bool HasChain => Chain.Count > 0;

    // -----------------------------------------------------------------------------------
    // Pretraga u drvetu
    // -----------------------------------------------------------------------------------

    /// <summary>Tekst koji se traži po Name, AutomationId, ControlType, ClassName i FrameworkId.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value ?? string.Empty))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>Tipovi kontrola koji u trenutnom drvetu zaista postoje, sa „svi" na početku.</summary>
    public ObservableCollection<string> ControlTypes { get; } = new() { UiaTreeSearch.AllControlTypes };

    /// <summary>Tip po kome se filtrira, ili „svi".</summary>
    public string SelectedControlType
    {
        get => _selectedControlType;
        set
        {
            if (Set(ref _selectedControlType, string.IsNullOrEmpty(value) ? UiaTreeSearch.AllControlTypes : value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>Oba režima prikaza poklapanja.</summary>
    public IReadOnlyList<string> FilterModes => UiaTreeSearch.ModeNames;

    /// <summary>Izabrani režim: „istakni" ili „samo poklapanja".</summary>
    public string SelectedFilterMode
    {
        get => _filterModeName;
        set
        {
            if (Set(ref _filterModeName, string.IsNullOrEmpty(value) ? UiaTreeSearch.HighlightModeName : value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>Broj prikazanih i ukupan broj čvorova.</summary>
    public string TreeCountText
    {
        get => _treeCountText;
        private set => Set(ref _treeCountText, value);
    }

    /// <summary>Izabrani čvor.</summary>
    public UiaNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (Set(ref _selectedNode, value))
            {
                Raise(nameof(HasSelectedNode));
                ShowChainOfSelectedNode();
                RefreshCommands();
            }
        }
    }

    /// <summary>Da li je neki čvor izabran.</summary>
    public bool HasSelectedNode => SelectedNode is not null;

    /// <summary>Vrednost koja se upisuje u izabrani čvor.</summary>
    public string NodeValue
    {
        get => _nodeValue;
        set => Set(ref _nodeValue, value);
    }

    // -----------------------------------------------------------------------------------
    // Desni panel — stalna dugmad
    // -----------------------------------------------------------------------------------

    /// <summary>Skupine kontrola, grupisane po prozoru kao u samom CableConnector-u.</summary>
    public ObservableCollection<UiaActionGroupViewModel> ActionGroups { get; } = new();

    // -----------------------------------------------------------------------------------
    // Log
    // -----------------------------------------------------------------------------------

    /// <summary>Ceo log, od otvaranja strane.</summary>
    public string LogText => _log.ToString();

    // -----------------------------------------------------------------------------------
    // Komande
    // -----------------------------------------------------------------------------------

    public AsyncRelayCommand ConnectCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand DiscoverAllCommand { get; }

    public AsyncRelayCommand CaptureElementCommand { get; }

    public RelayCommand ExpandAllCommand { get; }

    public RelayCommand CollapseAllCommand { get; }

    public RelayCommand ExportMapCommand { get; }

    public RelayCommand CopyLogCommand { get; }

    public RelayCommand ClearLogCommand { get; }

    public AsyncRelayCommand InvokeNodeCommand { get; }

    public AsyncRelayCommand PhysicalClickNodeCommand { get; }

    public AsyncRelayCommand ReadNodeValueCommand { get; }

    public AsyncRelayCommand WriteNodeValueCommand { get; }

    // -----------------------------------------------------------------------------------
    // Povezivanje i drvo
    // -----------------------------------------------------------------------------------

    private Task ConnectAsync() => FindWindowsAsync("Poveži");

    // „Osveži" radi isto što i „Poveži": traži prozore ispočetka i obilazi drvo. Razlika je samo u
    // tome što se posle prvog puta zove drugačije, pa se u logu vidi šta je operater pritisnuo.
    private Task RefreshAsync() => FindWindowsAsync("Osveži");

    private async Task FindWindowsAsync(string trigger)
    {
        IsBusy = true;

        try
        {
            Append($"— {trigger} —");
            EnsureWorker();

            UiaCall<IReadOnlyList<UiaWindowInfo>> call =
                await _worker.RunAsync(a => _probe.FindWindows(a), Timeout).ConfigureAwait(true);

            if (!call.Ok || call.Value is null)
            {
                LogFailure("traženje prozora", call);
                Main = UiaWindowInfo.NotFound(UiaWindowKind.Main, $"pretraga nije uspela: {call.FailureText}");
                Edit = UiaWindowInfo.NotFound(UiaWindowKind.Edit, $"pretraga nije uspela: {call.FailureText}");
                IsConnected = false;
                ClearTree();
                return;
            }

            Main = call.Value[0];
            Edit = call.Value[1];

            Append($"Glavni prozor: {MainStatusText} — {Main.Note}. PID={MainPidText}; naslov=\"{Main.Title}\"; ClassName=\"{Main.ClassName}\".");
            Append($"Prozor „Edit“: {EditStatusText} — {Edit.Note}. PID={EditPidText}; naslov=\"{Edit.Title}\"; ClassName=\"{Edit.ClassName}\".");

            ReadVersion();

            IsConnected = Main.Found || Edit.Found;
            Append($"Traženje prozora je trajalo {call.ElapsedMs} ms.");

            if (IsConnected)
            {
                await RebuildTreeAsync().ConfigureAwait(true);
            }
            else
            {
                ClearTree();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReadVersion()
    {
        if (!Main.Found)
        {
            VersionText = "—";
            VersionIsExpected = true;   // bez prozora nema ni razloga za upozorenje
            return;
        }

        string? version = CableConnectorVersion.Parse(Main.Title);
        VersionText = version ?? "nije pročitana iz naslova";
        VersionIsExpected = CableConnectorVersion.IsExpected(version);

        Append(VersionIsExpected
            ? $"Verzija iz naslova: {VersionText} — očekivana."
            : $"Verzija iz naslova: {VersionText} — RAZLIKUJE SE od očekivane {CableConnectorVersion.Expected}.");
    }

    private async Task RebuildTreeAsync()
    {
        UiaWindowInfo window = SelectedWindow == UiaWindowKind.Main ? Main : Edit;

        ClearTree();

        if (!window.Found)
        {
            Append($"Drvo za prozor {SelectedWindow}: prozor nije pronađen, nema šta da se obiđe.");
            return;
        }

        UiaWindowKind kind = SelectedWindow;
        UiaViewKind view = SelectedView;
        int depth = EffectiveDepth;

        EnsureWorker();

        UiaCall<UiaTreeNodeInfo?> call =
            await _worker.RunAsync(a => _probe.BuildTree(a, kind, depth, view), Timeout).ConfigureAwait(true);

        if (!call.Ok)
        {
            LogFailure($"obilazak drveta prozora {kind}", call);
            return;
        }

        if (call.Value is null)
        {
            Append($"Drvo za prozor {kind}: koren nije zapamćen; pritisnite „Osveži“.");
            return;
        }

        _treeRoot = call.Value;
        _rootNode = new UiaNodeViewModel(call.Value, this);

        RefreshControlTypes();
        ApplyFilter();

        string granica = UnlimitedDepth ? "bez ograničenja" : depth.ToString(CultureInfo.InvariantCulture);

        Append($"Drvo prozora {kind} obiđeno (pogled: {view}; granica dubine: {granica}): " +
               $"{UiaTreeSearch.Count(_treeRoot)} čvorova, najveća dostignuta dubina " +
               $"{UiaTreeSearch.MaxDepth(_treeRoot)}, trajanje {call.ElapsedMs} ms.");
    }

    private void ClearTree()
    {
        _treeRoot = null;
        _rootNode = null;

        TreeNodes.Clear();
        SelectedNode = null;

        RefreshControlTypes();
        TreeCountText = UiaTreeSearch.CountText(0, 0);
    }

    /// <summary>
    /// Puni padajuću listu tipovima koji u drvetu zaista postoje.
    /// </summary>
    /// <remarks>
    /// Izabrani tip se zadržava ako ga i novo drvo ima; inače se pada nazad na „svi", da filter
    /// ne bi ostao na tipu koga više nema i sakrio celo drvo.
    /// </remarks>
    private void RefreshControlTypes()
    {
        IReadOnlyList<string> types = UiaTreeSearch.ControlTypes(_treeRoot);

        ControlTypes.Clear();

        foreach (string type in types)
        {
            ControlTypes.Add(type);
        }

        if (!ControlTypes.Contains(SelectedControlType))
        {
            // Polje, a ne svojstvo: svojstvo bi odmah povuklo filtriranje, pa bi se isti red u
            // logu pojavio dvaput — jednom odavde, jednom iz obilaska koji je ovo i pozvao.
            _selectedControlType = UiaTreeSearch.AllControlTypes;
            Raise(nameof(SelectedControlType));
        }
    }

    /// <summary>Primenjuje pretragu na već obiđeno drvo; ne dodiruje UIA.</summary>
    private void ApplyFilter()
    {
        if (_treeRoot is null || _rootNode is null)
        {
            TreeNodes.Clear();
            TreeCountText = UiaTreeSearch.CountText(0, 0);
            return;
        }

        UiaTreeFilterMode mode = UiaTreeSearch.ModeFromName(SelectedFilterMode);

        UiaTreeFilterResult result = UiaTreeSearch.Apply(
            _treeRoot,
            SearchText,
            SelectedControlType,
            mode);

        LogFilter(result, mode);

        _rootNode.ApplyFilter(result);

        // Koren se sklanja samo kad u celom drvetu nema nijednog pogotka; inače ostaje isti
        // objekat, pa drvo ne treperi dok se kuca u polju za pretragu.
        bool rootVisible = result.Visible.Contains(_rootNode.Element.Id);

        if (rootVisible && TreeNodes.Count == 0)
        {
            TreeNodes.Add(_rootNode);
        }
        else if (!rootVisible && TreeNodes.Count != 0)
        {
            TreeNodes.Clear();
        }

        TreeCountText = UiaTreeSearch.CountText(result.Shown, result.Total);
    }

    private void SetAllExpanded(bool expanded) => _rootNode?.SetExpanded(expanded);

    /// <summary>
    /// Upisuje u log šta je traženo i šta je od toga ispalo.
    /// </summary>
    /// <remarks>
    /// Kad nema nijednog pogotka ispisuje i prva imena iz stabla: bez toga se ne vidi razlika
    /// između „kontrola se zove drugačije" i „kontrole uopšte nemaju imena", a to su dva sasvim
    /// različita zaključka.
    /// </remarks>
    private void LogFilter(UiaTreeFilterResult result, UiaTreeFilterMode mode)
    {
        Append(UiaTreeSearch.DiagnosticText(SearchText, SelectedControlType, mode, result));

        if (result.Matches == 0 && !UiaTreeSearch.IsEmpty(SearchText, SelectedControlType))
        {
            Append(UiaTreeSearch.FirstNamesText(_treeRoot));
        }
    }

    // -----------------------------------------------------------------------------------
    // Radnje nad izabranim čvorom
    // -----------------------------------------------------------------------------------

    private Task InvokeNodeAsync() => NodeActionAsync(
        "Invoke",
        id => _probe.Invoke(id, AllowPhysicalClick));

    private Task PhysicalClickNodeAsync() => NodeActionAsync(
        "Fizički klik",
        id => _probe.PhysicalClick(id));

    private Task ReadNodeValueAsync() => NodeActionAsync(
        "Pročitaj vrednost",
        id => _probe.ReadValue(id),
        readOnlyAllowed: true);

    private Task WriteNodeValueAsync()
    {
        string text = NodeValue;
        return NodeActionAsync("Upiši vrednost", id => _probe.WriteValue(id, text));
    }

    private async Task NodeActionAsync(string label, Func<int, UiaActionResult> action, bool readOnlyAllowed = false)
    {
        UiaNodeViewModel? node = SelectedNode;

        if (node is null)
        {
            return;
        }

        if (!readOnlyAllowed && !Guard(label))
        {
            return;
        }

        IsBusy = true;

        try
        {
            Append($"— {label} nad izabranim čvorom —");
            Append($"  element: {node.Element.LogLine}");

            EnsureWorker();

            int id = node.Element.Id;
            UiaCall<UiaActionResult> call = await _worker.RunAsync(_ => action(id), Timeout).ConfigureAwait(true);

            LogAction(label, call);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // -----------------------------------------------------------------------------------
    // Radnje nad stalnom dugmadi
    // -----------------------------------------------------------------------------------

    /// <summary>Traži kontrolu i pokreće je.</summary>
    internal async Task RunTargetAsync(UiaActionViewModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!Guard($"pokretanje „{action.Name}“"))
        {
            return;
        }

        if (action.NeedsConfirmation && !AskForConfirmation(action))
        {
            return;
        }

        bool allowClick = AllowPhysicalClick;
        await SearchAndActAsync(
            action,
            $"Pokretanje „{action.Name}“",
            id => _probe.Invoke(id, allowClick)).ConfigureAwait(true);
    }

    /// <summary>Traži kontrolu i čita joj vrednost.</summary>
    internal async Task ReadTargetAsync(UiaActionViewModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        UiaActionResult? result = await SearchAndActAsync(
            action,
            $"Čitanje „{action.Name}“",
            id => _probe.ReadValue(id)).ConfigureAwait(true);

        if (result is { Success: true })
        {
            // Detalj je oblika: vrednost = "…" — u polje ide samo ono između navodnika.
            int start = result.Detail.IndexOf('"');
            int end = result.Detail.LastIndexOf('"');

            if (start >= 0 && end > start)
            {
                action.Value = result.Detail[(start + 1)..end];
            }
        }
    }

    /// <summary>Traži kontrolu i upisuje joj vrednost.</summary>
    internal async Task WriteTargetAsync(UiaActionViewModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!Guard($"upis u „{action.Name}“"))
        {
            return;
        }

        string text = action.Value;
        await SearchAndActAsync(
            action,
            $"Upis u „{action.Name}“",
            id => _probe.WriteValue(id, text)).ConfigureAwait(true);
    }

    /// <summary>
    /// Jedan poziv: pretraga kontrole i radnja nad njom, pod istim rokom.
    /// </summary>
    /// <remarks>
    /// Pretraga i radnja idu zajedno zato što između njih element ne sme da „ostari": prozor koji
    /// se u međuvremenu zatvori ostavio bi COM referencu na nešto čega više nema.
    /// </remarks>
    private async Task<UiaActionResult?> SearchAndActAsync(
        UiaActionViewModel action,
        string label,
        Func<int, UiaActionResult> work)
    {
        if (!IsConnected)
        {
            Append($"{label}: preskočeno — nijedan prozor CableConnector-a nije pronađen.");
            action.SetStatus("nema prozora");
            return null;
        }

        IsBusy = true;

        try
        {
            Append($"— {label} —");
            EnsureWorker();

            UiaControlTarget target = action.Target;
            string? knownId = _map.KnownAutomationId(target.LogicalName);

            UiaCall<(UiaSearchReport Report, UiaActionResult? Result)> call = await _worker.RunAsync(
                _ =>
                {
                    UiaSearchReport report = _probe.Search(target, knownId);
                    return report.Element is null
                        ? (report, (UiaActionResult?)null)
                        : (report, work(report.Element.Id));
                },
                Timeout).ConfigureAwait(true);

            if (!call.Ok)
            {
                LogFailure(label, call);
                action.SetStatus(call.TimedOut ? "istekao rok" : "greška");
                return null;
            }

            (UiaSearchReport report, UiaActionResult? result) = call.Value;

            LogSearch(report);

            if (report.Element is null || report.Strategy is null)
            {
                action.SetStatus("nije pronađeno");
                return null;
            }

            _map.Remember(target, report.Element, report.Strategy.Value);
            ExportMapCommand.RaiseCanExecuteChanged();

            if (result is null)
            {
                action.SetStatus("pronađeno");
                return null;
            }

            LogResult(result, call.ElapsedMs);
            action.SetStatus(result.Success ? result.Method : $"neuspeh: {result.Method}");
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool AskForConfirmation(UiaActionViewModel action)
    {
        bool confirmed = _confirmation.Ask(
            "CableConnector — potvrda",
            $"„{action.Name}“ pokreće stvarni hardver testera.\n\n" +
            "Proverite da je radno mesto bezbedno i da niko ne dodiruje kabl.\n\n" +
            "Da li zaista želite da pokrenete ovu radnju?");

        Append(confirmed
            ? $"Potvrda za „{action.Name}“: operater je potvrdio."
            : $"Potvrda za „{action.Name}“: operater je odustao — radnja nije izvršena.");

        return confirmed;
    }

    private bool Guard(string what)
    {
        if (!ReadOnlyMode)
        {
            return true;
        }

        Append($"{what}: odbijeno — „Samo čitanje“ je uključeno.");
        return false;
    }

    // -----------------------------------------------------------------------------------
    // Otkrij sve
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Prolazi kroz ceo katalog poznatih kontrola i za svaku pokreće pretragu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Isključivo čitanje.</b> Radi se samo <see cref="CableConnectorProbe.Search"/>: nijedna
    /// kontrola se ne poziva, ništa se ne upisuje, i ništa se ne menja ni kad je „Samo čitanje"
    /// isključeno. Zato prolaz ne traži potvrdu ni za START ni za Download — iza njih se ovde
    /// ništa ne dešava.
    /// </para>
    /// <para>
    /// Svaka pretraga ide kao zaseban poziv, pod istim rokom kao i sve ostalo: kontrola koja
    /// zaglavi ne sme da obori ceo prolaz. Ako neka pretraga ne vrati odgovor, prolaz se nastavlja
    /// na novoj niti, ali se ne smatra uspelim — pa se ni mapa ne izvozi iz nepotpunog prolaza.
    /// </para>
    /// </remarks>
    private async Task DiscoverAllAsync()
    {
        if (!IsConnected)
        {
            Append("Otkrij sve: preskočeno — nijedan prozor CableConnector-a nije pronađen.");
            return;
        }

        IsBusy = true;

        try
        {
            Append("— Otkrij sve —");
            Append("Prolaz je isključivo čitanje: nijedna kontrola se ne poziva, ni kad je „Samo čitanje“ isključeno.");

            int found = 0;
            int missing = 0;
            int broken = 0;
            long total = 0;

            foreach (UiaActionGroupViewModel group in ActionGroups)
            {
                foreach (UiaActionViewModel action in group.Actions)
                {
                    EnsureWorker();

                    UiaControlTarget target = action.Target;
                    string? knownId = _map.KnownAutomationId(target.LogicalName);

                    UiaCall<UiaSearchReport> call = await _worker
                        .RunAsync(_ => _probe.Search(target, knownId), Timeout)
                        .ConfigureAwait(true);

                    total += call.ElapsedMs;

                    if (!call.Ok || call.Value is null)
                    {
                        broken++;
                        LogFailure($"pretraga „{target.Name}“", call);
                        action.SetStatus(call.TimedOut ? "istekao rok" : "greška");
                        action.ApplyDiscovery(null);
                        continue;
                    }

                    UiaSearchReport report = call.Value;

                    LogSearch(report);
                    action.ApplyDiscovery(report);

                    if (report.Element is not null && report.Strategy is not null)
                    {
                        found++;
                        action.SetStatus($"pronađeno: {report.Strategy}");
                        _map.Remember(target, report.Element, report.Strategy.Value);
                    }
                    else
                    {
                        missing++;
                        action.SetStatus("nije pronađeno");
                    }
                }
            }

            _discoveryDone = broken == 0 && _map.Count > 0;
            ExportMapCommand.RaiseCanExecuteChanged();

            Append($"Otkrij sve: pronađeno {found}, nije pronađeno {missing}" +
                   (broken == 0 ? string.Empty : $", bez odgovora {broken}") +
                   $" od ukupno {CableConnectorControls.All.Count} kontrola; trajanje {total} ms.");

            Append(_discoveryDone
                ? "Prolaz je uspeo — „Izvezi mapu“ je sada omogućeno."
                : broken == 0
                    ? "Nijedna kontrola nije pronađena — nema šta da se izveze."
                    : "Prolaz nije prošao do kraja — „Izvezi mapu“ ostaje zaključano.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // -----------------------------------------------------------------------------------
    // Element ispod miša
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Uzima element ispod pokazivača miša i ispisuje sve o njemu i o svim njegovim precima.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Radi i dok je „Samo čitanje" uključeno, i radi bez obzira na to da li su prozori
    /// CableConnector-a pronađeni: ovo je čitanje ekrana, ne radnja nad kontrolom. Ništa se ne
    /// poziva i ništa se ne upisuje.
    /// </para>
    /// <para>
    /// Položaj miša se čita <b>ovde</b>, na GUI niti, a ne na UIA niti: dok poziv čeka na red,
    /// pokazivač bi već bio negde drugde.
    /// </para>
    /// </remarks>
    private async Task CaptureElementAsync()
    {
        System.Drawing.Point point;

        try
        {
            point = FlaUI.Core.Input.Mouse.Position;
        }
        catch (Exception ex)
        {
            Append($"Uhvati element: položaj miša nije pročitan — {ex.GetType().Name}: {ex.Message}");
            return;
        }

        IsBusy = true;

        try
        {
            Append($"— Uhvati element (F8) — tačka {point.X},{point.Y} —");
            EnsureWorker();

            UiaCall<UiaCaptureInfo?> call = await _worker
                .RunAsync(a => _probe.CaptureFromPoint(a, point), Timeout)
                .ConfigureAwait(true);

            if (!call.Ok)
            {
                LogFailure("hvatanje elementa", call);
                return;
            }

            if (call.Value is not UiaCaptureInfo capture)
            {
                Append("  na toj tački nema nijednog UIA elementa.");
                return;
            }

            Append($"  element: {capture.Element.LogLine}");
            Append($"  lanac roditelja ({capture.Chain.Count} nivoa; {capture.Note}):");

            foreach (string red in UiaTreeText.Chain(capture.Chain)
                         .Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Append($"  {red.TrimEnd('\r')}");
            }

            // Prvo označavanje u stablu, pa tek onda lanac: izbor u stablu i sam puni panel
            // lancem iz stabla, a ovde treba da ostane ono što je UIA stvarno vratio — u njemu
            // ima i slojeva kojih u obiđenom stablu nema.
            RevealCaptured(capture.Element);
            ShowChain(capture.Chain, $"Lanac roditelja — uhvaćeno na {capture.PointText}");

            Append($"  hvatanje: {call.ElapsedMs} ms.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Označava uhvaćeni element u drvetu i skroluje do njega, ako ga drvo ima.</summary>
    private void RevealCaptured(UiaElementInfo element)
    {
        if (_rootNode is null)
        {
            Append("  u stablu: nema obiđenog stabla — pritisnite „Poveži“ ili „Osveži“.");
            return;
        }

        if (string.IsNullOrWhiteSpace(element.RuntimeId))
        {
            Append("  u stablu: element nema RuntimeId, pa se ne može prepoznati među čvorovima.");
            return;
        }

        UiaNodeViewModel? node = _rootNode.RevealByRuntimeId(element.RuntimeId);

        if (node is null)
        {
            Append($"  u stablu: nije pronađen (RuntimeId={element.RuntimeIdText}). " +
                   $"Element je verovatno iz drugog prozora, ispod granice dubine, ili ga pogled {SelectedView} ne prikazuje.");
            return;
        }

        if (!node.IsShown())
        {
            Append("  u stablu: pronađen, ali ga trenutna pretraga sakriva — očistite polje za pretragu da biste ga videli.");
        }

        // Redosled je bitan: prvo izbor u prikazu (prikaz sam skroluje do označenog reda), pa
        // tek onda upis u SelectedNode, da bi panel sa lancem pokazao isti čvor.
        node.IsSelected = true;
        SelectedNode = node;

        Append($"  u stablu: označen čvor {UiaTreeText.Head(node.Element)}.");
    }

    // -----------------------------------------------------------------------------------
    // Lanac roditelja
    // -----------------------------------------------------------------------------------

    /// <summary>Puni panel lancem izabranog čvora, pročitanim iz već obiđenog stabla.</summary>
    private void ShowChainOfSelectedNode()
    {
        UiaNodeViewModel? node = SelectedNode;

        if (node is null || _treeRoot is null)
        {
            ShowChain(Array.Empty<UiaElementInfo>(), "Lanac roditelja");
            return;
        }

        IReadOnlyList<UiaTreeNodeInfo> path = UiaTreeSearch.Path(_treeRoot, node.Element.Id);

        ShowChain(
            path.Select(n => n.Element).ToArray(),
            $"Lanac roditelja — {UiaTreeText.Head(node.Element)}");
    }

    private void ShowChain(IReadOnlyList<UiaElementInfo> chain, string title)
    {
        Chain.Clear();

        for (int level = 0; level < chain.Count; level++)
        {
            Chain.Add(new UiaChainItemViewModel(level, chain[level]));
        }

        ChainTitle = title;
        Raise(nameof(HasChain));
    }

    // -----------------------------------------------------------------------------------
    // Kopiranje čvora
    // -----------------------------------------------------------------------------------

    /// <summary>Kopira sve podatke o jednom čvoru u ostavu.</summary>
    internal void CopyNodeData(UiaNodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(node);
        CopyToClipboard(UiaTreeText.Node(node.Element), $"Podaci o čvoru „{node.Element.DisplayName}“");
    }

    /// <summary>Kopira čvor sa svom decom, uvučeno, u ostavu.</summary>
    internal void CopyNodeBranch(UiaNodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(node);

        string text = UiaTreeText.Branch(node.Source);
        CopyToClipboard(text, $"Grana čvora „{node.Element.DisplayName}“ ({UiaTreeSearch.Count(node.Source)} čvorova)");
    }

    private void CopyToClipboard(string text, string what)
    {
        try
        {
            System.Windows.Clipboard.SetText(text);
            Append($"{what}: prekopirano u ostavu.");
        }
        catch (Exception ex)
        {
            // Ostavu ume da drži drugi program; to nije razlog da strana padne.
            Append($"{what}: nije prekopirano u ostavu: {ex}");
        }
    }

    // -----------------------------------------------------------------------------------
    // Mapa
    // -----------------------------------------------------------------------------------

    private void ExportMap()
    {
        string path = Path.Combine(
            Path.GetDirectoryName(Core.Configuration.SettingsStore.DefaultFilePath) ?? ".",
            MapFileName);

        try
        {
            string? folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(path, _map.ToJson());
            Append($"Mapa ({_map.Count} kontrola) izvezena u \"{path}\".");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Append($"Mapa nije izvezena u \"{path}\": {ex}");
        }
    }

    // -----------------------------------------------------------------------------------
    // Log
    // -----------------------------------------------------------------------------------

    private void CopyLog()
    {
        try
        {
            System.Windows.Clipboard.SetText(LogText);
            Append("Log je prekopiran u ostavu.");
        }
        catch (Exception ex)
        {
            // Ostavu ume da drži drugi program; to nije razlog da strana padne.
            Append($"Log nije prekopiran u ostavu: {ex}");
        }
    }

    private void ClearLog()
    {
        _log.Clear();
        Raise(nameof(LogText));
        Append("Log je obrisan.");
    }

    private void LogSearch(UiaSearchReport report)
    {
        Append($"  traži se: „{report.Target.Name}“ u prozoru {report.Target.Window} " +
               $"(logičko ime {report.Target.LogicalName}).");

        foreach (UiaSearchAttempt attempt in report.Attempts)
        {
            Append($"    {attempt.LogLine}");
        }

        if (report.Element is not null)
        {
            Append($"  pronađeno korakom {report.Strategy}: {report.Element.LogLine}");
        }
        else
        {
            Append(report.Attempts.Count == 0
                ? $"  NIJE pronađeno: prozor {report.Target.Window} nije zapamćen — pritisnite „Osveži“."
                : "  NIJE pronađeno: nijedan korak nije dao pogodak.");

            if (report.Nearest.Count != 0)
            {
                Append($"  najbliža viđena imena: {string.Join(" | ", report.Nearest)}");
            }
            else
            {
                Append("  u prozoru nije viđeno nijedno ime — kontrole verovatno nisu izložene kroz UIA.");
            }
        }

        Append($"  pretraga: {report.ElapsedMs} ms.");
    }

    private void LogResult(UiaActionResult result, long elapsedMs)
    {
        if (result.UsedPhysicalClick)
        {
            Append("  PAŽNJA: upotrebljen je fizički klik mišem, kao poslednja mogućnost.");
        }

        Append($"  radnja: {(result.Success ? "USPELA" : "NIJE USPELA")} — način: {result.Method}; {result.Detail}");
        Append($"  ukupno: {elapsedMs} ms.");
    }

    private void LogFailure<T>(string what, UiaCall<T> call)
    {
        if (call.TimedOut)
        {
            Append($"  {what}: ISTEKAO ROK posle {call.ElapsedMs} ms (rok je {TimeoutSeconds} s). " +
                   "UIA nit je napuštena; sledeći poziv kreće na novoj niti.");
            return;
        }

        Append($"  {what}: GREŠKA posle {call.ElapsedMs} ms.");

        if (call.Error is not null)
        {
            Append(call.Error.ToString());
        }
    }

    private void LogAction(string label, UiaCall<UiaActionResult> call)
    {
        if (!call.Ok || call.Value is null)
        {
            LogFailure(label, call);
            return;
        }

        LogResult(call.Value, call.ElapsedMs);
    }

    private void Append(string line)
    {
        _log.Append(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append("  ")
            .AppendLine(line);

        Raise(nameof(LogText));
    }

    // -----------------------------------------------------------------------------------
    // Pomoćno
    // -----------------------------------------------------------------------------------

    private TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

    /// <summary>
    /// Zamenjuje UIA nit ako je prethodni poziv istekao i ostavio je zaglavljenu.
    /// </summary>
    private void EnsureWorker()
    {
        if (!_worker.IsAbandoned || _disposed)
        {
            return;
        }

        Append("Prethodni poziv nije vratio odgovor u roku; stara UIA nit je napuštena, rad se nastavlja na novoj.");
        _worker.Dispose();
        _worker = new UiaWorker();
    }

    /// <summary>Izuzetak iz komande — u log, nikad dalje.</summary>
    internal void ReportCommandError(Exception ex) => Append($"Neočekivana greška u komandi:\n{ex}");

    /// <summary>Prikaz javlja da li je Windows prihvatio prečicu F8.</summary>
    internal void ReportHotkey(bool registered) => Append(registered
        ? "Prečica F8 je prijavljena Windows-u: „Uhvati element“ radi i kad je u prvom planu CableConnector."
        : "Prečica F8 nije prijavljena Windows-u (verovatno je zauzeta) — ostaju dugme " +
          "„Uhvati element“ i taster F8 dok je CableTest u prvom planu.");

    private void RefreshCommands()
    {
        RaiseAll(nameof(CanAct), nameof(CanRead));

        ConnectCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        DiscoverAllCommand.RaiseCanExecuteChanged();
        CaptureElementCommand.RaiseCanExecuteChanged();
        InvokeNodeCommand.RaiseCanExecuteChanged();
        PhysicalClickNodeCommand.RaiseCanExecuteChanged();
        ReadNodeValueCommand.RaiseCanExecuteChanged();
        WriteNodeValueCommand.RaiseCanExecuteChanged();

        foreach (UiaActionGroupViewModel group in ActionGroups)
        {
            foreach (UiaActionViewModel action in group.Actions)
            {
                action.RefreshCommands();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _worker.Dispose();
    }
}
