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

Instead, it builds a business review package around the invoice:

- original document reference
- extracted invoice data
- deterministic validation report
- final invoice status
- clear issues explaining why human review may be required

The long-term vision is for InvoiceFlow to become a go-to .NET package for developers who want invoice-processing infrastructure without rebuilding the same pipeline, validation rules, upload safety, error handling, and integration structure from scratch.

---

## Current MVP Status

InvoiceFlow currently includes:

- clean Domain model
- deterministic invoice validation
- Application use case for processing invoice documents
- public processor contract: `IInvoiceDocumentProcessor`
- document extraction contract: `IDocumentExtractor`
- stream-ready `DocumentInput` contract
- Application stream contract proving storage and extractor can open independent document streams
- API stream-based document handoff from the upload boundary into the Application pipeline
- document storage failure handling through a dedicated Application exception
- API mapping for document storage failures
- API full-pipeline regression coverage for document storage failures
- Azure Blob Storage document storage provider
- Azure Blob Storage document storage Fluent registration
- Azure Blob Storage configuration-based provider selection through Fluent Composition
- API Azure Blob Storage provider selection through configuration
- API Azure Blob Storage configuration tests
- Azure Blob Storage provider options validation
- Azure Blob Storage document upload through Azure Blob SDK
- Azurite-backed Azure Blob Storage integration test
- Azure SDK stream-based document handoff to Azure Document Intelligence
- in-memory infrastructure implementations
- DI registration
- minimal Fluent Composition API
- custom document extractor registration through Fluent Composition
- Azure Document Intelligence provider registration
- Azure provider options validation
- Azure provider `ModelId` option with default `prebuilt-invoice`
- Azure provider minimum confidence threshold option with default `0.8`
- internal Azure analyze request contract
- internal Azure client abstraction
- Azure SDK client adapter
- Azure SDK stream-based document handoff
- Azure raw text extraction support through the SDK adapter
- Azure structured invoice field mapping for the MVP business fields
- Azure missing-field handling without treating missing data as infrastructure failure
- Azure low-confidence field filtering
- Azure currency extraction from money fields
- configuration-based Azure provider selection through Fluent Composition
- API Azure provider selection through configuration
- API Azure configuration tests
- real Azure extractor smoke test guarded by environment variables
- real Azure full-pipeline smoke test guarded by environment variables
- Fluent Azure configuration composition tests
- infrastructure extraction failure handling through a dedicated Application exception
- API mapping for document extraction/provider failures
- API mapping for invoice persistence failures
- API full-pipeline regression coverage for invoice persistence failures
- extraction analyzed page count propagation for usage and cost tracking
- ProcessingRun usage audit model
- ProcessingRun repository contract
- in-memory ProcessingRun repository
- default processing client context for local and MVP execution
- API key based client identity contract
- configured API key validator using SHA-256 key hashes
- API key hash helper
- API key identity options validation
- API key identity Fluent Composition through `UseApiKeyClientIdentity(...)`
- HTTP processing client context for resolving request client id
- API key endpoint filter for `X-API-Key`
- invoice processing endpoint protection when API key identity is configured
- processing-run client id resolution from a valid API key
- ProcessingRun decorator around `IInvoiceDocumentProcessor`
- Fluent Composition registration for ProcessingRun audit infrastructure
- ProcessingRun tests covering successful and failed processing attempts
- API key based client identification through `X-API-Key`
- Application API key validation contract returning a resolved `ClientId`
- configured API key validator using SHA-256 key hashes
- API key identity Fluent Composition through `UseApiKeyClientIdentity(...)`
- invoice processing endpoint protection when API key identity is configured
- API key identity integration tests proving `ProcessingRun.ClientId` is resolved from the API key
- Application per-client rate limiting contract
- in-memory per-client fixed-window rate limiter backed by .NET rate limiting primitives
- client rate limiting Fluent Composition through `UseClientRateLimiting(...)`
- API client rate limiting configuration through `InvoiceFlow:ClientRateLimiting`
- API endpoint filter returning `429 Too Many Requests` when a client exceeds the configured limit
- API integration tests proving API key validation runs before client rate limiting and invoice processing
- OpenAPI documentation for `429 Too Many Requests`
- first API endpoint for document processing
- health endpoint
- Swagger/OpenAPI documentation
- file upload validation
- basic file security checks
- configurable upload options
- API error response contract
- integration tests covering the main flow
- integration test covering Azure mapped fields through the business pipeline
- integration test proving partial or low-confidence Azure data becomes `RequiresHumanReview` instead of an infrastructure failure
- SQL Server invoice repository registration tests
- SQL Server invoice repository `SaveAsync` implementation
- SQL Server single-table invoice persistence to `dbo.Invoices`
- database schema script for `dbo.Invoices`
- optional SQL Server persistence test requiring `INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING`
- API SQL configuration tests
- API endpoint to SQL Server persistence integration test
- manual API run verified against `InvoiceFlowDb`
- API boundary hardening tests
- Application pipeline guard tests for invalid provider outputs
- API tests for document extraction failure behavior
- API tests for invoice persistence failure behavior
- API real-pipeline regression test proving invoice persistence failures travel through upload, extraction, mapping, validation, Application wrapping, and API response mapping

The current MVP is intentionally focused on a clean, testable processing core and a thin API adapter.

Azure provider registration, SDK client wiring, raw text extraction, selected invoice field mapping, and configuration-based Azure provider selection now exist.

The Azure integration is still intentionally limited to the business fields required by the current validator and mapper.

