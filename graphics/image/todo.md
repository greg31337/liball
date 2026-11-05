# Universal Image Metadata Extraction Library Plan

## Scope

Build a dependency-free C# library for extracting image metadata from popular raster and vector image formats. The first product surface is read-only extraction, format detection, metadata normalization, diagnostics, and raw metadata preservation. Write/edit support can be designed later, but the read model should not block future writers.

Implementation constraints:

- Use only built-in .NET APIs.
- Follow `syntax.md`: file-scoped namespaces, supported interfaces/classes/enums only, no properties, no records, no structs, nullable reference annotations only, no LINQ, no `try`/`catch`, no generic method declarations.
- Use only `static`, `public`, `internal`, and `const` modifiers. Reserve `public` for the supported API listed below and declare parser or implementation details `internal`.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member, using only the XML constructs allowed by `syntax.md`.
- Avoid external codecs and avoid full pixel decoding except where small header reads are needed.
- Treat malformed metadata as diagnostics instead of throwing from ordinary parser flow.

## Research Baseline

Existing libraries and tools suggest the target feature set:

- ExifTool: broadest metadata coverage; reads EXIF, GPS, IPTC, XMP, JFIF, GeoTIFF, ICC, Photoshop IRB, FlashPix, AFCP, C2PA/JUMBF, maker notes, thumbnails/previews, alternate-language tags, grouped output, metadata comparison, and raw metadata backup. Source: https://exiftool.sourceforge.net/
- Exiv2: image-focused matrix for JPEG, TIFF/BigTIFF, PNG, WebP, JP2, AVIF, HEIF/HEIC, JPEG XL, PSD, EPS, RAW formats, EXIF/IPTC/XMP/comments/ICC/thumbnails, sidecar extraction, and tag key syntax. Source: https://exiv2.org/manpage.html
- MetadataExtractor for .NET: simple reader API returning directories and tags; supports EXIF, IPTC, XMP, JFIF/JFXX, ICC, Photoshop, PNG/BMP/GIF/ICO/PCX/WebP properties, HEIF/AVIF, TIFF/BigTIFF, PSD, TGA, Netpbm, and camera RAW. Source: https://www.nuget.org/packages/metadataextractor/
- WIC metadata model: decoder/frame/block hierarchy, query-like paths, metadata handlers for App0/App1/App13/IFD/IRB/EXIF/XMP/GPS/IPTC/PNG text. Source: https://learn.microsoft.com/en-us/windows/win32/wic/-wic-about-metadata
- ImageMagick `identify`: common image facts include format, MIME type, dimensions, page geometry, colorspace, depth, compression, orientation, and corruption reporting. Source: https://imagemagick.org/identify/
- libvips: uniform metadata fields with typed values such as int, double, string, and binary blobs; this supports a simple typed tag model. Source: https://www.libvips.org/API/8.11/cpp/

## High-Level Development Plan

Phase 1: Core infrastructure.
Create byte readers, bounds checking, format detection, diagnostics, typed metadata values, directory/tag containers, and a public read API.

Phase 2: Common web formats.
Implement JPEG, PNG, GIF, WebP, BMP, ICO/CUR, SVG, and basic image property extraction.

Phase 3: Core metadata standards.
Implement TIFF/BigTIFF IFD parsing, EXIF, GPS, interoperability IFDs, embedded thumbnails, XMP XML extraction, IPTC IIM, ICC profile parsing, Photoshop IRB, JFIF/JFXX, Adobe APP14, comments, and common normalized fields.

Phase 4: Modern containers.
Implement ISO BMFF parsing for HEIF/HEIC/AVIF/CR3 and JPEG 2000/JPEG XL box parsing, including dimensions, item properties, color metadata, orientation, EXIF/XMP/ICC/JUMBF blocks, and thumbnails where practical.

Phase 5: Professional and legacy formats.
Implement PSD/PSB, TIFF-derived RAW formats, CRW/CIFF, RAF wrappers, Netpbm, PCX, TGA, DDS, QOI, Radiance HDR, EPS metadata, and safe generic extraction for unknown TIFF-like RAW files.

