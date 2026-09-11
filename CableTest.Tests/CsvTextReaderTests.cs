using System.Text;
using CableTest.Core.Results;
using Xunit;

namespace CableTest.Tests;

public class CsvTextReaderTests
{
    private const string CrLf = "\r\n";

    /// <summary>Red rezultata u kome operater ima srpsko slovo u imenu.</summary>
    private const string SampleCsv =
        "File Name: PROBA1" + CrLf +
        "Model:8761NK" + CrLf +
        "O/S:5KOHM" + CrLf +
        "-------------------------------" + CrLf +
        "001 O01-O02-O31-O32" + CrLf +
        "-------------------------------" + CrLf +
        "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit," + CrLf +
        "1,PROBA1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS," + CrLf;

    private static Encoding Windows1250
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1250);
        }
    }

    [Fact]
    public void Decode_Utf8_CitaSrpskaSlova()
    {
        byte[] bytes = new UTF8Encoding(false).GetBytes(SampleCsv);

        string text = CsvTextReader.Decode(bytes, out string encodingName);

        Assert.Equal(CsvTextReader.Utf8Name, encodingName);
        Assert.Contains(",Miloš,PASS,PASS,", text, StringComparison.Ordinal);
        Assert.Equal(SampleCsv, text);
    }

    [Fact]
    public void Decode_Windows1250_CitaSrpskaSlova()
    {
        byte[] bytes = Windows1250.GetBytes(SampleCsv);

        // Isti sadržaj u drugom kodiranju daje druge bajtove — inače test ne bi ništa dokazivao.
        Assert.NotEqual(new UTF8Encoding(false).GetBytes(SampleCsv), bytes);

        string text = CsvTextReader.Decode(bytes, out string encodingName);

        Assert.Equal(CsvTextReader.FallbackName, encodingName);
        Assert.Contains(",Miloš,PASS,PASS,", text, StringComparison.Ordinal);
        Assert.Equal(SampleCsv, text);
    }

    [Fact]
    public void Decode_Utf8SaBom_UklanjaBom()
    {
        byte[] bytes = new UTF8Encoding(true).GetPreamble()
            .Concat(new UTF8Encoding(false).GetBytes(SampleCsv))
            .ToArray();

        string text = CsvTextReader.Decode(bytes, out string encodingName);

        Assert.Equal(CsvTextReader.Utf8Name, encodingName);
        Assert.StartsWith("File Name: PROBA1", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\uFEFF', text);
    }

    [Fact]
    public void Decode_CistAscii_IdePrekoUtf8()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("Seq.,Filename,Pass," + CrLf + "1,PROBA1,pass," + CrLf);

        string text = CsvTextReader.Decode(bytes, out string encodingName);

        Assert.Equal(CsvTextReader.Utf8Name, encodingName);
        Assert.Contains("PROBA1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_PrazanFajl_DajePrazanTekst()
    {
        Assert.Equal(string.Empty, CsvTextReader.Decode(Array.Empty<byte>()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadAllText_CitaFajlKojiJeOtvorenUDrugomProgramu(bool utf8)
    {
        string path = Path.Combine(Path.GetTempPath(), "CableTestCsv_" + Guid.NewGuid().ToString("N") + ".csv");
        byte[] bytes = utf8 ? new UTF8Encoding(false).GetBytes(SampleCsv) : Windows1250.GetBytes(SampleCsv);
        File.WriteAllBytes(path, bytes);

        try
        {
            // Simulira CableConnector koji fajl drži otvorenim za upis dok mi čitamo.
            using var held = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);

            string text = CsvTextReader.ReadAllText(path, out string encodingName);

            Assert.Equal(utf8 ? CsvTextReader.Utf8Name : CsvTextReader.FallbackName, encodingName);
            Assert.Contains(",Miloš,PASS,PASS,", text, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
