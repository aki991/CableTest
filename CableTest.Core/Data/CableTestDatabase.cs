using Microsoft.Data.Sqlite;

namespace CableTest.Core.Data;

/// <summary>
/// SQLite baza aplikacije: putanja do fajla, otvaranje veze i primena migracija.
/// </summary>
/// <remarks>
/// <para>
/// Bez ORM-a. Upiti su jednostavni (jedna tabela, jedan spoj), a ovako se u kodu vidi tačno šta
/// se šalje bazi — što je kod istorije testova važnije od kratkoće.
/// </para>
/// <para>
/// Veza se otvara po poslu i odmah zatvara. <c>Microsoft.Data.Sqlite</c> drži skup veza, pa je to
/// jeftino, a izbegava se jedina veza koja se drži otvorena i koju treba čuvati od više niti —
/// a njih ima, jer nadgledanje CSV-a radi u pozadini.
/// </para>
/// </remarks>
public sealed class CableTestDatabase
{
    /// <summary>Podrazumevano ime fajla baze.</summary>
    public const string DefaultFileName = "CableTest.db";

    public CableTestDatabase(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Putanja baze nije zadata.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath);
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = FilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            // Strana ključa SQLite podrazumevano ne sprovodi; bez ovoga bi ostali zapisi o
            // testovima bez kabla i netovi bez kabla.
            ForeignKeys = true,
            Pooling = true
        }.ToString();
    }

    /// <summary>Puna putanja fajla baze.</summary>
    public string FilePath { get; }

    /// <summary>Spojni niz sa kojim se otvaraju veze.</summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Otvara bazu u podrazumevanom folderu aplikacije i primenjuje migracije.
    /// </summary>
    public static CableTestDatabase OpenDefault()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CableTest");

        Directory.CreateDirectory(folder);

        var database = new CableTestDatabase(Path.Combine(folder, DefaultFileName));
        database.Migrate();
        return database;
    }

    /// <summary>Otvara zadatu bazu i primenjuje migracije.</summary>
    public static CableTestDatabase OpenAndMigrate(string filePath)
    {
        var database = new CableTestDatabase(filePath);
        database.Migrate();
        return database;
    }

    /// <summary>Otvorena veza prema bazi. Pozivalac je zatvara.</summary>
    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Primenjuje sve migracije koje još nisu primenjene. Poziva se pri svakom pokretanju;
    /// ako nema šta da se primeni, ne radi ništa.
    /// </summary>
    /// <returns>Verzija šeme posle primene.</returns>
    public int Migrate()
    {
        string? folder = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        using SqliteConnection connection = OpenConnection();
        return DatabaseMigrator.Apply(connection);
    }

    /// <summary>Trenutna verzija šeme; 0 ako nijedna migracija nije primenjena.</summary>
    public int SchemaVersion()
    {
        using SqliteConnection connection = OpenConnection();
        return DatabaseMigrator.CurrentVersion(connection);
    }
}