Phase 6: Higher-level user features.
Add query paths, normalized metadata, GPS conversion, date parsing, sidecar XMP support, recursive directory scanning helpers, JSON/text formatting, metadata-only hashes, conformance tests, corpus tests, fuzz-style malformed input tests, and documentation.

## Complete Feature List, Low Level To High Level

Binary and stream primitives:

- Bounded stream reads with byte count limits.
- Big-endian and little-endian integer readers.
- TIFF rational and signed rational value readers.
- ISO BMFF box size, largesize, UUID, and nested box traversal.
- PNG chunk reader with CRC capture.
- RIFF chunk reader with padding handling.
- JPEG marker segment scanner with restart/SOS awareness.
- GIF block and sub-block reader.
- PSD section reader and Pascal/Unicode string helpers.
- Text token reader for Netpbm and EPS comments.
- Zlib/deflate decompression for PNG `zTXt`, `iTXt`, and `iCCP` using built-in .NET compression APIs.
- XML reading for XMP and SVG using built-in .NET XML APIs.
- Diagnostic collector for warnings, recoverable errors, unsupported chunks, truncation, overflow, and size-limit stops.
- Raw block capture with offset, length, block type, and optional payload.

Format detection:

- Magic-number detection before extension fallback.
- MIME type and preferred extension mapping.
- Ambiguous TIFF-like RAW detection.
- Container brand detection for HEIF, AVIF, CR3, JP2, and JPEG XL.
- Multi-frame and multi-image counting where the container exposes it cheaply.
- Unknown format result with diagnostics.

Core model:

- `ImageMetadataDocument` with format, MIME type, byte length, frames, directories, tags, profiles, raw blocks, and diagnostics.
- `ImageMetadataFrame` for frame/page/item-level dimensions and metadata.
- `MetadataDirectory` grouped by standard, container block, frame, or maker note.
- `MetadataTag` with key, name, family, group, numeric id, value type, raw value, display value, offset, length, confidence, and repeat index.
- `MetadataValue` supporting empty, integer, unsigned integer, rational, signed rational, double, string, string list, byte array, date text, XML text, and nested directory reference.
- `MetadataProfile` for ICC, EXIF blob, XMP packet, IPTC blob, Photoshop IRB, JUMBF/C2PA, and unknown binary profiles.
- Tag lookup tables for common EXIF, GPS, IPTC, XMP, TIFF, PNG, JPEG, WebP, BMFF, ICC, and Photoshop names.
- Normalized metadata summary for dimensions, orientation, camera make/model, lens, capture time, GPS, author, copyright, title, description, keywords, rating, color profile, color space, resolution, software, and thumbnails.

Image properties:

- Width, height, frame count, page/canvas size, pixel aspect ratio, resolution, units, color type, colorspace, bit depth, channel count, alpha presence, animation presence, loop count, duration, compression, interlace/progressive flag, orientation, and thumbnail/preview metadata.
- Corruption/incomplete-file detection where the header or block structure indicates it.
- Embedded profile presence and byte lengths.

Metadata standards:

- EXIF/TIFF IFD0, ExifIFD, GPSIFD, InteroperabilityIFD, SubIFDs, thumbnail IFDs, orientation, date/time, exposure, lens, camera, resolution, color, and user comment tags.
- TIFF 6.0 and BigTIFF typed values, inline/out-of-line values, multi-value arrays, endian handling, nested IFDs, and loop detection.
- MakerNote preservation for all vendors as raw directories.
- Selected MakerNote decoding for Apple, Canon, Nikon, Sony, Fujifilm, Olympus, Panasonic/Leica, Pentax, Samsung, DJI, GoPro, Ricoh, Minolta, Kodak, Sigma/Foveon, FLIR, and Hasselblad.
- IPTC IIM datasets including caption, writer, credit, source, headline, object name, keywords, category, supplemental categories, city, state, country, copyright, date/time, urgency, byline, and contact.
- XMP packet extraction, UTF-8/UTF-16 detection, extended XMP packet joining for JPEG, common namespaces, arrays, language alternatives, structured values, and sidecar `.xmp` reads.
- ICC profile header parsing, tag table parsing, profile class, color space, PCS, creation date, rendering intent, white point, description, copyright, and raw profile preservation.
- Photoshop Image Resource Blocks including IPTC, resolution info, clipping path names, layer comps metadata, print scale, alpha channel names, EXIF/XMP-related resources, and thumbnails.
- JFIF/JFXX density, units, version, thumbnail metadata.
- Adobe APP14 color transform and version flags.
- JPEG comments and Ducky APP12 preservation.
- MPF and FlashPix metadata discovery.
- C2PA/JUMBF block discovery and raw preservation.

