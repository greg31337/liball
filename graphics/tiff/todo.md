# Universal TIFF Library Development Plan

## Goal

Build a universal TIFF reading and writing library in simplified C# for .NET, using only built-in .NET APIs. The library should start with a reliable TIFF/BigTIFF container core, then grow into broad image-data, metadata, scientific, geospatial, and high-level raster support.

## Research Notes

Feature scope is based on capabilities and gaps observed in mature TIFF libraries and specifications:

- libtiff exposes multiple abstraction levels: high-level RGBA raster reading, decoded scanline access, decoded strip/tile access, and raw strip/tile access. It also treats multi-page files as independent directories and supports custom tags, warnings, codecs, byte-order handling, and BigTIFF-related offset widths. Source: https://libtiff.gitlab.io/libtiff/libtiff.html
- libtiff documents TIFF 6.0 read/write support and common compression schemes including None, CCITT 1D/Group 3/Group 4, LZW, PackBits, Deflate, JPEG, and selected historical/nonstandard schemes. Source: https://libtiff.gitlab.io/libtiff/functions/libtiff.html
- BigTIFF changes the version number from 42 to 43, uses 64-bit offsets, 8-byte value/offset fields, 8-byte-aligned external values, 64-bit IFD counts, and LONG8/SLONG8/IFD8 data types. Source: https://libtiff.gitlab.io/libtiff/specification/bigtiff.html
- tifffile covers classic TIFF, BigTIFF, OME-TIFF, ImageJ hyperstacks, shaped multidimensional metadata, pyramids/SubIFDs, sparse GeoTIFF segments, and many microscopy/digital-slide variants. Source: https://pypi.org/project/tifffile/
- Apache Commons Imaging supports TIFF 6.0 read/write, bilevel/palette/RGB/CMYK/YCbCr/CIELab/LogLuv images, LZW, CCITT, PackBits, tiled reading, metadata, XMP, EXIF, thumbnails, and partial image reads; it calls out JPEG TIFF compression as a notable gap. Source: https://commons.apache.org/proper/commons-imaging/formatsupport.html
- GeoTIFF 1.1 defines TIFF-based georeferencing through model tags and GeoKeyDirectoryTag/GeoDoubleParamsTag/GeoAsciiParamsTag, with EPSG-coded coordinate systems and user/private ranges. Source: https://docs.ogc.org/is/19-008r4/19-008r4.html

## Implementation Constraints

- Use the simplified C# subset in `syntax.md`.
- Do not use external libraries or native bindings.
- Prefer `Span<byte>`, `ReadOnlySpan<byte>`, `Memory<byte>`, `ReadOnlyMemory<byte>`, `ArrayPool<byte>`, `Stream`, `BinaryPrimitives`, `DeflateStream`, and other built-in .NET APIs when they fit.
- Do not define structs, records, properties, indexers, events, attributes, extension methods, generic classes/methods, or operator overloads. Use only the restricted interfaces and class-to-interface inheritance allowed by `syntax.md`.
- Use `public` only for the deliberately supported API layer; mark implementation classes, constructors, methods, and fields `internal`.
- Document every public type, constructor, method, field, enum, and enum member with XML documentation using only the elements allowed by `syntax.md`.
- Use result classes for multi-value returns instead of `ref`, `out`, or tuples.
- Avoid LINQ in source.
- Keep built-in codec logic internal and expose only the narrow `ITiffCompressionCodec` substitution boundary for application-provided managed codecs.
- JPEG-in-TIFF, JPEG 2000, WebP, ZSTD, LZMA, JXL, and other formats without built-in .NET codecs should be represented as unsupported or raw-pass-through until a no-external-library implementation is built.

## Feature Inventory

### Container And Binary Foundations

