using System.Windows.Input;

namespace CableTest.App.Mvvm;

/// <summary>Komanda iz delegata.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }

    /// <summary>Javlja prikazu da se dostupnost komande promenila.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Komanda koja radi asinhrono, npr. upis .c61 fajla na disk.
/// </summary>
/// <remarks>
/// Dok posao traje komanda je nedostupna, pa dvostruki klik ne pokreće dva upisa. Izuzetak se ne
/// propušta dalje — <c>async void</c> izuzetak bi srušio aplikaciju — nego ide pozivaocu kroz
/// <c>onError</c>, da bi ga ViewModel prikazao operateru.
/// </remarks>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _running;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    public event EventHandler? CanExecuteChanged;

    /// <summary>Da li posao trenutno traje.</summary>
    public bool IsRunning => _running;

    public bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _running = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            _running = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Pokreće komandu i čeka je — za testove, bez <c>async void</c>.</summary>
    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return;
        }

        _running = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            _running = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
