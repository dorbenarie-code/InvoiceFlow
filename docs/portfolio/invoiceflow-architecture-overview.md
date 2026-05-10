# InvoiceFlow Architecture Overview

This document gives a quick technical overview of InvoiceFlow without going into the full README-level details.

InvoiceFlow is built as a backend processing foundation for invoice documents.

The architecture is driven by separation of concerns: AI is treated as an external data provider, while deterministic domain rules retain full authority over business trust.

Document processing, business validation, provider integrations, API concerns, and persistence are kept separated from each other.

---

## Processing pipeline

~~~text
Client
  │
  ▼
API upload boundary
  │
  ├─ validates multipart request
  ├─ validates file type and size
  ├─ validates basic file signature
  └─ creates stream-ready DocumentInput
  │
  ▼
Application use case
  │
  ├─ stores original document
  ├─ extracts document data
  ├─ maps extracted fields into Invoice
  ├─ runs deterministic validation
  ├─ saves invoice
  └─ records processing run
  │
  ▼
Structured API response
~~~

The important point is that extraction is only one step in the pipeline.

The system does not treat extracted data as trusted business data until the domain validation rules run.

---

## Layered architecture

~~~text
┌────────────────────────────────────────────┐
│ API                                        │
│ HTTP, upload validation, API key, rate limit│
└────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────┐
│ Application                                │
│ Processing use case, contracts, orchestration│
└────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────┐
│ Domain                                     │
│ Invoice model, value objects, validation rules│
└────────────────────────────────────────────┘
                    ▲
                    │
┌────────────────────────────────────────────┐
│ Infrastructure                             │
│ Azure, SQL Server, Blob Storage, in-memory providers│
└────────────────────────────────────────────┘
~~~

The Domain layer does not know about HTTP, Azure, SQL Server, files, or cloud providers.

The Application layer depends on abstractions.

Infrastructure implementations can be replaced without changing the core processing use case.

---

## Responsibility boundaries

| Area | Responsibility |
|---|---|
| API | Protects the HTTP boundary and shapes responses |
| Application | Runs the invoice processing use case |
| Domain | Decides whether invoice data is valid |
| Infrastructure | Connects to external systems and providers |
| Tests | Protect behavior across layers |

This separation keeps the system easier to test and safer to extend.

For example, Azure Document Intelligence can be replaced by another extractor without moving business validation into the provider.

SQL Server can be used for persistence without making the Domain depend on database details.

---

## Runtime modes

InvoiceFlow can run in two practical modes.

### Local / demo mode

~~~text
API
→ In-memory document storage
→ Fake document extractor
→ In-memory invoice repository
→ In-memory processing run repository
~~~

This mode is useful for local development, demos, and fast tests.

### Real infrastructure mode

~~~text
API
→ Azure Blob Storage
→ Azure Document Intelligence
→ SQL Server
→ Processing run audit
~~~

This mode proves that the same architecture can connect to real infrastructure through configuration.

The core Application use case stays the same in both modes.

---

## API boundary hardening

The API boundary is intentionally narrow.

It handles:

- upload validation
- supported content types
- file size limits
- basic file signature checks
- file name sanitization
- API key based client identity
- per-client rate limiting
- stable error responses

The boundary behaves like a gate:

~~~text
[Blocked] Invalid request
          → fails upload validation

[Blocked] Valid client, but rate limit exceeded
          → rejected with 429 before invoice processing

[Passed]  Valid request under the limit
          → handed off to the Application pipeline
~~~

This keeps expensive processing work behind a controlled boundary.

---

## Scope Control (YAGNI)

InvoiceFlow is intentionally not designed as a full SaaS product at this stage.

The current architecture keeps the following areas out of scope:

- dashboard UI
- billing
- OAuth
- full user management
- accounting software integrations
- background job processing
- distributed rate limiting
- full Azure invoice schema parsing

These are possible future directions, but they are not required to prove the core backend foundation.

The current focus is reliable document processing, clean provider boundaries, deterministic validation, API stability, and developer experience.