- Read classic TIFF headers.
- Write classic TIFF headers.
- Read BigTIFF headers.
- Write BigTIFF headers.
- Detect little-endian and big-endian byte order.
- Preserve selected write byte order.
- Validate TIFF magic/version fields.
- Validate BigTIFF bytesize and reserved fields.
- Read and write 16-bit, 32-bit, and 64-bit unsigned/signed values.
- Read and write IEEE single/double values.
- Handle 32-bit classic offsets safely.
- Handle 64-bit BigTIFF offsets safely.
- Validate offsets and byte ranges against stream length.
- Support seekable streams.
- Support file-path helpers over streams.
- Report non-seekable streams as unsupported for random-access features.
- Guard against integer overflow in all size/count calculations.
- Enforce configurable maximum allocation sizes.
- Enforce configurable maximum decoded image dimensions.
- Enforce configurable maximum IFD count.
- Enforce configurable maximum tag count per IFD.
- Detect cyclic IFD chains.
- Detect overlapping or invalid data blocks when strict validation is enabled.
- Align BigTIFF external values to 8 bytes.
- Align classic external values as required by writer policy.
- Preserve trailing data when reading metadata.
- Provide diagnostics for warnings and recoverable errors.

### IFD And Tag Model

- Read classic IFD entries.
- Write classic IFD entries.
- Read BigTIFF IFD entries.
- Write BigTIFF IFD entries.
- Sort written tags by tag id.
- Read NextIFD links.
- Write NextIFD links.
- Support multi-page TIFF.
- Support SubIFD trees.
- Support EXIF IFD pointers.
- Support GPS IFD pointers.
- Support interoperability IFD pointers.
- Support custom/private tags.
- Preserve unknown tags as typed raw values.
- Provide typed access for known tags.
- Provide raw access for all tags.
- Apply TIFF default values where the specification defines them.
- Validate duplicate tags.
- Validate required tags for image directories.
- Support BYTE, ASCII, SHORT, LONG, RATIONAL, SBYTE, UNDEFINED, SSHORT, SLONG, SRATIONAL, FLOAT, DOUBLE, IFD, LONG8, SLONG8, and IFD8.
- Support tag values stored inline.
- Support tag values stored out-of-line.
- Support multiple value counts.
- Support ASCII with NUL termination rules.
- Support rational number numerator/denominator pairs.
- Preserve tag byte order semantics.
- Preserve original tag order in optional raw view.
- Validate count/type combinations for well-known tags.
- Support tag mutation for writer workflows.
- Support metadata-only file inspection.

### Baseline Image Tags

- ImageWidth.
- ImageLength.
- BitsPerSample.
- Compression.
- PhotometricInterpretation.
- Thresholding.
- CellWidth and CellLength.
- FillOrder.
- DocumentName.
- ImageDescription.
- Make.
- Model.
- StripOffsets.
- Orientation.
- SamplesPerPixel.
- RowsPerStrip.
- StripByteCounts.
- MinSampleValue and MaxSampleValue.
- XResolution and YResolution.
- PlanarConfiguration.
- PageName.
- XPosition and YPosition.
- FreeOffsets and FreeByteCounts.
- GrayResponseUnit and GrayResponseCurve.
- Group3Options and Group4Options.
- ResolutionUnit.
- PageNumber.
- ColorResponseCurves.
- Software.
- DateTime.
- Artist.
- HostComputer.
- Predictor.
- WhitePoint.
- PrimaryChromaticities.
- ColorMap.
- ExtraSamples.
- SampleFormat.
- SMinSampleValue and SMaxSampleValue.

### Extension And Common Tags

- TileWidth.
- TileLength.
- TileOffsets.
- TileByteCounts.
- SubIFDs.
- InkSet.
- InkNames.
- NumberOfInks.
- DotRange.
- TargetPrinter.
- HalftoneHints.
- SampleFormat.
- TransferRange.
- JPEGTables.
- YCbCrCoefficients.
- YCbCrSubSampling.
- YCbCrPositioning.
- ReferenceBlackWhite.
- Copyright.
- ICCProfile.
- XMP.
- Photoshop image resources.
- IPTC metadata blocks when embedded in TIFF.
- RichTIFF/IPTC-related text tags.
- GDAL metadata and NODATA tags.
- ImageDepth and TileDepth for volumetric extensions.
- NewSubfileType and SubfileType.
- Orientation-aware raster read options.
- Associated and unassociated alpha metadata.

