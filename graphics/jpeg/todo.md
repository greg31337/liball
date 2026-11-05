# Universal JPEG Library Development Plan

## Goal

Build a pure managed .NET JPEG reading and writing library with no external runtime libraries. The library should cover common production JPEGs first, then broaden toward full JPEG-1 family compatibility. It exposes a small high-level API for ordinary users plus marker and coefficient inspection for advanced pipelines. Entropy codecs, parsers, conversion kernels, buffers, and other implementation machinery remain internal.

The implementation must follow the simplified C# subset in `syntax.md`: file-scoped namespaces, classes, enums, and restricted instance-method-only interfaces, `static`, `public`, `internal`, and `const` modifiers only, block-bodied class methods, no properties, no records, no structs, no class inheritance outside exception types, no interface inheritance, no user-defined generics, no LINQ, no `try/catch`, no attributes, no `ref/out/params`, and no unsafe code. Public types and members alone define the supported API and require XML documentation using only the elements permitted by `syntax.md`.

## Research Notes

- libjpeg-turbo is the compatibility baseline for classic JPEG behavior: baseline and progressive DCT, arithmetic coding, in-memory source/destination APIs, IDCT scaling, separate luma/chroma quality, RGB extension formats, and lossless transform operations. It explicitly does not support SmartScale and treats some JPEG v7/v8/v9 features as out of scope.
- Pillow exposes a practical user-facing option set worth matching: identify JPEG/JFIF/Adobe JPEG, read L/RGB/CMYK, write standard and progressive JFIF, decode-time reduction, quality, optimized Huffman tables, progressive output, DPI, ICC, EXIF, comments, `keep_rgb`, subsampling, restart markers, quantization tables, and abbreviated streams.
- TwelveMonkeys highlights important real-world decoder compatibility: YCbCr without JFIF, CMYK, Adobe YCCK, embedded ICC handling, corrupt ICC recovery, non-standard color spaces, warnings for recoverable corruption, JFIF/JFXX/EXIF thumbnails, and tolerant metadata parsing.
- ImageSharp's API split is a useful model: format detection, decoder options, encoder options, image/pixel model separation, and metadata handling are separate concerns.
- JFIF 1.02 wraps ISO/IEC 10918-1 JPEG bitstreams with APP0 metadata and optional thumbnails. The broader JPEG family may contain baseline DCT, progressive DCT, JPEG extensions, original lossless JPEG, JPEG-LS, tiled pyramids, arithmetic coding, variable quantization, selective refinement, and other less-common modes.
- ITU-T T.86 describes APPn marker practice: APP0 for JFIF, APP1 for Exif, APP2 for ICC_PROFILE, and preservation/skipping of application markers that the library does not interpret.

## Scope Rules

- Use only built-in .NET APIs. No ImageSharp, System.Drawing, SkiaSharp, libjpeg, native shims, or color-management packages.
- Target classic JPEG/JFIF/Exif streams first. JPEG 2000, JPEG XR, JPEG XL, and HEIF are separate formats and are out of scope except for detection with a clear unsupported result.
- Preserve unknown metadata and unknown APPn segments by default.
- Prefer deterministic, portable integer algorithms over platform-specific behavior.
- Design public classes with fields instead of properties to stay compatible with `syntax.md`.
- Avoid public APIs that require `Span<T>`, `ref struct`, `ref`, `out`, `params`, attributes, interface inheritance, class inheritance, or user-defined generic declarations.
- Keep parsers, codecs, transforms, converters, buffer allocators, table models, constants, and validation helpers internal unless they are deliberately promoted into the documented API.
- Add XML documentation only to public types and members, including every public enum member and explicit public constructor.

## Feature Inventory

### Low-Level Binary and Stream Features

- SOI, EOI, SOS, TEM, RST0-RST7, APP0-APP15, COM, DQT, DHT, DAC, DRI, DNL, DHP, EXP, SOF, and unknown marker scanning.
- Big-endian 16-bit marker segment lengths.
- Entropy-coded byte stuffing handling for `0xFF00`.
- Restart marker alignment, sequence checking, and recovery.
- Strict and tolerant marker parsing modes.
- Streaming reads from non-seekable `Stream`.
- Buffered writes to any writable `Stream`.
- Byte-array convenience APIs.
- Segment size validation and configurable maximum marker payload size.
- Configurable maximum dimensions, components, scans, metadata bytes, entropy bytes, and output pixels.
- Raw marker preservation with original order.
- Marker-level diagnostics with offsets.
- Abbreviated table-only streams.
- Abbreviated image-only streams.
- Multiple table definitions and table redefinition handling.
- DNL late height definition support.
- Progressive scan validation.
- Component sampling factor validation.
- Detection of non-JPEG family formats with clear unsupported errors.

