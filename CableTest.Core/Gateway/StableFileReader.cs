using System.Security.Cryptography;
using System.Text;
using CableTest.Core.Model;
using CableTest.Core.Results;

namespace CableTest.Core.Gateway;

/// <summary>Ishod pokušaja da se fajl pročita ceo.</summary>
public enum StableReadStatus
{
    /// <summary>Fajl je pročitan i veličina mu se nije menjala između dva očitavanja.</summary>
    Ok,

    /// <summary>Fajla nema.</summary>
    Missing,

    /// <summary>Fajl je sve vreme bio zaključan.</summary>
    Locked,

    /// <summary>Fajl je rastao za sve vreme pokušaja — upis još traje.</summary>
    Growing,

    /// <summary>Čitanje je prekinuto otkazivanjem.</summary>
    Cancelled
}

/// <summary>Šta je čitanje dalo.</summary>
/// <param name="Status">Ishod.</param>
/// <param name="Bytes">Sadržaj, samo kod <see cref="StableReadStatus.Ok"/>.</param>
/// <param name="Length">Veličina fajla u trenutku čitanja.</param>
/// <param name="Attempts">Koliko je pokušaja bilo potrebno.</param>
/// <param name="Message">Objašnjenje za log; prazno kad je sve u redu.</param>
/// <param name="Error">Poslednji izuzetak, ako ga je bilo.</param>
public sealed record StableReadResult(
    StableReadStatus Status,
    byte[]? Bytes,
    long Length,
    int Attempts,
    string Message = "",
    Exception? Error = null)
{
    /// <summary>Da li je fajl pročitan.</summary>
    public bool IsOk => Status == StableReadStatus.Ok && Bytes is not null;
}

/// <summary>
/// Čitanje fajla koji neko drugi upravo upisuje.
/// </summary>
/// <remarks>
/// <para>
/// U trenutku kad stigne obaveštenje o promeni, fajl je najčešće još otvoren za upis, a ume da
/// bude i upisan do pola. Zato se ne čita iz prve: pokušava se sa odlaganjem 100, 200, 400 i 800
/// milisekundi, i traži se da <b>veličina bude ista u dva uzastopna očitavanja</b> pre nego što se
/// sadržaj proglasi gotovim. Fajl koji i dalje raste ostaje za sledeći prolaz.
/// </para>
/// <para>
/// Odlaganje je zadato spolja (<c>delay</c>) da bi testovi mogli da ga skrate; podrazumevano je
/// pravo čekanje.
/// </para>
/// </remarks>
public sealed class StableFileReader
{
    /// <summary>Podrazumevana odlaganja između pokušaja, u milisekundama.</summary>
    public static readonly IReadOnlyList<int> DefaultBackoffMs = new[] { 100, 200, 400, 800 };

    private readonly IReadOnlyList<int> _backoffMs;
    private readonly Func<int, CancellationToken, Task> _delay;

    public StableFileReader(IReadOnlyList<int>? backoffMs = null, Func<int, CancellationToken, Task>? delay = null)
    {
        _backoffMs = backoffMs is { Count: > 0 } ? backoffMs : DefaultBackoffMs;
        _delay = delay ?? ((ms, ct) => Task.Delay(ms, ct));
    }

    /// <summary>Koliko će pokušaja najviše biti.</summary>
    public int MaxAttempts => _backoffMs.Count + 1;

    /// <summary>
    /// Čita fajl, čekajući da se oslobodi i da mu se veličina umiri.
    /// </summary>
    /// <remarks>Ne baca: sve što pođe naopako vraća se kroz <see cref="StableReadResult"/>.</remarks>
    public async Task<StableReadResult> ReadAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        long previousLength = -1;
        Exception? lastError = null;
        string message = string.Empty;
        int attempt = 0;

        // Prvi pokušaj je odmah; posle svakog neuspelog se čeka sve duže.
        for (int i = 0; i <= _backoffMs.Count; i++)
        {
            if (i > 0)
            {
                try
                {
                    await _delay(_backoffMs[i - 1], ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return new StableReadResult(StableReadStatus.Cancelled, null, previousLength, attempt, "čitanje je otkazano");
                }
            }

            if (ct.IsCancellationRequested)
            {
                return new StableReadResult(StableReadStatus.Cancelled, null, previousLength, attempt, "čitanje je otkazano");
            }

            attempt++;

            long length;

            try
            {
                var info = new FileInfo(path);

                if (!info.Exists)
                {
                    return new StableReadResult(StableReadStatus.Missing, null, 0, attempt, $"fajla „{path}“ nema");
                }

                length = info.Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastError = ex;
                message = $"podaci o fajlu „{path}“ nisu dostupni: {ex.Message}";
                continue;
            }

            if (length != previousLength)
            {
                // Prvo viđenje ove veličine: upis možda još traje, pa se čeka još jedno očitavanje.
                previousLength = length;
                message = $"fajl „{path}“ još raste ({length} B)";
                continue;
            }

            try
            {
                byte[] bytes = CsvTextReader.ReadAllBytes(path);
                return new StableReadResult(StableReadStatus.Ok, bytes, length, attempt);
            }
            catch (FileNotFoundException ex)
            {
                return new StableReadResult(StableReadStatus.Missing, null, 0, attempt, $"fajla „{path}“ nema", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                return new StableReadResult(StableReadStatus.Missing, null, 0, attempt, $"foldera fajla „{path}“ nema", ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Očekivano: program koji piše drži fajl. Pokušava se ponovo, pa i pri sledećem prolazu.
                lastError = ex;
                message = $"fajl „{path}“ je zauzet: {ex.Message}";
            }
        }

        StableReadStatus status = lastError is null ? StableReadStatus.Growing : StableReadStatus.Locked;
        return new StableReadResult(status, null, previousLength, attempt, message, lastError);
    }
}

/// <summary>
/// Otisak sadržaja rezultata, po kome se prepoznaje da je isti rezultat već viđen.
/// </summary>
/// <remarks>
/// Računa se nad sirovim redom CSV-a, onakvim kakav je tester zapisao — dakle nad sadržajem, a ne
/// nad onim što je parser od njega napravio. Dva čitanja istog reda daju isti otisak i kad se fajl
/// prepiše ili pročita od početka.
/// </remarks>
public static class ResultHash
{
    /// <summary>SHA-256 zadatog teksta, u heksadekadnom obliku.</summary>
    public static string OfText(string? text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// SHA-256 jednog rezultata.
    /// </summary>
    /// <remarks>
    /// Kad sirovog reda nema (rezultat je napravljen programski), otisak se računa nad podacima
    /// koji rezultat jednoznačno određuju — isto što čini i <see cref="TestRunKey"/>.
    /// </remarks>
    public static string Of(TestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        string content = string.IsNullOrWhiteSpace(run.RawRow)
            ? string.Join(
                '',
                run.Seq.ToString(System.Globalization.CultureInfo.InvariantCulture),
                run.SpecFileName,
                run.TestedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                run.Passed ? "pass" : "fail",
                run.Operator)
            : run.RawRow;

        return OfText(content);
    }
}
