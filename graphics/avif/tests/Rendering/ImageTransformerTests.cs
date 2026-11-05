using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif.Tests;

public class ImageTransformerTests
{
    [Test]
    public void ItemPropertiesExposeCropOrientationAndPixelAspectMetadata()
    {
        List<AvifItemProperty> properties = new List<AvifItemProperty>
        {
            CreateCropProperty(),
            CreateRotationProperty(1),
            CreateMirrorProperty(1),
            CreateAspectProperty(4, 3)
        };

        AvifTransform transform = CreateItem(properties).GetTransform();
        AvifCrop? crop = transform.GetCleanAperture();
        AvifPixelAspectRatio ratio = transform.GetPixelAspectRatio();

        Assert.Multiple(() =>
        {
            Assert.That(transform.HasCleanAperture(), Is.True);
            Assert.That((crop ?? throw new InvalidOperationException("Invalid internal state.")).GetWidthNumerator(), Is.EqualTo(2));
            Assert.That(crop.GetHeightNumerator(), Is.EqualTo(3));
            Assert.That(crop.GetHorizontalOffsetDenominator(), Is.EqualTo(1));
            Assert.That(transform.HasRotation(), Is.True);
            Assert.That(transform.GetRotationQuarterTurnsCounterClockwise(),
                Is.EqualTo(1));
            Assert.That(transform.HasMirror(), Is.True);
            Assert.That(transform.GetMirrorAxis(), Is.EqualTo(1));
            Assert.That(transform.HasPixelAspectRatio(), Is.True);
            Assert.That(ratio.GetHorizontalSpacing(), Is.EqualTo(4));
            Assert.That(ratio.GetVerticalSpacing(), Is.EqualTo(3));
        });
    }

