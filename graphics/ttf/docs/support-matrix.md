# Support matrix

This matrix describes the implementation in this repository. It is narrower
than the general OpenType, Unicode, or PDF specifications and takes precedence
over an unqualified feature mention elsewhere in the documentation.
Compatibility when these statuses change is governed by the
[versioning policy](versioning.md).

## Status definitions

| Status | Meaning |
| --- | --- |
| **Fully supported** | The relevant structures are bounded and validated, their implemented semantics participate in production operations, and generated data is canonical where the library offers output. |
| **Inspect-only** | The structure can be recognized, bounded, and sometimes parsed, but its behavior is not executed or it has no supported semantic rewrite. Raw bytes may still be available. |
| **Preserved raw** | The library does not interpret the semantics. Bounded bytes can be copied when raw preservation is enabled and may be retained only under an output policy that can prove copying is safe. |
| **Unsupported** | The input or behavior is outside the implementation. A recognized container is rejected; a table may be diagnosed and ignored while another supported path remains usable. |

Status describes library capability, not permission to embed a font and not a
claim that every malformed input can be repaired. Strict mode can reject a
nominally supported format for any normative violation. Compatible mode accepts
only defined, deterministic repairs. Inspect-only load mode can deliberately
reduce a normally supported face to bounded raw access.

## Containers and font programs

| Container or program | Status | Scope and limitations |
| --- | --- | --- |
| Scalar SFNT TrueType/OpenType | **Fully supported** | Scaler types `0x00010000`, `OTTO`, `true`, and `typ1` are recognized. Usable outline semantics still require a supported table combination. |
| TTC/OTC collections | **Fully supported** | Every face is selected explicitly; shared table regions are bounded and validated. |
| WOFF 1 | **Fully supported** | Reconstructed to canonical SFNT with bounded decompression and metadata/private-block validation. |
| WOFF 2 | **Fully supported** | Known transformed tables are reconstructed with bounded Brotli decoding; unsupported transforms fail rather than being guessed. |
| TrueType `glyf` outlines | **Fully supported** | Simple and composite outlines, bounds, metrics, variable deltas, static instancing, closure, and deterministic subsetting. Hint programs are data only; they are not executed. |
| CFF 1 outlines in SFNT | **Fully supported** | Type 2 charstrings, local/global subroutines, CID-keyed data, outlines, closure, subsetting, and PDF artifacts. |
| CFF 2 variable outlines | **Fully supported** | Variation stores and blends are resolved into a deterministic static CFF 1 program before output. |
| SFNT bitmap/color strikes | **Fully supported** | Exposed as glyph representations when a supported location/data pairing exists; this library does not rasterize them. |
| Standalone Type 1 PFB/PFA | **Unsupported** | Recognized signatures are rejected. |
| EOT | **Unsupported** | Embedded OpenType containers are recognized and rejected. |
| PCF, BDF, and other bitmap-only non-SFNT fonts | **Unsupported** | Recognized PCF/BDF signatures are rejected. |
| macOS suitcase/resource-fork fonts | **Unsupported** | No container loader or operating-system integration is provided. |

## Feature support

