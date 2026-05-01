namespace InvoiceFlow.Domain.ValueObjects;

public sealed record CurrencyAmount
{
    public decimal Amount { get; }
    public string Currency { get; }

    public CurrencyAmount(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3)
        {
            throw new ArgumentException(
                "Currency must be a 3-letter ISO code.",
                nameof(currency));
        }

        if (!normalizedCurrency.All(char.IsAsciiLetter))
        {
            throw new ArgumentException(
                "Currency must contain ASCII letters only.",
                nameof(currency));
        }

        Amount = amount;
        Currency = normalizedCurrency;
    }

    public CurrencyAmount Add(CurrencyAmount other)
    {
        EnsureSameCurrency(other);

        return new CurrencyAmount(Amount + other.Amount, Currency);
    }

    public bool EqualsWithTolerance(CurrencyAmount other, decimal tolerance = 0.01m)
    {
        if (tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                tolerance,
                "Tolerance cannot be negative.");
        }

        EnsureSameCurrency(other);

        return Math.Abs(Amount - other.Amount) <= tolerance;
    }

    public static CurrencyAmount operator +(CurrencyAmount left, CurrencyAmount right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return left.Add(right);
    }

    private void EnsureSameCurrency(CurrencyAmount other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot operate on amounts with different currencies. Left: {Currency}, Right: {other.Currency}.");
        }
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }
}
