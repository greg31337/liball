# Usage guide

> **Current interoperability limitation:** the public encoder and decoder exchange a private
> `AVIFRAW1` planar payload inside an AV1 padding OBU, not normative AV1 coded frame data. The
> examples demonstrate this library's API and managed round trips; their encoded output is not an
> interoperable AVIF deliverable. See [Release status and policy](release-status.md).

The `avif` namespace provides one-call operations for ordinary still images and stateful readers and
writers for compound AVIF files. All operations return an `AvifResultCode`; unsuccessful results do
not contain usable output.

The library deliberately keeps ownership explicit:

- Byte-array inputs are copied when a reader or retained model is created.
- Result methods such as `GetData()` return independent arrays.
- `AvifWriter` snapshots an image or metadata object when it is added.
- `AvifImage`, `AvifReader`, `AvifWriter`, and other stateful codecs have an explicit `Close()`
  method. Closing them does not close caller-owned streams.
- `AvifSecurityLimits` should be set before an operation begins. Readers and writers snapshot the
  limits supplied to their creation methods.

The examples below use `using avif;` and `using System.IO;`.

## Use or inject the one-call API

Create one `AvifCodec` for direct calls. Application services that need a replaceable dependency
can accept its `IAvifCodec` contract:

```csharp
public sealed class ThumbnailService
{
    private readonly IAvifCodec codec;

    public ThumbnailService(IAvifCodec codec)
    {
        this.codec = codec;
    }

    public AvifDecodeResult Decode(byte[] source)
    {
        return codec.Decode(source, new AvifDecoderOptions());
    }
}

ThumbnailService service = new ThumbnailService(new AvifCodec());
```

Tests can implement `IAvifCodec` and construct controlled result objects directly, without reading
or encoding a real AVIF payload.

## Create and decode a still image

`AvifImage` stores planar samples. The short example below creates an 8-bit monochrome image; YUV
images expose additional `U` and `V` planes through `GetPlane`.

```csharp
AvifImage image = new AvifImage(
    640,
    480,
    8,
    AvifChromaFormat.Monochrome,
    AvifRange.Full,
    AvifChromaSamplePosition.Unknown,
    AvifAlphaMode.None,
    1,
    new AvifSecurityLimits());

AvifPlane yPlane = image.GetPlane(AvifPlaneKind.Y);
for (int row = 0; row < yPlane.GetHeight(); row++)
{
    Span<byte> samples = yPlane.GetRowSpan(row);
    samples.Fill((byte)(row % 256));
}

AvifEncoderOptions encodeOptions = new AvifEncoderOptions();
encodeOptions.Quality = 85;
IAvifCodec codec = new AvifCodec();
AvifEncodeResult encoded = codec.Encode(image, encodeOptions);
if (encoded.GetCode() != AvifResultCode.Success)
{
    throw new InvalidOperationException(encoded.GetCode().ToString());
}

File.WriteAllBytes("still.avif", encoded.GetData());
image.Close();
```

Decode a complete byte array with `AvifCodec.Decode`, or use `AvifReader` when the parsed document or
encoded item access is also needed:

```csharp
byte[] source = File.ReadAllBytes("still.avif");
IAvifCodec codec = new AvifCodec();
AvifDecodeResult decoded = codec.Decode(source, new AvifDecoderOptions());
if (decoded.GetCode() != AvifResultCode.Success)
{
    throw new InvalidOperationException(decoded.GetCode().ToString());
}

AvifImage decodedImage = decoded.GetImage();
Console.WriteLine($"{decodedImage.GetWidth()} x {decodedImage.GetHeight()}");
decodedImage.Close();
```

For untrusted input, set concrete limits before decoding:

```csharp
AvifDecoderOptions options = new AvifDecoderOptions();
options.Limits.MaxSourceBytes = 32 * 1024 * 1024;
options.Limits.MaxWidth = 8192;
options.Limits.MaxHeight = 8192;
options.Limits.MaxPixels = 40_000_000;

IAvifCodec codec = new AvifCodec();
AvifDecodeResult decoded = codec.Decode(source, options);
```

## Add Exif, XMP, and opaque metadata

Use `AvifWriter` when metadata or multiple items must be authored. `AddImage` returns the item ID
used by metadata associations. The writer copies each added value, so the caller may close the
source image or reuse the original arrays after the add operation succeeds.

