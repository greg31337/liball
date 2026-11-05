# jpeg

A pure managed .NET JPEG library focused on practical JPEG reading, writing, inspection, and metadata workflows.

## Scope

This library targets classic JPEG streams from the JPEG-1 family first: JFIF, Exif, and Adobe-style JPEG files built around ISO/IEC 10918-1 marker syntax. The implementation plan prioritizes common production images before expanding to less common JPEG-1 coding processes.

Initial compatibility goals include:

- Baseline sequential DCT JPEG with Huffman coding.
- Progressive DCT JPEG with Huffman coding.
- Common 8-bit grayscale, YCbCr, RGB, CMYK, and YCCK image data.
- JFIF APP0, Exif APP1, ICC APP2, Adobe APP14, COM, and unknown APPn marker preservation.
- Identify-only APIs that report dimensions, precision, component count, process type, metadata hints, and unsupported features without requiring a full decode.
- Encoder support for standard interoperable JFIF output, metadata preservation policies, quality control, subsampling control, restart markers, and progressive output as implementation milestones are completed.

All runtime code is intended to be pure managed C# using only built-in .NET APIs. It must not require native libraries, platform codecs, `System.Drawing`, ImageSharp, SkiaSharp, or external color-management packages.

## Examples

### Identify

Use `Identify` when you need dimensions, coding process, color hints, table counts, and metadata without decoding pixels.

```csharp
byte[] data = System.IO.File.ReadAllBytes("input.jpg");
jpeg.IJpegIdentifier identifier = new jpeg.JpegIdentifierService();

jpeg.JpegInfo info = identifier.Identify(data);

int width = info.Width;
int height = info.Height;
bool progressive = info.Progressive;
jpeg.JpegColorSpace colorSpace = info.ColorSpace;
int commentCount = info.CommentCount;
```

### Read

`Read` decodes supported baseline JPEG data to a `JpegImage`. For three-component images, set `ColorPolicy` when RGB pixels are preferred over raw YCbCr output.

```csharp
byte[] data = System.IO.File.ReadAllBytes("input.jpg");
jpeg.JpegReadOptions options = new jpeg.JpegReadOptions();
options.ColorPolicy = jpeg.JpegColorPolicy.ConvertToRgb;
options.OutputPixelFormat = jpeg.JpegPixelFormat.Rgb24;
jpeg.IJpegReader reader = new jpeg.JpegReader();

jpeg.JpegImage image = reader.ReadWithOptions(data, options);

byte[] rgbPixels = image.Pixels;
int width = image.Width;
int height = image.Height;
```

### Dependency Injection

Applications can depend on narrow service interfaces and replace them with test implementations. The library provides one constructible implementation for each public contract.

```csharp
jpeg.IJpegIdentifier identifier = new jpeg.JpegIdentifierService();
jpeg.IJpegReader reader = new jpeg.JpegReader();
jpeg.IJpegWriter writer = new jpeg.JpegWriter();

jpeg.JpegInfo info = identifier.Identify(data);
jpeg.JpegImage image = reader.Read(data);
byte[] encoded = writer.Write(image);
```

### Write

Create a `JpegImage` from packed pixels, attach metadata, and write baseline JPEG bytes. The current high-level writer supports `L8`, `Rgb24`, and `YCbCr24` input.

```csharp
byte[] rgbPixels = LoadRgbPixels();
jpeg.IJpegImageFactory imageFactory = new jpeg.JpegImageFactory();
jpeg.IJpegWriter writer = new jpeg.JpegWriter();
jpeg.JpegImage image = imageFactory.CreateImage(rgbPixels, 640, 480, jpeg.JpegPixelFormat.Rgb24);
image.Metadata.Comments.Add("created by jpeg");

jpeg.JpegWriteOptions options = new jpeg.JpegWriteOptions();
options.Quality = 85;
options.RestartInterval = 8;

byte[] output = writer.WriteWithOptions(image, options);
System.IO.File.WriteAllBytes("output.jpg", output);
```

### Metadata Preservation

Metadata-only helpers rewrite the JPEG container while preserving image coefficients and unrelated known or unknown metadata where possible.

