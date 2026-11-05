# tiff

A managed C# TIFF and BigTIFF library for reading metadata, validating files,
reading raw image data, converting common images to RGBA buffers, writing simple
TIFF files, and copying metadata or raw image data between files.

## First Alpha Release Notes

The first alpha is intended for early integration testing of managed TIFF and
BigTIFF workflows. The API surface is usable but still pre-release: callers
should keep diagnostics visible, validate output files in their own pipelines,
and expect naming or overload adjustments before a stable package.

### First Alpha Feature Matrix

| Surface | Alpha status | Exact scope |
| --- | --- | --- |
| Target runtime | Supported | `net10.0`; managed C# using this library and built-in .NET APIs only. |
| File containers | Supported | Classic TIFF and BigTIFF, little-endian and big-endian headers, IFD chains, and multi-page files. |
| IFD and tag values | Supported | Baseline TIFF data types, BigTIFF `Long8`/`SLong8`/`Ifd8`, ASCII, rational values, sorted IFD writing, defaulted tag lookup, duplicate-tag diagnostics, custom/private tag registration, and raw unknown-tag preservation. |
| Image organization | Supported | Stripped images, tiled images, planar contiguous samples, planar separate samples, SubIFDs, reduced-resolution pyramid detection, and pyramid writing. |
| Raw image data | Supported | Raw/encoded strip and tile reads, writes, byte-range validation, sparse segment handling, metadata-only opens, metadata copy, and compatible raw image-data copy. |
| Built-in codecs | Supported | No compression, PackBits, LZW, Deflate/AdobeDeflate, CCITT Modified Huffman/RLE, CCITT Group 3 one-dimensional data, and CCITT Group 4. |
| Pixel helpers | Supported with diagnostics | Scanline, decoded strip/tile, source raster, RGBA8, and RGBA16 helpers for supported compression, sample format, bit depth, and photometric combinations. |
| Photometric conversion | Supported with diagnostics | WhiteIsZero, BlackIsZero, RGB, Palette, CMYK, YCbCr, CIE Lab, ICC Lab, and ITU Lab conversion paths. |
| Metadata families | Supported targeted coverage | Baseline metadata, EXIF, GPS, XMP, ICC profile tags, Photoshop/IPTC blocks, ImageJ, OME-XML, GDAL metadata, GeoTIFF model/key metadata, scientific slide recognition, and vendor metadata helpers. |
| Validation and security limits | Supported | Header and IFD validation, offset/count/truncation/cycle checks, strip/tile range validation, required-tag validation, Cloud Optimized GeoTIFF structural validation, allocation budgets, decoded-byte budgets, image dimension limits, IFD count limits, tag count limits, and SubIFD depth limits. |
| Writing | Supported | Classic TIFF and BigTIFF output, automatic BigTIFF upgrade when classic offsets would overflow, raw strip/tile writing, baseline writer defaults for minimal single-strip images, metadata copy, raw image-data copy, SubIFD/pyramid writing, and Cloud Optimized GeoTIFF layout ordering. |
| Unknown/private payloads | Raw pass-through | Unknown tags and unsupported compressed strip/tile payloads can be preserved or copied when byte ranges are valid. |
| Registered but external codecs | Extensible / raw pass-through | JPEG, Old JPEG, JPEG 2000, WebP, JPEG XL, Zstd, LZMA, JBIG, ThunderScan, Pixar, IT8, DCS, and similar payloads remain raw-pass-through by default; callers can supply an `ITiffCompressionCodec` to the segment codec API. |
| Specialized photometrics | Recognized only | TransparencyMask, ColorFilterArray, LogL, LogLuv, and LinearRaw are known enum values, but high-level conversion is limited and can report unsupported photometric diagnostics. |
| Full metadata schema validation | Partial | Metadata readers and writers cover common fields and payload locations, but the alpha does not claim exhaustive validation of every EXIF, XMP, OME, GDAL, GeoTIFF, Photoshop, or vendor schema rule. |
| Arbitrary large image materialization | Unsupported by design | Security limits intentionally prevent unbounded allocation and can fail huge declared images, tag arrays, strip/tile counts, or decoded buffers with diagnostics. |

## External Codec Backlog

This library currently follows a built-in-.NET-only rule: external native codec
packages are not used to make high-level decoding or encoding work. The items
below stay on the backlog until they can be implemented in managed code with
this library and built-in .NET APIs, or until the project explicitly changes
that rule.

