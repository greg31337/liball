# Universal WebP Reading / Writing Library Development Plan

Created: 2026-08-05

## Goal

Build a dependency-free .NET WebP library that can read, inspect, edit, and write WebP files from low-level RIFF chunks through high-level still-image and animation workflows. Implementation must use only built-in .NET APIs and must follow the simplified C# subset in `syntax.md`.

## Research Sources

- Archived authoritative specifications and reference descriptions: `specs/SOURCES.md`
- WebP overview, support matrix, and reference implementation notes: https://developers.google.com/speed/webp
- WebP RIFF container specification: https://developers.google.com/speed/webp/docs/riff_container
- WebP lossless bitstream specification: https://developers.google.com/speed/webp/docs/webp_lossless_bitstream_specification
- VP8 lossy bitstream specification: https://www.rfc-editor.org/rfc/rfc6386
- libwebp image encode/decode API: https://developers.google.com/speed/webp/docs/api
- libwebp container mux/demux API: https://developers.google.com/speed/webp/docs/container-api
- cwebp encoder options: https://developers.google.com/speed/webp/docs/cwebp
- webpmux metadata and animation operations: https://developers.google.com/speed/webp/docs/webpmux
- SixLabors ImageSharp WebP API surface: https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Formats.Webp.html
- ImageSharp format and animation option patterns: https://docs.sixlabors.com/articles/imagesharp/imageformats.html and https://docs.sixlabors.com/articles/imagesharp/animations.html
- ImageMagick WebP encoder option set: https://imagemagick.org/webp/

## Complete Feature Inventory

### Low-Level Binary Infrastructure

- RIFF file signature detection for `RIFF....WEBP`.
- WebP MIME and extension detection helpers.
- Little-endian byte, uint16, uint24, int24, uint32, and signed field readers.
- Little-endian byte, uint16, uint24, int24, uint32, and signed field writers.
- Bit readers for least-significant-bit-first WebP lossless streams.
- Bit readers for most-significant/boolean arithmetic VP8 streams.
- Chunk header parsing with FourCC and payload length.
- RIFF chunk padding validation and writing.
- Bounds checking for every read and write.
- Overflow checking for file sizes, canvas sizes, stride math, frame rectangles, and pixel counts.
- Configurable strictness for reserved bits, malformed padding, trailing data, unknown chunks, and chunk order.
- Diagnostic collection with offset, chunk FourCC, severity, and message.
- Streaming read helpers for seekable and non-seekable streams.
- Span-first internal decode paths when data is already in memory.
- Chunk payload copy helpers for user-owned metadata and unknown chunk preservation.

### Container Demuxing

- Basic WebP single-image containers containing `VP8 ` or `VP8L`.
- Extended `VP8X` containers.
- Feature flag parsing for ICC, alpha, Exif, XMP, animation, and reserved bits.
- Canvas width and height parsing from 24-bit one-based fields.
- Validation of maximum RIFF size, maximum canvas product, and format-specific dimensions.
- Chunk order validation for reconstruction chunks: `VP8X`, `ICCP`, `ANIM`, `ANMF`, `ALPH`, `VP8 `, `VP8L`.
- Metadata and unknown chunks accepted out of reconstruction order.
- `ICCP` color profile chunk extraction.
- `EXIF` metadata chunk extraction.
- `XMP ` metadata chunk extraction.
- Unknown chunk enumeration, retention, replacement, and stripping.
- Multiple frame demuxing through `ANIM` and `ANMF`.
- Frame rectangle parsing: x, y, width, height, all 24-bit one-based where required.
- Frame duration parsing in milliseconds.
- Frame dispose flag parsing.
- Frame blend flag parsing.
- Animation background color parsing in BGRA byte order.
- Animation loop count parsing.
- Fragmented frame payload parsing with optional `ALPH` and image bitstream subchunks.
- Detection of invalid combinations such as `ALPH` with `VP8L` when it should be absent.
- Detection of missing required chunks based on `VP8X` feature flags.
- Identification-only mode that reads dimensions, flags, metadata presence, and frame table without decoding pixels.

### Container Muxing

- Basic still-image WebP writer for `VP8 ` and `VP8L` outputs without `VP8X` when no extended features are needed.
- Extended WebP writer using `VP8X` when alpha, ICC, Exif, XMP, animation, or unknown chunks require it.
- RIFF size calculation and validation before writing.
- Correct chunk padding emission.
- Deterministic standard chunk ordering.
- Optional unknown chunk preservation order.
- `ICCP`, `EXIF`, and `XMP ` insertion, replacement, and stripping.
- Animation assembly with `ANIM` and ordered `ANMF` frames.
- Frame rectangle, duration, dispose, and blend writing.
- `VP8X` feature flags derived from actual content.
- Validation before write to prevent inconsistent flags, dimensions, and frame data.

