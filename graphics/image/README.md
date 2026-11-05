# image

A dependency-free .NET library for detecting image formats and extracting structured metadata without fully decoding pixel data.

## Usage

The public model uses ordinary classes and fields, so metadata can be read without
reflection, callbacks, or format-specific parser types.

### Read a file

```csharp
using System;
using image;

IImageMetadataReader reader = new ImageMetadataReader();
ImageMetadataDocument document = reader.ReadFile("photo.jpg");

Console.WriteLine("Format: " + document.Format.Name);
Console.WriteLine("MIME type: " + document.Format.MimeType);
Console.WriteLine("Size: " + document.Summary.Width + " x " + document.Summary.Height);
Console.WriteLine("Camera: " + document.Summary.CameraMake + " " + document.Summary.CameraModel);
```

The normalized `Summary` exposes common fields across formats, including numeric
resolution, pixel aspect ratio, source color type, scan mode, and animation
timing. Format-specific values remain available through the document's
directories and tags.

Frames and image-bearing profiles expose an `ImageMetadataRole` together with
container item identifiers and frame relationships. Cropped animation frames
also report their `CanvasX` and `CanvasY` placement within the full canvas.

### Read a stream with limits

Use `ImageMetadataReaderOptions` when processing untrusted input or when raw
metadata retention needs to be controlled:

```csharp
using System.IO;
using image;

IImageMetadataFactory factory = new ImageMetadataFactory();
IImageMetadataReader reader = new ImageMetadataReader();
ImageMetadataReaderOptions options = factory.CreateConservativeReaderOptions();
options.CaptureRawBlocks = false;
options.DecodeMakerNotes = false;

using (FileStream stream = File.OpenRead("photo.jpg"))
{
    ImageMetadataDocument document =
        reader.ReadStreamWithOptions(stream, options);

    Console.WriteLine(document.Summary.Width);
    Console.WriteLine(document.Summary.Height);
}
```

`ReadStream` and `ReadStreamWithOptions` begin at the stream's current position.
Non-seekable streams are buffered up to `MaxFileBytes`.

### Use dependency injection

Applications that need to substitute metadata reads in tests can depend on
`IImageMetadataReader`. Both reader implementations use the same parser behavior:

```csharp
using image;

IImageMetadataReader reader = new DefaultImageMetadataReader();
ImageMetadataDocument document = reader.ReadFile("photo.jpg");
```

`ImageMetadataReader` is the primary implementation. `DefaultImageMetadataReader`
remains available as a compatibility name.

### Detect a format from a probe

`IImageFormatDetector` accepts arrays, caller-owned spans, and streams. A filename
or extension hint can establish a format when bytes are unavailable, and can
refine an otherwise generic TIFF or BigTIFF result to a TIFF-derived RAW format.
Specific magic bytes take precedence over conflicting hints.

```csharp
using System;
using image;

IImageFormatDetector detector = new ImageFormatDetector();
byte[] probe = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };
ImageFormatInfo format = detector.DetectSpanWithHint(probe, "photo.nef");

foreach (MetadataDiagnostic diagnostic in format.Diagnostics)
{
    Console.WriteLine(diagnostic.Code + ": " + diagnostic.Message);
}
```

### Query a format-specific tag

Queries accept a stable tag key or a family/group/tag path. Check the value kind
before reading its typed payload field:

```csharp
using System;
using image;

IImageMetadataReader reader = new ImageMetadataReader();
IImageMetadataQuery query = new ImageMetadataQuery();
ImageMetadataDocument document = reader.ReadFile("photo.jpg");
MetadataTag make = query.FindFirst(document, "Tiff.IFD0.Make");

if (make != null && make.Value.Kind == MetadataValueKind.String)
{
    Console.WriteLine(make.Value.Text);
}
```

Use `IImageMetadataQuery.Find` when repeated tags are expected; it returns a
`List<MetadataTag>` in source order.

### Inspect diagnostics

Malformed, truncated, unsupported, or safety-limited metadata is normally
reported as diagnostics. The document may still contain usable partial results.
`ImageMetadataDocument.IsCorrupt` and `IsIncomplete` provide convenient status
flags derived from structural-error and truncation diagnostics.