It does not yet try to parse the full Azure invoice schema.

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
→ Processing run audit is recorded around the processing attempt
```

---

## Processing Outcomes

A processed invoice can currently end in one of the main MVP states:

### Verified

The extracted invoice passed the current business validation rules.

### RequiresHumanReview

The document was processed successfully, but one or more validation errors were found.

Important:

`RequiresHumanReview` is not treated as a system failure.

It is a valid business outcome.

For example, if the subtotal plus VAT does not match the total amount, the API still returns:

```text
200 OK
```

But the response body contains:

```json
{
  "status": "RequiresHumanReview"
}
```

This means clients must inspect the `status` field in the response body and not rely only on the HTTP status code for business validation results.

---

## Document Extraction Failure

A document extraction failure is not a business validation result.

If the extraction provider fails, for example because of rate limiting, network failure, provider downtime, or another infrastructure issue, the API returns a stable error response instead of returning `RequiresHumanReview`.

In that case, the API returns:

```text
503 Service Unavailable
```

with an API error code such as:

```text
DOCUMENT_EXTRACTION_FAILED
```

This separation is intentional.

`RequiresHumanReview` means the document was successfully extracted and mapped, but deterministic invoice validation found business issues.

`DOCUMENT_EXTRACTION_FAILED` means the system could not complete extraction, so the invoice was not processed far enough to make a business validation decision.

Missing Azure fields, empty Azure field collections, low-confidence fields, or an Azure result with no analyzed documents are not treated as provider failures by themselves.

In those cases, InvoiceFlow keeps the raw extracted text, maps only the fields it can trust, and lets the mapper and validator decide whether the invoice requires human review.

---

## Invoice Persistence Failure

An invoice persistence failure is also not a business validation result.

If the invoice was extracted, mapped, and validated, but saving it fails because of SQL Server, configuration, connection, or database issues, the API returns a stable infrastructure error.

In that case, the API returns:

```text
503 Service Unavailable
```

with an API error code such as:

```text
INVOICE_PERSISTENCE_FAILED
```

This separation is intentional.

`RequiresHumanReview` means the invoice was processed successfully and business validation found issues.

`INVOICE_PERSISTENCE_FAILED` means the processing pipeline reached the persistence step, but the invoice could not be saved.

---

## Architecture

InvoiceFlow follows a layered architecture:

```text
Domain
Application
Infrastructure
API
Tests
```

---

## Domain Layer

The Domain layer contains the business model and deterministic business rules.

Current Domain components:

- `CurrencyAmount`
- `Vendor`
- `Invoice`
- `InvoiceStatus`
- `InvoiceValidationSeverity`
- `InvoiceValidationIssue`
- `InvoiceValidationReport`
- `IInvoiceValidator`
- `DefaultInvoiceValidator`

The Domain layer does not depend on:

- HTTP
- files
- databases
- cloud providers
- OCR providers
- AI providers
- infrastructure implementations

---

## Application Layer

The Application layer contains the main use case and the contracts required to run it.

Current Application components:

- `DocumentInput`
- `StoredDocument`
- `ExtractedDocument`
- `IDocumentStorage`
- `IDocumentExtractor`
- `IInvoiceMapper`
- `IInvoiceRepository`
- `IInvoiceDocumentProcessor`
- `ProcessInvoiceDocumentService`
- `ProcessInvoiceDocumentResult`
- `ProcessingRun`
- `IProcessingRunRepository`
- `IProcessingClientContext`
- `IClientApiKeyValidator`
- `ClientApiKeyValidationResult`
- `IClientRateLimiter`
- `ClientRateLimitResult`
- `ProcessingRunInvoiceDocumentProcessor`
- `DocumentExtractionFailedException`
- `InvoicePersistenceFailedException`

The main use case is exposed through:

```csharp
IInvoiceDocumentProcessor
```

Implemented by:

```csharp
ProcessInvoiceDocumentService
```

The service orchestrates the pipeline:

```text
Store original document
Extract document data
Map extracted data to Invoice
Validate invoice
Apply validation report
Save invoice
Return processing result
```

The Application layer depends on abstractions, not infrastructure implementations.

### Document Input Stream Contract

`DocumentInput` is now stream-ready.

It exposes document metadata such as file name, content type, and optional content length, and provides an `OpenReadStreamAsync(...)` operation for consumers that need to read the document content.

It does not expose a public byte-buffer property as part of the processing contract.

This is intentional for SDK stability.

Consumers such as document storage and document extractors can open independent readable streams from the same `DocumentInput` instead of sharing a single already-read stream or forcing callers to hold the whole document as a public byte array.

A byte-array constructor still exists as a convenience path for tests, demos, and small local inputs, but the public processing model is now based on opening streams.

This prepares the system for future object-storage implementations such as Azure Blob Storage without changing the Application use case later.

Full low-level multipart streaming and direct blob storage integration are still separate future slices.

### Provider Output Safety

InvoiceFlow treats external extraction, storage, and mapping components as infrastructure boundaries.

If document storage throws while saving the original document, the processing service wraps the failure in a dedicated `DocumentStorageFailedException`.

This gives the API a clear Application-level signal that the original document could not be stored before extraction started.

Storage failures are not treated as `RequiresHumanReview`.

`RequiresHumanReview` is reserved for valid business processing outcomes where the document was stored, extracted, and mapped, but deterministic validation found business issues.

If a document extractor returns no extracted document, the processing service fails fast with a clear infrastructure error instead of continuing with an invalid pipeline state.

If a document extractor throws during extraction, the processing service wraps the failure in a dedicated `DocumentExtractionFailedException`.

This gives the API a clear Application-level signal that extraction failed before business validation could run.

If an invoice mapper returns no invoice, the processing service also fails fast before validation or persistence.

These failures are not treated as `RequiresHumanReview`.

`RequiresHumanReview` is reserved for valid business processing outcomes where the invoice was extracted and mapped, but deterministic validation found business issues.

If invoice persistence fails after extraction, mapping, and validation, the processing service wraps the failure in a dedicated `InvoicePersistenceFailedException`.

This gives the API a clear Application-level signal that persistence failed after the business pipeline completed successfully.

Persistence failures are not treated as `RequiresHumanReview`.

---

## Infrastructure Layer

The Infrastructure layer currently contains MVP infrastructure implementations and provider integrations:

- `InMemoryDocumentStorage`
- `AzureBlobDocumentStorage`
- `FakeDocumentExtractor`
- `FieldBasedInvoiceMapper`
- `InMemoryInvoiceRepository`
- `AzureDocumentIntelligenceDocumentExtractor`
- `AzureDocumentIntelligenceSdkClient`
- `ConfiguredClientApiKeyValidator`
- `ClientApiKeyHash`
- `InMemoryClientRateLimiter`
- `ClientRateLimitOptions`
- `SqlServerInvoiceRepository`

The in-memory implementations allow the full pipeline to run end-to-end without:

- SQL
- OpenAI
- real OCR
- real Azure network calls

The in-memory implementations are suitable for MVP, tests, demos, and local development.

They are registered as singletons by the current composition setup and include basic thread-safety protection.

The in-memory processing-run repository records processing attempts for local development, tests, and demos.

It is not intended to replace a future SQL-backed usage ledger.

The in-memory client rate limiter provides per-client request limiting for the API MVP.

It uses built-in .NET rate limiting primitives instead of implementing manual counters, locks, or time-window logic.

The current limiter is process-local.

It is suitable for local development, tests, demos, and a single-instance MVP host.

It is not intended to replace a future distributed limiter if the API is scaled across multiple instances.

The Azure Document Intelligence provider now has:

- Fluent registration
- options validation
- `ModelId` support
- configurable minimum confidence threshold
- configuration-based provider selection
- internal analyze request object
- internal client abstraction
- SDK adapter around Azure Document Intelligence
- raw text extraction support
- selected structured invoice field mapping
- missing-field handling
- low-confidence field filtering
- currency extraction from Azure currency fields

The Azure provider currently maps only the selected invoice fields required for the MVP business pipeline.

That is intentional.

It keeps the provider useful without expanding into full invoice schema parsing too early.

The Azure Blob Storage document provider currently supports saving original invoice documents to Blob Storage through the Azure Storage Blobs SDK.

It is registered through Fluent Composition and implements the existing `IDocumentStorage` contract.

The provider opens the document stream from `DocumentInput`, uploads the content to Blob Storage, preserves the content type, and returns a `StoredDocument` with a generated document id and the original file name.

The current Blob Storage slice has been verified against Azurite using an optional integration test guarded by `INVOICEFLOW_AZURITE_BLOB_CONNECTION_STRING`.

The SQL Server invoice repository currently has a narrow persistence scope.

It saves processed invoices into a single SQL table and keeps more complex values such as metadata and validation reports as JSON.

That is intentional for the MVP.

---

## Fluent Composition API

InvoiceFlow currently exposes a small Fluent Composition API for dependency injection setup.

Basic local setup:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure();
```

