# Model 2 table extraction

Extracted and cross-checked 2026-09-16 against ISO/IEC 18004:2024. The
controlled edition and correction policy are recorded in
`CONFORMANCE_SOURCES.md`.

## Implemented material

| Standard material | Implementation |
|---|---|
| Version 1-40 dimensions, total codewords, data-module capacity, and remainder bits (Table 1) | `Model2VersionTable` |
| Numeric, alphanumeric, byte, and Kanji character-count widths for version groups 1-9, 10-26, and 27-40 (Table 3) | `Model2VersionTable` |
| ECC codewords per block and block counts for every version and L/M/Q/H level (Table 9) | `EccBlockTable` |
| Eight data-mask predicates (7.8.2, Table 10) | `Model2MaskTable` |
| Format BCH(15,5) values and the fixed XOR mask (7.9 and Annex C) | `FormatCodeTable` |
| Version BCH(18,6) values for versions 7-40 (7.10 and Annex D) | `VersionCodeTable` |
| Alignment-pattern centre coordinates (Annex E, Table E.1) | `AlignmentPositionTable` |
| Two-column zig-zag order, timing-column skip, format copies, version copies, and fixed dark module (7.7, 7.9, and 7.10) | `Model2PlacementTable` |

ECC groups are stored without redundancy. Table 9 supplies the total symbol
codewords, number of blocks, and ECC codewords per block. Those values uniquely
produce the short and long groups: long-block count is the total codeword count
modulo the block count; each long block has one more data codeword than a short
block. The table API exposes both groups explicitly.

## Independent checks

The extracted values were checked in three independent forms:

- ZXing's pinned `Version.java` expresses each ECC group as explicit block
  count/data-size pairs and contains the 34 version-information words.
- Nayuki's pinned `QrCode.java` expresses the same ECC data as two compact
  4-by-40 tables and independently derives raw module capacity, alignment
  positions, BCH words, masks, and zig-zag placement.
- `Model2TablesTests` uses separately transcribed total/data capacities and
  alignment coordinates, reconstructs every short/long block group, calculates
  both BCH families from their generator polynomials, and checks masks and
  placement against fixed coordinate grids.

The pinned reference revisions and their licenses are listed in `SOURCES.md`.
No separate amendment or corrigendum for ISO/IEC 18004:2024 was published in
the catalogue audit performed for T001; the check must be repeated before a
conformance release.
