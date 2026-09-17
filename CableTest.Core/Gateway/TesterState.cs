namespace CableTest.Core.Gateway;

/// <summary>
/// Faza u kojoj se veza sa testerom nalazi.
/// </summary>
/// <remarks>
/// Namerno mali broj stanja: ovo opisuje jedan test, od pripreme programa do ishoda, a ne sve što
/// se na disku ili na vezi dešava. Sve što se tiče nadgledanja fajla stoji u
/// <see cref="TesterGatewayState"/> i ne pomera ovu mašinu.
/// </remarks>
public enum TesterState
{
    /// <summary>Ništa nije pripremljeno.</summary>
    Idle,

    /// <summary>Test program je upisan; čeka se pokretanje testa.</summary>
    ProgramLoaded,

    /// <summary>Test je pokrenut ili se čeka da ga operater pokrene; čeka se rezultat.</summary>
    WaitingForResult,

    /// <summary>Rezultat je stigao.</summary>
    Completed,

    /// <summary>Radnja nije uspela, ili je isteklo vreme čekanja na rezultat.</summary>
    Failed
}

/// <summary>Radnja koja pomera <see cref="TesterState"/>.</summary>
public enum TesterOperation
{
    /// <summary>Priprema test programa.</summary>
    LoadProgram,

    /// <summary>Pokretanje testa iz aplikacije.</summary>
    StartTest,

    /// <summary>Početak čekanja na rezultat.</summary>
    WaitForResult,

    /// <summary>Stigao je rezultat.</summary>
    ReceiveResult,

    /// <summary>Radnja nije uspela ili je isteklo vreme.</summary>
    Fail
}

/// <summary>Ishod jedne operacije nad gateway-em.</summary>
public enum GatewayStatus
{
    /// <summary>Uspelo.</summary>
    Ok,

    /// <summary>Implementacija ovu radnju ne podržava.</summary>
    NotSupported,

    /// <summary>Radnja nije dozvoljena u trenutnom stanju.</summary>
    InvalidState,

    /// <summary>Isteklo je vreme čekanja.</summary>
    Timeout,

    /// <summary>Čekanje je otkazano.</summary>
    Cancelled,

    /// <summary>Radnja je pokušana i nije uspela.</summary>
    Failed
}

/// <summary>
/// Šta se od jedne operacije dobilo.
/// </summary>
/// <remarks>
/// <para>
/// Operacije gateway-a <b>ne bacaju izuzetke</b> na očekivane ishode — ni na nedozvoljen prelaz,
/// ni na radnju koju implementacija ne podržava, ni na istek vremena. Sve to su normalna stanja
/// pogona, a ne greške u programu, i pozivalac mora da ih obradi, ne da ih hvata.
/// </para>
/// <para>
/// <see cref="Error"/> postoji zbog onoga što je zaista puklo (nema šablona, folder ne postoji):
/// poruka ide operateru, a izuzetak ostaje dostupan za log i za test.
/// </para>
/// </remarks>
/// <param name="Status">Ishod.</param>
/// <param name="Message">Objašnjenje za operatera; prazno kad je sve u redu.</param>
/// <param name="Run">Rezultat, kad ga je operacija donela.</param>
/// <param name="Error">Izuzetak koji je doveo do <see cref="GatewayStatus.Failed"/>, ako ga je bilo.</param>
public sealed record GatewayResult(
    GatewayStatus Status,
    string Message = "",
    Model.TestRun? Run = null,
    Exception? Error = null)
{
    /// <summary>Da li je operacija uspela.</summary>
    public bool IsOk => Status == GatewayStatus.Ok;

    public static GatewayResult Ok(Model.TestRun? run = null) => new(GatewayStatus.Ok, string.Empty, run);

    public static GatewayResult NotSupported(string message) => new(GatewayStatus.NotSupported, message);

    public static GatewayResult InvalidState(string message) => new(GatewayStatus.InvalidState, message);

    public static GatewayResult Timeout(string message) => new(GatewayStatus.Timeout, message);

    public static GatewayResult Cancelled(string message) => new(GatewayStatus.Cancelled, message);

    public static GatewayResult Failed(string message, Exception? error = null)
        => new(GatewayStatus.Failed, message, null, error);
}

/// <summary>Šta zadata implementacija ume.</summary>
/// <param name="CanStartTest">
/// Da li aplikacija može sama da pokrene test. Kod rada preko fajlova ne može — START pritiska
/// operater na mašini — pa ekran umesto dugmeta mora da pokaže uputstvo.
/// </param>
/// <param name="StartInstruction">Šta piše operateru kad aplikacija ne može sama da pokrene test.</param>
/// <param name="Description">Kratak opis izvora rezultata, za traku stanja.</param>
public sealed record TesterCapabilities(bool CanStartTest, string StartInstruction, string Description);

