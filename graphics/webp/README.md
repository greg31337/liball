# webp

Small C# WebP reader, writer, validator, and mux helpers.

## Public API and Dependency Injection

Applications share one stateless `WebpCodec` and may request only the capability they use through `IWebpDecoder`, `IWebpEncoder`, `IWebpInfoReader`, `IWebpValidator`, or `IWebpMux`.

The interfaces use canonical byte-array operations with explicit options. Callers handle file and stream I/O with the standard .NET APIs, which keeps test doubles small and avoids putting I/O policy into application services.

```csharp
public class ImageInspector
{
    private readonly IWebpInfoReader reader;

    public ImageInspector(IWebpInfoReader reader)
    {
        this.reader = reader;
    }

    public WebpInfo Inspect(byte[] data)
    {
        return reader.Read(data, new WebpDecodeOptions());
    }
}

IWebpInfoReader reader = new WebpCodec();
ImageInspector inspector = new ImageInspector(reader);
```

Tests can replace any one of these narrow interfaces with a small fake without encoding or parsing a real WebP file. Low-level parsers, transforms, bit primitives, and convenience helpers remain internal implementation details.

## Identify

Use `IWebpInfoReader` when you need container and image metadata without decoding pixels.

```csharp
using webp;

byte[] data = File.ReadAllBytes("input.webp");
IWebpInfoReader reader = new WebpCodec();
WebpInfo info = reader.Read(data, new WebpDecodeOptions());

if (info.Diagnostics.Count == 0)
{
    Console.WriteLine($"{info.Width}x{info.Height}");
    Console.WriteLine(info.ContainerKind);
    Console.WriteLine(info.BitstreamKind);
    Console.WriteLine($"alpha: {info.HasAlpha}");
    Console.WriteLine($"animation: {info.HasAnimation}");
}
```

For decode-style identification, set `IdentifyOnly`. Frame metadata and encoded bitstreams are retained, but pixel buffers are skipped.

```csharp
WebpDecodeOptions options = new WebpDecodeOptions();
options.IdentifyOnly = true;

IWebpDecoder decoder = new WebpCodec();
WebpDecodeResult result = decoder.Decode(File.ReadAllBytes("input.webp"), options);
WebpFrame first = result.Image.Frames[0];

Console.WriteLine($"{first.Width}x{first.Height}");
Console.WriteLine(first.Pixels == null);
```

## Decode Still

Decode a complete WebP byte array. Decoded pixels are RGBA, row-major, 4 bytes per pixel.

```csharp
IWebpDecoder decoder = new WebpCodec();
WebpDecodeResult result = decoder.Decode(File.ReadAllBytes("input.webp"), new WebpDecodeOptions());

if (result.IsValid)
{
    WebpFrame frame = result.Image.Frames[0];
    WebpPixelBuffer pixels = frame.Pixels;

    byte red = pixels.Rgba[0];
    byte green = pixels.Rgba[1];
    byte blue = pixels.Rgba[2];
    byte alpha = pixels.Rgba[3];
}
else
{
    foreach (WebpDiagnostic diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic.Message);
    }
}
```

## Encode Still

Create a `WebpPixelBuffer`, fill RGBA pixels, and encode. Automatic mode chooses a lossless or lossy container based on the options and image characteristics.

```csharp
WebpPixelBuffer pixels = new WebpPixelBuffer();
pixels.Width = 2;
pixels.Height = 1;
pixels.Stride = 8;
pixels.Rgba = new byte[8];
pixels.Rgba[0] = 255;
pixels.Rgba[1] = 0;
pixels.Rgba[2] = 0;
pixels.Rgba[3] = 255;
pixels.Rgba[4] = 0;
pixels.Rgba[5] = 0;
pixels.Rgba[6] = 255;
pixels.Rgba[7] = 255;

WebpEncodeOptions options = new WebpEncodeOptions();
options.Mode = WebpEncodeMode.Lossless;

IWebpEncoder encoder = new WebpCodec();
byte[] data = encoder.Encode(pixels, options);
File.WriteAllBytes("output.webp", data);
```

Validate encoded or external data before further processing when you want diagnostics without a full decode.

```csharp
IWebpValidator validator = new WebpCodec();
WebpValidationResult validation = validator.Validate(data, new WebpDecodeOptions());

if (!validation.IsValid)
{
    foreach (WebpDiagnostic diagnostic in validation.Diagnostics)
    {
        Console.WriteLine(diagnostic.Message);
    }
}
```

