#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5030}"
LIVE_TEST_ROOT="tests/live-documents"
DOCUMENTS_DIR="$LIVE_TEST_ROOT/synthetic-invoices"
EXPECTED_RESULTS_FILE="$LIVE_TEST_ROOT/expected-results.json"

RUN_ID="${RUN_ID:-live-test-$(date +%Y%m%d-%H%M%S)}"
RESULTS_DIR="test-results/live-batch/$RUN_ID"

mkdir -p "$RESULTS_DIR"

if [ ! -f "$EXPECTED_RESULTS_FILE" ]; then
  echo "Missing expected results file: $EXPECTED_RESULTS_FILE"
  exit 1
fi

echo "InvoiceFlow live batch test"
echo "API_BASE_URL: $API_BASE_URL"
echo "RUN_ID:       $RUN_ID"
echo "RESULTS_DIR:  $RESULTS_DIR"
echo

HEALTH_STATUS="$(curl -s -o "$RESULTS_DIR/health.response.json" -w "%{http_code}" "$API_BASE_URL/health" || true)"

if [ "$HEALTH_STATUS" != "200" ]; then
  echo "API health check failed. HTTP status: $HEALTH_STATUS"
  echo "Response saved to: $RESULTS_DIR/health.response.json"
  exit 1
fi

python3 - "$EXPECTED_RESULTS_FILE" "$DOCUMENTS_DIR" "$RESULTS_DIR" "$API_BASE_URL" <<'PY'
import csv
import json
import subprocess
import sys
import time
from pathlib import Path

expected_results_path = Path(sys.argv[1])
documents_dir = Path(sys.argv[2])
results_dir = Path(sys.argv[3])
api_base_url = sys.argv[4].rstrip("/")

cases = json.loads(expected_results_path.read_text(encoding="utf-8"))

if not cases:
    raise SystemExit("expected-results.json contains no test cases.")

summary_rows = []
failures = []