```csharp
AvifImage image = CreateImage();
byte[] exifItemPayload = LoadExifItemPayload();
byte[] xmpPacket = File.ReadAllBytes("metadata.xmp");

AvifSecurityLimits limits = new AvifSecurityLimits();
AvifExif exif = new AvifExif(exifItemPayload, limits);
AvifXmp xmp = new AvifXmp(xmpPacket, limits);
if (exif.GetCode() != AvifResultCode.Success
    || xmp.GetCode() != AvifResultCode.Success)
{
    throw new InvalidDataException("Invalid metadata input.");
}

AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
uint imageId = writer.AddImage(image);
uint exifId = writer.AddExif(imageId, exif);
uint xmpId = writer.AddXmp(imageId, xmp);
if (imageId == 0 || exifId == 0 || xmpId == 0)
{
    throw new InvalidOperationException(writer.GetCode().ToString());
}

AvifEncodeResult encoded = writer.Finish();
File.WriteAllBytes("metadata.avif", encoded.GetData());

image.Close();
writer.Close();
```

`AvifMetadata` supports the predefined metadata kinds and arbitrary non-image item types. On read,
inspect `AvifDocument` to find metadata items and call `AvifReader.GetEncodedItem(itemId)` to obtain
a copy of an item's encoded payload. Exif orientation can be read or changed with `AvifExif`, and
`AvifXmp.HasExifConflict` can identify conflicting mapped values before authoring.

## Author and decode a grid

A grid contains independently encoded cells in row-major order. Interior cells have equal coded
dimensions. Only the rightmost column and bottom row may be cropped by the declared output size.
All cells must otherwise have compatible depth, chroma, range, alpha, and color signaling.

```csharp
AvifImage topLeft = CreateTile(3, 2, 20);
AvifImage topRight = CreateTile(3, 2, 60);
AvifImage bottomLeft = CreateTile(3, 2, 100);
AvifImage bottomRight = CreateTile(3, 2, 140);

// Four 3x2 coded cells produce a 5x3 visible result after border cropping.
AvifGrid grid = new AvifGrid(2, 2, 5, 3, new AvifSecurityLimits());
grid.SetCell(0, topLeft);
grid.SetCell(1, topRight);
grid.SetCell(2, bottomLeft);
grid.SetCell(3, bottomRight);

AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
uint gridId = writer.AddGrid(grid);
AvifEncodeResult encoded = writer.Finish();
if (gridId == 0 || encoded.GetCode() != AvifResultCode.Success)
{
    throw new InvalidOperationException(writer.GetCode().ToString());
}

topLeft.Close();
topRight.Close();
bottomLeft.Close();
bottomRight.Close();
writer.Close();
```

The primary-image decode renders the complete grid. A stateful reader can also decode one cell:

```csharp
AvifReader reader = new AvifReader(encoded.GetData(), new AvifDecoderOptions());
if (reader.Parse() != AvifResultCode.Success)
{
    throw new InvalidDataException(reader.GetCode().ToString());
}

AvifDecodeResult rendered = reader.DecodePrimaryImage();
AvifDecodeResult bottomRightCell = reader.DecodeTile(gridId, 1, 1);

rendered.GetImage().Close();
bottomRightCell.GetImage().Close();
reader.Close();
```

## Signal HDR images and metadata

Set image color interpretation separately from static HDR metadata. For example, a PQ image commonly
uses BT.2020 primaries and non-constant-luminance BT.2020 matrix coefficients:

```csharp
AvifImage hdrImage = CreateTenBitImage();
AvifColorInfo color = new AvifColorInfo(
    AvifColorPrimaries.Bt2020,
    AvifTransferCharacteristics.Smpte2084,
    AvifMatrixCoefficients.Bt2020NonConstantLuminance,
    AvifRange.Limited);
if (hdrImage.SetColorInfo(color) != AvifResultCode.Success)
{
    throw new InvalidOperationException("The color signaling is incompatible.");
}

AvifHdrMetadata hdr = new AvifHdrMetadata();
hdr.SetContentLightLevel(1000, 400);
hdr.SetMasteringDisplay(
    new int[] { 35400, 8500, 6550 },
    new int[] { 14600, 39850, 2300 },
    15635,
    16450,
    10_000_000,
    50);

AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
uint imageId = writer.AddImage(hdrImage);
AvifResultCode metadataCode = writer.SetHdrMetadata(imageId, hdr);
AvifEncodeResult encoded = writer.Finish();
```

