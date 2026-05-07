IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
        SourceDocumentId uniqueidentifier NOT NULL,
        VendorName nvarchar(250) NULL,
        VendorTaxId nvarchar(100) NULL,
        InvoiceNumber nvarchar(100) NULL,
        IssueDate date NULL,
        SubtotalAmount decimal(18, 2) NULL,
        SubtotalCurrency nvarchar(10) NULL,
        VatAmount decimal(18, 2) NULL,
        VatCurrency nvarchar(10) NULL,
        TotalAmount decimal(18, 2) NULL,
        TotalCurrency nvarchar(10) NULL,
        Status nvarchar(50) NOT NULL,
        MetadataJson nvarchar(max) NOT NULL,
        ValidationReportJson nvarchar(max) NOT NULL,
        CreatedAtUtc datetime2(7) NOT NULL
            CONSTRAINT DF_Invoices_CreatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO
