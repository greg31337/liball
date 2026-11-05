# gif

Initial scaffold created on 2026‑08‑05.

## Feature matrix

| Feature | Status | Public API / notes |
| --- | --- | --- |
| Metadata identify | Supported | `GifCodec.IdentifyBytes` and `IdentifyStream` read dimensions, version, frame count, animation flags, transparency, loop count, comments, application identifiers, warnings, and estimated decoded bytes. |
| Structural decode | Supported | `GifCodec.DecodeBytes` and `DecodeStream` return a `GifDocument` with logical screen data, palettes, raw frames, metadata, warnings, and statistics. |
| Coalesced animation decode | Supported | `GifCodec.DecodeAnimationBytes`, `DecodeAnimationStream`, and `Coalesce` produce full-frame `GifAnimation` output with disposal handling, transparency, loop count, delays, and configurable background behavior. |
| First-frame and metadata-only reads | Supported | `GifDecodeOptions.FirstFrameOnly` stops after the first image, and identify uses metadata-only parsing so compressed payloads are not retained. |
| Indexed GIF encode | Supported | `GifCodec.EncodeIndexedStill`, `EncodeIndexedAnimation`, `CreateStill`, `CreateAnimation`, `Encode`, and `Write` write palette-backed stills and animations. |
| RGBA GIF encode | Supported | `GifCodec.EncodeRgbaStill` and `EncodeRgbaAnimation` quantize RGBA pixels into GIF palettes before writing. |
| Palette modes | Supported | Automatic, global, and local palette modes are available through `GifEncodeOptions.ColorTableMode`; `GifCodec.SetPalette` and `SetPalettes` replace global and per-frame palettes. |
| Quantization and dithering | Supported | Exact, median-cut, octree, grayscale, and web-safe quantizers are available, with none, ordered Bayer, and Floyd-Steinberg dithering modes. |
| Transparency, delays, disposal, and loops | Supported | `GifAnimationOptions`, `GifEncodeOptions`, and helpers such as `SetDelay`, `SetDelays`, `SetDisposal`, `SetDisposals`, and `SetLoopCount` control animation metadata. |
| Metadata preservation | Supported | Comments, application extensions, Netscape and ANIMEXTS loop extensions, plain-text extensions, and unknown extensions can be parsed and re-emitted. |
| Interlacing | Supported | Interlaced image data is exposed as row-major indexed pixels after decoding and can be encoded in four-pass order with `GifEncodeOptions.Interlace`. |
| Optimization helpers | Supported | `GifCodec.Optimize` and `Deconstruct` support frame deconstruction and duplicate or zero-delay frame optimization modes. |
| Validation and diagnostics | Supported | `GifCodec.ValidateDocument`, `ValidateBitmap`, and `ValidateAnimation` expose structured error and warning codes for malformed or unsafe input. |
| Safety limits | Supported | Decode and encode options include configurable byte, dimension, frame, decoded-pixel, sub-block, and memory limits for untrusted data. |

## Examples

### Identify

```csharp
using gif;

byte[] data = File.ReadAllBytes("input.gif");
IGifDecoder decoder = new GifCodec();
GifInfo info = decoder.IdentifyBytes(data, null);

if (!info.Success)
{
    Console.WriteLine(info.Error.Message);
    return;
}

Console.WriteLine(info.Width.ToString() + "x" + info.Height.ToString());
Console.WriteLine(info.FrameCount.ToString() + " frame(s)");
```

### Decode

```csharp
using gif;

IGifDecoder decoder = new GifCodec();
using FileStream input = File.OpenRead("input.gif");
GifDocument document = decoder.DecodeStream(input, null);

GifFrame firstFrame = document.Frames[0];
byte[] compressedImageData = firstFrame.CompressedImageData;
GifPalette activePalette = firstFrame.HasLocalColorTable
    ? firstFrame.LocalColorTable
    : document.GlobalColorTable;
```

### Animation Decode

```csharp
using gif;

GifDecodeOptions options = new GifDecodeOptions();
options.FirstFrameOnly = true;

IGifDecoder decoder = new GifCodec();
GifAnimation animation = decoder.DecodeAnimationBytes(File.ReadAllBytes("input.gif"), options);
GifBitmap firstBitmap = animation.Frames[0].Bitmap;
byte[] rgbaPixels = firstBitmap.RgbaPixels;
```

### Still Encode

```csharp
using gif;

GifPalette palette = new GifPalette(new GifColor[]
{
    new GifColor(0, 0, 0),
    new GifColor(255, 255, 255)
});

GifCodec codec = new GifCodec();
GifBitmap bitmap = codec.CreateIndexedBitmap(2, 1, new byte[] { 0, 1 }, palette);
GifEncodeResult encoded = codec.EncodeIndexedStill(bitmap, null, false, -1);

if (encoded.Success)
{
    File.WriteAllBytes("still.gif", encoded.Data);
}
```

### Animation Encode

```csharp
using gif;

GifBitmap first = new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 });
GifBitmap second = new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 });

GifAnimationOptions animationOptions = new GifAnimationOptions();
animationOptions.DelaysMilliseconds = new int[] { 100, 100 };
animationOptions.LoopCount = 0;

GifCodec codec = new GifCodec();
GifDocument document = codec.CreateAnimation(new GifBitmap[] { first, second }, animationOptions, null);
byte[] bytes = codec.Encode(document, null);
File.WriteAllBytes("animation.gif", bytes);
```

### Metadata Preservation