### Storage Organization

- Stripped images.
- Tiled images.
- One-strip images.
- Chopped strip view for huge uncompressed single strips as an optional memory policy.
- Contiguous planar configuration.
- Separate planar configuration.
- Partial final strips.
- Partial edge tiles.
- Tiles with dimensions constrained to TIFF rules for writing.
- Tile/strip index calculation helpers.
- Byte count and decoded size calculation helpers.
- Raw strip reading.
- Raw strip writing.
- Encoded strip reading.
- Encoded strip writing.
- Raw tile reading.
- Raw tile writing.
- Encoded tile reading.
- Encoded tile writing.
- Scanline reading over stripped images.
- Scanline writing over stripped images.
- Region/window reads.
- Region/window writes where storage organization permits.
- Sparse segments with zero offsets/byte counts.
- Contiguous image-data detection.
- Memory-efficient streaming over strips/tiles.
- Lazy decode of image data.

### Compression Codecs

- No compression read/write.
- PackBits read/write.
- Deflate/ZLib read/write using built-in .NET compression streams.
- LZW read/write implemented in managed code.
- Horizontal differencing predictor for 8/16/32/64-bit integer samples.
- Floating-point predictor.
- CCITT Modified Huffman read/write.
- CCITT Group 3 read/write.
- CCITT Group 4 read/write.
- Legacy word-aligned 1D Huffman read/write if practical.
- ThunderScan read-only if practical.
- NeXT 2-bit read-only if practical.
- JPEG compression detection.
- JPEG raw segment pass-through for copy/rewrite workflows.
- JPEG decoded raster support only if implemented fully in managed code.
- Old-style JPEG tag detection and diagnostics.
- Unsupported compression diagnostics with raw access fallback.
- Compression option validation.
- Per-codec memory and output expansion limits.
- Fuzz tests for malformed compressed segments.

### Pixel Formats And Sample Layouts

- 1-bit bilevel.
- 2-bit and 4-bit grayscale/palette.
- 8-bit grayscale.
- 16-bit grayscale.
- 32-bit integer grayscale.
- 32-bit floating grayscale.
- 64-bit floating grayscale.
- Palette/indexed color with ColorMap expansion.
- RGB.
- RGBA with associated alpha.
- RGBA with unassociated alpha.
- RGB plus extra samples.
- CMYK.
- YCbCr.
- CIELab.
- ICCLab.
- ITULab.
- LogLuv and LogL.
- CFA/Bayer-like raw samples where described by tags.
- Signed integer sample formats.
- Unsigned integer sample formats.
- IEEE floating sample formats.
- Undefined/opaque sample formats.
- Mixed BitsPerSample arrays.
- FillOrder bit reversal.
- Byte-order swap for multi-byte samples.
- Sub-byte sample packing/unpacking.
- Planar separate sample assembly.
- Predictor reversal and application.
- Premultiplication and unpremultiplication helpers.
- Orientation transforms for high-level rasters.

### Color Management And Conversion

- WhiteIsZero to grayscale.
- BlackIsZero to grayscale.
- Palette to RGB/RGBA.
- RGB pass-through.
- CMYK to RGB approximation.
- YCbCr to RGB conversion.
- CIELab/ICCLab/ITULab conversion approximations.
- LogLuv decode where supported.
- ICC profile extraction.
- ICC profile preservation on write.
- Optional high-level conversion to 8-bit RGBA.
- Optional high-level conversion to 16-bit RGBA.
- Optional raw sample plane access with no color conversion.
- ReferenceBlackWhite handling for YCbCr.
- ExtraSamples alpha handling.
- Min/Max sample scaling policies.
- Photometric mismatch diagnostics.

### Metadata Families