Format-specific coverage:

- JPEG/JFIF/JPEG-LS: APP0-APP15, COM, SOF dimensions, progressive/baseline, EXIF, XMP, extended XMP, ICC, IPTC/Photoshop, Adobe APP14, JFIF/JFXX, MPF, FlashPix, C2PA/JUMBF.
- TIFF/BigTIFF: classic and BigTIFF headers, IFD traversal, EXIF/GPS/SubIFD, GeoTIFF keys, thumbnails, multipage frames, DNG and TIFF-like RAW.
- PNG/APNG: IHDR, PLTE, gAMA, cHRM, sRGB, iCCP, eXIf, tEXt, zTXt, iTXt, pHYs, tIME, bKGD, tRNS, cICP, mDCv, cLLi, acTL, fcTL, fdAT, unknown ancillary chunk preservation.
- GIF: logical screen descriptor, global/local color tables, graphic control extension, comment extension, application extensions, Netscape loop count, XMP application block, ICC application block where present, animation timing.
- WebP: RIFF/WEBP, VP8, VP8L, VP8X, ALPH, ICCP, EXIF, XMP, ANIM, ANMF, canvas size, alpha, animation.
- BMP/DIB: file header, DIB variants, width, height, bit depth, compression, resolution, color masks, color table, embedded ICC profile in V4/V5 headers.
- ICO/CUR: directory entries, embedded BMP/PNG images, sizes, bit depth, cursor hotspot.
- SVG/SVGZ: XML metadata element, title, desc, dimensions, viewBox, RDF/XMP extraction, image links as text only.
- HEIF/HEIC/AVIF: BMFF brands, meta/item boxes, primary item, dimensions, pixel info, color info, rotation/mirror/clap, EXIF item, XMP item, ICC/nclx, thumbnails, auxiliary alpha/depth items, JUMBF discovery.
- JPEG 2000/JP2/J2K/JPM/JPX: signature, file type, image header, color specification, resolution boxes, XML boxes, UUID boxes for EXIF/XMP/IPTC/GeoTIFF/JUMBF where present.
- JPEG XL/JXL: container boxes, codestream basic info where feasible, EXIF/XMP/JUMBF boxes, color encoding, orientation, animation basics; naked codestream has limited metadata.
- PSD/PSB: file header, color mode data length, image resources, EXIF/XMP/IPTC/ICC resources, resolution, thumbnails, layer metadata summary.
- Camera RAW: DNG, NEF, ARW, ORF, PEF, RW2, SRW, 3FR, KDC, MRW, RWL, GPR, CR2, CR3, CRW/CIFF, RAF; extract TIFF/BMFF/CIFF metadata and embedded JPEG previews when discoverable.
- Netpbm: PBM, PGM, PPM, PAM, PFM dimensions, max value, tuple type, endian marker for PFM, comments.
- PCX: header dimensions, bit depth, planes, palette info, DPI, version.
- TGA: header dimensions, pixel depth, image descriptor, origin, color map, extension area metadata, developer area offset.
- DDS: magic, header, DX10 header, dimensions, mipmaps, array size, cube map flags, pixel format.
- QOI: magic, dimensions, channels, colorspace.
- Radiance HDR/RGBE: header variables, format, exposure, gamma, dimensions.
- EPS/PS image files: DSC comments, bounding box, creator, creation date, title, XMP packet if present.

Query and presentation:

