# Universal TTF/OpenType library development plan

## Objective

Build a pure managed C# library that safely loads, inspects, shapes, measures, instances, subsets, and serializes SFNT-based fonts, with a first-class artifact API for PDF generators. The library must work without native code or third-party dependencies under `src/`; it may use only the .NET runtime and framework libraries. The primary success case is a PDF producer that can accept arbitrary licensed fonts, lay out international text, embed a deterministic minimal font program, and preserve searchable/copyable Unicode text.

“Universal” has three precise meanings in this plan:

- Container-universal: recognize SFNT TrueType/OpenType, TTC/OTC, WOFF 1, and WOFF 2; safely expose and preserve unknown tables.
- Outline-universal: support TrueType `glyf`, CFF 1, CFF 2, static and variable outlines, horizontal and vertical metrics, and monochrome extraction from color fonts.
- Text-universal: itemize and shape all Unicode scripts through the standard OpenType shaping models, including bidirectional and vertical text.

It does not mean executing every proprietary font technology. Apple AAT and SIL Graphite tables will be identified, bounded, exposed as raw tables, preserved when safe, and diagnosed as unsupported for shaping. Standalone Type 1/PFB, EOT, macOS resource-fork suitcase fonts, bitmap-only non-SFNT fonts, and platform font installation APIs are outside the core scope.

## Repository and implementation constraints

- Target `net10.0` and follow every rule in `syntax.md`.
- Source uses file-scoped namespaces, classes, enums, fields, constructors, and block-bodied methods only.
- Public API uses methods rather than properties and uses concrete classes rather than interfaces, structs, records, delegates, or caller-defined generic types.
- Options and result objects are classes. Public overloads replace optional parameters and named arguments.
- Source does not use nullable syntax, LINQ, async/await, iterators, unsafe code, pointers, custom inheritance, `ref`/`out` parameters, exception-catching, or user-defined operators.
- `ReadOnlySpan<byte>`, `Span<byte>`, `ReadOnlyMemory<byte>`, arrays, and framework generic collections are used where their lifetime and the syntax policy permit. Spans never escape a call or become object fields.
- Inputs are copied into library-owned memory by default, so the API does not require `IDisposable` and never retains caller streams. A documented trusted-input mode may retain caller-provided immutable memory.
- Arithmetic involving offsets, counts, sizes, point coordinates, and allocation lengths is checked before slicing or allocating.
- No external library, native binding, source generator, analyzer package, or downloaded executable is used by `src/`.
- External libraries and command-line validators are allowed only under `tests/` for differential and conformance testing.
- Do not create syntax-policy tests, source-scanning tests, or performance/benchmark tests.

## Delivery priorities

- P0 — PDF-safe core: SFNT/TTC loading, required tables, Unicode cmap, TrueType outlines, metrics, simple shaping, TrueType subsetting, and PDF Type 0 artifacts.
- P1 — production typography: complete GSUB/GPOS/GDEF, bidi, script shaping, fallback, line layout, vertical text, CFF 1, and full PDF artifacts.
- P2 — broad format support: WOFF/WOFF2, variations and static instancing, CFF 2, color-font exposure, advanced layout, and deterministic round-trip writing.
- P3 — interoperability hardening: legacy encodings, malformed-font tolerance policy, comprehensive differential corpus, fuzz regressions, and rarely used standardized tables.

## Research basis

The feature set and API boundaries are based on the authoritative documents archived under `specs/` and on the following mature implementations:

| Library | Capabilities used as design evidence | Design lesson for this library |
| --- | --- | --- |
| [FreeType](https://freetype.org/freetype2/docs/reference/index.html) | Robust face/charmap/glyph loading, TrueType and CFF outlines, variations, color layers, bounded glyph recursion | Keep face, size/instance, charmap, glyph metrics, and outline extraction separate; parsing must be hostile-input aware. Rasterization and hint execution are not required for PDF output. |
| [HarfBuzz](https://harfbuzz.github.io/) | Unicode-to-glyph shaping, stable clusters, OpenType/AAT/Graphite backends, feature ranges, variations, and a closure-aware subsetter | Shape results must retain source clusters and direction; layout execution and script-specific preprocessing are separate; subsetting starts from shaped glyphs and computes transitive closure. |
| [fontTools/ttLib](https://fonttools.readthedocs.io/en/latest/ttLib/) | Broad table parsing/writing, unknown-table preservation, variation instancing, CFF support | Preserve raw tables independently from parsed semantic views and make serialization deterministic. |
| [fontTools subsetter](https://fonttools.readthedocs.io/en/latest/subset/) | Glyph/Unicode/layout closure, table keep/drop policies, hint retention choices, glyph renumbering | Subsetting needs an explicit policy object and a result map, not a single opaque `Subset()` call. |
| [SixLabors.Fonts](https://github.com/SixLabors/Fonts) | Managed .NET loading, CFF/CFF2, WOFF/WOFF2, variations, color, GSUB/GPOS, bidi, fallback, line breaking, measurement | A useful .NET font library needs face discovery, shaping, fallback, measurement, and reusable layout results above raw table access. |
| [Apache FontBox/PDFBox](https://pdfbox.apache.org/) | TrueType parsing/subsetting and PDF Type 0 embedding | PDF integration must expose old/new GID maps, widths, descriptor values, embedding bytes, and subset names together. |
| [PDFsharp font embedding](https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Embedding.html) | Practical managed PDF font resolution and TrueType subsetting | Avoid its documented TTC and CFF-subset limitations; embedding-right decisions must be visible to callers. |
| [ttf-parser](https://github.com/harfbuzz/ttf-parser) | Small, read-only, bounds-checked parsing with explicit unsupported cases | Low-level reads should be allocation-light, deterministic, and independent of mutable stream position. |

Key conclusions from the survey:

- Parsing, shaping, text layout, subsetting, and PDF object construction are different responsibilities and need different public models.
- A PDF generator needs glyph IDs and source Unicode sequences simultaneously. Reconstructing Unicode from glyph names after shaping is unreliable.
- Variable fonts should be pinned to a static instance before PDF embedding for viewer compatibility.
- A subset operation must include `.notdef`, composite components, CFF subroutines, variation dependencies, color-layer dependencies when requested, and any retained layout closure.
- Full-font embedding and subsetting are both necessary. `OS/2.fsType` may prohibit embedding or subsetting, and policy decisions belong to the caller after receiving an explicit rights report.
- Unknown tables should never make an otherwise valid font unloadable unless they violate container bounds or the chosen strictness policy.

## Complete feature inventory

### 1. Input, ownership, and containers

- Load from file path, `Stream`, `byte[]`, `ReadOnlyMemory<byte>`, and `ReadOnlySpan<byte>`.
- Synchronous APIs only; streams are read from their current position and are never closed or retained.
- Detect content by signature rather than filename extension.
- Parse scalar-type SFNT fonts: `0x00010000`, `true`, `typ1` where structurally supportable, and `OTTO`.
- Parse TTC/OTC versions 1 and 2, shared tables, face selection, face enumeration, and TTC DSIG metadata.
- Decode WOFF 1 table compression with built-in `DeflateStream`, metadata, private block, checksums, and decompression limits.
- Decode WOFF 2 Brotli streams with built-in `BrotliStream`, packed integers, known-tag directory, collection form, transformed `glyf`/`loca` and `hmtx`, and null transforms.
- Preserve original container bytes, reconstructed SFNT bytes, face offsets, source kind, and media-type hints where requested.
- Configurable strict, compatible, and inspect-only modes.
- Configurable hard limits for input bytes, output bytes, faces, tables, glyphs, points, contours, recursion, lookups, coverage entries, strings, decompression ratio, and diagnostics.
- Deterministic exceptions plus structured warnings/errors that include container, face, table tag, absolute offset, and logical path.

### 2. SFNT directory and low-level access

- Big-endian signed/unsigned 8/16/24/32/64-bit reads, 16.16 and 2.14 fixed values, tags, offsets, and LONGDATETIME values.
- Bounded sub-readers with base-relative offsets and checked slice creation.
- Table directory sorting, duplicate-tag handling, alignment, overlap, truncation, and zero-padding validation.
- Table and whole-font checksum calculation, `head.checkSumAdjustment` verification, and TTC checksum rules.
- Lazy semantic table decoding cached per face, with thread-safe repeated reads.
- Raw table enumeration/copying for private and unsupported tables.
- Strict version handling: compatible higher minor versions may retain extensions; unknown major versions remain raw.
- Deterministic SFNT table ordering, padding, search fields, checksums, and byte emission.

### 3. Identification, naming, and rights

- Parse all versions/formats of `head`, `maxp`, `name`, `OS/2`, and `post` required by OpenType 1.9.1.
- Decode Unicode, Windows Unicode, Macintosh Roman, and supported legacy name encodings with explicit replacement diagnostics.
- Select localized names by exact BCP 47, parent language, platform priority, English fallback, then first valid record.
- Expose family, typographic family, subfamily, full, PostScript, unique, version, copyright, trademark, vendor, designer, description, license, and sample text.
- Normalize and validate PostScript names for PDF use without changing the font's original metadata view.
- Expose weight, width, italic/oblique, monospace, serif/script/symbolic classification, Unicode/code-page ranges, panose bytes, selection flags, and optical size metadata.
- Interpret every `OS/2.fsType` embedding bit: installable, restricted, preview/print, editable, no-subsetting, and bitmap-only.
- Return an embedding decision report; never silently override legal restrictions.

### 4. Character mapping and glyph identity

- Parse `cmap` formats 0, 2, 4, 6, 8, 10, 12, 13, and 14.
- Rank Unicode subtables predictably and merge mappings without losing variation-selector behavior.
- Support Unicode scalar values, non-BMP code points, symbol cmap remapping, UVS default/non-default mappings, and reverse GID-to-Unicode candidates.
- Expose code-point coverage ranges without expanding every range into a dictionary.
- Support legacy Macintosh/Windows cmap encodings through checked-in generated mapping data where needed for old fonts.
- Parse glyph names from `post` formats 1, 2, 2.5, 3, and 4; provide synthetic `gidNNNN` names when absent.
- Preserve multiple Unicode sequences per glyph and source clusters after shaping.

### 5. Metrics and baseline data

- Parse and validate `hhea`/`hmtx`, `vhea`/`vmtx`, `VORG`, `BASE`, `MVAR`, `HVAR`, and `VVAR` metric sources.
- Expose design-unit and scaled horizontal/vertical advances, bearings, origins, ascender, descender, line gap, cap height, x-height, underline, strikeout, caret slope, and global bounds.
- Apply OpenType metric-selection rules (`USE_TYPO_METRICS`, Windows metrics, hhea fallback) through an explicit policy.
- Compute glyph and run ink bounds from outlines, independent from advance bounds.
- Parse legacy `kern` formats needed in Windows and Apple SFNT fonts; apply it only when GPOS kerning is absent or policy requests it.
- Parse `gasp`, `hdmx`, `VDMX`, and `LTSH` for inspection/preservation; they do not affect PDF vector layout.

### 6. TrueType outlines

- Parse short/long `loca`, empty glyphs, simple glyph flags, repeated flags, delta coordinates, on/off-curve points, contours, and instruction bytes.
- Insert implied on-curve points and emit normalized move/line/quadratic/close path commands.
- Parse composite transforms, XY/point attachment, metrics inheritance, scaled/unscaled offsets, overlap flags, component instructions, and nested components.
- Detect component cycles, excessive depth, invalid point attachments, coordinate overflow, and inconsistent bounding boxes.
- Compute exact quadratic Bézier bounds and optionally compare/recalculate stored glyph bounds.
- Preserve or drop TrueType hint programs (`cvt `, `fpgm`, `prep`, glyph instructions) during subsetting; do not execute hints or rasterize.
- Expose component closure and normalized outlines for PDF path conversion.

### 7. CFF 1 and CFF 2 outlines

- Parse CFF headers, INDEX data, DICT operands/operators, standard/custom strings, charsets, encodings, FDArray, FDSelect, private dictionaries, local/global subroutines, and CID-keyed fonts.
- Execute Type 2 charstrings into cubic paths: stems, masks, moves, lines, curves, flex operators, subroutine bias/calls, width rules, and deprecated operators required for compatibility.
- Enforce operand stack, transient array, hint count, byte count, and subroutine recursion limits.
- Parse CFF 2 top/font/private dictionaries, variation stores, `vsindex`, and `blend`.
- Compute cubic Bézier bounds and advances consistently with OpenType metrics.
- Subset CFF 1 charstrings, charsets, encodings, FDSelect/FDArray, strings, and local/global subroutines.
- Instantiate CFF 2 to static outlines and emit a PDF-compatible CFF 1 or static OpenType program.

### 8. OpenType variations

- Parse `fvar`, `avar` versions 1/2, `STAT`, shared ItemVariationStore formats, DeltaSetIndexMap, and FeatureVariations.
- Expose axes, localized axis/value names, min/default/max, hidden flags, named instances, and elidable/fallback names.
- Validate and clamp user coordinates; map design coordinates to normalized coordinates and through `avar`.
- Apply `gvar` tuple deltas, shared/private points, packed deltas, IUP interpolation, phantom points, and composite deltas.
- Apply HVAR/VVAR advance and bearing deltas, MVAR global metric deltas, and cvar CVT deltas when hint data is retained.
- Evaluate GDEF/GPOS variation indices and CFF 2 blends during shaping/outline extraction.
- Freeze any variable font to a deterministic static instance, remove variation-only tables, update names/metrics/bounds, and serialize valid checksums.

### 9. OpenType layout table model and execution

- Parse ScriptList, LangSys, FeatureList, LookupList, FeatureParams, Coverage formats 1/2, ClassDef formats 1/2, Device, VariationIndex, and lookup flags.
- Parse GDEF glyph classes, attachment lists, ligature caret lists, mark attachment classes, mark glyph sets, and item variation store.
- Execute every GSUB lookup: single, multiple, alternate, ligature, contextual, chained contextual, extension, and reverse chained single.
- Execute every GPOS lookup: single, pair formats 1/2, cursive, mark-to-base, mark-to-ligature, mark-to-mark, contextual, chained contextual, and extension.
- Honor lookup direction, IgnoreBaseGlyphs/IgnoreLigatures/IgnoreMarks, mark attachment type, mark filtering set, nested lookup ordering, and lookup-cycle/operation limits.
- Apply required and selected features by script/language, feature range, and feature value; expose feature availability.
- Support GPOS kerning, mark attachment, cursive attachment, device adjustments, and variation adjustments in horizontal and vertical directions.
- Parse BASE, JSTF, and MATH into public inspection models; use BASE/JSTF where applicable to text layout and expose MATH constants/constructions without implementing a math formula formatter.

### 10. Unicode processing and shaping models

- Validate UTF-16 and expose strict/replacement handling for unpaired surrogates using `System.Text.Rune`.
- Preserve original UTF-16 ranges through normalization, decomposition/recomposition, mirroring, reordering, substitution, and fallback.
- Use .NET normalization where possible and bundled Unicode 17 data for script, script extensions, bidi, joining, line break, grapheme break, vertical orientation, Indic categories/positions, and emoji properties.
- Implement extended grapheme clusters (UAX #29), script itemization (UAX #24), bidi isolates/brackets/mirroring and visual runs (UAX #9), line-break opportunities (UAX #14), vertical orientation (UAX #50), and Arabic mark transient reordering (UAX #53).
- Map BCP 47 language tags to OpenType language-system tags with documented fallback.
- Implement standard/default shaping, Arabic/Syriac joining, Hebrew mark handling, Indic v1/v2 syllable machines and reordering, Khmer, Myanmar, Thai/Lao, Hangul/Jamo, and Universal Shaping Engine behavior for remaining complex scripts.
- Handle ZWJ, ZWNJ, CGJ, variation selectors, emoji modifiers, keycaps, regional indicators, tag sequences, and emoji ZWJ clusters without losing extraction text.
- Apply script-specific feature stages in deterministic order and distinguish mandatory, default, and discretionary features.
- Produce stable clusters, glyph flags, ligature component counts, x/y advances, x/y offsets, logical/visual indices, and unsafe-to-break boundaries.
- Support left-to-right, right-to-left, top-to-bottom, and bottom-to-top shaping.

### 11. Font families, matching, and fallback

- Build application-owned font sets from files, streams, memory, directories, or already loaded faces; no implicit OS-global state.
- Group typographic families and faces using localized names and style attributes.
- Match normal/italic/oblique, weight, stretch, optical size, and requested variation coordinates deterministically.
- Resolve fallback by grapheme/shaping cluster rather than individual UTF-16 code unit.
- Prefer one face covering a whole cluster and preserve combining marks, emoji sequences, joiners, and variation selectors.
- Allow ordered caller fallback families, generic family aliases, script defaults, and a final missing-glyph face.
- Split shaping runs only at safe cluster/script/direction/style/font boundaries.
- Report every substitution and missing glyph so a PDF generator can fail, warn, or draw a replacement intentionally.

### 12. Measurement and reusable text layout

- Shape without line layout, measure a shaped run, or build a reusable paragraph layout.
- Return advance bounds, exact ink bounds, ascent/descent, baselines, line metrics, glyph positions, cluster ranges, and resolved fonts.
- Apply font size, DPI-independent point scaling, character spacing, word spacing, horizontal scaling, baseline shift, and synthetic slant only when explicitly requested.
- Implement tabs, hard breaks, UAX #14 wrapping, soft hyphen, optional caller-supplied hyphenation dictionaries, emergency breaking, trimming, ellipsis, and maximum lines.
- Implement start/end/center alignment and justified lines with space, tracking, and optional JSTF/kashida opportunities.
- Lay out horizontal and vertical lines with bidi visual order while retaining logical text ranges.
- Expose hit testing, caret stops, selection rectangles, and logical-to-visual maps as useful by-products; PDF output primarily consumes positioned runs.
- Make prepared text reusable across widths without mutating fonts or prior results.

### 13. Color and bitmap glyph exposure

- Parse CPAL palettes and palette labels/types.
- Parse COLR v0 layer records and COLR v1 paint graphs, clips, transforms, gradients, composites, and variation values with cycle limits.
- Parse SVG document ranges and safely expose compressed/uncompressed XML bytes; XML parsing uses built-in secure settings with external entities disabled.
- Parse `sbix`, CBDT/CBLC, and EBDT/EBLC strike metadata and return raw PNG/JPEG/TIFF/bitmap payload plus metrics without depending on an image library.
- Select monochrome base outlines for standard PDF font embedding.
- Optionally return a normalized color paint graph so a PDF generator can emit vector/raster appearance content separately from searchable invisible text.
- Include color-layer glyphs in subset closure only when the caller requests retained color data.

### 14. Subsetting and deterministic font writing

- Incrementally collect Unicode sequences, shaped runs, raw glyph IDs, feature tags, palettes, and variation coordinates.
- Always include `.notdef`; compute composite, CFF subroutine, COLR, SVG/bitmap, and retained-layout closure.
- Offer compact-GID and preserve-GID modes, returning old-to-new and new-to-old maps.
- Support full embedding, glyph subset, Unicode subset, and already-shaped subset workflows.
- Enforce `fsType` no-subsetting and restricted embedding through caller-selected reject/report/full-embed policy.
- Rebuild required TrueType tables and optionally retain safe metadata, layout, hint, vertical, color, and unknown tables.
- Recompute `maxp`, `head`, `loca`, metrics counts, bounds, cmap, names, OS/2 ranges/first-last chars, post, checksums, padding, and search fields.
- Subset GSUB/GPOS/GDEF/BASE/JSTF with glyph remapping and lookup/feature/script pruning when layout retention is requested.
- Generate a deterministic six-uppercase-letter PDF subset prefix from font identity, instance coordinates, policy, and sorted glyph set, with caller collision override.
- Byte-for-byte deterministic output for identical inputs/options across processes and cultures.

### 15. PDF-generator artifacts

- Do not reference a PDF library from `src/`; return neutral byte arrays, strings, arrays, metrics, and mapping models.
- Build simple TrueType artifacts for restricted legacy use and Type 0 composite artifacts for general Unicode use.
- Support CIDFontType2 with `FontFile2`, CIDFontType0 with CID-keyed CFF `FontFile3`, and OpenType `FontFile3` where the chosen PDF compatibility level permits it.
- Default to Type 0 plus Identity-H/Identity-V and two-byte character codes, with dense document CIDs independent of original GIDs.
- Return code-to-CID, CID-to-GID, GID-to-CID, old/new GID, and Unicode-sequence maps.
- Generate `CIDToGIDMap` bytes or identify when `/Identity` is valid.
- Return `/CIDSystemInfo` values and an optional CIDSet bitset when a selected legacy PDF/A profile requires one.
- Generate compact `/W`, `/DW`, `/W2`, and `/DW2` data models in PDF 1000-unit space with correct rounding.
- Generate FontDescriptor values: subset base name, flags, bbox, italic angle, ascent, descent, cap height, x-height, stem estimate, missing width, and embedding stream subtype/length metadata.
- Generate a standards-compliant ToUnicode CMap for BMP, non-BMP surrogate pairs, combining sequences, ligatures, reordered clusters, and multi-code-point glyph semantics.
- Report mappings that cannot be represented unambiguously and return `ActualText` recommendations for the PDF content/tagging layer.
- Accumulate glyph usage over a document and finalize once; reject additions after finalization.
- Allow multiple independent PDF resources from one face/instance and deterministic resource deduplication keys.
- Report PDF version requirements, PDF/A and PDF/UA-relevant embedding/Unicode conditions, without claiming whole-document conformance.
- Provide positioned run data that a PDF generator can translate directly into text matrices and `TJ` adjustments, including vertical text.

### 16. Robustness, diagnostics, and maintainability

- Treat fonts as hostile binary input and make all offsets/counts/allocations bounded.
- Limit decompression, component recursion, CFF subroutines, layout nesting, lookup applications, paint graph recursion, and output size.
- Never depend on current culture, stream seekability, dictionary enumeration order, filesystem font availability, or mutable global configuration.
- Cache immutable semantic views and instance data safely for concurrent readers.
- Distinguish fatal structural errors, unsupported semantic features, recoverable inconsistencies, and informational repairs.
- Provide table/tag/version/offset context in diagnostics without embedding font data in messages.
- Preserve original bytes for audit and reproduce deterministic results from explicit options.
- Document every repair performed in compatible mode and never repair silently.

## Architectural sequence

1. Decode a container into bounded, immutable face/table byte regions.
2. Build a lazy `FontFace` semantic view and diagnostics without changing source bytes.
3. Create a `FontInstance` that resolves variation coordinates and scaled metrics.
4. Itemize Unicode text into normalization/script/language/bidi/font runs.
5. Apply the selected shaping model and generic GSUB/GPOS engines into positioned glyphs with source clusters.
6. Reflow shaped clusters into lines, or pass shaped runs directly to a PDF generator.
7. Accumulate document glyph use, compute closure, instance if needed, subset, and write a deterministic font program.
8. Build PDF-neutral font resource artifacts and mappings; the consuming PDF library owns indirect objects and content syntax.

## Acceptance definition

The project is complete when all P0–P2 features above are implemented and the following are true:

- Every downloaded normative conformance data set relevant to implemented behavior passes.
- Representative Latin, Arabic, Hebrew, Indic, Southeast Asian, CJK, emoji, supplementary-plane, vertical, variable, CFF, and collection fonts shape equivalently to pinned HarfBuzz expectations for supported OpenType behavior.
- TrueType, CFF 1, and statically instantiated variable subsets reopen in this library and fontTools and retain the requested glyph outlines/metrics.
- Generated PDF artifacts can be assembled by the test fixture into PDFs that render, extract Unicode text, and pass relevant font checks in at least two independent PDF consumers/validators.
- Malformed inputs fail within configured limits with deterministic diagnostics and no unbounded recursion or allocation.
- Identical inputs and options produce byte-identical subset font programs, ToUnicode CMaps, and width/CID maps.
- `src/ttf.csproj` has no package or project dependency other than the .NET framework; all differential dependencies remain test-only.
- Public XML documentation covers every public type/member required by `syntax.md`.

## Public API completion tasks

- [DONE] T001: Expose immutable container provenance for each loaded package, including original and reconstructed lengths, face offsets, TTC/OTC version and DSIG metadata, WOFF metadata and private blocks, and caller-supplied media-type hints.
- [DONE] T002: Complete low-level public face inspection with table version/support state, checksum and alignment validation results, raw-table preservation state, and deterministic standalone-SFNT emission from a selected face.
- [DONE] T003: Expose complete name-record enumeration and localized selection results, including platform, encoding, language, name identifiers, decoded values, replacement diagnostics, and RFC 4647 lookup provenance.
- [DONE] T004: Add public character-mapping operations for Unicode scalars, symbol remapping, variation-selector default and non-default mappings, subtable provenance, missing mappings, and reverse glyph-to-Unicode candidates without expanding coverage ranges.
- [DONE] T005: Complete public glyph identity inspection with all applicable PostScript name formats, synthetic-name provenance, multiple Unicode sequences per glyph, and safe lookup by glyph identifier or name.
- [DONE] T006: Expose BASE baselines and the planned legacy metric-inspection data from hdmx, VDMX, LTSH, gasp, and kern alongside resolved horizontal and vertical design/scaled metrics and exact run ink bounds.
- [DONE] T007: Add public variation-axis, named-instance, avar, and STAT models with localized names, min/default/max values, hidden and elidable flags, fallback names, clamping reports, and user-to-normalized coordinate mappings.
- [DONE] T008: Add public OpenType layout discovery for scripts, language systems, required and optional features, feature parameters, lookup availability, feature variations, and caller-selected feature values over UTF-16 ranges.
- [DONE] T009: Expose read-only GDEF, BASE, JSTF, and MATH inspection models for glyph classes, attachments, ligature carets, mark sets, baselines, justification priorities, math constants, variants, assemblies, and constructions.
- [DONE] T010: Add public shaping options for strict or replacement UTF-16 handling, normalization, direction, script, BCP 47 language, feature ranges and values, cluster policy, vertical behavior, and bounded operation limits.
- [DONE] T011: Add public string and ReadOnlySpan<char> shaping entry points that invoke the existing Unicode itemization, script-specific shaping, GSUB, GPOS, kerning, device, and variation logic for a FontInstance.
- [DONE] T012: Complete ShapedRun and IShapedRun and add public shaped-glyph and shaped-cluster results exposing resolved font instances, glyph identifiers, source UTF-16 and Unicode ranges, logical/visual maps, flags, ligature data, advances, offsets, unsafe-break boundaries, diagnostics, total advance, and exact ink bounds.
- [DONE] T013: Add fallback-aware shaping entry points over FontSet that resolve whole grapheme clusters, split only at safe font/script/direction/style boundaries, and return ordered runs with their resolved faces and instances.
- [DONE] T014: Expose deterministic fallback reports for family substitutions, generic and script fallback decisions, unsupported font technologies, missing glyphs, and the final missing-glyph face so callers can choose fail, warn, or replacement behavior.
- [DONE] T015: Add a public text-layout engine that prepares reusable text from a FontSet, source text, styles, and layout options, then reflows the immutable prepared result at multiple widths.
- [DONE] T016: Expose public text-layout, line, positioned-run, and positioned-glyph results with advance and ink bounds, line metrics, baselines, resolved fonts, cluster ranges, glyph positions, source ranges, and logical/visual ordering.
- [DONE] T017: Complete the public line-layout controls and results for tabs, hard breaks, UAX 14 wrapping, soft hyphens, caller dictionaries, emergency breaking, whitespace trimming, ellipsis, and maximum-line truncation.
- [DONE] T018: Expose horizontal and vertical bidi line construction, start/end/center alignment, space and tracking justification, JSTF and kashida opportunities, and the applied adjustments without mutating shaped input.
- [DONE] T019: Add public hit-test, caret-stop, selection-rectangle, and logical-to-visual mapping results that operate only at shaping-safe grapheme and cluster boundaries.
- [DONE] T020: Expose CPAL palette enumeration, colors, palette labels, entry labels, palette types, foreground-color handling, and variation-resolved palette selection.
- [DONE] T021: Complete strongly typed public COLR v0/v1 paint results for layers, clips, affine transforms, gradients, extend modes, composites, referenced-glyph closure, and variation-resolved values with cycle/depth diagnostics.
- [DONE] T022: Complete public SVG, sbix, CBDT/CBLC, and EBDT/EBLC inspection with format-specific payload kinds, glyph and line metrics, strike selection, compression state, secure built-in .NET XML loading, and monochrome fallback guidance.
- [DONE] T023: Extend FontSubsetBuilder with exact Unicode-sequence and glyph-to-sequence inputs and expose the computed composite, CFF subroutine, layout, color, SVG/bitmap, and recommended-glyph closures.
- [DONE] T024: Complete subset result reporting for source-instance identity, effective retention policies, rewritten and preserved table decisions, variation freezing, output outline kind, deterministic prefix inputs, and every compatible-mode repair.
- [DONE] T025: Complete PdfFontOptions for CID allocation, subset prefix collision override, hint/layout/vertical/color/metadata retention, simple-font restrictions, static-instance selection, and explicit PDF version/profile compatibility choices.
- [DONE] T026: Make Type 0 encoding metadata, CIDSystemInfo, CIDToGIDMap or Identity choice, optional CIDSet, horizontal and vertical widths, descriptor values, ToUnicode data, resource names, and all glyph/CID/code maps directly reachable from each applicable PdfFontResource.
- [DONE] T027: Add public PDF text-usage operations that can shape source text or consume fallback-shaped runs, allocate multiple independent resources when resolved fonts differ, and return positioned codes, TJ adjustments, source ranges, and ActualText recommendations.
- [DONE] T028: Carry cancellation, resource limits, deterministic diagnostics, and strict/compatible/inspect policies through public shaping, layout, color resolution, subsetting, static instancing, and PDF finalization operations.
- [DONE] T029: Complete FontApiFactory construction paths for every public service and safe-default option set needed for loading, coordinates, shaping, layout, color resolution, subsetting, hyphenation, and PDF resource generation.
- [DONE] T030: Add a separate consumer-style test project that references the library normally and exercises public contracts without InternalsVisibleTo access.
- [DONE] T031: Add complete public loading and container tests for every input form, signature, SFNT scalar type, TTC/OTC version, WOFF variant, metadata/private block, ownership mode, non-seekable stream behavior, checksum policy, limits, and structured diagnostic context.
- [DONE] T032: Add complete public identification, naming, classification, embedding-rights, raw-table, cmap, variation-selector, reverse-mapping, coverage-range, and glyph-name tests across modern and legacy encodings.
- [DONE] T033: Add complete public metric, baseline, legacy metric-table, TrueType outline, composite closure, CFF/CFF2 outline, hint-preservation, exact-bounds, variation-axis, named-instance, and deterministic static-instance tests.
- [DONE] T034: Add complete public layout-inspection and shaping-option tests covering script/language/feature discovery, required and ranged features, every GSUB and GPOS lookup family, GDEF behavior, kerning policy, device and variation adjustments, limits, and unsupported AAT or Graphite diagnostics.
- [DONE] T035: Drive Unicode normalization, grapheme segmentation, bidi, line breaking, vertical orientation, emoji handling, and representative Latin, Arabic, Hebrew, Indic, Khmer, Myanmar, Thai/Lao, Hangul, and USE shaping through public APIs and compare supported behavior with pinned conformance data and test-only HarfBuzz expectations.
- [DONE] T036: Add complete public FontSet, family grouping, style/weight/stretch/optical/variation matching, generic alias, ordered and script fallback, whole-cluster coverage, missing-glyph reporting, and fallback-shaped multi-run tests.
- [DONE] T037: Add complete public measurement and paragraph-layout tests for scaling and style effects, reusable reflow, tabs and hard breaks, wrapping and soft hyphens, dictionaries, emergency breaks, trimming and ellipsis, maximum lines, all alignments, justification, horizontal and vertical bidi layout, hit testing, carets, selections, and logical/visual maps.
- [DONE] T038: Add complete public color-glyph tests for CPAL metadata, COLR v0/v1 paints and variation values, cycles and limits, secure SVG decoding, bitmap formats and metrics, representation selection, monochrome outlines, and PDF appearance guidance.
- [DONE] T039: Add complete public subsetting tests for every seed type and policy, all closure categories, compact and preserved glyph identifiers, TrueType and CFF output, variable-font freezing, layout/hint/vertical/color/unknown-table retention, embedding rights, table reports, repairs, maps, prefixes, determinism, and reopening with this library plus test-only fontTools.
- [DONE] T040: Add complete public PDF artifact tests for simple TrueType, CIDFontType2, CIDFontType0, and OpenType FontFile3 resources; horizontal and vertical encodings and widths; dense CIDs; descriptor flags and metrics; CID maps and CIDSet; ToUnicode edge cases; ActualText; rights; profiles; version gating; resource deduplication; and finalization state.
- [DONE] T041: Add public contract tests for every constructor, factory, option value, enum-controlled branch, boundary validation, exception type, immutable defensive copy, null result, state transition, and diagnostic result exposed under src/Api.
- [DONE] T042: Add public concurrency and determinism tests for shared package, face, instance, shaping, layout, color, subset, and independent PDF resource operations, including culture independence and stable output ordering.
- [DONE] T043: Add end-to-end public-only tests that load representative international fonts, match and shape fallback text, lay it out, subset each resolved instance, build PDF-neutral resources, assemble test PDFs, verify extraction and vertical positioning, and validate the embedded fonts with independent test-only consumers.
