using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Data.Sqlite;

namespace CableTest.Core.Data;

/// <summary>
/// Primena numerisanih SQL skripti na bazu, uz evidenciju u tabeli <c>SchemaVersion</c>.
/// </summary>
/// <remarks>
/// <para>
/// Migracije su obični SQL fajlovi u <c>Data\Migrations</c>, imenovani <c>NNN_opis.sql</c> i
/// ugrađeni u sklop. Primenjuju se pri pokretanju, redom po broju, i svaka u svojoj transakciji:
/// ako skripta pukne na pola, baza ostaje na prethodnoj verziji umesto da bude polovična.
/// </para>
/// <para>
/// Skripta se nikada ne menja pošto je jednom primenjena — izmena se dodaje kao nova, sa većim
/// brojem. Inače bi baza kod jednog korisnika imala jednu šemu, a kod drugog drugu, uz isti broj.
/// </para>
/// </remarks>
public static class DatabaseMigrator
{
    private const string ResourcePrefix = "CableTest.Core.Data.Migrations.";

    /// <summary>Jedna migracija: broj, ime i sadržaj skripte.</summary>
    public sealed record Migration(int Version, string Name, string Sql);

    /// <summary>Sve ugrađene migracije, poređane po broju.</summary>
    public static IReadOnlyList<Migration> All { get; } = LoadMigrations();

    /// <summary>Verzija šeme u bazi; 0 ako tabela <c>SchemaVersion</c> još ne postoji.</summary>
    public static int CurrentVersion(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        EnsureVersionTable(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Primenjuje migracije koje nedostaju, zaključno sa zadatom verzijom. Vraća verziju posle
    /// primene.
    /// </summary>
    /// <param name="connection">Otvorena veza prema bazi.</param>
    /// <param name="throughVersion">
    /// Do koje verzije se ide; podrazumevano do poslednje. Postoji zbog provere nadogradnje —
    /// test napravi bazu stare verzije, ubaci u nju podatke i tek onda primeni novu migraciju,
    /// jer se tako ponaša i baza koju operater već ima na mašini.
    /// </param>
    public static int Apply(SqliteConnection connection, int throughVersion = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // WAL: aplikacija čita iz GUI-ja dok pozadinsko nadgledanje upisuje rezultate.
        // Ne ide unutar transakcije.
        using (SqliteCommand pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL;";
            pragma.ExecuteNonQuery();
        }

        int version = CurrentVersion(connection);

        foreach (Migration migration in All)
        {
            if (migration.Version <= version || migration.Version > throughVersion)
            {
                continue;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = migration.Sql;
                command.ExecuteNonQuery();
            }

            using (SqliteCommand record = connection.CreateCommand())
            {
                record.Transaction = transaction;
                record.CommandText =
                    "INSERT INTO SchemaVersion (Version, Name, AppliedAt) VALUES ($version, $name, $appliedAt);";
                record.Parameters.AddWithValue("$version", migration.Version);
                record.Parameters.AddWithValue("$name", migration.Name);
                record.Parameters.AddWithValue("$appliedAt", IsoDate.Now());
                record.ExecuteNonQuery();
            }

            transaction.Commit();
            version = migration.Version;
        }

        return version;
    }

    private static void EnsureVersionTable(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS SchemaVersion (
                Version   INTEGER PRIMARY KEY,
                Name      TEXT NOT NULL,
                AppliedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<Migration> LoadMigrations()
    {
        Assembly assembly = typeof(DatabaseMigrator).Assembly;
        var migrations = new List<Migration>();

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                || !resource.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string name = resource[ResourcePrefix.Length..^".sql".Length];
            int underscore = name.IndexOf('_');
            string numberText = underscore < 0 ? name : name[..underscore];

            if (!int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version))
            {
                throw new InvalidOperationException(
                    $"Migracija \"{resource}\" nema broj na početku imena. Očekuje se oblik \"001_opis.sql\".");
            }

            using Stream stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Migracija \"{resource}\" se ne može pročitati.");
            using var reader = new StreamReader(stream, Encoding.UTF8);

            migrations.Add(new Migration(version, name, reader.ReadToEnd()));
        }

        migrations.Sort((a, b) => a.Version.CompareTo(b.Version));

        for (int i = 1; i < migrations.Count; i++)
        {
            if (migrations[i].Version == migrations[i - 1].Version)
            {
                throw new InvalidOperationException(
                    $"Dve migracije imaju isti broj: \"{migrations[i - 1].Name}\" i \"{migrations[i].Name}\".");
            }
        }

        return migrations;
    }
}
