using CableTest.Core.Data;
using CableTest.Core.Model;
using CableTest.Core.Results;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Testovi baze rade nad <b>fajlom</b> u privremenom folderu, ne nad bazom u memoriji.
/// </summary>
/// <remarks>
/// U memoriji se baza ponaša drugačije nego u pogonu: nema WAL-a, nema zaključavanja fajla, a
/// veza koja se zatvori odnosi celu bazu sa sobom. Pošto se ovde upravo proverava da li se zapisi
/// zaista trajno upisuju i da li jedinstveni indeks radi, testovi moraju da rade nad istim
/// oblikom baze kakav ima aplikacija.
/// </remarks>
public sealed class DatabaseTests : IDisposable
{
    private readonly string _folder;
    private readonly CableTestDatabase _database;

    public DatabaseTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CableTestDb_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_folder);
        _database = CableTestDatabase.OpenAndMigrate(Path.Combine(_folder, "test.db"));
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

    /// <summary>Koliko vozila donosi početni sadržaj baze (migracija 003).</summary>
    private const int KatalogVozila = 3;

    /// <summary>Koliko kablova donosi početni sadržaj baze: 20 + 20 + 21.</summary>
    private const int KatalogKablova = 61;

    /// <summary>Ukupan broj netova u bazi.</summary>
    private long BrojNetova()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Net;";
        return (long)command.ExecuteScalar()!;
    }

    // -------------------------------------------------------------------------------------
    // Migracije
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Migracije_SePrimenjujuIBelezeUSchemaVersion()
    {
        Assert.NotEmpty(DatabaseMigrator.All);
        int poslednja = DatabaseMigrator.All[^1].Version;

        Assert.Equal(poslednja, _database.SchemaVersion());

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SchemaVersion;";

        Assert.Equal((long)DatabaseMigrator.All.Count, (long)command.ExecuteScalar()!);
    }

    [Fact]
    public void Migracije_PonovnoPokretanje_NePrimenjujeNistaDvaput()
    {
        int prvi = _database.SchemaVersion();

        _database.Migrate();
        _database.Migrate();

        Assert.Equal(prvi, _database.SchemaVersion());

        // Početni sadržaj se ne ponavlja.
        Assert.Equal(KatalogVozila, new SqliteVehicleRepository(_database).GetAll().Count);
    }

    [Fact]
    public void Migracije_SuNumerisaneIJedinstvene()
    {
        int[] brojevi = DatabaseMigrator.All.Select(m => m.Version).ToArray();

        Assert.Equal(brojevi.OrderBy(v => v).ToArray(), brojevi);
        Assert.Equal(brojevi.Distinct().Count(), brojevi.Length);
        Assert.All(brojevi, v => Assert.True(v > 0));
    }

    [Fact]
    public void Migracije_SeMoguPrimenitiNaPostojeciFajlBaze()
    {
        // Druga baza u istom folderu: potpuno nova, pa opet otvorena.
        string path = Path.Combine(_folder, "druga.db");

        CableTestDatabase prva = CableTestDatabase.OpenAndMigrate(path);
        int verzija = prva.SchemaVersion();

        CableTestDatabase druga = CableTestDatabase.OpenAndMigrate(path);

        Assert.Equal(verzija, druga.SchemaVersion());
        Assert.Equal(KatalogVozila, new SqliteVehicleRepository(druga).GetAll().Count);
    }

    // -------------------------------------------------------------------------------------
    // Početni sadržaj
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Katalog_PriPrvomPokretanju_DajeTriVozilaSaKablovima()
    {
        IReadOnlyList<Vehicle> vozila = new SqliteVehicleRepository(_database).GetAll();

        Assert.Equal(
            new[] { "Miloš Veliki", "Lazar 3M", "Perun" },
            vozila.Select(v => v.Name));

        var kablovi = new SqliteCableRepository(_database);

        // Oznake unutar vozila idu prirodnim redom: W1 pre W2, a ne W1, W10, W11, W2.
        string[] milos = kablovi.GetByVehicle(vozila[0].Id).Select(c => c.Code).ToArray();
        Assert.Equal(20, milos.Length);
        Assert.Equal("M30-W1", milos[0]);
        Assert.Equal("M30-W2", milos[1]);
        Assert.Equal("M30-W20", milos[^1]);

        Assert.Equal(20, kablovi.GetByVehicle(vozila[1].Id).Count);
        Assert.Equal(21, kablovi.GetByVehicle(vozila[2].Id).Count);

        Assert.Equal(KatalogKablova, kablovi.GetAll().Count);
    }

    [Fact]
    public void Katalog_SvakiKablImaIspravnuNetListu()
    {
        foreach (Cable kabl in new SqliteCableRepository(_database).GetAll())
        {
            Assert.NotEmpty(kabl.Nets);
            Assert.Equal(
                Enumerable.Range(1, kabl.Nets.Count),
                kabl.Nets.Select(n => n.Ordinal));

            // Jedna ispitna tačka sme da pripada samo jednom netu istog kabla.
            var vidjene = new HashSet<int>();

            foreach (CableNet net in kabl.Nets)
            {
                foreach (TestPoint tacka in net.ToNet())
                {
                    Assert.True(vidjene.Add(tacka.Index), $"{kabl.Code}: {tacka} je u dva neta.");
                }
            }
        }
    }

    [Fact]
    public void Katalog_KablSeNalaziPoImenuSpecFajlaIzCsvKolone()
    {
        var repository = new SqliteCableRepository(_database);

        Assert.NotNull(repository.GetBySpecFileName("M30-W1"));
        Assert.NotNull(repository.GetBySpecFileName("m30-w1"));
        Assert.NotNull(repository.GetBySpecFileName("M30-W1.c61"));
        Assert.Null(repository.GetBySpecFileName("NEMA-GA"));
        Assert.Null(repository.GetBySpecFileName(null));
    }

    // -------------------------------------------------------------------------------------
    // Vozila i kablovi
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Vozilo_SeUpisujeCitaIBrise()
    {
        var repository = new SqliteVehicleRepository(_database);

        long id = repository.Add(new Vehicle { Name = "Golf" });
        Assert.True(id > 0);

        Vehicle? procitano = repository.GetById(id);
        Assert.NotNull(procitano);
        Assert.Equal("Golf", procitano!.Name);
        Assert.Equal("Golf", repository.GetByName("Golf")!.Name);

        procitano.Name = "Golf 8";
        repository.Update(procitano);
        Assert.Equal("Golf 8", repository.GetById(id)!.Name);

        repository.Delete(id);
        Assert.Null(repository.GetById(id));
    }

    [Fact]
    public void BrisanjeVozila_BriseINjegoveKabloveINetove()
    {
        var vehicles = new SqliteVehicleRepository(_database);
        var cables = new SqliteCableRepository(_database);

        Vehicle? vozilo = vehicles.GetByName("Miloš Veliki");
        Assert.NotNull(vozilo);

        long netovaPre = BrojNetova();
        int njegovihNetova = cables.GetByVehicle(vozilo!.Id).Sum(c => c.Nets.Count);

        vehicles.Delete(vozilo.Id);

        Assert.Empty(cables.GetByVehicle(vozilo.Id));
        Assert.Null(cables.GetBySpecFileName("M30-W1"));

        // Netovi obrisanih kablova odlaze sa njima, a tuđi ostaju netaknuti.
        Assert.Equal(netovaPre - njegovihNetova, BrojNetova());
    }

    [Fact]
    public void Kabl_SeUpisujeSaNetovimaIMenja()
    {
        var vehicles = new SqliteVehicleRepository(_database);
        var cables = new SqliteCableRepository(_database);

        long vehicleId = vehicles.GetByName("Miloš Veliki")!.Id;

        var kabl = new Cable { VehicleId = vehicleId, Code = "M100-W2", SpecFileName = "M100W2" };
        kabl.Nets.Add(new CableNet { Ordinal = 1, Points = "O03-O04" });
        kabl.Nets.Add(new CableNet { Ordinal = 2, Points = "O05-O06-O07" });

        long id = cables.Add(kabl);

        Cable? procitan = cables.GetById(id);
        Assert.NotNull(procitan);
        Assert.Equal(2, procitan!.Nets.Count);
        Assert.Equal("O03-O04", procitan.Nets[0].Points);
        Assert.Equal("O05-O06-O07", procitan.Nets[1].Points);

        procitan.Nets.RemoveAt(0);
        procitan.Description = "izmenjen";
        cables.Update(procitan);

        Cable? ponovo = cables.GetById(id);
        Assert.Equal("izmenjen", ponovo!.Description);
        CableNet net = Assert.Single(ponovo.Nets);
        Assert.Equal("O05-O06-O07", net.Points);
        Assert.Equal(1, net.Ordinal);   // redni brojevi se dodeljuju iznova
    }

    [Fact]
    public void Kabl_DvaKablaSaIstimImenomSpecFajla_SeOdbijaju()
    {
        var cables = new SqliteCableRepository(_database);
        long vehicleId = new SqliteVehicleRepository(_database).GetByName("Miloš Veliki")!.Id;

        // Ime se upisuje velikim slovima (mala odbija SpecFileNameValidator), pa se ovde
        // proverava sam jedinstveni indeks.
        var duplikat = new Cable { VehicleId = vehicleId, Code = "DRUGI", SpecFileName = "M30-W1" };

        Assert.Throws<SqliteException>(() => cables.Add(duplikat));
    }

    [Fact]
    public void Kabl_NeispravnoImeSpecFajla_SeOdbija()
    {
        var cables = new SqliteCableRepository(_database);
        long vehicleId = new SqliteVehicleRepository(_database).GetByName("Miloš Veliki")!.Id;

        var kabl = new Cable { VehicleId = vehicleId, Code = "X", SpecFileName = "m100 w1" };

        Assert.Throws<ArgumentException>(() => cables.Add(kabl));
    }

    [Fact]
    public void Kabl_VozilaKojeNePostoji_SeOdbija()
    {
        var cables = new SqliteCableRepository(_database);
        var kabl = new Cable { VehicleId = 9999, Code = "X", SpecFileName = "XX" };

        Assert.Throws<SqliteException>(() => cables.Add(kabl));
    }

    // -------------------------------------------------------------------------------------
    // Istorija testova
    // -------------------------------------------------------------------------------------

    [Fact]
    public void TestRun_SeUpisujeSaGreskamaICitaNazad()
    {
        var runs = new SqliteTestRunRepository(_database);
        long cableId = new SqliteCableRepository(_database).GetBySpecFileName("M30-W1")!.Id;

        var run = new TestRun
        {
            CableId = cableId,
            Seq = 2,
            SpecFileName = "M100W1",
            Passed = false,
            TestedAt = new DateTime(2026, 9, 7, 11, 47, 34),
            Operator = "Miloš",
            Barcode = "ABC-123",
            RawRow = "2,M100W1,fail,..."
        };
        run.Defects.Add(ResultCsvParser.CreateDefect("SHORT O01-O02"));
        run.Defects.Add(ResultCsvParser.CreateDefect("HV LEAKAGE O05"));

        Assert.True(runs.Add(run));
        Assert.True(run.Id > 0);

        TestRun? procitan = runs.GetById(run.Id);
        Assert.NotNull(procitan);
        Assert.Equal(2, procitan!.Seq);
        Assert.Equal(cableId, procitan.CableId);
        Assert.True(procitan.IsRecognized);
        Assert.False(procitan.Passed);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 47, 34), procitan.TestedAt);
        Assert.Equal("Miloš", procitan.Operator);
        Assert.Equal("ABC-123", procitan.Barcode);

        Assert.Equal(2, procitan.Defects.Count);
        Assert.Equal(DefectKind.Short, procitan.Defects[0].Kind);
        Assert.Equal("O01-O02", procitan.Defects[0].Points);
        Assert.Equal(DefectKind.Unknown, procitan.Defects[1].Kind);
        Assert.Equal("HV LEAKAGE O05", procitan.Defects[1].RawText);
    }

    [Fact]
    public void TestRun_NeprepoznatKabl_SeIpakCuva()
    {
        var runs = new SqliteTestRunRepository(_database);

        var run = new TestRun
        {
            CableId = null,
            Seq = 1,
            SpecFileName = "PROBA1",
            Passed = true,
            TestedAt = new DateTime(2026, 9, 7, 11, 44, 11)
        };

        Assert.True(runs.Add(run));

        TestRun procitan = Assert.Single(runs.GetRecent(10));
        Assert.Null(procitan.CableId);
        Assert.False(procitan.IsRecognized);
        Assert.Equal("PROBA1", procitan.SpecFileName);
    }

    /// <summary>Pravilo iz tačke 2: isti zapis se ne upisuje dvaput.</summary>
    [Fact]
    public void TestRun_IstiPrirodniKljuc_SePreskaceUzUpozorenje()
    {
        var runs = new SqliteTestRunRepository(_database);
        var warnings = new List<string>();

        Assert.True(runs.Add(Run(1, new DateTime(2026, 9, 7, 11, 44, 11)), warnings));
        Assert.False(runs.Add(Run(1, new DateTime(2026, 9, 7, 11, 44, 11)), warnings));

        Assert.Equal(1, runs.Count());
        Assert.Single(warnings);
        Assert.Contains("već postoji", warnings[0], StringComparison.Ordinal);
    }

    [Fact]
    public void TestRun_ImeSpecFajlaSePorediBezObziraNaVelikaSlova()
    {
        var runs = new SqliteTestRunRepository(_database);
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11);

        Assert.True(runs.Add(Run(1, trenutak, "M100W1")));

        TestRun isti = Run(1, trenutak, "m100w1");
        Assert.False(runs.Add(isti));

        Assert.Equal(1, runs.Count());
    }

    [Fact]
    public void TestRun_RazlicitoVremeIliRedniBroj_NijeDuplikat()
    {
        var runs = new SqliteTestRunRepository(_database);
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11);

        Assert.True(runs.Add(Run(1, trenutak)));
        Assert.True(runs.Add(Run(2, trenutak)));
        Assert.True(runs.Add(Run(1, trenutak.AddSeconds(1))));

        Assert.Equal(3, runs.Count());
    }

    [Fact]
    public void AddRange_UpisujeSamoNovoIJavljaPreskocene()
    {
        var runs = new SqliteTestRunRepository(_database);
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11);

        var prvi = new[] { Run(1, trenutak), Run(2, trenutak.AddMinutes(1)) };
        Assert.Equal(2, runs.AddRange(prvi));

        var warnings = new List<string>();
        var ponovo = new[] { Run(1, trenutak), Run(2, trenutak.AddMinutes(1)), Run(3, trenutak.AddMinutes(2)) };

        Assert.Equal(1, runs.AddRange(ponovo, warnings));
        Assert.Equal(3, runs.Count());
        Assert.Equal(2, warnings.Count);
    }

    [Fact]
    public void Exists_VidiPostojeciZapis()
    {
        var runs = new SqliteTestRunRepository(_database);
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11);

        Assert.False(runs.Exists(TestRunKey.Of("M100W1", 1, trenutak)));
        runs.Add(Run(1, trenutak));

        Assert.True(runs.Exists(TestRunKey.Of("M100W1", 1, trenutak)));
        Assert.True(runs.Exists(TestRunKey.Of("m100w1.c61", 1, trenutak)));
        Assert.False(runs.Exists(TestRunKey.Of("M100W1", 1, trenutak.AddSeconds(1))));
    }

    [Fact]
    public void GetByCable_VracaNajnovijePrve()
    {
        var runs = new SqliteTestRunRepository(_database);
        long cableId = new SqliteCableRepository(_database).GetBySpecFileName("M30-W1")!.Id;

        var podne = new DateTime(2026, 9, 7, 12, 0, 0);
        runs.Add(Run(1, podne, cableId: cableId));
        runs.Add(Run(2, podne.AddMinutes(5), cableId: cableId));
        runs.Add(Run(3, podne.AddMinutes(10), cableId: cableId));
        runs.Add(Run(4, podne.AddMinutes(15)));   // bez kabla

        IReadOnlyList<TestRun> istorija = runs.GetByCable(cableId);

        Assert.Equal(3, istorija.Count);
        Assert.Equal(new[] { 3, 2, 1 }, istorija.Select(r => r.Seq));
        Assert.Equal(3, runs.GetLatestForCable(cableId)!.Seq);
        Assert.Equal(4, runs.GetRecent(1)[0].Seq);
    }

    [Fact]
    public void BrisanjeKabla_CuvaIstorijuBezVezeSaKablom()
    {
        var cables = new SqliteCableRepository(_database);
        var runs = new SqliteTestRunRepository(_database);

        long cableId = cables.GetBySpecFileName("M30-W1")!.Id;
        runs.Add(Run(1, new DateTime(2026, 9, 7, 11, 44, 11), cableId: cableId));

        cables.Delete(cableId);

        TestRun zapis = Assert.Single(runs.GetRecent(10));
        Assert.Null(zapis.CableId);
        Assert.Equal("M100W1", zapis.SpecFileName);
    }

    // -------------------------------------------------------------------------------------
    // Datumi
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Datumi_SeCuvajuKaoIsoTekstUUtc()
    {
        var runs = new SqliteTestRunRepository(_database);
        var lokalno = new DateTime(2026, 9, 7, 11, 44, 11, DateTimeKind.Local);
        runs.Add(Run(1, lokalno));

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT TestedAt FROM TestRun;";
        string zapis = (string)command.ExecuteScalar()!;

        Assert.EndsWith("Z", zapis, StringComparison.Ordinal);
        Assert.Equal(lokalno.ToUniversalTime().ToString(IsoDate.Format, System.Globalization.CultureInfo.InvariantCulture), zapis);

        // Nazad se čita u lokalnom vremenu, isto kao što je i upisano.
        Assert.Equal(lokalno, runs.GetRecent(1)[0].TestedAt);
    }

    [Fact]
    public void Datumi_NeprepoznatDatumIzCsvPrezivljavaUpisICitanje()
    {
        var runs = new SqliteTestRunRepository(_database);

        // DateTime.MinValue nastaje kad parser ne prepozna datum iz CSV-a.
        Assert.True(runs.Add(Run(1, DateTime.MinValue)));

        Assert.Equal(DateTime.MinValue, runs.GetRecent(1)[0].TestedAt);
        Assert.True(runs.Exists(TestRunKey.Of("M100W1", 1, DateTime.MinValue)));
    }

    [Fact]
    public void IsoDate_PovratnoPretvaranje()
    {
        var trenutak = new DateTime(2026, 9, 7, 11, 44, 11);

        string zapis = IsoDate.ToStorage(trenutak);
        Assert.EndsWith("Z", zapis, StringComparison.Ordinal);
        Assert.Equal(trenutak, IsoDate.FromStorage(zapis));

        Assert.Equal(DateTime.MinValue, IsoDate.FromStorage(null));
        Assert.Equal(DateTime.MinValue, IsoDate.FromStorage("ovo nije datum"));
        Assert.Equal(IsoDate.UnknownValue, IsoDate.ToStorage(DateTime.MinValue));
    }

    // -------------------------------------------------------------------------------------
    // Ceo put: CSV → parser → baza
    // -------------------------------------------------------------------------------------

    [Fact]
    public void CeoPut_CsvSeParsiraVezeZaKablIUpisujeBezDuplikata()
    {
        const string crLf = "\r\n";
        string csv =
            "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit," + crLf +
            "1,M30-W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS," + crLf +
            "2,M30-W1,fail,2026/09/07,11:47:34,,,Miloš,FAIL,SHORT O01-O02;FAIL," + crLf;

        var cables = new SqliteCableRepository(_database);
        var runs = new SqliteTestRunRepository(_database);

        List<TestRun> procitani = ResultCsvParser.ParseAll(csv).Runs.ToList();
        foreach (TestRun run in procitani)
        {
            run.CableId = cables.GetBySpecFileName(run.SpecFileName)?.Id;
        }

        Assert.Equal(2, runs.AddRange(procitani));
        Assert.All(runs.GetRecent(10), r => Assert.True(r.IsRecognized));

        // Isti fajl pročitan ponovo (prepisan istim imenom) ne sme da napravi duplikate.
        var warnings = new List<string>();
        List<TestRun> ponovo = ResultCsvParser.ParseAll(csv).Runs.ToList();

        Assert.Equal(0, runs.AddRange(ponovo, warnings));
        Assert.Equal(2, runs.Count());
        Assert.Equal(2, warnings.Count);

        TestRun pao = runs.GetRecent(10).First(r => !r.Passed);
        Assert.Equal("Kratak spoj između tačaka O01 i O02", Assert.Single(pao.Defects).Describe());
    }

    private static TestRun Run(int seq, DateTime testedAt, string specFileName = "M100W1", long? cableId = null)
        => new()
        {
            CableId = cableId,
            Seq = seq,
            SpecFileName = specFileName,
            Passed = true,
            TestedAt = testedAt,
            Operator = "Miloš"
        };
}