## Metadata

Use `IWebpMux` to read, replace, strip, or set individual metadata payloads. Metadata payloads are raw ICC, Exif, and XMP bytes.

```csharp
byte[] data = File.ReadAllBytes("input.webp");
IWebpMux mux = new WebpCodec();

WebpMetadata metadata = mux.GetMetadata(data);
byte[] existingExif = metadata.Exif;

WebpMetadata replacement = new WebpMetadata();
replacement.IccProfile = File.ReadAllBytes("profile.icc");
replacement.Exif = File.ReadAllBytes("metadata.exif");
replacement.Xmp = File.ReadAllBytes("metadata.xmp");

byte[] withMetadata = mux.ReplaceMetadata(data, replacement);
byte[] withoutMetadata = mux.StripMetadata(withMetadata);
byte[] withOnlyNewExif = mux.SetExif(data, File.ReadAllBytes("metadata.exif"));
```

## Animation

Animated images use a `WebpImage` with multiple `WebpFrame` entries. Frame positions in encoded WebP animation must be even.

```csharp
WebpPixelBuffer red = new WebpPixelBuffer();
red.Width = 1;
red.Height = 1;
red.Stride = 4;
red.Rgba = new byte[4];
red.Rgba[0] = 255;
red.Rgba[1] = 0;
red.Rgba[2] = 0;
red.Rgba[3] = 255;

WebpPixelBuffer blue = new WebpPixelBuffer();
blue.Width = 1;
blue.Height = 1;
blue.Stride = 4;
blue.Rgba = new byte[4];
blue.Rgba[0] = 0;
blue.Rgba[1] = 0;
blue.Rgba[2] = 255;
blue.Rgba[3] = 255;

WebpImage image = new WebpImage();
image.CanvasWidth = 4;
image.CanvasHeight = 1;
image.HasAnimation = true;
image.LoopCount = 0;
image.BackgroundColorBgra = 0x00000000U;

WebpFrame first = new WebpFrame();
first.X = 0;
first.Y = 0;
first.Width = 1;
first.Height = 1;
first.DurationMilliseconds = 100;
first.BitstreamKind = WebpBitstreamKind.Lossless;
first.Pixels = red;
image.Frames.Add(first);

WebpFrame second = new WebpFrame();
second.X = 2;
second.Y = 0;
second.Width = 1;
second.Height = 1;
second.DurationMilliseconds = 100;
second.BitstreamKind = WebpBitstreamKind.Lossless;
second.Pixels = blue;
image.Frames.Add(second);

WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
encodeOptions.Mode = WebpEncodeMode.Lossless;

IWebpEncoder encoder = new WebpCodec();
byte[] animated = encoder.EncodeImage(image, encodeOptions);
```

Decode animation as independent frame rectangles or as composited full-canvas frames.

```csharp
WebpCodec codec = new WebpCodec();
WebpDecodeResult independent = codec.Decode(animated, new WebpDecodeOptions());

WebpDecodeOptions decodeOptions = new WebpDecodeOptions();
decodeOptions.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;

WebpDecodeResult composited = codec.Decode(animated, decodeOptions);
List<WebpFrame> extractedEncodedFrames = codec.ExtractFrames(animated);
```

## Feature Parity Review

This library is not a drop-in replacement for `libwebp`, `cwebp`, ImageSharp, or ImageMagick. It focuses on a managed WebP model, parser/validator coverage, metadata and frame muxing, deterministic simple encoders, and security limits. The comparison below is based on the current public APIs in this repository and the upstream documentation linked at the end of this section.

