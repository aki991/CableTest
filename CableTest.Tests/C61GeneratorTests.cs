using System.Globalization;
using System.Text;
using CableTest.Core.Model;
using CableTest.Core.Spec;
using Xunit;

namespace CableTest.Tests;

public class C61GeneratorTests
{
    private const string CrLf = "\r\n";

    /// <summary>Putanja do stvarnog šablona koji aplikacija koristi.</summary>
    private static string MasterPath => Path.Combine(AppContext.BaseDirectory, "templates", "MASTER.c61");

    private static string MasterText => Encoding.ASCII.GetString(File.ReadAllBytes(MasterPath));

    private static C61Generator Master() => C61Generator.FromFile(MasterPath);

    private static Net DemoNet() => Net.Parse("O01-O02-O31-O32");

    /// <summary>Deli sadržaj na deo pre "OSNet=" i deo od "RLCNet=" nadalje — sve što se ne sme menjati.</summary>
    private static (string Head, string Tail) SplitAroundOsNetBlock(string text)
    {
        int head = text.IndexOf("OSNet=", StringComparison.Ordinal);
        int tail = text.IndexOf("RLCNet=", StringComparison.Ordinal);
        Assert.True(head >= 0, "U sadržaju nema reda OSNet=.");
        Assert.True(tail > head, "U sadržaju nema reda RLCNet= posle OSNet bloka.");
        return (text[..head], text[tail..]);
    }