```csharp
using gif;

GifCodec codec = new GifCodec();
GifDocument document = codec.DecodeBytes(File.ReadAllBytes("source.gif"), null);
document = codec.SetCommentsFromText(document, new string[] { "encoded with gif" });
document = codec.SetDocumentLoopCount(document, 0);

GifEncodeOptions options = new GifEncodeOptions();
options.PreserveMetadata = true;

byte[] bytes = codec.Encode(document, options);
File.WriteAllBytes("preserved.gif", bytes);
```

### Dependency Injection

`GifCodec` implements focused decoder, encoder, factory, and transformer interfaces. Use the narrowest role required by each application component.

```csharp
using gif;

IGifDecoder decoder = new GifCodec();
GifDocument document = decoder.DecodeBytes(File.ReadAllBytes("input.gif"), new GifDecodeOptions());
```

## Security Guidance

Treat every GIF from a user, network, archive, or third-party system as untrusted input.

- Prefer `GifCodec.IdentifyBytes` or `IdentifyStream` before full decode when you only need metadata. Identification does not retain compressed image payloads.
- Set explicit `GifDecodeOptions` limits for your environment: `MaxBytes`, `MaxWidth`, `MaxHeight`, `MaxFrames`, `MaxDecodedPixels`, `MaxSubBlocks`, `MaxSubBlockBytes`, and `MaxMemoryBytes`.
- Use `FirstFrameOnly` for thumbnails or previews that do not require every frame.
- Keep `TolerateMalformedStreams` disabled for normal ingestion. Enable tolerant behavior only for repair or best-effort preview workflows where partial output is acceptable.
- Check `Success`, `Error`, and `Warnings` on result objects. Do not treat an empty document or animation as proof that input was valid.
- Enforce transport-level limits before calling the library. For example, cap upload size or stream length before reading from a request body.
- For encoding untrusted or user-edited images, set `GifEncodeOptions.MaxBytes` and `GifEncodeOptions.MaxMemoryBytes`, and keep `ValidateBeforeWrite` enabled.

```csharp
using gif;

GifDecodeOptions options = new GifDecodeOptions();
options.MaxBytes = 4 * 1024 * 1024;
options.MaxWidth = 4096;
options.MaxHeight = 4096;
options.MaxFrames = 256;
options.MaxDecodedPixels = 4096 * 4096;
options.MaxSubBlocks = 8192;
options.MaxSubBlockBytes = 2 * 1024 * 1024;
options.MaxMemoryBytes = 32 * 1024 * 1024;

IGifDecoder decoder = new GifCodec();
GifInfo info = decoder.IdentifyBytes(uploadedBytes, options);
if (!info.Success)
{
    Console.WriteLine(info.Error.Message);
    return;
}

GifAnimation preview = decoder.DecodeAnimationBytes(uploadedBytes, options);
```

## Compatibility Notes

- GIF87a and GIF89a headers are both accepted. Encoding defaults to GIF87a for plain still images and selects GIF89a when comments, application extensions, plain-text extensions, unknown extensions, transparency, delays, disposal metadata, or loop metadata are present. Set `GifEncodeOptions.Version` to request a specific header version.
- GIF89a loop counts are written as Netscape-compatible application extensions. A loop count of `-1` means no loop extension, `0` means loop forever, and `1` through `65535` are finite repeat counts.
- Frame delays are stored in GIF hundredths of a second. Encode-time millisecond delays are rounded up to the next hundredth and must be from `0` through `655350` milliseconds.
- Zero-delay frames remain zero unless `GifDecodeOptions.MinimumFrameDelayMilliseconds` is set during animation decode or coalescing.
- Disposal methods `Unspecified`, `DoNotDispose`, `RestoreToBackground`, and `RestoreToPrevious` map to the GIF graphic control extension values `0` through `3`. Reserved disposal values `4` through `7` are preserved in raw metadata, and coalescing replaces them with `GifDecodeOptions.DisposalFallback` while reporting a warning.
- `RestoreToBackground` uses `GifDecodeOptions.BackgroundMode` to choose logical-screen background color, transparent pixels, or a solid color behavior for the cleared rectangle.
- Global and local color tables must contain a power-of-two color count from `2` through `256`. Encoders compact or pad valid palettes to legal GIF table sizes where supported.
- Indexed pixel values must refer to entries in the active palette. RGBA encode paths quantize to a legal GIF palette before writing.
- Interlaced GIF image data is decoded to row-major pixel order for public bitmaps, and interlaced output can be requested with `GifEncodeOptions.Interlace`.
- Metadata preservation is enabled by default for encode options. Set `GifEncodeOptions.PreserveMetadata` to `false` when you intentionally want to drop parsed metadata during transcoding.

## Project conventions

- Project name, package id, and assembly name: `gif`.
- Namespace: `gif`.
- Target framework: `net10.0`.
- `GifCodec` is the public instance façade and implements `IGifDecoder`, `IGifEncoder`, `IGifFactory`, and `IGifTransformer` for dependency injection and narrow role-based use.
- Public data is limited to models, options, metadata, diagnostics, statistics, and enums under `src/Api`. The static engine, binary readers and writers, block parsers, LZW codecs, compositors, quantizers, optimizers, validators, and their result classes are internal implementation details.
- Internal byte reader and writer interfaces keep block parsing and writing independent of memory, streams, and test doubles. Quantizer implementations share one internal strategy interface so mode selection is centralized.
- Public API types use PascalCase names with a `Gif` prefix, such as `GifCodec`, `GifDocument`, and `GifDecodeOptions`; public data classes use fields instead of properties for the simplified C# subset.
- Every public type, constructor, method, field, enum, and enum member has XML documentation. Internal symbols intentionally do not carry public API documentation.
- Source uses only the `static`, `public`, `internal`, and `const` modifiers allowed by `syntax.md`. Adding `public` commits that symbol to the defined API layer.
