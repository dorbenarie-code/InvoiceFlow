namespace InvoiceFlow.Domain.Invoices;

public sealed class DefaultInvoiceValidator : IInvoiceValidator
{
    private const string MissingVendorCode = "MISSING_VENDOR";
    private const string MissingInvoiceNumberCode = "MISSING_INVOICE_NUMBER";
    private const string MissingIssueDateCode = "MISSING_ISSUE_DATE";
    private const string FutureIssueDateCode = "FUTURE_ISSUE_DATE";
    private const string MissingSubtotalAmountCode = "MISSING_SUBTOTAL_AMOUNT";
    private const string MissingVatAmountCode = "MISSING_VAT_AMOUNT";
    private const string MissingTotalAmountCode = "MISSING_TOTAL_AMOUNT";
    private const string CurrencyMismatchCode = "CURRENCY_MISMATCH";
    private const string TotalMismatchCode = "TOTAL_MISMATCH";

    private const string RequiredMessageFormat = "{0} is required.";

    private readonly DateOnly _validationDate;

    public DefaultInvoiceValidator(DateOnly validationDate)
    {
        _validationDate = validationDate;
    }

    public InvoiceValidationReport Validate(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var issues = new List<InvoiceValidationIssue>();

        AddMissingFieldIssues(invoice, issues);
        AddFutureDateIssue(invoice, issues);
        AddCurrencyMismatchIssue(invoice, issues);
        AddTotalMismatchIssue(invoice, issues);

        return issues.Count == 0
            ? InvoiceValidationReport.Valid()
            : InvoiceValidationReport.FromIssues(issues);
    }

    private static void AddMissingFieldIssues(
        Invoice invoice,
        List<InvoiceValidationIssue> issues)
    {
        if (invoice.Vendor is null)
        {
            issues.Add(CreateRequiredFieldIssue(
                MissingVendorCode,
                nameof(invoice.Vendor),
                "Vendor"));
        }

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            issues.Add(CreateRequiredFieldIssue(
                MissingInvoiceNumberCode,
                nameof(invoice.InvoiceNumber),
                "Invoice number"));
        }

        if (invoice.IssueDate is null)
        {
            issues.Add(CreateRequiredFieldIssue(
                MissingIssueDateCode,
                nameof(invoice.IssueDate),
                "Issue date"));
        }

        if (invoice.SubtotalAmount is null)
        {
            issues.Add(CreateRequiredFieldIssue(
                MissingSubtotalAmountCode,
                nameof(invoice.SubtotalAmount),
                "Subtotal amount"));
        }

        if (invoice.VatAmount is null)
        {
            issues.Add(CreateRequiredFieldIssue(
                MissingVatAmountCode,
                nameof(invoice.VatAmount),
                "VAT amount"));
        }

        if (invoice.TotalAmount is null)
        {
            issues.Add(CreateRequiredFieldIssue(
                MissingTotalAmountCode,
                nameof(invoice.TotalAmount),
                "Total amount"));
        }
    }

    private void AddFutureDateIssue(
        Invoice invoice,
        List<InvoiceValidationIssue> issues)
    {
        if (invoice.IssueDate is not null && invoice.IssueDate > _validationDate)
        {
            issues.Add(InvoiceValidationIssue.Error(
                FutureIssueDateCode,
                nameof(invoice.IssueDate),
                "Issue date cannot be in the future."));
        }
    }

    private static void AddCurrencyMismatchIssue(
        Invoice invoice,
        List<InvoiceValidationIssue> issues)
    {
        if (!HasCurrencyMismatch(invoice))
        {
            return;
        }

        issues.Add(InvoiceValidationIssue.Error(
            CurrencyMismatchCode,
            "Currency",
            "Invoice amounts must use the same currency."));
    }

    private static void AddTotalMismatchIssue(
        Invoice invoice,
        List<InvoiceValidationIssue> issues)
    {
        if (HasCurrencyMismatch(invoice))
        {
            return;
        }

        if (invoice is not
            {
                SubtotalAmount: { } subtotal,
                VatAmount: { } vat,
                TotalAmount: { } total
            })
        {
            return;
        }

        var calculatedTotal = subtotal + vat;

        if (calculatedTotal.EqualsWithTolerance(total))
        {
            return;
        }

        issues.Add(InvoiceValidationIssue.Error(
            TotalMismatchCode,
            nameof(invoice.TotalAmount),
            "Subtotal amount plus VAT amount must match total amount."));
    }

    private static bool HasCurrencyMismatch(Invoice invoice)
    {
        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (invoice.SubtotalAmount is not null)
        {
            currencies.Add(invoice.SubtotalAmount.Currency);
        }

        if (invoice.VatAmount is not null)
        {
            currencies.Add(invoice.VatAmount.Currency);
        }

        if (invoice.TotalAmount is not null)
        {
            currencies.Add(invoice.TotalAmount.Currency);
        }

        return currencies.Count > 1;
    }

    private static InvoiceValidationIssue CreateRequiredFieldIssue(
        string code,
        string fieldName,
        string displayName)
    {
        return InvoiceValidationIssue.Error(
            code,
            fieldName,
            string.Format(RequiredMessageFormat, displayName));
    }
}