- EXIF reading.
- EXIF writing.
- GPS metadata reading.
- GPS metadata writing.
- XMP packet extraction.
- XMP packet writing.
- ICC profile extraction and writing.
- Photoshop resource block extraction.
- IPTC extraction where stored in known tags.
- ImageDescription plain text preservation.
- JSON-shaped metadata recognition.
- OME-XML recognition and extraction.
- ImageJ metadata recognition and extraction.
- GDAL metadata recognition and extraction.
- GeoTIFF key directory reading.
- GeoTIFF key directory writing.
- Date/time parsing helpers.
- Metadata copy between files.
- Metadata remove/update workflows.
- Unknown metadata preservation policy.

### Scientific, Medical, And Slide Variants

- BigTIFF files larger than 4 GB.
- OME-TIFF single-file read/write.
- OME-TIFF multi-file metadata recognition.
- ImageJ hyperstack metadata recognition.
- ImageJ hyperstack write support.
- Multidimensional series grouping.
- Axes/order metadata model.
- Pyramid/overview SubIFD reading.
- Pyramid/overview writing.
- Reduced-resolution image detection.
- Sparse GeoTIFF segment behavior.
- Aperio/SVS metadata recognition.
- Hamamatsu NDPI metadata recognition.
- Leica/SCN metadata recognition.
- Philips slide metadata recognition.
- Roche/Ventana BIF metadata recognition.
- Zeiss LSM metadata recognition.
- Micro-Manager metadata recognition.
- DNG tag recognition.
- Astro-TIFF metadata recognition.
- Volume/depth tag support.
- Tile coordinate and level lookup helpers.
- Read individual tiles from large slides without decoding the whole image.

### GeoTIFF And Cloud Optimized TIFF

- GeoKeyDirectoryTag parsing.
- GeoDoubleParamsTag parsing.
- GeoAsciiParamsTag parsing.
- ModelPixelScaleTag parsing.
- ModelTiepointTag parsing.
- ModelTransformationTag parsing.
- EPSG code exposure.
- User-defined CRS metadata exposure.
- Private GeoKey preservation.
- RasterPixelIsArea/RasterPixelIsPoint support.
- Georeferencing transform helpers.
- GDAL_NODATA tag support.
- GDAL_METADATA tag support.
- Internal overview recognition.
- External overview naming hooks.
- Cloud Optimized GeoTIFF validation.
- Cloud Optimized GeoTIFF writer mode.
- Ordered IFD and tile data layout for range-friendly reads.
- Tile offset/byte count monotonicity checks.

### High-Level Reading

- Open TIFF from file path.
- Open TIFF from stream.
- Inspect file header.
- Enumerate images/pages.
- Select image/page by index.
- Select SubIFD by parent/index.
- Read only metadata.
- Read raw tag values.
- Read normalized image info.
- Read scanlines.
- Read strips.
- Read tiles.
- Read rectangular regions.
- Read individual sample planes.
- Read decoded raster in source sample type.
- Read normalized 8-bit RGBA.
- Read normalized 16-bit RGBA.
- Read thumbnails/reduced images.
- Read image series from related IFDs.
- Read multidimensional planes by coordinates.
- Progress callback represented as ordinary class callback methods.
- Cancellation via `CancellationToken`.
- Configurable strict/permissive mode.

### High-Level Writing

- Create TIFF from file path.
- Create TIFF from stream.
- Choose classic TIFF or BigTIFF.
- Auto-upgrade to BigTIFF when size requires it.
- Choose byte order.
- Add image/page.
- Add SubIFD.
- Add pyramid levels.
- Add metadata tags.
- Add EXIF/GPS/XMP/ICC/GeoTIFF metadata.
- Write scanlines.
- Write strips.
- Write tiles.
- Write full raster buffers.
- Write sample planes.
- Choose compression.
- Choose predictor.
- Choose strip height.
- Choose tile size.
- Validate required tags before finalization.
- Flush directories and patch offsets.
- Rewrite metadata without decoding image data when possible.
- Copy image data from one TIFF to another when compatible.