for index, case in enumerate(cases, start=1):
    file_name = case["fileName"]
    expected_status = case["expectedStatus"]
    expected_invoice_number = case.get("expectedInvoiceNumber")
    expected_vendor_name = case.get("expectedVendorName")
    expected_issue_code = case.get("expectedIssueCode")

    document_path = documents_dir / file_name
    response_path = results_dir / f"{file_name}.response.json"

    print(f"[{index}/{len(cases)}] Uploading {file_name}...")

    if not document_path.exists():
        failures.append(f"{file_name}: missing document file: {document_path}")
        summary_rows.append({
            "fileName": file_name,
            "httpStatus": "MISSING_FILE",
            "actualStatus": "",
            "expectedStatus": expected_status,
            "durationSeconds": "",
            "result": "FAILED",
            "details": "Document file is missing"
        })
        continue

    started = time.perf_counter()

    command = [
        "curl",
        "-s",
        "-w", "\n%{http_code}",
        "-X", "POST",
        f"{api_base_url}/api/invoices/process",
        "-F", f"file=@{document_path};type=application/pdf"
    ]

    completed = subprocess.run(
        command,
        capture_output=True,
        text=True
    )

    duration = time.perf_counter() - started

    output = completed.stdout
    stderr = completed.stderr.strip()

    if completed.returncode != 0:
        failures.append(f"{file_name}: curl failed: {stderr}")
        summary_rows.append({
            "fileName": file_name,
            "httpStatus": "CURL_FAILED",
            "actualStatus": "",
            "expectedStatus": expected_status,
            "durationSeconds": f"{duration:.3f}",
            "result": "FAILED",
            "details": stderr
        })
        continue

    try:
        body, http_status = output.rsplit("\n", 1)
    except ValueError:
        failures.append(f"{file_name}: could not split curl body/status output")
        continue

    response_path.write_text(body, encoding="utf-8")

    actual_status = ""
    details = ""

    try:
        response_json = json.loads(body)
    except json.JSONDecodeError:
        response_json = None
        details = "Response is not valid JSON"

    if http_status.startswith("5") and http_status != "503":
        failures.append(f"{file_name}: unexpected server failure HTTP {http_status}")
        result = "FAILED"
    elif http_status == "503":
        error_code = response_json.get("code") if isinstance(response_json, dict) else ""
        if error_code in {
            "DOCUMENT_STORAGE_FAILED",
            "DOCUMENT_EXTRACTION_FAILED",
            "INVOICE_PERSISTENCE_FAILED"
        }:
            result = "INFRASTRUCTURE_FAILURE"
            details = error_code
        else:
            failures.append(f"{file_name}: unexpected 503 error code: {error_code}")
            result = "FAILED"
            details = error_code
    elif http_status != "200":
        failures.append(f"{file_name}: expected HTTP 200 or stable 503, got HTTP {http_status}")
        result = "FAILED"
    else:
        if not isinstance(response_json, dict):
            failures.append(f"{file_name}: response is not a JSON object")
            result = "FAILED"
        else:
            actual_status = response_json.get("status", "")

            if actual_status != expected_status:
                failures.append(
                    f"{file_name}: expected status {expected_status}, got {actual_status}"
                )
                result = "FAILED"
            else:
                result = "PASSED"

            invoice = response_json.get("invoice") or {}
            validation_report = response_json.get("validationReport") or {}
            issues = validation_report.get("issues") or []

            document_id = response_json.get("documentId")
            invoice_id = response_json.get("invoiceId")
            source_document_id = invoice.get("sourceDocumentId")

            if not document_id:
                failures.append(f"{file_name}: documentId is missing")
                result = "FAILED"

            if not invoice_id:
                failures.append(f"{file_name}: invoiceId is missing")
                result = "FAILED"

            if document_id and source_document_id and document_id != source_document_id:
                failures.append(
                    f"{file_name}: invoice.sourceDocumentId does not match documentId"
                )
                result = "FAILED"

            if expected_invoice_number:
                actual_invoice_number = invoice.get("invoiceNumber")
                if actual_invoice_number != expected_invoice_number:
                    failures.append(
                        f"{file_name}: expected invoiceNumber {expected_invoice_number}, got {actual_invoice_number}"
                    )
                    result = "FAILED"

            if expected_vendor_name:
                actual_vendor_name = invoice.get("vendorName")
                if actual_vendor_name != expected_vendor_name:
                    failures.append(
                        f"{file_name}: expected vendorName {expected_vendor_name}, got {actual_vendor_name}"
                    )
                    result = "FAILED"

            if expected_issue_code:
                issue_codes = {
                    issue.get("code")
                    for issue in issues
                    if isinstance(issue, dict)
                }

                if expected_issue_code not in issue_codes:
                    failures.append(
                        f"{file_name}: expected issue code {expected_issue_code}, got {sorted(issue_codes)}"
                    )
                    result = "FAILED"

    summary_rows.append({
        "fileName": file_name,
        "httpStatus": http_status,
        "actualStatus": actual_status,
        "expectedStatus": expected_status,
        "durationSeconds": f"{duration:.3f}",
        "result": result,
        "details": details
    })

summary_csv_path = results_dir / "summary.csv"
summary_json_path = results_dir / "summary.json"

with summary_csv_path.open("w", newline="", encoding="utf-8") as file:
    writer = csv.DictWriter(
        file,
        fieldnames=[
            "fileName",
            "httpStatus",
            "actualStatus",
            "expectedStatus",
            "durationSeconds",
            "result",
            "details"
        ])
    writer.writeheader()
    writer.writerows(summary_rows)

summary = {
    "total": len(summary_rows),
    "passed": sum(1 for row in summary_rows if row["result"] == "PASSED"),
    "failed": sum(1 for row in summary_rows if row["result"] == "FAILED"),
    "infrastructureFailures": sum(1 for row in summary_rows if row["result"] == "INFRASTRUCTURE_FAILURE"),
    "missingFiles": sum(1 for row in summary_rows if row["httpStatus"] == "MISSING_FILE"),
    "failures": failures
}

summary_json_path.write_text(
    json.dumps(summary, indent=2, ensure_ascii=False),
    encoding="utf-8")

print()
print("Summary:")
print(json.dumps(summary, indent=2, ensure_ascii=False))
print()
print(f"CSV:  {summary_csv_path}")
print(f"JSON: {summary_json_path}")

if failures:
    raise SystemExit(1)
PY