For deterministic tests or demos, a fixed validation date can be provided:

```csharp
services
    .AddInvoiceFlow(new DateOnly(2026, 4, 30))
    .UseInMemoryInfrastructure();
```

This is intentionally a Fluent Composition API, not yet a full Fluent Pipeline API.

In other words, the current fluent layer answers this question:

> How should InvoiceFlow be registered and composed?

It does not yet try to model the full business workflow as a fluent DSL.

That is intentional.

The current API stays small, predictable, and aligned with YAGNI while still moving the project toward the final SDK vision.

Current composition methods:

- `AddInvoiceFlow()`
- `AddInvoiceFlow(DateOnly validationDate)`
- `UseInMemoryInfrastructure()`
- `UseApiKeyClientIdentity(...)`
- `UseClientRateLimiting(...)`
- `UseAzureBlobDocumentStorage(...)`
- `UseAzureBlobDocumentStorageIfConfigured()`
- `UseDocumentExtractor<TDocumentExtractor>()`
- `UseAzureDocumentIntelligence(...)`
- `UseAzureDocumentIntelligenceIfConfigured()`
- `UseSqlServerInvoiceRepository(...)`

Lower-level registration methods also exist:

- `AddInvoiceFlowCore()`
- `AddInvoiceFlowCore(DateOnly validationDate)`
- `AddInvoiceFlowInMemory()`

---

## Custom Document Extractor Registration

InvoiceFlow supports replacing the default document extractor through the Fluent Composition API.

This is the first controlled step toward provider extensibility.

The current in-memory infrastructure uses `FakeDocumentExtractor` for local development and tests.

A developer can replace it with a custom implementation of `IDocumentExtractor`:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseDocumentExtractor<MyCustomDocumentExtractor>();
```

The recommended order is:

```text
AddInvoiceFlow()
→ UseInMemoryInfrastructure()
→ UseDocumentExtractor<TDocumentExtractor>()
```

This keeps the basic MVP infrastructure in place while explicitly overriding only the document extraction provider.

A custom extractor must implement:

```csharp
using InvoiceFlow.Application.Documents;

public sealed class MyCustomDocumentExtractor : IDocumentExtractor
{
    public Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        // Call OCR, AI, or another document intelligence service here.
        // Return extracted raw text and fields.
        throw new NotImplementedException();
    }
}
```

`IDocumentExtractor` is intentionally small:

```csharp
public interface IDocumentExtractor
{
    Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default);
}
```

The extractor is responsible only for extracting document data.

It should not:

- validate invoice business rules
- decide whether an invoice is verified
- save invoices
- know about HTTP
- know about SQL
- mutate the Domain model directly

Business trust is decided later by the Domain validation layer.

---

## Azure Blob Document Storage Provider

InvoiceFlow now includes a first Azure Blob Storage document storage provider.

It is registered explicitly through Fluent Composition:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureBlobDocumentStorage(options =>
    {
        options.ConnectionString = "UseDevelopmentStorage=true";
        options.ContainerName = "invoice-documents";
    });
```

This replaces the active `IDocumentStorage` implementation with `AzureBlobDocumentStorage` while keeping the Application use case unchanged.

For API-hosted applications, Azure Blob Storage can also be enabled through configuration without hardcoding the storage provider decision in the API layer:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureBlobDocumentStorageIfConfigured();
```

This keeps local development simple:

```text
No InvoiceFlow:AzureBlobStorage configuration
→ InMemoryDocumentStorage remains active
```

When Azure Blob Storage configuration exists, the same composition switches the active document storage provider:

```text
InvoiceFlow:AzureBlobStorage:ConnectionString + InvoiceFlow:AzureBlobStorage:ContainerName configured
→ AzureBlobDocumentStorage becomes active
```

Azure Blob Storage provider options:

- `ConnectionString`
- `ContainerName`

Both options are required.

The provider flow is:

```text
AzureBlobDocumentStorage
→ validates DocumentInput
→ opens the document stream through OpenReadStreamAsync(...)
→ creates the configured Blob container if needed
→ uploads the document content to Blob Storage
→ preserves the document content type
→ returns StoredDocument
```

The Blob provider is covered by Fluent Composition tests, options validation tests, and an optional Azurite-backed integration test.

The Azurite integration test runs only when this environment variable is configured:

```text
INVOICEFLOW_AZURITE_BLOB_CONNECTION_STRING
```

Example local setup:

```bash
export INVOICEFLOW_AZURITE_BLOB_CONNECTION_STRING="UseDevelopmentStorage=true"

dotnet test --filter "AzureBlobDocumentStorageIntegrationTests"
```

For Azurite versions that do not support the latest Azure Storage SDK API version, run Azurite with:

```text
--skipApiVersionCheck
```

This slice proves real Blob-compatible document storage without spending Azure cloud credit.

Configuration-based Blob provider activation now exists for host/API scenarios.

Real Azure Blob smoke tests are still intentionally not implemented.

They should remain a separate, explicit slice if the product needs real cloud-storage smoke coverage beyond Azurite.

---

## Azure Document Intelligence Provider

InvoiceFlow currently includes Azure Document Intelligence provider registration, SDK client wiring, raw text extraction, selected structured field mapping, and configuration-based provider selection.

Explicit registration example:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureDocumentIntelligence(options =>
    {
        options.Endpoint = "https://your-resource.cognitiveservices.azure.com/";
        options.ApiKey = "your-api-key";
    });
```

This registers `AzureDocumentIntelligenceDocumentExtractor` as the active `IDocumentExtractor`.

It also registers the Azure SDK client adapter behind an internal infrastructure abstraction.