| Feature | Status | Scope and limitations |
| --- | --- | --- |
| Bounded loading, ownership, limits, cancellation, and diagnostics | **Fully supported** | File, stream, array, memory, and span inputs; strict, compatible, and inspect-only policies. |
| Names, classification, coverage, metrics, rights, tables, and glyph inspection | **Fully supported** | Public immutable/copying results; localized name fallback and Unicode coverage ranges. |
| TrueType, CFF 1, and resolved CFF 2 outline extraction | **Fully supported** | Returns vector geometry and exact bounds; no scan conversion. |
| OpenType shaping | **Fully supported** | `cmap`, GSUB, GPOS, and GDEF with dedicated Arabic/Syriac, Hebrew, Indic, Khmer, Myanmar, Thai/Lao, Hangul, Universal, emoji, and default paths. The engine currently has no public external-assembly facade for creating `ShapedRun`; see the [shaping guide](guides/text-and-fallback.md). |
| Unicode processing | **Fully supported** | Unicode 17.0.0 data, strict/replacement UTF-16 decoding, normalization, script, bidi, grapheme/line boundaries, vertical orientation, and original source clusters. |
| Application-owned matching and cluster fallback | **Fully supported** | Deterministic families, generic mappings, script and global fallback, variation sequences, and indivisible-cluster resolution. No system font discovery. |
| Horizontal and vertical text layout | **Fully supported** | Line fitting, trimming, hyphenation, justification, caret/selection models, and vertical orientation/metrics. The layout engine currently remains an internal service. |
| Variable-font coordinates and static instancing | **Fully supported** | `fvar`, `avar`, TrueType variations, CFF 2 blends, metric variations, metadata changes, and GSUB/GPOS FeatureVariations resolution. |
| Deterministic subsetting | **Fully supported** | TrueType and CFF, shaped/text/glyph seeds, composite and layout closure, compact/preserved GIDs, selected vertical/color retention, maps, and diagnostics. See table-specific output limits below. |
| PDF font artifacts | **Fully supported** | Type 0 CIDFontType2, CIDFontType0, permitted OpenType FontFile3, restricted simple TrueType, CIDs, widths, vertical widths, descriptors, ToUnicode, `ActualText` recommendations, and positioned runs. PDF objects and content streams are not emitted. |
| Color-glyph resolution and PDF appearance guidance | **Fully supported** | COLR/CPAL, sanitized SVG, `sbix`, CBDT/CBLC, EBDT/EBLC, palettes, foreground color, and monochrome fallback. The consuming renderer creates vector/image appearances. |
| Embedding-right interpretation and enforcement policies | **Fully supported** | Interprets `OS/2.fsType`, reports restrictions, rejects conflicts, or permits the defined full-font fallback. It is not legal advice. |
| TrueType hint retention and exposure | **Inspect-only** | `cvt `, `fpgm`, `prep`, and glyph instructions can be exposed, retained, or consistently dropped. |
| TrueType hint execution and rasterization | **Unsupported** | No grid fitting, scan conversion, antialiasing, or bitmap rendering API. |
| AAT shaping | **Unsupported** | AAT tables are recognized and can be exposed as bounded raw data; `morx`/`mort` programs are not executed. |
| SIL Graphite shaping | **Unsupported** | Graphite tables are recognized and can be exposed as bounded raw data; `Silf` programs are not executed. |
| Operating-system font discovery or installation | **Unsupported** | `FontSet` contains only caller-added packages, faces, files, and directories. |

“Fully supported” shaping and layout describe the managed implementation and its
test coverage. They do not override the noted public-facade limitation in the
current assembly version.

## SFNT table support

Tables grouped on one row have the same primary support status. A supported
table version outside the implemented version range becomes inspect-only/raw;
it is never interpreted as a known older version.