```csharp
using System;
using image;

IImageMetadataReader reader = new ImageMetadataReader();
ImageMetadataDocument document = reader.ReadFile("photo.jpg");

foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
{
    Console.WriteLine(
        diagnostic.Severity + ": " + diagnostic.Code + " - " + diagnostic.Message);
}
```

File access failures can still throw `IOException` or `UnauthorizedAccessException`.

### Format output

The complete document can be rendered as human-readable text or JSON:

```csharp
using System;
using image;

IImageMetadataReader reader = new ImageMetadataReader();
IImageMetadataFormatter formatter = new ImageMetadataFormatter();
ImageMetadataDocument document = reader.ReadFile("photo.jpg");

string text = formatter.ToText(document);
string json = formatter.ToJson(document);

Console.WriteLine(text);
Console.WriteLine(json);
```

## Support matrix

Coverage describes metadata extraction, not pixel decoding. All listed formats
are detected from file content; RAW filename extensions may additionally refine
an otherwise TIFF-like detection result.

### Image and container formats

| Format | Structural coverage | Metadata coverage |
| --- | --- | --- |
| JPEG | Markers, SOF dimensions, bit depth, components, baseline/progressive state | JFIF/JFXX, EXIF/TIFF/GPS, XMP and extended XMP, ICC, Photoshop IRB/IPTC, Adobe APP14, comments, MPF, FlashPix, Ducky, JUMBF/C2PA |
| TIFF and BigTIFF | Headers, typed IFD values, nested IFDs, pages, SubIFDs, thumbnails, loop detection | TIFF, EXIF, GPS, interoperability IFD, GeoTIFF keys, maker notes |
| PNG and APNG | Chunks, CRC diagnostics, image/frame dimensions, color type, animation control | EXIF, text/iTXt/zTXt, ICC, gamma/chromaticity/sRGB/CICP/HDR color chunks, physical size, time, transparency, background, unknown ancillary blocks |
| GIF | Logical screen, color tables, images, extensions, frames, animation timing and loops | Comments, application extensions, XMP, ICC |
| WebP | RIFF chunks, VP8/VP8L/VP8X dimensions, alpha, animation and frames | EXIF, XMP, ICC |
| BMP and DIB | Common DIB headers, dimensions, masks, palette, compression and resolution | Embedded V4/V5 ICC profiles |
| ICO and CUR | Directory entries, sizes, bit depth, cursor hotspots | Structural metadata from embedded PNG and BMP images |
| SVG and SVGZ | Safe XML parsing, dimensions, viewBox and image links as text | Title, description, metadata/RDF and XMP |
| HEIF, HEIC and AVIF | BMFF boxes, brands, primary item, item properties, dimensions, color, transform/crop, thumbnails and auxiliary items | EXIF, XMP, ICC/nclx, JUMBF/C2PA |
| JPEG 2000 (codestream and JP2 family) | Signature, boxes, image header, color and resolution | XML/XMP, UUID-carried EXIF/IPTC/GeoTIFF/Photoshop data, ICC, JUMBF |
| JPEG XL (codestream and container) | Boxes and limited codestream basics including dimensions, orientation, color and animation fields | EXIF, XMP, JUMBF/C2PA |
| PSD and PSB | Header, color-mode section, image resources and layer/mask section summary | EXIF, XMP, IPTC, ICC, resolution, thumbnails and Photoshop resources |
| Camera RAW | TIFF-like DNG, NEF, ARW, ORF, PEF, RW2, SRW, 3FR, KDC, MRW, RWL and GPR; CR2, BMFF-based CR3, CIFF-based CRW and RAF wrappers | Container metadata, EXIF/TIFF/GPS, maker notes, and discoverable JPEG/TIFF previews |
| Netpbm | PBM, PGM, PPM, PAM and PFM headers, dimensions, max value, tuple type and comments | Format-native header metadata |
| PCX | Header, dimensions, planes, bit depth, palette facts and DPI | Format-native header metadata |
| TGA | Header, origin, color map, extension area and developer-area location | Author/comment, timestamp, software, aspect and gamma fields from the extension area |
| DDS | Core and DX10 headers, dimensions, mipmaps, arrays, cube flags and pixel format | Format-native header metadata |
| QOI | Header, dimensions, channels and colorspace flag | Format-native header metadata |
| Radiance HDR/RGBE | Header variables, dimensions and scan orientation | Format, exposure, gamma and other text variables |
| EPS | DSC comments and bounding boxes; PostScript is not executed | Creator, creation date, title and embedded XMP |
| XMP sidecar | Safe XML packet reading | Common XMP namespaces, arrays, language alternatives and structured values |