For API-hosted applications, Azure can also be enabled through configuration without hardcoding the provider decision in the API layer:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureDocumentIntelligenceIfConfigured();
```

This keeps local development simple:

```text
No InvoiceFlow:AzureDocumentIntelligence configuration
→ FakeDocumentExtractor remains active
```

When Azure configuration exists, the same composition switches the active document extractor:

```text
InvoiceFlow:AzureDocumentIntelligence:Endpoint + InvoiceFlow:AzureDocumentIntelligence:ApiKey configured
→ AzureDocumentIntelligenceDocumentExtractor becomes active
```

Example configuration shape:

```json
{
  "InvoiceFlow": {
    "AzureDocumentIntelligence": {
      "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "ApiKey": "use-user-secrets-or-env-vars",
      "ModelId": "prebuilt-invoice",
      "MinimumConfidenceThreshold": 0.8
    }
  }
}
```

`ModelId` and `MinimumConfidenceThreshold` are optional.

If they are not configured, InvoiceFlow uses the provider defaults.

This configuration-based path is intended for API projects, staging environments, and production hosts where provider selection should come from configuration rather than source-code changes.

It does not perform any Azure call during service registration.

Real Azure smoke coverage is guarded by environment variables. One smoke test verifies the Azure extractor boundary directly. A second smoke test runs the full processing pipeline with Azure extraction, field mapping, deterministic validation, and in-memory invoice persistence.

In addition, InvoiceFlow has completed a controlled 50-document live full-pipeline smoke test through the API using real Azure Document Intelligence, real Azure Blob Storage, SQL Server persistence, deterministic business validation, and stable API responses.

Azure provider options:

- `Endpoint`
- `ApiKey`
- `ModelId`
- `MinimumConfidenceThreshold`

`Endpoint` and `ApiKey` are required when Azure is enabled.

`ModelId` defaults to:

```text
prebuilt-invoice
```

`MinimumConfidenceThreshold` defaults to:

```text
0.8
```

The threshold controls which Azure fields are trusted enough to enter `ExtractedDocument.Fields`.

A field below the configured threshold is ignored.

It is not treated as an infrastructure failure.

A custom model id and confidence threshold can be configured explicitly:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureDocumentIntelligence(options =>
    {
        options.Endpoint = "https://your-resource.cognitiveservices.azure.com/";
        options.ApiKey = "your-api-key";
        options.ModelId = "custom-invoice-model";
        options.MinimumConfidenceThreshold = 0.85f;
    });
```

The current Azure provider flow is:

```text
AzureDocumentIntelligenceDocumentExtractor
→ validates DocumentInput
→ checks cancellation
→ builds AzureDocumentIntelligenceAnalyzeRequest
→ passes ModelId, DocumentInput, and MinimumConfidenceThreshold to the internal Azure client
→ AzureDocumentIntelligenceSdkClient calls Azure Document Intelligence SDK
→ returns ExtractedDocument with RawText, selected Fields, and analyzed page count when page metadata is available
```

The current Azure SDK adapter maps selected Azure invoice fields into the internal `ExtractedDocument.Fields` dictionary:

| Azure field | Internal field |
|---|---|
| `VendorName` | `VendorName` |
| `VendorTaxId` | `VendorTaxId` |
| `InvoiceId` | `InvoiceNumber` |
| `InvoiceNumber` | `InvoiceNumber` |
| `InvoiceDate` | `IssueDate` |
| `SubTotal` | `SubtotalAmount` |
| `TotalTax` | `VatAmount` |
| `InvoiceTotal` | `TotalAmount` |

Currency is extracted from Azure currency fields.

The current priority order is:

```text
InvoiceTotal
→ SubTotal
→ TotalTax
```

If a currency code is available, it is preferred.

If only a currency symbol is available, the symbol is used.

The `FieldBasedInvoiceMapper` later normalizes known currency values such as `₪`, `NIS`, `ILS`, `$`, `USD`, `€`, and `EUR`.

Important:

The Azure provider still does not parse the full Azure invoice schema.

It currently maps only the fields needed by the current MVP business validation flow.

The following Azure scenarios are handled safely:

- missing Azure fields
- empty Azure field collection
- no analyzed documents
- low-confidence fields
- raw text returned without structured fields

In all of these cases, the adapter does not throw only because invoice data is partial.

It returns whatever trustworthy data exists and lets the Application and Domain pipeline continue.

If required business fields are missing after mapping, the invoice can still become `RequiresHumanReview` through deterministic validation.

### Secret Handling Note

The examples above use inline values only to demonstrate the composition API.

In a real application, API keys should not be hardcoded in source code.

They should come from secure configuration, environment variables, user secrets, or a managed secret store.

---

## Internal Azure Client Contract

The Azure provider uses an internal client abstraction inside the Infrastructure layer.

This abstraction is not part of the public SDK surface.

The public Application-level abstraction remains:

```csharp
IDocumentExtractor
```

The internal Azure client contract exists to keep the extractor testable and to isolate Azure SDK details from the rest of the system.

Current internal flow:

```text
AzureDocumentIntelligenceAnalyzeRequest
→ carries ModelId, DocumentInput, and MinimumConfidenceThreshold
→ IAzureDocumentIntelligenceClient
→ AzureDocumentIntelligenceSdkClient
→ Azure SDK
→ ExtractedDocument
```

The current SDK client behavior is intentionally focused:

- passes `ModelId` to Azure
- opens the document stream from `DocumentInput`
- sends the stream content to Azure
- passes the cancellation token
- returns `AnalyzeResult.Content` as `ExtractedDocument.RawText`
- maps selected Azure invoice fields into `ExtractedDocument.Fields`
- maps analyzed page count from Azure result pages when available
- ignores missing fields
- ignores fields below `MinimumConfidenceThreshold`
- extracts invoice currency from Azure money fields using a clear priority order

This keeps the Azure integration useful for the MVP without expanding into full provider complexity too early.

---

## Provider Resilience and Retry Decision

InvoiceFlow intentionally does not implement retry policy in the Application layer.

The Application layer owns the invoice processing use case.

It should continue to depend only on abstractions such as:

```csharp
IDocumentExtractor
IInvoiceRepository
```

Provider-specific resilience belongs in the Infrastructure layer or provider setup.

Rate limits, transient network failures, provider timeouts, `Retry-After` behavior, SDK retry configuration, and provider throttling rules are technical infrastructure concerns.

Current architectural decision:

- Domain must not know about retry.
- Application must not know about Azure, HTTP 429, SDK retry options, or provider throttling details.
- API must not retry provider calls.
- Azure-specific retry should be configured around the Azure provider or Azure SDK client setup in a future provider-resilience slice.
- A future cross-provider retry mechanism may use a decorator around `IDocumentExtractor`, but only if multiple providers need the same resilience behavior.

For now, provider failures continue to surface through `DocumentExtractionFailedException`.

The API maps that Application-level signal to:

```text
503 Service Unavailable
```

with:

```text
DOCUMENT_EXTRACTION_FAILED
```

This keeps the MVP deterministic and avoids adding operational retry policy before real provider behavior is measured.

Retry policy is intentionally still not implemented.

---

## Future Provider Direction

Future direction may look like:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureDocumentIntelligenceIfConfigured()
    .UseSqlServerInvoiceRepository(options =>
    {
        options.ConnectionString = configuration.GetConnectionString("InvoiceFlow")
            ?? throw new InvalidOperationException("InvoiceFlow SQL connection string is required.");
    });
