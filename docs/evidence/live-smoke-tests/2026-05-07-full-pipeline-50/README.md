# InvoiceFlow Live Full-Pipeline Smoke Test Evidence
ש
## Run Summary

Date: 2026-05-07  
Run type: Live full-pipeline smoke test  
Document count: 50  
Execution mode: Sequential  
Environment: Local API host with real external providers configured through local user-secrets

## Pipeline Covered

```text
API Upload
→ Azure Blob Storage
→ Azure Document Intelligence
→ Field Mapping
→ Domain Validation
→ SQL Server Persistence
→ Stable API Response

Result
The 50-document live smoke test completed successfully.

Total documents: 50
Passed: 50
Failed: 0
Infrastructure failures: 0
Missing files: 0

Business Outcome Distribution
Verified: 30
RequiresHumanReview: 20

Validation Issue Distribution
TOTAL_MISMATCH: 10
MISSING_VENDOR: 5
MISSING_TOTAL_AMOUNT: 5

Persistence Verification
SQL Server invoice count before the 50-document run:

32


SQL Server invoice count after the 50-document run:

82


Expected increase:

50


Actual increase:

50


Blob Storage Verification
Azure Blob container count before the 50-document run:

20


Azure Blob container count after the 50-document run:

70


Expected increase:

50


Actual increase:

50


Identity and Consistency Checks
The audit verified:

Response files: 50
Document IDs: 50
Duplicate document IDs: 0
Invoice IDs: 50
Duplicate invoice IDs: 0
Missing IDs: 0
sourceDocumentId mismatches: 0

Timing Summary
Average duration: 9.985 seconds
Median duration: 7.931 seconds
Minimum duration: 4.947 seconds
Maximum duration: 26.299 seconds

Some documents took more than 15 seconds, but all completed successfully. This is treated as provider latency evidence, not as a product failure.

Conclusion
InvoiceFlow successfully completed a 50-document live full-pipeline smoke test covering API upload, Azure Blob Storage, Azure Document Intelligence, deterministic business validation, SQL Server persistence, and stable API responses.
This validates the current MVP pipeline at live smoke-test level.
This was not a load test, stress test, or production readiness test.

