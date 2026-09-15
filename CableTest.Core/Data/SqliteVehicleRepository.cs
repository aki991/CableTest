using CableTest.Core.Model;
using Microsoft.Data.Sqlite;

namespace CableTest.Core.Data;

/// <summary>Vozila u SQLite bazi.</summary>
public sealed class SqliteVehicleRepository : IVehicleRepository
{
    private readonly CableTestDatabase _database;

    public SqliteVehicleRepository(CableTestDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <summary>
    /// Sva vozila, redom kojim su ušla u katalog.
    /// </summary>
    /// <remarks>
    /// Redosled je namerno redosled unosa, a ne azbučni: padajuća lista vozila na glavnom ekranu
    /// treba da stoji onako kako je katalog složen, da operater navikne na mesto svake stavke.
    /// </remarks>
    public IReadOnlyList<Vehicle> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, CreatedAt FROM Vehicle ORDER BY Id;";

        var vehicles = new List<Vehicle>();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            vehicles.Add(Read(reader));
        }

        return vehicles;
    }

    public Vehicle? GetById(long id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, CreatedAt FROM Vehicle WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public Vehicle? GetByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, CreatedAt FROM Vehicle WHERE Name = $name;";
        command.Parameters.AddWithValue("$name", name);

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public long Add(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        if (string.IsNullOrWhiteSpace(vehicle.Name))
        {
            throw new ArgumentException("Ime vozila nije uneto.", nameof(vehicle));
        }

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Vehicle (Name, CreatedAt) VALUES ($name, $createdAt);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", vehicle.Name.Trim());
        command.Parameters.AddWithValue(
            "$createdAt",
            vehicle.CreatedAt == default ? IsoDate.Now() : IsoDate.ToStorage(vehicle.CreatedAt));

        vehicle.Id = (long)(command.ExecuteScalar() ?? 0L);
        return vehicle.Id;
    }

    public void Update(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE Vehicle SET Name = $name WHERE Id = $id;";
        command.Parameters.AddWithValue("$name", vehicle.Name.Trim());
        command.Parameters.AddWithValue("$id", vehicle.Id);
        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Vehicle WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static Vehicle Read(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Name = reader.GetString(1),
        CreatedAt = IsoDate.FromStorage(reader.GetString(2))
    };
}