```

The current `UseAzureDocumentIntelligence(...)` method allows the system to use Azure Document Intelligence without changing the Application use case.

The `UseAzureDocumentIntelligenceIfConfigured()` method allows hosts such as the API project to select Azure through configuration while keeping local development on the fake extractor when Azure configuration is missing. The older configuration-parameter overload was removed to keep the public composition API smaller and clearer.

The Azure field mapping MVP slice is now implemented for the core business fields.

Future Azure-related slices may include:

1. Validate behavior against real Azure Document Intelligence responses.
2. Add more invoice schema fields only when the business flow needs them.
3. Add line item parsing as a separate, explicit slice.
4. Add richer confidence metadata if the product needs explainability at field level.
5. Add provider resilience such as retry policy or circuit breaker after real provider behavior is understood.

Full invoice schema parsing should remain separate because it has its own business and regression risks.

### Future Extraction Quality Findings

During manual Swagger UI verification with a synthetic invoice PDF, the API successfully processed the document and returned a `Verified` invoice.

Two extraction-quality findings were observed and intentionally left for a future provider quality slice:

- the source document contained a vendor tax id, but the processed response returned `vendorTaxId` as `null`
- the source document declared `Currency: ILS`, but the processed response returned `CAD`

These findings do not block the current Developer Experience slice.

They are not OpenAPI, Swagger, Postman, or API key issues.

They belong to a future extraction quality and provider mapping review, where field extraction accuracy, currency normalization, and optional versus required business fields can be evaluated deliberately.

### Future Extraction Quality Findings

During manual Swagger UI verification with a synthetic invoice PDF, the API successfully processed the document and returned a `Verified` invoice.

Two extraction-quality findings were observed and intentionally left for a future provider quality slice:

- the source document contained a vendor tax id, but the processed response returned `vendorTaxId` as `null`
- the source document declared `Currency: ILS`, but the processed response returned `CAD`

These findings do not block the current Developer Experience slice.

They are not OpenAPI, Swagger, Postman, or API key issues.

They belong to a future extraction quality and provider mapping review, where field extraction accuracy, currency normalization, and optional versus required business fields can be evaluated deliberately.

---

## SQL Server Persistence

InvoiceFlow now includes a first real SQL Server persistence slice for invoices.

This slice is intentionally small and MVP-focused.

It implements invoice saving through:

```csharp
SqlServerInvoiceRepository
```

registered through:

```csharp
builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseSqlServerInvoiceRepository(options =>
    {
        options.ConnectionString = configuration.GetConnectionString("InvoiceFlow")
            ?? throw new InvalidOperationException("InvoiceFlow SQL connection string is required.");
    });
```

The current SQL implementation uses `Microsoft.Data.SqlClient` directly.

It does not use Entity Framework Core or an ORM.

That is intentional for the MVP.

The repository currently supports:

- validating repository options
- opening a SQL Server connection
- inserting an invoice into `dbo.Invoices`
- storing searchable invoice fields as regular SQL columns
- storing invoice metadata as JSON
- storing the validation report as JSON
- preserving the final invoice status

The API can now select SQL persistence through configuration.

When `InvoiceFlow:SqlServer:ConnectionString` is not configured, the API keeps using the in-memory repository for local demos and lightweight development.

When `InvoiceFlow:SqlServer:ConnectionString` is configured, the API resolves `IInvoiceRepository` to `SqlServerInvoiceRepository` and persists processed invoices to SQL Server.

This behavior is covered by API configuration tests.

The current SQL schema uses a single table:

```text
dbo.Invoices
```

This keeps persistence simple, predictable, and aligned with YAGNI.

Complex data such as metadata and validation report output is stored as JSON instead of being split into multiple relational tables too early.

The SQL schema is tracked in the project under:

```text
database/001_create_invoices_table.sql
```

Local development uses a dedicated database such as:

```text
InvoiceFlowDb
```

SQL integration tests use a separate database such as:

```text
InvoiceFlowTests
```

Local API execution was also manually verified against `InvoiceFlowDb` using `user-secrets`, `curl`, and SSMS.

The manual flow proved:

```text
POST /api/invoices/process
→ API upload validation
→ Application invoice processing pipeline
→ SqlServerInvoiceRepository
→ dbo.Invoices in InvoiceFlowDb
```

A later 50-document live full-pipeline smoke test also verified SQL persistence as part of the complete API → Azure Blob Storage → Azure Document Intelligence → validation → SQL Server flow. The SQL invoice count increased by exactly 50 during that run.

The optional SQL persistence tests run only when the following environment variable is defined:

```text
INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING
```

Example local test setup:

```bash
export INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING="Server=172.31.192.1,14333;Database=InvoiceFlowTests;User Id=invoiceflow_test;Password=<local-password>;Encrypt=True;TrustServerCertificate=True;"

dotnet test --filter "FullyQualifiedName~SqlServerInvoiceRepositoryPersistenceTests"
dotnet test --filter "FullyQualifiedName~InvoiceFlowApiSqlServerPersistenceIntegrationTests"
```

For manual local API execution, SQL configuration should be stored with user secrets or another secure local configuration source:

```bash
dotnet user-secrets set \
  "InvoiceFlow:SqlServer:ConnectionString" \
  "Server=<server>;Database=InvoiceFlowDb;User Id=<user>;Password=<local-password>;Encrypt=True;TrustServerCertificate=True;" \
  --project src/InvoiceFlow.Api/InvoiceFlow.Api.csproj
