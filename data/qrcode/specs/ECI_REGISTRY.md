# ECI registry extraction

Extracted and cross-checked 2026-09-16 against AIM ECI Part 1 and AIM ECI
Part 3, Version 2. The controlled editions and source limitations are recorded
in `CONFORMANCE_SOURCES.md`.

## Implemented material

`EciRegistry` records the registered character-set assignments 000000 through
000035 (excluding unassigned 000014 and 000019), ISO/IEC 646 invariant
assignment 000170, binary assignment 000899, GZIP transformation 001800,
generic application launcher 800000, and the transformation/null/
interpretative protocol controls.

Each entry has one canonical name and the registry accepts common standardized
or platform aliases case-insensitively. Ambiguous legacy pairs resolve to the
non-GLI assignment: CP437 resolves to 000002 and ISO-8859-1 resolves to
000003, while GLI0 and GLI1 remain explicitly addressable.

The base .NET runtime supplies strict conversion paths without an external
code-page provider for these assignments:

| Assignments | Encoding |
|---|---|
| 000001, 000003 | ISO-8859-1 |
| 000025 | UTF-16BE |
| 000026 | UTF-8 |
| 000027 | US-ASCII |
| 000033 | UTF-16LE |
| 000034 | UTF-32BE |
| 000035 | UTF-32LE |

Other character sets remain registered but deliberately expose no built-in
.NET conversion path. This preserves their assignment and raw bytes until a
dedicated mapping is available. In particular, Shift JIS and GB2312 are owned
by T007 rather than by a platform-dependent code-page provider. Binary,
transformation, application, and prefix/null entries are protocol-only and
are never presented as text encodings.

## Assignment and QR designator ranges

The AIM protocol assignment space ends at 811799. It includes standardized
and private-use transformation/application ranges plus the defined prefix and
null controls. Values 799997 and 799998 are invalid, while 811800 through
999999 are outside the AIM protocol assignment space.

QR's ECI designator can nevertheless carry the full six-digit range. The
shortest representation is selected at these exact boundaries:

| Assignment value | Designator bytes |
|---|---:|
| 0-127 | 1 |
| 128-16383 | 2 |
| 16384-999999 | 3 |

Keeping QR encodability separate from AIM registration allows a later decoder
to preserve unknown or unregistered designators without treating them as
known text encodings.

## Independent checks

- ISO/IEC 18004's ECI designator table independently confirms the QR byte
  boundaries and six-digit upper limit.
- IANA's character-set registry confirms preferred names and common aliases
  for the standardized character encodings.
- ZXing's character-set ECI table independently confirms the commonly used
  assignment-to-encoding pairs and duplicate legacy assignments.
- `EciRegistryTests` exhaustively checks the registered assignment order,
  alias resolution, entry classifications, strict built-in conversions,
  protocol ranges, and every designator boundary.

The saved research artifacts were verified against `SHA256SUMS` before
extraction. The current AIM register remains a licensed source; the saved AIM
catalogue pages establish product/version provenance but are not substitutes
for its normative tables.
