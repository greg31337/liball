# Universal RAW / CR2 / NEF library development plan

## Goal and definition of “universal”

Build a dependency-free .NET 10 C# library that reads, inspects, validates, decodes, renders, edits, and writes:

- headerless or minimally wrapped `.raw` sensor/pixel dumps when the caller supplies an exact `RawDescriptor`;
- Canon TIFF-derived `.cr2` files across documented and corpus-confirmed camera profiles;
- Nikon TIFF-derived `.nef` files across documented and corpus-confirmed camera profiles;
- lossless structural rewrites that preserve unknown/private data whenever semantic decoding is unavailable.

“Universal `.raw`” cannot mean guessing arbitrary bytes from the extension alone: a headerless file contains no authoritative width, height, sample packing, byte order, CFA, stride, or color metadata. The library will probe recognizable containers, but generic `.raw` input requires a descriptor or sidecar. Likewise, CR2 and NEF are evolving proprietary format families. “Universal” means an extensible profile-driven implementation, precise capability reporting, safe fallback behavior, and no silent corruption—not a claim that undocumented future codecs can be decoded or generated automatically.

The primary outcome is a reusable library, not an editor UI or command-line application. DNG is used as a public raw-processing and tag reference; DNG file I/O is not in the requested v1 scope.

## Non-negotiable constraints

- `src/` uses only .NET built-ins. No NuGet/native codec, imaging, metadata, color-management, or compression dependency may be referenced by the production project.
- `tests/` may use external packages and command-line interoperability tools where licensing and CI availability permit.
- Implementation must follow `syntax.md`: public types are organized by category under `src/Api`, public data classes expose fields and only internal helper methods, and public operations are instance methods on interface implementations; project-declared structs/generics/properties/indexers/events/delegates remain forbidden.
- Source must avoid `ref`, `out`, `in`, nullable value type syntax and the null-forgiving operator, inheritance outside interface implementation and direct exception bases, LINQ, `try`/`catch`/`finally`, `unsafe`, iterators, patterns, named/default arguments, and async/`await`.
- Public APIs must use positional arguments, arrays or built-in collection types, ordinary synchronous instance methods, explicit result/report classes, and interfaces at stable operation boundaries.
- `Span<T>` and `ReadOnlySpan<T>` are preferred for bounded transient parsing/packing, `BinaryPrimitives` for endian-safe primitives, `ArrayPool<T>` for reusable large buffers, and checked arithmetic before every allocation/offset calculation.
- Do not create syntax-policy or source-checking tests. `syntax.md` is an implementation constraint, not a requested test feature.
- Do not create benchmarks, timing assertions, throughput tests, or a performance test project. Memory-bounded and streaming behavior is still a functional requirement.
- Public types, fields, enum members, constructors, and methods require XML documentation using only the XML constructs allowed by `syntax.md`.

## Research-derived scope