| Tables | Status | Implemented use and output behavior |
| --- | --- | --- |
| `head`, `maxp` | **Fully supported** | Core geometry, glyph count, limits, checksums, canonical static/subset output. |
| `name`, `OS/2`, `post` | **Fully supported** | Localized names, classification, metrics, rights, glyph names, deterministic instance/subset metadata, and PDF descriptors. `post` formats 1, 2, 2.5, and 3 are handled. |
| `cmap` | **Fully supported** | Formats 0, 2, 4, 6, 8, 10, 12, 13, and variation format 14; mapping, coverage, fallback, shaping, Unicode maps, and canonical subset output. |
| `hhea`, `hmtx`, `vhea`, `vmtx`, `VORG` | **Fully supported** | Horizontal/vertical glyph and line metrics, variation application, static output, subset rewriting, and PDF widths. |
| `glyf`, `loca` | **Fully supported** | Simple/composite outlines, closure, variation, static instancing, and subset rewriting. |
| `CFF `, `CFF2` | **Fully supported** | CFF 1 inspection/subsetting and CFF 2 static resolution; emitted subset/PDF program depends on outline and selected PDF policy. |
| `fvar`, `avar`, `gvar`, `cvar`, `HVAR`, `VVAR`, `MVAR`, `STAT` | **Fully supported** | Coordinate resolution, outline/metric/CVT deltas, static metadata and metric resolution. Live variation tables are removed from static output. |
| `GSUB`, `GPOS`, `GDEF`, `kern` | **Fully supported** | OpenType substitution/positioning, glyph classes/attachments, legacy kerning, FeatureVariations selection, closure, pruning, and remapping when retained. |
| `COLR`, `CPAL` | **Fully supported** | COLR v0 layers and normalized COLR v1 paint graphs. Compact subsets rewrite COLR v0; COLR v1 is copied only with preserved GIDs or otherwise dropped. CPAL is copied with retained color output. |
| `SVG ` | **Fully supported** | Bounded and sanitized documents, optional gzip decoding, color selection, and compact subset document-range rewriting. External resources and active content are not allowed. |
| `sbix` | **Fully supported** | Strikes, origins, duplicate references, image exposure, appearance guidance, closure, and compact subset rewriting. |
| `CBDT`/`CBLC`, `EBDT`/`EBLC` | **Fully supported** | Supported bitmap location/image formats and metrics are exposed. Subsets retain these pairs unchanged only with preserved GIDs; compact subsets drop them. |
| `BASE`, `JSTF`, `MATH` | **Inspect-only** | Bounded semantic parsers exist, but complete public execution/rewrite is not offered. They may be copied only for full-font, preserved-GID layout retention; otherwise subsets drop them. |
| `cvt `, `fpgm`, `prep` | **Inspect-only** | TrueType hint data can be inspected and retained or dropped consistently; instructions are never executed. Static TrueType instancing resolves `cvar` into `cvt `. |
| `gasp` | **Fully supported** | Validated ranges are exposed through global metrics and rewritten or retained consistently, but no rasterizer consumes them. |
| `EBSC`, `hdmx`, `LTSH`, `VDMX` | **Inspect-only** | Device/bitmap scaling metadata is bounded and parsed; no hint executor or rasterizer consumes it and compact output does not rewrite it. |
| `DSIG` | **Inspect-only** | Signature records are bounded and diagnosed. Cryptographic trust is not established, and signatures are removed when bytes change. |
| `meta`, `PCLT`, `MERG` | **Inspect-only** | Bounded semantic parsing is implemented internally; no public behavior executor or glyph-remapping writer is offered. |
| AAT `acnt`, `ankr`, `bdat`, `bhed`, `bloc`, `bsln`, `feat`, `fdsc`, `fmtx`, `fond`, `gcid`, `hsty`, `just`, `kerx`, `lcar`, `ltag`, `mort`, `morx`, `opbd`, `prop`, `trak`, `xref`, `Zapf` | **Preserved raw** | Identified as unsupported AAT data. Safe metadata-only tags `feat`, `fdsc`, `ltag`, and `trak` may be copied by the explicit safe-unknown policy with preserved GIDs; shaping tables are not executed. |
| Graphite `Feat`, `Glat`, `Gloc`, `Silf`, `Sill` | **Preserved raw** | Identified as unsupported Graphite data. `Feat` and `Sill` may be copied by the explicit safe-unknown policy with preserved GIDs; Graphite shaping is not executed. |
| Any unrecognized SFNT tag | **Preserved raw** | Bounded raw access is available when enabled. Unknown tables are not copied into compact subsets and are not assumed safe merely because their bytes are valid. |

## Raw preservation and output rules

`FontLoadOptions` preserves bounded raw tables by default. Disabling raw
preservation does not disable structural validation; it only removes raw table
access and later pass-through eligibility.

Static instancing and subsetting are different operations. Static instancing can
copy tables whose glyph identities remain unchanged, except invalidated tables
such as `DSIG` and resolved live-variation tables. Compact subsetting changes
glyph IDs, so every retained table that can refer to glyphs needs a semantic
rewriter. The subset builder therefore:

- rewrites supported core, outline, mapping, metric, GSUB/GPOS/GDEF, COLR v0,
  SVG, and `sbix` data;
- copies selected layout/color or safe unsupported tables only when the policy
  also preserves glyph IDs (and, where required, embeds the full font);
- drops unknown or unsupported tables whose internal references cannot be
  proven safe; and
- reports retained and dropped tags through `FontSubsetTableReport` and stable
  diagnostics.

Raw preservation never turns an unsupported technology into an executed one.
Applications that require a table or format not listed as fully supported must
treat the relevant semantic operation as unavailable.
