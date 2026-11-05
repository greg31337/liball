# Universal PNG Library Development Plan

## Goal

Build a universal PNG parsing, processing, and generating library in simplified C# for .NET. The library should support static PNG and APNG, preserve and edit metadata, expose low-level chunk access, decode to usable pixels, encode valid PNG datastreams, and provide practical image-processing and optimization tools.

Implementation must use only built-in .NET libraries. No libpng, ImageSharp, SkiaSharp, Magick.NET, zlib NuGet package, or other third-party runtime dependency is allowed.

## Research Inputs

- PNG Specification Third Edition, W3C Recommendation, 2025-06-24: https://www.w3.org/TR/png-3/
- libpng reference library overview and release notes: https://www.libpng.org/pub/png/libpng.html
- libpng manual, especially chunk handling, transforms, gamma, interlace, read/write workflows, and unknown chunks: https://libpng.org/pub/png/libpng-manual.html
- SixLabors ImageSharp PNG encoder API, for practical managed API controls such as bit depth, chunk filtering, color type, compression, filter method, gamma, interlace, and text compression threshold: https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Formats.Png.PngEncoder.html
- stb_image.h, for a compact dependency-free loader model with memory, file, callback style input, dimension probing, 1/2/4/8/16-bit PNG support, automatic palette expansion, and hard size limits: https://github.com/nothings/stb/blob/master/stb_image.h
- pngcrush manpage, for optimizer features such as compression/filter trials, ancillary chunk removal/addition, lossless reductions, CRC/Adler checks, pHYs/gAMA/iCCP/text/tRNS insertion, and safety limits: https://manpages.debian.org/testing/pngcrush/pngcrush.1.en.html
- .NET `System.IO.Compression` documentation, for built-in zlib stream support: https://learn.microsoft.com/en-us/dotnet/api/system.io.compression?view=net-10.0

## Implementation Constraints From syntax.md

- Use one file-scoped namespace per source file.
- Use classes, enums, and focused interfaces. Interfaces contain instance methods only, use an `I` followed by an uppercase letter, and do not use fields, default implementations, static abstract members, generic methods, or interface inheritance.
- Classes may implement interfaces. Do not use structs, records, delegates, attributes, or other inheritance.
- Use fields instead of properties. Public data models use public fields; implementation state uses internal fields.
- Use ordinary block-bodied methods only. Do not use expression-bodied members or local functions.
- Use only `public`, `internal`, `static`, and `const` modifiers where modifiers are needed.
- Do not use generic type or method declarations. Generic .NET library types such as `List<T>` are allowed.
- Do not use `ref`, `out`, `in`, `params`, default parameter values, operators, indexers, events, unsafe code, pointer code, tuples, patterns, `try`/`catch`, `await`, or LINQ.
- Prefer explicit loops and helper classes over LINQ and advanced syntax.
- Treat every public class, method, field, enum, and enum member as supported API. Keep implementation-only declarations internal.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member. Use only `summary`, `remarks`, `param`, `returns`, `exception`, `example`, `see`, `seealso`, `para`, and `c` XML elements.
- Public API shapes should be simple class models with fields, constructors, and methods; internal helpers may use the same shapes without becoming supported API.

## Feature Inventory

### Level 0: Binary, Safety, And Infrastructure

- PNG signature detection and validation.
- Big-endian unsigned integer read/write helpers.
- Bounded stream and byte-array reading.
- Chunk length validation before allocation.
- CRC-32 calculation and validation.
- Adler-32 verification for decompressed zlib payloads when accessible.
- Overflow-safe dimension, stride, row byte, frame byte, and allocation calculations.
- Configurable decode limits for width, height, pixel count, decompressed bytes, metadata bytes, chunk count, text bytes, animation frames, and IDAT size.
- Error policy modes: strict, tolerant metadata, tolerant repair, and best-effort inspection.
- Warning collection without relying on exceptions for non-fatal conditions.
- Cancellation-free synchronous API first, because simplified syntax and low-level image operations are easier to validate synchronously.
- Deterministic allocation policy based on byte arrays and streams.
- No external dependencies beyond built-in .NET APIs.

### Level 1: Datastream And Chunk Model

