using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CableTest.App.Mvvm;
using CableTest.Core.Data;
using CableTest.Core.Model;

namespace CableTest.App.ViewModels;

/// <summary>Jedno vozilo u pregledu, sa prebrojanim sadržajem.</summary>
/// <param name="Name">Ime vozila.</param>
/// <param name="CableCount">Koliko kablova vozilo ima.</param>
/// <param name="NetCount">Ukupan broj netova svih njegovih kablova.</param>
/// <param name="PointCount">Ukupan broj ispitnih tačaka svih njegovih kablova.</param>
/// <param name="Range">Prva i poslednja oznaka kabla, npr. „M30-W1 … M30-W20".</param>
public sealed record VehicleRow(
    string Name,
    int CableCount,
    int NetCount,
    int PointCount,
    string Range);

/// <summary>Jedan kabl u pregledu kataloga.</summary>
/// <param name="Code">Oznaka kabla.</param>
/// <param name="VehicleName">Vozilo kome kabl pripada.</param>
/// <param name="Description">Naziv kabla.</param>
/// <param name="NetCount">Broj netova.</param>
/// <param name="PointCount">Broj ispitnih tačaka.</param>
/// <param name="Ports">Slova portova koje kabl koristi.</param>
/// <param name="SpecFile">Ime .c61 fajla.</param>
public sealed record CableRow(
    string Code,
    string VehicleName,
    string Description,
    int NetCount,
    int PointCount,
    string Ports,
    string SpecFile);

/// <summary>
/// Katalog vozila i kablova — ono što prikazuju ekrani „Vozila", „Kablovi" i „Test programi".
/// </summary>
/// <remarks>
/// Katalog se čita jednom, pri pokretanju: u pogonu se ne menja iz same aplikacije, a spisak od
/// nekoliko desetina kablova sa net listama ne treba da se dohvata iz baze pri svakom prelasku
/// sa ekrana na ekran.
/// </remarks>
public sealed class CatalogViewModel : ObservableObject
{
    private readonly IVehicleRepository _vehicles;
    private readonly ICableRepository _cables;

    private string _cableFilter = string.Empty;
    private string _programFilter = string.Empty;

    public CatalogViewModel(IVehicleRepository vehicles, ICableRepository cables)
    {
        ArgumentNullException.ThrowIfNull(vehicles);
        ArgumentNullException.ThrowIfNull(cables);

        _vehicles = vehicles;
        _cables = cables;

        CollectionViewSource.GetDefaultView(CableRows).Filter = o => Matches(o, _cableFilter);
        CollectionViewSource.GetDefaultView(ProgramRows).Filter = o => Matches(o, _programFilter);
    }

    /// <summary>Vozila sa prebrojanim sadržajem.</summary>
    public ObservableCollection<VehicleRow> VehicleRows { get; } = new();

    /// <summary>Svi kablovi — ekran „Kablovi".</summary>
    public ObservableCollection<CableRow> CableRows { get; } = new();

    /// <summary>Isti kablovi, gledani kao test programi — ekran „Test programi".</summary>
    public ObservableCollection<CableRow> ProgramRows { get; } = new();

    /// <summary>Ukupan broj kablova u katalogu.</summary>
    public int CableCount => CableRows.Count;

    /// <summary>Ukupan broj vozila u katalogu.</summary>
    public int VehicleCount => VehicleRows.Count;

    /// <summary>Tekst pretrage na ekranu „Kablovi".</summary>
    public string CableFilter
    {
        get => _cableFilter;
        set
        {
            if (Set(ref _cableFilter, value))
            {
                CollectionViewSource.GetDefaultView(CableRows).Refresh();
            }
        }
    }

    /// <summary>Tekst pretrage na ekranu „Test programi".</summary>
    public string ProgramFilter
    {
        get => _programFilter;
        set
        {
            if (Set(ref _programFilter, value))
            {
                CollectionViewSource.GetDefaultView(ProgramRows).Refresh();
            }
        }
    }

    /// <summary>Učitava katalog iz baze.</summary>
    public void Load()
    {
        VehicleRows.Clear();
        CableRows.Clear();
        ProgramRows.Clear();

        IReadOnlyList<Vehicle> vehicles = _vehicles.GetAll();
        IReadOnlyList<Cable> cables = _cables.GetAll();

        foreach (Vehicle vehicle in vehicles)
        {
            Cable[] njegovi = cables.Where(c => c.VehicleId == vehicle.Id).ToArray();

            VehicleRows.Add(new VehicleRow(
                vehicle.Name,
                njegovi.Length,
                njegovi.Sum(c => c.Nets.Count),
                njegovi.Sum(CableLayout.CountPoints),
                Range(njegovi)));
        }

        foreach (Cable cable in cables)
        {
            string vehicleName = vehicles.FirstOrDefault(v => v.Id == cable.VehicleId)?.Name ?? NetRow.Unknown;

            var row = new CableRow(
                cable.Code,
                vehicleName,
                string.IsNullOrWhiteSpace(cable.Description) ? NetRow.Unknown : cable.Description,
                cable.Nets.Count,
                CableLayout.CountPoints(cable),
                CableLayout.PortLetters(cable),
                cable.SpecFileNameWithExtension);

            CableRows.Add(row);
            ProgramRows.Add(row);
        }

        RaiseAll(nameof(CableCount), nameof(VehicleCount));
    }

    private static string Range(IReadOnlyList<Cable> cables) => cables.Count switch
    {
        0 => NetRow.Unknown,
        1 => cables[0].Code,
        _ => $"{cables[0].Code} … {cables[^1].Code}"
    };

    private static bool Matches(object item, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        if (item is not CableRow row)
        {
            return false;
        }

        string trazeno = filter.Trim();

        return row.Code.Contains(trazeno, StringComparison.OrdinalIgnoreCase)
               || row.Description.Contains(trazeno, StringComparison.OrdinalIgnoreCase)
               || row.VehicleName.Contains(trazeno, StringComparison.OrdinalIgnoreCase)
               || row.SpecFile.Contains(trazeno, StringComparison.OrdinalIgnoreCase);
    }
}