| Backlog item | Current alpha behavior | Completion requirement |
| --- | --- | --- |
| JPEG-in-TIFF and Old JPEG | Compression values are recognized; raw strip/tile payloads can be preserved when byte ranges are valid; high-level decode/encode reports unsupported compression diagnostics. | Managed decoder/encoder or a documented built-in-.NET-only bridge for the TIFF JPEG storage variants. |
| JPEG 2000 | Recognized only / raw pass-through. | Managed JPEG 2000 codec with limits, malformed-input diagnostics, and round-trip tests. |
| WebP | Recognized only / raw pass-through. | Managed WebP codec or accepted built-in .NET API coverage, with encode/decode tests. |
| JPEG XL | Recognized only / raw pass-through. | Managed JPEG XL codec with allocation and expansion-limit enforcement. |
| Zstd | Recognized only / raw pass-through. | Built-in-.NET-only Zstd decoder/encoder with streaming-safe limits. |
| LZMA | Recognized only / raw pass-through. | Built-in-.NET-only LZMA decoder/encoder with expansion-limit diagnostics. |
| JBIG and JBIG2-style payloads | Recognized only / raw pass-through where represented by TIFF compression values. | Managed bilevel codec support with malformed-input tests. |
| ThunderScan, Pixar, IT8, DCS, and other registered proprietary schemes | Recognized by enum/tag metadata where known; high-level pixel APIs should not guess. | Per-codec implementation, conformance fixtures, diagnostics, and compatibility documentation. |
| SGILog compression variants and packed LogLuv edge cases | Some photometric conversion helpers exist, but registered compression/storage variants are not complete high-level codecs. | Complete managed codec coverage and pixel round-trip tests for each supported storage variant. |

### Custom Compression Codecs

`ITiffCompressionCodec` is the public substitution boundary for compression
formats that are not built in, or for callers that need to replace a built-in
implementation. Pass an implementation to `TiffApi.Codec.DecodeWithCompressionCodec`
or `TiffApi.Codec.EncodeWithCompressionCodec`. The custom codec
takes precedence when it reports support for the context's compression value;
otherwise the built-in implementation remains the fallback.

The interface operates inside the common TIFF codec pipeline. The library still
validates resource limits and handles fill order, predictors, and multibyte
sample order. An implementation is responsible only for the compression format
and returns failures through `TiffCodecResult.Diagnostics`. This boundary also
allows applications to test their TIFF orchestration with a deterministic fake
codec instead of constructing compressed fixtures.

## Compatibility

This section describes the current compatibility surface. "Supported" means the
library has read/write, validation, or conversion logic for the feature.
"Raw pass-through" means the library can preserve the bytes or tags without
fully decoding their semantics. "Recognized only" means the tag, enum value, or
metadata family is known by name but only partially handled. "Unsupported" means
high-level decode, encode, or semantic interpretation is expected to report a
diagnostic instead of silently guessing.

### Supported

| Area | Current support |
| --- | --- |
| File formats | Classic TIFF and BigTIFF headers, IFD chains, little-endian and big-endian primitive parsing/writing. |
| IFD data types | TIFF baseline types plus BigTIFF `Long8`, `SLong8`, and `Ifd8`. |
| Image layout | Single-page and multi-page files, stripped images, tiled images, planar contiguous and separate samples, SubIFDs, and reduced-resolution pyramid detection/writing. |
| Raw image access | Raw/encoded strip and tile reads and writes, byte-range validation, allocation limits, and metadata-only opens that block raw data reads. |
| Decoded image access | Scanline, strip, tile, source raster, RGBA8, and RGBA16 helpers for supported sample layouts and photometric conversions. |
| Compression codecs | `None`, PackBits, LZW, Deflate/AdobeDeflate, CCITT Modified Huffman/RLE, CCITT Group 3 one-dimensional data, and CCITT Group 4. |
| Photometric conversion | WhiteIsZero, BlackIsZero, RGB, Palette, CMYK, YCbCr, CIE Lab, ICC Lab, and ITU Lab conversion paths where the sample format and bit depth are supported. |
| Metadata | Baseline tags, custom/private tag registration, EXIF, GPS, XMP, ICC profile tags, Photoshop/IPTC blocks, ImageJ, OME-XML, GDAL metadata, GeoTIFF model/key metadata, scientific slide metadata recognition, and vendor metadata helpers. |
| Validation | Header/IFD validation, malformed offset/count/cycle/truncation checks, strip/tile range validation, required-tag validation, Cloud Optimized GeoTIFF validation, and security-oriented size/allocation limits. |
| Writing | High-level writer for classic TIFF and BigTIFF, auto-upgrade to BigTIFF when classic offsets would overflow, raw strip/tile writing, metadata copying, raw image data copying, SubIFD/pyramid writing, and Cloud Optimized GeoTIFF layout ordering. |

