# Micro QR table extraction

Extracted and cross-checked 2026-09-16 against ISO/IEC 18004:2024. The
controlled edition and correction policy are recorded in
`CONFORMANCE_SOURCES.md`.

## Implemented material

| Standard material | Implementation |
|---|---|
| M1-M4 dimensions, two-module quiet zone, total encoding-region size, terminator widths, and final data-symbol widths | `MicroVersionTable` |
| Version-specific numeric, alphanumeric, byte, and Kanji mode indicators and character-count widths | `MicroVersionTable` |
| All eight legal version/ECC symbol numbers, data capacities, symbol-character counts, one-block ECC sizes, and maximum input capacities | `MicroVersionTable` |
| Four Micro QR data-mask predicates | `MicroMaskTable` |
| All 32 masked BCH(15,5) format words | `MicroFormatCodeTable` |
| Finder, separator, timing, format, two-column zig-zag, and quiet-zone geometry | `MicroPlacementTable` |

Versions M1 and M3 end their data region with a four-bit symbol character.
The implementation therefore records bit capacity separately from symbol
character count instead of rounding the 20-bit and 84/68-bit capacities to
whole bytes. Version M1 uses symbol number zero and error detection only; it
is not represented as error correction level L.

## Independent checks

The extracted values were checked in three independent forms:

- DENSO WAVE's Micro QR technical page independently lists every dimension,
  supported ECC configuration, maximum input capacity, and the two-module
  quiet zone.
- Segno at pinned commit `b11dc2913c22b22b3bc0a6efaa989904c44977bf`
  supplies an independently maintained set of symbol capacities, mode widths,
  format words, masks, and placement behavior.
- libqrencode at pinned commit
  `715e29fd4cd71b6e452ae0f4e36d917b43122ce8` independently expresses the
  capacity/ECC configurations, count widths, format mapping, and frame
  placement.
- `MicroTablesTests` reconstructs the encoding-region size from geometry,
  reconstructs every data/ECC split, calculates all format words from the BCH
  generator polynomial and XOR mask, and checks fixed mask and placement
  coordinates.

The pinned reference revisions and their licenses are listed in `SOURCES.md`.
No separate amendment or corrigendum for ISO/IEC 18004:2024 was published in
the catalogue audit performed for T001; the check must be repeated before a
conformance release.
