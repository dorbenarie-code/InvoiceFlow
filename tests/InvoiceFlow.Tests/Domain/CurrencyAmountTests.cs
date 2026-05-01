using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Domain;

public sealed class CurrencyAmountTests
{
    [Fact]
    public void Constructor_ShouldNormalizeCurrency()
    {
        var amount = new CurrencyAmount(100, " ils ");

        Assert.Equal(100, amount.Amount);
        Assert.Equal("ILS", amount.Currency);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAmountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CurrencyAmount(-1, "ILS"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, ""));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsNotThreeLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "SHEKEL"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyContainsNonLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "₪"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyContainsNonAsciiLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "שקל"));
    }

    [Fact]
    public void Add_ShouldReturnCombinedAmount_WhenCurrencyIsSame()
    {
        var subtotal = new CurrencyAmount(1000, "ILS");
        var vat = new CurrencyAmount(180, "ILS");

        var result = subtotal.Add(vat);

        Assert.Equal(1180, result.Amount);
        Assert.Equal("ILS", result.Currency);
    }

    [Fact]
    public void Add_ShouldThrow_WhenCurrenciesAreDifferent()
    {
        var ils = new CurrencyAmount(100, "ILS");
        var usd = new CurrencyAmount(50, "USD");

        Assert.Throws<InvalidOperationException>(() =>
            ils.Add(usd));
    }

    [Fact]
    public void OperatorPlus_ShouldReturnCombinedAmount_WhenCurrencyIsSame()
    {
        var subtotal = new CurrencyAmount(1000, "ILS");
        var vat = new CurrencyAmount(180, "ILS");

        var result = subtotal + vat;

        Assert.Equal(1180, result.Amount);
        Assert.Equal("ILS", result.Currency);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnTrue_WhenDifferenceIsWithinTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.009m, "ILS");

        var result = expected.EqualsWithTolerance(actual);

        Assert.True(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnFalse_WhenDifferenceIsOutsideTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.02m, "ILS");

        var result = expected.EqualsWithTolerance(actual);

        Assert.False(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldThrow_WhenToleranceIsNegative()
    {
        var first = new CurrencyAmount(100, "ILS");
        var second = new CurrencyAmount(100, "ILS");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            first.EqualsWithTolerance(second, -0.01m));
    }
}
