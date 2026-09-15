using CableTest.Core.Model;
using Microsoft.Data.Sqlite;

namespace CableTest.Core.Data;

/// <summary>Kablovi sa net listama u SQLite bazi.</summary>
/// <remarks>
/// Netovi se uvek učitavaju zajedno sa kablom: kabl bez net liste nema smisla — iz nje se
/// generiše .c61 — a broj kablova je mali, pa nema razloga za odloženo učitavanje.
/// </remarks>
public sealed class SqliteCableRepository : ICableRepository
{
    private const string SelectCable =
        "SELECT Id, VehicleId, Code, Description, SpecFileName FROM Cable";

    private readonly CableTestDatabase _database;

    public SqliteCableRepository(CableTestDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <summary>
    /// Svi kablovi, sa učitanim netovima.
    /// </summary>
    /// <remarks>
    /// Sortira se po dužini oznake pa po oznaci. Oznake kablova jednog vozila se razlikuju samo
    /// po broju na kraju („M30-W1“ … „M30-W20“), a čisto azbučno sortiranje bi ih poredalo kao
    /// W1, W10, W11, …, W2 — što je u spisku kroz koji operater traži kabl neupotrebljivo.
    /// </remarks>
    public IReadOnlyList<Cable> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectCable + " ORDER BY length(Code), Code;";
        return ReadCables(connection, command);
    }

    public IReadOnlyList<Cable> GetByVehicle(long vehicleId)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectCable + " WHERE VehicleId = $vehicleId ORDER BY length(Code), Code;";
        command.Parameters.AddWithValue("$vehicleId", vehicleId);
        return ReadCables(connection, command);
    }

    public Cable? GetById(long id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectCable + " WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return ReadCables(connection, command).FirstOrDefault();
    }

    public Cable? GetBySpecFileName(string? specFileName)
    {
        string name = Cable.NormalizeResultFileName(specFileName);
        if (name.Length == 0)
        {
            return null;
        }

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();

        // Kolona je COLLATE NOCASE, pa poređenje samo po sebi ne razlikuje velika i mala slova.
        command.CommandText = SelectCable + " WHERE SpecFileName = $name;";
        command.Parameters.AddWithValue("$name", name);
        return ReadCables(connection, command).FirstOrDefault();
    }

    public long Add(Cable cable)
    {
        ArgumentNullException.ThrowIfNull(cable);
        Spec.SpecFileNameValidator.EnsureValid(cable.SpecFileName, nameof(cable));

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
                VALUES ($vehicleId, $code, $description, $specFileName, $createdAt);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$vehicleId", cable.VehicleId);
            command.Parameters.AddWithValue("$code", cable.Code);
            command.Parameters.AddWithValue("$description", cable.Description);
            command.Parameters.AddWithValue("$specFileName", cable.SpecFileName);
            command.Parameters.AddWithValue("$createdAt", IsoDate.Now());

            cable.Id = (long)(command.ExecuteScalar() ?? 0L);
        }

        WriteNets(connection, transaction, cable);
        transaction.Commit();
        return cable.Id;
    }

    public void Update(Cable cable)
    {
        ArgumentNullException.ThrowIfNull(cable);
        Spec.SpecFileNameValidator.EnsureValid(cable.SpecFileName, nameof(cable));

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                UPDATE Cable
                   SET VehicleId = $vehicleId,
                       Code = $code,
                       Description = $description,
                       SpecFileName = $specFileName
                 WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$vehicleId", cable.VehicleId);
            command.Parameters.AddWithValue("$code", cable.Code);
            command.Parameters.AddWithValue("$description", cable.Description);
            command.Parameters.AddWithValue("$specFileName", cable.SpecFileName);
            command.Parameters.AddWithValue("$id", cable.Id);
            command.ExecuteNonQuery();
        }

        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM Net WHERE CableId = $cableId;";
            delete.Parameters.AddWithValue("$cableId", cable.Id);
            delete.ExecuteNonQuery();
        }

        WriteNets(connection, transaction, cable);
        transaction.Commit();
    }

    public void Delete(long id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Cable WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void WriteNets(SqliteConnection connection, SqliteTransaction transaction, Cable cable)
    {
        int ordinal = 0;
        foreach (CableNet net in cable.Nets.OrderBy(n => n.Ordinal))
        {
            // Redni broj se dodeljuje iznova, da u bazi nikad ne ostane rupa ni duplikat.
            ordinal++;

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO Net (CableId, Ordinal, Points) VALUES ($cableId, $ordinal, $points);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$cableId", cable.Id);
            command.Parameters.AddWithValue("$ordinal", ordinal);
            command.Parameters.AddWithValue("$points", net.Points);

            net.CableId = cable.Id;
            net.Ordinal = ordinal;
            net.Id = (long)(command.ExecuteScalar() ?? 0L);
        }
    }

    private static List<Cable> ReadCables(SqliteConnection connection, SqliteCommand command)
    {
        var cables = new List<Cable>();

        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                cables.Add(new Cable
                {
                    Id = reader.GetInt64(0),
                    VehicleId = reader.GetInt64(1),
                    Code = reader.GetString(2),
                    Description = reader.GetString(3),
                    SpecFileName = reader.GetString(4)
                });
            }
        }

        foreach (Cable cable in cables)
        {
            LoadNets(connection, cable);
        }

        return cables;
    }

    private static void LoadNets(SqliteConnection connection, Cable cable)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, CableId, Ordinal, Points FROM Net WHERE CableId = $cableId ORDER BY Ordinal;";
        command.Parameters.AddWithValue("$cableId", cable.Id);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            cable.Nets.Add(new CableNet
            {
                Id = reader.GetInt64(0),
                CableId = reader.GetInt64(1),
                Ordinal = reader.GetInt32(2),
                Points = reader.GetString(3)
            });
        }
    }
}