### Raw Pass-Through

| Area | Current behavior |
| --- | --- |
| Unknown/private tags | Preserved by default through `TiffReadOptions.PreserveUnknownTags` and copy helpers; custom definitions can give private tags names and preferred types. |
| Unsupported compression payloads | Raw strip/tile bytes can be read with `ReadRawStrip` or `ReadRawTile` and copied with `TiffApi.Copy.CopyRawImageData` when the source byte ranges are valid. Encoded reads decode through a matching `ITiffCompressionCodec` when one is supplied. |
| Uninterpreted metadata payloads | Raw tag values can be retained even when no specialized semantic reader exists for that private/vendor payload. |
| Sparse image segments | Zero offsets or zero byte counts are represented as sparse byte ranges and can be preserved without forcing allocation of missing data. |

### Recognized Only

| Area | Current behavior |
| --- | --- |
| Registered compression values | JPEG, Old JPEG, JPEG 2000, JBIG, LZMA, Zstd, WebP, JPEG XL, SGILog, Pixar, IT8, ThunderScan, and other registered enum values are recognized by name but do not all have high-level decode/encode codecs. |
| Specialized photometrics | TransparencyMask, ColorFilterArray, LogL, LogLuv, and LinearRaw are known enum values, but high-level RGBA conversion is limited and may report unsupported photometric diagnostics. |
| Rich metadata ecosystems | EXIF/GPS/XMP/ICC/IPTC/ImageJ/OME/GDAL/GeoTIFF/vendor families have targeted readers and writers, but this library does not claim complete coverage of every vendor-specific field or schema extension. |
| Cloud Optimized GeoTIFF | The validator checks structural layout expectations and the writer can order output for COG-style layout, but it is not a full geospatial server or tiling pipeline. |

### Unsupported

| Area | Expected behavior |
| --- | --- |
| JPEG-style image decode/encode | JPEG, Old JPEG, JPEG 2000, WebP, JPEG XL, and similar external codecs are not decoded or encoded by the high-level pixel APIs. Use raw pass-through if preservation is enough. |
| Proprietary compression schemes | Proprietary or rarely used schemes such as ThunderScan, Pixar, IT8, DCS, JBIG, LZMA, Zstd, and SGILog variants are not fully decoded/encoded unless a dedicated codec exists in this library. |
| Unsupported sample layouts | High-level pixel conversion can reject unsupported bit depths, sample formats, compression modes, or photometric combinations with diagnostics. |
| Full semantic validation of all metadata schemas | The library parses and writes many common metadata families, but it does not validate every EXIF, XMP, OME, GDAL, GeoTIFF, Photoshop, or vendor schema rule. |
| Arbitrary large image materialization | Security limits intentionally prevent unbounded allocations. Huge declared dimensions, tag counts, strip/tile counts, or decoded buffers can fail with allocation/limit diagnostics. |

## Usage Examples

The examples below assume:

```csharp
using tiff;
```

Earlier alpha entry points were reorganized as instance services: `TiffFile.OpenRead`
became `TiffApi.Factory.OpenReadPath` (with source-specific variants),
`TiffInspector.Inspect` became `TiffApi.Inspection`, `TiffCodec.ReadRgba8`
became `TiffApi.Codec.ReadRgba8`, `TiffWriter.Create` became
`TiffApi.Factory.CreateWriter` or a destination-specific variant, and
`TiffCopy.CopyMetadata` became `TiffApi.Copy.CopyMetadata`.

### Opening A TIFF

Open from a path, inspect the first image, and read basic image information:

```csharp
TiffFile file = TiffApi.Factory.OpenReadPath("input.tif", new TiffReadOptions());
if (!file.GetSuccess())
{
    TiffDiagnostics diagnostics = file.GetDiagnostics();
    throw new InvalidOperationException(diagnostics.Format(diagnostics.Get(0)));
}

Console.WriteLine(file.GetHeader().FileKind);
Console.WriteLine(file.GetImageCount());

TiffImage image = file.GetImage(0);
TiffImageInfo info = image.GetInfo();
Console.WriteLine(info.Width + " x " + info.Length);
Console.WriteLine(info.Compression);
```

