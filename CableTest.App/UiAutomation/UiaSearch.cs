namespace CableTest.App.UiAutomation;

/// <summary>Način na koji se kontrola traži.</summary>
/// <remarks>
/// Redosled vrednosti je ujedno i redosled pokušaja — vidi <see cref="UiaSearchPlan.For"/>.
/// </remarks>
public enum UiaSearchStrategy
{
    /// <summary>AutomationId, ako je poznat iz ranijeg otkrivanja. Najpouzdaniji.</summary>
    AutomationId,

    /// <summary>Ime, tačno poklapanje.</summary>
    NameExact,

    /// <summary>Ime, „sadrži" nad očišćenim imenom.</summary>
    NameNormalizedContains,

    /// <summary>Ime iz LegacyIAccessible — poslednja nada za starije Win32 kontrole.</summary>
    LegacyName
}

/// <summary>Jedna kontrola koja se traži.</summary>
/// <param name="LogicalName">Ključ pod kojim se pamti u mapi, npr. <c>Main.START</c>.</param>
/// <param name="Window">U kom prozoru se traži.</param>
/// <param name="Kind">Dugme, stavka menija, kartica ili polje.</param>
/// <param name="Name">Natpis onakav kakav stoji u CableConnector-u.</param>
/// <param name="NeedsConfirmation">Da li pokretanje pokreće stvarni hardver, pa traži potvrdu.</param>
/// <param name="Writable">Da li se u kontrolu i upisuje, a ne samo čita.</param>
/// <param name="KnownAutomationId">AutomationId iz ranijeg otkrivanja, ako postoji.</param>
public sealed record UiaControlTarget(
    string LogicalName,
    UiaWindowKind Window,
    UiaControlKind Kind,
    string Name,
    bool NeedsConfirmation = false,
    bool Writable = false,
    string? KnownAutomationId = null)
{
    /// <summary>Da li je AutomationId poznat, pa ima smisla njime i početi.</summary>
    public bool HasKnownAutomationId => !string.IsNullOrWhiteSpace(KnownAutomationId);
}

/// <summary>Skupina kontrola u prikazu i u mapi — odgovara jednom delu CableConnector-a.</summary>
public sealed record UiaControlGroup(string Title, UiaWindowKind Window, IReadOnlyList<UiaControlTarget> Targets);

/// <summary>
/// Red kojim se kontrola traži.
/// </summary>
/// <remarks>
/// Uvek isti, i uvek se zapisuje koji je korak pogodio — jer je cilj ove strane upravo da se
/// sazna šta je od CableConnector-a uopšte dohvatljivo i čime.
/// </remarks>
public static class UiaSearchPlan
{
    /// <summary>Koraci za zadatu kontrolu; AutomationId otpada kad nije poznat.</summary>
    public static IReadOnlyList<UiaSearchStrategy> For(UiaControlTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return For(target.HasKnownAutomationId);
    }

    /// <summary>Koraci, u redosledu pokušaja.</summary>
    public static IReadOnlyList<UiaSearchStrategy> For(bool hasKnownAutomationId)
        => hasKnownAutomationId
            ? new[]
            {
                UiaSearchStrategy.AutomationId,
                UiaSearchStrategy.NameExact,
                UiaSearchStrategy.NameNormalizedContains,
                UiaSearchStrategy.LegacyName
            }
            : new[]
            {
                UiaSearchStrategy.NameExact,
                UiaSearchStrategy.NameNormalizedContains,
                UiaSearchStrategy.LegacyName
            };

    /// <summary>Svi koraci, bez obzira na to da li je AutomationId poznat.</summary>
    public static IReadOnlyList<UiaSearchStrategy> All => For(hasKnownAutomationId: true);

    /// <summary>Da li zadati element odgovara traženoj kontroli po zadatom koraku.</summary>
    /// <remarks>
    /// <see cref="UiaSearchStrategy.LegacyName"/> radi nad <see cref="UiaElementInfo.LegacyName"/>;
    /// dok to ime nije pročitano (<c>null</c>) korak ne može da pogodi ništa.
    /// </remarks>
    public static bool Matches(UiaSearchStrategy strategy, UiaElementInfo element, UiaControlTarget target)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(target);

        return strategy switch
        {
            UiaSearchStrategy.AutomationId =>
                target.HasKnownAutomationId && UiaNames.MatchesExact(element.AutomationId, target.KnownAutomationId),
            UiaSearchStrategy.NameExact =>
                UiaNames.MatchesExact(element.Name, target.Name),
            UiaSearchStrategy.NameNormalizedContains =>
                UiaNames.MatchesNormalizedContains(element.Name, target.Name),
            UiaSearchStrategy.LegacyName =>
                UiaNames.MatchesExact(element.LegacyName, target.Name)
                || UiaNames.MatchesNormalizedContains(element.LegacyName, target.Name),
            _ => false
        };
    }

    /// <summary>Zašto korak nije ni pokušan, ili prazno ako jeste.</summary>
    public static string SkipReason(UiaSearchStrategy strategy, UiaControlTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return strategy == UiaSearchStrategy.AutomationId && !target.HasKnownAutomationId
            ? "AutomationId nije poznat iz ranijeg otkrivanja"
            : string.Empty;
    }
}

/// <summary>Jedan pokušaj pretrage — šta je probano i sa kakvim ishodom.</summary>
/// <param name="Strategy">Koji korak.</param>
/// <param name="SkipReason">Zašto korak nije ni pokušan; prazno ako jeste.</param>
/// <param name="Examined">Koliko je elemenata pregledano.</param>
/// <param name="Matched">Koliko je elemenata pogođeno.</param>
/// <param name="Element">Prvi pogođeni element, ili <c>null</c>.</param>
public sealed record UiaSearchAttempt(
    UiaSearchStrategy Strategy,
    string SkipReason,
    int Examined,
    int Matched,
    UiaElementInfo? Element)
{
    /// <summary>Da li je korak pogodio bar jedan element.</summary>
    public bool Hit => Element is not null;

    /// <summary>Red za log.</summary>
    public string LogLine => SkipReason.Length != 0
        ? $"{Strategy}: preskočeno — {SkipReason}"
        : Hit
            ? $"{Strategy}: POGODAK ({Matched} od {Examined} pregledanih)"
            : $"{Strategy}: bez pogotka (pregledano {Examined})";
}

/// <summary>Izveštaj o jednoj pretrazi — sve što se o njoj upisuje u log.</summary>
/// <param name="Target">Šta se tražilo.</param>
/// <param name="Attempts">Koraci, redom.</param>
/// <param name="Element">Pogođeni element, ili <c>null</c>.</param>
/// <param name="Strategy">Korak koji je pogodio, ili <c>null</c>.</param>
/// <param name="Nearest">Imena najbližih kandidata — objašnjenje zašto pretraga nije uspela.</param>
/// <param name="ElapsedMs">Koliko je pretraga trajala.</param>
public sealed record UiaSearchReport(
    UiaControlTarget Target,
    IReadOnlyList<UiaSearchAttempt> Attempts,
    UiaElementInfo? Element,
    UiaSearchStrategy? Strategy,
    IReadOnlyList<string> Nearest,
    long ElapsedMs)
{
    /// <summary>Da li je kontrola pronađena.</summary>
    public bool Found => Element is not null;
}
