using System.Globalization;

namespace CableTest.Core.Gateway;

/// <summary>
/// Trenutno stanje veze sa testerom. Nepromenljiv snimak — čita ga GUI da bi prikazao
/// šta se dešava, a ne drži ga.
/// </summary>
/// <remarks>
/// Postoji zato što je razmena preko fajlova ćutljiva: ako CableConnector ne piše, ako je putanja
/// pogrešna ili je fajl nestao, ništa se ne dešava i operater nema kako da zna da li aplikacija
/// radi. Zato se sve što se zna o nadgledanju izlaže na jednom mestu.
/// </remarks>
public sealed record TesterGatewayState
{
    /// <summary>Stanje pre pokretanja nadgledanja.</summary>
    public static TesterGatewayState Idle { get; } = new();

    /// <summary>Da li je nadgledanje aktivno.</summary>
    public bool IsMonitoring { get; init; }

    /// <summary>
    /// Putanja koja je podešena — fajl ili folder. Ako je folder, prati se najnoviji CSV u njemu.
    /// </summary>
    public string? WatchedPath { get; init; }

    /// <summary>
    /// CSV fajl koji se trenutno prati. <c>null</c> dok fajl ne postoji ili ga u folderu nema.
    /// </summary>
    public string? CurrentFilePath { get; init; }

    /// <summary>Vreme poslednje primećene promene praćenog fajla, u lokalnom vremenu.</summary>
    public DateTime? LastChangeAt { get; init; }

    /// <summary>Broj redova fajla koje je parser obradio od pokretanja nadgledanja.</summary>
    public int ProcessedLineCount { get; init; }

    /// <summary>Broj rezultata prosleđenih kroz <c>TestRunReceived</c>.</summary>
    public int ProcessedRunCount { get; init; }

    /// <summary>
    /// Broj rezultata preskočenih jer su već pročitani (isti <see cref="Model.TestRunKey"/>).
    /// Nije greška — vidi <see cref="Model.TestRunKey"/>.
    /// </summary>
    public int SkippedDuplicateCount { get; init; }

    /// <summary>
    /// Poslednje upozorenje iz čitanja CSV-a (npr. neprepoznat datum), ili <c>null</c>.
    /// Upozorenje nije greška — obrada se zbog njega ne prekida.
    /// </summary>
    public string? LastWarning { get; init; }

    /// <summary>Poslednja greška pri nadgledanju, ili <c>null</c> ako je nije bilo.</summary>
    public string? LastError { get; init; }

    /// <summary>Vreme poslednje greške, u lokalnom vremenu.</summary>
    public DateTime? LastErrorAt { get; init; }

    /// <summary>Putanja poslednjeg upisanog .c61 fajla, ili <c>null</c> ako nijedan nije upisan.</summary>
    public string? LastPreparedSpecPath { get; init; }

    /// <summary>Kratak opis za statusnu liniju.</summary>
    public string Describe()
    {
        if (!IsMonitoring)
        {
            return LastError is null ? "Nadgledanje nije pokrenuto." : $"Nadgledanje nije pokrenuto. Poslednja greška: {LastError}";
        }

        string where = CurrentFilePath ?? WatchedPath ?? "(putanja nije podešena)";
        string counts =
            $"{ProcessedRunCount.ToString(CultureInfo.InvariantCulture)} rezultata / " +
            $"{ProcessedLineCount.ToString(CultureInfo.InvariantCulture)} redova";

        string when = LastChangeAt is null
            ? "još nije bilo promene"
            : "poslednja promena " + LastChangeAt.Value.ToString("dd.MM.yyyy. HH:mm:ss", CultureInfo.InvariantCulture);

        string text = $"Nadgleda se {where} — {counts}, {when}.";
        return LastError is null ? text : text + $" Poslednja greška: {LastError}";
    }
}
