using System;
using InvoiceFlow.Domain.ValueObjects;
using Xunit;

namespace InvoiceFlow.Tests.Domain;

public sealed class CurrencyAmountTests
{
    [Fact]
    public void Constructor_ShouldCreateCurrencyAmount_WhenAmountAndCurrencyAreValid()
    {
        var amount = new CurrencyAmount(100, "ILS");

        Assert.Equal(100, amount.Amount);
        Assert.Equal("ILS", amount.Currency);
    }

    [Fact]
    public void Constructor_ShouldAllowZeroAmount()
    {
        var amount = new CurrencyAmount(0, "ILS");

        Assert.Equal(0, amount.Amount);
        Assert.Equal("ILS", amount.Currency);
    }

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
    public void Constructor_ShouldThrow_WhenCurrencyIsNull()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, null!));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, ""));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "   "));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsShorterThanThreeLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "IL"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsLongerThanThreeLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "SHEKEL"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyContainsDigits()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "IL1"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyContainsSymbols()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyAmount(100, "I₪S"));
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
    public void Add_ShouldThrow_WhenOtherIsNull()
    {
        var amount = new CurrencyAmount(100, "ILS");

        Assert.Throws<ArgumentNullException>(() =>
            amount.Add(null!));
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
    public void OperatorPlus_ShouldThrow_WhenLeftOperandIsNull()
    {
        CurrencyAmount? left = null;
        var right = new CurrencyAmount(100, "ILS");

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = left! + right;
        });
    }

    [Fact]
    public void OperatorPlus_ShouldThrow_WhenRightOperandIsNull()
    {
        var left = new CurrencyAmount(100, "ILS");
        CurrencyAmount? right = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = left + right!;
        });
    }

    [Fact]
    public void OperatorPlus_ShouldThrow_WhenCurrenciesAreDifferent()
    {
        var ils = new CurrencyAmount(100, "ILS");
        var usd = new CurrencyAmount(50, "USD");

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ils + usd;
        });
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnTrue_WhenAmountsAreEqual()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.00m, "ILS");

        var result = expected.EqualsWithTolerance(actual);

        Assert.True(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnTrue_WhenDifferenceIsWithinDefaultTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.009m, "ILS");

        var result = expected.EqualsWithTolerance(actual);

        Assert.True(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnTrue_WhenDifferenceEqualsDefaultTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.01m, "ILS");

        var result = expected.EqualsWithTolerance(actual);

        Assert.True(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnFalse_WhenDifferenceIsJustAboveDefaultTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.011m, "ILS");

        var result = expected.EqualsWithTolerance(actual);

        Assert.False(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnTrue_WhenDifferenceEqualsCustomTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.05m, "ILS");

        var result = expected.EqualsWithTolerance(actual, 0.05m);

        Assert.True(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnFalse_WhenDifferenceIsJustAboveCustomTolerance()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.051m, "ILS");

        var result = expected.EqualsWithTolerance(actual, 0.05m);

        Assert.False(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnTrue_WhenToleranceIsZeroAndAmountsAreEqual()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.00m, "ILS");

        var result = expected.EqualsWithTolerance(actual, 0);

        Assert.True(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldReturnFalse_WhenToleranceIsZeroAndAmountsAreDifferent()
    {
        var expected = new CurrencyAmount(118.00m, "ILS");
        var actual = new CurrencyAmount(118.001m, "ILS");

        var result = expected.EqualsWithTolerance(actual, 0);

        Assert.False(result);
    }

    [Fact]
    public void EqualsWithTolerance_ShouldThrow_WhenOtherIsNull()
    {
        var amount = new CurrencyAmount(100, "ILS");

        Assert.Throws<ArgumentNullException>(() =>
            amount.EqualsWithTolerance(null!));
    }

    [Fact]
    public void EqualsWithTolerance_ShouldThrow_WhenToleranceIsNegative()
    {
        var expected = new CurrencyAmount(100, "ILS");
        var actual = new CurrencyAmount(100, "ILS");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            expected.EqualsWithTolerance(actual, -0.01m));
    }

    [Fact]
    public void EqualsWithTolerance_ShouldThrow_WhenCurrenciesAreDifferent()
    {
        var ils = new CurrencyAmount(100, "ILS");
        var usd = new CurrencyAmount(100, "USD");

        Assert.Throws<InvalidOperationException>(() =>
            ils.EqualsWithTolerance(usd));
    }
}