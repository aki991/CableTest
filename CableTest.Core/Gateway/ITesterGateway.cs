using CableTest.Core.Model;

namespace CableTest.Core.Gateway;

/// <summary>
/// Veza aplikacije sa testerom: priprema test programa i dolazak rezultata.
/// </summary>
/// <remarks>
/// <para>
/// Interfejs postoji zato što u ovoj fazi razmena ide preko fajlova
/// (<see cref="FileBasedTesterGateway"/>), a u sledećoj dolazi <c>SerialTesterGateway</c> koji
/// priča direktno sa uređajem preko RS-232 i potpuno zamenjuje rad preko fajlova. Ostatak
/// aplikacije ne sme da zna koja je implementacija aktivna — zato ovde nema nijednog pojma iz
/// sveta fajlova (ni putanje, ni foldera, ni CSV-a).
/// </para>
/// <para>
/// Za razvoj i testove bez mašine postoji <see cref="FakeTesterGateway"/>.
/// </para>
/// </remarks>
public interface ITesterGateway
{
    /// <summary>
    /// Priprema test za zadati kabl. Kod rada preko fajlova to znači: generiši .c61 iz šablona i
    /// net liste kabla i upiši ga u spec folder. Ništa se ne pokreće — dalje operater ručno radi
    /// u CableConnector-u (izabere spec, Download, pa test).
    /// </summary>
    Task PrepareTestAsync(Cable cable, CancellationToken ct);

    /// <summary>
    /// Okida se po jednom pročitanom rezultatu, na niti koja je pokrenula
    /// <see cref="StartMonitoring"/> (GUI nit).
    /// </summary>
    event EventHandler<TestRunReceivedEventArgs> TestRunReceived;

    /// <summary>Počinje da osluškuje rezultate. Poziva se sa GUI niti.</summary>
    void StartMonitoring();

    /// <summary>Prestaje da osluškuje. Može se pozvati i kad nadgledanje nije aktivno.</summary>
    void StopMonitoring();

    /// <summary>Trenutno stanje veze, za prikaz operateru.</summary>
    TesterGatewayState State { get; }
}