### JPEG Coding Process Features

- Baseline sequential DCT, Huffman entropy, 8-bit samples, SOF0.
- Extended sequential DCT, Huffman entropy, 8-bit and 12-bit samples, SOF1.
- Progressive DCT, Huffman entropy, SOF2.
- Original lossless sequential Huffman JPEG, SOF3.
- Differential sequential DCT Huffman, SOF5.
- Differential progressive DCT Huffman, SOF6.
- Differential lossless Huffman, SOF7.
- Extended sequential DCT arithmetic, SOF9.
- Progressive DCT arithmetic, SOF10.
- Original lossless arithmetic, SOF11.
- Differential sequential DCT arithmetic, SOF13.
- Differential progressive DCT arithmetic, SOF14.
- Differential lossless arithmetic, SOF15.
- Unsupported reserved SOF handling with useful diagnostics.
- JPEG-LS marker recognition and unsupported fallback unless implemented later.
- Hierarchical mode marker parsing and unsupported fallback until differential modes are decoded.
- SmartScale detection and unsupported fallback.
- Lossless RGB SmartScale detection as unsupported.

### Entropy Coding Features

- Canonical Huffman table parsing from DHT.
- Huffman decode table construction with fast lookup and slow fallback.
- Baseline DC difference decoding.
- Baseline AC run-length decoding.
- Progressive DC first/refinement scans.
- Progressive AC first/refinement scans.
- EOB run handling.
- Successive approximation bit handling.
- Optimized Huffman table generation for encoding.
- User-supplied Huffman table encoding.
- Default JPEG Huffman tables for baseline output.
- Arithmetic conditioning table parsing from DAC.
- Arithmetic binary probability state machine.
- Arithmetic DC and AC decode.
- Arithmetic DC and AC encode.
- Restart interval state reset for Huffman and arithmetic coding.
- Bit writer with byte stuffing.
- Bit reader with marker lookahead.

### Transform and Quantization Features

- Standard quantization table parsing with 8-bit and 16-bit precision.
- Default luma and chroma quantization tables.
- Quality scaling from user quality values.
- Separate luminance and chrominance quality.
- User-supplied quantization tables.
- Quantization table preservation from source.
- Zig-zag and natural coefficient order conversion.
- Forward 8x8 DCT for encoding.
- Inverse 8x8 DCT for decoding.
- Integer accurate IDCT.
- Fast integer IDCT option.
- Floating-point DCT/IDCT optional compatibility mode if useful with built-in .NET only.
- Partial IDCT scaling for 1/8, 1/4, 1/2, and selected libjpeg-compatible scale ratios.
- Coefficient-domain representation for inspection and lossless transforms.
- Coefficient clipping and range validation.

### Sample Precision and Pixel Buffer Features

- 8-bit sample precision decode and encode.
- 12-bit sample precision decode and encode for extended JPEG.
- 16-bit sample support for original lossless JPEG.
- Interleaved packed pixel buffers.
- Planar component buffers.
- Grayscale 1-component images.
- 2-component images with raw access.
- RGB 3-component images.
- YCbCr 3-component images.
- CMYK 4-component images.
- YCCK 4-component images.
- Raw N-component access for uncommon files.
- RGBA/BGRA/BGR/RGB convenience conversion despite JPEG alpha absence.
- Opaque alpha fill when decoding to 4-channel RGB buffers.
- Row-by-row decode callbacks.
- Row-by-row encode callbacks.
- Whole-image decode convenience.
- Whole-image encode convenience.
- Memory ownership rules documented for returned buffers.

### Chroma Subsampling and MCU Features

- Arbitrary legal horizontal and vertical sampling factors from 1 to 4.
- Common 4:4:4, 4:2:2, 4:2:0, 4:4:0, 4:1:1, and grayscale presets.
- MCU dimension calculation.
- Edge padding for partial MCUs.
- Interleaved scans.
- Non-interleaved scans.
- Component order preservation.
- Fancy upsampling option.
- Fast box upsampling option.
- Downsampling for encoder with selectable filters.
- No-subsampling RGB output mode.
- Restart markers per MCU block count.
- Restart markers per MCU row count.

### Color Features

