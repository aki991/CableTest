using CableTest.Core.Spec;
using Xunit;

namespace CableTest.Tests;

public class SpecFileNameValidatorTests
{
    [Theory]
    [InlineData("M100-W1")]   // demo kabl
    [InlineData("CRD-A")]     // primeri iz priručnika
    [InlineData("BURN-1")]
    [InlineData("1REVEN-D")]
    [InlineData("09071126")]  // ime kakvo generiše Learn
    [InlineData("A")]
    [InlineData("12345678")]  // tačno na granici
    [InlineData("-")]
    public void Validate_PrihvataIspravnaImena(string name)
    {
        Assert.Null(SpecFileNameValidator.Validate(name));
        Assert.True(SpecFileNameValidator.IsValid(name));
    }

    [Fact]
    public void Validate_PredugoIme_JavljaDuzinu()
    {
        string? error = SpecFileNameValidator.Validate("123456789");

        Assert.NotNull(error);
        Assert.Contains("9 znakova", error, StringComparison.Ordinal);
        Assert.Contains("8 znakova", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_MaloSlovo_JavljaKojeSlovo()
    {
        string? error = SpecFileNameValidator.Validate("M100-w1");

        Assert.NotNull(error);
        Assert.Contains("malo slovo 'w'", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_Razmak_SeOdbija()
    {
        string? error = SpecFileNameValidator.Validate("M100 W1");

        Assert.NotNull(error);
        Assert.Contains("razmak", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MILOŠ")]
    [InlineData("ČKALJA")]
    [InlineData("ĐAK-1")]
    public void Validate_Dijakritika_SeOdbija(string name)
    {
        string? error = SpecFileNameValidator.Validate(name);

        Assert.NotNull(error);
        Assert.Contains("nije osnovno latinično", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_TackaIEkstenzija_SeOdbijaju()
    {
        string? error = SpecFileNameValidator.Validate("M100.C61");

        Assert.NotNull(error);
        Assert.Contains("tačku", error, StringComparison.Ordinal);
        Assert.Contains(".c61", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_PraznoIme_SeOdbija(string? name)
    {
        string? error = SpecFileNameValidator.Validate(name);

        Assert.NotNull(error);
        Assert.Contains("nije uneto", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("M100_W1")]   // donja crta nije dozvoljena
    [InlineData("M100/W1")]
    [InlineData("M100#1")]
    [InlineData(" M100")]
    [InlineData("M100 ")]
    public void Validate_NedozvoljeniZnakovi_SeOdbijaju(string name)
    {
        Assert.NotNull(SpecFileNameValidator.Validate(name));
    }

    [Fact]
    public void EnsureValid_BacaArgumentExceptionSaPorukom()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => SpecFileNameValidator.EnsureValid("M100 W1"));

        Assert.Contains("razmak", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MaxLength_JeOsamZnakova()
    {
        // Pretpostavka koju treba potvrditi kod proizvođača; test je tu da promena bude svesna.
        Assert.Equal(8, SpecFileNameValidator.MaxLength);
    }
}
