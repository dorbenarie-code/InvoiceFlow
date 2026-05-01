namespace InvoiceFlow.Domain.Invoices;

public enum InvoiceStatus
{
    Extracted = 1,
    RequiresHumanReview = 2,
    Verified = 3,
    Rejected = 4,
    Exported = 5
}