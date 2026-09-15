using CableTest.Core.Model;

namespace CableTest.Core.Gateway;

/// <summary>Jedan rezultat koji je stigao sa testera.</summary>
public sealed class TestRunReceivedEventArgs : EventArgs
{
    public TestRunReceivedEventArgs(
        TestRun run,
        string? sourcePath = null,
        IReadOnlyList<string>? warnings = null,
        bool isBackfill = false)
    {
        ArgumentNullException.ThrowIfNull(run);
        Run = run;
        SourcePath = sourcePath;
        Warnings = warnings ?? Array.Empty<string>();
        IsBackfill = isBackfill;
        ReceivedAt = DateTime.Now;
    }

    /// <summary>Pročitani rezultat. <see cref="TestRun.CableId"/> još nije popunjen.</summary>
    public TestRun Run { get; }

    /// <summary>
    /// <c>true</c> ako je rezultat zatečen u fajlu pri pokretanju nadgledanja, a ne stigao uživo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ovo je bezbednosna oznaka, ne podatak za prikaz.</b> Veliki prikaz ishoda na glavnom
    /// ekranu sme da se pomeri <b>isključivo</b> na rezultat sa <c>IsBackfill == false</c>.
    /// </para>
    /// <para>
    /// Razlog: operater ujutru pokrene aplikaciju, ona pročita jučerašnji CSV sa sto redova i na
    /// ekranu ostane poslednji od njih — PASS od sinoć u 18:40. Operater stavi kabl, pogleda
    /// ekran, vidi PASS i pusti kabl dalje, a test nije ni pokrenut. Time bi neispitan kabl otišao
    /// u vozilo — a sprečavanje upravo toga je razlog postojanja ove aplikacije.
    /// </para>
    /// <para>
    /// Baza i istorija primaju oba tipa bez razlike: zatečeni redovi su stvarni testovi i moraju
    /// da uđu u istoriju. Razlika postoji samo u prikazu trenutnog ishoda.
    /// </para>
    /// </remarks>
    public bool IsBackfill { get; }

    /// <summary>Suprotno od <see cref="IsBackfill"/>; jedino stanje na koje prikaz ishoda sme da reaguje.</summary>
    public bool IsLive => !IsBackfill;

    /// <summary>
    /// Odakle je rezultat stigao — putanja CSV fajla kod rada preko fajlova,
    /// <c>null</c> kod lažnog gateway-a i kasnije kod serijske veze.
    /// </summary>
    public string? SourcePath { get; }

    /// <summary>
    /// Upozorenja iz istog čitanja (npr. neprepoznat datum, nepoznat ishod). Ako je u jednom
    /// čitanju stiglo više rezultata, isti spisak ide uz svaki od njih — upozorenja se odnose na
    /// čitanje, ne na pojedinačan red.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Vreme kada je aplikacija primila rezultat (ne vreme testa).</summary>
    public DateTime ReceivedAt { get; }
}
