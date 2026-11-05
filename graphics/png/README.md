# png

Initial scaffold created on 2026‑08‑04.

## Feature Matrix

| Area | First milestone status | Main APIs |
| --- | --- | --- |
| Metadata-only identify | Reads signature, IHDR, APNG control, metadata summaries, text counts, unknown chunks, and IDAT byte counts without inflating image data. | `IPngIdentifier`, `PngIdentifierService` |
| Validation and inspection | Strict validation, tolerant metadata validation, tolerant repair for selected ancillary issues, best-effort inspection, CRC checks, chunk ordering, APNG sequence checks, and size limits. | `IPngValidator`, `PngValidationService` |
| PNG decode | Decodes non-interlaced and Adam7 PNGs into native pixel buffers for grayscale, truecolor, indexed-color, grayscale-alpha, and truecolor-alpha images at supported bit depths. | `IPngDecoder`, `PngCodec` |
| PNG encode | Encodes native pixel buffers, indexed images with palettes, metadata preservation/stripping, configurable compression, filters, interlace, and IDAT splitting. | `IPngEncoder`, `PngCodec`, `PngEncoderOptions` |
| Low-level chunks | Reads and writes complete PNG datastream chunk lists, preserves unknown ancillary chunks, repairs selected ancillary ordering and CRC issues. | `PngChunkService`, `PngRepairService`, `PngCrcCalculator` |
| Ancillary metadata | Parser/writer coverage for transparency, color metadata, ICC, text, physical pixels, suggested palette, Exif, time, and PNG-3 color chunks. | `PngMetadata`, `PngMetadataService`, `PngChunkService` |
| APNG | Parses and writes animation control chunks, validates sequence numbers and frame rectangles, decodes frame-local pixels, composes frames, applies dispose modes, and emits APNG frame chunks. | `PngCodec`, `PngAnimation`, `PngFrame` |
| Pixel transforms and reductions | Palette expansion/trimming, tRNS expansion, grayscale/RGB conversion, alpha operations, 16-bit byte swap, flips, crop, padding, and resize helpers. | `IPngTransformer`, `PngTransformer` |
| Optimization helpers | Compression-level trials, filter-strategy trials, early bailout, and optimization report building. | `IPngOptimizer`, `PngOptimizationService` |
| Runtime dependency profile | Library project has no external runtime package references; tests may use test-only packages. | `src/png.csproj` |

## Public API Boundary

The supported API consists only of symbols declared `public`. Parsing stages, chunk codecs, validators, scanline machinery, APNG composition internals, and other implementation helpers are declared `internal` and can change without compatibility guarantees. Public symbols carry XML documentation in the generated assembly documentation file; internal symbols intentionally do not.

Public types are organized by role under `src/Api`: contracts, data models, enums, and interface-backed implementation classes. Static orchestration remains internal. Applications can depend on focused interfaces and construct the corresponding stateless service, such as `PngCodec`, `PngIdentifierService`, `PngValidationService`, `PngTransformer`, or `PngMetadataService`. Explicitly named variants replace overloaded public methods where parameter sets differ.

`PngOptimizationService` also accepts an `IPngEncoder`. This lets applications supply an alternate encoder and lets tests exercise trial selection and option propagation without performing compression.

## API Usage Examples

Use these focused workflow examples:

