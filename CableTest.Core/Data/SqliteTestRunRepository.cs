using System.Globalization;
using CableTest.Core.Model;
using Microsoft.Data.Sqlite;

namespace CableTest.Core.Data;

/// <summary>Istorija testova u SQLite bazi.</summary>
/// <remarks>
/// Duplikati se sprečavaju u samoj bazi, jedinstvenim indeksom nad
/// <c>(SpecFileName, Seq, TestedAt)</c>. Upis koristi <c>ON CONFLICT ... DO NOTHING</c>, pa je
/// preskakanje atomsko — nema provere pa upisa između kojih neko drugi može da upiše isti zapis.
/// Namerno se ne koristi <c>INSERT OR IGNORE</c>, koji bi progutao i prekršaj stranog ključa i
/// time sakrio stvarnu grešku.
/// </remarks>
public sealed class SqliteTestRunRepository : ITestRunRepository
{
    private const string SelectRun =
        """
        SELECT Id, CableId, Seq, SpecFileName, Passed, TestedAt, Operator, Barcode, RawRow
          FROM TestRun
        """;

    private readonly CableTestDatabase _database;

    public SqliteTestRunRepository(CableTestDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    public bool Add(TestRun run, ICollection<string>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        bool inserted = Insert(connection, transaction, run, warnings);

        transaction.Commit();
        return inserted;
    }

    public int AddRange(IEnumerable<TestRun> runs, ICollection<string>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(runs);

        List<TestRun> list = runs.Where(r => r is not null).ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        int inserted = 0;
        foreach (TestRun run in list)
        {
            if (Insert(connection, transaction, run, warnings))
            {
                inserted++;
            }
        }

        transaction.Commit();
        return inserted;
    }

    public bool Exists(TestRunKey key)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1 FROM TestRun
             WHERE SpecFileName = $specFileName AND Seq = $seq AND TestedAt = $testedAt
             LIMIT 1;
            """;
        command.Parameters.AddWithValue("$specFileName", key.SpecFileName);
        command.Parameters.AddWithValue("$seq", key.Seq);
        command.Parameters.AddWithValue("$testedAt", IsoDate.ToStorage(key.TestedAt));

        return command.ExecuteScalar() is not null;
    }

    public IReadOnlyList<TestRun> GetRecent(int count)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectRun + " ORDER BY TestedAt DESC, Id DESC LIMIT $count;";
        command.Parameters.AddWithValue("$count", Math.Max(count, 0));
        return ReadRuns(connection, command);
    }

    public IReadOnlyList<TestRun> GetByCable(long cableId, int count = 100)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectRun + " WHERE CableId = $cableId ORDER BY TestedAt DESC, Id DESC LIMIT $count;";
        command.Parameters.AddWithValue("$cableId", cableId);
        command.Parameters.AddWithValue("$count", Math.Max(count, 0));
        return ReadRuns(connection, command);
    }

    public TestRun? GetLatestForCable(long cableId) => GetByCable(cableId, 1).FirstOrDefault();

    public TestRun? GetById(long id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectRun + " WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return ReadRuns(connection, command).FirstOrDefault();
    }

    public int Count()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM TestRun;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static bool Insert(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TestRun run,
        ICollection<string>? warnings)
    {
        TestRunKey key = TestRunKey.Of(run);

        int affected;
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO TestRun (CableId, Seq, SpecFileName, Passed, TestedAt, Operator, Barcode, RawRow, CreatedAt)
                VALUES ($cableId, $seq, $specFileName, $passed, $testedAt, $operator, $barcode, $rawRow, $createdAt)
                ON CONFLICT (SpecFileName, Seq, TestedAt) DO NOTHING;
                """;
            command.Parameters.AddWithValue("$cableId", (object?)run.CableId ?? DBNull.Value);
            command.Parameters.AddWithValue("$seq", run.Seq);
            command.Parameters.AddWithValue("$specFileName", key.SpecFileName);
            command.Parameters.AddWithValue("$passed", run.Passed ? 1 : 0);
            command.Parameters.AddWithValue("$testedAt", IsoDate.ToStorage(run.TestedAt));
            command.Parameters.AddWithValue("$operator", run.Operator);
            command.Parameters.AddWithValue("$barcode", run.Barcode);
            command.Parameters.AddWithValue("$rawRow", run.RawRow);
            command.Parameters.AddWithValue("$createdAt", IsoDate.Now());

            affected = command.ExecuteNonQuery();
        }

        if (affected == 0)
        {
            // Zapis već postoji. Preskače se tiho, ali se mora videti — vidi TestRunKey.
            warnings?.Add($"Rezultat ({key}) već postoji u bazi i nije upisan ponovo.");
            return false;
        }

        using (SqliteCommand identity = connection.CreateCommand())
        {
            identity.Transaction = transaction;
            identity.CommandText = "SELECT last_insert_rowid();";
            run.Id = (long)(identity.ExecuteScalar() ?? 0L);
        }

        foreach (TestDefect defect in run.Defects)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO TestDefect (TestRunId, Kind, Points, RawText)
                VALUES ($testRunId, $kind, $points, $rawText);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$testRunId", run.Id);
            command.Parameters.AddWithValue("$kind", defect.Kind.ToString());
            command.Parameters.AddWithValue("$points", defect.Points);
            command.Parameters.AddWithValue("$rawText", defect.RawText);

            defect.TestRunId = run.Id;
            defect.Id = (long)(command.ExecuteScalar() ?? 0L);
        }

        return true;
    }

    private static List<TestRun> ReadRuns(SqliteConnection connection, SqliteCommand command)
    {
        var runs = new List<TestRun>();

        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                runs.Add(new TestRun
                {
                    Id = reader.GetInt64(0),
                    CableId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    Seq = reader.GetInt32(2),
                    SpecFileName = reader.GetString(3),
                    Passed = reader.GetInt32(4) != 0,
                    TestedAt = IsoDate.FromStorage(reader.GetString(5)),
                    Operator = reader.GetString(6),
                    Barcode = reader.GetString(7),
                    RawRow = reader.GetString(8)
                });
            }
        }

        foreach (TestRun run in runs)
        {
            LoadDefects(connection, run);
        }

        return runs;
    }

    private static void LoadDefects(SqliteConnection connection, TestRun run)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, TestRunId, Kind, Points, RawText FROM TestDefect WHERE TestRunId = $id ORDER BY Id;";
        command.Parameters.AddWithValue("$id", run.Id);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            run.Defects.Add(new TestDefect
            {
                Id = reader.GetInt64(0),
                TestRunId = reader.GetInt64(1),
                Kind = ParseKind(reader.GetString(2)),
                Points = reader.GetString(3),
                RawText = reader.GetString(4)
            });
        }
    }

    /// <summary>Neprepoznata vrsta greške u bazi ne ruši čitanje — postaje "Unknown".</summary>
    private static DefectKind ParseKind(string text)
        => Enum.TryParse(text, ignoreCase: true, out DefectKind kind) ? kind : DefectKind.Unknown;
}
