using System.Text.Json;
using System.Text.Json.Serialization;

namespace CableTest.App.UiAutomation;

/// <summary>Jedan pogodak: logičko ime i sve što o kontroli treba zapamtiti.</summary>
/// <param name="LogicalName">Ključ, npr. <c>Main.START</c>.</param>
/// <param name="Window">Prozor u kome je nađena.</param>
/// <param name="ControlType">ControlType.</param>
/// <param name="AutomationId">AutomationId, ili prazno.</param>
/// <param name="Name">Ime koje je UIA prijavio.</param>
/// <param name="ClassName">Ime klase kontrole.</param>
/// <param name="FrameworkId">Čime je kontrola napravljena: <c>Win32</c>, <c>WinForm</c>, <c>WPF</c>…</param>
/// <param name="IsEnabled">Da li je kontrola bila dostupna u trenutku otkrivanja.</param>
/// <param name="Patterns">Podržani obrasci.</param>
/// <param name="FoundBy">Korak pretrage koji je pogodio.</param>
public sealed record UiaControlMapEntry(
    string LogicalName,
    string Window,
    string ControlType,
    string AutomationId,
    string Name,
    string ClassName,
    string FrameworkId,
    bool IsEnabled,
    string Patterns,
    string FoundBy);

/// <summary>
/// Mapa otkrivenih kontrola: logičko ime → ono što je zaista nađeno.
/// </summary>
/// <remarks>
/// <para>
/// Živi samo u pamćenju, dokle traje rad aplikacije. Smisao joj je da se jednom pronađena
/// kontrola ne traži ponovo kroz sva četiri koraka, i da se otkriveno može izvesti u JSON —
/// taj JSON kasnije popunjava <see cref="UiaControlTarget.KnownAutomationId"/> pravog gateway-a.
/// </para>
/// <para>
/// Ova strana ne pravi gateway i ne zna za njega; samo ostavlja zapis.
/// </para>
/// </remarks>
public sealed class UiaControlMap
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<string, UiaControlMapEntry> _entries = new(StringComparer.Ordinal);

    /// <summary>Koliko je kontrola do sada pogođeno.</summary>
    public int Count => _entries.Count;

    /// <summary>Upisuje pogodak; ponovni pogodak iste kontrole menja stari zapis.</summary>
    public void Remember(UiaControlTarget target, UiaElementInfo element, UiaSearchStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(element);

        _entries[target.LogicalName] = new UiaControlMapEntry(
            target.LogicalName,
            target.Window.ToString(),
            element.ControlType,
            element.AutomationId,
            element.Name,
            element.ClassName,
            element.FrameworkId,
            element.IsEnabled,
            element.PatternsText,
            strategy.ToString());
    }

    /// <summary>Zapamćeni AutomationId, ili <c>null</c> ako kontrola još nije pogođena.</summary>
    public string? KnownAutomationId(string logicalName)
        => _entries.TryGetValue(logicalName, out UiaControlMapEntry? entry)
           && !string.IsNullOrWhiteSpace(entry.AutomationId)
            ? entry.AutomationId
            : null;

    /// <summary>Da li je kontrola već pogođena.</summary>
    public bool Contains(string logicalName) => _entries.ContainsKey(logicalName);

    /// <summary>Svi zapisi, poređani po logičkom imenu.</summary>
    public IReadOnlyList<UiaControlMapEntry> Entries()
        => _entries.Values.OrderBy(e => e.LogicalName, StringComparer.Ordinal).ToArray();

    /// <summary>Briše mapu.</summary>
    public void Clear() => _entries.Clear();

    /// <summary>Mapa kao JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(Entries(), Options);
}
