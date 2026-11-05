# Offline research sources

Retrieved on 2026-08-27. RFC text files and registry XML are authoritative snapshots; library pages and READMEs are comparison material only.

## Core and extension specifications

- `rfcs/rfc8949.txt` — CBOR (STD 94): https://www.rfc-editor.org/rfc/rfc8949.txt
- `rfcs/rfc8610.txt` — CDDL: https://www.rfc-editor.org/rfc/rfc8610.txt
- `rfcs/rfc8742.txt` — CBOR Sequences: https://www.rfc-editor.org/rfc/rfc8742.txt
- `rfcs/rfc9165.txt` — additional CDDL control operators: https://www.rfc-editor.org/rfc/rfc9165.txt
- `rfcs/rfc9682.txt` — corrected and extended CDDL grammar: https://www.rfc-editor.org/rfc/rfc9682.txt
- `rfcs/rfc9741.txt` — CDDL text conversion and processing controls: https://www.rfc-editor.org/rfc/rfc9741.txt

## Normative supporting material

- `rfcs/rfc3339.txt` — date/time syntax used by CBOR tag 0 and CDDL `tdate`: https://www.rfc-editor.org/rfc/rfc3339.txt
- `rfcs/rfc3629.txt` — UTF-8 validity: https://www.rfc-editor.org/rfc/rfc3629.txt
- `rfcs/rfc4648.txt` — base16, base32, base64, and base64url encodings: https://www.rfc-editor.org/rfc/rfc4648.txt
- `rfcs/rfc5234.txt` and `rfcs/rfc7405.txt` — ABNF and case-sensitive ABNF: https://www.rfc-editor.org/rfc/rfc5234.txt and https://www.rfc-editor.org/rfc/rfc7405.txt
- `rfcs/rfc8259.txt` — JSON and CBOR/JSON conversion: https://www.rfc-editor.org/rfc/rfc8259.txt
- `rfcs/rfc9285.txt` — Base45 used by the CDDL `.b45` control: https://www.rfc-editor.org/rfc/rfc9285.txt
- `rfcs/xml-schema-part2-datatypes.html` — XML Schema regular-expression semantics referenced by CDDL `.regexp`: https://www.w3.org/TR/2004/REC-xmlschema-2-20041028/

## Live registries and interoperability vectors

- `registries/iana-cbor-tags.xml` — CBOR tags, simple values, and related registries: https://www.iana.org/assignments/cbor-tags/cbor-tags.xml
- `registries/iana-cddl.xml` — CDDL control-operator registry: https://www.iana.org/assignments/cddl/cddl.xml
- `test-vectors/rfc8949-appendix-a.json` and `test-vectors/README-upstream.md` — machine-readable RFC 8949 examples: https://github.com/cbor/test-vectors

## Existing-library feature surveys

- `library-notes/libcbor-README.md` and `library-notes/libcbor-overview.html` — https://github.com/PJK/libcbor and https://libcbor.readthedocs.io/en/latest/
- `library-notes/TinyCBOR-overview.html` — https://intel.github.io/tinycbor/current/
- `library-notes/cbor2-README.rst` and `library-notes/cbor2-api.html` — https://github.com/agronholm/cbor2 and https://cbor2.readthedocs.io/en/stable/api.html
- `library-notes/fxamacker-cbor-README.md` — https://github.com/fxamacker/cbor
- `library-notes/cddl-cat-README.md` — https://github.com/ericseppanen/cddl-cat
- `library-notes/anweiss-cddl-README.md` — an additional feature-complete CDDL comparison: https://github.com/anweiss/cddl

The plan in `../todo.md` uses the standards as requirements. Existing libraries are used only to identify practical API layers, safety controls, extension points, and commonly expected interoperability behavior.