- JFIF YCbCr to RGB conversion.
- Raw YCbCr decode without conversion.
- RGB JPEG decode and encode.
- Grayscale decode and encode.
- Adobe APP14 transform detection.
- Adobe CMYK inversion handling.
- YCCK to CMYK conversion.
- YCCK to RGB conversion.
- CMYK to RGB fallback conversion.
- ICC profile extraction and preservation.
- ICC APP2 chunk reassembly and validation.
- Basic ICC profile parser for headers, tags, curves, matrix/TRC profiles, and device class checks.
- In-house ICC conversion for common RGB, Gray, and CMYK matrix/curve profiles when feasible.
- Configurable ICC behavior: ignore, preserve, convert when supported, fail if unsupported.
- Exif ColorSpace and related color hints.
- CICP metadata preservation if present in APP markers.
- Full-range and limited-range YCbCr policy if encountered.
- Unknown color-space policy: raw samples, best-effort RGB, or fail.

### Metadata Features

- JFIF APP0 parse and write.
- JFIF version, density unit, X density, Y density, and thumbnail metadata.
- JFXX extension parse and write.
- JFXX JPEG thumbnail support.
- JFXX indexed thumbnail support.
- JFXX RGB thumbnail support.
- Exif APP1 raw preservation.
- Exif TIFF header parsing for byte order, IFD traversal, tag values, orientation, dimensions, resolution, color space, date/time, camera fields, GPS, and thumbnail offsets.
- Exif thumbnail extraction for JPEG, RGB, and YCbCr thumbnails.
- Exif orientation read and optional auto-orient pixel transform.
- XMP APP1 raw preservation.
- Extended XMP chunk parse and reassembly.
- ICC APP2 parse, reassembly, write, strip, and replace.
- MPF APP2 detection and raw preservation.
- Photoshop APP13 / IPTC raw preservation and basic resource block parsing.
- Adobe APP14 parse and write.
- COM marker parse, write, multiple-comments support.
- Unknown APPn and unknown marker preservation.
- Metadata round-trip policies: preserve all, preserve known, strip all, strip unsafe, replace selected.
- Marker ordering policies for standard-compatible writes.
- Metadata size limits and diagnostic warnings.

### Decoder Features

- Identify-only read: dimensions, precision, component count, process type, progressive flag, arithmetic flag, color hint, metadata summary, memory estimate.
- Full decode to `JpegImage`.
- Decode to RGB, RGBA, BGR, BGRA, grayscale, CMYK, YCbCr, YCCK, or raw component buffers.
- Decode target size with IDCT scaling where possible.
- Decode region/crop where MCU boundaries allow efficient skipping.
- Skip metadata option.
- Preserve metadata option.
- Tolerant truncated-image decode option.
- Strict standards mode.
- Warning collection for recoverable issues.
- Deterministic exception types for invalid JPEG, unsupported JPEG, resource limit, and I/O failure.
- Progressive image decode across scans.
- Optional scan callback for progressive previews.
- Non-seekable stream support.
- Cancellation checks through `CancellationToken` values.
- Thread-safe independent decoder instances.

### Encoder Features

- Write JFIF baseline sequential JPEG.
- Write progressive JPEG.
- Write extended sequential 12-bit JPEG.
- Write original lossless JPEG.
- Write arithmetic-coded JPEG.
- Quality parameter with sensible default.
- Separate luminance/chrominance quality.
- Subsampling presets and explicit component sampling.
- Optimized Huffman table pass.
- User quantization tables.
- User Huffman tables.
- Restart markers by block or row.
- Interleaved and non-interleaved scans.
- Custom scan script for progressive output.
- RGB to YCbCr conversion.
- Keep RGB option.
- Grayscale output.
- CMYK output.
- YCCK output.
- Preserve, strip, or write metadata.
- Write ICC profiles split across APP2 chunks.
- Write Exif, XMP, JFIF, JFXX, Adobe, COM, and unknown APPn markers.
- Write abbreviated table-only streams.
- Write abbreviated image-only streams.
- Deterministic output mode for tests.
- Compatibility presets: web, archival, maximum compatibility, maximum compression, metadata preserve.

### Lossless Transform and Inspection Features

- Parse JPEG into marker model without pixel decode.
- Read and write coefficient-domain data.
- Lossless rotate 90, 180, 270 where MCU alignment allows.
- Lossless horizontal and vertical flip.
- Lossless transpose and transverse.
- Lossless crop with MCU alignment.
- Perfect-transform validation.
- Drop or preserve metadata during transform.
- Grayscale transform option by dropping chroma when safe.
- Comment insertion/removal without pixel re-encode.
- ICC/Exif/XMP replacement without pixel re-encode when marker layout allows.
- Table extraction and table injection.

### Robustness, Security, and Compatibility Features