```

Secrets and local SQL passwords must not be committed to Git.

Current SQL scope is save-only.

The following SQL behaviors are intentionally not implemented yet:

- reading invoices back from SQL
- updating invoice status
- querying invoices by status, vendor, date, or invoice number
- duplicate invoice detection
- automated database migrations
- production-grade operational database setup

Those should be added only when the product flow needs them.

---

## API Layer

InvoiceFlow currently includes a thin API adapter.

Current endpoints:

```text
GET  /health
POST /api/invoices/process
```

The invoice processing endpoint receives a file using `multipart/form-data`, creates a stream-ready `DocumentInput`, calls `IInvoiceDocumentProcessor`, and returns a structured response.

When API key client identity is configured through `UseApiKeyClientIdentity(...)`, the same endpoint requires a valid `X-API-Key` header before the invoice processing pipeline is executed.

A valid API key resolves the request client id and allows the processing-run audit decorator to save `ProcessingRun.ClientId` for usage tracking.

When client rate limiting is configured, the invoice processing endpoint applies per-client rate limiting after API key validation and before invoice processing.

If the configured limit is exceeded, the API returns `429 Too Many Requests` with a stable `RATE_LIMIT_EXCEEDED` error body and does not execute the invoice processor.

The health endpoint remains public and is not protected by API key identity or client rate limiting.

In the default composed setup, `IInvoiceDocumentProcessor` is wrapped by a processing-run audit decorator before reaching the core `ProcessInvoiceDocumentService`.

When Azure Blob Storage configuration is provided, the same endpoint stores the original uploaded document through `AzureBlobDocumentStorage`.

When Azure Blob Storage configuration is missing, the API keeps the local `InMemoryDocumentStorage` setup for local demos and lightweight development.

When SQL Server configuration is provided, the same endpoint persists the processed invoice into `dbo.Invoices` through `SqlServerInvoiceRepository`.

When Azure Document Intelligence configuration is provided, the API resolves `IDocumentExtractor` to `AzureDocumentIntelligenceDocumentExtractor`.

When Azure Document Intelligence configuration is missing, the API keeps the local `FakeDocumentExtractor` setup so local development and tests do not require cloud credentials.

The API layer does not contain business validation logic.

It only handles HTTP and input-boundary concerns:

- API key validation through `X-API-Key` when client identity is configured
- per-client rate limiting when client identity and rate limiting are configured
- multipart request validation
- malformed form-data handling
- file presence validation
- single-file upload enforcement
- file size validation
- supported content type validation
- basic file signature validation
- file name sanitization
- content type normalization
- stream-based `DocumentInput` handoff
- response shaping
- document storage failure response mapping
- extraction failure response mapping
- persistence failure response mapping
- Swagger/OpenAPI documentation

Provider-specific failures are not handled directly in the API.

The API only knows about Application-level failure signals such as `DocumentStorageFailedException`, `DocumentExtractionFailedException`, and `InvoicePersistenceFailedException`, and maps them to stable HTTP responses.

This keeps Azure-specific details out of the HTTP layer.

---

## Supported Upload Types

Current supported MVP file types:

- PDF
- JPG / JPEG
- PNG

Supported content types:

```text
application/pdf
image/jpeg
image/png
```

The API also supports content types with parameters.

For example:

```text
application/pdf; charset=utf-8
```

is normalized internally to:

```text
application/pdf
```

---

## Upload Validation and Security Checks

The API currently protects the upload boundary with:

- rejection of non-multipart requests
- rejection of malformed multipart form data
- rejection of missing files
- rejection of empty files
- rejection of multiple invoice files in one request
- rejection of unsupported content types
- configurable file size limit
- basic magic-number / file-signature validation
- file name sanitization to remove path segments
- conversion of invalid document input into a stable API error

Examples:

```text
../../invoice.pdf        → invoice.pdf
C:\fakepath\invoice.pdf  → invoice.pdf
```

The current magic-number validation checks the beginning bytes of the uploaded file stream before creating the Application `DocumentInput`.

This avoids unnecessary processing for files that clearly do not match their declared type.

After validation, the API creates a stream-ready `DocumentInput` and passes it into the processing pipeline without exposing uploaded content as a public byte-array buffer.

This is an intentional SDK contract improvement.

It prevents consumers from depending on a public in-memory document buffer and prepares the system for direct storage integrations such as Azure Blob Storage.

Note:

The API still uses ASP.NET Core multipart form handling, which may buffer form data internally depending on host configuration and request size.

A full low-level multipart streaming parser is intentionally not implemented yet.

That should remain a separate API hardening slice if the product needs very large files or high-throughput upload scenarios.

---

## Upload Configuration

Upload settings are configurable through configuration.

Current section:

```json
{
  "InvoiceFlow": {
    "Upload": {
      "MaxFileSizeInBytes": 10485760
    }
  }
}
```

Default:

```text
10 MB
```

The API validates upload options on startup.

Invalid configuration, such as a maximum file size of `0`, causes the application to fail fast instead of starting with broken settings.

The test host also provides a valid upload configuration explicitly, so API integration tests remain deterministic even when the regular application settings file is not loaded by the test environment.

---

## API Error Response Format

HTTP-level errors use a consistent error response format:

```json
{
  "code": "FILE_REQUIRED",
  "message": "Invoice document file is required."
}
```

Current API error examples:

| Code | Meaning |
|---|---|
| `INVALID_CONTENT_TYPE` | Request is not `multipart/form-data`. |
| `INVALID_FORM_DATA` | Multipart form data is malformed. |
| `FILE_REQUIRED` | File is missing or empty. |
| `TOO_MANY_FILES` | More than one invoice document file was uploaded. |
| `FILE_TOO_LARGE` | File exceeds the configured upload limit. |
| `UNSUPPORTED_FILE_CONTENT_TYPE` | File content type is not supported. |
| `INVALID_FILE_SIGNATURE` | File bytes do not match the declared file type. |
| `INVALID_FILE_NAME` | File name is missing or invalid. |
| `INVALID_DOCUMENT` | Uploaded document failed input model validation. |
| `INVALID_API_KEY` | API key is missing, inactive, unknown, or invalid. |
| `RATE_LIMIT_EXCEEDED` | Valid API key exceeded the configured per-client request limit. |
| `DOCUMENT_STORAGE_FAILED` | Document storage failed because of an infrastructure or object storage error. |
| `DOCUMENT_EXTRACTION_FAILED` | Document extraction failed because of an infrastructure or provider error. |
| `INVOICE_PERSISTENCE_FAILED` | Invoice persistence failed because of an infrastructure or database error. |

Important:

`DOCUMENT_STORAGE_FAILED` is treated as a system/infrastructure failure, not as a business validation result.

For example, if Azure Blob Storage, Azurite, or another object storage provider is unavailable while saving the original document, the API returns:

```text
503 Service Unavailable
```

with a stable API error body.

This must not be interpreted as:

```text
RequiresHumanReview
```

`DOCUMENT_EXTRACTION_FAILED` is treated as a system/infrastructure failure, not as a business validation result.

For example, if a document intelligence provider fails because of rate limiting, network failure, or another provider-side issue, the API returns:

```text
503 Service Unavailable
```

with a stable API error body.

This must not be interpreted as:

```text
RequiresHumanReview
```

`RequiresHumanReview` is reserved only for documents that were successfully extracted and mapped, but failed deterministic business validation.

`RATE_LIMIT_EXCEEDED` is an API usage-control response.

It means the API key was valid, the client id was resolved, and the client exceeded the configured rate limit for the invoice processing endpoint.

In that case, the API returns:

```text
429 Too Many Requests
```

with:

```json
{
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Rate limit exceeded. Please try again later."
}
```

The invoice processor is not executed for blocked requests.

`INVOICE_PERSISTENCE_FAILED` is also treated as a system/infrastructure failure, not as a business validation result.

For example, if SQL Server is unavailable, the connection string is invalid, or saving the invoice fails after processing, the API returns:

```text
503 Service Unavailable
```

with a stable API error body.

The API error contract is covered by tests to make sure clients can rely on a stable response shape:

```json
{
  "code": "...",
  "message": "..."
}
```

---

## Swagger / OpenAPI

Swagger is available in Development mode.

Run the API:

```bash
dotnet run --project src/InvoiceFlow.Api/InvoiceFlow.Api.csproj
```

Open:

```text
http://localhost:5030
```

The root path redirects to Swagger in Development mode.

Swagger currently documents:

- `GET /health`
- `POST /api/invoices/process`
- multipart file upload
- API key security scheme for `X-API-Key`
- `POST /api/invoices/process` OpenAPI security requirement
- `200 OK` processing response
- `400 Bad Request` API errors
- `401 Unauthorized` API key errors
- `429 Too Many Requests` client rate limit errors
- `413 Payload Too Large` API errors
- `503 Service Unavailable` document storage, document extraction/provider, and invoice persistence failures

The invoice processing endpoint is documented as a protected endpoint in OpenAPI.

The health endpoint remains public and does not require an API key.

The OpenAPI metadata is covered by tests to prevent accidental regressions in developer experience.

---

## Example API Usage

Create a small PDF-like test file:

```bash
printf '%s\n' '%PDF-1.7 fake invoice content' > /tmp/invoice.pdf
```

Send it to the API:

```bash
curl -i -X POST http://localhost:5030/api/invoices/process \
  -F "file=@/tmp/invoice.pdf;type=application/pdf"
