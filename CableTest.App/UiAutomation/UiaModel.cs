namespace CableTest.App.UiAutomation;

/// <summary>Koji od dva prozora CableConnector-a.</summary>
public enum UiaWindowKind
{
    /// <summary>Glavni prozor; naslov počinje sa „CableConnector".</summary>
    Main,

    /// <summary>Prozorče „Edit"; naslov je tačno „Edit".</summary>
    Edit
}

/// <summary>
/// Koji se pogled na UIA drvo obilazi.
/// </summary>
/// <remarks>
/// <para>
/// UIA isto drvo prikazuje na tri načina. <see cref="Raw"/> je drvo onakvo kakvo jeste — sa svim
/// slojevima koje okvir napravi, uključujući WPF <c>Grid</c>, <c>Border</c> i <c>Panel</c>.
/// <see cref="Control"/> izbacuje sve što nije kontrola, a <see cref="Content"/> ostavlja samo
/// ono što nosi sadržaj.
/// </para>
/// <para>
/// Zato je podrazumevan Raw: ovde se traži šta je od tuđeg programa uopšte vidljivo, a ne šta je
/// od toga lepo za čitanje. Ono što se u Control pogledu ne vidi ne znači da ne postoji.
/// </para>
/// </remarks>
public enum UiaViewKind
{
    /// <summary>Sve, sa svim slojevima okvira.</summary>
    Raw,

    /// <summary>Samo ono što UIA smatra kontrolom.</summary>
    Control,

    /// <summary>Samo ono što nosi sadržaj.</summary>
    Content
}

/// <summary>Šta je kontrola po nameni — od toga zavisi kako se sa njom radi.</summary>
public enum UiaControlKind
{
    /// <summary>Dugme; pokreće se.</summary>
    Button,

    /// <summary>Stavka glavnog menija; pokreće se.</summary>
    MenuItem,

    /// <summary>Kartica; bira se.</summary>
    Tab,

    /// <summary>Polje; čita se i upisuje.</summary>
    Field
}

/// <summary>Obrasci koji nas ovde zanimaju.</summary>
/// <remarks>
/// Namerno samo ovih šest: to je tačno ono što se traži da se vidi u drvetu, a ne ceo spisak
/// obrazaca koje UIA poznaje.
/// </remarks>
[Flags]
public enum UiaPatterns
{
    None = 0,
    Invoke = 1,
    Value = 2,
    SelectionItem = 4,
    ExpandCollapse = 8,
    Toggle = 16,
    LegacyIAccessible = 32
}

/// <summary>Ispis obrazaca u obliku za prikaz i za log.</summary>
public static class UiaPatternText
{
    /// <summary>Spisak podržanih obrazaca, ili „—" kad nijedan nije podržan.</summary>
    public static string Describe(UiaPatterns patterns)
    {
        if (patterns == UiaPatterns.None)
        {
            return "—";
        }

        var names = new List<string>(6);

        foreach (UiaPatterns flag in Enum.GetValues<UiaPatterns>())
        {
            if (flag != UiaPatterns.None && patterns.HasFlag(flag))
            {
                names.Add(flag.ToString());
            }
        }

        return string.Join(", ", names);
    }
}

