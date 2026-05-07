from pathlib import Path

OUTPUT_DIR = Path("tests/live-documents/synthetic-invoices")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


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

    objects.append(
        b"<< /Type /Catalog /Pages 2 0 R >>"
    )

    objects.append(
        b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>"
    )

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


def verified_invoice(index: int, subtotal: int, vat: int) -> list[str]:
    total = subtotal + vat

    return [
        "INVOICE",
        f"Vendor: Synthetic Vendor {index:03d} Ltd",
        f"Vendor Tax ID: 516000{index:03d}",
        f"Invoice Number: SYN-{index:04d}",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        f"Subtotal: {subtotal}.00",
        f"VAT: {vat}.00",
        f"Total: {total}.00",
    ]


invoices = {
    "invoice-001.pdf": verified_invoice(1, 1000, 180),
    "invoice-002.pdf": verified_invoice(2, 250, 45),
    "invoice-003.pdf": verified_invoice(3, 500, 90),
    "invoice-004.pdf": verified_invoice(4, 1200, 216),
    "invoice-005.pdf": verified_invoice(5, 700, 126),
    "invoice-006.pdf": verified_invoice(6, 300, 54),
    "invoice-007.pdf": verified_invoice(7, 1500, 270),
    "invoice-008.pdf": verified_invoice(8, 80, 14),

    "invoice-009.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 009 Ltd",
        "Vendor Tax ID: 516000009",
        "Invoice Number: SYN-0009",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 1000.00",
        "VAT: 170.00",
        "Total: 1180.00",
        "Note: this invoice intentionally contains a total mismatch.",
    ],

    "invoice-010.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 010 Ltd",
        "Vendor Tax ID: 516000010",
        "Invoice Number: SYN-0010",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 400.00",
        "VAT: 60.00",
        "Total: 472.00",
        "Note: this invoice intentionally contains a total mismatch.",
    ],

    "invoice-011.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 011 Ltd",
        "Vendor Tax ID: 516000011",
        "Invoice Number: SYN-0011",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 900.00",
        "VAT: 162.00",
        "Total: 1000.00",
        "Note: this invoice intentionally contains a total mismatch.",
    ],

    "invoice-012.pdf": [
        "INVOICE",
        "Vendor Tax ID: 516000012",
        "Invoice Number: SYN-0012",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 500.00",
        "VAT: 90.00",
        "Total: 590.00",
        "Note: this invoice intentionally has no vendor name.",
    ],

    "invoice-013.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 013 Ltd",
        "Vendor Tax ID: 516000013",
        "Invoice Number: SYN-0013",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 700.00",
        "VAT: 126.00",
        "Note: this invoice intentionally has no total amount.",
    ],

    "invoice-014.pdf": [
        "TAX INVOICE",
        "Vendor: Synthetic Vendor 014 Ltd",
        "Tax ID: 516000014",
        "Invoice Number: SYN-0014",
        "Date: 01/05/2026",
        "Currency: ILS",
        "Subtotal Amount: 1100.00",
        "VAT Amount: 198.00",
        "Total Amount Due: 1298.00",
    ],

    "invoice-015.pdf": [
        "RECEIPT / INVOICE",
        "Supplier: Synthetic Vendor 015 Ltd",
        "Supplier Tax ID: 516000015",
        "Invoice No: SYN-0015",
        "Issue Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 50.00",
        "VAT: 9.00",
        "Grand Total: 59.00",
    ],
}

for file_name, lines in invoices.items():
    path = OUTPUT_DIR / file_name
    create_simple_pdf(path, lines)
    print(f"Created {path}")