- Read PNG signature.
- Read all chunks in order.
- Expose chunk type, raw data, CRC, offset, length, and known/unknown classification.
- Decode chunk type flags: critical/ancillary, public/private, reserved bit, safe-to-copy.
- Validate chunk naming rules.
- Validate chunk ordering rules.
- Preserve unknown chunks by default when editing or re-encoding.
- Allow unknown chunks to be dropped, copied, or replaced according to safe-to-copy and caller policy.
- Support multiple IDAT chunks as one compressed image stream.
- Support empty chunks where valid.
- Reject duplicate chunks where PNG forbids duplicates.
- Track pre-PLTE, pre-IDAT, in-IDAT, and post-IDAT chunk placement.
- Write chunks with correct length, type, data, and CRC.
- Provide low-level chunk enumeration without decoding pixels.
- Provide metadata-only identification without inflating IDAT.

### Level 2: Critical PNG Support

- IHDR parsing and writing.
- Width and height validation.
- Compression method 0 validation.
- Filter method 0 validation.
- Interlace method 0 and 1 validation.
- Color type validation for grayscale, truecolor, indexed color, grayscale-alpha, and truecolor-alpha.
- Bit depth validation for every color type.
- PLTE parsing, validation, and writing.
- PLTE length validation from 1 to 256 RGB entries and multiple-of-3 payload length.
- IDAT concatenation, zlib decompression, and zlib compression using built-in .NET `System.IO.Compression`.
- IEND parsing and writing.
- Detection of extra data after IEND.

### Level 3: Scanlines, Filters, And Interlace

- Scanline byte count calculation for packed and unpacked formats.
- Packed 1-bit, 2-bit, and 4-bit sample unpacking.
- Packed 1-bit, 2-bit, and 4-bit sample packing.
- 8-bit and 16-bit sample read/write.
- Big-endian 16-bit sample handling.
- Filter type 0 None decode and encode.
- Filter type 1 Sub decode and encode.
- Filter type 2 Up decode and encode.
- Filter type 3 Average decode and encode.
- Filter type 4 Paeth decode and encode.
- Invalid filter byte detection.
- Adaptive per-row filter selection.
- Fixed filter selection.
- Fast heuristic filter selection.
- Exhaustive filter scoring for optimization mode.
- Adam7 pass geometry.
- Adam7 decode.
- Adam7 encode.
- Progressive row callback model for Adam7 and non-interlaced images.
- Optional progressive preview frames.

### Level 4: Pixel Formats And Color Conversion

- Native decoded formats for all PNG color type and bit depth combinations.
- Normalized output to grayscale 8-bit.
- Normalized output to grayscale-alpha 8-bit.
- Normalized output to RGB 8-bit.
- Normalized output to RGBA 8-bit.
- Normalized output to grayscale 16-bit.
- Normalized output to grayscale-alpha 16-bit.
- Normalized output to RGB 16-bit.
- Normalized output to RGBA 16-bit.
- Palette index output for indexed-color PNG.
- Palette-to-RGB expansion.
- Palette-to-RGBA expansion using tRNS.
- Low-bit grayscale expansion to 8-bit.
- Low-bit grayscale preservation.
- tRNS to alpha expansion for grayscale, truecolor, and indexed-color images.
- Strip alpha transform.
- Add opaque alpha transform.
- Invert alpha transform.
- Invert monochrome transform.
- Swap RGB/BGR transform.
- Swap RGBA/ARGB transform.
- Byte-swap 16-bit samples.
- Significant-bit downshift/upshift using sBIT.
- RGB to grayscale conversion using PNG/libpng-style luminance weights.
- Grayscale to RGB expansion.
- Premultiply alpha.
- Unpremultiply alpha.
- Composite over background color.
- Clear RGB channels under fully transparent pixels.
- Vertical flip.
- Crop.
- Pad/extend canvas.
- Copy rectangular region.
- Basic nearest-neighbor resize.
- Optional bilinear resize.

### Level 5: Ancillary And PNG-3 Chunks

