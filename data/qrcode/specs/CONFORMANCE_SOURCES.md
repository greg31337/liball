# Conformance source control

Audit date: 2026-09-16.

This document controls which references may justify implementation constants
and conformance claims. It deliberately records links and metadata for
copyrighted standards rather than checking in unlicensed copies.

## Authority order

When sources disagree, use this order:

1. The named edition of the licensed ISO/IEC or AIM publication, including any
   published amendment or corrigendum for that edition.
2. A formally identical national adoption of that same edition.
3. Public material from the registration authority or symbology owner, such as
   the current AIM ECI register or DENSO WAVE technical material.
4. GS1 specifications for GS1 application rules only.
5. Independent implementations and explanatory material as cross-checks only.

Items 3 through 5 are not substitutes for clauses or tables that are available
only in a licensed publication. A value is not described as ISO-conformant
until item 1 or 2 has been checked.

## Controlled editions

| Publication | Controlled edition | Official state at audit | Use in this project |
|---|---|---|---|
| ISO/IEC 18004 | 4th edition, 2024-08, 111 pages | Published; ISO moved it to stage 90.92 on 2026-02-25 and lists a replacement working draft | Model 2, Micro QR, and legacy compatibility requirements remain frozen to the published 2024 edition; unpublished draft work is not adopted |
| ISO/IEC 23941 | 1st edition, 2022-05, 80 pages | Published, stage 60.60 | rMQR symbology requirements |
| ISO/IEC 15424 | 3rd edition, 2025-02, 19 pages | Published, stage 60.60; replaces the 2008 edition | Symbology identifiers and modifier semantics |
| ISO/IEC 15415 | 3rd edition, 2024-12, 46 pages | Published, stage 60.60; replaces the 2011 edition | Two-dimensional symbol quality measurements and grades |
| AIM ECI Part 1 | Publication dated 2004-08 in AIM's current store listing | Current licensed store product; no public revision identifier is shown | ECI protocol |
| AIM ECI Part 3 | Version 2, released 2022-02-03 | Current licensed store product, item 805 | Registered ECI assignments and aliases |

The official ISO catalogue records are linked from `SOURCES.md`. Searches of
those records and the ISO catalogue on the audit date found no separately
published amendment or corrigendum for the four controlled editions. This is a
negative audit result, not a claim that no editorial issue can exist. Repeat
the audit before freezing a table and before a conformance release.

## Public counterparts and their limits

| Public source | Useful coverage | Limit |
|---|---|---|
| DENSO WAVE pages under `denso/` | Model 1/2 dimensions, headline capacities, ECC overview, Micro QR sizes/quiet zone, and rMQR overview | Explanatory summaries; they do not expose all normative tables or placement rules |
| JIS X 0510:2018 public rendering at <https://kikakurui.com/x0/X0510-2018-01.html> | A national adoption stated to preserve the technical content and structure of ISO/IEC 18004:2015 | It corresponds to the superseded 2015 ISO edition, not the controlled 2024 edition; use only for historical cross-checks |
| ISO catalogue previews | Edition, publication date, scope, status, predecessor/successor, and page count | Not the full normative text |
| GS1 General Specifications 26.0.0 and the GS1 retail guideline under `gs1/` | GS1 data syntax, FNC1/application behavior, and retail application rules | Not a QR symbology definition and not a substitute for ISO/IEC 15424 |
| RFC 3629 under `ietf/` | UTF-8 definition and validity constraints | Does not define QR ECI signaling |
| Unicode `SHIFTJIS.TXT` under `unicode/` | Historical mapping cross-check | Marked obsolete by Unicode; cannot by itself establish the QR Kanji repertoire |
| AIM catalogue snapshots under `aim/` | Product identity and provenance | They do not contain the licensed ECI protocol/register; the Part 3 snapshot is an expired public-review listing |
| Reference-library files under `libraries/` | Feature discovery and independent behavior comparisons | Never normative; implementation code is not copied from them |

No freely accessible source found in the audit is a complete, current,
formally equivalent replacement for ISO/IEC 18004:2024, ISO/IEC 23941:2022,
ISO/IEC 15424:2025, ISO/IEC 15415:2024, or the current AIM ECI Parts 1 and 3.
The public sources above are therefore retained as independently useful
cross-checks, while table extraction remains gated on licensed text.

## Requirement ownership