```csharp
using png;

public static class PngApiExamples
{
    public static PngIdentifyResult Identify(byte[] inputBytes)
    {
        PngIdentifyResult result = new PngIdentifierService().IdentifyBytes(inputBytes);
        if (result.IsPng && result.IsValid)
        {
            int width = result.Info.Width;
            int height = result.Info.Height;
            long metadataBytes = result.Metadata.MetadataBytes;
        }

        return result;
    }

    public static PngImage Decode(byte[] inputBytes)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgba8;
        return new PngCodec().DecodeBytes(inputBytes, options);
    }

    public static byte[] Encode()
    {
        PngImage image = new PngImageFactory().CreateImage(2, 1, PngPixelFormat.Rgba8);
        image.Pixels.Data[0] = 255;
        image.Pixels.Data[3] = 255;
        image.Pixels.Data[5] = 255;
        image.Pixels.Data[7] = 255;

        PngEncoderOptions options = new PngEncoderOptions();
        options.MetadataPolicy = PngMetadataPolicy.Strip;
        options.CompressionLevel = 9;
        return new PngCodec().EncodeToBytes(image, options);
    }

    public static byte[] EditMetadata(byte[] inputBytes)
    {
        PngImage image = new PngCodec().DecodeBytes(inputBytes, new PngDecoderOptions());
        PngText title = new PngText();
        title.Keyword = "Title";
        title.Text = "Edited with png";

        PngMetadataService editor = new PngMetadataService();
        PngMetadata metadata = editor.SetGammaValue(image.Metadata, 45455);
        metadata = editor.SetPhysicalDimensionValues(metadata, 3780, 3780, 1);
        image.Metadata = editor.ReplaceTextModels(metadata, new PngText[] { title });
        return new PngCodec().EncodeToBytes(image, new PngEncoderOptions());
    }

    public static byte[] EncodeApng()
    {
        PngImage image = new PngImageFactory().CreateImage(2, 1, PngPixelFormat.Rgba8);
        PngFrame firstFrame = CreateFrame(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 });
        PngFrame secondFrame = CreateFrame(new byte[] { 0, 255, 0, 255, 255, 255, 0, 255 });

        image.Animation.FrameCount = 2;
        image.Animation.PlayCount = 0;
        image.Animation.Frames = new PngFrame[] { firstFrame, secondFrame };

        PngEncoderOptions options = new PngEncoderOptions();
        options.EncodeAnimation = true;
        return new PngCodec().EncodeToBytes(image, options);
    }

    public static byte[] Transform(byte[] inputBytes)
    {
        PngImage image = new PngCodec().DecodeBytes(inputBytes, new PngDecoderOptions());
        PngTransformer transformer = new PngTransformer();
        PngPixelBuffer cropped = transformer.Crop(image.Pixels, 0, 0, image.Pixels.Width, image.Pixels.Height);
        PngPixelBuffer resized = transformer.ResizeNearestNeighbor(cropped, image.Pixels.Width, image.Pixels.Height);
        PngPixelBuffer normalized = transformer.StripAlphaIfOpaque(resized);

        PngImage output = new PngImage();
        output.Pixels = normalized;
        return new PngCodec().EncodeToBytes(output, new PngEncoderOptions());
    }

    public static PngValidationResult Validate(byte[] inputBytes)
    {
        PngValidationService validator = new PngValidationService();
        PngValidationResult strict = validator.ValidateBytes(inputBytes);
        if (strict.IsValid)
        {
            return strict;
        }

        return validator.ValidateTolerantRepairBytes(inputBytes);
    }

    public static PngOptimizerReport Optimize(byte[] inputBytes)
    {
        PngImage image = new PngCodec().DecodeBytes(inputBytes, new PngDecoderOptions());
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 6;
        options.MaximumCompressionLevel = 9;
        options.UseEarlyBailout = false;

        PngOptimizationService optimizer = new PngOptimizationService();
        PngCompressionTrialResult compression = optimizer.RunCompressionTrialsWithOptions(image, options);
        PngFilterStrategyTrialResult filters = optimizer.RunFilterTrialsWithOptions(image, options);
        return optimizer.CreateReportFromBytes(inputBytes, compression, filters);
    }

    static PngFrame CreateFrame(byte[] rgba)
    {
        PngImage frameImage = new PngImageFactory().CreateImageWithData(2, 1, PngPixelFormat.Rgba8, rgba);
        PngFrame frame = new PngFrame();
        frame.Width = 2;
        frame.Height = 1;
        frame.DelayNumerator = 10;
        frame.DelayDenominator = 100;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.Pixels = frameImage.Pixels;
        return frame;
    }
}
```

## Dependency Injection Example

Register one `PngCodec` instance for either or both roles in the application's container, or pass it directly:

```csharp
PngCodec codec = new PngCodec();
IPngEncoder encoder = codec;
IPngDecoder decoder = codec;
PngImage image = new PngImageFactory().CreateImage(1, 1, PngPixelFormat.Rgba8);

byte[] encoded = encoder.EncodeToBytes(image, new PngEncoderOptions());
PngImage decoded = decoder.DecodeBytes(encoded, new PngDecoderOptions());
```

Tests can implement either interface with a small in-memory fake. In particular, a fake `IPngEncoder` can return controlled byte counts to test compression-level and filter-strategy selection deterministically.

## Command-Line Sample

Create a small console app that references the library project:

```bash
dotnet new console -n PngSample
dotnet add PngSample/PngSample.csproj reference src/png.csproj
dotnet run --project PngSample -- input.png output.png
```

Use this `Program.cs`:

```csharp
using System;
using System.IO;
using png;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: PngSample <input.png> <output.png>");
    return 2;
}

byte[] inputBytes = File.ReadAllBytes(args[0]);
PngIdentifyResult identify = new PngIdentifierService().IdentifyBytes(inputBytes);
if (!identify.IsPng)
{
    Console.Error.WriteLine("Input is not a PNG file.");
    return 3;
}

PngValidationResult validation = new PngValidationService().ValidateBytes(inputBytes);
if (!validation.IsValid)
{
    Console.Error.WriteLine("Input PNG is not valid.");
    return 4;
}

PngCodec codec = new PngCodec();
PngImage image = codec.DecodeBytes(inputBytes, new PngDecoderOptions());
Console.WriteLine($"{image.Info.Width}x{image.Info.Height} {image.Info.ColorType} {image.Info.BitDepth}");

PngEncoderOptions options = new PngEncoderOptions();
options.MetadataPolicy = PngMetadataPolicy.Strip;

byte[] outputBytes = codec.EncodeToBytes(image, options);
File.WriteAllBytes(args[1], outputBytes);
return 0;
```
