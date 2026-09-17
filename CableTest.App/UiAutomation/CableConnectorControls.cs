using System.Text.RegularExpressions;

namespace CableTest.App.UiAutomation;

/// <summary>
/// Verzija CableConnector-a, pročitana iz naslova glavnog prozora.
/// </summary>
/// <remarks>
/// Naslov je jedino mesto na kome verzija piše, a od verzije zavisi da li se natpisi dugmadi i
/// raspored kartica uopšte poklapaju sa onim što ova strana očekuje. Zato se verzija čita i
/// poredi sa <see cref="Expected"/>, pa se razlika vidno javlja umesto da se otkrije tek kad
/// nijedno dugme ne bude pronađeno.
/// </remarks>
public static partial class CableConnectorVersion
{
    /// <summary>Čime počinje naslov glavnog prozora.</summary>
    public const string WindowTitlePrefix = "CableConnector";

    /// <summary>Naslov prozorčeta koje se traži pod tačnim imenom.</summary>
    public const string EditWindowTitle = "Edit";

    /// <summary>Verzija na kojoj je ova strana rađena.</summary>
    public const string Expected = "V3.12.17";

    /// <summary>Verzija iz naslova, npr. <c>V3.12.17</c>, ili <c>null</c> ako je u naslovu nema.</summary>
    public static string? Parse(string? windowTitle)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            return null;
        }

        Match match = VersionPattern().Match(windowTitle);
        return match.Success ? match.Value : null;
    }

    /// <summary>Da li je verzija upravo ona na kojoj je strana rađena.</summary>
    public static bool IsExpected(string? version)
        => version is not null && string.Equals(version, Expected, StringComparison.OrdinalIgnoreCase);

    /// <summary>Da li naslov pripada glavnom prozoru CableConnector-a.</summary>
    public static bool IsMainWindowTitle(string? title)
        => !string.IsNullOrWhiteSpace(title)
           && title.TrimStart().StartsWith(WindowTitlePrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Da li je naslov tačno „Edit".</summary>
    public static bool IsEditWindowTitle(string? title)
        => title is not null && string.Equals(title.Trim(), EditWindowTitle, StringComparison.Ordinal);

    // „V" pa broj, pa koliko god delova odvojenih tačkom: V3, V3.12, V3.12.17.
    [GeneratedRegex(@"V\d+(?:\.\d+)*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}

/// <summary>
/// Spisak kontrola CableConnector-a koje ova strana pokušava da dohvati.
/// </summary>
/// <remarks>
/// <para>
/// Natpisi su prepisani onako kako stoje u samom programu, sa tačkama i razmacima uključujući i
/// <c>„Download ( AUTO Save and Exit )"</c>. Menjanje natpisa ovde menja i ono što se traži —
/// zato se ne „sređuju".
/// </para>
/// <para>
/// <see cref="UiaControlTarget.KnownAutomationId"/> je za sada svuda prazan. Tu upisuje ono što
/// se otkrije i izveze dugmetom „Izvezi mapu"; od tada pretraga počinje od AutomationId-a.
/// </para>
/// </remarks>
public static class CableConnectorControls
{
    /// <summary>Natpis dugmeta koje pokreće merenje.</summary>
    public const string StartName = "START";

    /// <summary>Natpis dugmeta koje upisuje spec u tester.</summary>
    public const string DownloadName = "Download ( AUTO Save and Exit )";

    /// <summary>Sve skupine kontrola, redom kako se prikazuju.</summary>
    public static IReadOnlyList<UiaControlGroup> Groups { get; } = new[]
    {
        new UiaControlGroup("Glavni prozor", UiaWindowKind.Main, new[]
        {
            // START pokreće stvarno merenje na testeru — zato traži potvrdu.
            Button(UiaWindowKind.Main, StartName, needsConfirmation: true),
            Button(UiaWindowKind.Main, "Zero"),
            Button(UiaWindowKind.Main, "Connect Set"),
            Button(UiaWindowKind.Main, "Data File"),
            Button(UiaWindowKind.Main, "Add spec."),
            Button(UiaWindowKind.Main, "Open"),
            Button(UiaWindowKind.Main, "Save"),
            Button(UiaWindowKind.Main, "Close"),
            Button(UiaWindowKind.Main, "Modify")
        }),

        new UiaControlGroup("Glavni meni", UiaWindowKind.Main, new[]
        {
            MenuItem("File"),
            MenuItem("Settings"),
            MenuItem("View"),
            MenuItem("Help")
        }),

        new UiaControlGroup("Prozor „Edit“ — dugmad", UiaWindowKind.Edit, new[]
        {
            Button(UiaWindowKind.Edit, "New spec. file"),
            Button(UiaWindowKind.Edit, "Save spec. file"),
            Button(UiaWindowKind.Edit, "Save and Exit"),
            // Download upisuje spec u tester — takođe stvarni hardver, takođe uz potvrdu.
            Button(UiaWindowKind.Edit, DownloadName, needsConfirmation: true)
        }),

        new UiaControlGroup("Prozor „Edit“ — kartice", UiaWindowKind.Edit, new[]
        {
            Tab("Test items"),
            Tab("O/S"),
            Tab("Cond"),
            Tab("HV"),
            Tab("Mode"),
            Tab("Multi-DUT"),
            Tab("Ground"),
            Tab("R/C/D")
        }),

        new UiaControlGroup("Prozor „Edit“ — polja", UiaWindowKind.Edit, new[]
        {
            Field("Model"),
            Field("Type"),
            Field("Point"),
            Field("CMD Ver")
        })
    };

    /// <summary>Sve kontrole iz svih skupina.</summary>
    public static IReadOnlyList<UiaControlTarget> All { get; } =
        Groups.SelectMany(g => g.Targets).ToArray();

    /// <summary>Kontrola po logičkom imenu, ili <c>null</c>.</summary>
    public static UiaControlTarget? Find(string logicalName)
        => All.FirstOrDefault(t => string.Equals(t.LogicalName, logicalName, StringComparison.Ordinal));

    private static UiaControlTarget Button(UiaWindowKind window, string name, bool needsConfirmation = false)
        => new(Key(window, name), window, UiaControlKind.Button, name, needsConfirmation);

    private static UiaControlTarget MenuItem(string name)
        => new(Key(UiaWindowKind.Main, name), UiaWindowKind.Main, UiaControlKind.MenuItem, name);

    private static UiaControlTarget Tab(string name)
        => new(Key(UiaWindowKind.Edit, name), UiaWindowKind.Edit, UiaControlKind.Tab, name);

    private static UiaControlTarget Field(string name)
        => new(Key(UiaWindowKind.Edit, name), UiaWindowKind.Edit, UiaControlKind.Field, name, Writable: true);

    private static string Key(UiaWindowKind window, string name) => $"{window}.{name}";
}