### Pixel Model

- Canonical decoded pixel format: 8-bit RGBA.
- Optional decoded BGRA copy method for .NET interop scenarios.
- Width, height, stride, and pixel buffer classes.
- Multiple-frame image class with canvas size, frames, metadata, and unknown chunks.
- Frame class with x/y offset, width, height, duration, blend mode, dispose mode, and pixel buffer.
- Alpha presence detection.
- Premultiplied and straight alpha conversion helpers.
- Transparent RGB preservation policy for encoding.
- Exact transparent pixel option for lossless and alpha-aware output.
- Background compositing helpers for animation rendering.
- Animation frame flattening according to blend and dispose rules.
- First-frame and selected-frame decode shortcuts.

### VP8 Lossy Decode

- Parse WebP `VP8 ` chunk frame tag.
- Parse VP8 keyframe signature and dimensions.
- Parse scale bits and color space/clamping flags.
- Boolean arithmetic decoder.
- Macroblock segmentation parsing.
- Loop filter header parsing.
- Quantizer header parsing.
- Token partition parsing.
- Intra prediction modes for luma and chroma.
- Coefficient tree decoding.
- Inverse transforms for DC and AC coefficients.
- YUV420 reconstruction.
- Deblocking loop filter.
- YUV to RGB conversion with clamping.
- Cropped and scaled decode planning hooks.
- Error detection for unsupported interframes, invalid partitions, bad signatures, and truncated bitstreams.
- Conformance tests against known VP8/WebP vectors.

### VP8 Lossy Encode

- Encode still-image lossy WebP using VP8 keyframe-only bitstreams.
- RGB/RGBA to YUV420 conversion.
- Optional alpha separation into `ALPH` for lossy RGB data.
- Macroblock subdivision and padding.
- Intra prediction mode search.
- Forward transforms and quantization.
- Token generation and entropy coding.
- Boolean arithmetic encoder.
- Segments and quantizer selection.
- Loop filter parameter selection.
- Quality setting from 0 to 100.
- Method/speed setting from 0 to 6.
- Presets inspired by libwebp: default, photo, picture, drawing, icon, text.
- Target-size and target-PSNR multi-pass planning.
- Spatial noise shaping setting.
- Filter strength and sharpness settings.
- Auto-filter option.
- Preprocessing and dithering option for lossy conversion.
- Alpha quality, alpha filter, and alpha compression method.
- No-alpha and blend-alpha options.
- Deterministic output mode for tests.

### VP8L Lossless Decode

- Parse `VP8L` signature.
- Parse 14-bit width and height minus one fields.
- Parse alpha-used hint and version.
- Least-significant-bit-first bitstream reading.
- Transform list parsing.
- Predictor transform parsing and inverse application.
- Color transform parsing and inverse application.
- Subtract-green inverse transform.
- Color-indexing transform including packed pixels for small palettes.
- Prefix-code description parsing.
- Canonical Huffman/prefix code table construction.
- Meta prefix code image parsing.
- Spatially variant entropy decoding.
- Literal ARGB symbol decoding.
- LZ77 length and distance prefix decoding.
- WebP 2D distance mapping.
- Color cache support with valid bit-size range.
- Recursive image stream decoding for transform metadata images.
- Validation for duplicate transforms, invalid version, invalid color cache bits, invalid references, and truncated data.

### VP8L Lossless Encode

- Emit valid `VP8L` bitstream and `VP8L` chunk.
- Width and height validation up to the lossless limit.
- Exact ARGB preservation.
- Optional subtract-green transform.
- Optional predictor transform generation.
- Optional color transform generation.
- Optional color-indexing transform for low-color images.
- Optional color cache selection.
- LZ77 match finding over ARGB pixels.
- 2D distance mapping.
- Prefix code generation.
- Meta prefix grouping.
- Entropy image generation.
- Quality/speed trade-off for lossless mode.
- Near-lossless preprocessing from 0 to 100.
- Palette-aware lossless encoding.
- Deterministic simple encoder mode for initial correctness.
- Later optimized encoder mode for file size.

### Alpha Handling

