# ttf

`ttf` is a planned pure-managed .NET library for safely reading, inspecting,
shaping, measuring, instancing, subsetting, and writing SFNT-based fonts. Its
primary use case is giving PDF generators deterministic, library-neutral font
programs, metrics, glyph mappings, and Unicode extraction data without a native
font dependency.

The project currently targets `net10.0` and is under active development. The
contracts below describe the intended public behavior; a capability is not
available merely because it appears in this README. Progress is tracked in
[`todo.md`](todo.md).

Practical, task-oriented documentation is available in the
[`docs/guides/`](docs/guides/README.md) guide index. It covers loading and
inspection, text and fallback decisions, variable instances and subsetting,
PDF artifact assembly, vertical output, color appearances, and embedding
rights. The current implementation boundary is summarized in the
[`support matrix`](docs/support-matrix.md), and compatibility across releases is
defined by the [`versioning policy`](docs/versioning.md).

## Scope

The library's supported scope is:

- Containers: scalar SFNT TrueType/OpenType fonts, TTC/OTC collections, WOFF 1,
  and WOFF 2. Unknown SFNT tables are exposed as bounded raw data and preserved
  when the selected operation can do so safely.
- Outlines: TrueType `glyf`, CFF 1, CFF 2, static and variable outlines,
  horizontal and vertical metrics, and monochrome outline extraction from color
  fonts.
- Text: Unicode text itemization, bidirectional processing, OpenType shaping,
  fallback, measurement, horizontal and vertical layout, and source-cluster
  preservation for searchable and copyable PDF text.
- Output: deterministic static instancing, subsetting and SFNT serialization,
  plus neutral Type 0/CID and restricted simple-TrueType artifacts for a PDF
  library to consume. This project does not create PDF objects or content
  streams itself.

Apple AAT and SIL Graphite tables may be identified, bounded, exposed, and
preserved, but their shaping programs are not executed. Standalone Type 1/PFB,
EOT, macOS resource-fork suitcase fonts, bitmap-only non-SFNT fonts, proprietary
font technologies, rasterization, hint execution, and operating-system font
installation or discovery are outside the core scope.

Production code uses only the .NET runtime and framework libraries. Native or
third-party tools may be used by tests as independent conformance and
differential oracles, but are not runtime dependencies of `src/`.

## Color glyphs in PDF output

Color glyphs use two coordinated PDF layers with different responsibilities.
The font resource layer embeds the selected static font instance and its
ordinary monochrome outlines as real PDF text. Glyph codes, advances,
`ToUnicode` mappings, and any required `ActualText` retain search, copy,
selection, and accessibility semantics. A PDF generator must not replace that
text layer with only paths, SVG, or bitmap images.

The visible color rendering is separate appearance content owned by the
consuming PDF generator. For each positioned glyph, the generator can call
`new ColorGlyphResolver().ResolveGlyphWithOptions` with the same `FontInstance`,
glyph ID, palette, and foreground color used for layout.
`ColorGlyph.GetPdfAppearanceGuidance()`
then distinguishes these cases:

- `DrawVectorAppearance`: translate the normalized COLR paint graph into PDF
  vector operations.
- `DrawSvgAppearance`: convert the sanitized SVG document into supported PDF
  vector or image content.
- `DrawImageAppearance`: place the selected `sbix`, CBDT, or EBDT image at the
  glyph position.
- `DrawImageAndOutlineAppearance`: place the image and also paint the ordinary
  outline as required by the source `sbix` flags.
- `UseTextGlyph`: paint only the ordinary embedded-font glyph when no separate
  color appearance is selected.

The appearance must use the text glyph's resolved origin, advance, scale,
writing direction, and transforms so reflow and hit testing remain aligned.
When a separate color appearance is visible, the generator normally keeps the
corresponding PDF text object for semantics but suppresses its monochrome paint,
for example with PDF text rendering mode 3. In tagged output, the color
appearance should be treated as an artifact of that text rather than as a
second textual reading-order item. If the chosen PDF profile or generator
cannot express a representation safely, it should omit the separate appearance
and show the monochrome text glyph instead.

Embedding COLR, CPAL, SVG, or bitmap tables in a font program is not itself the
color-rendering contract and must not be assumed to produce portable viewer
behavior. The consuming generator owns PDF objects, graphics state, clipping,
transparency, image decoding, tagging, and profile compatibility. Font
embedding rights still apply to the monochrome font resource and are not
changed by emitting separate appearance content.