/// <summary>
/// Snimak jednog elementa UIA drveta.
/// </summary>
/// <remarks>
/// Obična vrednost, bez ijednog COM objekta: pravi se na niti koja radi sa UIA, a prikazuje na
/// GUI niti. Sam element ostaje u <see cref="CableConnectorProbe"/>, dohvata se preko
/// <see cref="Id"/> i nikada ne prelazi granicu niti.
/// </remarks>
/// <param name="Id">Redni broj pod kojim probe čuva stvarni element.</param>
/// <param name="ControlType">ControlType, npr. „Button".</param>
/// <param name="Name">Ime kako ga UIA prijavljuje.</param>
/// <param name="AutomationId">AutomationId, ili prazno.</param>
/// <param name="ClassName">Ime Win32 klase kontrole, ili prazno.</param>
/// <param name="IsEnabled">Da li je kontrola dostupna.</param>
/// <param name="IsOffscreen">Da li je van vidljivog dela ekrana.</param>
/// <param name="Bounds">Okvir kontrole na ekranu.</param>
/// <param name="Patterns">Obrasci koje kontrola podržava.</param>
/// <param name="FrameworkId">Čime je kontrola napravljena: <c>Win32</c>, <c>WinForm</c>, <c>WPF</c>…</param>
/// <param name="NativeWindowHandle">HWND kontrole, ili 0 kad kontrola nije zaseban prozor.</param>
/// <param name="RuntimeId">
/// RuntimeId u obliku <c>„42,131072,4"</c>, ili prazno. Jedini podatak po kome se dva snimka
/// istog elementa mogu prepoznati kao isti element — po njemu se uhvaćeni element traži u drvetu.
/// </param>
/// <param name="LegacyName">Ime iz LegacyIAccessible, ako je pročitano; inače <c>null</c>.</param>
public sealed record UiaElementInfo(
    int Id,
    string ControlType,
    string Name,
    string AutomationId,
    string ClassName,
    bool IsEnabled,
    bool IsOffscreen,
    System.Drawing.Rectangle Bounds,
    UiaPatterns Patterns,
    string FrameworkId = "",
    long NativeWindowHandle = 0,
    string RuntimeId = "",
    string? LegacyName = null)
{
    /// <summary>Okvir u obliku „x,y 120×28".</summary>
    public string BoundsText => Bounds.IsEmpty
        ? "—"
        : string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Bounds.X},{Bounds.Y} {Bounds.Width}×{Bounds.Height}");

    /// <summary>Spisak podržanih obrazaca.</summary>
    public string PatternsText => UiaPatternText.Describe(Patterns);

    /// <summary>FrameworkId za prikaz; prazan se vidi kao „—".</summary>
    public string FrameworkIdText => string.IsNullOrWhiteSpace(FrameworkId) ? "—" : FrameworkId;

    /// <summary>HWND u heksadekadnom obliku, kako ga prijavljuju i alati za prozore; 0 je „—".</summary>
    public string NativeWindowHandleText => NativeWindowHandle == 0
        ? "—"
        : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"0x{NativeWindowHandle:X}");

    /// <summary>Ime za prikaz — prazno ime se vidi kao „(bez imena)".</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(bez imena)" : Name;

    /// <summary>Jedan red za log: sve što se o pogođenom elementu mora zapisati.</summary>
    public string LogLine => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"ControlType={ControlType}; Name=\"{Name}\"; AutomationId=\"{AutomationId}\"; " +
        $"ClassName=\"{ClassName}\"; FrameworkId=\"{FrameworkId}\"; HWND={NativeWindowHandleText}; " +
        $"IsEnabled={IsEnabled}; IsOffscreen={IsOffscreen}; " +
        $"Bounds={BoundsText}; Patterns={PatternsText}; RuntimeId={RuntimeIdText}");

    /// <summary>RuntimeId za prikaz; prazan se vidi kao „—".</summary>
    public string RuntimeIdText => string.IsNullOrWhiteSpace(RuntimeId) ? "—" : RuntimeId;
}

/// <summary>Čvor UIA drveta sa svojom decom.</summary>
public sealed class UiaTreeNodeInfo
{
    public UiaTreeNodeInfo(UiaElementInfo element, int depth)
    {
        ArgumentNullException.ThrowIfNull(element);
        Element = element;
        Depth = depth;
    }

    /// <summary>Podaci o samom elementu.</summary>
    public UiaElementInfo Element { get; }

    /// <summary>Dubina u odnosu na koren obilaska; koren je 0.</summary>
    public int Depth { get; }

    /// <summary>Deca; prazno ako ih nema ili je dostignuta granica dubine.</summary>
    public List<UiaTreeNodeInfo> Children { get; } = new();

    /// <summary>Da li je obilazak stao ovde zbog granice dubine, a dece ima.</summary>
    public bool TruncatedByDepth { get; set; }
}

/// <summary>Stanje jednog od dva prozora CableConnector-a.</summary>
/// <param name="Kind">Koji prozor.</param>
/// <param name="Found">Da li je pronađen.</param>
/// <param name="ProcessId">PID procesa, ili 0.</param>
/// <param name="Title">Tačan naslov prozora.</param>
/// <param name="ClassName">Ime klase prozora.</param>
/// <param name="FrameworkId">Čime je prozor napravljen: <c>Win32</c>, <c>WinForm</c>, <c>WPF</c>…</param>
/// <param name="Note">Kako je pronađen, ili zašto nije.</param>
/// <param name="RootId">Redni broj pod kojim probe čuva sam prozor, ili -1.</param>
public sealed record UiaWindowInfo(
    UiaWindowKind Kind,
    bool Found,
    int ProcessId,
    string Title,
    string ClassName,
    string FrameworkId,
    string Note,
    int RootId = -1)
{
    /// <summary>Prozor koji nije pronađen.</summary>
    public static UiaWindowInfo NotFound(UiaWindowKind kind, string note)
        => new(kind, false, 0, "—", "—", "—", note);
}

/// <summary>
/// Element uhvaćen ispod pokazivača miša, sa celim lancem roditelja.
/// </summary>
/// <param name="Point">Tačka na ekranu sa koje je uzet.</param>
/// <param name="Element">Sam element.</param>
/// <param name="Chain">
/// Lanac od prozora nadole do elementa, uključujući i njega samog: prvi je najviši predak koji je
/// dohvaćen, poslednji je uhvaćeni element.
/// </param>
/// <param name="Note">Dodatno objašnjenje: gde je lanac stao i zašto.</param>
public sealed record UiaCaptureInfo(
    System.Drawing.Point Point,
    UiaElementInfo Element,
    IReadOnlyList<UiaElementInfo> Chain,
    string Note)
{
    /// <summary>Tačka u obliku „x,y".</summary>
    public string PointText => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"{Point.X},{Point.Y}");
}