| Requirement or constant family | Primary authority | Public cross-check | Extraction task |
|---|---|---|---|
| Model 2 dimensions, capacities, count widths, remainder bits, block groups, BCH, masks, placement | ISO/IEC 18004:2024 | DENSO WAVE; independent libraries | T002 |
| Micro QR modes, dimensions, capacities, ECC, masks, format, placement, quiet zone | ISO/IEC 18004:2024 | DENSO WAVE; independent libraries | T003 |
| rMQR sizes, capacities, ECC, modes, masks, function patterns, format and placement | ISO/IEC 23941:2022 | DENSO WAVE; independent libraries | T004 |
| Legacy Model 1 recognition and decoding differences | ISO/IEC 18004:2024 Annex N and applicable historical source named there | DENSO WAVE | T005 |
| ECI protocol widths and validity | AIM ECI Part 1 plus profile restrictions in ISO/IEC 18004:2024 and ISO/IEC 23941:2022 | Existing implementations | T006 |
| ECI assignments and aliases | Current AIM ECI Part 3 register | IANA/encoding registries where applicable | T006 |
| Shift JIS and optional GB2312 mappings | QR profile requirements plus the exact character-set editions they cite | Unicode historical mapping; platform codecs | T007 |
| Symbology identifiers and modifiers | ISO/IEC 15424:2025 | AIM/device-vendor identifier tables | T101 |
| Quality metrics and grade thresholds | ISO/IEC 15415:2024 plus profile-specific quality clauses | Public ISO preview and verifier guidance | T114-T120 |

## Constant traceability gate

Every normative constant table added to `src/` must have a reviewed ledger row
with all of these fields before it can be called conformant:

