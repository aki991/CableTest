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
        """
        SELECT Id, VehicleId, Code, Description, SpecFileName,
               Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive
          FROM Cable
        """;

    private readonly CableTestDatabase _database;

    public SqliteCableRepository(CableTestDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <summary>
    /// Redosled kablova u spisku.
    /// </summary>
    /// <remarks>
    /// Prvo po strani crteža: operater kablove traži onim redom kojim stoje u dokumentaciji, pa
    /// spisak prati nju. Kablovi bez podatka o strani (stariji zapisi) idu iza, poređani po
    /// dužini oznake pa po oznaci — oznake se razlikuju samo po broju na kraju („M30-W1“ …
    /// „M30-W20“), a čisto azbučno sortiranje bi ih poredalo kao W1, W10, W11, …, W2.
    /// </remarks>
    private const string OrderByDrawing =
        " ORDER BY CASE WHEN SourcePage > 0 THEN 0 ELSE 1 END, SourcePage, length(Code), Code;";

    /// <summary>Svi kablovi, sa učitanim netovima.</summary>
    public IReadOnlyList<Cable> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectCable + OrderByDrawing;
        return ReadCables(connection, command);
    }

    /// <summary>
    /// Kablovi jednog vozila koji se nude za ispitivanje.
    /// </summary>
    /// <remarks>
    /// Ugašeni kablovi (<see cref="Cable.IsActive"/> = <c>false</c>) se ne prikazuju: izašli su iz
    /// upotrebe, a nisu obrisani jer im istorija testova mora ostati. Istorija ih i dalje vidi
    /// preko <see cref="GetById"/> i <see cref="GetBySpecFileName"/>.
    /// </remarks>
    public IReadOnlyList<Cable> GetByVehicle(long vehicleId)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectCable + " WHERE VehicleId = $vehicleId AND IsActive = 1" + OrderByDrawing;
        command.Parameters.AddWithValue("$vehicleId", vehicleId);
        return ReadCables(connection, command);
    }

    /// <summary>Kablovi vozila bez obzira na to da li su u upotrebi — za katalog i istoriju.</summary>
    public IReadOnlyList<Cable> GetByVehicleIncludingInactive(long vehicleId)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectCable + " WHERE VehicleId = $vehicleId" + OrderByDrawing;
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
                INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
                VALUES ($vehicleId, $code, $description, $specFileName, $createdAt,
                        $designation, $cableType, $lengthM, $notes, $sourceDocument, $sourcePage, $isActive);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$vehicleId", cable.VehicleId);
            command.Parameters.AddWithValue("$code", cable.Code);
            command.Parameters.AddWithValue("$description", cable.Description);
            command.Parameters.AddWithValue("$specFileName", cable.SpecFileName);
            command.Parameters.AddWithValue("$createdAt", IsoDate.Now());
            AddDrawingParameters(command, cable);

            cable.Id = (long)(command.ExecuteScalar() ?? 0L);
        }

        WriteWiring(connection, transaction, cable);
        WriteNets(connection, transaction, cable);
        transaction.Commit();
        return cable.Id;
    }

    private static void AddDrawingParameters(SqliteCommand command, Cable cable)
    {
        command.Parameters.AddWithValue("$designation", cable.Designation);
        command.Parameters.AddWithValue("$cableType", cable.CableType);
        command.Parameters.AddWithValue("$lengthM", (double)cable.LengthM);
        command.Parameters.AddWithValue("$notes", cable.Notes);
        command.Parameters.AddWithValue("$sourceDocument", cable.SourceDocument);
        command.Parameters.AddWithValue("$sourcePage", cable.SourcePage);
        command.Parameters.AddWithValue("$isActive", cable.IsActive ? 1 : 0);
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
                       SpecFileName = $specFileName,
                       Designation = $designation,
                       CableType = $cableType,
                       LengthM = $lengthM,
                       Notes = $notes,
                       SourceDocument = $sourceDocument,
                       SourcePage = $sourcePage,
                       IsActive = $isActive
                 WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$vehicleId", cable.VehicleId);
            command.Parameters.AddWithValue("$code", cable.Code);
            command.Parameters.AddWithValue("$description", cable.Description);
            command.Parameters.AddWithValue("$specFileName", cable.SpecFileName);
            command.Parameters.AddWithValue("$id", cable.Id);
            AddDrawingParameters(command, cable);
            command.ExecuteNonQuery();
        }

        foreach (string table in new[] { "Net", "CableTerminal", "CableWire" })
        {
            using SqliteCommand delete = connection.CreateCommand();
            delete.Transaction = transaction;
            delete.CommandText = $"DELETE FROM {table} WHERE CableId = $cableId;";
            delete.Parameters.AddWithValue("$cableId", cable.Id);
            delete.ExecuteNonQuery();
        }

        WriteWiring(connection, transaction, cable);
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

    /// <summary>Upisuje ožičenje: terminale sa priključnom tabelom i provodnike.</summary>
    private static void WriteWiring(SqliteConnection connection, SqliteTransaction transaction, Cable cable)
    {
        foreach (CableTerminal terminal in cable.Terminals)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
                VALUES ($cableId, $label, $side, $contactType, $testerPoint, $isProvisional, $notes);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$cableId", cable.Id);
            command.Parameters.AddWithValue("$label", terminal.Label);
            command.Parameters.AddWithValue("$side", terminal.Side.ToString());
            command.Parameters.AddWithValue("$contactType", terminal.ContactType);
            command.Parameters.AddWithValue(
                "$testerPoint",
                terminal.HasTesterPoint ? terminal.TesterPoint! : DBNull.Value);
            command.Parameters.AddWithValue("$isProvisional", terminal.IsProvisional ? 1 : 0);
            command.Parameters.AddWithValue("$notes", terminal.Notes);

            terminal.CableId = cable.Id;
            terminal.Id = (long)(command.ExecuteScalar() ?? 0L);
        }

        foreach (CableWire wire in cable.Wires.OrderBy(w => w.WireNo))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM,
                                       FromTerminal, ToTerminal, Notes)
                VALUES ($cableId, $wireNo, $color, $cross, $lengthM, $from, $to, $notes);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$cableId", cable.Id);
            command.Parameters.AddWithValue("$wireNo", wire.WireNo);
            command.Parameters.AddWithValue("$color", wire.Color);
            command.Parameters.AddWithValue("$cross", (double)wire.CrossSectionMm2);
            command.Parameters.AddWithValue(
                "$lengthM",
                wire.LengthM is null ? DBNull.Value : (double)wire.LengthM.Value);
            command.Parameters.AddWithValue("$from", wire.FromTerminal);
            command.Parameters.AddWithValue("$to", wire.ToTerminal);
            command.Parameters.AddWithValue("$notes", wire.Notes);

            wire.CableId = cable.Id;
            wire.Id = (long)(command.ExecuteScalar() ?? 0L);
        }
    }

    /// <summary>
    /// Upisuje net listu.
    /// </summary>
    /// <remarks>
    /// Kabl sa ožičenjem ne upisuje ništa: net lista mu je izvedena i tabela <c>Net</c> bi bila
    /// drugi zapis iste stvari, koji vremenom ume da se raziđe sa izvorom istine.
    /// </remarks>
    private static void WriteNets(SqliteConnection connection, SqliteTransaction transaction, Cable cable)
    {
        if (cable.HasWiring)
        {
            return;
        }

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
                    SpecFileName = reader.GetString(4),
                    Designation = reader.GetString(5),
                    CableType = reader.GetString(6),
                    LengthM = (decimal)reader.GetDouble(7),
                    Notes = reader.GetString(8),
                    SourceDocument = reader.GetString(9),
                    SourcePage = reader.GetInt32(10),
                    IsActive = reader.GetInt64(11) != 0
                });
            }
        }

        foreach (Cable cable in cables)
        {
            LoadTerminals(connection, cable);
            LoadWires(connection, cable);
            LoadNets(connection, cable);
        }

        return cables;
    }

    private static void LoadTerminals(SqliteConnection connection, Cable cable)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes
              FROM CableTerminal
             WHERE CableId = $cableId
             ORDER BY Side, Id;
            """;
        command.Parameters.AddWithValue("$cableId", cable.Id);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            cable.Terminals.Add(new CableTerminal
            {
                Id = reader.GetInt64(0),
                CableId = reader.GetInt64(1),
                Label = reader.GetString(2),
                Side = string.Equals(reader.GetString(3), "B", StringComparison.OrdinalIgnoreCase)
                    ? CableSide.B
                    : CableSide.A,
                ContactType = reader.GetString(4),
                TesterPoint = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsProvisional = reader.GetInt64(6) != 0,
                Notes = reader.GetString(7)
            });
        }
    }

    private static void LoadWires(SqliteConnection connection, Cable cable)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes
              FROM CableWire
             WHERE CableId = $cableId
             ORDER BY WireNo;
            """;
        command.Parameters.AddWithValue("$cableId", cable.Id);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            cable.Wires.Add(new CableWire
            {
                Id = reader.GetInt64(0),
                CableId = reader.GetInt64(1),
                WireNo = reader.GetInt32(2),
                Color = reader.GetString(3),
                CrossSectionMm2 = (decimal)reader.GetDouble(4),
                LengthM = reader.IsDBNull(5) ? null : (decimal)reader.GetDouble(5),
                FromTerminal = reader.GetString(6),
                ToTerminal = reader.GetString(7),
                Notes = reader.GetString(8)
            });
        }
    }

    /// <summary>
    /// Puni net listu kabla.
    /// </summary>
    /// <remarks>
    /// Kabl sa ožičenjem dobija <b>izvedenu</b> net listu — iz terminala i provodnika, kroz
    /// <see cref="NetBuilder"/>. Tabela <c>Net</c> se za takve kablove i ne čita: izvor istine je
    /// ožičenje, a ručno upisan net bi mogao da mu protivreči. Kad izvođenje ne uspe (npr.
    /// terminal još nema tačku testera), net lista ostaje prazna, a razlog se vidi tamo gde se
    /// priprema test — vidi <see cref="NetDerivation.Errors"/>.
    /// </remarks>
    private static void LoadNets(SqliteConnection connection, Cable cable)
    {
        if (cable.HasWiring)
        {
            foreach (CableNet net in NetBuilder.Derive(cable).Nets)
            {
                cable.Nets.Add(net);
            }

            return;
        }

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
