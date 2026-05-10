# InvoiceFlow: Reliable Invoice Processing on Top of AI Extraction

## A .NET backend foundation for turning invoice documents into validated business results

InvoiceFlow is a .NET backend project built around a simple idea:

> AI can help extract data. Reliable software still needs deterministic business rules.

The system receives invoice or receipt documents, stores the original file, extracts invoice fields, maps them into a strongly typed `Invoice` model, validates the result through domain rules, persists the invoice, and returns a structured API response.

The project started as an SDK-style idea: give .NET developers a clean invoice-processing foundation without forcing them to rebuild the same validation, storage, provider integration, upload safety, and error-handling pipeline from scratch.

InvoiceFlow is not trying to replace accounting software.

It focuses on the backend infrastructure behind reliable document processing.

---

## What problem does it solve?

Invoice processing looks simple from the outside:

1. Upload a document.
2. Extract fields.
3. Save the invoice.

In practice, a real backend needs to handle much more:

- unsafe or invalid file uploads
- missing or low-confidence extracted fields
- provider failures
- storage failures
- persistence failures
- business validation errors
- stable API responses
- developer-friendly configuration
- testable infrastructure boundaries

InvoiceFlow focuses on that foundation.

The goal is not to build a full SaaS product at this stage.

The goal is to build a clean processing core that can later evolve into a NuGet SDK, hosted API, or internal document-processing service.

---

## What I built

InvoiceFlow currently includes a full document-processing pipeline:

~~~text
Document upload
→ Upload validation
→ Original document storage
→ Data extraction
→ Mapping into an Invoice model
→ Deterministic business validation
→ Invoice persistence
→ Processing run audit
→ Structured API response
~~~

The system is built with layered architecture:

~~~text
Domain
Application
Infrastructure
API
Tests
~~~

The Domain layer owns the business model and validation rules.

The Application layer owns the main use case and depends on abstractions.

The Infrastructure layer contains replaceable implementations such as Azure Document Intelligence, Azure Blob Storage, SQL Server, in-memory repositories, API key validation, and rate limiting.

The API layer stays thin and handles HTTP concerns such as upload validation, API protection, rate limiting, Swagger/OpenAPI, and response shaping.

For a more visual breakdown of the pipeline, layers, and responsibility boundaries, see the [Architecture Overview](./invoiceflow-architecture-overview.md).

---

## Key architecture decisions

### 1. AI is treated as an external provider, not as business logic

Document intelligence providers can extract fields, but they do not decide whether an invoice is valid.

InvoiceFlow maps extracted data into a strongly typed invoice model and then applies deterministic validation rules.

That keeps the business decision inside the domain instead of hiding it inside an OCR or AI provider.

### 2. `RequiresHumanReview` is a valid business outcome

If the document was processed successfully but the extracted invoice data has business issues, the API can still return `200 OK`.

The response body tells the client that the invoice requires human review.

This is intentional.

A document that was successfully extracted but failed validation is different from a provider failure, SQL failure, or storage failure.

### 3. Infrastructure failures are separated from business validation

InvoiceFlow returns stable infrastructure error codes for failures such as:

~~~text
DOCUMENT_STORAGE_FAILED
DOCUMENT_EXTRACTION_FAILED
INVOICE_PERSISTENCE_FAILED
RATE_LIMIT_EXCEEDED
INVALID_API_KEY
~~~

This makes the API easier for clients to integrate with because they can react to stable machine-readable codes instead of parsing exception messages.

### 4. API security and stability are handled at the boundary

The API boundary is explicitly hardened.

It enforces upload validation, API-key-based client identity, hashed API keys instead of raw key storage, and per-client fixed-window rate limiting.

The flow is intentional:

~~~text
Missing or invalid API key → request is blocked
Valid API key over the limit → request is rejected with 429
Valid API key under the limit → invoice processing continues
~~~

This keeps invalid or abusive requests away from the expensive processing pipeline.

### 5. Product scope is intentionally controlled

Several features are intentionally not implemented yet:

- dashboard
- billing
- OAuth
- full accounting integration
- full Azure invoice schema parsing
- line item parsing
- background jobs
- distributed rate limiting
- SQL read/query screens
- full SaaS user management

This keeps the current version focused on the core backend problem instead of turning the project into an unfocused SaaS clone.

---

## Real integrations

InvoiceFlow currently includes:

- Azure Document Intelligence for document extraction
- Azure Blob Storage for original document storage
- SQL Server persistence for processed invoices
- Swagger/OpenAPI documentation
- Postman collection for manual API verification
- API key based client identity
- per-client fixed-window rate limiting
- stable API error response format

The API can run locally with in-memory infrastructure and switch to real providers through configuration.

This keeps the developer experience simple while proving that the architecture can connect to real infrastructure.

---

## Evidence

The system is covered by unit, integration, API, configuration, OpenAPI, and smoke tests.

A controlled 50-document full-pipeline smoke test was completed through the API using real infrastructure:

~~~text
Total documents: 50
Passed: 50
Failed: 0
Infrastructure failures: 0
Missing files: 0
~~~

This test helped verify that the pipeline can process multiple documents through the real API flow without treating every partial extraction as a system failure.

---

## What this project demonstrates

InvoiceFlow demonstrates practical backend engineering work:

- Clean Architecture and SOLID principles applied in a real project
- Replaceable provider design for OCR, storage, and persistence
- API boundary hardening with validation, client identity, and rate limiting
- Stable API error contracts for predictable client integration
- Test-driven progression across unit, integration, API, configuration, OpenAPI, and smoke tests
- YAGNI-based product restraint and clear separation between current scope and future features

The goal was not to build the largest possible feature set.

The goal was to build a reliable backend foundation that is understandable, testable, and ready to evolve without becoming messy too early.

---

## Tech stack

- C#
- .NET 8
- ASP.NET Core
- SQL Server
- Azure Document Intelligence
- Azure Blob Storage
- xUnit
- Swagger / OpenAPI
- Postman
- Clean Architecture
- Fluent dependency injection composition

---

## Current status

InvoiceFlow is currently an MVP-level backend foundation.

It has a working API, real provider integrations, SQL persistence, client identity, rate limiting, manual verification flows, and broad automated test coverage.

The next planned direction is not to add random features, but to improve product positioning, developer onboarding, and selected infrastructure maturity only where it strengthens the core use case.
