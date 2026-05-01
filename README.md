# InvoiceFlow

InvoiceFlow is a .NET-based invoice processing engine for turning invoice or receipt documents into validated business objects.

The system receives a financial document, extracts invoice fields, maps them into a strongly typed `Invoice` model, applies deterministic business validation rules, and returns a structured processing result.

The goal is not to replace accounting software.

The goal is to give .NET developers a clean, reusable invoice-processing foundation that can later be packaged as a NuGet SDK and connected to real OCR/AI providers such as Azure Document Intelligence, OpenAI, or other document intelligence services.

---

## Product Vision

InvoiceFlow is built around one core idea:

> AI can extract data, but the domain model decides whether the data is trustworthy.

The system does not blindly trust extracted document data.

It builds a business review package around the invoice:

- original document reference
- extracted invoice data
- deterministic validation report
- final invoice status
- clear issues explaining why human review may be required

The long-term vision is for InvoiceFlow to become a go-to .NET package for developers who want invoice-processing infrastructure without rebuilding the same pipeline, validation rules, error handling, and integration structure from scratch.

---

## Current MVP Status

InvoiceFlow currently includes:

- clean Domain model
- deterministic invoice validation
- Application use case for processing invoice documents
- public processor contract: `IInvoiceDocumentProcessor`
- in-memory infrastructure implementations
- DI registration
- minimal Fluent Composition API
- first API endpoint for document processing
- health endpoint
- Swagger/OpenAPI documentation
- file upload validation
- basic file security checks
- configurable upload options
- integration tests covering the main flow

The current MVP is intentionally focused on a clean, testable processing core and a thin API adapter.

---

## Current Processing Workflow