- Detect alpha requirements from pixels and container flags.
- Decode uncompressed `ALPH` payload.
- Decode compressed `ALPH` payload using VP8L-style alpha stream with implicit dimensions.
- Extract alpha from green channel after alpha lossless decode.
- Apply alpha filtering modes: none, horizontal, vertical, gradient.
- Apply alpha pre-processing flags.
- Encode uncompressed `ALPH`.
- Encode compressed `ALPH` using lossless alpha coding.
- Alpha quality setting.
- Alpha filter mode setting: none, fast, best.
- Alpha compression method setting: none or lossless.
- Preserve or discard RGB under transparent pixels based on exact option.

### Metadata

- Raw ICC profile get/set/remove.
- Raw Exif get/set/remove.
- Raw XMP get/set/remove.
- Metadata copy policy: none, all, ICC only, Exif only, XMP only, selected.
- Metadata size validation.
- Metadata chunk ordering during write.
- Metadata preservation during read-modify-write.
- No semantic parsing dependency for ICC/Exif/XMP in v1; store and expose raw bytes.

### Animation

- Decode animation global background color.
- Decode animation loop count where 0 means infinite looping.
- Decode all frames as independent rectangles.
- Decode all frames composited to full canvas.
- Decode first frame only.
- Decode selected frame only.
- Frame duration in milliseconds.
- Frame blend modes: source/no blend and over/blend.
- Frame disposal modes: none and background.
- Validation for frame bounds and zero-sized frames.
- Animation writer from encoded frame bitstreams.
- Animation writer from RGBA frame pixels.
- Per-frame lossy or lossless encoding mode.
- Per-frame alpha handling.
- Animation metadata editing without recompressing image data when possible.
- Frame extraction and replacement.
- Loop count and background color editing.

### High-Level User Workflows

- Identify a WebP from path, stream, byte array, or span without full decode.
- Decode still WebP to RGBA.
- Decode animated WebP to frames.
- Decode animated WebP to composited timeline frames.
- Encode still RGBA to lossy WebP.
- Encode still RGBA to lossless WebP.
- Encode RGBA frames to animated WebP.
- Convert between basic and extended containers as needed.
- Extract ICC/Exif/XMP metadata.
- Add, replace, or strip ICC/Exif/XMP metadata.
- Enumerate, preserve, or strip unknown chunks.
- Inspect chunk table for tooling.
- Validate file and collect diagnostics.
- Repair or normalize container ordering when image bitstreams are otherwise valid.
- Round-trip metadata and unknown chunks.
- Read and write from `Stream`.
- Read and write from file path convenience methods.
- Read and write from `byte[]` convenience methods.
- Memory-conscious decode options for max dimensions, max frames, metadata skipping, and first-frame-only decode.
- Security-conscious limits for allocation size, RIFF size, chunk count, frame count, recursion depth, and total animation pixels.
- Cross-platform behavior on .NET without native dependencies.

### Testing, Quality, and Tooling

- Syntax conformance tests for the simplified C# subset.
- Unit tests for endian readers/writers and bit readers/writers.
- RIFF chunk parser and writer tests.
- Container validation test corpus.
- Known-good WebP still and animation fixture corpus.
- Metadata round-trip tests.
- Unknown chunk preservation tests.
- VP8L bitstream conformance tests.
- VP8 lossy conformance tests.
- Alpha decode and encode tests.
- Animation composition tests.
- Encoder smoke tests and decoder round-trip tests.
- Fuzz-style malformed input tests using generated byte arrays.
- Allocation limit tests.
- Public API tests.
- Optional comparison tests against external generated fixtures, while the library itself remains dependency-free.

## Public API Completion Tasks