```csharp
byte[] data = System.IO.File.ReadAllBytes("input.jpg");

System.Collections.Generic.List<string> comments = new System.Collections.Generic.List<string>();
comments.Add("reviewed");
jpeg.IJpegMetadataEditor editor = new jpeg.JpegMetadataEditor();

byte[] withComments = editor.SetComments(data, comments);
byte[] withIcc = editor.SetIccProfile(withComments, LoadIccProfile());
byte[] withExif = editor.SetExif(withIcc, LoadExifPayload());
```

### CMYK Handling

Four-component JPEG streams are identified as CMYK or YCCK when marker data provides enough information. The public `Read` API currently focuses on grayscale and three-component baseline images, so use `Identify` to detect these streams before choosing another decoding path.

```csharp
jpeg.IJpegIdentifier identifier = new jpeg.JpegIdentifierService();
jpeg.JpegInfo info = identifier.Identify(data);
if (info.ColorSpace == jpeg.JpegColorSpace.CMYK ||
    info.ColorSpace == jpeg.JpegColorSpace.YCCK)
{
    // Select an application-specific fallback before calling IJpegReader.Read.
}
```

### Progressive Output

Progressive JPEG marker streams are identified, but `IJpegWriter.WriteWithOptions` currently rejects `Progressive = true` until container-level progressive output is available.

```csharp
jpeg.JpegWriteOptions options = new jpeg.JpegWriteOptions();
options.Progressive = true;
jpeg.IJpegWriter writer = new jpeg.JpegWriter();

try
{
    byte[] output = writer.WriteWithOptions(image, options);
}
catch (jpeg.JpegUnsupportedException)
{
    // Progressive output is not yet available through the public API.
}
```

### Lossless Transforms

Lossless transform helpers operate in the coefficient domain and preserve metadata. Rotations, flips, transpose, transverse, and crops require dimensions or rectangles aligned to the JPEG MCU grid.

```csharp
byte[] data = System.IO.File.ReadAllBytes("input.jpg");
jpeg.IJpegTransformer transformer = new jpeg.JpegTransformer();

byte[] rotated = transformer.Rotate90(data);
byte[] flipped = transformer.FlipHorizontal(data);
byte[] cropped = transformer.Crop(data, 0, 0, 512, 512);

System.IO.File.WriteAllBytes("rotated.jpg", rotated);
System.IO.File.WriteAllBytes("flipped.jpg", flipped);
System.IO.File.WriteAllBytes("cropped.jpg", cropped);
```

## JPEG Family Detection

The library should distinguish classic JPEG/JFIF/Exif streams from other image formats and related JPEG-named formats. Detection starts with marker-level validation such as SOI, segment structure, SOF markers, APP markers, SOS, entropy-coded data, and EOI where available.

Classic JPEG streams should produce useful identify results even when the full coding process is not yet supported. Related but out-of-scope formats should be detected as unsupported rather than misreported as decodable classic JPEG.

Unsupported detection targets include:

- JPEG 2000.
- JPEG XR.
- JPEG XL.
- HEIF/AVIF containers that may use JPEG-family terminology in metadata.
- JPEG-LS until explicitly implemented.
- SmartScale and other libjpeg-incompatible extensions unless explicitly implemented.

## Non-Goals

This project does not aim to wrap native codecs or match every behavior of a specific platform image stack. It also does not aim to implement unrelated modern image formats under the JPEG name.

Explicit non-goals:

- No native shims, P/Invoke codec wrappers, or dependency on libjpeg/libjpeg-turbo at runtime.
- No dependency on external image-processing or color-management packages.
- No JPEG 2000, JPEG XR, JPEG XL, HEIF, AVIF, PNG, GIF, WebP, or TIFF decoding or encoding.
- No SmartScale support unless a later task explicitly changes that scope.
- No best-effort decoding that silently treats unknown JPEG-family processes as baseline JPEG.
- No public API shapes that conflict with the simplified C# subset documented in `syntax.md`.

## Development Rules

Source files in `src` follow the simplified C# subset in `syntax.md`. Public symbols are organized by category under `src/Api`; implementation algorithms outside that tree remain internal. Public data models use fields instead of properties, and every public symbol carries XML documentation using only the constructs allowed by the syntax policy. Public service classes implement narrow contracts and expose instance operations, while internal interfaces remain limited to algorithm-selection seams. The implementation favors deterministic, portable algorithms over platform-specific behavior.