- Fuzz-test-friendly parser with resource limits.
- Graceful handling of missing EOI in tolerant mode.
- Graceful handling of malformed metadata when pixel data can be recovered.
- Corrupt ICC policy compatible with tolerant readers.
- Unknown APPn preservation.
- Duplicate marker diagnostics.
- Invalid component/table references diagnostics.
- Dimension overflow protection.
- Allocation overflow protection.
- Entropy decode runaway protection.
- Deterministic behavior across platforms.
- No native code and no unsafe blocks.
- Benchmark suite for common file classes.
- Golden corpus for baseline, progressive, CMYK, YCCK, ICC, Exif, restart, metadata-heavy, truncated, and malformed samples.

## Public API Completion Tasks

- [DONE] T001: Add public format-family identification data that distinguishes classic JPEG, JPEG-LS, JPEG 2000, JPEG XR, JPEG XL, HEIF, unknown input, and recognized-but-unsupported coding processes without requiring pixel decode.
- [DONE] T002: Extend the identifier contract and implementation with file-path, `ReadOnlySpan<byte>`, explicit-options, and `CancellationToken` entry points while preserving non-seekable stream support.
- [DONE] T003: Extend the reader and writer contracts and implementations with file-path, span-appropriate, explicit-options, and `CancellationToken` entry points, with consistent stream-position and leave-open behavior.
- [DONE] T004: Expand the public image and component-buffer models to represent interleaved and planar 8-bit, 12-bit, and 16-bit samples, arbitrary component counts, row strides, component sampling, and clear caller/library buffer ownership.
- [DONE] T005: Extend the image factory with uniquely named `ReadOnlySpan<byte>` and `ReadOnlySpan<int>` creation methods for packed and planar sample data without requiring external libraries.
- [DONE] T006: Add restricted-syntax callback interfaces and reader/writer methods for row-by-row decode, row-by-row encode, and progressive scan previews, including cancellation checks and bounded reusable buffers.
- [DONE] T007: Add public read options and result fields for target dimensions, supported IDCT scale ratios, decoded regions, MCU-aligned fast cropping, and the exact output rectangle actually produced.
- [DONE] T008: Add public read controls for fancy versus box chroma upsampling, raw-component output, best-effort versus exact unknown-color handling, Adobe inversion overrides, and full-range versus limited-range YCbCr interpretation.
- [DONE] T009: Complete the public resource-limit model with marker-payload, retained-marker, table, component-buffer, progressive-state, and callback-output limits in addition to the existing dimension, scan, metadata, entropy, pixel, and byte limits.
- [DONE] T010: Add public data models for frame components, scan headers and components, quantization tables, Huffman tables, arithmetic conditioning tables, and preserved raw markers so callers can inspect and supply every structure required by advanced workflows.
- [DONE] T011: Add public table-set and abbreviated-stream APIs for reading and writing table-only streams and for decoding or encoding image-only streams with explicitly supplied tables.
- [DONE] T012: Add encoder options for JPEG process, sample precision, Huffman versus arithmetic entropy coding, lossless predictor selection, point transform, and supported sequential, progressive, lossless, arithmetic, differential, and hierarchical modes.
- [DONE] T013: Add encoder models for explicit per-component identifiers and sampling factors, component order, interleaved versus non-interleaved scans, and legal arbitrary sampling factors from one through four.
- [DONE] T014: Add encoder inputs for user quantization, Huffman, and arithmetic-conditioning tables, quantization-table preservation, and explicit table assignment per component and scan.
- [DONE] T015: Add a public progressive scan-script model and validation path covering DC/AC first and refinement scans, spectral ranges, successive approximation, component selectors, and a standard default script.
- [DONE] T016: Add write options that distinguish restart intervals measured in MCU blocks from intervals measured in MCU rows and report the resolved interval in write results or inspection data.
- [DONE] T017: Add explicit web, archival, maximum-compatibility, maximum-compression, and metadata-preserving encoder presets plus a deterministic-output option whose resolved settings remain inspectable.
- [DONE] T018: Expose preserved APPn, COM, and unknown marker payloads with original ordering in `JpegMetadata`, and allow callers to retain, replace, insert, remove, or reorder them under the metadata policies.
- [DONE] T019: Complete writable JFIF and JFXX metadata models for versions, density, inline RGB thumbnails, JPEG thumbnails, indexed thumbnails, RGB extension thumbnails, and validation of all marker size constraints.
- [DONE] T020: Add general Exif IFD and tag-value models alongside the common convenience fields, and expose selected-field replacement including orientation, dimensions, resolution, color, date/time, camera, lens, GPS, and JPEG/RGB/YCbCr thumbnails.
- [DONE] T021: Expose standard and Extended XMP packet/chunk models, GUID and offset validation, reassembly diagnostics, and metadata-editor replacement or removal of both forms.
- [DONE] T022: Complete the ICC public model with tag payload access, supported matrix/TRC conversion descriptions for RGB, Gray, and CMYK profiles, rendering intent, conversion outcome diagnostics, and an explicit destination color space.
- [DONE] T023: Add public CICP and other color-hint metadata fields and define their precedence relative to JFIF, Exif, Adobe APP14, component identifiers, and ICC profiles.
- [DONE] T024: Extend the metadata-editor contract and implementation to support metadata policies, generic marker editing, stripping, unknown-marker preservation, and byte-array, `ReadOnlySpan<byte>`, stream, and file workflows without re-encoding coefficients.
- [DONE] T025: Extend marker inspection with read options, strict/tolerant diagnostics, cancellation, `ReadOnlySpan<byte>` input, and parsed frame, scan, table, restart, DNL, hierarchical, JPEG-LS, and SmartScale information.
- [DONE] T026: Complete the coefficient image model with the public frame, scan, entropy-table, quantization-table, arithmetic-conditioning, restart, precision, and marker data needed to validate, modify, and write independently constructed coefficient images.
- [DONE] T027: Add public table extraction and injection operations for complete JPEG streams, abbreviated streams, and coefficient images while preserving unrelated metadata and entropy data.
- [DONE] T028: Add transform options for perfect-transform validation, MCU crop behavior, metadata policy, safe grayscale conversion by chroma removal, cancellation, and byte-array, span, stream, and file variants of every lossless transform.
- [DONE] T029: Add a deterministic public I/O failure exception contract and apply consistent argument, invalid-data, unsupported-feature, resource-limit, write-validation, cancellation, and I/O behavior across every public service.
- [DONE] T030: Validate and wire every new public implementation exclusively to built-in .NET APIs, keep independent service instances thread-safe, and avoid hidden dependencies on mutable caller buffers.
- [DONE] T031: Add tests for every public enum value, data-model field, option default, preset resolution, constructor, validation rule, buffer-copy/ownership rule, and exception category.
- [DONE] T032: Add identifier tests covering every JPEG-1 SOF process, DNL, hierarchical and SmartScale markers, abbreviated streams, JPEG-LS recognition, non-JPEG family detection, metadata summaries, estimates, limits, diagnostics, spans, streams, files, and cancellation.
- [DONE] T033: Add reader tests covering every supported coding process and precision, packed and planar output format, color and ICC policy, sampling layout, upsampler, scaling ratio, target size, region, auto-orientation, metadata policy, whole-image path, row callback, progressive callback, and cancellation path.
- [DONE] T034: Add writer tests covering every supported coding process and precision, pixel and component input form, color space, sampling configuration, downsampler, quality control, table source, scan script, entropy mode, restart unit, metadata policy, preset, deterministic mode, abbreviated stream, destination form, and cancellation path.
- [DONE] T035: Add metadata tests covering parse, malformed-input diagnostics, write, replace, strip, preserve, ordering, chunking, and byte-for-byte round trips for JFIF, JFXX, Exif/TIFF/GPS/thumbnails, XMP/Extended XMP, ICC v2/v4, MPF, Photoshop/IPTC, Adobe APP14, COM, CICP, and unknown markers.
- [DONE] T036: Add inspector and coefficient tests covering every marker kind and offset, strict/tolerant parsing, table extraction/injection, abbreviated streams, coefficient edits, independently constructed coefficient images, all supported precisions/processes, and byte-array/span/stream/file round trips.
- [DONE] T037: Add lossless-transform tests for rotate, flip, transpose, transverse, MCU-aligned crop, rejected imperfect transforms, grayscale conversion, metadata policies, coefficient preservation, cancellation, and every input/output form.
- [DONE] T038: Add public API robustness tests for null and invalid arguments, non-readable/non-writable/non-seekable streams, truncated and malformed data, duplicate and reordered markers, missing/redefined tables, bad scans and restarts, allocation and arithmetic overflow, configured limits, tolerant recovery, and deterministic diagnostics.
- [DONE] T039: Add concurrency and cancellation tests proving independent identifier, reader, writer, inspector, metadata-editor, and transformer instances are safe for simultaneous use and stop promptly without corrupting caller-owned buffers or streams.
- [DONE] T040: Add a standards-derived and interoperability corpus that exercises every public API feature and error branch, verifies decode samples and encoder structure against independent test fixtures, and closes all remaining public-layer line and branch coverage gaps.