```

When API key client identity is enabled, include the API key header:

```bash
curl -i -X POST http://localhost:5030/api/invoices/process \
  -H "X-API-Key: if_dev_valid-secret-key" \
  -F "file=@/tmp/invoice.pdf;type=application/pdf"
```

If API key client identity is configured and the header is missing or invalid, the endpoint returns:

```text
HTTP/1.1 401 Unauthorized
```

with the stable machine-readable error code:

```json
{
  "code": "INVALID_API_KEY",
  "message": "..."
}
```

Clients should use the code field for programmatic error handling.

Expected result:

```text
HTTP/1.1 200 OK
```

With a response body containing either:

```json
{
  "status": "Verified"
}
```

or:

```json
{
  "status": "RequiresHumanReview"
}
```

depending on the extracted invoice data and validation result.

---

## Postman Collection

A minimal Postman collection is available at:

```text
docs/postman/InvoiceFlow.postman_collection.json
```

The collection is intentionally small and matches the current API surface.

It includes:

- public GET /health
- POST /api/invoices/process with X-API-Key
- POST /api/invoices/process without X-API-Key
- POST /api/invoices/process with an invalid non-multipart content type

The collection uses variables instead of hardcoded request values:

- {{base_url}}
- {{api_key}}

This allows developers to switch between local, staging, and future hosted environments without editing every request.

The collection does not yet include a dedicated rate-limit scenario.

It also does not document billing, login, OAuth, or JWT flows because those features are not part of the current API contract.

---

## Example Successful Response

A valid invoice may return:

```json
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
```

---

## Example Human Review Response

If business validation finds an error, the API still returns `200 OK`, because the document was processed successfully.

Example:

```json
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
```

---

## Example Document Storage Failure Response

If document storage fails before extraction starts, the API does not return `RequiresHumanReview`.

Example:

```text
HTTP/1.1 503 Service Unavailable
```

```json
{
  "code": "DOCUMENT_STORAGE_FAILED",
  "message": "Document storage failed. Please try again later."
}
```

This response means the original document could not be stored, so the invoice did not reach extraction or business validation.

A client can safely treat this as an infrastructure failure, depending on its own retry policy and operational needs.

This behavior is covered at two levels:

- Application wrapping from storage provider failure to `DocumentStorageFailedException`
- full API pipeline regression proving a real storage failure is returned by the API as `DOCUMENT_STORAGE_FAILED`

---

## Example Document Extraction Failure Response

If document extraction fails because of an infrastructure or provider issue, the API does not return `RequiresHumanReview`.

Example:

```text
HTTP/1.1 503 Service Unavailable
```

```json
{
  "code": "DOCUMENT_EXTRACTION_FAILED",
  "message": "Document extraction failed. Please try again later."
}
```

This response means the document did not reach the business validation stage.

A client can safely treat this as a retryable infrastructure failure, depending on its own retry policy and operational needs.

InvoiceFlow does not currently implement retry policy internally.

That is intentionally left for a later provider-resilience slice.

---

## Example Invoice Persistence Failure Response

If invoice persistence fails after the document was extracted, mapped, and validated, the API does not return `RequiresHumanReview`.

Example:

```text
HTTP/1.1 503 Service Unavailable
```

```json
{
  "code": "INVOICE_PERSISTENCE_FAILED",
  "message": "Invoice persistence failed. Please try again later."
}
```

This response means the invoice processing pipeline reached the persistence step, but the invoice could not be saved.

A client can safely treat this as an infrastructure failure, depending on its own retry policy and operational needs.

This behavior is covered at two levels:

- direct API mapping from `InvoicePersistenceFailedException` to `503 Service Unavailable`
- full API pipeline regression proving a real repository failure is wrapped by the Application layer and returned by the API as `INVOICE_PERSISTENCE_FAILED`

InvoiceFlow does not currently implement retry policy internally.

That is intentionally left for a later provider-resilience slice.

---

## Current Validation Rules

The default invoice validator currently detects:

- missing vendor
- missing invoice number
- missing issue date
- future issue date
- missing subtotal amount
- missing VAT amount
- missing total amount
- mixed currencies
- subtotal + VAT mismatch

Validation errors cause the invoice to be marked as:

```text
RequiresHumanReview
```

A valid invoice is marked as:

```text
Verified
```

Warnings do not currently force human review.

Low-confidence Azure fields are currently filtered out before mapping rather than represented as warnings.

That keeps the current Application contract simple.

If confidence explainability becomes a product requirement, it should be added as a separate slice.

---

## Tests

The project includes tests for:

- Domain models
- value objects
- validation issues
- validation reports
- default invoice validation rules
- Application use case orchestration
- stream-ready `DocumentInput` contract
- Application stream pipeline behavior proving storage and extractor can read independent document streams
- Application provider output guards
- Application exception wrapping for document storage failures
- Application exception wrapping for document extraction failures
- Application exception wrapping for invoice persistence failures
- analyzed page count propagation from extraction result to processing result
- ProcessingRun model validation
- ProcessingRun repository contract
- ProcessingRun decorator behavior for successful processing attempts
- ProcessingRun decorator behavior for document storage, extraction, and persistence failures
- ProcessingRun Fluent Composition registration
- default processing client context registration
- Application API key validation result contract
- Application API key validator contract
- configured API key validator behavior
- API key hash generation
- API key identity options validation
- API key identity Fluent Composition
- HTTP processing client context behavior
- API key endpoint filter behavior
- API key protected invoice endpoint integration behavior
- OpenAPI `401 Unauthorized` metadata for the invoice processing endpoint
- in-memory infrastructure
- thread-safety behavior for in-memory storage/repository
- Fluent DI composition
- custom document extractor registration
- Azure Document Intelligence composition
- Azure configuration-based provider selection composition
- Azure Blob Storage provider options validation
- Azure Blob Storage upload behavior through Azurite
- API Azure provider configuration behavior
- real Azure smoke test skip/run transparency
- real Azure extractor smoke test against a real Azure Document Intelligence resource
- real Azure full-pipeline smoke test through extractor, mapper, validator, and in-memory repository
- Azure provider options validation
- Azure `ModelId` default and validation behavior
- Azure minimum confidence threshold default and validation behavior
- Azure extractor skeleton behavior
- Azure internal client contract behavior
- Azure request handoff of `ModelId`, document, cancellation token, and confidence threshold
- Azure SDK client adapter behavior
- Azure SDK stream-based document handoff behavior
- Azure SDK client DI wiring
- Azure raw text extraction behavior
- Azure selected invoice field mapping
- Azure missing-field handling
- Azure low-confidence field filtering
- Azure empty-field and no-document handling
- Azure currency priority and fallback behavior
- Azure mapped fields flowing through mapper, invoice model, validator, and repository
- Azure partial or low-confidence mapped data flowing through the business pipeline as `RequiresHumanReview`
- API integration behavior
- health endpoint
- Swagger/OpenAPI metadata
- upload configuration binding
- startup options validation
- API input validation
- API error response contract
- API behavior when document storage fails
- API full-pipeline document storage failure behavior using the real processing service and a failing storage provider
- API behavior when document extraction fails
- API behavior when invoice persistence fails
- API full-pipeline persistence failure behavior using the real processing service and a failing repository
- API real-pipeline persistence failure regression covering repository failure after successful extraction, mapping, and validation
- malformed multipart handling
- multiple-file upload rejection
- basic file security checks
- file name sanitization
- content type normalization
- clean stream-ready `DocumentInput` handoff from API to Application
- Azurite-backed Azure Blob Storage integration test
- SQL Server invoice repository options and registration
- SQL Server invoice `SaveAsync` persistence behavior
- API SQL configuration behavior
- API endpoint persistence into a real SQL Server database
- optional SQL Server integration tests against a real database

Run all tests:

```bash
dotnet test
```

Run build and tests:

```bash
dotnet build
dotnet test
```

---

## Current Project Structure

```text
src/
  InvoiceFlow.Api/
    ClientIdentity/
    Health/
    Invoices/
  InvoiceFlow.Application/
    ClientIdentity/
    Documents/
    Invoices/
    ProcessingRuns/
  InvoiceFlow.Domain/
    Invoices/
    ValueObjects/
  InvoiceFlow.Infrastructure/
    ClientIdentity/
    DependencyInjection/
    Documents/
    Invoices/
    ProcessingRuns/

