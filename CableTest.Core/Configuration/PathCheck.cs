using CableTest.Core.Spec;

namespace CableTest.Core.Configuration;

/// <summary>Težina nalaza pri proveri putanje.</summary>
public enum PathIssueSeverity
{
    /// <summary>Sve je u redu.</summary>
    Ok,

    /// <summary>Radiće, ali nešto nije kako treba.</summary>
    Warning,

    /// <summary>Ovako ne može da radi.</summary>
    Error
}

/// <summary>Jedan nalaz provere putanje.</summary>
public sealed record PathIssue(PathIssueSeverity Severity, string Message)
{
    public bool IsError => Severity == PathIssueSeverity.Error;

    public bool IsWarning => Severity == PathIssueSeverity.Warning;

    public override string ToString() => Message;
}

/// <summary>
/// Provera putanja iz podešavanja — pri unosu i pri pokretanju.
/// </summary>
/// <remarks>
/// Postoji zato što se sve ove greške inače javljaju tek u najgorem trenutku: operater pritisne
/// „Pripremi test", a tek tada se sazna da spec folder ne postoji ili da je u OneDrive-u. Bolje
/// je reći odmah.
/// </remarks>
public static class PathCheck
{
    /// <summary>Deo putanje po kome se prepoznaje OneDrive.</summary>
    public const string OneDriveMarker = "OneDrive";

    /// <summary>
    /// Poruka o OneDrive putanji. CableConnector u tom slučaju javlja „Could not find file",
    /// jer OneDrive fajl može držati samo u oblaku, bez sadržaja na disku.
    /// </summary>
    public const string OneDriveMessage =
        "Putanja je unutar OneDrive foldera. CableConnector tada javlja grešku " +
        "\"Could not find file\", jer OneDrive fajl može držati samo u oblaku. " +
        "Izaberi folder van OneDrive-a, npr. na C: disku.";

    /// <summary>Da li je putanja unutar OneDrive foldera.</summary>
    public static bool IsInOneDrive(string? path)
        => !string.IsNullOrWhiteSpace(path)
           && path.Contains(OneDriveMarker, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Provera foldera u koji aplikacija upisuje (spec folder): postoji li, da li je u OneDrive-u
    /// i da li se u njega može upisati.
    /// </summary>
    public static IReadOnlyList<PathIssue> CheckSpecFolder(string? folder)
    {
        var issues = new List<PathIssue>();

        if (string.IsNullOrWhiteSpace(folder))
        {
            issues.Add(new PathIssue(PathIssueSeverity.Error, "Spec folder nije podešen."));
            return issues;
        }

        if (IsInOneDrive(folder))
        {
            issues.Add(new PathIssue(PathIssueSeverity.Warning, "Spec folder: " + OneDriveMessage));
        }

        if (!Directory.Exists(folder))
        {
            issues.Add(new PathIssue(
                PathIssueSeverity.Error,
                $"Spec folder \"{folder}\" ne postoji."));
            return issues;
        }

        if (!CanWriteTo(folder, out string? reason))
        {
            issues.Add(new PathIssue(
                PathIssueSeverity.Error,
                $"U spec folder \"{folder}\" se ne može upisivati: {reason}"));
        }

        return issues;
    }

    /// <summary>Provera putanje sa rezultatima: fajl ili folder koji aplikacija samo čita.</summary>
    public static IReadOnlyList<PathIssue> CheckResultPath(string? path)
    {
        var issues = new List<PathIssue>();

        if (string.IsNullOrWhiteSpace(path))
        {
            issues.Add(new PathIssue(
                PathIssueSeverity.Error,
                "Putanja CSV fajla sa rezultatima nije podešena — rezultati neće stizati."));
            return issues;
        }

        if (IsInOneDrive(path))
        {
            issues.Add(new PathIssue(PathIssueSeverity.Warning, "Putanja rezultata: " + OneDriveMessage));
        }

        if (File.Exists(path))
        {
            return issues;
        }

        if (Directory.Exists(path))
        {
            bool imaCsv = false;
            try
            {
                imaCsv = Directory.EnumerateFiles(path, "*.csv").Any();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                issues.Add(new PathIssue(
                    PathIssueSeverity.Error,
                    $"Folder sa rezultatima \"{path}\" nije dostupan: {ex.Message}"));
                return issues;
            }

            if (!imaCsv)
            {
                issues.Add(new PathIssue(
                    PathIssueSeverity.Warning,
                    $"U folderu \"{path}\" još nema nijednog .csv fajla. " +
                    "Nadgledanje čeka da se pojavi."));
            }

            return issues;
        }

        issues.Add(new PathIssue(
            PathIssueSeverity.Warning,
            $"Putanja \"{path}\" još ne postoji. Nadgledanje čeka da se pojavi."));

        return issues;
    }

    /// <summary>Provera šablona MASTER.c61: postoji li i da li je ispravan .c61 fajl.</summary>
    public static PathIssue CheckTemplate(string? templatePath)
    {
        string path = string.IsNullOrWhiteSpace(templatePath)
            ? Path.Combine(AppContext.BaseDirectory, Gateway.TesterGatewayOptions.DefaultTemplatePath)
            : templatePath;

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        if (!File.Exists(path))
        {
            return new PathIssue(
                PathIssueSeverity.Error,
                $"Šablon \"{path}\" nije pronađen. Bez njega se .c61 ne može generisati.");
        }

        try
        {
            C61Generator.FromFile(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return new PathIssue(PathIssueSeverity.Error, $"Šablon \"{path}\" nije ispravan: {ex.Message}");
        }

        return new PathIssue(PathIssueSeverity.Ok, $"Šablon je pronađen i ispravan: {path}");
    }

    /// <summary>Sve provere zajedno, za prikaz pri pokretanju.</summary>
    public static IReadOnlyList<PathIssue> CheckAll(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var issues = new List<PathIssue>();
        issues.AddRange(CheckSpecFolder(settings.SpecFolder));
        issues.AddRange(CheckResultPath(settings.ResultPath));

        PathIssue template = CheckTemplate(settings.TemplatePath);
        if (template.Severity != PathIssueSeverity.Ok)
        {
            issues.Add(template);
        }

        return issues;
    }

    /// <summary>Pokušava upis privremenog fajla — jedini pouzdan način da se sazna da li se sme pisati.</summary>
    private static bool CanWriteTo(string folder, out string? reason)
    {
        string probe = Path.Combine(folder, ".cabletest-provera-" + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            using (FileStream stream = File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
                stream.WriteByte(0);
            }

            reason = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            reason = ex.Message;
            return false;
        }
    }
}