### Metadata families

| Family | Coverage |
| --- | --- |
| TIFF, EXIF and GPS | Baseline TIFF scalar and array types, classic TIFF and BigTIFF offsets, IFD0, EXIF, GPS, interoperability and SubIFD routing, user comments, orientation and thumbnails |
| XMP | Packet extraction with UTF-8/UTF-16 handling, JPEG extended-packet joining, common namespaces, arrays, language alternatives, structured values and sidecar files |
| IPTC IIM | Common Application Record 2 datasets such as caption, byline, credit, location, keywords, dates and copyright |
| ICC | Raw profile retention plus header, tag table, description, copyright and white-point decoding |
| Photoshop IRB | Resource block traversal and common EXIF, XMP, IPTC, ICC, resolution and thumbnail resources |
| Maker notes | Raw preservation for unknown layouts and selected decoding for Apple, Canon, Nikon, Sony, Fujifilm, Olympus, Panasonic/Leica, Pentax, Samsung, DJI, GoPro, Ricoh, Minolta, Kodak, Sigma/Foveon, FLIR and Hasselblad |
| JFIF and JPEG application data | JFIF/JFXX density and thumbnail facts, Adobe APP14, comments, MPF, FlashPix and Ducky discovery |
| JUMBF and C2PA | Container discovery and raw profile preservation |
| Format-native metadata | PNG, GIF, WebP, BMP, ICO/CUR, SVG, BMFF, JPEG 2000, JPEG XL, PSD, RAW and legacy-format structural fields |
| Normalized summary | Dimensions, frame count, pixel aspect ratio, numeric resolution and units, orientation, camera and lens, capture time, GPS, descriptive fields, rating, source and normalized color, scan mode, animation loops and duration, and software when corresponding source fields exist |

### Known limitations

- The library is read-only and does not decode, render, edit, or re-encode pixel data.
- Structural and metadata coverage is intentionally bounded. Unknown blocks can be
  reported or preserved, but arbitrary vendor-private payloads are not decoded.
- Maker-note decoding is selective and vendor layouts can vary by camera and
  firmware. Preserve raw maker notes when exact vendor data is required.
- Naked JPEG XL codestreams expose fewer metadata fields than container-based
  JPEG XL files. Camera RAW preview discovery is best effort and container-dependent.
- ICC, XMP, IPTC and Photoshop parsing focuses on common interoperable fields;
  uncommon tags remain available through raw profiles when retention is enabled.
- JUMBF and C2PA data is discovered and preserved, but manifests, signatures and
  trust chains are not validated.
- SVG image links are returned as text. Remote references are never fetched, and
  EPS/PostScript content is never executed.
- Safety limits can stop traversal, decompression, XML parsing, profile retention
  or raw block capture. The result then contains partial metadata and diagnostics.
- Format detection primarily uses magic bytes and container brands. Ambiguous
  TIFF-derived RAW formats may require a filename or extension hint through the
  public detector or `ReadFile`.

## Public API policy

Only symbols declared `public` are supported API. `IImageMetadataReader` is the
substitution boundary for applications; the static reader remains the convenience
façade. Parsers, binary readers, mappings, lookup tables, and formatting/query
implementation helpers are `internal` and may change without notice.

Source code follows [syntax.md](syntax.md). In particular, source modifiers are limited to `static`, `public`, `internal`, and `const`; every public type, constructor, method, field, enum, and enum member has XML documentation using only the constructs allowed there.
