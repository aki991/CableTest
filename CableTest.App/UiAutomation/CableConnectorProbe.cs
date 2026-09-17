using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Patterns;
using FlaUI.UIA3;

namespace CableTest.App.UiAutomation;

/// <summary>Ishod jedne radnje nad kontrolom.</summary>
/// <param name="Success">Da li je radnja izvršena.</param>
/// <param name="Method">Čime je izvršena, npr. „InvokePattern".</param>
/// <param name="Detail">Dodatno objašnjenje, npr. pročitana vrednost ili razlog neuspeha.</param>
/// <param name="UsedPhysicalClick">Da li je morao fizički klik mišem.</param>
public sealed record UiaActionResult(bool Success, string Method, string Detail, bool UsedPhysicalClick = false);

/// <summary>
/// Sav rad sa CableConnector-om preko UI Automation.
/// </summary>
/// <remarks>
/// <para>
/// Svaka metoda se poziva <b>isključivo</b> sa UIA niti (<see cref="UiaWorker"/>). Pronađeni
/// elementi su COM objekti i ostaju ovde; napolje idu samo <see cref="UiaElementInfo"/> snimci, a
/// nazad se element bira po <see cref="UiaElementInfo.Id"/>.
/// </para>
/// <para>
/// Ništa ovde ne otvara COM port niti razgovara sa testerom. Jedini dodir sa hardverom je
/// posredan — kroz dugmad samog CableConnector-a, i to tek kad operater to izričito zatraži.
/// </para>
/// </remarks>
public sealed class CableConnectorProbe
{
    private readonly Dictionary<int, AutomationElement> _elements = new();
    private readonly Dictionary<UiaWindowKind, AutomationElement> _windows = new();

    private int _nextId;

    /// <summary>
    /// Traži oba prozora, počev od korena radne površine.
    /// </summary>
    /// <remarks>
    /// Namerno se ne traži po imenu procesa: CableConnector se na raznim mašinama zove različito,
    /// a naslov prozora je ono što se zaista vidi. Prozorče „Edit" se prvo traži među prozorima
    /// radne površine, pa tek onda unutar glavnog prozora — u zavisnosti od toga kako ga program
    /// pravi, može biti i jedno i drugo, i u logu se vidi koje.
    /// </remarks>
    public IReadOnlyList<UiaWindowInfo> FindWindows(UIA3Automation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);

        _elements.Clear();
        _windows.Clear();
        _nextId = 0;

        AutomationElement[] desktopChildren = automation.GetDesktop().FindAllChildren();

        AutomationElement? main = desktopChildren
            .FirstOrDefault(w => CableConnectorVersion.IsMainWindowTitle(SafeName(w)));

        UiaWindowInfo mainInfo;
        int mainPid = 0;

        if (main is null)
        {
            mainInfo = UiaWindowInfo.NotFound(
                UiaWindowKind.Main,
                $"među {desktopChildren.Length} prozora radne površine nema nijednog čiji naslov počinje sa " +
                $"„{CableConnectorVersion.WindowTitlePrefix}“; program verovatno nije pokrenut");
        }
        else
        {
            mainPid = SafeValue(() => main.Properties.ProcessId.Value, 0);
            _windows[UiaWindowKind.Main] = main;
            mainInfo = new UiaWindowInfo(
                UiaWindowKind.Main,
                Found: true,
                mainPid,
                SafeName(main),
                SafeClassName(main),
                SafeFrameworkId(main),
                "pronađen među prozorima radne površine",
                Register(main));
        }

        UiaWindowInfo editInfo = FindEditWindow(desktopChildren, main, mainPid);

