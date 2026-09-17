using System.Text;

namespace CableTest.App.UiAutomation;

/// <summary>Kako se poklapanja prikazuju u drvetu.</summary>
public enum UiaTreeFilterMode
{
    /// <summary>Celo drvo se vidi, a poklapanja su istaknuta.</summary>
    Highlight,

    /// <summary>Vide se samo poklapanja i čvorovi iznad njih.</summary>
    MatchesOnly
}

/// <summary>Ishod filtriranja drveta.</summary>
/// <param name="Matched">Id-evi čvorova koji se poklapaju sa traženim.</param>
/// <param name="Visible">Id-evi čvorova koji se prikazuju.</param>
/// <param name="Total">Ukupan broj čvorova u drvetu.</param>
public sealed record UiaTreeFilterResult(
    IReadOnlySet<int> Matched,
    IReadOnlySet<int> Visible,
    int Total)
{
    /// <summary>Koliko se čvorova prikazuje.</summary>
    public int Shown => Visible.Count;

    /// <summary>Koliko se čvorova poklapa sa traženim.</summary>
    public int Matches => Matched.Count;
}

/// <summary>
/// Pretraga i prebrojavanje čvorova UIA drveta.
/// </summary>
/// <remarks>
/// <para>
/// Sve ovde je čista funkcija nad već pročitanim snimkom drveta (<see cref="UiaTreeNodeInfo"/>):
/// nijedan poziv UIA i nijedan COM objekat. Zato se i filtriranje i brojanje mogu ispitati
/// testom, bez pokrenutog CableConnector-a — isto kao i <see cref="UiaNames"/>.
/// </para>
/// <para>
/// Poređenje ide kroz <see cref="UiaNames.Normalize"/>, istu funkciju kojom se traže i poznate
/// kontrole: razlika u veličini slova i višak razmaka ne smeju da sakriju čvor.
/// </para>
/// </remarks>
public static class UiaTreeSearch
{
    /// <summary>Stavka padajuće liste koja znači „ne filtriraj po tipu".</summary>
    public const string AllControlTypes = "svi";

    /// <summary>Natpis režima u kome se vidi celo drvo.</summary>
    public const string HighlightModeName = "istakni";

    /// <summary>Natpis režima u kome se vide samo poklapanja.</summary>
    public const string MatchesOnlyModeName = "samo poklapanja";

    /// <summary>Oba režima, redom kako stoje u padajućoj listi.</summary>
    public static IReadOnlyList<string> ModeNames { get; } = new[] { HighlightModeName, MatchesOnlyModeName };

    /// <summary>Režim po natpisu; nepoznat natpis znači „istakni".</summary>
    public static UiaTreeFilterMode ModeFromName(string? name)
        => string.Equals(name, MatchesOnlyModeName, StringComparison.Ordinal)
            ? UiaTreeFilterMode.MatchesOnly
            : UiaTreeFilterMode.Highlight;

    /// <summary>Natpis režima.</summary>
    public static string NameOfMode(UiaTreeFilterMode mode)
        => mode == UiaTreeFilterMode.MatchesOnly ? MatchesOnlyModeName : HighlightModeName;

    /// <summary>Da li izbor u padajućoj listi znači „svi tipovi".</summary>
    public static bool IsAllControlTypes(string? controlType)
        => string.IsNullOrWhiteSpace(controlType)
           || string.Equals(controlType, AllControlTypes, StringComparison.Ordinal);

    /// <summary>Da li se ništa ne traži — ni tekst ni tip.</summary>
    public static bool IsEmpty(string? query, string? controlType)
        => UiaNames.Normalize(query).Length == 0 && IsAllControlTypes(controlType);

