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


invoices = {
    "invoice-001.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 001 Ltd",
        "Vendor Tax ID: 516000001",
        "Invoice Number: SYN-0001",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 1000.00",
        "VAT: 180.00",
        "Total: 1180.00",
    ],
    "invoice-002.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 002 Ltd",
        "Vendor Tax ID: 516000002",
        "Invoice Number: SYN-0002",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 250.00",
        "VAT: 45.00",
        "Total: 295.00",
    ],
    "invoice-003.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 003 Ltd",
        "Vendor Tax ID: 516000003",
        "Invoice Number: SYN-0003",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 1000.00",
        "VAT: 170.00",
        "Total: 1180.00",
        "Note: this invoice intentionally contains a total mismatch.",
    ],
    "invoice-004.pdf": [
        "INVOICE",
        "Invoice Number: SYN-0004",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 500.00",
        "VAT: 90.00",
        "Total: 590.00",
        "Note: this invoice intentionally has no vendor name.",
    ],
    "invoice-005.pdf": [
        "INVOICE",
        "Vendor: Synthetic Vendor 005 Ltd",
        "Vendor Tax ID: 516000005",
        "Invoice Number: SYN-0005",
        "Invoice Date: 2026-05-01",
        "Currency: ILS",
        "Subtotal: 700.00",
        "VAT: 126.00",
        "Note: this invoice intentionally has no total amount.",
    ],
}

for file_name, lines in invoices.items():
    path = OUTPUT_DIR / file_name
    create_simple_pdf(path, lines)
    print(f"Created {path}")