Always check the return from each `Set` call in production code. Chromaticity values use units of
0.00002; mastering luminance values use units of 0.0001 candela per square metre. Content-light
levels use candelas per square metre.

For adaptive HDR, create an `AvifGainMap` from a decoded or generated gain-map image and validated
`AvifGainMapMetadata`, then associate it with a base item through `AvifWriter.AddGainMap`. Rendering
behavior is selected through `AvifRenderOptions.ToneMapMode` and the target display headroom.

## Author and select progressive layers

A progressive image contains two through four compatible layers ordered from preview to final
presentation. Later layers may increase dimensions, but sample depth, chroma layout, range, alpha,
and color interpretation must remain compatible.

```csharp
AvifImage preview = CreatePreviewImage();
AvifImage intermediate = CreateIntermediateImage();
AvifImage finalImage = CreateFinalImage();

AvifLayeredImage layered = new AvifLayeredImage(0, true);
layered.AddLayer(preview);
layered.AddLayer(intermediate);
layered.AddLayer(finalImage);

AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
uint layeredItemId = writer.AddLayeredImage(layered);
AvifEncodeResult encoded = writer.Finish();
if (layeredItemId == 0 || encoded.GetCode() != AvifResultCode.Success)
{
    throw new InvalidOperationException(writer.GetCode().ToString());
}

preview.Close();
intermediate.Close();
finalImage.Close();
writer.Close();
```

Decode an indexed layer or let primary-image selection choose the final presentation:

```csharp
AvifReader reader = new AvifReader(encoded.GetData(), new AvifDecoderOptions());
reader.Parse();
AvifDecodeResult previewResult = reader.DecodeLayer(0);
AvifDecodeResult finalResult = reader.DecodePrimaryImage();

previewResult.GetImage().Close();
finalResult.GetImage().Close();
reader.Close();
```

Pass `false` as the second `AvifLayeredImage` constructor argument when intermediate layers are
dependencies but must not be advertised for progressive presentation.

## Author and read a timed sequence

Frame durations are expressed in units of the sequence timescale. The first frame must be a key
frame, and every frame in a sequence must use compatible dimensions and sample configuration.

```csharp
AvifImage firstImage = CreateSequenceFrame(30);
AvifImage secondImage = CreateSequenceFrame(180);

// A timescale of 1000 makes these durations 100 ms and 250 ms.
AvifSequence sequence = new AvifSequence(1000);
sequence.AddFrame(new AvifFrame(firstImage, null, 100, true));
sequence.AddFrame(new AvifFrame(secondImage, null, 250, true));
sequence.SetRepetitionCount(2);

AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
uint trackId = writer.AddSequence(sequence);
AvifEncodeResult encoded = writer.Finish();
if (trackId == 0 || encoded.GetCode() != AvifResultCode.Success)
{
    throw new InvalidOperationException(writer.GetCode().ToString());
}

firstImage.Close();
secondImage.Close();
writer.Close();
```

`SetRepetitionCount` specifies repetitions after the initial presentation; use
`SetInfiniteRepetition` for unbounded playback. `AddSequence` also creates a primary still fallback
from the first frame.

Use `AvifReader` for indexed frame decode and timing:

```csharp
AvifReader reader = new AvifReader(encoded.GetData(), new AvifDecoderOptions());
reader.Parse();

AvifDecodeResult first = reader.DecodeFrame(0);
AvifFrameTiming timing = reader.GetFrameTiming(0);
Console.WriteLine($"duration={timing.GetDuration()} / {timing.GetTimescale()} second");

first.GetImage().Close();
reader.Close();
```

For playback planning without immediately decoding pixels, select the timed source from the parsed
document and create an `AvifSequence` cursor:

```csharp
AvifImageSource source = reader.GetDocument().SelectSource(
    AvifSourceSelection.ImageSequence);
AvifSequence cursor = source.CreateSequence();
while (cursor.DecodeNextFrame() == AvifResultCode.Success)
{
    int frameIndex = cursor.GetCurrentFrameIndex();
    AvifFrameTiming timing = cursor.GetCurrentFrameTiming();
    AvifByteRange bytes = cursor.GetCurrentFrameDecodeRange();
    // Schedule the required byte range and decode frameIndex with AvifReader.
}
```

Keep the `AvifReader` open while using its document and encoded source. Close each decoded image as
soon as its planes are no longer needed.