### Validation, Diagnostics, And Security

- Structured error codes.
- Structured warnings.
- Strict validation mode.
- Permissive recovery mode.
- Malformed header diagnostics.
- Invalid offset diagnostics.
- Invalid tag type/count diagnostics.
- Missing required tag diagnostics.
- Unsupported compression diagnostics.
- Unsupported photometric diagnostics.
- Codec expansion limit diagnostics.
- Cycle detection diagnostics.
- Duplicate tag diagnostics.
- Truncated stream diagnostics.
- Data overlap diagnostics.
- Integer overflow diagnostics.
- Allocation limit diagnostics.
- Test corpus metadata reports.
- Human-readable dump utility.

### Performance And Memory

- Span-based endian reads and writes.
- Pooled buffers for strip/tile decode.
- Streaming read of large images.
- Streaming write of large images.
- Avoid whole-file loading.
- Lazy tag value materialization.
- Lazy compressed segment reads.
- Reuse codec state where valid.
- Optional memory-map-like abstraction only if built-in APIs are enough.
- Benchmark low-level IFD parsing.
- Benchmark strip/tile decode paths.
- Benchmark high-level RGBA conversion.
- Allocation budget tests.

### Developer Tooling

- Unit tests for every binary primitive.
- Golden-file tests for TIFF/BigTIFF headers and IFDs.
- Round-trip tests for every supported tag type.
- Round-trip tests for every implemented compression.
- Round-trip tests for stripped/tiled/planar images.
- Negative tests for malformed files.
- Syntax conformance tests remain green.
- Fuzz-style generated malformed inputs.
- Public API smoke tests.
- Sample command-line metadata dumper.
- Sample command-line tile extractor.
- Sample command-line transcoder.
- Documentation examples.

## Public API Completion Tasks

