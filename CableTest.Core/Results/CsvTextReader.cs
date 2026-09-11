using System.Text;

namespace CableTest.Core.Results;

/// <summary>
/// Čitanje CSV fajla sa rezultatima koji upisuje CableConnector.
/// </summary>
/// <remarks>
/// Za razliku od .c61 fajla, koji je naš i uvek ASCII, CSV piše tuđi program i u kolonama
/// "Operater" i "Barcode1" se mogu naći srpska slova ("Miloš"). Zato se ne pretpostavlja ASCII:
/// prvo se pokušava UTF-8 sa strogom proverom, pa se, ako bajtovi nisu ispravan UTF-8,
/// pada na Windows-1250 (srpska latinica na Windows-u).
/// </remarks>
public static class CsvTextReader
{
    /// <summary>Kodna strana na koju se pada kada sadržaj nije ispravan UTF-8.</summary>
    public const int FallbackCodePage = 1250;

    public const string Utf8Name = "UTF-8";
    public const string FallbackName = "windows-1250";

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Encoding Fallback;

    static CsvTextReader()
    {
        // Windows-1250 nije u osnovnom skupu .NET-a; dolazi iz System.Text.Encoding.CodePages.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Fallback = Encoding.GetEncoding(FallbackCodePage);
    }

    /// <summary>Dekodira sadržaj CSV-a. Vraća tekst bez BOM-a.</summary>
    public static string Decode(byte[] bytes) => Decode(bytes, out _);

    /// <summary>
    /// Dekodira sadržaj CSV-a i kroz <paramref name="encodingName"/> javlja koje je kodiranje upotrebljeno.
    /// </summary>
    public static string Decode(byte[] bytes, out string encodingName)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        int offset = HasUtf8Bom(bytes) ? 3 : 0;

        try
        {
            string text = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
            encodingName = Utf8Name;
            return text;
        }
        catch (DecoderFallbackException)
        {
            encodingName = FallbackName;
            return Fallback.GetString(bytes, offset, bytes.Length - offset);
        }
    }

    /// <summary>
    /// Čita ceo CSV fajl. Otvara ga sa <see cref="FileShare.ReadWrite"/> jer ga CableConnector
    /// u tom trenutku može držati otvorenim za upis.
    /// </summary>
    public static string ReadAllText(string path) => ReadAllText(path, out _);

    /// <inheritdoc cref="ReadAllText(string)"/>
    public static string ReadAllText(string path, out string encodingName)
        => Decode(ReadAllBytes(path), out encodingName);

    /// <summary>Čita bajtove fajla koji je možda otvoren u drugom programu.</summary>
    public static byte[] ReadAllBytes(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static bool HasUtf8Bom(byte[] bytes)
        => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
}
