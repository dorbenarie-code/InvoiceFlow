from pathlib import Path
import json

OUTPUT_DIR = Path("tests/live-documents/synthetic-invoices")
EXPECTED_RESULTS_PATH = Path("tests/live-documents/expected-results.json")

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

for old_pdf in OUTPUT_DIR.glob("invoice-*.pdf"):
    old_pdf.unlink()


def escape_pdf_text(value: str) -> str:
    return value.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")


def create_simple_pdf(path: Path, lines: list[str]) -> None:
    content_lines = [
        "BT",
        "/F1 12 Tf",
        "72 760 Td",
        "16 TL",
    ]

    for line in lines:
        content_lines.append(f"({escape_pdf_text(line)}) Tj")
        content_lines.append("T*")

    content_lines.append("ET")

    content = "\n".join(content_lines).encode("utf-8")

    objects: list[bytes] = []

    objects.append(b"<< /Type /Catalog /Pages 2 0 R >>")
    objects.append(b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>")

    objects.append(
        b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
        b"/Resources << /Font << /F1 4 0 R >> >> "
        b"/Contents 5 0 R >>"
    )

    objects.append(
        b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
    )

    objects.append(
        b"<< /Length " + str(len(content)).encode("ascii") + b" >>\n"
        b"stream\n" + content + b"\nendstream"
    )

    pdf = bytearray()
    pdf.extend(b"%PDF-1.7\n")

    offsets = [0]

    for index, obj in enumerate(objects, start=1):
        offsets.append(len(pdf))
        pdf.extend(f"{index} 0 obj\n".encode("ascii"))
        pdf.extend(obj)
        pdf.extend(b"\nendobj\n")

    xref_offset = len(pdf)

    pdf.extend(f"xref\n0 {len(objects) + 1}\n".encode("ascii"))
    pdf.extend(b"0000000000 65535 f \n")

    for offset in offsets[1:]:
        pdf.extend(f"{offset:010d} 00000 n \n".encode("ascii"))

    pdf.extend(
        f"trailer\n<< /Size {len(objects) + 1} /Root 1 0 R >>\n"
        f"startxref\n{xref_offset}\n%%EOF\n".encode("ascii")
    )

    path.write_bytes(pdf)


def invoice_file_name(index: int) -> str:
    return f"invoice-{index:03d}.pdf"


def invoice_number(index: int) -> str:
    return f"SYN-{index:04d}"


def vendor_name(index: int) -> str:
    return f"Synthetic Vendor {index:03d} Ltd"


def tax_id(index: int) -> str:
    return f"516000{index:03d}"


def verified_lines(index: int, subtotal: int, vat: int) -> list[str]:
    total = subtotal + vat

    return [
        "INVOICE",
        f"Vendor: {vendor_name(index)}",
        f"Vendor Tax ID: {tax_id(index)}",
        f"Invoice Number: {invoice_number(index)}",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        f"Subtotal: {subtotal}.00",
        f"VAT: {vat}.00",
        f"Total: {total}.00",
    ]


def total_mismatch_lines(index: int, subtotal: int, vat: int, wrong_total: int) -> list[str]:
    return [
        "INVOICE",
        f"Vendor: {vendor_name(index)}",
        f"Vendor Tax ID: {tax_id(index)}",
        f"Invoice Number: {invoice_number(index)}",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        f"Subtotal: {subtotal}.00",
        f"VAT: {vat}.00",
        f"Total: {wrong_total}.00",
    ]


def missing_vendor_lines(index: int, subtotal: int, vat: int) -> list[str]:
    total = subtotal + vat

    return [
        "INVOICE",
        f"Vendor Tax ID: {tax_id(index)}",
        f"Invoice Number: {invoice_number(index)}",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        f"Subtotal: {subtotal}.00",
        f"VAT: {vat}.00",
        f"Total: {total}.00",
    ]


def missing_total_lines(index: int, subtotal: int, vat: int) -> list[str]:
    return [
        "INVOICE",
        f"Vendor: {vendor_name(index)}",
        f"Vendor Tax ID: {tax_id(index)}",
        f"Invoice Number: {invoice_number(index)}",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        f"Subtotal: {subtotal}.00",
        f"VAT: {vat}.00",
    ]


cases = []

# 30 clean verified invoices.
for index in range(1, 31):
    subtotal = 100 + index * 10
    vat = 18 + index * 2

    cases.append({
        "index": index,
        "lines": verified_lines(index, subtotal, vat),
        "expected": {
            "fileName": invoice_file_name(index),
            "expectedStatus": "Verified",
            "expectedInvoiceNumber": invoice_number(index),
            "expectedVendorName": vendor_name(index),
        }
    })

# 10 invoices with deterministic total mismatch.
for index in range(31, 41):
    subtotal = 500 + index * 5
    vat = 90 + index
    wrong_total = subtotal + vat + 7

    cases.append({
        "index": index,
        "lines": total_mismatch_lines(index, subtotal, vat, wrong_total),
        "expected": {
            "fileName": invoice_file_name(index),
            "expectedStatus": "RequiresHumanReview",
            "expectedInvoiceNumber": invoice_number(index),
            "expectedIssueCode": "TOTAL_MISMATCH",
        }
    })

# 5 invoices missing vendor name.
for index in range(41, 46):
    subtotal = 300 + index * 4
    vat = 54 + index

    cases.append({
        "index": index,
        "lines": missing_vendor_lines(index, subtotal, vat),
        "expected": {
            "fileName": invoice_file_name(index),
            "expectedStatus": "RequiresHumanReview",
            "expectedInvoiceNumber": invoice_number(index),
            "expectedIssueCode": "MISSING_VENDOR",
        }
    })

# 5 invoices missing total amount.
for index in range(46, 51):
    subtotal = 200 + index * 3
    vat = 36 + index

    cases.append({
        "index": index,
        "lines": missing_total_lines(index, subtotal, vat),
        "expected": {
            "fileName": invoice_file_name(index),
            "expectedStatus": "RequiresHumanReview",
            "expectedInvoiceNumber": invoice_number(index),
            "expectedIssueCode": "MISSING_TOTAL_AMOUNT",
        }
    })


expected_results = []

for case in cases:
    file_name = invoice_file_name(case["index"])
    path = OUTPUT_DIR / file_name

    create_simple_pdf(path, case["lines"])
    expected_results.append(case["expected"])

    print(f"Created {path}")

EXPECTED_RESULTS_PATH.write_text(
    json.dumps(expected_results, indent=2, ensure_ascii=False) + "\n",
    encoding="utf-8"
)

print()
print(f"Created {EXPECTED_RESULTS_PATH}")
print(f"Total documents: {len(cases)}")
print("Expected distribution:")
print("Verified:", sum(1 for item in expected_results if item["expectedStatus"] == "Verified"))
print("RequiresHumanReview:", sum(1 for item in expected_results if item["expectedStatus"] == "RequiresHumanReview"))