## Test-oracle dependency policy

The dependency boundary is the project boundary, not the repository boundary:

- `src/` may reference only the .NET runtime and framework. It must not reference
  a test project, NuGet package, native binding, external executable, downloaded
  tool, source generator, or analyzer package.
- `tests/` may use managed packages, native libraries, command-line programs, and
  language runtimes as independent conformance or differential oracles. Expected
  uses include comparisons with HarfBuzz, FreeType, fontTools, and PDF validators.
- Oracle adapters, downloads, caches, generated comparison output, and licenses
  remain under test-support directories. Production code never invokes an
  oracle, probes for one, or changes behavior based on whether one is installed.
- Every external corpus or oracle used for a reproducible test must pin its
  version and inputs and record provenance, license information, and integrity
  data. A missing optional oracle may skip its explicitly labeled differential
  test, but must not hide failures in dependency-free unit tests.
- A deterministic, reviewed artifact generated from pinned normative data may be
  checked into `src/` when the runtime library needs it. The generator and its
  dependencies remain test-side, and the checked-in artifact must not require
  them at build time or runtime.

An oracle establishes an independent expected result; it is never a production
fallback implementation. Adding a dependency to `tests/ttf.Tests.csproj` does
not authorize adding the same dependency to `src/ttf.csproj`.

## Shaping compatibility and Unicode data

The managed shaper executes OpenType `cmap`, GSUB, GPOS, and GDEF behavior. It
selects dedicated plans for Arabic/Syriac, Hebrew, Indic, Khmer, Myanmar,
Thai/Lao, Hangul, and scripts covered by the Universal Shaping Engine, with a
default plan for other scripts. The differential corpus records numeric output
from `hb-shape` 14.3.0. That version is a pinned test oracle, not a runtime
dependency or a promise that every HarfBuzz release produces identical output.

Supported corpus cases are expected to agree with the pinned oracle for glyph
IDs, clusters, advances, and offsets. The following differences are intentional:

- Shaped source locations are absolute UTF-16 ranges into caller text. They
  retain the original ranges and Unicode scalars across normalization,
  reordering, ligatures, and inserted shaping glyphs. HarfBuzz cluster numbers
  instead depend on its input encoding and selected cluster level.
- NFC normalization is the default and can be disabled explicitly. HarfBuzz
  does not itself provide this caller-facing normalization contract.
- Malformed UTF-16 is rejected by default. Replacement mode substitutes U+FFFD
  while retaining the malformed code unit's original one-unit range.
- An unmapped default-ignorable character is omitted from the glyph stream but
  remains represented by a source cluster with no glyphs. It does not generate
  a missing-glyph diagnostic.
- Broken Khmer and Myanmar clusters receive the configured dotted-circle
  fallback. Thai/Lao legacy PUA fallback is disabled in the high-level shaping
  path; the library does not guess a platform-specific legacy mapping.

Apple Advanced Typography and SIL Graphite shaping programs are not executed.
The loader can bound, expose, diagnose, and preserve their tables, and shaping
reports `UnsupportedShapingTechnology` when it finds `morx`, `mort`, `Silf`,
`Glat`, or `Gloc`. If usable OpenType data is also present, the OpenType path is
still deterministic; an AAT- or Graphite-only result must not be treated as
equivalent to execution by an engine that supports that technology.

Runtime Unicode behavior is pinned to Unicode 17.0.0 by the version and input
fingerprint stored in `src/Unicode/UnicodeData.bin`. The binary is generated
from the archived UCD named in
[`tests/Tools/UnicodeData/inputs.json`](tests/Tools/UnicodeData/inputs.json), and
its reproducible upgrade procedure is documented in
[`tests/Tools/UnicodeData/README.md`](tests/Tools/UnicodeData/README.md). Unicode
data is never downloaded or generated during a production build. Upgrading it
is a deliberate compatibility change that requires regenerated data, updated
conformance pins, and review of shaping and layout differences.

## Compatibility tiers

Loading behavior is selected explicitly; it never depends on a process-global
setting.