Capability | This library | libwebp / `cwebp` | ImageSharp WebP | ImageMagick WebP
--- | --- | --- | --- | ---
Identify | `IWebpInfoReader.Read(...)` and `IWebpDecoder` with `IdentifyOnly` report dimensions, container kind, bitstream kind, metadata flags, animation frames, chunks, and diagnostics. | `webpinfo`, demux APIs, and decoder APIs expose container/bitstream details. | Format detection and decoder options integrate with the general ImageSharp image model. | `identify` and verbose coder metadata expose format details and defines.
Decode still images | Decodes basic/extended still containers, VP8L lossless pixels, ALPH side alpha, and a limited VP8 lossy skipped-residual path. | Full VP8 lossy and VP8L lossless decode, including advanced decode options such as crop/scale and external buffers. | Full image decode into ImageSharp image types, with general decoder options such as frame limits, metadata skipping, and target size. | Full decode through ImageMagick's image pipeline.
Encode still images | Encodes VP8L lossless/near-lossless and a simple deterministic VP8 lossy path for small supported inputs. Produces complete basic or extended container byte arrays for standard .NET I/O. | Full lossy/lossless encoder with mature rate control and detailed quality/speed tuning. | WebP encoder supports lossy/lossless selection and common tuning options. | WebP coder exposes many `webp:*` defines for quality, lossless mode, filtering, alpha, target size, target PSNR, and related controls.
Animation | Reads, writes, extracts, decodes independent frames, and can compose full-canvas frames. Encodes RGBA animation frames and can preserve pre-encoded frame bitstreams. | Container animation APIs support frame timestamps, assembly, mux/demux, and encoder configuration per frame. `cwebp` itself does not encode animated PNG/WebP inputs. | WebP encoder derives from animated encoder infrastructure and supports animated image output through the ImageSharp model. | Supports animated WebP through ImageMagick's multi-image pipeline and format defines.
Metadata | Raw ICC, Exif, and XMP extraction, replacement, per-field setters, stripping, and VP8X flag updates. | `cwebp -metadata` can copy `all`, `none`, `exif`, `icc`, or `xmp`; mux APIs expose chunk operations. | Metadata is represented through ImageSharp metadata APIs and encoder `SkipMetadata` behavior. | Profiles and coder defines support metadata handling in the broader ImageMagick pipeline.
Unknown chunks | Enumerate, preserve, strip, and replace top-level unknown chunks; frame-local unknown subchunks are preserved when their parent frame payload is preserved. | WebP mux/demux APIs support chunk-level operations. | Not exposed as raw WebP chunk muxing in the same style. | Can preserve or transform profiles/artifacts through its image pipeline, but raw chunk workflows are not the primary API.
Validation and diagnostics | Dedicated `IWebpValidator`, parser diagnostics, strict/permissive mode, and configurable security limits for sizes, chunks, frames, recursion, and total pixels. | Mature decoder/encoder validation through libwebp return codes and error enums. | Exceptions and decoder options integrate with ImageSharp's validation model. | Command diagnostics and resource limits integrate with ImageMagick policy/resource controls.
Lossy tuning | Exposes quality, method, preset, filter strength/sharpness, auto-filter, spatial noise shaping, preprocessing, target size, target PSNR, and pass count. Current encoder behavior is intentionally simpler than full libwebp. | `cwebp` exposes `-q`, `-m`, `-preset`, `-sns`, `-f`, `-sharpness`, `-af`, `-size`, `-psnr`, `-pass`, `-jpeg_like`, `-low_memory`, `-segments`, `-partition_limit`, and more. | Exposes quality, method, entropy passes, filter strength, spatial noise shaping, near-lossless settings, alpha compression, and file format selection. | Exposes `webp:*` defines such as quality, method, lossless, near-lossless behavior, alpha settings, filter settings, target size, target PSNR, pass count, thread level, and sharp YUV.
Alpha and transparent RGB | ALPH decode/encode, alpha filtering, alpha quality, alpha compression mode, lossless alpha, and exact transparent RGB controls. | `cwebp` has `-alpha_q`, `-alpha_filter`, `-alpha_method`, `-exact`, `-blend_alpha`, and `-noalpha`. The libwebp API also exposes exact transparent RGB behavior. | Exposes alpha compression and transparent-color behavior through encoder options and inherited alpha-aware options. | Exposes alpha compression/filtering/quality and `exact` defines.
Not currently covered | Full production VP8 lossy reconstruction/entropy decode, full libwebp rate control, crop/resize during decode, multithreaded encoding, low-memory modes, sharp YUV conversion, advanced partition controls, and broad external conformance corpus coverage. | Covers these areas in native code. | Covers many practical image-pipeline scenarios through ImageSharp abstractions, not raw mux tooling. | Covers many production conversion workflows through command-line defines and policy/resource settings.

Sources reviewed:

- Google WebP `cwebp` documentation: https://developers.google.com/speed/webp/docs/cwebp
- Google WebP API documentation: https://developers.google.com/speed/webp/docs/api
- Google WebP container API documentation: https://developers.google.com/speed/webp/docs/container-api
- ImageSharp `WebpEncoder` API documentation: https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Formats.Webp.WebpEncoder.html
- ImageSharp image format documentation: https://docs.sixlabors.com/articles/imagesharp/imageformats.html
- ImageMagick WebP encoding options: https://imagemagick.org/webp/