    /// <summary>
    /// Da li se element poklapa sa traženim.
    /// </summary>
    /// <remarks>
    /// Tip mora da se poklopi tačno — bira se iz spiska tipova koji u drvetu zaista postoje — a
    /// tekst se traži kao „sadrži" kroz <see cref="UiaElementInfo.Name"/>,
    /// <see cref="UiaElementInfo.AutomationId"/>, <see cref="UiaElementInfo.ControlType"/>,
    /// <see cref="UiaElementInfo.ClassName"/> i <see cref="UiaElementInfo.FrameworkId"/>.
    /// </remarks>
    public static bool Matches(UiaElementInfo element, string? query, string? controlType)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!IsAllControlTypes(controlType)
            && !string.Equals(element.ControlType, controlType, StringComparison.Ordinal))
        {
            return false;
        }

        if (UiaNames.Normalize(query).Length == 0)
        {
            // Bez teksta se traži samo po tipu; kad ni tipa nema, poklapa se sve.
            return true;
        }

        return UiaNames.MatchesNormalizedContains(element.Name, query)
               || UiaNames.MatchesNormalizedContains(element.AutomationId, query)
               || UiaNames.MatchesNormalizedContains(element.ControlType, query)
               || UiaNames.MatchesNormalizedContains(element.ClassName, query)
               || UiaNames.MatchesNormalizedContains(element.FrameworkId, query);
    }

    /// <summary>
    /// Filtrira drvo: šta se poklapa i šta se od toga prikazuje.
    /// </summary>
    /// <remarks>
    /// U režimu <see cref="UiaTreeFilterMode.Highlight"/> prikazuje se celo drvo, pa je
    /// prikazanih uvek koliko i ukupno. U režimu <see cref="UiaTreeFilterMode.MatchesOnly"/>
    /// prikazuju se poklapanja i svi čvorovi iznad njih — bez roditelja se ne bi videlo gde u
    /// drvetu pogodak stoji.
    /// </remarks>
    public static UiaTreeFilterResult Apply(
        UiaTreeNodeInfo? root,
        string? query,
        string? controlType,
        UiaTreeFilterMode mode)
    {
        var matched = new HashSet<int>();
        var visible = new HashSet<int>();
        int total = 0;

        if (root is null)
        {
            return new UiaTreeFilterResult(matched, visible, 0);
        }

        bool nothingAsked = IsEmpty(query, controlType);

        Walk(root);

        return new UiaTreeFilterResult(matched, visible, total);

        // Vraća da li se čvor prikazuje. Deca se obilaze uvek, i kad je već poznato da se čvor
        // prikazuje: bez toga se ne bi prebrojalo celo drvo.
        bool Walk(UiaTreeNodeInfo node)
        {
            total++;

            bool self = !nothingAsked && Matches(node.Element, query, controlType);

            if (self)
            {
                matched.Add(node.Element.Id);
            }

            bool childVisible = false;

            foreach (UiaTreeNodeInfo child in node.Children)
            {
                childVisible |= Walk(child);
            }

            bool shown = nothingAsked || mode == UiaTreeFilterMode.Highlight || self || childVisible;

            if (shown)
            {
                visible.Add(node.Element.Id);
            }

            return shown;
        }
    }

    /// <summary>Koliko drvo ima čvorova.</summary>
    public static int Count(UiaTreeNodeInfo? node)
        => node is null ? 0 : 1 + node.Children.Sum(Count);

    /// <summary>Najveća dubina do koje je obilazak zaista stigao; koren je 0.</summary>
    public static int MaxDepth(UiaTreeNodeInfo? node)
        => node is null
            ? 0
            : node.Children.Count == 0
                ? node.Depth
                : node.Children.Max(MaxDepth);

    /// <summary>
    /// Tipovi kontrola koji u drvetu zaista postoje, poređani po abecedi, sa „svi" na početku.
    /// </summary>
    public static IReadOnlyList<string> ControlTypes(UiaTreeNodeInfo? root)
    {
        var types = new SortedSet<string>(StringComparer.Ordinal);

        Collect(root);

        var list = new List<string>(types.Count + 1) { AllControlTypes };
        list.AddRange(types);
        return list;

        void Collect(UiaTreeNodeInfo? node)
        {
            if (node is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(node.Element.ControlType))
            {
                types.Add(node.Element.ControlType);
            }

            foreach (UiaTreeNodeInfo child in node.Children)
            {
                Collect(child);
            }
        }
    }

    /// <summary>Broj prikazanih i ukupan broj čvorova, u obliku za prikaz.</summary>
    public static string CountText(int shown, int total)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Prikazano {shown} od {total} čvorova.");

    /// <summary>
    /// Jedan red u log: šta je traženo, u kom obliku se zaista poredilo i šta je od toga ispalo.
    /// </summary>
    /// <remarks>
    /// Normalizovan oblik se ispisuje zato što se poredi baš on, a ne ono što je otkucano:
    /// razmak na kraju ili dvostruki razmak unutra inače izgledaju kao da su nestali sami od sebe.
    /// </remarks>
    public static string DiagnosticText(
        string? query,
        string? controlType,
        UiaTreeFilterMode mode,
        UiaTreeFilterResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string normalized = UiaNames.Normalize(query);
        string tip = IsAllControlTypes(controlType) ? AllControlTypes : controlType!;

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"pretraga: upit=\"{query}\" → normalizovano \"{normalized}\"; ControlType={tip}; " +
            $"režim={NameOfMode(mode)}; poklapanja {result.Matches}; " +
            $"prikazano {result.Shown} od {result.Total} čvorova.");
    }

    /// <summary>Imena prvih nekoliko čvorova, redom kojim se drvo obilazi.</summary>
    /// <remarks>
    /// Ispisuje se kad pretraga nema nijedan pogodak: tek se iz imena vidi nad čim se poredilo —
    /// da li kontrole uopšte imaju imena, ili glase drugačije nego što se očekivalo.
    /// </remarks>
    public static IReadOnlyList<string> FirstNames(UiaTreeNodeInfo? root, int max = 10)
    {
        var names = new List<string>();

        if (max <= 0)
        {
            return names;
        }

        Collect(root);
        return names;

        void Collect(UiaTreeNodeInfo? node)
        {
            if (node is null || names.Count >= max)
            {
                return;
            }

            names.Add(node.Element.Name);

            foreach (UiaTreeNodeInfo child in node.Children)
            {
                Collect(child);
            }
        }
    }

    /// <summary>Imena prvih nekoliko čvorova, u obliku za log.</summary>
    public static string FirstNamesText(UiaTreeNodeInfo? root, int max = 10)
    {
        IReadOnlyList<string> names = FirstNames(root, max);

        if (names.Count == 0)
        {
            return "  u stablu nema nijednog čvora.";
        }

        string ispis = string.Join(
            " | ",
            names.Select(n => string.IsNullOrWhiteSpace(n) ? "(bez imena)" : $"\"{n}\""));

        return $"  prvih {names.Count} imena u stablu: {ispis}";
    }

    /// <summary>
    /// Put od korena do čvora sa zadatim <see cref="UiaElementInfo.Id"/>, uključujući i sam čvor.
    /// </summary>
    /// <remarks>
    /// Odavde se čita lanac roditelja: sve osim poslednjeg u nizu su preci, redom od prozora
    /// nadole. Prazan niz znači da čvora u ovom drvetu nema.
    /// </remarks>
    public static IReadOnlyList<UiaTreeNodeInfo> Path(UiaTreeNodeInfo? root, int id)
    {
        var path = new List<UiaTreeNodeInfo>();

        return Walk(root) ? path : Array.Empty<UiaTreeNodeInfo>();

        bool Walk(UiaTreeNodeInfo? node)
        {
            if (node is null)
            {
                return false;
            }

            path.Add(node);

            if (node.Element.Id == id)
            {
                return true;
            }

            foreach (UiaTreeNodeInfo child in node.Children)
            {
                if (Walk(child))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }
    }

    /// <summary>Čvor sa zadatim RuntimeId-em, ili <c>null</c> ako ga u drvetu nema.</summary>
    /// <remarks>
    /// RuntimeId je jedino što se ne menja između dva čitanja istog elementa: redni broj pod kojim
    /// ga probe pamti dodeljuje se iznova pri svakom obilasku, pa po njemu poređenje ne bi radilo.
    /// </remarks>
    public static UiaTreeNodeInfo? FindByRuntimeId(UiaTreeNodeInfo? root, string? runtimeId)
    {
        if (root is null || string.IsNullOrWhiteSpace(runtimeId))
        {
            return null;
        }

        if (string.Equals(root.Element.RuntimeId, runtimeId, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (UiaTreeNodeInfo child in root.Children)
        {
            UiaTreeNodeInfo? hit = FindByRuntimeId(child, runtimeId);

            if (hit is not null)
            {
                return hit;
            }
        }

        return null;
    }
}

/// <summary>
/// Čvor i grana u obliku teksta — ono što odlazi u ostavu.
/// </summary>
/// <remarks>
/// Odvojeno od prikaza namerno: tekst koji operater prekopira i tekst koji ide u log prave se
/// iz istog snimka, pa se ne mogu razići.
/// </remarks>
public static class UiaTreeText
{
    /// <summary>Svi podaci o jednom čvoru, red po red.</summary>
    public static string Node(UiaElementInfo element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var builder = new StringBuilder();

        builder.AppendLine($"ControlType: {element.ControlType}");
        builder.AppendLine($"Name: {element.Name}");
        builder.AppendLine($"AutomationId: {element.AutomationId}");
        builder.AppendLine($"ClassName: {element.ClassName}");
        builder.AppendLine($"FrameworkId: {element.FrameworkIdText}");
        builder.AppendLine($"NativeWindowHandle: {element.NativeWindowHandleText}");
        builder.AppendLine($"IsEnabled: {element.IsEnabled}");
        builder.AppendLine($"IsOffscreen: {element.IsOffscreen}");
        builder.AppendLine($"Bounds: {element.BoundsText}");
        builder.AppendLine($"Obrasci: {element.PatternsText}");
        builder.AppendLine($"RuntimeId: {element.RuntimeIdText}");

        return builder.ToString();
    }

    /// <summary>Čelo reda: tip kontrole i ime.</summary>
    public static string Head(UiaElementInfo element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return $"{element.ControlType} — {element.DisplayName}";
    }

    /// <summary>Ostali podaci o kontroli, u uglastim zagradama.</summary>
    public static string Data(UiaElementInfo element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return $"[AutomationId=\"{element.AutomationId}\"; ClassName=\"{element.ClassName}\"; " +
               $"FrameworkId=\"{element.FrameworkId}\"; HWND={element.NativeWindowHandleText}; " +
               $"IsEnabled={element.IsEnabled}; Obrasci={element.PatternsText}; " +
               $"Bounds={element.BoundsText}; RuntimeId={element.RuntimeIdText}]";
    }

    /// <summary>Jedan red grane: tip, ime i ono po čemu se kontrola prepoznaje.</summary>
    public static string Line(UiaElementInfo element) => $"{Head(element)}  {Data(element)}";

    /// <summary>
    /// Lanac roditelja, od prozora nadole; svaki nivo je uvučen za dva razmaka.
    /// </summary>
    /// <remarks>
    /// Isti red kao u <see cref="Branch"/>, pa se lanac iz loga i grana iz ostave čitaju isto.
    /// </remarks>
    public static string Chain(IEnumerable<UiaElementInfo> chain)
    {
        ArgumentNullException.ThrowIfNull(chain);

        var builder = new StringBuilder();
        int level = 0;

        foreach (UiaElementInfo element in chain)
        {
            builder.Append(' ', level * 2).AppendLine(Line(element));
            level++;
        }

        return builder.ToString();
    }

    /// <summary>Čvor sa svom decom; svaki nivo je uvučen za dva razmaka.</summary>
    public static string Branch(UiaTreeNodeInfo node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var builder = new StringBuilder();
        Write(node, 0);
        return builder.ToString();

        void Write(UiaTreeNodeInfo current, int level)
        {
            builder.Append(' ', level * 2).AppendLine(Line(current.Element));

            foreach (UiaTreeNodeInfo child in current.Children)
            {
                Write(child, level + 1);
            }
        }
    }
}
