#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${INVOICEFLOW_BASE_URL:?Set INVOICEFLOW_BASE_URL, for example http://localhost:5030}"
API_KEY="${INVOICEFLOW_API_KEY:?Set INVOICEFLOW_API_KEY}"
INVOICE_FILE="${INVOICEFLOW_INVOICE_FILE:?Set INVOICEFLOW_INVOICE_FILE}"

RESOURCE_PATH="/api/invoices/process"
EXPECTED_RATE_LIMIT_STATUS="429"
EXPECTED_RATE_LIMIT_CODE="RATE_LIMIT_EXCEEDED"
EXPECTED_RATE_LIMIT_REASON="Too Many Requests"

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required for manual API verification." >&2
  exit 1
fi

if [[ ! -f "$INVOICE_FILE" ]]; then
  echo "Invoice file was not found: $INVOICE_FILE" >&2
  exit 1
fi

RUN_ID="$(date -u +%Y%m%d-%H%M%S)"
EVIDENCE_DIR="${INVOICEFLOW_RATE_LIMIT_EVIDENCE_DIR:-docs/evidence/rate-limiting/${RUN_ID}-local-rate-limit}"

mkdir -p "$EVIDENCE_DIR"

REQUEST_01_RESPONSE="$EVIDENCE_DIR/request-01-response.txt"
REQUEST_02_RESPONSE="$EVIDENCE_DIR/request-02-response.txt"
SUMMARY_JSON="$EVIDENCE_DIR/summary.json"
README_FILE="$EVIDENCE_DIR/README.md"

ENDPOINT="${BASE_URL%/}${RESOURCE_PATH}"

send_invoice_request() {
  local response_file="$1"

  curl \
    --silent \
    --show-error \
    --write-out "%{http_code}" \
    --output "$response_file" \
    --request POST "$ENDPOINT" \
    --header "X-API-Key: ${API_KEY}" \
    -F "file=@${INVOICE_FILE};type=application/pdf"
}

echo "Running InvoiceFlow manual rate limit verification..."
echo "Endpoint: $ENDPOINT"
echo "Evidence directory: $EVIDENCE_DIR"

FIRST_STATUS="$(send_invoice_request "$REQUEST_01_RESPONSE")"
SECOND_STATUS="$(send_invoice_request "$REQUEST_02_RESPONSE")"

FIRST_PASSED=false
SECOND_PASSED=false
RATE_LIMIT_CODE_FOUND=false

if [[ "$FIRST_STATUS" == "200" ]]; then
  FIRST_PASSED=true
fi

if [[ "$SECOND_STATUS" == "$EXPECTED_RATE_LIMIT_STATUS" ]]; then
  SECOND_PASSED=true
fi

if grep -q "$EXPECTED_RATE_LIMIT_CODE" "$REQUEST_02_RESPONSE"; then
  RATE_LIMIT_CODE_FOUND=true
fi

cat > "$SUMMARY_JSON" <<JSON
{
  "endpoint": "$RESOURCE_PATH",
  "firstRequest": {
    "statusCode": $FIRST_STATUS,
    "expectedStatusCode": 200,
    "passed": $FIRST_PASSED
  },
  "secondRequest": {
    "statusCode": $SECOND_STATUS,
    "expectedStatusCode": 429,
    "expectedReason": "$EXPECTED_RATE_LIMIT_REASON",
    "expectedCode": "$EXPECTED_RATE_LIMIT_CODE",
    "passed": $SECOND_PASSED,
    "rateLimitCodeFound": $RATE_LIMIT_CODE_FOUND
  }
}
JSON

cat > "$README_FILE" <<MD
# InvoiceFlow Rate Limiting Manual Verification Evidence

## Run Summary

Run type: Local manual rate limit verification  
Endpoint: \`POST $RESOURCE_PATH\`  
Evidence directory: \`$EVIDENCE_DIR\`

## Preconditions

This verification expects the API to be running with:

- API key identity configured
- \`InvoiceFlow:ClientRateLimiting\` configured
- a low per-client permit limit, for example \`PermitLimit=1\`
- a valid API key supplied through \`INVOICEFLOW_API_KEY\`

## Expected Behavior

1. First request with a valid \`X-API-Key\` is accepted.
2. Second request with the same \`X-API-Key\` exceeds the configured client rate limit.
3. The API returns \`429 Too Many Requests\`.
4. The response body contains \`RATE_LIMIT_EXCEEDED\`.

## Results

First request status: \`$FIRST_STATUS\`  
Second request status: \`$SECOND_STATUS\`  
Expected rate limit code found: \`$RATE_LIMIT_CODE_FOUND\`

## Artifacts

- \`request-01-response.txt\`
- \`request-02-response.txt\`
- \`summary.json\`

## Scope

This is local manual verification evidence.

This is not a load test, stress test, distributed rate limiter test, or production readiness test.
MD

if [[ "$FIRST_PASSED" != true ]]; then
  echo "Expected first request to return 200, but got $FIRST_STATUS." >&2
  echo "See: $REQUEST_01_RESPONSE" >&2
  exit 1
fi

if [[ "$SECOND_PASSED" != true ]]; then
  echo "Expected second request to return 429 Too Many Requests, but got $SECOND_STATUS." >&2
  echo "See: $REQUEST_02_RESPONSE" >&2
  exit 1
fi

if [[ "$RATE_LIMIT_CODE_FOUND" != true ]]; then
  echo "Expected second response body to contain RATE_LIMIT_EXCEEDED." >&2
  echo "See: $REQUEST_02_RESPONSE" >&2
  exit 1
fi

echo "Manual rate limit verification passed."
echo "Evidence written to: $EVIDENCE_DIR"