    [Test]
    public void AppliesCropRotationMirrorAndExifInNormativeOrder()
    {
        AvifPixelBuffer source = CreateGrayBuffer(4, 3);
        AvifTransform transform = new AvifTransform();
        Assert.That(transform.SetCleanAperture(2, 1, 3, 1, 0, 1, 0, 1),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(transform.SetRotation(1), Is.EqualTo(AvifResultCode.Success));
        Assert.That(transform.SetMirror(1), Is.EqualTo(AvifResultCode.Success));
        Assert.That(transform.SetExifOrientation(6),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(transform.SetPixelAspectRatio(4, 3),
            Is.EqualTo(AvifResultCode.Success));

        AvifRenderOptions options = new AvifRenderOptions();
        AvifPixelBuffer output = transform.Apply(source, options);
        AvifPixelAspectRatio? renderedRatio =
            transform.GetRenderedPixelAspectRatio(options);

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetWidth(), Is.EqualTo(2));
            Assert.That(output.GetHeight(), Is.EqualTo(3));
            Assert.That(GetPixels(output),
                Is.EqualTo(new byte[] { 9, 10, 5, 6, 1, 2 }));
            Assert.That((renderedRatio ?? throw new InvalidOperationException("Invalid internal state.")).GetHorizontalSpacing(), Is.EqualTo(4));
            Assert.That(renderedRatio.GetVerticalSpacing(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ExifReconciliationSuppressesEquivalentTransformsAndRejectsConflicts()
    {
        AvifTransform matching = new AvifTransform();
        matching.SetRotation(3);
        AvifExif clockwiseExif = CreateExif(6);
        AvifResultCode matchingCode =
            matching.ReconcileExifOrientation(clockwiseExif);

        AvifTransform conflicting = new AvifTransform();
        conflicting.SetRotation(3);
        AvifExif counterClockwiseExif = CreateExif(8);
        AvifResultCode conflictCode =
            conflicting.ReconcileExifOrientation(counterClockwiseExif);

        Assert.Multiple(() =>
        {
            Assert.That(matchingCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(matching.GetExifOrientation(), Is.EqualTo(1));
            Assert.That(conflictCode,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(conflicting.GetExifOrientation(), Is.Zero);
        });
    }

    [Test]
    public void ComponentsCanBeClearedAndAspectRatioTracksAxisExchange()
    {
        AvifTransform transform = new AvifTransform();
        transform.SetCleanAperture(2, 1, 3, 1, 0, 1, 0, 1);
        transform.SetRotation(1);
        transform.SetMirror(0);
        transform.SetPixelAspectRatio(4, 3);
        transform.SetExifOrientation(6);
        AvifRenderOptions rotationOnly = new AvifRenderOptions();
        rotationOnly.ApplyExifOrientation = false;
        AvifRenderOptions rotationAndExif = new AvifRenderOptions();
        AvifPixelAspectRatio? exchanged =
            transform.GetRenderedPixelAspectRatio(rotationOnly);
        AvifPixelAspectRatio? exchangedTwice =
            transform.GetRenderedPixelAspectRatio(rotationAndExif);

        transform.ClearCleanAperture();
        transform.ClearRotation();
        transform.ClearMirror();
        transform.ClearPixelAspectRatio();
        transform.ClearExifOrientation();

        Assert.Multiple(() =>
        {
            Assert.That((exchanged ?? throw new InvalidOperationException("Invalid internal state.")).GetHorizontalSpacing(), Is.EqualTo(3));
            Assert.That(exchanged.GetVerticalSpacing(), Is.EqualTo(4));
            Assert.That((exchangedTwice ?? throw new InvalidOperationException("Invalid internal state.")).GetHorizontalSpacing(), Is.EqualTo(4));
            Assert.That(exchangedTwice.GetVerticalSpacing(), Is.EqualTo(3));
            Assert.That(transform.HasCleanAperture(), Is.False);
            Assert.That(transform.HasRotation(), Is.False);
            Assert.That(transform.HasMirror(), Is.False);
            Assert.That(transform.HasPixelAspectRatio(), Is.False);
            Assert.That(transform.GetExifOrientation(), Is.Zero);
            Assert.That(transform.GetPixelAspectRatio().GetHorizontalSpacing(),
                Is.EqualTo(1));
            Assert.That(transform.GetRenderedPixelAspectRatio(null), Is.Null);
        });
    }

    [Test]
    public void OptionsCanReturnUntransformedPixelsAndInvalidCropsFailSafely()
    {
        AvifPixelBuffer source = CreateGrayBuffer(4, 3);
        AvifTransform transform = new AvifTransform();
        transform.SetCleanAperture(3, 2, 2, 1, 0, 1, 0, 1);
        transform.SetRotation(1);
        transform.SetMirror(0);
        transform.SetExifOrientation(8);

        AvifRenderOptions disabled = new AvifRenderOptions();
        disabled.ApplyCleanAperture = false;
        disabled.ApplyRotation = false;
        disabled.ApplyMirror = false;
        disabled.ApplyExifOrientation = false;
        AvifPixelBuffer unchanged = transform.Apply(source, disabled);

        AvifRenderOptions enabled = new AvifRenderOptions();
        AvifPixelBuffer invalidCrop = transform.Apply(source, enabled);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(unchanged.GetWidth(), Is.EqualTo(4));
            Assert.That(unchanged.GetHeight(), Is.EqualTo(3));
            Assert.That(GetPixels(unchanged),
                Is.EqualTo(new byte[]
                {
                    0, 1, 2, 3,
                    4, 5, 6, 7,
                    8, 9, 10, 11
                }));
            Assert.That(invalidCrop.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
        });
    }

    static AvifPixelBuffer CreateGrayBuffer(int width, int height)
    {
        AvifPixelBuffer buffer = AvifPixelBuffer.Create(
            width,
            height,
            AvifPixelFormat.Gray,
            8,
            false);
        int value = 0;
        int row = 0;
        while (row < height)
        {
            Span<byte> pixels = buffer.GetRowSpan(row);
            int column = 0;
            while (column < width)
            {
                pixels[column] = (byte)value;
                value++;
                column++;
            }
            row++;
        }
        return buffer;
    }

    static byte[] GetPixels(AvifPixelBuffer buffer)
    {
        byte[] pixels = new byte[buffer.GetWidth() * buffer.GetHeight()];
        int row = 0;
        while (row < buffer.GetHeight())
        {
            buffer.GetRowSpan(row).CopyTo(
                pixels.AsSpan(row * buffer.GetWidth(), buffer.GetWidth()));
            row++;
        }
        return pixels;
    }

    static AvifItemProperty CreateCropProperty()
    {
        AdditionalItemPropertyParseResult parsed =
            new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.CleanAperture = new ParsedCleanAperture(
            2, 1, 3, 1, 0, 1, 0, 1);
        return CreateProperty("clap", parsed);
    }

    static AvifItemProperty CreateRotationProperty(int rotation)
    {
        AdditionalItemPropertyParseResult parsed =
            new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.ImageRotation = new ParsedImageRotation(rotation);
        return CreateProperty("irot", parsed);
    }

    static AvifItemProperty CreateMirrorProperty(int axis)
    {
        AdditionalItemPropertyParseResult parsed =
            new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.ImageMirror = new ParsedImageMirror(axis);
        return CreateProperty("imir", parsed);
    }

    static AvifItemProperty CreateAspectProperty(uint horizontal, uint vertical)
    {
        AdditionalItemPropertyParseResult parsed =
            new AdditionalItemPropertyParseResult(AvifResultCode.Success);
        parsed.PixelAspectRatio = new ParsedPixelAspectRatio(horizontal, vertical);
        return CreateProperty("pasp", parsed);
    }

    static AvifItemProperty CreateProperty(
        string type,
        AdditionalItemPropertyParseResult parsed)
    {
        ParsedItemProperty property = new ParsedItemProperty(
            1,
            type,
            0,
            false,
            System.Array.Empty<byte>(),
            System.Array.Empty<byte>());
        property.SetAdditionalProperties(parsed);
        return new AvifItemProperty(new ParsedPropertyAssociation(1, false, property));
    }

    static AvifImageItem CreateItem(List<AvifItemProperty> properties)
    {
        ParsedItem item = new ParsedItem(
            1,
            2,
            false,
            0,
            "av01",
            "image",
            string.Empty,
            string.Empty,
            string.Empty);
        return new AvifImageItem(
            item,
            null,
            true,
            new List<AvifItemExtent>(),
            properties,
            new List<AvifItemReference>(),
            new List<AvifItemReference>(),
            new List<uint>());
    }

    static AvifExif CreateExif(int orientation)
    {
        byte[] data = new byte[30];
        data[4] = 73;
        data[5] = 73;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6, 2), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(12, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(14, 2), 274);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(16, 2), 3);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(18, 4), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(22, 2),
            (ushort)orientation);
        return AvifExif.Create(data);
    }
}
