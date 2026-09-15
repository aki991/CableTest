using System.Diagnostics;

namespace CableTest.App.Services;

/// <summary>Otvaranje gotovog dokumenta (merne karte) u programu koji je za njega podešen.</summary>
public interface IDocumentViewer
{
    /// <summary>Otvara fajl sa zadate putanje.</summary>
    void Open(string path);
}

/// <summary>Otvara dokument u podrazumevanom pregledaču.</summary>
/// <remarks>
/// <c>UseShellExecute = true</c> je ovde obavezno: bez njega .NET pokušava da .html fajl pokrene
/// kao program i javlja grešku. Ovako se koristi ista veza tipova fajlova koju korisnik ima u
/// Windows-u, pa se merna karta otvara u pregledaču koji mu je poznat i u kome ume da štampa.
/// </remarks>
public sealed class BrowserDocumentViewer : IDocumentViewer
{
    public void Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Putanja dokumenta nije zadata.", nameof(path));
        }

        using Process? process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        _ = process;
    }
}
