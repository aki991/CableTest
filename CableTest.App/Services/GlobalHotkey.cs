using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CableTest.App.Services;

/// <summary>
/// Prečica na nivou celog Windows-a, dok je strana otvorena.
/// </summary>
/// <remarks>
/// <para>
/// Obična WPF prečica radi samo dok je CableTest u prvom planu — a „uhvati element ispod miša"
/// se koristi upravo obrnuto: operater radi u CableConnector-u, tamo mu je i taster, i tek onda
/// pritiska F8. Zato se prečica prijavljuje Windows-u preko <c>RegisterHotKey</c>, pa stiže i
/// kad je u prvom planu tuđi prozor.
/// </para>
/// <para>
/// Ako je F8 već zauzeo neki drugi program, prijava ne uspe — <see cref="IsRegistered"/> je tada
/// <c>false</c>, a strana i dalje radi preko dugmeta i preko prečice unutar same aplikacije.
/// Ovo ne sme da sruši ništa: razvojni alat nije razlog da aplikacija ne krene.
/// </para>
/// </remarks>
public sealed class GlobalHotkey : IDisposable
{
    private const int WmHotkey = 0x0312;

    /// <summary>Virtuelni kod tastera F8.</summary>
    public const uint VkF8 = 0x77;

    private readonly int _id;
    private readonly Action _onPressed;

    private HwndSource? _source;
    private bool _disposed;

    private GlobalHotkey(int id, Action onPressed)
    {
        _id = id;
        _onPressed = onPressed;
    }

    /// <summary>Da li je Windows prihvatio prijavu prečice.</summary>
    public bool IsRegistered { get; private set; }

    /// <summary>
    /// Prijavljuje prečicu za prozor u kome se nalazi zadati element.
    /// </summary>
    /// <remarks>Nikad ne baca: neuspeh se vidi kroz <see cref="IsRegistered"/>.</remarks>
    public static GlobalHotkey Register(DependencyObject owner, uint virtualKey, Action onPressed)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(onPressed);

        // Id mora biti jedinstven u okviru prozora i u opsegu 0x0000–0xBFFF; ovde je prečica
        // jedna jedina, pa je dovoljan jedan stalan broj.
        var hotkey = new GlobalHotkey(0x0F08, onPressed);

        try
        {
            Window? window = Window.GetWindow(owner);

            if (window is null)
            {
                return hotkey;
            }

            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (handle == IntPtr.Zero)
            {
                return hotkey;
            }

            hotkey._source = HwndSource.FromHwnd(handle);

            if (hotkey._source is null)
            {
                return hotkey;
            }

            hotkey._source.AddHook(hotkey.Hook);
            hotkey.IsRegistered = RegisterHotKey(handle, hotkey._id, fsModifiers: 0, virtualKey);

            if (!hotkey.IsRegistered)
            {
                hotkey._source.RemoveHook(hotkey.Hook);
                hotkey._source = null;
            }
        }
        catch (Exception)
        {
            // Prijava prečice nije posao bez koga se ne može; ostaje dugme.
            hotkey.IsRegistered = false;
        }

        return hotkey;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_source is null)
        {
            return;
        }

        try
        {
            if (IsRegistered && _source.Handle != IntPtr.Zero)
            {
                UnregisterHotKey(_source.Handle, _id);
            }

            _source.RemoveHook(Hook);
        }
        catch (Exception)
        {
            // Prozor je već zatvoren; odjava prečice tada nema šta da odjavi.
        }
        finally
        {
            IsRegistered = false;
            _source = null;
        }
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey || wParam.ToInt32() != _id)
        {
            return IntPtr.Zero;
        }

        handled = true;
        _onPressed();
        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