/// <summary>Promena stanja veze sa testerom.</summary>
public sealed class TesterStateChangedEventArgs : EventArgs
{
    public TesterStateChangedEventArgs(TesterState previous, TesterState current, string reason)
    {
        Previous = previous;
        Current = current;
        Reason = reason ?? string.Empty;
    }

    /// <summary>Stanje pre promene.</summary>
    public TesterState Previous { get; }

    /// <summary>Novo stanje.</summary>
    public TesterState Current { get; }

    /// <summary>Zašto je do promene došlo; može biti prazno.</summary>
    public string Reason { get; }
}

/// <summary>
/// Greška koju gateway prijavljuje umesto da je baci.
/// </summary>
/// <remarks>
/// Nadgledanje radi u pozadini, na tajmeru i na niti <see cref="FileSystemWatcher"/>-a. Izuzetak
/// odatle nema ko da uhvati i srušio bi aplikaciju pred operaterom — zato sve izlazi ovuda.
/// </remarks>
public sealed class GatewayErrorEventArgs : EventArgs
{
    public GatewayErrorEventArgs(string message, Exception? error = null, bool isWarning = false)
    {
        Message = message ?? string.Empty;
        Error = error;
        IsWarning = isWarning;
        OccurredAt = DateTime.Now;
    }

    /// <summary>Šta se desilo, rečeno operateru.</summary>
    public string Message { get; }

    /// <summary>Izuzetak, ako ga je bilo.</summary>
    public Exception? Error { get; }

    /// <summary>Upozorenje (rad se nastavlja) umesto greške.</summary>
    public bool IsWarning { get; }

    /// <summary>Kada je prijavljeno, u lokalnom vremenu.</summary>
    public DateTime OccurredAt { get; }
}

/// <summary>
/// Dozvoljeni prelazi između stanja.
/// </summary>
/// <remarks>
/// Čista funkcija nad stanjem i radnjom — bez fajlova, niti i vremena — pa se ceo red rada može
/// ispitati testom. Nedozvoljen prelaz nije izuzetak nego odgovor: vidi <see cref="GatewayResult"/>.
/// </remarks>
public static class TesterStateMachine
{
    /// <summary>Da li je radnja dozvoljena u zadatom stanju.</summary>
    public static bool IsAllowed(TesterState state, TesterOperation operation) => operation switch
    {
        // Priprema novog programa je dozvoljena uvek osim usred testa: tada bi se ispod ruke
        // operateru promenio program po kome mašina upravo meri.
        TesterOperation.LoadProgram => state != TesterState.WaitingForResult,

        // Test se pokreće samo nad pripremljenim programom.
        TesterOperation.StartTest => state == TesterState.ProgramLoaded,

        // Čeka se nad pripremljenim programom, ili se nastavlja već započeto čekanje.
        TesterOperation.WaitForResult => state is TesterState.ProgramLoaded or TesterState.WaitingForResult,

        // Rezultat sme da stigne bilo kada — i kad ga niko ne čeka (operater je pritisnuo START
        // sam, ili se čita zatečeni sadržaj fajla). Stanje tada ostaje kakvo je bilo.
        TesterOperation.ReceiveResult => true,

        TesterOperation.Fail => true,

        _ => false
    };

    /// <summary>Stanje posle dozvoljene radnje; za nedozvoljenu vraća zatečeno stanje.</summary>
    public static TesterState Next(TesterState state, TesterOperation operation)
    {
        if (!IsAllowed(state, operation))
        {
            return state;
        }

        return operation switch
        {
            TesterOperation.LoadProgram => TesterState.ProgramLoaded,
            TesterOperation.StartTest => TesterState.WaitingForResult,
            TesterOperation.WaitForResult => TesterState.WaitingForResult,
            TesterOperation.ReceiveResult => state == TesterState.WaitingForResult ? TesterState.Completed : state,
            TesterOperation.Fail => TesterState.Failed,
            _ => state
        };
    }

    /// <summary>Zašto radnja nije dozvoljena; prazno ako jeste.</summary>
    public static string Explain(TesterState state, TesterOperation operation)
    {
        if (IsAllowed(state, operation))
        {
            return string.Empty;
        }

        return operation switch
        {
            TesterOperation.LoadProgram =>
                "Test je u toku — sačekajte rezultat ili istek vremena pre nego što pripremite nov program.",
            TesterOperation.StartTest => state == TesterState.Idle
                ? "Test program nije pripremljen. Prvo pritisnite „Pripremi test program“."
                : $"Test se ne može pokrenuti iz stanja {state}.",
            TesterOperation.WaitForResult => state == TesterState.Idle
                ? "Test program nije pripremljen, pa nema šta da se čeka."
                : $"Čekanje na rezultat nije moguće iz stanja {state}.",
            _ => $"Radnja {operation} nije moguća iz stanja {state}."
        };
    }
}