| Project/reference | Relevant feature set adopted into this plan |
|---|---|
| [LibRaw](https://www.libraw.org/docs) | Format probing, raw unpacking, camera/profile data, raw and processed image access, multiple thumbnails, black/white levels, white balance, demosaic selection, highlight handling, denoise, exposure, gamma, color-space output, and explicit unsupported-camera errors. |
| [RawSpeed](https://github.com/darktable-org/rawspeed) | Decode from bounded memory, unmodified or normalized raw samples, arbitrary CFA descriptions, optical-black handling, active-area crop, camera aliases/profile data, strict input validation, and fuzz-oriented robustness. |
| [dcraw](https://www.dechifro.org/dcraw/) | Historical Canon/Nikon algorithms and the minimal end-to-end raw development pipeline; useful as an independent behavioral oracle, not as source to copy. |
| [libopenraw](https://libopenraw.freedesktop.org/) | File/metadata/thumbnail/raw-data separation and public descriptions of CR2/NEF organization. |
| [ExifTool](https://exiftool.org/) and [Exiv2](https://exiv2.org/) | Exif/IPTC/XMP and maker-note reading, metadata editing, preservation of unknown tags, and the warning that raw writes require strict compatibility testing. |
| [Adobe DNG specification/SDK](https://helpx.adobe.com/camera-raw/digital-negative.html) | TIFF/EP-derived raw tags, raw-to-linear mapping, profiles, opcodes, validation, digest concepts, masked areas, preview metadata, and read/write separation. |
| [rawpy](https://letmaik.github.io/rawpy/api/rawpy.Params.html) | Practical processing controls: half-size output, camera/auto/user white balance, black/white overrides, brightness/exposure, highlight modes, gamma, chromatic-aberration correction, bad-pixel repair, and output bit depth/color space. |
| [darktable](https://www.darktable.org/about/features/) and [RawTherapee](https://rawpedia.rawtherapee.com/) | Full high-level pipeline: raw black/white point, defect correction, highlight reconstruction, denoise, demosaic, chromatic adaptation, camera color transform, lens corrections, scene-linear exposure/tone mapping, gamut handling, crop/orientation, and output conversion. |

Research documents downloaded for implementation are indexed in `specs/README.md`. Public standards are authoritative where applicable; proprietary reverse-engineering documents and source snapshots are interoperability evidence only.

## Compatibility contract

### Support levels

| Level | Meaning |
|---|---|
| Recognized | Container/signature and basic identity can be reported safely. |
| Inspected | IFD graph, standard metadata, private block locations, previews, and raw payload descriptors can be enumerated. |
| Decoded | Raw samples are reconstructed with known dimensions, packing/compression, CFA or channel layout, black/white information, and active area. |
| Rendered | A displayable linear or encoded RGB/gray image can be produced with selected pipeline options. |
| Preserving rewrite | Known metadata can be changed while unknown tags/blocks, raw payload, and maker-note offset semantics remain intact. |
| Encoded | A new payload/container can be emitted for a named, validated write profile. |
| Vendor-confirmed | Output passes this project’s structural checks and opens in named independent/vendor tools for the profile. |

Every opened file and every writer profile reports these levels independently. Unsupported compression never degrades to guessed pixels. Unknown metadata is returned as typed/opaque values and preserved by default.

### Initial format matrix

| Family/variant | Read target | Write target | Important notes |
|---|---|---|---|
| Headerless `.raw` integer samples | 1–32-bit packed or byte-aligned, signed/unsigned, planar/interleaved, row padding, arbitrary offset/order | Symmetric descriptor-driven write | Descriptor required; initial render path prioritizes 8–16-bit CFA/gray/RGB. |
| Headerless `.raw` floating samples | Half/single precision, planar/interleaved | Symmetric write | Built-in `Half`/`float`; NaN/infinity policy is explicit. |
| Common sensor packing | MSB/LSB bit streams; RAW10/12/14 groups; 10/12/14/16-bit words | Symmetric write | Packing identifiers are explicit, never inferred solely from length. |
| CR2 classic Bayer | TIFF/CR2 header, lossless JPEG, strips/slices, 12/14-bit | Lossless JPEG for corpus-confirmed profiles | Preserve Canon maker notes and unknown IFDs. |
| Early/old CR2 layouts | Old raw offsets and half-height/double-width cases | Preserving rewrite first; new encode only after profile confirmation | Model-specific behavior is profile data. |
| Canon sRAW/mRAW CR2 | Subsampled multi-component decode, interpolation, WB coefficients | Preserving rewrite; profile-gated encoding | Semantics differ from Bayer and vary by generation. |
| NEF uncompressed | Packed/unpacked 12/14/16-bit variants, strips/tiles, CFA/linear data | Uncompressed profile-driven encode | Safest new-NEF baseline. |
| NEF compressed/lossless | Nikon Huffman/delta tables, split rows, tone/linearization curves | Corpus-confirmed lossless profiles; lossy only with explicit opt-in | Must distinguish reversible and curve-quantized variants. |
| Older/small/linear NEF variants | Model-specific CFA, RGB/YCbCr/linear and preview layouts | Preserving rewrite; encode only when independently validated | Never force a Bayer interpretation. |
| Nikon HE/HE* NEF | Recognize, inspect metadata/previews, report codec requirement | Not enabled until a lawful public specification or independently implementable route exists | Explicit gated capability due undocumented codec/IP and current ecosystem limitations. |

## Architecture and data flow

```text
bounded input -> probe -> TIFF/RAW parse -> format profile -> metadata + payload map
                                              |                    |
                                              v                    v
                                      validation report      sample decoder
                                                                   |
                                               raw mosaic/planes <-+
                                                      |
              normalize -> repair -> WB -> demosaic -> color -> tone -> orient/crop
                                                      |
                                                 RGB/gray image

raw document + edit plan -> layout planner -> codec/opaque copy -> validate -> output
```

Parsing, decoding, rendering, metadata reconciliation, and writing are separate layers. Container parsing never applies image processing. Decoders produce sensor-domain samples plus a complete descriptor. Rendering is deterministic for a fixed profile/options set. Writers plan all offsets and sizes before emission when the destination is non-seekable, or use a bounded seek-and-patch strategy when allowed.

## Complete feature inventory

### 1. Input, probing, and resource ownership

- Open from path, seekable/non-seekable `Stream`, byte array, `ReadOnlyMemory<byte>`, or bounded `ReadOnlySpan<byte>` convenience entry points.
- Signature/content probing independent of filename; report mismatched extension without rejecting valid content.
- Generic `.raw` descriptor loading from API objects and an optional JSON sidecar using `System.Text.Json`.
- Caller-configurable limits for file size, dimensions, pixel count, IFD count/depth, tag count/value size, strip/tile count, metadata/XMP size, preview size, recursion, and decoded allocation.
- Full-read, metadata-only, preview-only, raw-only, and deferred-payload modes.
- Caller-owned stream semantics; no public type needs to implement forbidden `IDisposable` syntax.
- Synchronous cancellation checks through built-in `CancellationToken` at strip/tile/row boundaries.
- Reentrant decoding/rendering with no mutable global image state; independently opened documents are safe for concurrent use, profile catalogue mutation is locked, and optional managed parallelism remains deterministic.
- Deterministic diagnostics including format, offset, IFD path, tag, codec stage, and whether recovery was possible.

### 2. Binary primitives and bitstreams

- Bounds-checked little/big-endian reads/writes for signed/unsigned 8/16/32/64-bit integers, `Half`, `float`, and `double`.
- TIFF rational/signed-rational decode with numerator/denominator preserved exactly and safe floating conversion.
- Checked offset+length/count×element-size arithmetic using 64-bit file positions.
- Seekable window reader and bounded substream/memory views that cannot escape declared regions.
- MSB-first and LSB-first bit readers/writers, byte alignment, peek/consume, marker-aware reads, and zero/one padding validation.
- Canonical Huffman table construction, validation, fast prefix lookup, slow fallback, encode table generation, and malformed-tree rejection.
- Reusable bit unpack/pack kernels for arbitrary integer widths plus named RAW10/12/14 layouts.
- CRC/checksum/digest helpers required by supported metadata/container variants, using built-in cryptography/hash APIs.

### 3. TIFF/BigTIFF container core

- Classic TIFF and BigTIFF headers, both byte orders, all standard field types including 64-bit offset types.
- IFD chains, nested Exif/GPS/Interop IFDs, SubIFDs, unknown/private IFD pointers, cycle/alias detection, and stable IFD paths.
- Inline versus out-of-line values, exact raw byte preservation, typed scalar/array access, ASCII with terminator preservation, and undefined blobs.
- Strip/tile descriptions, planar/chunky configuration, fill order, predictor, sample format, extra samples, and multi-image/subfile discovery.
- Compression support needed by target files/previews: none, PackBits, LZW, Deflate/Adobe Deflate, baseline JPEG, old/new TIFF JPEG interpretation where encountered, and lossless JPEG.
- Unknown compression discovery and opaque-copy support without attempting decode.
- TIFF layout planner with sorting, alignment, deduplication policy, 32/64-bit offset selection, forward references, padding, and non-overlap checks.
- Deterministic TIFF writer with byte-order selection/preservation, strips/tiles, SubIFD graph emission, and unknown-tag round-trip.
- In-place fixed-size patch analysis versus complete rewrite; no unsafe in-place growth.

### 4. JPEG codecs and embedded images

- JPEG marker parser with byte stuffing, restart markers, DHT/DQT/DRI/SOF/SOS validation, component/sampling descriptions, and marker preservation.
- Predictive lossless JPEG (SOF3) decode/encode for 2–16-bit samples, predictors 1–7, point transform, multi-component scans, restart intervals, and TIFF/CR2 dimension conventions.
- Baseline sequential Huffman JPEG decode for embedded thumbnails/previews, including grayscale and YCbCr sampling.
- Baseline JPEG encode for generated thumbnails with explicit quality, sampling, restart, and metadata-free deterministic modes.
- YCbCr-to-RGB conversion with correct range, coefficients, chroma positioning, upsampling, and clipping.
- Return original compressed preview bytes without re-encoding whenever the caller asks for extraction only.

### 5. Generic `.raw` descriptors and codecs

- Descriptor fields for dimensions, depth, stored word width, sample kind, signedness, byte order, bit order, row/plane stride, data offset/length, channel count/order, planar/interleaved layout, CFA repeat pattern, active/masked area, orientation, black/white levels, and color/WB data.
- Presets for gray, RGB/BGR/RGBA/BGRA, Bayer RGGB/BGGR/GRBG/GBRG, arbitrary CFA grids, and common RAW10/12/14 packing.
- Validation that descriptor geometry exactly fits or intentionally leaves trailing bytes; trailing bytes can be exposed/preserved.
- Region/row/plane decode to caller buffers as stored integers, normalized `ushort`, or normalized `float`.
- Symmetric encode from stored/normalized sample buffers with explicit rounding, clamp, dithering-off-by-default, and signed/float policies.
- JSON sidecar read/write with versioning and preservation of unknown sidecar properties at the document level.
- Conservative descriptor inference suggestions based on file length and caller hints, always returned as ambiguous candidates rather than silently selected.

### 6. Canon CR2

- CR2 signature/version/IFD3 pointer parsing layered over TIFF.
- Discovery of raw IFD, Exif/GPS, Canon maker-note IFDs, JPEG thumbnail, full preview, and optional secondary images.
- Canon sensor info, active/border geometry, CFA, black/white level, WB coefficients, color data, lens/shooting data, model ID, and quality mode extraction.
- Lossless JPEG decode with CR2-specific component mapping, slice table reconstruction, unslicing, odd dimensions, and old-format layout fixes.
- Canon sRAW/mRAW recognition and generation-specific subsampled component interpolation/color conversion.
- Multiple raw/preview images where present, including dual-pixel/secondary raw planes and explicit image index/plane selection.
- Camera alias/profile matching by normalized make/model/unique ID plus observed structural fingerprint.
- CR2 write profiles describing required IFD topology, slice geometry, lossless-JPEG parameters, preview constraints, maker-note base rules, and known vendor quirks.
- New classic Bayer CR2 encoding for confirmed profiles, with caller-supplied provenance fields and no fabrication of unknown camera telemetry.
- Safe CR2 metadata rewrite with raw payload byte identity, preview reuse/replacement, unknown tag/block retention, maker-note anchoring or known-offset relocation, and post-write reopen validation.

### 7. Nikon NEF

- TIFF-derived NEF detection using make/model, raw IFD structure, and Nikon maker-note signatures rather than extension alone.
- Discovery of raw IFD/SubIFD, embedded JPEG previews/thumbnails, Exif/GPS, maker notes, Nikon Capture/Edit blocks, and optional secondary images.
- Nikon camera/lens/shooting data, serial/shutter data where documented, WB versions, color balance, crop/active area, CFA, black/white levels, tone/linearization curves, and compression mode extraction.
- Versioned Nikon color-balance/white-balance block deobfuscation where the serial/shutter-key scheme is independently documented, while retaining the original encrypted bytes.
- Uncompressed packed/unpacked decode across observed 12/14/16-bit endian and row-layout variants.
- Nikon Huffman/delta decompression for 12/14-bit lossy and lossless trees, split-row tree changes, predictors, curve lookup/quantization, and malformed-data validation.
- Older/small/linear/RGB/YCbCr NEF handling based on explicit profiles; monochrome sensors bypass demosaic.
- NEF write profiles describing IFD topology, strip/tile geometry, compression tables, curve representation, preview layout, maker-note bases, and required identity fields.
- New uncompressed NEF encoding for confirmed profiles, then profile-gated lossless compressed encoding with pixel-identity verification.
- Lossy NEF generation only through an explicitly named lossy option that reports irreversible curve quantization.
- Safe NEF metadata rewrite with raw payload byte identity, Nikon Capture data preservation, preview reuse/replacement, and maker-note anchoring/relocation rules.
- HE/HE* recognition and metadata/preview extraction with a precise unsupported-codec diagnostic until the legal/specification gate is satisfied.

### 8. Camera and format profiles

- Versioned built-in profile catalogue with provenance for each non-obvious value and no unreviewed copying from external databases.
- Make/model normalization, aliases, firmware/version constraints, structural fingerprint matching, and ambiguity reporting.
- Profile data for dimensions, stored/active/masked areas, CFA, black/white defaults, compression/packing, row quirks, WB tag versions, color matrices/illuminants, and writer layout.
- Runtime custom profile registration from a documented JSON schema so new cameras do not require a package release.
- Read profile and write profile are separate: successful decode never implies safe new-file encoding.
- Confidence/evidence fields and compatibility-tool/sample identifiers for each write profile.
- Profile override precedence, conflict diagnostics, and opt-in experimental profiles.

### 9. Metadata

- Unified typed view plus access to the original TIFF/Exif/GPS/Interop/Canon/Nikon/IPTC/XMP tag locations.
- Exact integer, rational, floating, ASCII, UTF-8/UTF-16, byte blob, date/time text, and array preservation without forced lossy conversion.
- Exif 3.1 and GPS read/write, including offset time, subsecond, lens/camera, exposure, sensitivity, focal-plane, user comment, and interoperability fields.
- Canon and Nikon maker-note decoding by version/profile while retaining every undecoded byte and original offset-base semantics.
- XMP packet discovery, UTF encoding, RDF/XML parsing with safe `XmlReader` settings, standard schema access, padding preservation, and packet replacement.
- IPTC IIM record parsing/writing and IPTC Core/Extension mapping through XMP.
- ICC profile discovery, header/tag-table validation, byte-preserving access, and supported transform parsing.
- Exif/XMP/IPTC reconciliation policy with caller-selectable precedence, conflict reporting, synchronized writes, and “do not reconcile” mode.
- Metadata edit sets, remove/replace/add operations, privacy scrub presets, timestamp/GPS handling, and unknown-field preservation.
- Preview/thumbnail metadata consistency, orientation propagation, and image dimension updates after a caller-authorized pixel replacement.
- Metadata-only reads that never decode the raw image.

### 10. Raw-domain image model and preprocessing

- Stored sample buffer and normalized sensor-domain buffer, both with width/height/channels/stride and explicit ownership.
- CFA model supporting arbitrary repeating grids, plane colors, two green planes, monochrome, and non-CFA multi-channel data.
- Active/default/user crop, masked areas, optical-black rows/columns, and orientation represented without destructive application.
- Black-level subtraction using scalar, per-channel, repeating-grid, per-row, and per-column data; optional optical-black estimation.
- White-level normalization, linearization/tone table application, clipping statistics, and preservation of original values when requested.
- Bad/dead/hot pixel repair from metadata, caller lists, and conservative neighborhood detection.
- Row/column defect correction, green-channel equilibrium, and optional banding correction driven by masked pixels.
- Lens shading/gain map and vignette correction when embedded metadata provides enough information.
- Raw chromatic-aberration scaling before demosaic.
- Half-size/fast preview path that combines CFA cells without full interpolation.
- Raw histogram, per-channel minima/maxima/percentiles, clipping masks, and diagnostic image planes.

### 11. Demosaic and multi-component reconstruction

- No-op paths for monochrome, already-linear RGB, and caller-requested mosaic output.
- Nearest-neighbor and bilinear Bayer demosaic as reference/simple modes.
- Edge-aware/VNG-quality Bayer mode for balanced quality and implementation complexity.
- AHD-class high-quality Bayer mode with artifact/false-color suppression.
- Four-color mode retaining separate green planes until color conversion.
- Border handling, odd crop origins, arbitrary Bayer phase, and orientation-independent results.
- Canon sRAW/mRAW chroma interpolation separate from Bayer demosaic.
- Pluggable-by-enum dispatch within the assembly; no project-defined interface or inheritance hierarchy is required.

### 12. White balance, color, and tone processing

- As-shot camera WB, daylight/profile WB, auto WB over configurable regions, and user multipliers/temperature-tint input.
- Chromatic adaptation with documented matrix math and selectable Bradford/CAT-style mode.
- Camera-space to XYZ using one/two/three-illuminant profile interpolation, forward/reduction/calibration matrices where available.
- Built-in output spaces: raw camera, XYZ D50/D65, linear sRGB, sRGB, linear/display P3, Adobe RGB, ProPhoto RGB, Rec.2020, and ACEScg where matrices are well-defined.
- ICC matrix/TRC input/output profiles first, then LUT profile transforms with tetrahedral interpolation and rendering intent where implementable from ICC.1.
- Exposure compensation, highlight clip/blend/reconstruct modes, shadow scaling, automatic brightness with clipping threshold, and per-channel saturation guard.
- Scene-linear denoise options: hot-pixel, median chroma, and wavelet-like luminance/chroma modes implemented in managed code.
- False-color suppression, post-demosaic chromatic-aberration correction, and optional capture sharpening.
- Tone curve, gamma/transfer function, contrast, gamut clipping/compression, and optional ordered dithering for 8-bit output.
- Rotate/flip/orientation, crop, and bilinear/bicubic/Lanczos resampling after sensor/color operations.
- Deterministic 8-bit, 16-bit, and float gray/RGB/RGBA output buffers with documented channel order and alpha behavior.

### 13. Writing and editing safety

- Three explicit modes: create from pixels, full rewrite, and fixed-size in-place patch when proven safe.
- A dry-run write plan listing changed IFDs/tags, copied/re-encoded blocks, relocated offsets, dropped data, compatibility profile, expected length, and warnings.
- Unknown/private blocks copied byte-for-byte by default with original alignment and offset-base constraints recorded.
- Maker-note strategies: fixed-offset anchor, recognized-relative relocation, or reject rewrite; never guess.
- Payload identity mode verifies raw compressed bytes before/after metadata-only rewrites.
- Pixel identity mode reopens output and compares decoded stored samples for lossless writers.
- Deterministic tag order/layout and stable serialization for identical inputs/options, excluding caller-requested timestamps/identifiers.
- Seekable and non-seekable output; staged buffering/spooling is bounded by caller limits.
- Destination-path writes use a sibling temporary file and atomic replace/move only after validation; failed validation leaves the original untouched and reports the recoverable temp path.
- Write compatibility validation against the selected profile plus warnings for software/version behaviors observed in the corpus.
- Explicit data-loss consent for metadata removal, preview replacement, lossy compression, bit-depth reduction, crop baking, or unknown-block omission.
- Authenticity/signature/original-decision blocks are preserved opaquely when possible; edits report signature invalidation, and the library never fabricates or re-signs vendor authenticity data.

### 14. Validation, diagnostics, and security

- Structural validation of headers, IFD graphs, types/counts, offsets, overlaps, alignment, required tags, strip/tile coverage, and payload boundaries.
- Codec validation of Huffman trees, markers, predictors, restart state, bit consumption, decoded sample count, and sample range.
- Semantic validation of dimensions, crop/masked regions, CFA, black/white values, WB, matrices, orientation, preview references, and metadata consistency.
- Format/profile conformance reports with error/warning/info severity and machine-readable codes.
- Strict mode rejects any non-conformance; tolerant mode applies only named, reported recovery rules.
- Defense against integer overflow, recursive/cyclic IFDs, decompression bombs, allocation bombs, pathologically large metadata, malformed XML, external XML entities, and truncated streams.
- No network access, dynamic code loading, external entity resolution, or executable metadata behavior in production.
- Fuzz/property tests for parsers, bit packers, Huffman codecs, offset planners, and round trips; fuzzing is correctness/security testing, not performance testing.
- Failure diagnostics retain stage/offset/context without including unrelated image metadata that could leak private information.

### 15. Test and interoperability coverage

- Unit tests for every primitive, tag type, packing, predictor, compression codec, matrix operation, and processing stage.
- Synthetic minimal TIFF/CR2/NEF builders in tests to isolate edge cases without production test helpers.
- Golden samples for each supported profile/variant with documented provenance and redistribution status; private corpora represented by hashes/manifests only.
- Decode comparison against independent tools such as LibRaw/dcraw/ExifTool/vendor applications where available, invoked only from tests/tooling.
- Metadata round-trip comparison against ExifTool/Exiv2 and byte-level unknown-block checks.
- New-file interoperability matrix recording which applications/versions accept each write profile.
- Truncation/corruption/mutation corpus, limit tests, cancellation tests, non-seekable stream tests, and hostile XML tests.
- Pixel-exact round trips for generic RAW and lossless CR2/NEF; error-bound tests only for explicitly lossy paths.
- Cross-platform tests on supported .NET platforms without relying on `System.Drawing` or platform image codecs.
- No syntax-policy/source-scanning tests and no benchmarks or timing thresholds.

## Public API completion tasks

- [DONE] T001: Add uniquely named byte-array, `ReadOnlySpan<byte>`, bounded caller-owned `Stream`, and path operations for probing, full opening, metadata-only opening, preview-only opening, decoding, rendering, and validation; preserve stream ownership, support seekable and non-seekable inputs, and enforce limits and cancellation consistently.
- [DONE] T002: Add public service operations for creating the gray, RGB/BGR/RGBA/BGRA, standard Bayer, arbitrary-CFA, and RAW10/12/14 descriptor presets and for validating descriptors, computing bounded row/payload/trailing-byte geometry, creating and querying CFA patterns, and creating, validating, comparing, and converting signed and unsigned rationals.
- [DONE] T003: Complete generic RAW decoding in the public codec contract for stored unsigned, stored signed, stored floating, normalized `ushort`, and normalized `float` destinations over regions, rows, channels, and planes from bounded memory and streams, using caller-owned buffers and `Span<T>` internally where appropriate.
- [DONE] T004: Complete generic RAW writing in the public writer contract with dry-run plans and caller-owned stream/path destinations for every existing stored and normalized input form, retaining explicit rounding, range, signed, non-finite, padding, and trailing-byte policies.
- [DONE] T005: Complete the generic sidecar public workflow with bounded memory and caller-owned stream read/write operations, deterministic UTF-8 serialization, schema validation, unknown top-level property preservation, cancellation, and size-limit enforcement.
- [DONE] T006: Add public document operations to retrieve selected thumbnails, extract any mapped payload without exposing parser internals, decode baseline-JPEG thumbnails/previews to `RawRgbImage`, and encode rendered gray/RGB images into deterministic baseline-JPEG `RawThumbnail` values suitable for CR2/NEF write inputs.
- [DONE] T007: Replace the public document render baseline with the complete preprocessing, demosaic or sRAW/mRAW reconstruction, white-balance, color, exposure/highlight, detail, tone/gamut, crop/orientation, resampling, and output-conversion pipeline; add explicit image/plane and user-crop selection and a public render-from-`RawSensorImage` operation.
- [DONE] T008: Complete the public processing surface with uniquely named operations for optical-black estimation, preprocessing an existing sensor image, crop resolution, reconstruction, white balance, camera-to-XYZ and built-in output conversion, ICC-to/from-XYZ conversion, exposure/highlight handling, detail enhancement, tone/gamut processing, final orientation/resampling/bit-depth conversion, and histograms over stored, sensor, and rendered images.
- [DONE] T009: Add the planned masked-pixel-driven banding correction to preprocessing options, results, and the public processing workflow, with deterministic row/column correction, disabled-stage identity, limit, cancellation, and incomplete-calibration behavior.
- [DONE] T010: Add public metadata utility operations for exact typed `RawTagValue` creation and extraction across every TIFF field type, text encoding and raw-byte copying, IFD/tag lookup, Exif timestamp conversion, and exact signed/unsigned rational handling without lossy coercion.
- [DONE] T011: Add public bounded parse, inspect, lookup, replacement, and encode operations for standalone XMP packets, IPTC IIM records/DataSets, and ICC profiles, preserving original bytes, encodings, packet padding, unknown fields/tags, and safe XML settings.
- [DONE] T012: Add public metadata reconciliation, unified-field lookup, edit dry-run/apply, privacy scrub preset, timestamp/GPS removal, preview replacement, and Canon/Nikon maker-note lookup/edit/relocation-plan operations so preserving CR2/NEF rewrites can be configured without internal access.
- [DONE] T013: Complete the profile public layer with a reviewed built-in-catalogue factory, built-in/custom source reporting for both read and write profiles, profile-document validation and deterministic JSON read/write, and caller-accessible matching, ambiguity, override, provenance, evidence, compatibility, and experimental-state results.
- [DONE] T014: Make all CR2 and NEF create/rewrite destinations symmetric in the public writer contract by exposing bounded caller-owned stream and staged path operations, with and without a caller profile catalogue, while leaving streams open and preserving post-write validation, raw-payload identity, pixel-identity/error-bound, and recoverable-temporary-file results.
- [DONE] T015: Add public dry-run validation and application of eligible fixed-size TIFF/CR2/NEF metadata patches, including exact tag-value sizing, overlap and offset-base rejection, authenticity invalidation, explicit data-loss consent, source-byte preservation, and reopen validation.
- [DONE] T016: Expand public validation operations to cover bounded memory, streams, paths, descriptors, option sets, profile documents, metadata packets, image models, and write plans with selectable structural, codec, semantic, profile, metadata, and writer categories in strict or tolerant mode.
- [DONE] T017: Add black-box public codec tests covering every input transport and read mode, content/extension mismatch, generic descriptor requirement, image/plane selection, deferred payload behavior, caller ownership, non-seekable input, limits, cancellation, diagnostics, capabilities, and unsupported-codec reporting.
- [DONE] T018: Add black-box public generic RAW and sidecar tests covering every descriptor preset, CFA phase, sample kind, packing, byte/bit order, planar/interleaved layout, region/row/plane decode form, encode form, rounding/range/non-finite/trailing-byte policy, memory/stream/path round trip, invalid geometry, and unknown JSON property preservation.
- [DONE] T019: Add black-box public metadata and profile tests covering every typed TIFF value, Exif/GPS timestamp form, XMP/IPTC/ICC standalone round trip, reconciliation precedence and conflicts, add/replace/remove/scrub edits, preview and maker-note edits, built-in/custom profile registration and override, ambiguity, provenance/evidence, and preservation of unknown bytes.
- [DONE] T020: Add black-box public processing and rendering tests covering every preprocessing toggle, banding correction, demosaic and sRAW/mRAW path, white-balance mode, color space and transfer function, ICC intent, exposure/highlight and detail mode, tone/gamut mode, crop/orientation/resampler, output depth/channel layout, all histogram inputs, stage ordering, disabled-stage identity, limits, and cancellation.
- [DONE] T021: Add black-box public writer tests covering generic plans, CR2/NEF create and preserving rewrite through memory/stream/path destinations, fixed-size patch acceptance and rejection, unknown/private/authenticity preservation, preview replacement, consent gates, deterministic output, non-seekable output, staged failure recovery, reopen validation, and lossless or explicitly lossy pixel reports.
- [DONE] T022: Add public-surface integration and corpus tests for every enabled generic RAW, CR2, and NEF capability/profile, including independent test-only interoperability checks where available, then collect line and branch coverage through public entry points and close every reachable public-layer coverage gap without source-policy, performance, benchmark, documentation, or release tests.

## Release gates

1. Generic RAW is releasable when every advertised descriptor/packing round-trips exactly and ambiguous input is never guessed silently.
2. A CR2/NEF read profile is enabled only with redistributable or privately hashed corpus evidence, independent decode comparison, metadata validation, and hostile-input coverage.
3. A CR2/NEF write profile is enabled only after parse-write-parse, payload/pixel identity as applicable, metadata/unknown-block preservation, and named application compatibility checks.
4. Experimental profiles require explicit opt-in and never appear as vendor-confirmed.
5. HE/HE Star support remains recognition/metadata/preview-only; the completed review in `specs/nikon-he-legal-review.md` found no cleared decoder/encoder basis, and any re-review must satisfy its documented gates.
6. The package ships no production dependency beyond .NET, no syntax-policy tests, and no performance/benchmark tests.
