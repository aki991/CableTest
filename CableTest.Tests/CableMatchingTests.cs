using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Jedan kabl — jedan .c61 fajl, pa kolona "Filename" iz CSV-a jednoznačno određuje kabl.
/// </summary>
public class CableMatchingTests
{
    private static Cable DemoCable() => new()
    {
        Id = 1,
        Code = "M100-W1",
        SpecFileName = "M100-W1"
    };

    [Theory]
    [InlineData("M100-W1")]
    [InlineData("m100-w1")]       // tester ume da vrati drugačija slova
    [InlineData("M100-W1.c61")]   // podnosi i zapis sa ekstenzijom
    [InlineData(" M100-W1 ")]
    public void MatchesResultFileName_PrepoznajeSvojeIme(string fromCsv)
    {
        Assert.True(DemoCable().MatchesResultFileName(fromCsv));
    }

    [Theory]
    [InlineData("PROBA1")]
    [InlineData("M100-W2")]
    [InlineData("")]
    [InlineData(null)]
    public void MatchesResultFileName_NePrepoznajeTudjeIme(string? fromCsv)
    {
        Assert.False(DemoCable().MatchesResultFileName(fromCsv));
    }

    [Fact]
    public void MatchesResultFileName_KablBezImenaSpecFajlaNePogadja()
    {
        var cable = new Cable { Code = "M100-W1", SpecFileName = string.Empty };

        Assert.False(cable.MatchesResultFileName(string.Empty));
        Assert.False(cable.MatchesResultFileName(null));
    }

    [Fact]
    public void FindByResultFileName_NalaziKablIliVracaNull()
    {
        Cable[] cables =
        {
            DemoCable(),
            new() { Id = 2, Code = "M100-W2", SpecFileName = "M100-W2" }
        };

        Assert.Equal(2, Cable.FindByResultFileName(cables, "M100-W2")!.Id);
        Assert.Null(Cable.FindByResultFileName(cables, "PROBA1"));
    }

    [Fact]
    public void TestRun_NeprepoznatRezultat_OstajeBezKabla()
    {
        // Operater je možda pokrenuo test iz drugog spec fajla — rezultat se čuva i tada.
        var run = new TestRun { SpecFileName = "PROBA1", CableId = null };

        Assert.False(run.IsRecognized);
    }

    [Fact]
    public void SpecFileNameWithExtension_DodajeC61()
    {
        Assert.Equal("M100-W1.c61", DemoCable().SpecFileNameWithExtension);
    }
}