database/
  001_create_invoices_table.sql

tests/
  InvoiceFlow.Tests/
    Api/
    Application/
    Composition/
    Domain/
    Infrastructure/
    Integration/
```

---

## Implemented So Far

The current MVP includes:

- invoice domain model
- currency amount value object
- vendor model
- invoice statuses
- validation issue model
- validation report model
- default invoice validator
- document input model
- stream-ready document input contract
- independent document stream opening through `OpenReadStreamAsync(...)`
- document storage contract
- document extractor contract
- invoice mapper contract
- invoice repository contract
- public invoice document processor contract
- application use case for processing invoice documents
- application guards for invalid extractor and mapper outputs
- application exception wrapping for document storage failures
- application exception wrapping for document extraction failures
- application exception wrapping for invoice persistence failures
- analyzed page count on extracted documents
- analyzed page count propagation to processing results
- ProcessingRun usage audit model
- ProcessingRun repository contract
- ProcessingRun audit decorator around `IInvoiceDocumentProcessor`
- default processing client context for local and MVP execution
- in-memory ProcessingRun repository
- ProcessingRun Fluent Composition registration
- in-memory document storage provider Fluent registration
- Azure Blob Storage provider options validation
- Azure Blob Storage document upload through Azure Storage Blobs SDK
- Azurite-backed Azure Blob Storage integration test
- fake document extractor
- field-based invoice mapper
- in-memory invoice repository
- thread-safe in-memory implementations
- DI registration
- minimal Fluent Composition API
- custom document extractor registration
- Azure Document Intelligence provider registration
- Azure provider options validation
- Azure `ModelId` option with default `prebuilt-invoice`
- Azure minimum confidence threshold option with default `0.8`
- internal Azure analyze request contract
- internal Azure client abstraction
- Azure SDK client adapter
- Azure SDK client DI wiring
- Azure raw text extraction support
- Azure selected invoice field mapping
- Azure missing-field handling
- Azure low-confidence field filtering
- Azure currency extraction from money fields
- Azure field mapping integration through the business pipeline
- configuration-based Azure provider selection through `UseAzureDocumentIntelligenceIfConfigured(...)`
- API Azure provider selection through configuration
- real Azure extractor smoke test guarded by environment variables
- real Azure full-pipeline smoke test guarded by environment variables
- SQL Server invoice repository options
- SQL Server invoice repository Fluent registration
- SQL Server invoice repository `SaveAsync` implementation
- single-table SQL Server invoice persistence to `dbo.Invoices`
- SQL schema script under `database/001_create_invoices_table.sql`
- API SQL repository selection through configuration
- optional SQL Server persistence integration test guarded by `INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING`
- optional API-to-SQL persistence integration test guarded by `INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING`
- manual API execution verified against `InvoiceFlowDb`
- first API endpoint for processing invoice files
- health endpoint
- API response DTOs
- API error DTO
- upload options
- API input validation
- basic file signature checks
- file name sanitization
- content type normalization
- API mapping for document storage failures
- API mapping for document extraction failures
- API mapping for invoice persistence failures
- Swagger/OpenAPI documentation
- integration tests for the full flow
- hardened API boundary tests

---

## Not Implemented Yet

The following features are intentionally not implemented yet:

- full Azure invoice schema parsing
- Azure line item parsing
- rich field-level confidence metadata in Application response
- confidence-based warning output
- OpenAI Vision integration
- SQL repository read, update, and query operations
- automated database migrations
- SQL-backed ProcessingRun persistence
- per-client rate limiting
- automated billing integration
- authentication
- authorization
- dashboard / review UI
- invoice export
- accounting software integration
- duplicate invoice detection
- Israeli tax id validation
- retry policy
- provider retry configuration
- queue / background jobs
- dynamic mapping into custom developer-defined objects
- full low-level multipart streaming parser
- full Fluent Pipeline API

These are future steps.

The current focus is a clean, testable processing core with a thin API adapter, developer-friendly composition setup, a stream-ready document input contract, a first real Azure Document Intelligence mapping slice for the fields needed by the MVP, verified configuration-based provider selection for Azure, real Azure extractor and full-pipeline smoke coverage, Azure Blob document storage verified against Azurite, a verified SQL-backed invoice persistence slice from API to database, a lightweight processing-run usage audit foundation for future SaaS readiness, and API key based client identity for resolving real client ids on protected invoice processing requests.

---

## Development Principles

InvoiceFlow is developed with the following principles:

- SOLID
- Clean Architecture
- KISS
- YAGNI
- deterministic business validation
- thin API layer
- replaceable infrastructure
- SDK-first composition
- test-first progression
- explicit business outcomes
- safe handling of external input
- developer experience as part of the product

The system is intentionally built in small steps.

Each new behavior is tested before moving to the next layer.
