using System.Text.Json;
using System.Text.Json.Serialization;

namespace CableTest.Core.Configuration;

/// <summary>Čitanje i upis <see cref="AppSettings"/> u <c>settings.json</c>.</summary>
/// <remarks>
/// Neispravan ili oštećen fajl se ne smatra greškom koja zaustavlja aplikaciju: vraćaju se
/// podrazumevana podešavanja, a razlog se javlja kroz <see cref="LastError"/>. Aplikacija u
/// pogonu mora da se pokrene i onda kad je neko ručno menjao fajl.
/// </remarks>
public sealed class SettingsStore
{
    /// <summary>Ime fajla sa podešavanjima.</summary>
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true
    };

    public SettingsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Putanja fajla sa podešavanjima nije zadata.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath);
    }

    /// <summary>Podrazumevano mesto: <c>%APPDATA%\CableTest\settings.json</c>.</summary>
    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CableTest",
        FileName);

    /// <summary>Store na podrazumevanoj putanji.</summary>
    public static SettingsStore Default() => new(DefaultFilePath);

    /// <summary>Puna putanja fajla sa podešavanjima.</summary>
    public string FilePath { get; }

    /// <summary>Poslednja greška pri čitanju ili upisu, ili <c>null</c>.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Učitava podešavanja. Ako fajla nema, vraća podrazumevana i <b>ne</b> pravi fajl —
    /// fajl nastaje tek kad korisnik nešto sačuva.
    /// </summary>
    public AppSettings Load()
    {
        LastError = null;

        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LastError =
                $"Podešavanja iz \"{FilePath}\" nisu pročitana ({ex.Message}). " +
                "Rad se nastavlja sa podrazumevanim vrednostima.";
            return new AppSettings();
        }
    }

    /// <summary>Upisuje podešavanja. Vraća <c>false</c> ako upis nije uspeo; razlog je u <see cref="LastError"/>.</summary>
    public bool Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        LastError = null;

        try
        {
            string? folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LastError = $"Podešavanja nisu sačuvana u \"{FilePath}\": {ex.Message}";
            return false;
        }
    }
}
