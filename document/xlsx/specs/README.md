# XLSX implementation specifications

Large reference files are available through [Reference downloads](../REFERENCE-DOWNLOADS.md). The source inventory below also records historical acquisitions.

Retrieved on 2026-08-25. These local copies cover the normative OOXML package and SpreadsheetML formats, Microsoft Excel extensions, ZIP storage, XML processing, relationship URI resolution, timestamps, and package signatures/encryption needed by the feature inventory in `todo.md`.

## ECMA-376

ECMA-376 Part 1, 5th edition, is download-only. The `ecma-376/` directory retains the other three parts plus the official Strict and Transitional XSD/RELAX NG schema archives, OPC schemas, preset DrawingML geometries, and SpreadsheetML style resources. They were downloaded from the [ECMA-376 publication page](https://ecma-international.org/publications-and-standards/standards/ecma-376/).

## Microsoft Open Specifications

The `microsoft/` directory contains current published PDFs available on the retrieval date:

- `MS-XLSX.pdf`: Excel extensions to SpreadsheetML.
- `MS-OE376.pdf`: Microsoft Office implementation notes for ECMA-376.
- `MS-OEXTXML.pdf`: shared Office OOXML extension-list structures.
- `MS-ODRAWXML.pdf`: DrawingML extensions used by spreadsheet drawings and charts.
- `MS-OFFMACRO.pdf` and `MS-OFFMACRO2.pdf`: macro-enabled OOXML package structures.
- `MS-OVBA.pdf`: VBA project binary storage.
- `MS-OFFCRYPTO.pdf`: Office document encryption and signing structures.
- `MS-CFB.pdf`: Compound File Binary storage used by encrypted OOXML and VBA/OLE payloads.

The documents were downloaded from the [Microsoft Open Specifications library](https://learn.microsoft.com/en-us/openspecs/).

## Supporting standards

- `zip/PKWARE-APPNOTE.txt`: PKWARE ZIP File Format Specification 6.3.10.
- `web-standards/RFC3986.txt`: URI generic syntax used to resolve OPC relationship targets.
- `web-standards/RFC3339.txt`: interoperable timestamp syntax used by package metadata.
- `web-standards/XML-1.0-Fifth-Edition.html`: XML 1.0.
- `web-standards/Namespaces-in-XML-1.0-Third-Edition.html`: XML namespaces.
- `web-standards/XML-Signature-1.1.html`: XML signature syntax and processing.
- `web-standards/Canonical-XML-1.1.html`: canonical XML used by XML signatures.
