using CableTest.Core.Model;

namespace CableTest.Core.Gateway;

/// <summary>
/// Oni koji čekaju rezultat kroz <see cref="ITesterGateway.WaitForResultAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// Postoji da bi se čekanje pisalo jednom, a ne u svakoj implementaciji gateway-a ponovo:
/// isti red radnji (dodaj čekaoca, probudi ga kad stigne rezultat, ukloni ga kad se otkaže) lako
/// se u dva primerka razidje, a greške u ovakvom kodu se vide tek kad operater ostane da čeka
/// zauvek.
/// </para>
/// <para>
/// Sve metode su bezbedne za poziv sa više niti: rezultat stiže sa pozadinske niti, a čeka se sa
/// one koja je pozvala <c>WaitForResultAsync</c>.
/// </para>
/// </remarks>
internal sealed class ResultWaitList
{
    private readonly object _sync = new();
    private readonly List<TaskCompletionSource<TestRun>> _waiters = new();

    /// <summary>Da li iko trenutno čeka.</summary>
    public bool HasWaiters
    {
        get
        {
            lock (_sync)
            {
                return _waiters.Count != 0;
            }
        }
    }

    /// <summary>Prijavljuje novog čekaoca.</summary>
    public TaskCompletionSource<TestRun> Add()
    {
        var waiter = new TaskCompletionSource<TestRun>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_sync)
        {
            _waiters.Add(waiter);
        }

        return waiter;
    }

    /// <summary>Uklanja čekaoca; poziva se i kad je čekanje uspelo i kad je prekinuto.</summary>
    public void Remove(TaskCompletionSource<TestRun> waiter)
    {
        lock (_sync)
        {
            _waiters.Remove(waiter);
        }
    }

    /// <summary>Budi sve koji čekaju; vraća <c>true</c> ako je iko čekao.</summary>
    public bool CompleteAll(TestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        TaskCompletionSource<TestRun>[] waiters = Take();

        foreach (TaskCompletionSource<TestRun> waiter in waiters)
        {
            waiter.TrySetResult(run);
        }

        return waiters.Length != 0;
    }

    /// <summary>Prekida sve koji čekaju — gateway se gasi.</summary>
    public void CancelAll()
    {
        foreach (TaskCompletionSource<TestRun> waiter in Take())
        {
            waiter.TrySetCanceled();
        }
    }

    private TaskCompletionSource<TestRun>[] Take()
    {
        lock (_sync)
        {
            TaskCompletionSource<TestRun>[] waiters = _waiters.ToArray();
            _waiters.Clear();
            return waiters;
        }
    }
}