- tRNS transparency chunk.
- cHRM primary chromaticities and white point.
- gAMA image gamma.
- iCCP embedded ICC profile with zlib compression/decompression.
- sBIT significant bits.
- sRGB standard RGB color space and rendering intent.
- cICP coding-independent video signal code points.
- mDCV mastering display color volume.
- cLLI content light level information.
- tEXt Latin-1 textual data.
- zTXt compressed Latin-1 textual data.
- iTXt international textual data with language and translated keyword.
- bKGD background color.
- hIST palette histogram.
- pHYs physical pixel dimensions and unit.
- sPLT suggested palette.
- eXIf Exif profile payload.
- tIME last modification time.
- acTL APNG animation control.
- fcTL APNG frame control.
- fdAT APNG frame data.
- Known extension chunk passthrough with typed wrappers where useful.
- Unknown ancillary chunk preservation.
- Metadata byte limit enforcement.
- Text keyword validation.
- Text encoding validation and conversion.
- Color chunk precedence handling: cICP, iCCP, sRGB, then cHRM/gAMA.

### Level 6: APNG

- APNG detection through acTL.
- Frame count and play count parsing.
- fcTL sequence validation.
- fdAT sequence validation.
- Default image as first frame handling.
- Frame rectangle validation against canvas.
- Frame delay numerator/denominator handling.
- Dispose operations: none, background, previous.
- Blend operations: source and over.
- Frame decode to raw frame pixels.
- Frame composition to full canvas pixels.
- Encode APNG from full frames.
- Encode APNG from frame rectangles.
- Preserve or synthesize animation chunks according to encoder options.
- Animation frame limit enforcement.

### Level 7: Encoding And Generation

- Create blank images.
- Create image from raw pixel bytes.
- Encode from normalized RGBA8.
- Encode from normalized RGB8.
- Encode from normalized GA8 and G8.
- Encode from 16-bit normalized formats.
- Encode indexed-color images from supplied palette and indices.
- Optional automatic palette generation up to 256 entries.
- Optional lossless color type reduction.
- Optional bit depth reduction when samples permit it.
- Optional palette reduction and palette length trimming.
- Choose color type automatically from pixel content.
- Choose bit depth automatically from pixel content.
- Write all critical chunks in canonical order.
- Write ancillary chunks in valid order.
- Split IDAT by configurable maximum chunk size.
- Configure compression level.
- Configure zlib strategy where built-in .NET exposes it.
- Configure filter method.
- Configure interlace method.
- Configure metadata preservation and chunk filtering.
- Generate PNG test images programmatically.

### Level 8: Processing, Editing, And Optimization

- Read, modify, and write metadata without decoding pixels where possible.
- Remove selected ancillary chunks.
- Keep selected chunks.
- Replace gAMA, sRGB, cICP, iCCP, pHYs, tIME, and text chunks.
- Add text chunks.
- Add or remove tRNS.
- Normalize to canonical RGBA8.
- Normalize to sRGB-like output using available metadata.
- Preserve original color metadata when not transforming color samples.
- Validate only mode.
- Repair mode for selected recoverable errors such as bad CRC policy, extra trailing bytes, invalid metadata chunks, and bad ancillary ordering.
- Optimizer mode that tries filter strategies and compression levels.
- Lossless reducer mode for color type, bit depth, alpha removal, and palette trimming.
- Compare encoded size per strategy.
- Stop optimizer trials early when current output exceeds best output.
- Report optimization decisions.
- Stable round-trip mode for editors.

### Level 9: Testing, Compliance, And Tooling

- Unit tests for binary readers and writers.
- Unit tests for CRC.
- Unit tests for chunk type flags.
- Unit tests for chunk ordering rules.
- Unit tests for every critical chunk.
- Unit tests for every supported ancillary chunk.
- Unit tests for filter decode and encode.
- Unit tests for Adam7 pass geometry.
- Unit tests for all color type and bit depth combinations.
- Unit tests for tRNS handling.
- Unit tests for gamma and color metadata precedence.
- Unit tests for text chunk validation.
- Unit tests for APNG frame sequence validation and composition.
- Encode/decode round-trip tests.
- Metadata-only identify tests.
- Strict/tolerant policy tests.
- Allocation limit and overflow tests.
- Fuzz-style malformed chunk tests using deterministic generated inputs.
- Syntax conformance tests for simplified C#.
- Interoperability corpus tests against known valid PNG files.
- Golden byte tests for small generated PNG files.
- Performance benchmark harness without external benchmark packages.