- [DONE] T001: Introduce a public random-access data-source abstraction over seekable `Stream` and built-in .NET file APIs so opened files, images, tag values, strips, and tiles can remain lazy without requiring or exposing a complete `byte[]` copy.
- [DONE] T002: Complete public reader lifetime and ownership behavior for paths, streams, memory, metadata-only mode, `LeaveOpen`, non-seekable rejection, trailing-data preservation and access, and deterministic close semantics.
- [DONE] T003: Add high-level typed and raw tag-value access on `ITiffImage`, including scalar and array values for every classic TIFF and BigTIFF data type, defaulted values, lazy external values, unknown tags, original tag order, and nested IFD pointers.
- [DONE] T004: Complete `TiffTagId` and the default tag registry for all baseline, extension, fax, JPEG, EXIF/GPS/interoperability, depth, GeoTIFF, GDAL, metadata, and DNG tags named by the feature inventory, with their allowed types, counts, and defaults.
- [DONE] T005: Expand normalized image information to expose volumetric depth, tile depth, subfile classification, resolution and position, min/max sample values, chromaticities, ink metadata, transfer data, JPEG tables, CFA information, and other planned baseline and extension fields.
- [DONE] T006: Add public strip and tile layout models and helpers for coordinates, sample planes, decoded and stored byte counts, partial edge segments, sparse segments, planar layouts, contiguous-data detection, and optional chopped-strip views.
- [DONE] T007: Add allocation-free scanline reading methods on `ITiffImage` that write to caller-provided `Span<byte>` buffers for stripped and supported tiled images and all planar configurations.
- [DONE] T008: Add rectangular region models and allocation-free region reading methods that operate over strips or tiles, support source samples and normalized output, and avoid decoding unrelated segments.
- [DONE] T009: Add source-raster and individual sample-plane reading methods on `ITiffImage`, including caller-provided span destinations and explicit result metadata for packed, integer, floating-point, mixed-bit-depth, and planar data.
- [DONE] T010: Add high-level RGBA8 and RGBA16 reading methods on `ITiffImage` with public conversion options for orientation, alpha association, min/max scaling, fill order, photometric conversion, and unsupported-feature handling.
- [DONE] T011: Add high-level navigation for thumbnails, reduced-resolution images, SubIFD pyramids, series, and multidimensional planes, including explicit C/Z/T coordinates and direct tile lookup and reading at a selected pyramid level.
- [DONE] T012: Expand OME-TIFF public models and operations to expose dimension order, plane-to-IFD mappings, UUID and file references, single-file and multi-file datasets, partial metadata, and OME pyramid relationships using built-in XML APIs only.
- [DONE] T013: Expand ImageJ public models and operations to expose hyperstack axes, calibration, ranges, labels, LUTs, properties, overlays, and supported private metadata blocks while preserving unrecognized blocks.
- [DONE] T014: Add writer image and layout configuration models for source pixel format, dimensions, depth, planar organization, strips or tiles, compression, predictor, byte order, alpha, sparse segments, and required-tag validation without forcing callers to construct structural tags manually.
- [DONE] T015: Add allocation-free scanline, rectangular-region, and individual sample-plane writing methods to `ITiffWriter`, with explicit ordering and storage-layout rules.
- [DONE] T016: Add full-raster writer methods for source samples, RGBA8, and RGBA16 using `ReadOnlySpan<byte>` or `ReadOnlySpan<ushort>`, including conversion to supported photometric and sample layouts.
- [DONE] T017: Integrate SubIFD and pyramid creation into `ITiffWriter`, including ordered overview levels, reduced-resolution classification, per-level compression and tiling, and automatic classic TIFF or BigTIFF offset handling.
- [DONE] T018: Add high-level metadata readers on `ITiffImage` for EXIF, GPS, XMP, ICC, Photoshop resources, IPTC, ImageDescription, OME, ImageJ, GDAL, GeoTIFF, and recognized vendor metadata without requiring callers to pass directories and complete file buffers.
- [DONE] T019: Add high-level metadata writers on `ITiffWriter` for EXIF, GPS, XMP, ICC, Photoshop image resources, IPTC, ImageDescription, OME, ImageJ, GDAL, and GeoTIFF for both classic TIFF and BigTIFF.
- [DONE] T020: Add public metadata update, removal, copy, and unknown-metadata preservation operations that can rewrite compatible files without decoding image data and clearly report when raw image data can or cannot be passed through.
- [DONE] T021: Add public date/time parsing and formatting helpers and structured recognition for JSON-shaped ImageDescription content while retaining the exact original text and bytes.
- [DONE] T022: Expand GeoTIFF models with resolved GeoKey values, EPSG identifiers, user-defined CRS data, private keys, RasterPixelIsArea/RasterPixelIsPoint semantics, forward and inverse transforms, and explicit diagnostics when keys or model tags conflict.
- [DONE] T023: Add external-overview resolution hooks based on caller-supplied paths or streams, without filesystem conventions hidden inside the library, and expose relationships between base images and external overview images.
- [DONE] T024: Expand DNG and vendor-variant recognition models to expose the planned DNG, Aperio/SVS, Hamamatsu NDPI, Leica SCN, Philips, Roche/Ventana BIF, Zeiss LSM, Micro-Manager, and Astro-TIFF fields while preserving vendor-private payloads.
- [DONE] T025: Complete public color and sample conversion controls for bilevel, sub-byte grayscale and palette, integer and floating grayscale, RGB/RGBA, CMYK, YCbCr, CIELab/ICCLab/ITULab, LogL/LogLuv, CFA, extra samples, byte swapping, predictors, and premultiplication behavior.
- [DONE] T026: Integrate application-provided `ITiffCompressionCodec` instances into normal image reading and writer workflows through read and write options, with separate decode, encode, and raw-pass-through capability reporting.
- [DONE] T027: Complete directional codec capability reporting and public diagnostics for None, PackBits, Deflate/ZLib, LZW, CCITT Modified Huffman/Group 3/Group 4, legacy word-aligned Huffman, ThunderScan, NeXT, old-style JPEG, current JPEG, and external-codec compression identifiers.
- [DONE] T028: Add public progress reporting through a restricted-syntax interface and cancellation through `CancellationToken` variants for long-running open, validation, decode, region, copy, and write operations.
- [DONE] T029: Complete strict and permissive public validation results with structured locations and error codes for malformed headers, offsets, ranges, types, counts, required tags, cycles, duplicates, truncation, overlaps, integer overflow, allocation limits, codec expansion, unsupported compression, unsupported photometric modes, and photometric mismatches.
- [DONE] T030: Add public memory-policy options for maximum allocation and decode sizes, lazy tag thresholds, chopped strips, pooled segment buffers, and streaming behavior, with predictable failures before oversized allocations.
- [DONE] T031: Complete high-level Cloud Optimized GeoTIFF writing and validation APIs for tiled imagery, ordered IFD and data placement, internal overviews, monotonic offsets and byte counts, BigTIFF rules, and range-friendly layout.
- [DONE] T032: Add tests for every public enum value, static tag identifier, data-class constructor and copy behavior, service factory, interface member, success result, diagnostic result, invalid argument, and closed-object behavior in `src/Api`.
- [DONE] T033: Add public API tests for classic TIFF and BigTIFF opening and writing in both byte orders through byte arrays, spans, paths, and streams, including ownership, non-seekable streams, lazy access, trailing bytes, multi-page chains, cycles, and large synthetic offsets.
- [DONE] T034: Add public API tests for all TIFF field types, inline and external values, ASCII termination, rational values, defaults, custom and unknown tags, duplicate and invalid tags, original order, mutation, nested SubIFDs, and EXIF/GPS/interoperability pointers.
- [DONE] T035: Add public API tests for stripped, tiled, planar-contiguous, planar-separate, partial-edge, sparse, chopped, and volumetric layouts across raw, encoded, scanline, region, source-raster, and sample-plane reads and writes.
- [DONE] T036: Add public codec API conformance and round-trip tests for every built-in encode/decode path, predictor, fill order, compression option, expansion limit, malformed stream, unsupported codec, custom codec, and raw-pass-through path using authoritative vectors where available.
- [DONE] T037: Add public raster API tests covering every planned bits-per-sample, sample-format, photometric, alpha, planar, byte-order, predictor, orientation, min/max scaling, palette, and high-level RGBA8/RGBA16 conversion combination, including buffer-too-small and overflow failures.
- [DONE] T038: Add end-to-end public writer round-trip tests for scanline, strip, tile, region, sample-plane, source-raster, RGBA8, RGBA16, multi-page, SubIFD, pyramid, classic TIFF, BigTIFF, byte-order, compression, predictor, and auto-upgrade workflows.
- [DONE] T039: Add public metadata API read, write, update, removal, preservation, and copy tests for EXIF, GPS, XMP, ICC, Photoshop resources, IPTC, plain and JSON ImageDescription, OME, ImageJ, GDAL, GeoTIFF, unknown metadata, and byte-for-byte raw preservation.
- [DONE] T040: Add public scientific and slide API tests for OME single-file and multi-file mappings, ImageJ hyperstacks, multidimensional series coordinates, pyramids, sparse large-slide tiles, volume/depth tags, DNG recognition, and every planned vendor recognizer.
- [DONE] T041: Add public GeoTIFF and Cloud Optimized GeoTIFF tests for model tags, GeoKeys and parameters, EPSG and user-defined CRS exposure, pixel-is-area/point behavior, forward and inverse transforms, private-key preservation, internal and external overviews, layout requirements, and malformed geospatial metadata.
- [DONE] T042: Add public validation and resource-safety tests for strict and permissive modes, every structured error code and location, dimension and allocation limits, codec expansion limits, cancellation, progress callbacks, malformed generated inputs, and deterministic cleanup after failure.
- [DONE] T043: Add public copy and rewrite workflow tests proving compatible compressed image data remains byte-identical, incompatible layouts and codecs are rejected with diagnostics, metadata-only changes avoid raster decoding, and path and stream ownership rules are preserved.
