# InvoiceFlow Portfolio

InvoiceFlow is a backend-focused .NET project for reliable invoice document processing.

It combines AI-assisted data extraction with deterministic domain validation, so extracted invoice data is not treated as trusted business data until it passes clear business rules.

The project is presented here as a portfolio case study, not as a marketing landing page.

The goal is to show the engineering work behind the system: architecture, provider boundaries, API stability, infrastructure integration, testing, and product restraint.

---

## Start here

### 1. Case Study

Read this first for the main story:

[InvoiceFlow Case Study](./invoiceflow-case-study.md)

This page explains what problem InvoiceFlow solves, what was built, the main architecture decisions, real integrations, evidence, and what the project demonstrates as backend engineering work.

### 2. Architecture Overview

Read this next for a quick technical view:

[Architecture Overview](./invoiceflow-architecture-overview.md)

This page summarizes the processing pipeline, layered architecture, responsibility boundaries, runtime modes, API boundary hardening, and scope control.

### 3. Full Technical README

For the complete project documentation, see:

[Full InvoiceFlow README](../../README.md)

The full README contains the detailed implementation notes, configuration examples, API behavior, validation rules, tests, and development principles.


---

## Current proof points

InvoiceFlow currently demonstrates:

- Clean Architecture with clear layer boundaries
- deterministic invoice validation
- Azure Document Intelligence integration
- Azure Blob Storage document storage
- SQL Server invoice persistence
- API key based client identity
- per-client rate limiting
- stable API error responses
- Swagger and Postman developer experience
- broad automated test coverage
- controlled full-pipeline smoke verification

A 50-document full-pipeline smoke test was completed successfully with:

```text
Total documents: 50
Passed: 50
Failed: 0
Infrastructure failures: 0
Missing files: 0
```

---

## Scope

InvoiceFlow is intentionally kept focused.

It is not presented as a full SaaS product, accounting platform, or dashboard application.

The current portfolio focus is the backend foundation:
reliable document processing, clean provider boundaries,
deterministic validation, API stability, real infrastructure integration,
and test-driven development.