        return new[] { mainInfo, editInfo };
    }

    /// <summary>Obilazi drvo izabranog prozora do zadate dubine, u zadatom pogledu.</summary>
    /// <remarks>
    /// <para>
    /// Dubina je ograničena zato što UIA drvo ume da bude duboko i široko, a svaki čvor je poziv
    /// u tuđi proces: obilazak bez granice je najlakši način da se dođe do isteklog roka.
    /// </para>
    /// <para>
    /// Deca se traže kroz <see cref="ITreeWalker"/> izabranog pogleda, a ne kroz
    /// <c>FindAllChildren</c>: samo tako Raw pogled zaista vrati sve slojeve — i one koje okvir
    /// napravi sam od sebe, kao što su WPF <c>Grid</c>, <c>Border</c> i <c>Panel</c>.
    /// </para>
    /// </remarks>
    public UiaTreeNodeInfo? BuildTree(UIA3Automation automation, UiaWindowKind window, int maxDepth, UiaViewKind view)
    {
        ArgumentNullException.ThrowIfNull(automation);

        if (!_windows.TryGetValue(window, out AutomationElement? root))
        {
            return null;
        }

        return Walk(root, depth: 0, maxDepth, Walker(automation, view));
    }

    /// <summary>Obilazač drveta za zadati pogled.</summary>
    private static ITreeWalker Walker(UIA3Automation automation, UiaViewKind view)
        => view switch
        {
            UiaViewKind.Control => automation.TreeWalkerFactory.GetControlViewWalker(),
            UiaViewKind.Content => automation.TreeWalkerFactory.GetContentViewWalker(),
            _ => automation.TreeWalkerFactory.GetRawViewWalker()
        };

    /// <summary>
    /// Traži kontrolu u prozoru, redom: AutomationId, tačno ime, ime „sadrži", LegacyIAccessible.
    /// </summary>
    /// <remarks>
    /// LegacyIAccessible ime se čita tek ako prva tri koraka nisu pogodila: to je jedno čitanje
    /// svojstva po elementu, u tuđem procesu, i nema razloga plaćati ga kad obično ime pogađa.
    /// </remarks>
    public UiaSearchReport Search(UiaControlTarget target, string? knownAutomationId)
    {
        ArgumentNullException.ThrowIfNull(target);

        var stopwatch = Stopwatch.StartNew();

        UiaControlTarget effective = knownAutomationId is null
            ? target
            : target with { KnownAutomationId = knownAutomationId };

        var attempts = new List<UiaSearchAttempt>();

        if (!_windows.TryGetValue(effective.Window, out AutomationElement? root))
        {
            stopwatch.Stop();
            return new UiaSearchReport(
                effective,
                attempts,
                Element: null,
                Strategy: null,
                Nearest: Array.Empty<string>(),
                stopwatch.ElapsedMilliseconds);
        }

        AutomationElement[] descendants = root.FindAllDescendants();
        List<UiaElementInfo> infos = descendants.Select(d => Describe(d)).ToList();
        bool legacyRead = false;

        foreach (UiaSearchStrategy strategy in UiaSearchPlan.For(effective))
        {
            string skip = UiaSearchPlan.SkipReason(strategy, effective);

            if (skip.Length != 0)
            {
                attempts.Add(new UiaSearchAttempt(strategy, skip, 0, 0, null));
                continue;
            }

            if (strategy == UiaSearchStrategy.LegacyName && !legacyRead)
            {
                for (int i = 0; i < infos.Count; i++)
                {
                    infos[i] = infos[i] with { LegacyName = ReadLegacyName(descendants[i]) };
                }

                legacyRead = true;
            }

            List<UiaElementInfo> hits = infos
                .Where(info => UiaSearchPlan.Matches(strategy, info, effective))
                .ToList();

            attempts.Add(new UiaSearchAttempt(strategy, string.Empty, infos.Count, hits.Count, hits.FirstOrDefault()));

            if (hits.Count != 0)
            {
                stopwatch.Stop();
                return new UiaSearchReport(
                    effective,
                    attempts,
                    hits[0],
                    strategy,
                    Nearest: Array.Empty<string>(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        stopwatch.Stop();

        IReadOnlyList<string> nearest = UiaNames.NearestNames(
            infos.Select(i => string.IsNullOrWhiteSpace(i.Name) ? i.LegacyName : i.Name),
            effective.Name);

        return new UiaSearchReport(effective, attempts, Element: null, Strategy: null, nearest, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Pokreće kontrolu: prvo InvokePattern, pa LegacyIAccessible.DoDefaultAction, pa — samo ako
    /// je izričito dozvoljeno — fizički klik mišem.
    /// </summary>
    /// <remarks>
    /// Fizički klik je poslednja mogućnost zato što ne pogađa kontrolu nego tačku na ekranu: ako
    /// je prozor u međuvremenu pomeren ili prekriven, klik ode nekud drugde. Zato je iza posebnog
    /// prekidača i zato se u logu uvek naglašava da je upotrebljen.
    /// </remarks>
    public UiaActionResult Invoke(int elementId, bool allowPhysicalClick)
    {
        if (!_elements.TryGetValue(elementId, out AutomationElement? element))
        {
            return new UiaActionResult(false, "—", "element više nije u pamćenju; potrebno je osvežiti drvo");
        }

        var reasons = new List<string>();

        try
        {
            if (element.Patterns.Invoke.TryGetPattern(out IInvokePattern? invoke) && invoke is not null)
            {
                invoke.Invoke();
                return new UiaActionResult(true, "InvokePattern", "pozvan Invoke()");
            }

            reasons.Add("InvokePattern nije podržan");
        }
        catch (Exception ex)
        {
            reasons.Add($"InvokePattern je pukao — {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (element.Patterns.LegacyIAccessible.TryGetPattern(out ILegacyIAccessiblePattern? legacy) && legacy is not null)
            {
                legacy.DoDefaultAction();
                return new UiaActionResult(true, "LegacyIAccessible.DoDefaultAction", string.Join("; ", reasons));
            }

            reasons.Add("LegacyIAccessible nije podržan");
        }
        catch (Exception ex)
        {
            reasons.Add($"LegacyIAccessible.DoDefaultAction je pukao — {ex.GetType().Name}: {ex.Message}");
        }

        if (!allowPhysicalClick)
        {
            reasons.Add("fizički klik nije dozvoljen (prekidač „Dozvoli fizički klik“ je isključen)");
            return new UiaActionResult(false, "—", string.Join("; ", reasons));
        }

        return PhysicalClick(elementId, string.Join("; ", reasons));
    }

    /// <summary>Fizički klik mišem u sredinu kontrole.</summary>
    public UiaActionResult PhysicalClick(int elementId, string precedingReasons = "")
    {
        if (!_elements.TryGetValue(elementId, out AutomationElement? element))
        {
            return new UiaActionResult(false, "—", "element više nije u pamćenju; potrebno je osvežiti drvo");
        }

        try
        {
            if (!element.TryGetClickablePoint(out System.Drawing.Point point))
            {
                System.Drawing.Rectangle bounds = SafeValue(
                    () => element.Properties.BoundingRectangle.Value,
                    System.Drawing.Rectangle.Empty);

                if (bounds.IsEmpty)
                {
                    return new UiaActionResult(
                        false,
                        "—",
                        Join(precedingReasons, "kontrola nema ni tačku za klik ni okvir na ekranu"));
                }

                point = new System.Drawing.Point(
                    bounds.X + (bounds.Width / 2),
                    bounds.Y + (bounds.Height / 2));
            }

            element.SetForeground();
            Mouse.Click(point);

            return new UiaActionResult(
                true,
                "Fizički klik",
                Join(precedingReasons, $"kliknuto na ({point.X},{point.Y})"),
                UsedPhysicalClick: true);
        }
        catch (Exception ex)
        {
            return new UiaActionResult(
                false,
                "Fizički klik",
                Join(precedingReasons, $"{ex.GetType().Name}: {ex.Message}"),
                UsedPhysicalClick: true);
        }
    }

    /// <summary>Čita vrednost kontrole: ValuePattern, pa LegacyIAccessible.Value, pa ime.</summary>
    public UiaActionResult ReadValue(int elementId)
    {
        if (!_elements.TryGetValue(elementId, out AutomationElement? element))
        {
            return new UiaActionResult(false, "—", "element više nije u pamćenju; potrebno je osvežiti drvo");
        }

        var reasons = new List<string>();

        try
        {
            if (element.Patterns.Value.TryGetPattern(out IValuePattern? value) && value is not null)
            {
                string text = value.Value.ValueOrDefault ?? string.Empty;
                return new UiaActionResult(true, "ValuePattern", $"vrednost = \"{text}\"");
            }

            reasons.Add("ValuePattern nije podržan");
        }
        catch (Exception ex)
        {
            reasons.Add($"ValuePattern je pukao — {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (element.Patterns.LegacyIAccessible.TryGetPattern(out ILegacyIAccessiblePattern? legacy) && legacy is not null)
            {
                string text = legacy.Value.ValueOrDefault ?? string.Empty;
                return new UiaActionResult(
                    true,
                    "LegacyIAccessible.Value",
                    Join(string.Join("; ", reasons), $"vrednost = \"{text}\""));
            }

            reasons.Add("LegacyIAccessible nije podržan");
        }
        catch (Exception ex)
        {
            reasons.Add($"LegacyIAccessible.Value je pukao — {ex.GetType().Name}: {ex.Message}");
        }

        string name = SafeName(element);
        return new UiaActionResult(
            true,
            "Name",
            Join(string.Join("; ", reasons), $"nema nijedan obrazac za vrednost; ime = \"{name}\""));
    }

    /// <summary>Upisuje vrednost: ValuePattern, pa LegacyIAccessible.SetValue.</summary>
    public UiaActionResult WriteValue(int elementId, string text)
    {
        if (!_elements.TryGetValue(elementId, out AutomationElement? element))
        {
            return new UiaActionResult(false, "—", "element više nije u pamćenju; potrebno je osvežiti drvo");
        }

        var reasons = new List<string>();

        try
        {
            if (element.Patterns.Value.TryGetPattern(out IValuePattern? value) && value is not null)
            {
                if (value.IsReadOnly.ValueOrDefault)
                {
                    reasons.Add("ValuePattern kaže da je kontrola samo za čitanje");
                }
                else
                {
                    value.SetValue(text);
                    return new UiaActionResult(true, "ValuePattern.SetValue", $"upisano \"{text}\"");
                }
            }
            else
            {
                reasons.Add("ValuePattern nije podržan");
            }
        }
        catch (Exception ex)
        {
            reasons.Add($"ValuePattern.SetValue je pukao — {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (element.Patterns.LegacyIAccessible.TryGetPattern(out ILegacyIAccessiblePattern? legacy) && legacy is not null)
            {
                legacy.SetValue(text);
                return new UiaActionResult(
                    true,
                    "LegacyIAccessible.SetValue",
                    Join(string.Join("; ", reasons), $"upisano \"{text}\""));
            }

            reasons.Add("LegacyIAccessible nije podržan");
        }
        catch (Exception ex)
        {
            reasons.Add($"LegacyIAccessible.SetValue je pukao — {ex.GetType().Name}: {ex.Message}");
        }

        return new UiaActionResult(false, "—", string.Join("; ", reasons));
    }

    /// <summary>
    /// Uzima element sa zadate tačke na ekranu i čita ceo lanac njegovih roditelja.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Isključivo čitanje: <c>ElementFromPoint</c> i penjanje uz Raw obilazač. Ništa se ne poziva,
    /// ništa se ne upisuje i ništa se ne klikće — zato ovo radi i dok je „Samo čitanje" uključeno.
    /// </para>
    /// <para>
    /// Lanac se penje uz <b>Raw</b> pogled, bez obzira na to koji je pogled izabran za drvo: cilj
    /// je da se vidi gde element zaista stoji, a ne gde bi stajao kad bi se slojevi sakrili.
    /// Penjanje staje na radnoj površini — ona je koren svega i nije deo nijednog prozora.
    /// </para>
    /// </remarks>
    public UiaCaptureInfo? CaptureFromPoint(UIA3Automation automation, System.Drawing.Point point)
    {
        ArgumentNullException.ThrowIfNull(automation);

        AutomationElement? element = automation.FromPoint(point);

        if (element is null)
        {
            return null;
        }

        UiaElementInfo info = Describe(element);

        ITreeWalker walker = automation.TreeWalkerFactory.GetRawViewWalker();
        AutomationElement desktop = automation.GetDesktop();

        var chain = new List<UiaElementInfo> { info };
        string note = "lanac je stigao do radne površine";

        try
        {
            AutomationElement? current = walker.GetParent(element);

            // Granica od 40 nivoa: dublje od toga UIA drvo ne ide, a zaštita od kruga mora da
            // postoji — element koji sam sebi bude roditelj ovde bi vrteo u mestu.
            for (int i = 0; i < 40 && current is not null; i++)
            {
                if (automation.Compare(current, desktop))
                {
                    break;
                }

                chain.Insert(0, Describe(current));
                current = walker.GetParent(current);
            }

            if (current is null)
            {
                note = "lanac je stigao do vrha drveta";
            }
        }
        catch (Exception ex)
        {
            note = $"penjanje uz lanac je stalo — {ex.GetType().Name}: {ex.Message}";
        }

        return new UiaCaptureInfo(point, info, chain, note);
    }

    /// <summary>Ponovo pročitani podaci o jednom elementu.</summary>
    public UiaElementInfo? Refresh(int elementId)
        => _elements.TryGetValue(elementId, out AutomationElement? element)
            ? Describe(element, elementId)
            : null;

    // -----------------------------------------------------------------------------------
    // Pomoćno
    // -----------------------------------------------------------------------------------

    private UiaWindowInfo FindEditWindow(AutomationElement[] desktopChildren, AutomationElement? main, int mainPid)
    {
        AutomationElement? edit = desktopChildren
            .FirstOrDefault(w => CableConnectorVersion.IsEditWindowTitle(SafeName(w))
                                 && (mainPid == 0 || SafeValue(() => w.Properties.ProcessId.Value, 0) == mainPid));

        string note = "pronađen među prozorima radne površine";

        if (edit is null && main is not null)
        {
            try
            {
                edit = main
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Window))
                    .FirstOrDefault(w => CableConnectorVersion.IsEditWindowTitle(SafeName(w)));

                note = "pronađen unutar glavnog prozora";
            }
            catch (Exception ex)
            {
                return UiaWindowInfo.NotFound(
                    UiaWindowKind.Edit,
                    $"pretraga unutar glavnog prozora je pukla — {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (edit is null)
        {
            return UiaWindowInfo.NotFound(
                UiaWindowKind.Edit,
                main is null
                    ? $"nema prozora sa naslovom „{CableConnectorVersion.EditWindowTitle}“; glavni prozor takođe nije pronađen"
                    : $"nema prozora sa naslovom „{CableConnectorVersion.EditWindowTitle}“ ni na radnoj površini ni " +
                      "unutar glavnog prozora; prozorče verovatno nije otvoreno");
        }

        _windows[UiaWindowKind.Edit] = edit;

        return new UiaWindowInfo(
            UiaWindowKind.Edit,
            Found: true,
            SafeValue(() => edit.Properties.ProcessId.Value, 0),
            SafeName(edit),
            SafeClassName(edit),
            SafeFrameworkId(edit),
            note,
            Register(edit));
    }

    private UiaTreeNodeInfo Walk(AutomationElement element, int depth, int maxDepth, ITreeWalker walker)
    {
        var node = new UiaTreeNodeInfo(Describe(element), depth);

        if (depth >= maxDepth)
        {
            node.TruncatedByDepth = SafeChildren(element, walker).Count != 0;
            return node;
        }

        foreach (AutomationElement child in SafeChildren(element, walker))
        {
            node.Children.Add(Walk(child, depth + 1, maxDepth, walker));
        }

        return node;
    }

    /// <summary>Deca elementa u zadatom pogledu: prvo dete, pa redom sledeća braća.</summary>
    private static IReadOnlyList<AutomationElement> SafeChildren(AutomationElement element, ITreeWalker walker)
    {
        var children = new List<AutomationElement>();

        try
        {
            AutomationElement? child = walker.GetFirstChild(element);

            while (child is not null)
            {
                children.Add(child);
                child = walker.GetNextSibling(child);
            }
        }
        catch (Exception)
        {
            // Kontrola je u međuvremenu nestala; ono što je do tada pokupljeno je tačniji odgovor
            // od rušenja celog obilaska.
        }

        return children;
    }

    private UiaElementInfo Describe(AutomationElement element, int? reuseId = null)
    {
        int id = reuseId ?? Register(element);

        return new UiaElementInfo(
            id,
            SafeValue(() => element.Properties.ControlType.Value.ToString(), "Unknown"),
            SafeName(element),
            SafeValue(() => element.Properties.AutomationId.Value, string.Empty) ?? string.Empty,
            SafeClassName(element),
            SafeValue(() => element.Properties.IsEnabled.Value, false),
            SafeValue(() => element.Properties.IsOffscreen.Value, false),
            SafeValue(() => element.Properties.BoundingRectangle.Value, System.Drawing.Rectangle.Empty),
            ReadPatterns(element),
            SafeFrameworkId(element),
            SafeValue(() => (long)element.Properties.NativeWindowHandle.Value, 0L),
            SafeRuntimeId(element));
    }

    private static UiaPatterns ReadPatterns(AutomationElement element)
    {
        UiaPatterns patterns = UiaPatterns.None;

        patterns |= Supported(() => element.Patterns.Invoke.IsSupported) ? UiaPatterns.Invoke : UiaPatterns.None;
        patterns |= Supported(() => element.Patterns.Value.IsSupported) ? UiaPatterns.Value : UiaPatterns.None;
        patterns |= Supported(() => element.Patterns.SelectionItem.IsSupported) ? UiaPatterns.SelectionItem : UiaPatterns.None;
        patterns |= Supported(() => element.Patterns.ExpandCollapse.IsSupported) ? UiaPatterns.ExpandCollapse : UiaPatterns.None;
        patterns |= Supported(() => element.Patterns.Toggle.IsSupported) ? UiaPatterns.Toggle : UiaPatterns.None;
        patterns |= Supported(() => element.Patterns.LegacyIAccessible.IsSupported) ? UiaPatterns.LegacyIAccessible : UiaPatterns.None;

        return patterns;
    }

    private static bool Supported(Func<bool> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string? ReadLegacyName(AutomationElement element)
    {
        try
        {
            return element.Patterns.LegacyIAccessible.TryGetPattern(out ILegacyIAccessiblePattern? legacy) && legacy is not null
                ? legacy.Name.ValueOrDefault
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private int Register(AutomationElement element)
    {
        int id = ++_nextId;
        _elements[id] = element;
        return id;
    }

    private static string SafeName(AutomationElement element)
        => SafeValue(() => element.Properties.Name.Value, string.Empty) ?? string.Empty;

    private static string SafeClassName(AutomationElement element)
        => SafeValue(() => element.Properties.ClassName.Value, string.Empty) ?? string.Empty;

    private static string SafeFrameworkId(AutomationElement element)
        => SafeValue(() => element.Properties.FrameworkId.Value, string.Empty) ?? string.Empty;

    private static string SafeRuntimeId(AutomationElement element)
        => SafeValue(
            () => string.Join(",", element.Properties.RuntimeId.Value),
            string.Empty) ?? string.Empty;

    private static T SafeValue<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            // Svojstvo koje kontrola ne podržava, ili prozor koji je nestao usred čitanja: nijedno
            // od toga nije razlog da obilazak stane.
            return fallback;
        }
    }

    private static string Join(string first, string second)
        => first.Length == 0 ? second : $"{first}; {second}";
}