```text
Document received
→ Original document stored
→ Data extracted
→ Extracted data mapped to Invoice
→ Business validation runs
→ Invoice status is updated
→ Invoice is saved
→ Result is returned
Processing Outcomes

A processed invoice can currently end in one of the main MVP states:

Verified — the extracted invoice passed the current business validation rules.
RequiresHumanReview — the document was processed successfully, but one or more validation errors were found.

Important:

RequiresHumanReview is not treated as a system failure.

It is a valid business outcome.

For example, if the subtotal plus VAT does not match the total amount, the API still returns:

200 OK

But the response body contains:

{
  "status": "RequiresHumanReview"
}

This means the client must inspect the status field in the response body and not rely only on the HTTP status code for business validation results.

Architecture

InvoiceFlow follows a layered architecture:

Domain
Application
Infrastructure
API
Tests
Domain Layer

The Domain layer contains the business model and deterministic business rules.

Current Domain components:

CurrencyAmount
Vendor
Invoice
InvoiceStatus
InvoiceValidationSeverity
InvoiceValidationIssue
InvoiceValidationReport
IInvoiceValidator
DefaultInvoiceValidator

The Domain layer does not depend on:

HTTP
files
databases
cloud providers
OCR providers
AI providers
infrastructure implementations
Application Layer

The Application layer contains the use case and the contracts required to run it.

Current Application components:

DocumentInput
StoredDocument
ExtractedDocument
IDocumentStorage
IDocumentExtractor
IInvoiceMapper
IInvoiceRepository
IInvoiceDocumentProcessor
ProcessInvoiceDocumentService
ProcessInvoiceDocumentResult

The main use case is exposed through:

IInvoiceDocumentProcessor

Implemented by:

ProcessInvoiceDocumentService

The service orchestrates the pipeline:

Store original document
Extract document data
Map extracted data to Invoice
Validate invoice
Apply validation report
Save invoice
Return processing result

The Application layer depends on abstractions, not infrastructure implementations.

Infrastructure Layer

The Infrastructure layer currently contains simple MVP implementations:

InMemoryDocumentStorage
FakeDocumentExtractor
FieldBasedInvoiceMapper
InMemoryInvoiceRepository

These implementations allow the full pipeline to run end-to-end without:

SQL
Azure
OpenAI
real OCR
blob storage

The in-memory implementations are suitable for MVP, tests, demos, and local development.

They are registered as singletons by the current composition setup and include basic thread-safety protection.

Fluent Composition API

InvoiceFlow currently exposes a small Fluent Composition API for dependency injection setup.

Example:

builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure();

For deterministic tests or demos, a fixed validation date can be provided:

services
    .AddInvoiceFlow(new DateOnly(2026, 4, 30))
    .UseInMemoryInfrastructure();

This is intentionally a Fluent Composition API, not yet a full Fluent Pipeline API.

In other words, the current fluent layer answers this question:

How should InvoiceFlow be registered and composed?

It does not yet try to model the full business workflow as a fluent DSL.

That is intentional.

The current API stays small, predictable, and aligned with YAGNI while still moving the project toward the final SDK vision.

Current composition methods:

AddInvoiceFlow()
AddInvoiceFlow(DateOnly validationDate)
UseInMemoryInfrastructure()

Lower-level registration methods also exist:

AddInvoiceFlowCore()
AddInvoiceFlowCore(DateOnly validationDate)
AddInvoiceFlowInMemory()

Future direction may look like:

builder.Services
    .AddInvoiceFlow()
    .UseAzureDocumentIntelligence()
    .UseSqlServerStorage();

These provider/storage integrations are not implemented yet.

API Layer

InvoiceFlow currently includes a first thin API adapter.

Current endpoints:

GET /health
POST /api/invoices/process

The invoice processing endpoint receives a file using multipart/form-data, creates a DocumentInput, calls IInvoiceDocumentProcessor, and returns a structured response.

The API layer does not contain business validation logic.

It only handles HTTP and input-boundary concerns:

multipart request validation
file presence validation
file size validation
supported content type validation
basic file signature validation
file name sanitization
response shaping
Swagger/OpenAPI documentation
Supported Upload Types

Current supported MVP file types:

PDF
JPG / JPEG
PNG

Supported content types:

application/pdf
image/jpeg
image/png

The API also supports content types with parameters, for example:

application/pdf; charset=utf-8
Upload Validation and Security Checks

The API currently protects the upload boundary with:

rejection of non-multipart requests
rejection of missing files
rejection of empty files
rejection of unsupported content types
configurable file size limit
basic magic-number / file-signature validation
file name sanitization to remove path segments

Examples:

../../invoice.pdf        → invoice.pdf
C:\fakepath\invoice.pdf  → invoice.pdf

The current magic-number validation checks the beginning bytes of the file before reading the full file into memory.

This avoids unnecessary memory allocation for files that clearly do not match their declared type.

Note:

The current API still stores uploaded content in memory as a byte array before passing it into the processing pipeline.

This is acceptable for small MVP invoice files.

For high-throughput production scenarios or large files, the design should move toward stream-based processing and direct storage integration, such as Azure Blob Storage or another object storage provider.

Upload Configuration

Upload settings are configurable through configuration.

Current section:

{
  "InvoiceFlow": {
    "Upload": {
      "MaxFileSizeInBytes": 10485760
    }
  }
}

Default:

10 MB

The API validates upload options on startup.

Invalid configuration, such as a maximum file size of 0, causes the application to fail fast instead of starting with broken settings.

Swagger / OpenAPI

Swagger is available in Development mode.

Run the API:

dotnet run --project src/InvoiceFlow.Api/InvoiceFlow.Api.csproj

Open:

http://localhost:5030

The root path redirects to Swagger in Development mode.

Swagger currently documents:

GET /health
POST /api/invoices/process
multipart file upload
200 OK processing response
400 Bad Request API errors
413 Payload Too Large API errors

The OpenAPI metadata is covered by tests to prevent accidental regressions in developer experience.

Example API Usage

Create a small PDF-like test file:

printf '%s\n' '%PDF-1.7 fake invoice content' > /tmp/invoice.pdf

Send it to the API:

curl -i -X POST http://localhost:5030/api/invoices/process \
  -F "file=@/tmp/invoice.pdf;type=application/pdf"

Expected result:

HTTP/1.1 200 OK

With a response body containing either:

{
  "status": "Verified"
}

or:

{
  "status": "RequiresHumanReview"
}

depending on the extracted invoice data and validation result.

Example Successful Response

A valid invoice may return:

{
  "documentId": "c2b8d7e5-2e89-4f90-b6cb-123456789abc",
  "invoiceId": "82d2c0b7-7ef2-4b9a-b26d-abcdef123456",
  "status": "Verified",
  "invoice": {
    "vendorName": "Cohen Office Supplies Ltd",
    "vendorTaxId": "516789123",
    "invoiceNumber": "INV-1001",
    "issueDate": "2026-04-30",
    "subtotalAmount": {
      "amount": 1000.00,
      "currency": "ILS"
    },
    "vatAmount": {
      "amount": 180.00,
      "currency": "ILS"
    },
    "totalAmount": {
      "amount": 1180.00,
      "currency": "ILS"
    },
    "status": "Verified"
  },
  "validationReport": {
    "hasIssues": false,
    "hasErrors": false,
    "hasWarnings": false,
    "requiresHumanReview": false,
    "issues": []
  }
}
Example Human Review Response

If business validation finds an error, the API still returns 200 OK, because the document was processed successfully.

Example:

{
  "status": "RequiresHumanReview",
  "validationReport": {
    "hasIssues": true,
    "hasErrors": true,
    "hasWarnings": false,
    "requiresHumanReview": true,
    "issues": [
      {
        "code": "TOTAL_MISMATCH",
        "fieldName": "TotalAmount",
        "message": "Subtotal amount plus VAT amount must match total amount.",
        "severity": "Error"
      }
    ]
  }
}
API Error Response Format

HTTP-level errors use a consistent error response format:

{
  "code": "FILE_REQUIRED",
  "message": "Invoice document file is required."
}

Current API error examples:

Code	Meaning
INVALID_CONTENT_TYPE	Request is not multipart/form-data
INVALID_FORM_DATA	Multipart form data is malformed
FILE_REQUIRED	File is missing or empty
FILE_TOO_LARGE	File exceeds the configured upload limit
UNSUPPORTED_FILE_CONTENT_TYPE	File content type is not supported
INVALID_FILE_SIGNATURE	File bytes do not match the declared file type
INVALID_FILE_NAME	File name is missing or invalid
INVALID_DOCUMENT	Uploaded document failed input model validation
Current Validation Rules

The default invoice validator currently detects:

missing vendor
missing invoice number
missing issue date
future issue date
missing subtotal amount
missing VAT amount
missing total amount
mixed currencies
subtotal + VAT mismatch

Validation errors cause the invoice to be marked as:

RequiresHumanReview

A valid invoice is marked as:

Verified

Warnings do not currently force human review.

Tests

The project includes tests for:

Domain models
Value objects
validation issues
validation reports
default invoice validation rules
Application use case orchestration
in-memory infrastructure
thread-safety behavior for in-memory storage/repository
Fluent DI composition
API integration behavior
health endpoint
Swagger/OpenAPI metadata
upload configuration binding
startup options validation
API input validation
basic file security checks
file name sanitization

Run all tests:

dotnet test

Run build and tests:

dotnet build
dotnet test
Current Project Structure
src/
  InvoiceFlow.Api/
    Health/
    Invoices/
  InvoiceFlow.Application/
    Documents/
    Invoices/
  InvoiceFlow.Domain/
    Invoices/
    ValueObjects/
  InvoiceFlow.Infrastructure/
    DependencyInjection/
    Documents/
    Invoices/

tests/
  InvoiceFlow.Tests/
    Api/
    Application/
    Composition/
    Domain/
    Infrastructure/
    Integration/
Implemented So Far

The current MVP includes:

invoice domain model
currency amount value object
vendor model
invoice statuses
validation issue model
validation report model
default invoice validator
document input model
document storage contract
document extractor contract
invoice mapper contract
invoice repository contract
public invoice document processor contract
application use case for processing invoice documents
in-memory document storage
fake document extractor
field-based invoice mapper
in-memory invoice repository
thread-safe in-memory implementations
DI registration
minimal Fluent Composition API
first API endpoint for processing invoice files
health endpoint
API response DTOs
upload options
API input validation
basic file signature checks
file name sanitization
Swagger/OpenAPI documentation
integration tests for the full flow
Not Implemented Yet

The following features are intentionally not implemented yet:

real OCR provider
real AI / LLM provider
Azure Document Intelligence integration
OpenAI Vision integration
SQL persistence
blob storage
authentication
authorization
dashboard / review UI
invoice export
accounting software integration
duplicate invoice detection
Israeli tax id validation
retry policy
queue / background jobs
dynamic mapping into custom developer-defined objects
full streaming-based file processing
full Fluent Pipeline API

These are future steps.

The current focus is a clean, testable processing core with a thin API adapter and developer-friendly composition setup.

Development Principles

InvoiceFlow is developed with the following principles:

SOLID
Clean Architecture
KISS
YAGNI
deterministic business validation
thin API layer
replaceable infrastructure
SDK-first composition
test-first progression
explicit business outcomes
safe handling of external input
developer experience as part of the product

The system is intentionally built in small steps.

Each new behavior is tested before moving to the next layer.
