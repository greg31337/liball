using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngImageFactoryTests
{
    [TestCase(PngPixelFormat.Grayscale1, PngColorType.Grayscale, PngBitDepth.One, 1)]
    [TestCase(PngPixelFormat.Grayscale2, PngColorType.Grayscale, PngBitDepth.Two, 1)]
    [TestCase(PngPixelFormat.Grayscale4, PngColorType.Grayscale, PngBitDepth.Four, 2)]
    [TestCase(PngPixelFormat.Grayscale8, PngColorType.Grayscale, PngBitDepth.Eight, 3)]
    [TestCase(PngPixelFormat.Grayscale16, PngColorType.Grayscale, PngBitDepth.Sixteen, 6)]
    [TestCase(PngPixelFormat.Indexed1, PngColorType.IndexedColor, PngBitDepth.One, 1)]
    [TestCase(PngPixelFormat.Indexed2, PngColorType.IndexedColor, PngBitDepth.Two, 1)]
    [TestCase(PngPixelFormat.Indexed4, PngColorType.IndexedColor, PngBitDepth.Four, 2)]
    [TestCase(PngPixelFormat.Indexed8, PngColorType.IndexedColor, PngBitDepth.Eight, 3)]
    [TestCase(PngPixelFormat.Rgb8, PngColorType.Truecolor, PngBitDepth.Eight, 9)]
    [TestCase(PngPixelFormat.Rgb16, PngColorType.Truecolor, PngBitDepth.Sixteen, 18)]
    [TestCase(PngPixelFormat.GrayscaleAlpha8, PngColorType.GrayscaleAlpha, PngBitDepth.Eight, 6)]
    [TestCase(PngPixelFormat.GrayscaleAlpha16, PngColorType.GrayscaleAlpha, PngBitDepth.Sixteen, 12)]
    [TestCase(PngPixelFormat.Rgba8, PngColorType.TruecolorAlpha, PngBitDepth.Eight, 12)]
    [TestCase(PngPixelFormat.Rgba16, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, 24)]
    [TestCase(PngPixelFormat.Bgr8, PngColorType.Truecolor, PngBitDepth.Eight, 9)]
    [TestCase(PngPixelFormat.Bgr16, PngColorType.Truecolor, PngBitDepth.Sixteen, 18)]
    [TestCase(PngPixelFormat.Bgra8, PngColorType.TruecolorAlpha, PngBitDepth.Eight, 12)]
    [TestCase(PngPixelFormat.Bgra16, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, 24)]
    [TestCase(PngPixelFormat.Argb8, PngColorType.TruecolorAlpha, PngBitDepth.Eight, 12)]
    [TestCase(PngPixelFormat.Argb16, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, 24)]
    public void CreateBlank_CreatesImageForPixelFormat(PngPixelFormat pixelFormat, PngColorType colorType, PngBitDepth bitDepth, int rowBytes)
    {
        PngImage image = new PngImageFactory().CreateImage(3, 2, pixelFormat);

        Assert.That(image.Info.Width, Is.EqualTo(3));
        Assert.That(image.Info.Height, Is.EqualTo(2));
        Assert.That(image.Info.PixelFormat, Is.EqualTo(pixelFormat));
        Assert.That(image.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(image.Info.BitDepth, Is.EqualTo(bitDepth));
        Assert.That(image.Info.CompressionMethod, Is.EqualTo(PngCompressionMethod.Deflate));
        Assert.That(image.Info.FilterMethod, Is.EqualTo(PngFilterMethod.Adaptive));
        Assert.That(image.Info.InterlaceMethod, Is.EqualTo(PngInterlaceMethod.None));
        Assert.That(image.Info.IsAnimated, Is.False);
        Assert.That(image.Info.FrameCount, Is.EqualTo(0));
        Assert.That(image.Info.PlayCount, Is.EqualTo(0));

        Assert.That(image.Pixels.Format, Is.EqualTo(pixelFormat));
        Assert.That(image.Pixels.Width, Is.EqualTo(3));
        Assert.That(image.Pixels.Height, Is.EqualTo(2));
        Assert.That(image.Pixels.Stride, Is.EqualTo(rowBytes));
        Assert.That(image.Pixels.Data, Has.Length.EqualTo(rowBytes * 2));
        AssertBytesAreZero(image.Pixels.Data);
        Assert.That(image.Animation.Frames, Is.Empty);
        Assert.That(image.Warnings, Is.Empty);
    }

    [Test]
    public void CreateImage_UsesBlankFactory()
    {
        PngImage image = Png.CreateImage(5, 4, PngPixelFormat.Rgba8);

        Assert.That(image.Info.Width, Is.EqualTo(5));
        Assert.That(image.Info.Height, Is.EqualTo(4));
        Assert.That(image.Info.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(image.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(image.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(image.Pixels.Stride, Is.EqualTo(20));
        Assert.That(image.Pixels.Data, Has.Length.EqualTo(80));
        AssertBytesAreZero(image.Pixels.Data);
    }

    [Test]
    public void CreateFromBytes_CreatesImageFromRawPixelBytes()
    {
        byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        PngImage image = new PngImageFactory().CreateImageWithData(2, 1, PngPixelFormat.Rgba8, data);

        Assert.That(image.Info.Width, Is.EqualTo(2));
        Assert.That(image.Info.Height, Is.EqualTo(1));
        Assert.That(image.Info.PixelFormat, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(image.Info.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(image.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(image.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(image.Pixels.Width, Is.EqualTo(2));
        Assert.That(image.Pixels.Height, Is.EqualTo(1));
        Assert.That(image.Pixels.Stride, Is.EqualTo(8));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }

    [Test]
    public void CreateImage_WithRawPixelBytesUsesFactory()
    {
        byte[] data = new byte[] { 1, 2, 3, 4, 5, 6 };

        PngImage image = Png.CreateImage(2, 1, PngPixelFormat.Rgb8, data);

        Assert.That(image.Info.ColorType, Is.EqualTo(PngColorType.Truecolor));
        Assert.That(image.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(image.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(image.Pixels.Stride, Is.EqualTo(6));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }

    [Test]
    public void CreateFromBytes_UsesPackedRowLength()
    {
        byte[] data = new byte[] { 0x80, 0x01, 0x23, 0x45 };

        PngImage image = new PngImageFactory().CreateImageWithData(9, 2, PngPixelFormat.Grayscale1, data);

        Assert.That(image.Pixels.Stride, Is.EqualTo(2));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }

    [Test]
    public void CreateFromBytes_CopiesSourceBytes()
    {
        byte[] data = new byte[] { 1, 2, 3, 4 };

        PngImage image = new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, data);
        data[0] = 99;

        Assert.That(image.Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void CreateBlank_UsesPackedRowLength()
    {
        PngImage grayscale = new PngImageFactory().CreateImage(9, 2, PngPixelFormat.Grayscale1);
        PngImage indexed = new PngImageFactory().CreateImage(5, 3, PngPixelFormat.Indexed4);

        Assert.That(grayscale.Pixels.Stride, Is.EqualTo(2));
        Assert.That(grayscale.Pixels.Data, Has.Length.EqualTo(4));
        Assert.That(indexed.Pixels.Stride, Is.EqualTo(3));
        Assert.That(indexed.Pixels.Data, Has.Length.EqualTo(9));
    }

    [Test]
    public void CreateBlank_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImage(0, 1, PngPixelFormat.Rgba8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImage(1, 0, PngPixelFormat.Rgba8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImage(1, 1, PngPixelFormat.Unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.CreateImage(1, 1, (PngPixelFormat)99));
        Assert.Throws<OverflowException>(() => new PngImageFactory().CreateImage(int.MaxValue, 1, PngPixelFormat.Rgba16));
    }

    [Test]
    public void CreateFromBytes_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImageWithData(0, 1, PngPixelFormat.Rgba8, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImageWithData(1, 0, PngPixelFormat.Rgba8, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Unknown, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[5]));
        Assert.Throws<ArgumentNullException>(() => Png.CreateImage(1, 1, PngPixelFormat.Rgba8, null!));
        Assert.Throws<OverflowException>(() => new PngImageFactory().CreateImageWithData(int.MaxValue, 1, PngPixelFormat.Rgba16, new byte[0]));
    }

    [Test]
    public void CreateFromSpanAndWithStride_CopyOnlyBoundedCallerData()
    {
        PngImageFactory factory = new PngImageFactory();
        byte[] boundedSource = new byte[] { 99, 1, 2, 3, 4, 88 };
        PngImage bounded = factory.CreateImageFromSpan(1, 1, PngPixelFormat.Rgba8, boundedSource.AsSpan(1, 4));
        byte[] paddedSource = new byte[] { 10, 20, 30, 91, 92, 40, 50, 60 };
        PngImage padded = factory.CreateImageWithStride(1, 2, PngPixelFormat.Rgb8, 5, paddedSource);

        boundedSource[1] = 77;
        paddedSource[0] = 77;

        Assert.That(bounded.Pixels.Stride, Is.EqualTo(4));
        Assert.That(bounded.Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(padded.Pixels.Stride, Is.EqualTo(5));
        Assert.That(padded.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 91, 92, 40, 50, 60 }));

        byte[] encoded = new PngCodec().EncodeToBytesWithDefaultOptions(padded);
        PngImage decoded = new PngCodec().DecodeBytesWithDefaultOptions(encoded);
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
    }

    [Test]
    public void CreateIndexedImage_CopiesPaletteAndValidatesEveryIndex()
    {
        PngImageFactory factory = new PngImageFactory();
        PngPalette palette = CreatePalette(
            CreatePaletteEntry(1, 2, 3),
            CreatePaletteEntry(4, 5, 6),
            CreatePaletteEntry(7, 8, 9));
        byte[] indices = new byte[] { 0x18 };

        PngImage image = factory.CreateIndexedImage(3, 1, PngPixelFormat.Indexed2, 1, indices, palette);
        indices[0] = 0;
        palette.Entries[0].Red = 99;

        Assert.That(image.Pixels.Data, Is.EqualTo(new byte[] { 0x18 }));
        Assert.That(image.Metadata.Palette.Entries, Has.Length.EqualTo(3));
        Assert.That(image.Metadata.Palette.Entries[0].Red, Is.EqualTo(1));

        byte[] encoded = new PngCodec().EncodeToBytesWithDefaultOptions(image);
        PngImage decoded = new PngCodec().DecodeBytesWithDefaultOptions(encoded);
        Assert.That(decoded.Pixels.Format, Is.EqualTo(PngPixelFormat.Indexed2));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 0x18 }));
        Assert.That(decoded.Metadata.Palette.Entries[2].Blue, Is.EqualTo(9));

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateIndexedImage(1, 1, PngPixelFormat.Rgb8, 3, new byte[] { 0, 0, 0 }, palette));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateIndexedImage(1, 1, PngPixelFormat.Indexed2, 1, new byte[] { 0xC0 }, palette));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateIndexedImage(1, 1, PngPixelFormat.Indexed1, 1, new byte[] { 0 }, palette));
    }

    [Test]
    public void CreateImageWithMetadata_AttachesCallerMetadata()
    {
        PngMetadata metadata = new PngMetadata();
        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        metadata.Gamma = gamma;

        PngImage image = new PngImageFactory().CreateImageWithMetadata(1, 1, PngPixelFormat.Grayscale8, 1, new byte[] { 27 }, metadata);

        Assert.That(image.Metadata, Is.SameAs(metadata));
        Assert.That(image.Metadata.Gamma, Is.SameAs(gamma));
        Assert.That(image.Pixels.Data, Is.EqualTo(new byte[] { 27 }));
    }

    [Test]
    public void CreateFrameWithData_CopiesPixelsAndPopulatesFrameControlFields()
    {
        byte[] data = new byte[] { 1, 2, 3, 4, 90, 91, 5, 6, 7, 8 };

        PngFrame frame = new PngImageFactory().CreateFrameWithData(1, 2, 3, 4, PngPixelFormat.Rgba8, 6, data, 7, 100, PngApngDisposeOperation.Previous, PngApngBlendOperation.Over);
        data[0] = 99;

        Assert.That(frame.Width, Is.EqualTo(1));
        Assert.That(frame.Height, Is.EqualTo(2));
        Assert.That(frame.XOffset, Is.EqualTo(3));
        Assert.That(frame.YOffset, Is.EqualTo(4));
        Assert.That(frame.DelayNumerator, Is.EqualTo(7));
        Assert.That(frame.DelayDenominator, Is.EqualTo(100));
        Assert.That(frame.DisposeOperation, Is.EqualTo(PngApngDisposeOperation.Previous));
        Assert.That(frame.BlendOperation, Is.EqualTo(PngApngBlendOperation.Over));
        Assert.That(frame.Pixels.Stride, Is.EqualTo(6));
        Assert.That(frame.Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4, 90, 91, 5, 6, 7, 8 }));
        Assert.That(frame.UsesDefaultImage, Is.False);
    }

    [Test]
    public void CreateAnimationImage_ChoosesDefaultFrameOrSeparateStaticImageAndCopiesFrames()
    {
        PngImageFactory factory = new PngImageFactory();
        byte[] defaultPixels = new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 };
        PngImage defaultImage = factory.CreateImageFromSpan(2, 1, PngPixelFormat.Rgba8, defaultPixels);
        PngFrame first = factory.CreateFrameWithData(2, 1, 0, 0, PngPixelFormat.Rgba8, 8, defaultPixels, 1, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source);
        PngFrame second = factory.CreateFrameWithData(1, 1, 1, 0, PngPixelFormat.Rgba8, 4, new byte[] { 7, 8, 9, 255 }, 2, 10, PngApngDisposeOperation.Background, PngApngBlendOperation.Over);
        PngFrame[] sourceFrames = new PngFrame[] { first, second };

        PngImage included = factory.CreateAnimationImage(defaultImage, 3, true, sourceFrames);
        PngImage separate = factory.CreateAnimationImage(defaultImage, 4, false, sourceFrames);
        sourceFrames[0] = second;
        first.Pixels.Data[0] = 99;
        defaultImage.Pixels.Data[0] = 88;

        Assert.That(included.Info.IsAnimated, Is.True);
        Assert.That(included.Info.FrameCount, Is.EqualTo(2));
        Assert.That(included.Info.PlayCount, Is.EqualTo(3));
        Assert.That(included.Animation.FrameCount, Is.EqualTo(2));
        Assert.That(included.Animation.PlayCount, Is.EqualTo(3));
        Assert.That(included.Animation.Frames[0].UsesDefaultImage, Is.True);
        Assert.That(included.Animation.Frames[1].UsesDefaultImage, Is.False);
        Assert.That(included.Animation.Frames[0].Pixels.Data[0], Is.EqualTo(1));
        Assert.That(included.Pixels.Data[0], Is.EqualTo(1));
        Assert.That(separate.Animation.PlayCount, Is.EqualTo(4));
        Assert.That(separate.Animation.Frames[0].UsesDefaultImage, Is.False);
        Assert.That(separate.Animation.Frames[1].UsesDefaultImage, Is.False);

        PngEncoderOptions options = new PngEncoderOptions();
        options.EncodeAnimation = true;
        byte[] includedBytes = new PngCodec().EncodeToBytes(included, options);
        byte[] separateBytes = new PngCodec().EncodeToBytes(separate, options);
        PngIdentifyResult includedInfo = new PngIdentifierService().IdentifyBytes(includedBytes);
        PngIdentifyResult separateInfo = new PngIdentifierService().IdentifyBytes(separateBytes);
        Assert.That(includedInfo.Info.FrameCount, Is.EqualTo(2));
        Assert.That(includedInfo.Info.PlayCount, Is.EqualTo(3));
        Assert.That(separateInfo.Info.FrameCount, Is.EqualTo(2));
        Assert.That(separateInfo.Info.PlayCount, Is.EqualTo(4));
    }

    [Test]
    public void ExtendedCreationApis_RejectInvalidStrideFramesAndAnimationChoices()
    {
        PngImageFactory factory = new PngImageFactory();
        PngImage image = factory.CreateImageFromSpan(1, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 4 });
        PngFrame matching = factory.CreateFrameWithData(1, 1, 0, 0, PngPixelFormat.Rgba8, 4, new byte[] { 1, 2, 3, 4 }, 1, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source);
        PngFrame different = factory.CreateFrameWithData(1, 1, 0, 0, PngPixelFormat.Rgba8, 4, new byte[] { 9, 8, 7, 6 }, 1, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source);

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateImageFromSpan(1, 1, PngPixelFormat.Rgba8, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateImageWithStride(1, 1, PngPixelFormat.Rgba8, 3, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateImageWithStride(1, 2, PngPixelFormat.Rgba8, 5, new byte[8]));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateFrameWithData(1, 1, -1, 0, PngPixelFormat.Rgba8, 4, new byte[4], 1, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateFrameWithData(1, 1, 0, 0, PngPixelFormat.Rgba8, 4, new byte[4], 65536, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAnimationImage(image, -1, false, new PngFrame[] { matching }));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAnimationImage(image, 0, false, new PngFrame[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAnimationImage(image, 0, true, new PngFrame[] { different }));
    }

    static PngPalette CreatePalette(params PngPaletteEntry[] entries)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = entries;
        return palette;
    }

    static PngPaletteEntry CreatePaletteEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static void AssertBytesAreZero(byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            Assert.That(data[index], Is.EqualTo(0));
            index += 1;
        }
    }
}
