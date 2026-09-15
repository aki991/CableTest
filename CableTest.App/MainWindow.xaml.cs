using System.Windows;
using System.Windows.Threading;
using CableTest.App.ViewModels;

namespace CableTest.App;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly DispatcherTimer _statusTimer;

    public MainWindow(ShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        InitializeComponent();

        _shell = shell;
        DataContext = shell;

        // Stanje nadgledanja se menja i kad nijedan rezultat ne stigne (fajl nestane, folder se
        // pojavi, greška prođe), pa traka stanja mora sama da se osvežava.
        _statusTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statusTimer.Tick += (_, _) => _shell.Testing.RefreshState();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Tek ovde, sa GUI niti: gateway tada hvata SynchronizationContext na kome okida događaje.
        _shell.Start();
        _statusTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _statusTimer.Stop();
        _shell.Dispose();
    }

    // -----------------------------------------------------------------------------------
    // Dugmad prozora
    //
    // Zaglavlje je ujedno i naslovna traka (WindowChrome), pa umanji/uvećaj/zatvori nisu
    // Windows-ovi nego naši — inače bi iznad tamnog zaglavlja stajala svetla sistemska traka.
    // -----------------------------------------------------------------------------------

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