For metadata-only workflows, use `OpenMetadata`; raw strip/tile reads are then
blocked by design:

```csharp
TiffFile metadataOnly = TiffApi.Factory.OpenMetadataPath("input.tif", new TiffReadOptions());
TiffImage image = metadataOnly.GetImage(0);
TiffImageInfo info = image.GetInfo();
```

### Inspecting Metadata

Use `TiffApi.Inspection` for a compact summary of the header, images, tags, and
SubIFD counts:

```csharp
TiffInspectionResult inspection = TiffApi.Inspection.InspectPath("input.tif", new TiffReadOptions());
if (!inspection.Success)
{
    throw new InvalidOperationException(inspection.Diagnostics.Format(inspection.Diagnostics.Get(0)));
}

Console.WriteLine(inspection.Header.ByteOrder);
Console.WriteLine(inspection.FileByteCount);

for (int imageIndex = 0; imageIndex < inspection.Images.Length; imageIndex++)
{
    TiffInspectionImage image = inspection.Images[imageIndex];
    Console.WriteLine(image.Info.Width + " x " + image.Info.Length);
    Console.WriteLine("Tags: " + image.Tags.Length);
    Console.WriteLine("SubIFDs: " + image.SubImageCount);
}
```

### Reading Pixels

For a common display-oriented path, convert the first image to packed RGBA8
bytes. The result buffer is row-major, four bytes per pixel: red, green, blue,
alpha.

```csharp
TiffFile file = TiffApi.Factory.OpenReadPath("input.tif", new TiffReadOptions());
TiffImage image = file.GetImage(0);

TiffRgba8ReadResult rgba = TiffApi.Codec.ReadRgba8(
    image.GetDirectory(),
    file.GetFileBytes(),
    file.GetHeader().ByteOrder);

if (!rgba.Success)
{
    throw new InvalidOperationException(rgba.Diagnostics.Format(rgba.Diagnostics.Get(0)));
}

int firstPixelRed = rgba.Bytes[0];
int firstPixelGreen = rgba.Bytes[1];
int firstPixelBlue = rgba.Bytes[2];
int firstPixelAlpha = rgba.Bytes[3];
```

For raw access, read encoded strip or tile bytes directly:

```csharp
TiffRawStripReadResult strip = image.ReadRawStrip(0);
byte[] encodedStripBytes = strip.Bytes;
```

### Writing Pixels

For a simple uncompressed 8-bit grayscale image, the pixel bytes are the raw
strip bytes:

```csharp
byte[] pixels = new byte[]
{
    0, 64,
    128, 255
};

TiffTagValue[] tags = new TiffTagValue[]
{
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 2),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 2),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 2)
};

TiffWriter writer = TiffApi.Factory.CreatePathWriter("gray.tif", new TiffWriteOptions());
int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(tags));
writer.WriteRawStrip(imageIndex, 0, pixels);

TiffDiagnostics diagnostics = writer.Flush();
if (diagnostics.HasErrors())
{
    throw new InvalidOperationException(diagnostics.Format(diagnostics.Get(0)));
}
```

To write a tiled image, provide tile layout tags and call `WriteRawTile` for
each tile.

### Copying Metadata

Copy non-structural metadata tags from one image to a new image definition while
supplying new pixel geometry and pixel data:

```csharp
TiffFile sourceFile = TiffApi.Factory.OpenReadPath("source.tif", new TiffReadOptions());
TiffImage sourceImage = sourceFile.GetImage(0);

TiffMetadataCopyResult metadata = TiffApi.Copy.CopyMetadata(sourceImage);
if (!metadata.Success)
{
    throw new InvalidOperationException(metadata.Diagnostics.Format(metadata.Diagnostics.Get(0)));
}

TiffImageDefinition destination = TiffApi.Data.CreateImageDefinition(new TiffTagValue[]
{
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 2),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 2),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
    TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 2)
});

for (int index = 0; index < metadata.Values.Length; index++)
{
    TiffApi.Data.SetImageTag(destination, metadata.Values[index]);
}

TiffWriter writer = TiffApi.Factory.CreatePathWriter("metadata-copy.tif", new TiffWriteOptions());
int destinationImageIndex = writer.AddImage(destination);
writer.WriteRawStrip(destinationImageIndex, 0, new byte[] { 1, 2, 3, 4 });
writer.Flush();
```

For compatible images, `TiffApi.Copy.CopyRawImageData` can copy the raw strip or
tile payloads and the required geometry/storage tags into a `TiffImageDefinition`.
