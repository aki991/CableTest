using CableTest.Core.Model;

namespace CableTest.Core.Gateway;

/// <summary>
/// Veza aplikacije sa testerom: priprema test programa, pokretanje testa i dolazak rezultata.
/// </summary>
/// <remarks>
/// <para>
/// Interfejs postoji zato što u ovoj fazi razmena ide preko fajlova
/// (<see cref="FileBasedTesterGateway"/>), a u sledećoj dolazi <c>SerialTesterGateway</c> koji
/// priča direktno sa uređajem i potpuno zamenjuje rad preko fajlova. Ostatak aplikacije ne sme da
/// zna koja je implementacija aktivna — zato ovde nema nijednog pojma iz sveta fajlova (ni
/// putanje, ni foldera, ni CSV-a).
/// </para>
/// <para>
/// <b>Ovde nema WPF-a.</b> Gateway ne zna ni za <c>Dispatcher</c> ni za
/// <c>SynchronizationContext</c>: događaji se okidaju sa niti na kojoj se posao desio — tajmera,
/// <see cref="FileSystemWatcher"/>-a ili <c>Task</c>-a iz reda niti. Prebacivanje na GUI nit je
/// posao ViewModel-a, jer samo on zna da GUI uopšte postoji.
/// </para>
/// <para>
/// <b>Operacije ne bacaju izuzetke</b> na očekivane ishode. Nedozvoljen prelaz, radnja koju
/// implementacija ne podržava i istek vremena vraćaju se kroz <see cref="GatewayResult"/>; sve
/// što pukne u pozadini ide kroz <see cref="GatewayError"/>. Gateway ne sme da obori aplikaciju
/// koja stoji pred operaterom.
/// </para>
/// </remarks>
public interface ITesterGateway : IAsyncDisposable
{
    /// <summary>Faza u kojoj je veza: vidi <see cref="TesterState"/>.</summary>
    TesterState State { get; }

    /// <summary>Šta ova implementacija ume — pre svega, da li može sama da pokrene test.</summary>
    TesterCapabilities Capabilities { get; }

    /// <summary>Sve što se zna o nadgledanju, za traku stanja i ekran Podešavanja.</summary>
    TesterGatewayState Diagnostics { get; }

    /// <summary>Promena <see cref="State"/>. Okida se sa pozadinske niti.</summary>
    event EventHandler<TesterStateChangedEventArgs> StateChanged;

    /// <summary>Stigao je rezultat. Okida se sa pozadinske niti.</summary>
    event EventHandler<TestRunReceivedEventArgs> ResultReceived;

    /// <summary>Greška ili upozorenje iz pozadinskog rada. Okida se sa pozadinske niti.</summary>
    event EventHandler<GatewayErrorEventArgs> GatewayError;

    /// <summary>
    /// Priprema test program za zadati kabl i prelazi u <see cref="TesterState.ProgramLoaded"/>.
    /// Kod rada preko fajlova to znači: generiši .c61 iz šablona i net liste kabla i upiši ga u
    /// spec folder.
    /// </summary>
    Task<GatewayResult> LoadProgramAsync(Cable cable, CancellationToken ct);

    /// <summary>
    /// Pokreće test i prelazi u <see cref="TesterState.WaitingForResult"/>.
    /// </summary>
    /// <remarks>
    /// Implementacija koja test ne može da pokrene vraća <see cref="GatewayStatus.NotSupported"/>
    /// — bez izuzetka. Kod rada preko fajlova START pritiska operater na mašini; vidi
    /// <see cref="TesterCapabilities.CanStartTest"/>.
    /// </remarks>
    Task<GatewayResult> StartTestAsync(CancellationToken ct);

    /// <summary>
    /// Čeka prvi rezultat koji stigne uživo, najduže zadato vreme.
    /// </summary>
    /// <remarks>
    /// Istek vremena vraća <see cref="GatewayStatus.Timeout"/> i vodi u
    /// <see cref="TesterState.Failed"/>; otkazivanje vraća <see cref="GatewayStatus.Cancelled"/> i
    /// ostavlja stanje kakvo je bilo — test na mašini se time ne prekida.
    /// </remarks>
    Task<GatewayResult> WaitForResultAsync(TimeSpan timeout, CancellationToken ct);

    /// <summary>Počinje da osluškuje rezultate.</summary>
    /// <remarks>
    /// Odvojeno od <see cref="WaitForResultAsync"/> zato što rezultati moraju da stižu u istoriju
    /// i kad ih niko ne čeka: operater ume da pritisne START bez pripreme iz aplikacije.
    /// </remarks>
    void StartMonitoring();

    /// <summary>Prestaje da osluškuje. Može se pozvati i kad nadgledanje nije aktivno.</summary>
    void StopMonitoring();
}