| Tier | Intended use | Structural handling | Diagnostics and output |
| --- | --- | --- | --- |
| Strict | Validation, authoring, and security-sensitive ingestion | Reject every violated invariant required by the supported specifications, including malformed offsets, lengths, overlaps, required ordering, and required checksums. Unsupported semantic technology remains distinguishable from malformed data. | Repairs are not performed. Fatal findings stop the operation with stable diagnostics. Successfully emitted output is canonical. |
| Compatible | Normal production use with real-world fonts | Enforce all memory-safety and configured resource limits, while accepting well-understood, unambiguous inconsistencies that can be isolated or repaired without guessing text, glyph, or metric semantics. | Every accepted inconsistency or repair is reported. Rewritten output is canonical and must not reproduce unsafe structure. |
| Inspect-only | Forensics and metadata/raw-table inspection | Parse only structures whose declared regions can be bounded safely. Preserve accessible raw data even when semantic parsing or later font operations are unavailable. Limits are never relaxed. | Report malformed and unsupported data, and disable shaping, instancing, subsetting, or serialization when their prerequisites are not trustworthy. |

No tier permits out-of-bounds access, unchecked allocation, unbounded recursion,
or decompression beyond configured limits. Compatibility mode is not a license
to silently invent missing semantic data, and inspect-only mode does not imply
that a malformed font can be rendered or re-emitted.

## Input ownership

Safe ownership is the default for every loader:

- File and stream inputs are read synchronously into library-owned immutable
  storage. A stream is read from its current position and is never closed or
  retained by the library.
- `byte[]`, `ReadOnlyMemory<byte>`, and `ReadOnlySpan<byte>` inputs are copied
  before a successful load result is returned. Later caller mutation cannot
  change a loaded font.
- Faces, tables, and parsed views refer only to bounded regions of that owned
  storage. Returned mutable arrays are copies rather than aliases of internal
  font data.
- A future trusted-input option may explicitly retain caller-provided immutable
  memory to avoid a copy. That mode must be opt-in, must state the lifetime and
  immutability obligation, and is excluded from the default safety guarantee.

The library never takes ownership of a caller's stream and does not require
loaded packages or faces to implement `IDisposable` merely to manage input
bytes.

## Load policies and cancellation

`FontLoadOptions` makes each policy independent and visible. The predefined
strict, compatible, and inspect-only factories select matching `FontLimits`,
copy their input, and preserve bounded raw table data. Callers may construct a
custom option set when they need different limits or retention behavior.

- Raw-table preservation keeps bounded bytes for known, unknown, and unsupported
  tables available for inspection and eligible for later serialization. Turning
  it off does not relax validation; it only prevents raw-table access and
  pass-through after semantic parsing.
- Strict mode performs no repairs and requires valid checksums. Compatible mode
  may apply only deterministic, unambiguous safe repairs, and every repair is
  diagnosed. Inspect-only mode does not repair structures or present invalid
  semantic data as usable.
- Checksum handling is explicit: require valid checksums, report mismatches while
  continuing when structurally safe, or skip input checksum calculation. Output
  writers always generate correct checksums regardless of the input policy.
- Cancellation is synchronous and is checked before I/O and at bounded points in
  long parsing or decompression loops. Cancellation throws
  `OperationCanceledException` and never returns a partial package. A caller's
  stream remains open and is not rewound, so its position reflects bytes already
  consumed.

The retain-read-only-memory ownership option is trusted-input mode. It applies
only to an immutable-memory overload; file, stream, array, and span inputs remain
copied. The caller must keep retained memory alive and unchanged for the entire
lifetime of every package, face, and view backed by it.

## Deterministic-output contract

Given identical input bytes, face selection, variation coordinates, options,
requested text/glyph usage, and library version, a successful operation
produces byte-for-byte identical serialized artifacts across processes,
machines, and cultures on supported .NET runtimes.

This contract covers subset and statically instanced font programs, table order
and padding, search fields and checksums, glyph and CID maps, width data,
ToUnicode CMaps, PDF subset names, diagnostics order, and resource-deduplication
keys. Implementations therefore use explicit stable ordering and rounding,
invariant encodings, normalized coordinate keys, and no filesystem font state,
current culture, mutable global configuration, random values, wall-clock values,
or collection enumeration order as output inputs.

The contract does not promise identical bytes between library versions when a
documented bug fix, specification update, or Unicode-data update changes the
canonical result. Callers that require reproducible archives should pin the
library version, Unicode-data version, inputs, and all options. Diagnostics that
describe external I/O failures may contain environment-specific context, but
their code and ordering remain stable for the same parsed input and policy.

