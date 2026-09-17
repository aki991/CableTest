using CableTest.Core.Model;

namespace CableTest.Core.Data;

/// <summary>
/// Vozila.
/// </summary>
/// <remarks>
/// Interfejsi postoje da GUI ne zna za SQL: prikaz radi sa <see cref="Vehicle"/>,
/// <see cref="Cable"/> i <see cref="TestRun"/>, a gde se to čuva je stvar ovog sloja.
/// Isto omogućava da se u testovima GUI-ja podmetne repozitorijum bez baze.
/// </remarks>
public interface IVehicleRepository
{
    IReadOnlyList<Vehicle> GetAll();

    Vehicle? GetById(long id);

    Vehicle? GetByName(string name);

    /// <summary>Upisuje vozilo i vraća dodeljeni <see cref="Vehicle.Id"/>.</summary>
    long Add(Vehicle vehicle);

    void Update(Vehicle vehicle);

    /// <summary>Briše vozilo i sve njegove kablove sa netovima.</summary>
    void Delete(long id);
}

/// <summary>Kablovi sa svojim net listama.</summary>
public interface ICableRepository
{
    /// <summary>Svi kablovi, sa učitanim netovima.</summary>
    IReadOnlyList<Cable> GetAll();

    /// <summary>Kablovi vozila koji se nude za ispitivanje; ugašeni se ne prikazuju.</summary>
    IReadOnlyList<Cable> GetByVehicle(long vehicleId);

    /// <summary>Svi kablovi vozila, uključujući ugašene — za katalog i istoriju.</summary>
    IReadOnlyList<Cable> GetByVehicleIncludingInactive(long vehicleId);

    Cable? GetById(long id);

    /// <summary>
    /// Kabl po imenu spec fajla — ovim se rezultat iz CSV-a vezuje za kabl.
    /// Poređenje je bez obzira na velika i mala slova i podnosi zapis sa ekstenzijom.
    /// </summary>
    Cable? GetBySpecFileName(string? specFileName);

    /// <summary>Upisuje kabl zajedno sa netovima i vraća dodeljeni <see cref="Cable.Id"/>.</summary>
    long Add(Cable cable);

    /// <summary>Menja kabl; net lista se upisuje iznova, onakva kakva je u objektu.</summary>
    void Update(Cable cable);

    void Delete(long id);
}

/// <summary>Istorija izvršenih testova.</summary>
public interface ITestRunRepository
{
    /// <summary>
    /// Upisuje jedan rezultat sa njegovim greškama. Zapis koji već postoji po prirodnom ključu
    /// (<see cref="TestRunKey"/>) se tiho preskače — uz upozorenje u
    /// <paramref name="warnings"/>, ako je lista zadata.
    /// </summary>
    /// <returns><c>true</c> ako je zapis upisan, <c>false</c> ako je preskočen kao duplikat.</returns>
    bool Add(TestRun run, ICollection<string>? warnings = null);

    /// <summary>Upisuje više rezultata u jednoj transakciji. Vraća koliko ih je zaista upisano.</summary>
    int AddRange(IEnumerable<TestRun> runs, ICollection<string>? warnings = null);

    /// <summary>Da li zapis sa tim prirodnim ključem već postoji.</summary>
    bool Exists(TestRunKey key);

    /// <summary>Poslednjih <paramref name="count"/> testova, najnoviji prvi.</summary>
    IReadOnlyList<TestRun> GetRecent(int count);

    /// <summary>Testovi jednog kabla, najnoviji prvi.</summary>
    IReadOnlyList<TestRun> GetByCable(long cableId, int count = 100);

    /// <summary>Poslednji test jednog kabla, ili <c>null</c> ako ga nema.</summary>
    TestRun? GetLatestForCable(long cableId);

    TestRun? GetById(long id);

    /// <summary>Ukupan broj zapisa.</summary>
    int Count();
}