- Exact key lookup by family/group/tag.
- Path-like lookup similar to WIC and Exiv2 key syntax.
- Directory and tag enumeration preserving original order.
- Normalized summary lookup for common application fields.
- Raw profile extraction by family.
- Human-readable descriptions for common enumerated values.
- Machine-readable values for precise consumers.
- JSON and plain text formatting helpers.
- Metadata diff helper for two documents.
- Metadata-only hash helper for selected raw blocks or image data ranges where the format permits separating pixels from metadata.

Reliability and security:

- Max file size, max segment size, max decompressed bytes, max XML bytes, max IFD count, max nesting depth, and max tag count options.
- Overflow-checked offset arithmetic.
- Loop detection for IFD chains and nested boxes.
- Graceful partial extraction after malformed blocks.
- No executing external files or resolving remote references.
- XML settings that prohibit DTD and external entity expansion.
- Tests for truncation, huge declared lengths, circular IFDs, zip bombs, invalid encodings, duplicate tags, and unknown vendor blocks.

## Public API Completion Tasks

All source work below must use only built-in .NET APIs. Use span-based inputs where data can be inspected without retaining it; tests may use external test packages.

- [DONE] T001: Extend public format detection with filename or extension hints, ambiguous TIFF-derived RAW resolution, unknown-format diagnostics, and a `ReadOnlySpan<byte>` input that avoids copying caller-owned probes.
- [DONE] T002: Complete the public document, frame, and normalized-summary models for pixel aspect ratio, numeric resolution and units, source color type, interlace or progressive state, animation loop count and total duration, and convenient corruption or incomplete-input status derived from diagnostics.
- [DONE] T003: Add public frame and profile identity and relationship fields for primary images, pages, thumbnails, previews, auxiliary alpha or depth images, container item identifiers, parent or source-frame links, and cropped frame placement within a canvas.
- [DONE] T004: Extend `MetadataValueKind`, `MetadataValue`, and `IImageMetadataFactory` so decoded signed, unsigned, rational, and floating-point arrays and string lists can be represented and created without falling back to display text or opaque bytes, using arrays for retained values and spans for copy-in factory inputs where appropriate.
- [DONE] T005: Add a public metadata date parser and result model that understands the planned EXIF, XMP, IPTC IIM, PNG, TGA, and EPS date forms, preserves precision and time-zone presence, and lets normalization expose a parsed capture time without discarding the original text.
- [DONE] T006: Extend the public query layer with ordered directory, profile, raw-block, thumbnail, and preview lookup by family, kind, key or path, and frame so raw profile extraction and all source-order enumeration features are available without caller-written tree traversal.
- [DONE] T007: Add a public recursive and non-recursive directory scanning service with extension or detected-format filtering, caller-supplied reader options, stable path ordering, per-file results, and per-file failure diagnostics, implemented only with built-in `System.IO` APIs.
- [DONE] T008: Extend metadata-only hashing with public selection options for metadata families, profile kinds, and raw-block types, plus span and stream inputs and an explicit result that distinguishes unsupported separation or algorithms from an empty metadata set, using only built-in .NET cryptography.
- [DONE] T009: Add exhaustive behavioral tests for every public data constructor, default field value, enum value, factory operation, interface implementation, null input, invalid input, and option preset currently exposed under `src/Api`, including compatibility coverage for both reader classes.
- [DONE] T010: Add public-entry-point integration tests that read representative files and seekable and non-seekable streams for every advertised image and XMP sidecar format, verify extension fallback and all reader options, and assert partial public results and diagnostics for malformed or limit-constrained inputs.
- [DONE] T011: Add complete public query, normalization, formatting, diff, GPS, and hash tests covering nested and repeated directories, every metadata value kind, escaped JSON and text output, all normalized fields and precedence rules, profile and raw-block differences, selectable hash ranges, and all documented failure results.
- [DONE] T012: Add complete public tests for the new common image fields, image relationships, date parsing, raw profile and thumbnail extraction, span-based operations, and directory scanning, including empty directories, inaccessible or disappearing files, symlink cycles, mixed supported and unsupported files, and deterministic ordering.