## Static-instance FeatureVariations policy

Static instancing resolves FeatureVariations in every retained GSUB and GPOS
table. It does not drop a complete layout table merely because that table has
conditional feature data, and it does not retain a FeatureVariations table after
removing `fvar`.

Conditions are evaluated against the instance's final normalized coordinates in
`fvar` axis order, including `avar` mapping. Records are considered in source
order. Conditions in one set are conjunctive, range endpoints are inclusive,
and a null or empty condition set is universal. An unsupported condition format
or invalid axis index makes that record fail to match. A matching record whose
FeatureTableSubstitution version is unsupported is skipped as required by
OpenType; the next record is then considered. The first matching record with a
supported substitution table wins. If no record wins, the default FeatureList
remains selected.

For the winning record, each listed alternate Feature table replaces the
corresponding default FeatureList entry. Unlisted entries, ScriptList data, and
the LookupList remain unchanged, and alternate FeatureParams data is preserved
with its feature. This selection changes feature definitions only: static
instancing does not execute GSUB/GPOS lookups or alter glyph closure.

The resolved GSUB or GPOS table is serialized canonically without its
FeatureVariations data and with a version 1.0 header. A structurally malformed
table, an invalid selected substitution, or a layout version for which the
writer cannot prove that no live variation dependency remains makes static
instancing fail. Silently keeping unresolved conditional data or silently
discarding all layout behavior is not a compatibility fallback. Dropping a
complete layout table is allowed only under a separate, explicit output policy.

## Delivery and acceptance matrix

Priorities are cumulative: P1 acceptance includes P0, and P2 acceptance includes
P0 and P1. P3 interoperability hardening can continue after the P0-P2 product
contract is complete.

| Priority | Deliverable | Acceptance gate |
| --- | --- | --- |
| P0 — PDF-safe core | Bounded SFNT/TTC loading; required metadata and metrics; Unicode `cmap`; TrueType outlines and component closure; basic horizontal shaping; deterministic TrueType subsetting; Type 0/CIDFontType2 PDF artifacts with glyph/CID maps, widths, descriptor data, and ToUnicode. | Representative static TrueType and collection fonts load within limits; malformed inputs fail deterministically; basic Unicode text maps, shapes, and measures correctly; subsets reopen with retained outlines and metrics; assembled test PDFs render and extract the source text; identical inputs produce identical bytes. |
| P1 — production typography | Complete GSUB/GPOS/GDEF execution; bidi and script-specific shaping; cluster-safe fallback; reusable line layout and vertical text; CFF 1; embedding-rights reporting; complete PDF artifact mappings including multi-code-point extraction and `ActualText` recommendations. | Supported-script results match pinned HarfBuzz expectations for glyph IDs, clusters, advances, and offsets; CFF/TrueType subsets reopen in this library and independent tools; horizontal and vertical PDFs render and extract correctly in at least two independent consumers or validators; rights restrictions are never silently overridden. |
| P2 — broad format support | WOFF 1/2 and collections; variable TrueType/CFF 2 parsing and deterministic static instancing; advanced metrics/layout data; color and bitmap glyph exposure; retained-layout/color closure; deterministic round-trip writing and unknown-table preservation where safe. | Relevant normative WOFF, OpenType, and Unicode conformance data passes; static instances agree with pinned fontTools/FreeType results and retain no live variation dependency; color/variation closure is bounded and policy-controlled; generated outputs reopen independently and remain byte-identical for identical inputs and options. |

Across every priority, production source must remain free of non-framework
dependencies, public APIs must follow [`syntax.md`](syntax.md), failures must
respect configured limits, and behavior covered by normative data or an
independent implementation must have corresponding conformance or differential
tests.

## Repository map

- `docs/guides/` contains task-oriented user guides and end-to-end examples.
- `docs/support-matrix.md` records implemented, inspection-only, raw-preserved,
  and unsupported formats, features, and tables.
- `docs/versioning.md` defines release, Unicode, shaping, deterministic-output,
  diagnostic, and public-API compatibility promises.
- `src/` contains the dependency-free production library.
- `tests/` contains unit, conformance, differential, and regression tests and may
  host test-only external adapters and redistributable fixtures.
- `specs/` archives the authoritative specifications and provenance notes used by
  the implementation.
- `todo.md` is the ordered implementation plan and task-status record.
- `syntax.md` defines the C# syntax and public-API policy.
