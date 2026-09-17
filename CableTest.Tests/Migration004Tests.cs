using CableTest.Core.Data;
using CableTest.Core.Model;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Nadogradnja zatečene baze na verziju 4 (kablovi sa crteža).
/// </summary>
/// <remarks>
/// <para>
/// Baza na mašini nije prazna: u njoj su demo kablovi iz ranijih migracija, a nad nekima od njih
/// je već nešto ispitano. Pravilo je jasno i ovde se brani: <b>kabl bez istorije se briše, kabl
/// sa istorijom se ne dira nego gasi.</b> Istorija testova je dokaz da je kabl ispitan i ne sme
/// da nestane zato što je katalog zamenjen.
/// </para>
/// <para>
/// Zato svaki test ide istim putem kao i prava nadogradnja: napravi bazu verzije 3, upiše u nju
/// podatke, pa tek onda primeni migraciju 4.
/// </para>
/// </remarks>
public sealed class Migration004Tests : IDisposable
{
    private const int BeforeVersion = 3;

    private readonly string _folder;

    public Migration004Tests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestMig_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Privremeni folder.
        }
    }

    [Fact]
    public void DemoKablBezIstorije_SeBrise()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "bez-istorije.db"));
        MigrateTo(database, BeforeVersion);

        Assert.True(CableExists(database, "M30-W1"));

        database.Migrate();

        Assert.False(CableExists(database, "M30-W1"));
        Assert.Equal(0, Count(database, "SELECT COUNT(*) FROM Net WHERE CableId NOT IN (SELECT Id FROM Cable);"));
    }

    [Fact]
    public void DemoKablSaIstorijom_OstajeAliUgasen_IIstorijaJeNetaknuta()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "sa-istorijom.db"));
        MigrateTo(database, BeforeVersion);

        var runs = new SqliteTestRunRepository(database);
        long cableId = CableId(database, "M30-W1");

        runs.Add(new TestRun
        {
            CableId = cableId,
            Seq = 1,
            SpecFileName = "M30-W1",
            Passed = true,
            TestedAt = new DateTime(2026, 9, 7, 11, 44, 11),
            Operator = "Miloš",
            RawRow = "1,M30-W1,pass,2026/09/07,11:44:11"
        });

        database.Migrate();

        // Kabl je ostao, sa istim Id-em, ali se više ne nudi za ispitivanje.
        Assert.True(CableExists(database, "M30-W1"));
        Assert.Equal(cableId, CableId(database, "M30-W1"));
        Assert.Equal(0, Count(database, "SELECT IsActive FROM Cable WHERE SpecFileName = 'M30-W1';"));

        // Istorija je netaknuta i i dalje vezana za taj kabl.
        TestRun run = Assert.Single(runs.GetByCable(cableId));
        Assert.Equal(1, run.Seq);
        Assert.True(run.Passed);

        // Ugašen kabl nestaje iz padajuće liste, ali ga istorija i dalje nalazi.
        var cables = new SqliteCableRepository(database);
        Cable cable = cables.GetBySpecFileName("M30-W1")!;

        Assert.False(cable.IsActive);
        Assert.DoesNotContain(cables.GetByVehicle(cable.VehicleId), c => c.SpecFileName == "M30-W1");
        Assert.Contains(cables.GetByVehicleIncludingInactive(cable.VehicleId), c => c.SpecFileName == "M30-W1");

        // Stari kabl zadržava ručno unetu net listu — nju izvođenje ne dodiruje.
        Assert.NotEmpty(cable.Nets);
        Assert.False(cable.HasWiring);
    }

    /// <summary>Kablovi sa crteža stižu i u bazu koja već ima istoriju.</summary>
    [Fact]
    public void PosleNadogradnje_KabloviSaCrtezaSuUBazi()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "nadogradnja.db"));
        MigrateTo(database, BeforeVersion);
        database.Migrate();

        var cables = new SqliteCableRepository(database);
        Cable cable = cables.GetBySpecFileName("40W1-1")!;

        Assert.Equal("=40-W1.1", cable.Designation);
        Assert.Equal(new[] { "A01-B01", "A02-B02" }, cable.Nets.Select(n => n.Points));
    }

    /// <summary>
    /// Vozilo „Miloš“ postaje „Miloš Veliki“, a njegov Id ostaje isti — inače bi veza kablova sa
    /// vozilom pukla.
    /// </summary>
    /// <remarks>
    /// Baza se namerno dovodi u stanje u kome postoji samo „Miloš“: migracija 003 je to vozilo
    /// već preimenovala kod većine korisnika, pa se ova grana inače ne bi ni okinula — a mora da
    /// radi kod svakoga ko je do sada ostao sa starim imenom.
    /// </remarks>
    [Fact]
    public void VoziloMilos_PostajeMilosVeliki_SaIstimId()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "vozilo.db"));
        MigrateTo(database, BeforeVersion);

        Execute(database, "UPDATE Vehicle SET Name = 'Miloš' WHERE Name = 'Miloš Veliki';");

        var vehicles = new SqliteVehicleRepository(database);
        long id = vehicles.GetByName("Miloš")!.Id;

        database.Migrate();

        Assert.Null(vehicles.GetByName("Miloš"));

        Vehicle vehicle = vehicles.GetByName("Miloš Veliki")!;
        Assert.Equal(id, vehicle.Id);
        Assert.Equal(12, new SqliteCableRepository(database).GetByVehicle(vehicle.Id).Count);
    }

    /// <summary>
    /// Kad i „Miloš“ i „Miloš Veliki“ postoje (prvi je preživeo zbog istorije, drugi je iz
    /// kataloga), kablovi se spajaju pod jedno vozilo, a istorija ostaje vezana za svoj kabl.
    /// </summary>
    [Fact]
    public void ObaVozila_SeSpajajuUJedno_BezGubitkaIstorije()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "spajanje.db"));

        using (SqliteConnection connection = database.OpenConnection())
        {
            DatabaseMigrator.Apply(connection, throughVersion: 2);
        }

        // Test nad oglednim kablom: zbog njega „Miloš“ preživljava migraciju 003.
        var runs = new SqliteTestRunRepository(database);
        long cableId = CableId(database, "M100W1");

        runs.Add(new TestRun
        {
            CableId = cableId,
            Seq = 7,
            SpecFileName = "M100W1",
            Passed = false,
            TestedAt = new DateTime(2026, 9, 7, 12, 0, 0),
            Operator = "Miloš"
        });

        using (SqliteConnection connection = database.OpenConnection())
        {
            DatabaseMigrator.Apply(connection, throughVersion: BeforeVersion);
        }

        var vehicles = new SqliteVehicleRepository(database);
        Assert.NotNull(vehicles.GetByName("Miloš"));
        Assert.NotNull(vehicles.GetByName("Miloš Veliki"));

        database.Migrate();

        Vehicle vehicle = Assert.Single(vehicles.GetAll());
        Assert.Equal("Miloš Veliki", vehicle.Name);

        var cables = new SqliteCableRepository(database);
        Cable stari = cables.GetBySpecFileName("M100W1")!;

        Assert.Equal(cableId, stari.Id);
        Assert.Equal(vehicle.Id, stari.VehicleId);
        Assert.False(stari.IsActive);
        Assert.Single(runs.GetByCable(cableId));

        // Aktivni ostaju samo kablovi sa crteža.
        Assert.Equal(12, cables.GetByVehicle(vehicle.Id).Count);
    }

    /// <summary>Ponovno pokretanje migracija ne sme ništa da udvostruči.</summary>
    [Fact]
    public void PonovnaPrimena_NeMenjaNista()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "ponovo.db"));
        database.Migrate();
        database.Migrate();

        var cables = new SqliteCableRepository(database);
        Cable cable = cables.GetBySpecFileName("40W5")!;

        Assert.Equal(12, cables.GetAll().Count);
        Assert.Equal(15, cable.Terminals.Count);
        Assert.Equal(9, cable.Wires.Count);
        Assert.Equal(6, cable.Nets.Count);
    }

    /// <summary>Demo baza ide kroz istu nadogradnju kao i prava.</summary>
    [Fact]
    public void DemoBaza_SeNadogradjujeIsto()
    {
        var database = new CableTestDatabase(Path.Combine(_folder, "CableTest.demo.db"));
        MigrateTo(database, BeforeVersion);
        database.Migrate();

        var cables = new SqliteCableRepository(database);

        Assert.Equal(12, cables.GetAll().Count);
        Assert.NotNull(cables.GetBySpecFileName("40W5"));
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private static void MigrateTo(CableTestDatabase database, int version)
    {
        using SqliteConnection connection = database.OpenConnection();
        DatabaseMigrator.Apply(connection, version);
    }

    private static void Execute(CableTestDatabase database, string sql)
    {
        using SqliteConnection connection = database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static bool CableExists(CableTestDatabase database, string spec)
        => Count(database, $"SELECT COUNT(*) FROM Cable WHERE SpecFileName = '{spec}';") == 1;

    private static long CableId(CableTestDatabase database, string spec)
        => Count(database, $"SELECT Id FROM Cable WHERE SpecFileName = '{spec}';");

    private static long Count(CableTestDatabase database, string sql)
    {
        using SqliteConnection connection = database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? value = command.ExecuteScalar();
        return value is null or DBNull ? 0 : Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