## Remaining Public-Layer Tasks

All source implementation in these tasks must use only built-in .NET APIs. Use `Span<T>` and `ReadOnlySpan<T>` for bounded in-memory operations where they avoid copies without making the stream APIs less capable.

- [DONE] T001: Make `IPngDecoder` and `PngCodec` honor every `PngDecoderOptions` field, including requested native or normalized pixel format, CRC policy, unknown-chunk preservation, animation decoding, error policy, trailing-byte policy, and every security limit.
- [DONE] T002: Add uniquely named byte-array-offset and `ReadOnlySpan<byte>` decode entry points to the decoder contract and implementation so embedded and bounded PNG datastreams do not require caller-side copies.
- [DONE] T003: Add a progressive decoder contract using interfaces rather than delegates, with non-interlaced row delivery, Adam7 pass updates, optional progressive previews, frame delivery, warnings, and the same limits and policies as full decode.
- [DONE] T004: Complete APNG decoding through the public codec by parsing frame-local compressed streams, handling the default image correctly, validating sequence numbers and rectangles, normalizing delays, applying blend and dispose operations, and populating both raw-frame and composed-canvas results in `PngAnimation`.
- [DONE] T005: Complete APNG encoding through the public codec for full-canvas and frame-rectangle inputs, including default-image selection, canonical sequence numbering, frame limits, metadata preservation, configurable IDAT/fdAT splitting, and round-trippable timing and operation values.
- [DONE] T006: Extend public metadata data models to represent every planned standard chunk: tRNS, cHRM, gAMA, iCCP, sBIT, sRGB, cICP, mDCV, cLLI, tEXt, zTXt, iTXt, bKGD, hIST, pHYs, sPLT, eXIf, tIME, acTL, fcTL, and fdAT.
- [DONE] T007: Expand `PngText` or add distinct public text models so Latin-1, compressed Latin-1, and international UTF-8 entries retain compression state, language tag, translated keyword, keyword, text, and original ordering without lossy conversion.
- [DONE] T008: Populate typed metadata, unknown chunks, size totals, color-precedence results, and diagnostics consistently in both `PngImage` and `PngIdentifyResult` while keeping metadata-only identification free of IDAT inflation.
- [DONE] T009: Extend `IPngMetadataEditor` and `PngMetadataService` with typed set, add, replace, and remove operations for every planned ancillary metadata family, including all tRNS forms and color, HDR, text, palette, Exif, and time chunks.
- [DONE] T010: Add explicit metadata keep/remove selection fields and public unknown-chunk replacement models so `KeepSelected`, `RemoveSelected`, and `Replace` policies are fully configurable and enforce safe-to-copy rules after pixel changes.
- [DONE] T011: Add chunk placement and preservation state to the public raw-chunk model, and make `IPngChunkCodec` preserve or canonicalize pre-PLTE, pre-IDAT, contiguous-IDAT, and post-IDAT placement according to caller policy.
- [DONE] T012: Add uniquely named `ReadOnlySpan<byte>` and `Span<byte>` operations to the chunk and CRC contracts where the output size is known, retaining byte-array and stream methods for complete datastream workflows.
- [DONE] T013: Add a general public pixel-format conversion operation and implement every planned native, grayscale, grayscale-alpha, RGB, RGBA, indexed, 8-bit, and 16-bit conversion, including palette expansion, tRNS expansion, low-bit preservation or scaling, alpha addition or removal, and correct 16-bit byte order.
- [DONE] T014: Correct channel-reordering contracts so BGR and ARGB results have accurately declared public pixel formats, can be converted back without ambiguity, and are rejected or normalized before PNG encoding as appropriate.
- [DONE] T015: Expose color-metadata precedence and sRGB-like normalization through an interface-backed public service, applying cICP, iCCP, sRGB, and cHRM/gAMA precedence, significant-bit handling, gamma transforms, background compositing, and explicit diagnostics when an embedded profile cannot be transformed with built-in .NET facilities.
- [DONE] T016: Complete transformer behavior for every compatible 8-bit, 16-bit, packed, grayscale, RGB, and alpha format across alpha operations, monochrome inversion, geometry, nearest-neighbor and bilinear resize, palette operations, significant-bit scaling, gamma correction, and transparency expansion.
- [DONE] T017: Extend image creation APIs for caller-supplied stride, palette and indexed data, metadata, static-image/default-frame choice, and APNG frame collections, using span-based input when an immediate defensive copy is appropriate.
- [DONE] T018: Complete encoder content analysis so automatic color type, bit depth, alpha removal, grayscale reduction, exact palette generation, palette trimming, and packed indexed or grayscale output select the smallest permitted lossless representation while honoring explicit caller choices.
- [DONE] T019: Add explicit public fast, adaptive, fixed, and exhaustive row-filter selection controls, and make encoder and optimizer workflows apply them consistently to non-interlaced rows and each Adam7 pass.
- [DONE] T020: Complete metadata-preserving encoding with canonical ordering, selected keep/remove behavior, safe unknown-chunk copying, text compression choices, color-chunk precedence, stable editor round trips, and correct preservation or regeneration of animation chunks.
- [DONE] T021: Complete `IPngValidator` coverage for signature and framing, all critical and ancillary payload rules, reserved values, duplicates, chunk ordering, CRC and zlib integrity, decompressed scanline sizes, filter bytes, palette indices, color-metadata precedence, text encodings, APNG structure, and trailing data under every error policy.
- [DONE] T022: Extend `IPngRepairer` with explicit, report-producing repairs for trailing bytes, malformed or misplaced ancillary chunks, ancillary CRCs, and other planned recoverable cases while never silently repairing critical image-data corruption.
- [DONE] T023: Enforce width, height, pixel, row, chunk, metadata, text, IDAT/fdAT, decompressed-byte, frame-count, frame-byte, and aggregate animation limits consistently across byte-array, span, stream, progressive, identify, validate, decode, encode, repair, and optimization entry points.
- [DONE] T024: Add a single public optimization workflow that applies permitted lossless reductions, metadata stripping, filter and compression trials, real early termination while output is being produced, and a report containing every decision and the best encoded result.
- [DONE] T025: Complete public model, enum, option, constructor, and invariant tests for every supported field and value, including independent mutable defaults, limit defaults, metadata variants, chunk placement, progressive state, animation state, and optimization reports.
- [DONE] T026: Add contract tests that exercise every method on every public interface and its implementation through byte-array, offset, span, and stream inputs, verifying option propagation, equivalent results, stream-position behavior, short reads, non-seekable streams, and failure semantics.
- [DONE] T027: Add end-to-end public codec tests covering every valid color-type and bit-depth combination, fixed/adaptive/exhaustive filters, non-interlaced and Adam7 images, all requested output formats, automatic lossless reductions, metadata policies, golden encodings, and encode/decode round trips.
- [DONE] T028: Add end-to-end public APNG tests for static fallback, both default-image arrangements, full and sub-rectangle frames, delay edge cases, both blend operations, all dispose operations, raw and composed output, sequence failures, limits, encode/decode round trips, and metadata preservation.
- [DONE] T029: Add public metadata, chunk, identify, validation, and repair tests for every standard chunk, unknown safe and unsafe chunks, typed/raw round trips, color precedence, text encodings and limits, selected keep/remove/replace policies, CRC behavior, trailing data, stable editing, and deterministic malformed inputs.
- [DONE] T030: Add public transformer and image-factory tests across every compatible pixel format, stride and padding case, packed-row boundary, alpha and transparency edge value, significant-bit value, gamma table, geometry boundary, palette capacity, channel order, invalid input, overflow, and allocation limit.
- [DONE] T031: Add public optimizer tests for every option and reduction decision, filter/compression winner selection, metadata handling, deterministic tie behavior, early termination, injected encoder behavior, report accuracy, invalid inputs, and output round trips.
- [DONE] T032: Add interoperability tests that identify, validate, decode, and where lossless re-encoding applies round-trip every image in the pinned `specs/pngsuite` corpus, supplementing it with deterministic APNG and PNG-3 chunk fixtures for coverage absent from PNGSuite.
