namespace CableTest.App.UiAutomation;

/// <summary>
/// Poređenje imena kontrola onako kako ih UI Automation prijavljuje.
/// </summary>
/// <remarks>
/// <para>
/// Ime koje stoji na dugmetu i ime koje UIA vrati retko su isti niz znakova: oko njega ume da
/// bude razmak, unutra dvostruki razmak ili prelom reda, a kod menija i znak <c>&amp;</c> za
/// prečicu. Zato se traži u koracima — prvo tačno, pa tek onda „sadrži" nad očišćenim imenom.
/// </para>
/// <para>
/// Sve ovde je čista funkcija nad nizovima znakova: nijedan poziv UIA. Tako se red pretrage i
/// pravila poređenja mogu ispitati testom, bez pokrenutog CableConnector-a.
/// </para>
/// </remarks>
public static class UiaNames
{
    /// <summary>Oblik u kome se imena porede: bez okolnog razmaka, sa jednostrukim razmacima, mala slova.</summary>
    /// <remarks>
    /// Mala slova se prave po <see cref="System.Globalization.CultureInfo.InvariantCulture"/>: imena
    /// u CableConnector-u su engleska, a turski „I" na mašini sa drugim regionalnim podešavanjima
    /// promenio bi rezultat poređenja.
    /// </remarks>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(name.Length);
        bool pendingSpace = false;

        foreach (char c in name)
        {
            if (char.IsWhiteSpace(c))
            {
                // Razmak se ne upisuje odmah — tako višestruki razmaci postanu jedan, a oni na
                // kraju imena ne ostanu uopšte.
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>Tačno poklapanje imena, znak za znak.</summary>
    public static bool MatchesExact(string? candidate, string? wanted)
        => !string.IsNullOrEmpty(candidate)
           && !string.IsNullOrEmpty(wanted)
           && string.Equals(candidate, wanted, StringComparison.Ordinal);

    /// <summary>Poklapanje nad očišćenim imenima: traženo ime je sadržano u imenu kontrole.</summary>
    /// <remarks>
    /// Namerno „sadrži", a ne „jednako": tako se pogodi i <c>„&amp;File"</c> i <c>„Save   spec. file"</c>
    /// i dugme čije ime nosi i prečicu na kraju. Cena je da kraće traženo ime može da pogodi više
    /// kontrola — zato ovaj korak dolazi tek posle tačnog poklapanja, a pretraga prijavljuje
    /// koliko je kontrola pogođeno.
    /// </remarks>
    public static bool MatchesNormalizedContains(string? candidate, string? wanted)
    {
        string normalizedWanted = Normalize(wanted);
        if (normalizedWanted.Length == 0)
        {
            return false;
        }

        string normalizedCandidate = Normalize(candidate);
        return normalizedCandidate.Length != 0
               && normalizedCandidate.Contains(normalizedWanted, StringComparison.Ordinal);
    }

    /// <summary>
    /// Imena koja najviše liče na traženo — objašnjenje zašto pretraga nije uspela.
    /// </summary>
    /// <remarks>
    /// „Nije pronađeno" samo po sebi ne kaže ništa: ne zna se da li prozor nema nijednu kontrolu
    /// sa imenom, ili natpis glasi malo drugačije nego što ova strana očekuje. Zato se uz neuspeh
    /// upisuje i šta je od imena zaista viđeno, poređano po broju zajedničkih reči sa traženim.
    /// </remarks>
    public static IReadOnlyList<string> NearestNames(IEnumerable<string?> candidates, string? wanted, int max = 6)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (max <= 0)
        {
            return Array.Empty<string>();
        }

        string[] wantedTokens = Normalize(wanted).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var scored = new List<(string Name, int Score, int Order)>();
        int order = 0;

        foreach (string? candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            string normalized = Normalize(candidate);
            if (normalized.Length == 0 || !seen.Add(normalized))
            {
                continue;
            }

            int score = wantedTokens.Count(token => normalized.Contains(token, StringComparison.Ordinal));
            scored.Add((candidate.Trim(), score, order++));
        }

        // Prvo ona koja dele najviše reči sa traženim; kad nijedno ne deli nijednu, prvih nekoliko
        // viđenih — i to je podatak: znači da su imena sasvim drugačija ili da ih uopšte nema.
        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Order)
            .Take(max)
            .Select(x => x.Name)
            .ToArray();
    }
}
