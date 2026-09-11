using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

public class TestPointTests
{
    [Theory]
    [InlineData("A01", 'A', 1)]
    [InlineData("O31", 'O', 31)]
    [InlineData("P32", 'P', 32)]
    [InlineData("o1", 'O', 1)]        // malo slovo i jedna cifra se prihvataju
    [InlineData("  O02  ", 'O', 2)]   // razmaci se uklanjaju
    public void Parse_PrihvataIspravneOznake(string text, char port, int number)
    {
        TestPoint point = TestPoint.Parse(text);

        Assert.Equal(port, point.Port);
        Assert.Equal(number, point.Number);
    }

    [Theory]
    [InlineData("A01", "A01")]
    [InlineData("o1", "O01")]
    [InlineData("p9", "P09")]
    public void ToString_UvekVelikoSlovoIDveCifre(string input, string expected)
    {
        Assert.Equal(expected, TestPoint.Parse(input).ToString());
    }

    [Theory]
    [InlineData("Q01")]   // port van opsega A..P
    [InlineData("Z32")]
    [InlineData("O33")]   // broj van opsega 1..32
    [InlineData("O00")]
    [InlineData("O001")]  // tri cifre
    [InlineData("OO1")]   // druga pozicija nije cifra
    [InlineData("O")]     // nema broja
    [InlineData("01")]    // nema porta
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_OdbijaNeispravneOznake(string? text)
    {
        bool ok = TestPoint.TryParse(text, out TestPoint _, out string? error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Parse_NeispravnaOznakaBacaFormatExceptionSaPorukom()
    {
        FormatException ex = Assert.Throws<FormatException>(() => TestPoint.Parse("Q01"));

        Assert.Contains("Q01", ex.Message);
    }

    [Fact]
    public void Index_PokrivaSvih512Tacaka()
    {
        Assert.Equal(0, new TestPoint('A', 1).Index);
        Assert.Equal(31, new TestPoint('A', 32).Index);
        Assert.Equal(448, new TestPoint('O', 1).Index);
        Assert.Equal(511, new TestPoint('P', 32).Index);
        Assert.Equal(512, TestPoint.TotalPoints);
    }

    [Fact]
    public void Konstruktor_OdbijaVrednostiVanOpsega()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestPoint('Q', 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestPoint('A', 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestPoint('A', 33));
    }

    [Fact]
    public void JednakostIPoredjenje_RadePoIndeksu()
    {
        Assert.Equal(TestPoint.Parse("O01"), TestPoint.Parse("o1"));
        Assert.NotEqual(TestPoint.Parse("O01"), TestPoint.Parse("O02"));
        Assert.True(TestPoint.Parse("A01").CompareTo(TestPoint.Parse("B01")) < 0);
    }
}
