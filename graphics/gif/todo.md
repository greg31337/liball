# Universal GIF Reading / Writing Library Development Plan

## Goal

Build a universal GIF87a/GIF89a reader and writer in simplified C# using only built-in .NET APIs. The implementation should cover low-level format access, full image decode/encode, animation metadata, palette management, LZW compression/decompression, high-level convenience APIs, robust diagnostics, and compatibility tooling.

## Non-Negotiable Constraints

- Use only built-in .NET libraries. Do not depend on ImageSharp, Magick.NET, SkiaSharp, System.Drawing, native giflib, or other external packages.
- Follow `syntax.md`: no structs, records, attributes, properties, indexers, events, user-defined generic types or methods, `ref`/`out`/`in`, `params`, `try/catch`, LINQ, class inheritance outside exceptions and interface implementation, pattern matching, `async`/`await`, unsafe code, or preprocessor directives. Interfaces are limited to the method-only form defined there.
- Use public classes, fields, constructors, and ordinary block-bodied methods only for the defined API layer. Mark implementation types and members `internal`.
- Document every public type, constructor, method, field, enum, and enum member with the XML constructs allowed by `syntax.md`; internal symbols do not require XML API documentation.
- Use arrays, `Stream`, `MemoryStream`, `ArrayPool<T>`, `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, and `ReadOnlyMemory<T>` when they fit the syntax rules and improve allocation or copy behavior.
- Keep the public API shape simple and stable: operations are exposed by the interface-backed `GifCodec`, while models and option classes carry data in public fields.
- Treat all external input as untrusted: enforce size limits, frame limits, sub-block limits, decoded pixel limits, and LZW growth limits.
- Preserve unknown extensions by default so the library can round-trip data it does not understand.
- Do not use external image libraries for tests except optional fixture generation scripts outside `src`; core tests should use known byte fixtures and the library itself.

## Research Basis

- GIF89a specification: defines GIF87a/GIF89a versions, required Header and Logical Screen Descriptor, optional global/local color tables, image descriptors, table-based image data, data sub-blocks, graphic control, comment, plain text, application extensions, trailer, interlace passes, and variable-length LZW compression. Source: https://giflib.sourceforge.net/gifstandard/GIF89a.html
- GIFLIB: mature C library exposes both low-level record processing and high-level slurp/spew model, stores global color maps, saved images, image descriptors, extension blocks, interlacing support, callbacks, and detailed encoder/decoder errors. Source: https://giflib.sourceforge.net/gif_lib.html
- Pillow: mature image library supports GIF87a/GIF89a read/write, LZW writing, frame seeking, version/background/transparency/duration/loop/comment/application extension metadata, local color tables, interlace, disposal, palette choice, optimization, and multi-frame save options. Source: https://pillow.readthedocs.io/en/stable/handbook/image-file-formats.html#gif
- ImageSharp: .NET image library exposes GIF encoder concepts including quantization, pixel sampling, transparent color mode, repeat count, background color, animation root frame handling, frame delay, disposal mode, and global/local color table mode. Source: https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Formats.Gif.GifEncoder.html
- ImageMagick: mature image tool exposes animation commands that matter for a full GIF feature set: delay, dispose, loop, interlace, coalesce, deconstruct, flatten, frame cropping, duplicate removal, zero-delay removal, and animation layer optimization. Source: https://imagemagick.org/command-line-options/
- stb_image: compact decoder API highlights useful high-level entry points: decode from memory, decode through callbacks/files, identify dimensions without full decode, force output channel count, impose max dimensions, and expose animated GIF frame delays from memory. Source: https://github.com/nothings/stb/blob/master/stb_image.h

## Feature Inventory

### Low-Level Binary Format

- GIF signature validation for `GIF87a` and `GIF89a`.
- Header read/write with earliest-valid version selection on encode.
- Logical Screen Descriptor read/write.
- Little-endian unsigned 16-bit field read/write.
- Packed field bit extraction and composition.
- Global Color Table read/write.
- Local Color Table read/write.
- Color table size validation: powers of two from 2 through 256 entries.
- Color resolution and sort flag preservation.
- Background color index preservation and validation against active palette.
- Pixel aspect ratio preservation, expose raw byte and optional computed ratio helper.
- Image Descriptor read/write.
- Image rectangle validation against logical screen.
- Interlace flag read/write.
- Local color table flag and sort flag read/write.
- Table-based image data read/write.
- Data sub-block reader and writer with 0..255 byte chunking.
- Block terminator handling.
- Trailer read/write.
- Unknown block detection with diagnostic error.
- Unknown extension preservation and optional skipping.
- Extension scoping model: Graphic Control applies only to next graphic-rendering block; special-purpose extensions do not consume it.
- Support valid streams containing no image frames.
- Support streams that rely on a previously supplied global color table through decode options.

### GIF89a Extensions

- Graphic Control Extension read/write.
- Disposal method values 0 through 7, with known names for 0 through 3 and preservation of reserved values.
- User input flag preservation.
- Transparent color flag and transparent index handling.
- Frame delay in hundredths of a second.
- Delay conversion helpers for milliseconds.
- Comment Extension read/write.
- Multiple comment blocks and multi-sub-block comments.
- Application Extension read/write with 8-byte identifier and 3-byte authentication code.
- Netscape loop extension read/write.
- ANIMEXTS loop extension read/write if encountered.
- Preserve unknown application extension payloads.
- Plain Text Extension read/write as structured metadata.
- Option to ignore, preserve, or expose Plain Text Extensions.
- Extension ordering preservation where possible.
- Trailer-adjacent extension preservation, matching GIFLIB's high-level model.

### LZW Decompression

- GIF LZW minimum code size handling.
- Clear code and end code handling.
- Variable code width growth from minimum + 1 through 12 bits.
- Dictionary reset on clear code.
- Special KwKwK code case handling.
- Bit-packed code reader using least-significant-bit order.
- Validate early EOF, missing end code, bad clear code placement, and invalid code values.
- Optional tolerant mode for common malformed files.
- Output pixel count cap per frame.
- Reusable buffers for dictionary prefix/suffix/output stack.
- Allocation limits for compressed and decoded data.
- Incremental sub-block input path so full compressed image data does not always need one contiguous allocation.

### LZW Compression

- GIF LZW encoder with clear/end codes.
- Variable code width growth through 12 bits.
- Dictionary reset policy when table fills.
- Optional early clear policy for compression ratio.
- Bit-packed code writer using least-significant-bit order.
- Sub-block output chunking.
- Deterministic output for repeatable tests.
- Support minimum code sizes from 2 through 8.
- Validate palette index range before compression.
- Compression statistics for diagnostics and tuning.

### Raw Frame Decode

- Decode frames to indexed pixels.
- Preserve frame rectangle, offsets, interlace flag, local palette, and metadata.
- Deinterlace four-pass GIF image data into row order.
- Validate decoded pixel count exactly matches frame width * frame height unless tolerant mode allows truncation.
- Decode only metadata without image pixels.
- Decode first frame only.
- Decode up to maximum frame count.
- Lazy frame decode path for callers that want metadata first and pixels later.
- Decode from `Stream`.
- Decode from `byte[]`.
- Decode from `ReadOnlyMemory<byte>`.
- Identify dimensions/version/frame count without full pixel decode when possible.
- Expose raw frame index data without compositing.

### Composited Animation Decode

- Render frames onto logical screen canvas.
- Convert indexed frames to RGBA32 pixels.
- Apply transparency index.
- Apply frame offsets and clipping.
- Apply disposal method 0: unspecified, configurable as no-dispose by default.
- Apply disposal method 1: do not dispose.
- Apply disposal method 2: restore to background.
- Apply disposal method 3: restore to previous.
- Preserve reserved disposal methods, render with configurable fallback.
- Respect logical screen background color.
- Support transparent logical screen background when requested.
- Produce full-canvas frames for player/browser-like playback.
- Produce raw sub-rectangle frames for editing/optimization workflows.
- Coalesce animation into full frames.
- Decompose full frames into optimized sub-rectangles.
- Calculate accumulated timeline duration.
- Handle zero-delay frames with configurable minimum display duration.
- Optionally remove zero-delay frames while merging delays.
- Optionally remove duplicate consecutive frames while merging delays.

### Encoding Inputs

- Encode from indexed pixels and explicit palette.
- Encode from RGBA32 pixels with quantization.
- Encode single still image.
- Encode animation with multiple frames.
- Encode full-canvas frames.
- Encode sub-rectangle frames with offsets.
- Encode coalesced frames by deconstructing changed bounds.
- Encode exact raw frames without optimization.
- Validate frame dimensions and offsets.
- Validate all pixels have palette entries for indexed encode.
- Validate transparent index exists when transparency is enabled.
- Allow global palette only.
- Allow per-frame local palettes.
- Allow automatic selection of global or local palettes.
- Allow forced GIF87a when no GIF89a feature is used.
- Automatically select GIF89a when transparency, animation timing, comments, application extensions, or plain text extensions are used.

### Palette And Quantization

- Represent palette entries as RGB triples.
- Optional alpha-aware transparency selection for RGBA input.
- Exact palette construction when input has <= 256 colors.
- Median-cut quantizer.
- Octree quantizer.
- Uniform/web-safe palette option.
- Grayscale palette option.
- Caller-supplied palette.
- Palette compaction to nearest legal power-of-two size.
- Palette sorting or preservation.
- Global palette generation across all frames.
- Local palette generation per frame.
- Palette reuse heuristic.
- Transparent color assignment.
- Nearest-color mapping.
- Optional Floyd-Steinberg dithering.
- Optional ordered Bayer dithering.
- No-dither mode.
- Pixel sampling strategy for faster quantization.
- Quantization quality/performance options.

### Metadata Model

- GIF version.
- Logical screen width and height.
- Background color index.
- Pixel aspect ratio byte.
- Global color table.
- Frame count.
- Per-frame left/top/width/height.
- Per-frame delay.
- Per-frame disposal.
- Per-frame transparency index.
- Per-frame user input flag.
- Per-frame local color table.
- Per-frame interlace flag.
- Comments.
- Application extensions.
- Netscape repeat count, where 0 means forever.
- Plain text extensions.
- Unknown extensions.
- Raw block ordering information for round-trip workflows.
- Warnings collected during tolerant decode.
- Encode statistics.
- Decode statistics.

### Diagnostics And Validation

- Strongly typed error codes.
- Human-readable error messages.
- Byte offset on parse errors.
- Frame index on frame errors.
- Block type on block errors.
- Validation mode: strict, normal, tolerant.
- Detect non-GIF input.
- Detect unsupported version.
- Detect malformed packed fields and reserved bits in strict mode.
- Detect missing logical screen descriptor.
- Detect bad color table size.
- Detect image rectangle outside logical screen.
- Detect missing active color table.
- Detect malformed extension block size.
- Detect unterminated sub-block chains.
- Detect LZW code stream errors.
- Detect decoded pixel overflow.
- Detect too many frames.
- Detect excessive dimensions.
- Detect excessive memory estimate before allocation.
- Expose warnings without throwing where possible.
- Deterministic exception type for fatal failures.

### Performance And Memory

- Span-based byte parsing for memory inputs.
- Stream parser path for large files.
- Avoid loading all frame pixels when identifying metadata.
- Reuse LZW buffers.
- Reuse compositing canvas.
- Use `ArrayPool<T>` for large temporary arrays.
- Avoid LINQ and iterator allocations.
- Avoid per-pixel object allocation.
- Row-based color conversion.
- Optional caller-provided buffer for decoded RGBA output.
- Configurable maximum dimensions, frames, compressed bytes, decoded pixels, and memory.

### High-Level API Features

- `GifCodec.DecodeBytes` and `DecodeStream` for full document decode and raw indexed-frame workflows.
- `GifCodec.IdentifyBytes` and `IdentifyStream` for metadata-only identification.
- `GifCodec.DecodeAnimationBytes` and `DecodeAnimationStream` for composited RGBA frames.
- `GifCodec.Encode` and `Write` for document output.
- `GifCodec.CreateIndexedBitmap`, `CreateStill`, and `CreateAnimation` for simple inputs and documents.
- `GifCodec.Coalesce` and `Deconstruct` for animation representation changes.
- `GifCodec.ValidateDocument`, `ValidateBitmap`, and `ValidateAnimation` for validation.
- `GifCodec.SetDelay`, `SetDelays`, `SetDisposal`, `SetDisposals`, loop, comment, and palette methods for in-memory metadata changes.
- `GifCodec.Optimize` for timeline recalculation, deconstruction, duplicate removal, and zero-delay removal.
- Convenience file methods only if acceptable within built-in .NET APIs.

### Testing And Compatibility

- Unit tests for every binary block.
- Unit tests for sub-block edge cases.
- Unit tests for LZW decode code-size growth.
- Unit tests for LZW clear/end behavior.
- Unit tests for LZW special code case.
- Unit tests for LZW encode/decode round trips.
- Golden tests for minimal GIF87a.
- Golden tests for minimal GIF89a.
- Golden tests for transparent single-frame GIF.
- Golden tests for interlaced GIF.
- Golden tests for local color table.
- Golden tests for animated GIF with each disposal mode.
- Golden tests for Netscape loop extension.
- Golden tests for comments.
- Golden tests for application extensions.
- Golden tests for plain text extension preservation.
- Malformed input tests for every diagnostic category.
- Fuzz-like randomized parser tests with deterministic random bytes.
- Round-trip tests preserving metadata and unknown extensions.
- Cross-check test fixtures against browser-observed or known-good expected RGBA frames.
- Large-limit tests that prove oversized inputs fail before large allocations.

## Remaining Public API Tasks

- [DONE] T001: Make fatal identify, decode, compositing, encode, write, validation, and transformation failures observable through a consistent public result or `GifException` contract while retaining structured diagnostics, byte offsets, frame indexes, block names, tolerant partial output, and warnings.
- [DONE] T002: Add `ReadOnlyMemory<byte>` identify, raw decode, and composited animation decode operations to the public decoder contract and `GifCodec`, backed only by built-in .NET memory and span APIs.
- [DONE] T003: Expose metadata-first, on-demand decoding of a selected raw indexed frame or composited RGBA frame from bytes, memory, and seekable streams without retaining every frame's pixels.
- [DONE] T004: Expose selected-frame and composited RGBA decode operations that accept caller-provided `Span<byte>` output storage, validate its required size, and report the number of bytes written.
- [DONE] T005: Add a public plain-text-extension decode policy that lets callers ignore with a warning, preserve without rendering, or expose structured plain-text graphic data.
- [DONE] T006: Complete background handling by adding a caller-selected solid RGBA color for canvas initialization and restore-to-background disposal when `GifBackgroundMode.SolidColor` is selected.
- [DONE] T007: Complete RGBA quantization controls with a caller-supplied palette, an exact transparent color, explicit palette sorting versus order preservation, and validation of incompatible transparency and palette settings.
- [DONE] T008: Let callers combine deconstruction, duplicate-frame removal, zero-delay-frame removal, delay normalization, and timeline recalculation in one optimization request instead of selecting only one mutually exclusive mode.
- [DONE] T009: Add public raw block-order and scope data sufficient to preserve the relative positions of graphic control, comment, application, plain text, unknown, image, and trailer-adjacent extension blocks during decode-edit-encode round trips.
- [DONE] T010: Add public delay conversion operations between GIF hundredths and milliseconds with defined rounding, range validation, and overflow diagnostics.
- [DONE] T011: Complete strongly typed public diagnostics for malformed packed or reserved fields, missing required trailer data in strict mode, invalid operation state, and destination write failures without collapsing distinct failures into unrelated error codes.
- [DONE] T012: Ensure public decode options independently cap total input bytes, compressed image bytes, sub-block count and bytes, dimensions, frames, decoded pixels, and estimated memory before allocation, and expose which limit was exceeded.
- [DONE] T013: Add public tests that invoke every `GifCodec` method through `IGifDecoder`, `IGifEncoder`, `IGifFactory`, and `IGifTransformer`, covering successful calls, default or null options, invalid inputs, and parity with the concrete codec.
- [DONE] T014: Add public decoder tests for byte-array, `ReadOnlyMemory<byte>`, and stream parity; non-closing stream ownership; metadata-only, first-frame, maximum-frame, on-demand-frame, and caller-buffer paths; and streams containing no image frames or relying on a supplied global palette.
- [DONE] T015: Add public raw decode and identify tests covering GIF87a and GIF89a headers, logical-screen fields, global and local palettes, interlacing, indexed pixels, every standard extension, NETSCAPE and ANIMEXTS loops, unknown extensions, trailer-adjacent data, warnings, and statistics.
- [DONE] T016: Add public compositing tests covering transparency, clipping, all defined disposal methods, every reserved disposal fallback, logical-screen, transparent, and solid backgrounds, zero-delay normalization, accumulated duration, raw sub-rectangles, and full-canvas output.
- [DONE] T017: Add public indexed and raw-document encode tests covering stills, animations, full-canvas and offset sub-rectangle frames, global, local, and automatic palettes, interlace, exact frame preservation, forced and automatically selected versions, metadata extensions, both LZW clear policies, stream writes, validation, limits, and statistics.
- [DONE] T018: Add public RGBA encode tests covering exact, median-cut, octree, grayscale, web-safe, and caller-supplied palettes; global and local animation palettes; palette compaction and reuse; sampling; no dither, Floyd-Steinberg, and Bayer dither; alpha-threshold and exact-color transparency; and out-of-palette failures.
- [DONE] T019: Add public factory tests for indexed bitmap, still document, and animation creation, including dimensions, offsets, frame rectangles, delays, disposal, transparency, looping, palette selection, null inputs, mismatched option arrays, and invalid palette indexes.
- [DONE] T020: Add public transformer tests for delay, disposal, loop, comments, global and local palettes, coalescing, deconstruction, each optimization behavior, combined optimization behavior, timeline recalculation, null inputs, invalid values, and atomic failure without partial mutation.
- [DONE] T021: Add complete public model tests for every constructor default, field invariant, nullable input normalization, bitmap stride boundary, metadata and diagnostic representation, statistics counter, enum value, delay conversion, and raw block-order model.
- [DONE] T022: Add public failure-contract tests for every `GifErrorCode` and `GifWarningCode`, all `GifException` conversion and formatting branches, fatal versus tolerant behavior, partial results, source positions, frame context, block context, and write failures.
- [DONE] T023: Add strict, normal, and tolerant public decode tests for malformed signatures, versions, packed fields, palettes, rectangles, fixed extension sizes, sub-block termination, LZW streams, decoded pixel counts, missing trailers, extra trailing data, and unknown blocks.
- [DONE] T024: Add public safety-limit tests proving every input, compressed-byte, sub-block, dimension, frame, decoded-pixel, output-byte, and memory limit fails with the intended diagnostic before oversized allocation or partial destination writes.
- [DONE] T025: Add public round-trip tests that preserve semantic metadata, raw block ordering and scope, multiple and multi-sub-block comments, application identifiers and authentication codes, plain text controls, unknown extension payloads, local palettes, sort flags, aspect ratio, reserved disposal values, and loop semantics.
- [DONE] T026: Add independently generated or externally validated compatibility fixtures with expected indexed and RGBA output for interlace, transparency, local palettes, extension scoping, all disposal behaviors, and finite and infinite looping, and exercise them only through the public API.
- [DONE] T027: Add deterministic randomized tests against the public identify, decode, animation decode, and on-demand frame operations that accept arbitrary bytes without process crashes, hangs, unbounded allocation, or unstructured exceptions.
- [DONE] T028: Measure executable line and branch coverage under `src/Api`, add focused functional tests for every uncovered public path, and verify that all public methods and behavior-bearing constructors are exercised without adding syntax-policy or source-shape tests.
