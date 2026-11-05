# Reference archive

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Retrieved on 2026-08-17 for implementation and test design, with the IETF language-matching and Brotli references added on 2026-08-25. These files are reference inputs, not production dependencies. Copyright and license terms remain with each publisher; do not redistribute this directory without reviewing those terms.

## Normative and primary references

### OpenType and TrueType

- `opentype/*.html` — the complete Microsoft OpenType 1.9.1 overview, container, recommendation, registry, layout, variation, and table chapters used by this plan. Source: <https://learn.microsoft.com/en-us/typography/opentype/spec/>.
- `opentype/Apple_TrueType_Reference_Manual_Index.html` — Apple TrueType Reference Manual index.
- `opentype/Apple_TrueType_Font_Tables.html` — Apple's SFNT and TrueType table reference, including Apple/AAT table links.
- `opentype/Apple_TrueType_Instruction_Set.html` — TrueType instruction reference. Instructions are preserved but not executed by the planned PDF-focused library.
- `download-opentype.sh` — repeatable downloader for the Microsoft OpenType pages. The archived HTML remains the review baseline even if the live pages later change.

### CFF and Type 2

- `adobe-font-tech/Adobe_TN_5176_CFF.pdf` — Adobe Technical Note #5176, Compact Font Format Specification.
- `adobe-font-tech/Adobe_TN_5177_Type2.pdf` — Adobe Technical Note #5177, Type 2 Charstring Format.
- Source index: <https://adobe-type-tools.github.io/font-tech-notes/>.

### Web font containers

- `webfonts/WOFF_1.0.html` — W3C Recommendation, WOFF File Format 1.0.
- `webfonts/WOFF_2.0.html` — W3C Recommendation, WOFF File Format 2.0, including transformed `glyf`, `loca`, and `hmtx` reconstruction.

### Unicode 17.0

- `unicode/UAX9_Bidi_Unicode17.html` — UAX #9 revision 51, Unicode Bidirectional Algorithm.
- `unicode/UAX14_LineBreak_Unicode17.html` — UAX #14 revision 55, Unicode Line Breaking Algorithm.
- `unicode/UAX15_Normalization_Unicode17.html` — UAX #15 revision 57, Unicode Normalization Forms.
- `unicode/UAX24_Script_Unicode17.html` — UAX #24 revision 39, Unicode Script Property.
- `unicode/UAX29_Segmentation_Unicode17.html` — UAX #29 revision 47, Unicode Text Segmentation.
- `unicode/UAX50_Vertical_Unicode17.html` — UAX #50 revision 32, Unicode Vertical Text Layout.
- `unicode/UAX53_ArabicMarks_Unicode17.html` — UAX #53 revision 11, Unicode Arabic Mark Rendering.
- `unicode/UTS51_Emoji_Unicode17.html` — UTS #51 revision 29, Unicode Emoji.
- `unicode/UCD_17.0.0.zip` — pinned Unicode Character Database and official bidi, grapheme, line-break, normalization, and property conformance data.

### Shaping descriptions

- `shaping/Microsoft_Standard_Shaping.html` — standard non-complex OpenType shaping model.
- `shaping/Microsoft_Arabic_Shaping.html` — Arabic/Syriac OpenType shaping model.
- `shaping/Microsoft_Devanagari_Shaping.html` — representative dedicated Indic v1/v2 shaping model.
- `shaping/Microsoft_Myanmar_Shaping.html` — Myanmar shaping model.
- `shaping/Microsoft_USE_Shaping.html` — Universal Shaping Engine model.
- `shaping/HarfBuzz_Shaping_Concepts.html` and `HarfBuzz_Clusters.html` — comparative descriptions of shaping stages and cluster semantics.
- `shaping/HarfBuzz_Subsetting_API.html` — comparative subset closure and policy reference.

### PDF

- `pdf/PDF_Reference_1.7.pdf` — Adobe PDF Reference 1.7 archive, including the font, CIDFont, embedded font program, widths, encoding, and ToUnicode requirements used by the artifact design.
- The current ISO 32000-2:2020 PDF 2.0 bundle with Errata Collection 3 is available at no charge only through the PDF Association's account/cart workflow: <https://www.pdfa-inc.org/product/iso-32000-2-pdf-2-0-bundle-sponsored-access/>. It was not automated because obtaining it requires user registration/checkout. Review its updated font clauses before final PDF 2.0 release certification.

### IETF

- `ietf/RFC4647_Language_Tag_Matching.txt` — BCP 47 language-range matching and lookup behavior used by localized-name and language-system fallback.
- `ietf/RFC5646_Language_Tags.txt` — BCP 47 language-tag syntax and fallback input.
- `ietf/RFC7932_Brotli_Compressed_Data_Format.txt` — Brotli compressed-stream format used by WOFF 2 decompression.
- `ietf/RFC8081_Font_Media_Types.txt` — registered top-level font media types and format identifiers.

## Secondary implementation comparisons

The HarfBuzz pages are comparative rather than normative. FreeType, fontTools, SixLabors.Fonts, Apache FontBox/PDFBox, PDFsharp, and ttf-parser are linked from `todo.md`; their source code is not vendored here and must not become a `src/` dependency.

## Integrity

`SHA256SUMS` records the exact local baseline. Regenerate it only when deliberately updating reference versions, then record the version/date change in this file and in the Unicode/shaping compatibility documentation.
