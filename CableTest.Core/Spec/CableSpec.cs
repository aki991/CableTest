using CableTest.Core.Model;

namespace CableTest.Core.Spec;

/// <summary>
/// Net lista kabla spremna za upis u .c61.
/// </summary>
/// <remarks>
/// Jedno mesto na kome se odlučuje odakle net lista dolazi: kabl sa unetim ožičenjem je izvodi iz
/// terminala i provodnika, a stariji kabl bez ožičenja koristi ono što je ručno upisano u bazi.
/// Ostatak aplikacije o toj razlici ne treba da zna.
/// </remarks>
public static class CableSpec
{
    /// <summary>Izvedena net lista sa razlozima, bez bacanja izuzetka.</summary>
    public static NetDerivation Derive(Cable cable)
    {
        ArgumentNullException.ThrowIfNull(cable);

        return cable.HasWiring
            ? NetBuilder.Derive(cable)
            : new NetDerivation(cable.Nets.OrderBy(n => n.Ordinal).ToArray(), Array.Empty<string>());
    }

    /// <summary>
    /// Net lista za generator .c61.
    /// </summary>
    /// <remarks>
    /// Kad se net lista ne može izvesti, .c61 se <b>ne upisuje</b>: program koji ode u tester mora
    /// da odgovara kablu na stolu. Poruka je ona koju vidi operater — kaže šta tačno nedostaje.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Kabl nije spreman za ispitivanje.</exception>
    public static IReadOnlyList<Net> NetsFor(Cable cable)
    {
        NetDerivation derivation = Derive(cable);

        if (!derivation.IsValid)
        {
            throw new InvalidOperationException(derivation.ErrorText);
        }

        if (derivation.Nets.Count == 0)
        {
            throw new InvalidOperationException(
                $"{NetBuilder.NotReadyPrefix}kabl {cable.Code} nema nijedan net. " +
                "Unesi ožičenje kabla ili net listu pre pripreme test programa.");
        }

        return derivation.ToNets();
    }
}
