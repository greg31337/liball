# Legacy Model 1 table extraction

Extracted and cross-checked 2026-09-16 against ISO/IEC 18004:2024 Annex N
and the detailed legacy definition retained in ISO/IEC 18004:2000 Annex M.
The controlled-edition and correction policy are recorded in
`CONFORMANCE_SOURCES.md`.

## Implemented material

| Standard material | Implementation |
|---|---|
| Versions 1-14, dimensions, function/format/encoding module counts, and total symbol-character counts | `LegacyModel1Table` |
| L/M/Q/H data capacity, equal-sized Reed-Solomon block counts, ECC widths, and trailing remainder codewords | `LegacyModel1Table` |
| Numeric, alphanumeric, byte, and Kanji character-count widths for versions 1-9 and 10-14 | `LegacyModel1Table` |
| The Model 1 BCH(15,5) format XOR mask used to discriminate Model 1 from Model 2 | `LegacyModel1Table` |
| Finder-adjacent format copies, fixed dark module, extension-pattern positions, and both permitted extension-pattern appearances | `LegacyModel1Table` |
| Four-bit first symbol character, vertical/horizontal eight-bit character layouts, and right-to-left character-column orientation rules | `LegacyModel1Table` |
| Decode restrictions: no ECI, version information, alignment patterns, mirror imaging, reflectance reversal, or block interleaving | `LegacyModel1Table` |

The first data symbol character contains four bits. It is prefixed with four
zero bits only while calculating Reed-Solomon parity. Later data and ECC symbol
characters contain eight bits. Multiple data blocks are followed by their ECC
blocks without Model 2's codeword interleaving; any remainder codewords follow
the final ECC block.

Model 1 uses extension patterns instead of alignment patterns. Every version
has a two-by-two corner block. Each side additionally has `version / 2`
two-by-four blocks, beginning at module 17 for odd versions or module 13 for
even versions and repeating every eight modules. Early symbols with the two
additional dark endpoint modules documented by the legacy standard are
accepted as an alternate fixed pattern.

## Independent checks

The extracted values were checked in these independent forms:

- DENSO WAVE's Model 1 technical material confirms versions 1-14, the
  21-by-21 through 73-by-73 dimensions, and the version 14-L headline
  capacities.
- The public JIS X 0510:2018 Annex N rendering independently confirms the
  legacy geometry and the compatibility differences retained by the current
  ISO edition.
- `LegacyModel1TableTests` exhaustively reconstructs all 56 data/ECC/remainder
  configurations, independently calculates every format word, checks all
  extension-pattern modules for every version, and verifies the symbol-
  character bit layouts and format coordinates.

The saved research artifacts were verified against `SHA256SUMS` before
extraction. No separate amendment or corrigendum for ISO/IEC 18004:2024 was
published in the catalogue audit performed for T001; the check must be
repeated before a conformance release.
