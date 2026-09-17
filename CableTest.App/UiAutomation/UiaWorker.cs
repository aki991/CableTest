using System.Collections.Concurrent;
using System.Diagnostics;
using FlaUI.UIA3;

namespace CableTest.App.UiAutomation;

/// <summary>Ishod jednog poziva prema UIA.</summary>
/// <param name="Value">Rezultat, ako je poziv uspeo.</param>
/// <param name="Error">Izuzetak, ako je pukao.</param>
/// <param name="TimedOut">Da li je istekao rok.</param>
/// <param name="ElapsedMs">Koliko je trajao, u milisekundama.</param>
public sealed record UiaCall<T>(T? Value, Exception? Error, bool TimedOut, long ElapsedMs)
{
    /// <summary>Da li je poziv uspeo.</summary>
    public bool Ok => Error is null && !TimedOut;

    /// <summary>Kratko objašnjenje neuspeha, za log.</summary>
    public string FailureText => TimedOut
        ? "istekao rok"
        : Error is null ? string.Empty : $"{Error.GetType().Name}: {Error.Message}";
}

/// <summary>
/// Jedna nit koja radi sav posao sa UI Automation, i rok za svaki poziv.
/// </summary>
/// <remarks>
/// <para>
/// <b>Zašto zasebna nit:</b> UIA pozivi idu u tuđi proces i traju koliko taj proces hoće.
/// Pokrenuti ih sa GUI niti značilo bi da zamrznut CableConnector zamrzne i CableTest — a
/// CableTest je program pred kojim operater stoji dok ispituje kabl.
/// </para>
/// <para>
/// <b>Zašto rok:</b> poziv koji ne vrati odgovor ne može da se prekine — COM poziv se ne
/// prekida spolja. Zato se posle isteka roka nit <i>napušta</i>: ostaje zaglavljena u svom
/// pozivu, ali je označena kao neupotrebljiva (<see cref="IsAbandoned"/>), pozivalac dobija
/// odgovor, a sledeći rad kreće na novoj niti. Nit je pozadinska, pa ne drži aplikaciju u
/// životu pri izlasku.
/// </para>
/// <para>
/// Nit je MTA: UIA3 objekti se tako mogu slobodno koristiti, bez pumpe poruka.
/// </para>
/// </remarks>
public sealed class UiaWorker : IDisposable
{
    /// <summary>Podrazumevani rok za jedan poziv.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    private UIA3Automation? _automation;
    private volatile bool _abandoned;
    private volatile bool _disposed;

    public UiaWorker()
    {
        _thread = new Thread(Pump)
        {
            IsBackground = true,
            Name = "CableConnector UIA"
        };

        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
    }

    /// <summary>
    /// Nit je zaglavljena u pozivu koji nije vratio odgovor u roku i više se ne koristi.
    /// </summary>
    public bool IsAbandoned => _abandoned;

    /// <summary>
    /// Izvršava posao na UIA niti i čeka ga najduže <paramref name="timeout"/>.
    /// </summary>
    /// <remarks>
    /// Ne baca ništa: i greška i istekli rok vraćaju se kao <see cref="UiaCall{T}"/>, jer strana
    /// mora da ostane u životu i kad CableConnector nije pokrenut ili ne odgovara.
    /// </remarks>
    public async Task<UiaCall<T>> RunAsync<T>(Func<UIA3Automation, T> work, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(work);

        var stopwatch = Stopwatch.StartNew();

        if (_disposed || _abandoned)
        {
            return new UiaCall<T>(
                default,
                new InvalidOperationException("UIA nit je napuštena posle isteklog roka; potrebno je ponovo se povezati."),
                TimedOut: false,
                stopwatch.ElapsedMilliseconds);
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _queue.Add(() =>
            {
                try
                {
                    completion.TrySetResult(work(EnsureAutomation()));
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // Red je zatvoren — Dispose je već prošao.
            return new UiaCall<T>(default, ex, TimedOut: false, stopwatch.ElapsedMilliseconds);
        }

        Task finished = await Task.WhenAny(completion.Task, Task.Delay(timeout)).ConfigureAwait(true);

        if (!ReferenceEquals(finished, completion.Task))
        {
            _abandoned = true;
            return new UiaCall<T>(default, Error: null, TimedOut: true, stopwatch.ElapsedMilliseconds);
        }

        try
        {
            T value = await completion.Task.ConfigureAwait(true);
            return new UiaCall<T>(value, Error: null, TimedOut: false, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new UiaCall<T>(default, ex, TimedOut: false, stopwatch.ElapsedMilliseconds);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();

        // Zaglavljena nit se ne čeka: ona je pozadinska i gasi se sa aplikacijom.
        if (!_abandoned)
        {
            _thread.Join(TimeSpan.FromSeconds(2));
        }
    }

    private UIA3Automation EnsureAutomation() => _automation ??= new UIA3Automation();

    private void Pump()
    {
        try
        {
            foreach (Action work in _queue.GetConsumingEnumerable())
            {
                work();
            }
        }
        catch (ObjectDisposedException)
        {
            // Red je zatvoren usred čekanja — uredan kraj niti.
        }
        finally
        {
            try
            {
                _automation?.Dispose();
            }
            catch (Exception)
            {
                // Gašenje UIA veze ne sme da sruši nit koja se ionako završava.
            }
        }
    }
}
