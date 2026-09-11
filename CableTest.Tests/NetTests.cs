using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

public class NetTests
{
    [Fact]
    public void Parse_DemoNetImaCetiriTacke()
    {
        Net net = Net.Parse("O01-O02-O31-O32");

        Assert.Equal(4, net.Count);
        Assert.Equal("O01-O02-O31-O32", net.ToString());
    }

    [Fact]
    public void Parse_NetSmeDaSpajaTackeSaRazlicitihPortova()
    {
        Net net = Net.Parse("A01-P32");

        Assert.Equal("A01-P32", net.ToString());
    }

    [Fact]
    public void Parse_NormalizujeZapis()
    {
        Net net = Net.Parse(" o1 - o2 - o31 - o32 ");

        Assert.Equal("O01-O02-O31-O32", net.ToString());
    }

    [Theory]
    [InlineData("O01")]              // jedna tačka nije net
    [InlineData("O01-O01")]          // ista tačka dva puta
    [InlineData("O01-Q02")]          // neispravan port
    [InlineData("O01-O33")]          // broj van opsega
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_OdbijaNeispravneNetove(string? text)
    {
        bool ok = Net.TryParse(text, out Net? net, out string? error);

        Assert.False(ok);
        Assert.Null(net);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Konstruktor_OdbijaDuplikateITackuManjeOdDve()
    {
        Assert.Throws<ArgumentException>(() => new Net(new[] { TestPoint.Parse("O01") }));
        Assert.Throws<ArgumentException>(() => new Net(new[] { TestPoint.Parse("O01"), TestPoint.Parse("O01") }));
    }

    [Fact]
    public void Jednakost_ZavisiOdRedosledaTacaka()
    {
        Assert.Equal(Net.Parse("O01-O02"), Net.Parse("o1-o2"));
        Assert.NotEqual(Net.Parse("O01-O02"), Net.Parse("O02-O01"));
    }
}
