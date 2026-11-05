# rMQR table extraction

Extracted and cross-checked 2026-09-16 against ISO/IEC 23941:2022. The
controlled edition and correction policy are recorded in
`CONFORMANCE_SOURCES.md`.

## Implemented material

| Standard material | Implementation |
|---|---|
| All 32 version indicators, dimensions, total codewords, function/format modules, and remainder bits (Table 1) | `RectangularMicroVersionTable` |
| Three-bit mode indicators and all version-specific character-count widths (Tables 2 and 3) | `RectangularMicroVersionTable` |
| M/H data codewords and maximum numeric, alphanumeric, byte, and Kanji capacities (Table 6) | `RectangularMicroVersionTable` |
| M/H block counts, ECC widths, and short/long data-block groups (Table 8) | `RectangularMicroVersionTable` |
| The single fixed data-mask predicate (7.8.2) | `RectangularMicroMaskTable` |
| Both sets of 64 masked BCH(18,6) format words (7.9 and Annex C) | `RectangularMicroFormatCodeTable` |
| Alignment-pattern centre columns for every permitted width (Annex D) | `RectangularMicroAlignmentTable` |
| Finder, separator, timing, alignment, subfinder, corner, format, data traversal, and two-module quiet-zone geometry (6.3 and 7.7) | `RectangularMicroPlacementTable` |

The maximum input capacities are stored explicitly but are also independently
reconstructible from the data-bit capacity, the three-bit mode indicator, each
version's character-count width, and the mode's payload packing rule. ECC block
groups are stored without redundancy: the total and data codeword counts plus
the block count uniquely determine equal ECC widths and the short/long data
groups.

## Independent checks

The extracted values were checked in these independent forms:

- DENSO WAVE's rMQR technical page confirms the standardized profile, the
  minimum and maximum dimensions, and the headline minimum/maximum numeric
  capacities.
- Segno at pinned commit `b11dc2913c22b22b3bc0a6efaa989904c44977bf`
  independently contains the data-bit capacities, format words, and alignment
  positions.
- Zint at pinned commit `baa91878f488f472fee78585184ffcee07c1dd51`
  independently contains all 32 dimensions, total/data codewords, character-
  count widths, block counts, format words, alignment positions, fixed mask,
  and placement behavior.
- `RectangularMicroTablesTests` exhaustively reconstructs all symbol areas and
  ECC block groups, derives every input capacity from the mode packing rules,
  calculates both copies of all 64 format values from the BCH generator and
  XOR masks, and checks fixed mask, alignment, and placement coordinates.

One discrepancy was rejected during review: the pinned Segno work-in-progress
rMQR mode mapping spells the Kanji indicator as hexadecimal `64`. ISO/IEC
23941:2022 Table 2 and Zint both specify the three-bit value `100` (integer 4),
which is the value implemented here. The remaining compared constants agreed.

The saved artifacts were verified against `SHA256SUMS` before extraction. No
separate amendment or corrigendum for ISO/IEC 23941:2022 was published in the
catalogue audit performed for T001; the check must be repeated before a
conformance release.
