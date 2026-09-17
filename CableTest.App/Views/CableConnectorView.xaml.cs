using System.Windows;
using System.Windows.Controls;
using CableTest.App.Services;
using CableTest.App.ViewModels;

namespace CableTest.App.Views;

/// <summary>
/// Prikaz razvojnog alata „CableConnector“.
/// </summary>
/// <remarks>
/// <para>
/// Ovde je samo ono što je zaista posao prikaza: <c>TreeView.SelectedItem</c> se u WPF-u ne može
/// vezati u oba smera, pa se izabrani čvor prosleđuje ViewModel-u iz događaja, a log se pomera na
/// dno kad mu se doda red.
/// </para>
/// <para>
/// Tu je i prečica F8: prijavljuje se Windows-u dok je strana otvorena, pa stiže i kad je u prvom
/// planu CableConnector — a to je i jedini način da se uhvati element ispod miša u tuđem prozoru.
/// Kad prijava ne uspe (F8 je zauzeo neko drugi), ostaje dugme i prečica unutar aplikacije.
/// </para>
/// </remarks>
public partial class CableConnectorView : UserControl
{
    private GlobalHotkey? _hotkey;

    public CableConnectorView()
    {
        InitializeComponent();

        Loaded += OnUcitano;
        Unloaded += OnSklonjeno;
    }

    private void OnUcitano(object sender, RoutedEventArgs e)
    {
        if (_hotkey is not null)
        {
            return;
        }

        _hotkey = GlobalHotkey.Register(this, GlobalHotkey.VkF8, Uhvati);

        if (DataContext is CableConnectorViewModel model)
        {
            model.ReportHotkey(_hotkey.IsRegistered);
        }
    }

    private void OnSklonjeno(object sender, RoutedEventArgs e)
    {
        _hotkey?.Dispose();
        _hotkey = null;
    }

    private void Uhvati()
    {
        if (DataContext is CableConnectorViewModel model && model.CaptureElementCommand.CanExecute(null))
        {
            model.CaptureElementCommand.Execute(null);
        }
    }

    private void OnCvorIzabran(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is CableConnectorViewModel model)
        {
            model.SelectedNode = e.NewValue as UiaNodeViewModel;
        }
    }

    /// <summary>
    /// Skroluje do reda koji je upravo označen.
    /// </summary>
    /// <remarks>
    /// Kad strana sama označi čvor (F8), prikaz ga ne dovodi u vidno polje od sebe — red ume da
    /// ostane daleko izvan ekrana. Zato se ovde traži baš onaj red koji je javio da je izabran.
    /// </remarks>
    private void OnRedIzabran(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem red)
        {
            red.BringIntoView();
        }
    }

    private void OnLogPromenjen(object sender, TextChangedEventArgs e) => LogPolje.ScrollToEnd();
}