- [DONE] T001: Expose WebP signature, MIME type, and file-extension detection through an instance contract and `WebpCodec`, with byte-array and `ReadOnlySpan<byte>` inputs where applicable.
- [DONE] T002: Add uniquely named `ReadOnlySpan<byte>` entry points for decode, identify, validate, and container-edit operations so in-memory callers can avoid an input copy without introducing method overloads.
- [DONE] T003: Add public decode, identify, validate, and container-edit entry points for seekable streams, non-seekable streams, and file paths, with explicit and consistent stream-position and ownership behavior.
- [DONE] T004: Add public encode entry points that write a still image or animation to a caller-owned `Stream` or file path while enforcing the same options, validation, and size limits as byte-array encoding.
- [DONE] T005: Extend decode options and public result models for cropped and scaled still-image and animation output, including source rectangles, target dimensions, aspect-ratio behavior, and frame-coordinate handling.
- [DONE] T006: Expose pixel utilities for RGBA-to-BGRA copies, straight/premultiplied alpha conversion, alpha-presence detection, background blending, and flattening independent animation frames according to WebP blend and disposal rules.
- [DONE] T007: Extend `WebpInfo` so identification exposes animation loop count and background color, declared VP8X feature flags, reserved-bit state, and enough frame and chunk information to inspect a container without decoding pixels.
- [DONE] T008: Add dedicated metadata, unknown-chunk, and aggregate-allocation ceilings to `WebpSecurityLimits`, and enforce them before copying retained payloads or creating output buffers in every public operation.
- [DONE] T009: Expose unknown-chunk enumeration, replacement, and stripping through `IWebpMux` and `WebpCodec`, preserving unknown-chunk order and caller-selected placement relative to recognized chunks.
- [DONE] T010: Expose animation frame insertion, replacement, and removal without pixel decoding when supplied encoded frame payloads are compatible, while validating frame rectangles, duration, blend, disposal, and canvas bounds.
- [DONE] T011: Expose container normalization that repairs permitted chunk ordering, padding, derived VP8X feature flags, and basic-versus-extended layout without recompressing valid image bitstreams, and returns diagnostics for changes that cannot be made safely.
- [DONE] T012: Add per-frame encode settings to the public animation model so each pixel-backed frame can select lossy, lossless, or near-lossless coding and its own quality and alpha settings while retaining global defaults.
- [DONE] T013: Add public lossy-encoding controls for discarding alpha or blending it against a caller-supplied background color, keeping exact-transparent-RGB behavior distinct from those modes.
- [DONE] T014: Add a deterministic encode option covering still and animated lossy, lossless, near-lossless, and alpha streams so identical inputs and options produce identical bytes.
- [DONE] T015: Define and enforce public model invariants and exception behavior for null inputs, invalid enum values, dimensions, strides, frame selections, option ranges, metadata sizes, FourCC values, and mutually incompatible decode or encode settings.
- [DONE] T016: Add public-surface tests for all model constructors, defaults, mutable fields, enums, contracts, codec capabilities, invalid values, null handling, and exception behavior.
- [DONE] T017: Add parity tests for byte-array, `ReadOnlySpan<byte>`, seekable-stream, non-seekable-stream, and file-path decode, identify, validate, encode, and mux workflows, including stream positions, partial reads, output ownership, and I/O failures.
- [DONE] T018: Add detection and identification tests covering signatures, MIME and extension helpers, simple lossy and lossless containers, every VP8X feature flag, metadata presence, unknown chunks, animation globals, frame tables, reserved bits, padding, and trailing data.
- [DONE] T019: Add public decode conformance tests for VP8, VP8L, uncompressed and compressed alpha, metadata retention and skipping, unknown-chunk policy, first and selected frames, independent and composited animations, crop and scale options, and RGBA pixel accuracy.
- [DONE] T020: Add public encode conformance and round-trip tests for automatic, lossy, lossless, and near-lossless modes; all quality, preset, filter, preprocessing, target-size, target-PSNR, alpha, metadata, unknown-chunk, deterministic, and security options; and basic versus extended output selection.
- [DONE] T021: Add animation encode tests for mixed per-frame compression modes, alpha handling, offsets, durations, blend and disposal modes, loop count, background color, preserved encoded frames, deterministic output, and independent and composited decode round trips.
- [DONE] T022: Add pixel-utility tests for padded strides, RGBA/BGRA channel order, straight and premultiplied alpha edge cases, transparent RGB policy, alpha scans, background blending, clipping, and frame disposal across partial-canvas rectangles.
- [DONE] T023: Add mux tests for ICC, Exif, XMP, unknown-chunk placement, frame extraction and mutation, loop and background edits, feature-flag derivation, simple-to-extended conversion, payload preservation, and normalization without image recompression.
- [DONE] T024: Add strict and permissive validation tests for RIFF sizes and padding, chunk order and duplication, reserved bits, feature mismatches, malformed VP8 and VP8L payloads, invalid alpha combinations, invalid animation rectangles, zero-sized frames, and unsafe trailing data.
- [DONE] T025: Add allocation-limit and malformed-input tests through every public entry point, covering oversized RIFF data, dimensions, pixel products, metadata, unknown chunks, chunk and frame counts, VP8L recursion, aggregate animation pixels, truncated input, and integer-overflow boundaries.
- [DONE] T026: Add reference-interoperability tests using externally generated fixtures under `tests/` to verify decoded pixels and animation timelines against trusted WebP decoders and to verify every encoder mode and mux result is accepted by an independent WebP implementation.
- [DONE] T027: Measure line and branch coverage of the public API test suite, add missing behavioral and boundary cases for every public method and option, and require complete public-surface coverage.