| Constant/table key | Destination | Publication and edition | Clause/table/figure | Amendment/corrigendum applied | Extracted value or derivation | Independent cross-check | Reviewer/date |
|---|---|---|---|---|---|---|---|
| Model 2 version geometry/capacity | `Model2VersionTable` | ISO/IEC 18004:2024 | Table 1 | None published at 2026-09-16 audit | Versions 1-40 dimensions, total codewords, raw modules, remainder bits | ZXing, Nayuki, exhaustive fixture tests | Codex/2026-09-16 |
| Model 2 character-count widths | `Model2VersionTable` | ISO/IEC 18004:2024 | Table 3 | None published at 2026-09-16 audit | Four data modes across three version groups | Nayuki and boundary fixture tests | Codex/2026-09-16 |
| Model 2 ECC blocks | `EccBlockTable` | ISO/IEC 18004:2024 | 7.5.1, Table 9 | None published at 2026-09-16 audit | ECC width/block count plus derived short/long groups for 160 combinations | ZXing explicit groups, Nayuki compact tables, exhaustive capacity tests | Codex/2026-09-16 |
| Model 2 masks | `Model2MaskTable` | ISO/IEC 18004:2024 | 7.8.2, Table 10 | None published at 2026-09-16 audit | Eight row/column predicates | Nayuki and fixed reference grids | Codex/2026-09-16 |
| Model 2 format/version BCH | `FormatCodeTable`, `VersionCodeTable` | ISO/IEC 18004:2024 | 7.9, 7.10, Annexes C-D | None published at 2026-09-16 audit | 32 masked format words and 34 version words | ZXing tables, Nayuki derivation, independent polynomial tests | Codex/2026-09-16 |
| Model 2 alignment and placement | `AlignmentPositionTable`, `Model2PlacementTable` | ISO/IEC 18004:2024 | 7.7 and Annex E | None published at 2026-09-16 audit | All alignment centres and coordinate/traversal rules | ZXing function maps, Nayuki traversal, exhaustive/fixed-coordinate tests | Codex/2026-09-16 |
| Micro QR geometry, modes, and capacities | `MicroVersionTable` | ISO/IEC 18004:2024 | 5.1, 5.3, Tables 2, 3, and 7 | None published at 2026-09-16 audit | M1-M4 dimensions, legal modes, header widths, data bits, symbol characters, and input capacities | DENSO WAVE, Segno, libqrencode, exhaustive configuration tests | Codex/2026-09-16 |
| Micro QR ECC configurations | `MicroVersionTable` | ISO/IEC 18004:2024 | 7.5.1, Tables 7 and 9 | None published at 2026-09-16 audit | Eight symbol configurations, each with one data/ECC block, including M1 error detection only | Segno, libqrencode, reconstruction tests | Codex/2026-09-16 |
| Micro QR masks | `MicroMaskTable` | ISO/IEC 18004:2024 | 7.8.2, Table 10 | None published at 2026-09-16 audit | Four row/column predicates | Segno, libqrencode, fixed reference grids | Codex/2026-09-16 |
| Micro QR format BCH | `MicroFormatCodeTable` | ISO/IEC 18004:2024 | 7.9.2, Table 13, Annex C | None published at 2026-09-16 audit | Eight symbol numbers by four masks; BCH(15,5) words XORed with `100010001000101` | Segno, libqrencode, independent polynomial tests | Codex/2026-09-16 |
| Micro QR function and data placement | `MicroPlacementTable` | ISO/IEC 18004:2024 | 5.3, 7.7, and 7.9.2 | None published at 2026-09-16 audit | Finder/separator/timing/format coordinates, zig-zag direction, and two-module quiet zone | DENSO WAVE, Segno, libqrencode, fixed-coordinate tests | Codex/2026-09-16 |
| rMQR geometry and codeword capacity | `RectangularMicroVersionTable` | ISO/IEC 23941:2022 | 6.3.2, 6.3.10, Table 1 | None published at 2026-09-16 audit | All 32 dimensions, function/format modules, total codewords, remainder bits, and two-module quiet zone | DENSO WAVE, Segno, Zint, exhaustive area tests | Codex/2026-09-16 |
| rMQR modes and input capacity | `RectangularMicroVersionTable` | ISO/IEC 23941:2022 | 7.3-7.4, Tables 2, 3, and 6 | None published at 2026-09-16 audit | Mode indicators, character-count widths, data codewords, and maximum capacities for four data modes | Zint and independent bit-length calculations; Segno Kanji-indicator typo rejected | Codex/2026-09-16 |
| rMQR ECC configurations | `RectangularMicroVersionTable` | ISO/IEC 23941:2022 | 7.5.1, Table 8 | None published at 2026-09-16 audit | M/H block counts and derived equal-width ECC and short/long data groups for 64 configurations | Zint and exhaustive reconstruction tests | Codex/2026-09-16 |
| rMQR mask | `RectangularMicroMaskTable` | ISO/IEC 23941:2022 | 7.8.2 | None published at 2026-09-16 audit | Single fixed row/column predicate | Zint and fixed reference grid | Codex/2026-09-16 |
| rMQR format BCH | `RectangularMicroFormatCodeTable` | ISO/IEC 23941:2022 | 7.9 and Annex C | None published at 2026-09-16 audit | Finder/subfinder copies for 32 versions and M/H, using distinct XOR masks | Segno, Zint, independent polynomial tests | Codex/2026-09-16 |
| rMQR function and data placement | `RectangularMicroAlignmentTable`, `RectangularMicroPlacementTable` | ISO/IEC 23941:2022 | 6.3.3-6.3.10, 7.7, Annex D | None published at 2026-09-16 audit | Finder, separator, timing, alignment, subfinder, corner, format, and zig-zag coordinate rules | Segno, Zint, fixed-coordinate tests | Codex/2026-09-16 |
| Model 1 geometry and decode capacities | `LegacyModel1Table` | ISO/IEC 18004:2024 and ISO/IEC 18004:2000 | 2024 Annex N; 2000 Tables M.1-M.5 | None published at 2026-09-16 audit | Versions 1-14, module accounting, data/ECC/remainder counts, equal-sized block layouts, and count widths | DENSO WAVE, JIS X 0510:2018 Annex N, exhaustive reconstruction tests | Codex/2026-09-16 |
| Model 1 format and recognition | `LegacyModel1Table` | ISO/IEC 18004:2024 and ISO/IEC 18004:2000 | 2024 Annex N; 2000 8.9 and Annex M | None published at 2026-09-16 audit | BCH(15,5) format words XORed with `010100000100101`, distinct from Model 2 | Independent polynomial tests and all 32 Model 2 comparisons | Codex/2026-09-16 |
| Model 1 function and symbol-character placement | `LegacyModel1Table` | ISO/IEC 18004:2024 and ISO/IEC 18004:2000 | 2024 Annex N; 2000 M.3 and M.7, Figures M.4-M.7 | None published at 2026-09-16 audit | Extension patterns including the permitted early variant, no version/alignment fields, format coordinates, and four/eight-bit symbol-character layouts | JIS X 0510:2018 Annex N and fixed-coordinate tests | Codex/2026-09-16 |
| ECI registered assignments and aliases | `EciRegistry` | AIM ECI Part 3, Version 2 | Interpretative and transformation ECI register | None published at 2026-09-16 audit | Registered character sets, binary, GZIP, application, prefix, and null entries with canonical/common aliases | IANA character-set registry, ZXing, exhaustive lookup tests | Codex/2026-09-16 |
| ECI protocol and QR designator ranges | `EciRegistry` | AIM ECI Part 1; ISO/IEC 18004:2024 | ECI assignment classes; QR ECI designator table | None published at 2026-09-16 audit | AIM range through 811799, private/invalid/control regions, and QR 1/2/3-byte boundaries through 999999 | ISO/IEC 18004 historical public rendering and boundary tests | Codex/2026-09-16 |
| ECI built-in conversion paths | `EciRegistry` | AIM ECI Part 3, Version 2 | Character Set ECIs | None published at 2026-09-16 audit | Strict base-.NET paths for Latin-1, UTF-8, ASCII, UTF-16BE/LE, and UTF-32BE/LE; other entries retained as protocol data | IANA aliases and byte-order fixtures | Codex/2026-09-16 |

T002 through T006 established the implementation-constant rows above. Task
T007 must add rows as it introduces further tables. A missing citation,
conflict between sources, or unexplained correction keeps the affected row and
task open.

## Audit procedure

Before extracting or changing a normative value:

1. Verify the acquired artifacts with `specs/SHA256SUMS`.
2. Recheck the publisher catalogue for edition status, amendments, corrigenda,
   and replacements.
3. Record the exact licensed clause/table/figure and any correction in the
   ledger; do not cite a search result or public summary as the authority.
4. Independently derive or cross-check the value using a public counterpart or
   a separately maintained implementation.
5. Record disagreements explicitly and resolve them in favor of the authority
   order above before merging the constant.

Draft replacements do not silently change behavior. Adopting a newly published
edition requires an explicit compatibility review and new ledger entries.
