using System.Windows;
using System.Windows.Threading;

namespace CableTest.Tests;

/// <summary>
/// Jedna STA nit sa <see cref="Application"/>-om, zajednička za sve testove koji diraju WPF.
/// </summary>
/// <remarks>
/// <para>
/// WPF dopušta <b>jedan</b> <see cref="Application"/> po AppDomain-u, a xUnit testove pušta
/// uporedo. Kad bi svaki test pravio svoj, drugi po redu bi pukao sa „Cannot create more than one
/// System.Windows.Application instance". Zato se ovde pravi tačno jedan, na sopstvenoj STA niti,
/// a testovi svoj posao prebacuju na nju.
/// </para>
/// <para>
/// Nit je i jedina — a ne samo prva: WPF objekti pripadaju niti na kojoj su napravljeni, pa bi
/// resurs napravljen na jednoj, a pročitan sa druge, bio greška koja se javlja nasumično.
/// </para>
/// <para>
/// Nijedan test ne otvara prozor; pravi se samo <see cref="Application"/> i njegovi resursi.
/// </para>
/// </remarks>
internal static class WpfHost
{
    private static readonly object Brava = new();
    private static Dispatcher? _dispatcher;

    /// <summary>Resursi aplikacije — sve što stoji u <c>App.xaml</c>, uključujući i temu.</summary>
    public static ResourceDictionary Resursi => Izvrsi(() => Application.Current.Resources);

    /// <summary>Izvršava radnju na WPF niti i vraća njen rezultat.</summary>
    public static T Izvrsi<T>(Func<T> radnja)
    {
        ArgumentNullException.ThrowIfNull(radnja);
        return Nit().Invoke(radnja, DispatcherPriority.Normal, CancellationToken.None, TimeSpan.FromSeconds(60));
    }

    /// <summary>Izvršava radnju na WPF niti.</summary>
    public static void Izvrsi(Action radnja)
    {
        ArgumentNullException.ThrowIfNull(radnja);

        Izvrsi(() =>
        {
            radnja();
            return true;
        });
    }

    private static Dispatcher Nit()
    {
        lock (Brava)
        {
            if (_dispatcher is not null)
            {
                return _dispatcher;
            }

            using var spremno = new ManualResetEventSlim();
            Exception? greska = null;

            var nit = new Thread(() =>
            {
                try
                {
                    // Aplikacija se pravi ovde, pa joj resursi pripadaju ovoj niti.
                    var app = new global::CableTest.App.App();
                    app.InitializeComponent();

                    _dispatcher = Dispatcher.CurrentDispatcher;
                }
                catch (Exception ex)
                {
                    greska = ex;
                }
                finally
                {
                    spremno.Set();
                }

                // Nit ostaje živa do kraja testova, da prima posao.
                if (greska is null)
                {
                    Dispatcher.Run();
                }
            })
            {
                // Pozadinska, da proces sa testovima ne ostane da visi kad se testovi završe.
                IsBackground = true,
                Name = "WPF (testovi)"
            };

            nit.SetApartmentState(ApartmentState.STA);
            nit.Start();

            if (!spremno.Wait(TimeSpan.FromSeconds(60)))
            {
                throw new TimeoutException("WPF nit nije podignuta na vreme.");
            }

            if (greska is not null)
            {
                throw new InvalidOperationException("WPF nit nije podignuta.", greska);
            }

            return _dispatcher!;
        }
    }
}