    [Fact]
    public void Master_SablonSeUcitava()
    {
        C61Generator generator = Master();

        Assert.Contains("Machine information:CT-8761NK", generator.TemplateText, StringComparison.Ordinal);
        Assert.Contains("OSNet=", generator.TemplateText, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_JedanNet_UpisujeBrojIRedNeta()
    {
        string text = Master().Build(new[] { DemoNet() });

        Assert.Contains("OSNet=1" + CrLf + "OSNet:O01-O02-O31-O32" + CrLf + "RLCNet=", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_BezNetova_UpisujeNulaIEmpty()
    {
        string text = Master().Build(Array.Empty<Net>());

        Assert.Contains("OSNet=0" + CrLf + "OSNet:EMPTY" + CrLf + "RLCNet=", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ViseNetova_UpisujeJedanRedPoNetu()
    {
        Net[] nets =
        {
            Net.Parse("O01-O02-O31-O32"),
            Net.Parse("A01-A02"),
            Net.Parse("B01-P32")
        };

        string text = Master().Build(nets);

        Assert.Contains(
            "OSNet=3" + CrLf +
            "OSNet:O01-O02-O31-O32" + CrLf +
            "OSNet:A01-A02" + CrLf +
            "OSNet:B01-P32" + CrLf +
            "RLCNet=",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SveOsimOsNetBlokaOstajeNepromenjeno()
    {
        string template = MasterText;
        string text = Master().Build(new[] { DemoNet() });

        (string templateHead, string templateTail) = SplitAroundOsNetBlock(template);
        (string resultHead, string resultTail) = SplitAroundOsNetBlock(text);

        Assert.Equal(templateHead, resultHead);
        Assert.Equal(templateTail, resultTail);
    }

    [Fact]
    public void Build_ZavrsneSekcijeOstajuSaPraznimRedovima()
    {
        string text = Master().Build(new[] { DemoNet() });

        Assert.EndsWith("PARADNet=0" + CrLf + CrLf + "PARAVNet=0" + CrLf + CrLf, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ZamenjujePostojeciOsNetBlok()
    {
        // Šablon koji već sadrži netove — ceo blok mora biti zamenjen, ne dopunjen.
        string template =
            "Machine information:CT-8761NK 512 1.012 Merge" + CrLf +
            "OSNet=2" + CrLf +
            "OSNet:A01-A02" + CrLf +
            "OSNet:B01-B02" + CrLf +
            "RLCNet=0" + CrLf +
            "PARAVNet=0" + CrLf + CrLf;

        string text = new C61Generator(template).Build(new[] { DemoNet() });

        Assert.Equal(
            "Machine information:CT-8761NK 512 1.012 Merge" + CrLf +
            "OSNet=1" + CrLf +
            "OSNet:O01-O02-O31-O32" + CrLf +
            "RLCNet=0" + CrLf +
            "PARAVNet=0" + CrLf + CrLf,
            text);
    }

    [Fact]
    public void Build_KoristiSamoCrlfPrelome()
    {
        string text = Master().Build(new[] { DemoNet() });

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                Assert.True(i > 0 && text[i - 1] == '\r', $"Usamljen LF na poziciji {i}.");
            }
        }
    }

    [Fact]
    public void Build_IstaTackaUDvaNeta_BacaGresku()
    {
        Net[] nets = { Net.Parse("O01-O02"), Net.Parse("O02-O03") };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Master().Build(nets));

        Assert.Contains("O02", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Konstruktor_SablonBezOsNetReda_BacaGresku()
    {
        Assert.Throws<InvalidDataException>(() => new C61Generator("Machine information:CT-8761NK" + CrLf));
    }

    [Fact]
    public void WriteSpec_UpisujeCistAsciiBezBom()
    {
        string folder = Path.Combine(Path.GetTempPath(), "CableTestSpec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Master().WriteSpec(folder, "M100-W1", new[] { DemoNet() });

            Assert.Equal(Path.Combine(folder, "M100-W1.c61"), path);

            byte[] bytes = File.ReadAllBytes(path);
            Assert.All(bytes, b => Assert.True(b <= 127, "Fajl sadrži znak koji nije ASCII."));
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "Fajl ima UTF-8 BOM.");
            Assert.Equal(Master().Build(new[] { DemoNet() }), Encoding.ASCII.GetString(bytes));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void WriteSpec_NepostojeciFolder_BacaJasnuGresku()
    {
        string folder = Path.Combine(Path.GetTempPath(), "CableTestNemaOvog_" + Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => Master().WriteSpec(folder, "M100-W1", new[] { DemoNet() }));
    }

    [Theory]
    [InlineData("M100-W1", "M100-W1.c61")]
    [InlineData("CRD-A", "CRD-A.c61")]
    [InlineData("09071126", "09071126.c61")]
    public void BuildSpecFileName_DodajeEkstenziju(string input, string expected)
    {
        Assert.Equal(expected, C61Generator.BuildSpecFileName(input));
    }

    [Theory]
    [InlineData("M100-w1")]       // malo slovo
    [InlineData("M100 W1")]       // razmak
    [InlineData("M100-W1.c61")]   // ekstenziju dodaje generator
    [InlineData("123456789")]     // predugo
    [InlineData("")]
    [InlineData(null)]
    public void BuildSpecFileName_OdbijaNeispravnoIme(string? input)
    {
        Assert.Throws<ArgumentException>(() => C61Generator.BuildSpecFileName(input));
    }

    [Fact]
    public void WriteSpec_NeispravnoImeSeOdbijaPreUpisa()
    {
        string folder = Path.Combine(Path.GetTempPath(), "CableTestSpec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Assert.Throws<ArgumentException>(() => Master().WriteSpec(folder, "M100 W1", new[] { DemoNet() }));
            Assert.Empty(Directory.GetFiles(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Build_NaSrpskojKulturi_DajeIstiSadrzajKaoNaInvarijantnoj()
    {
        // Windows je na srpskom, gde je decimalni separator zarez. .c61 je razdvojen zarezima,
        // pa bi svaki lokalni format srušio fajl. Sve mora ići kroz InvariantCulture.
        Net[] nets = Enumerable.Range(1, 12)
            .Select(i => Net.Parse($"A{i:D2}-B{i:D2}"))
            .ToArray();

        string expected = Master().Build(nets);

        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var serbian = new CultureInfo("sr-RS");
            CultureInfo.CurrentCulture = serbian;
            CultureInfo.CurrentUICulture = serbian;

            // Provera da kultura zaista koristi zarez kao decimalni separator.
            Assert.Equal(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            string actual = Master().Build(nets);

            Assert.Equal(expected, actual);
            Assert.Contains("OSNet=12" + CrLf + "OSNet:A01-B01" + CrLf, actual, StringComparison.Ordinal);
            Assert.DoesNotContain("OSNet=12,", actual, StringComparison.Ordinal);
            Assert.All(Encoding.ASCII.GetBytes(actual), b => Assert.True(b <= 127));

            // I upis na disk mora dati identične bajtove.
            string folder = Path.Combine(Path.GetTempPath(), "CableTestSr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string path = Master().WriteSpec(folder, "M100-W1", nets);
                Assert.Equal(Encoding.ASCII.GetBytes(expected), File.ReadAllBytes(path));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void TackaIBrojNeta_NaSrpskojKulturi_ImajuInvarijantanZapis()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sr-RS");

            Assert.Equal("O01", TestPoint.Parse("o1").ToString());
            Assert.Equal("O01-O02-O31-O32", Net.Parse("o1-o2-o31-o32").ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
