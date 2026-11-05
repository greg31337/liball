using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class EncoderContentAnalysisTests
{
    [Test]
    public void AutomaticAnalysis_RemovesOpaqueAlphaAndPacksExactGrayscale()
    {
        PngImage image = Png.CreateImage(4, 1, PngPixelFormat.Rgba8, new byte[]
        {
            0, 0, 0, 255,
            255, 255, 255, 255,
            0, 0, 0, 255,
            255, 255, 255, 255
        });

        PngEncoderOptions options = new PngEncoderOptions();
        options.MetadataPolicy = PngMetadataPolicy.Strip;
        byte[] encoded = Png.EncodeToBytes(image, options);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.One));
        Assert.That(decoded.Pixels.Format, Is.EqualTo(PngPixelFormat.Grayscale1));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 0x50 }));
    }

    [Test]
    public void AutomaticAnalysis_SelectsPackedGrayscaleForExactLowBitSamples()
    {
        PngImage image = Png.CreateImage(4, 1, PngPixelFormat.Grayscale8, new byte[] { 0, 85, 170, 255 });

        PngEncoderOptions options = new PngEncoderOptions();
        options.MetadataPolicy = PngMetadataPolicy.Strip;
        byte[] encoded = Png.EncodeToBytes(image, options);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Two));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 0x1B }));
    }

    [Test]
    public void AutomaticAnalysis_GeneratesExactPaletteAndTransparency()
    {
        byte[] pattern = new byte[]
        {
            10, 20, 30, 255,
            10, 20, 30, 64,
            40, 50, 60, 128,
            10, 20, 30, 255
        };
        byte[] pixels = new byte[256];
        int pixelIndex = 0;
        while (pixelIndex < 64)
        {
            int component = 0;
            while (component < 4)
            {
                pixels[pixelIndex * 4 + component] = pattern[(pixelIndex % 4) * 4 + component];
                component += 1;
            }

            pixelIndex += 1;
        }

        PngImage image = Png.CreateImage(64, 1, PngPixelFormat.Rgba8, pixels);

        PngEncoderOptions options = new PngEncoderOptions();
        options.MetadataPolicy = PngMetadataPolicy.Strip;
        byte[] encoded = Png.EncodeToBytes(image, options);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.PreferredPixelFormat = PngPixelFormat.Rgba8;
        PngImage decoded = Png.DecodeBytes(encoded, decoderOptions);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Two));
        Assert.That(identify.Metadata.Palette.Entries, Has.Length.EqualTo(3));
        Assert.That(identify.Metadata.IndexedTransparency, Is.Not.Null);
        Assert.That(identify.Metadata.IndexedTransparency!.Alpha, Is.EqualTo(new byte[] { 255, 64, 128 }));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(pixels));
    }

    [Test]
    public void AutomaticAnalysis_UsesOneExactPaletteAcrossAnimationFrames()
    {
        byte[] defaultPixels = CreateRepeatedRgbaPixels(32, 200, 10, 20, 255);
        byte[] framePixels = CreateRepeatedRgbaPixels(32, 30, 40, 220, 96);
        PngImage image = Png.CreateImage(32, 1, PngPixelFormat.Rgba8, defaultPixels);
        PngFrame frame = new PngFrame();
        frame.Width = 32;
        frame.Height = 1;
        frame.DelayNumerator = 1;
        frame.DelayDenominator = 10;
        frame.Pixels = Png.CreateImage(32, 1, PngPixelFormat.Rgba8, framePixels).Pixels;
        image.Animation.FrameCount = 1;
        image.Animation.Frames = new PngFrame[] { frame };
        PngEncoderOptions options = new PngEncoderOptions();
        options.EncodeAnimation = true;

        byte[] encoded = Png.EncodeToBytes(image, options);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.PreferredPixelFormat = PngPixelFormat.Rgba8;
        PngImage decoded = Png.DecodeBytes(encoded, decoderOptions);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.One));
        Assert.That(identify.Metadata.Palette.Entries, Has.Length.EqualTo(2));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(defaultPixels));
        Assert.That(decoded.Animation.Frames, Has.Length.EqualTo(1));
        Assert.That(decoded.Animation.Frames[0].Pixels.Data, Is.EqualTo(framePixels));
    }

    [Test]
    public void AutomaticAnalysis_ReducesLosslessSixteenBitGrayscaleToEightBits()
    {
        byte[] pixels = new byte[512];
        int index = 0;
        while (index < 256)
        {
            pixels[index * 2] = (byte)index;
            pixels[index * 2 + 1] = (byte)index;
            index += 1;
        }

        PngImage image = Png.CreateImage(256, 1, PngPixelFormat.Grayscale16, pixels);
        byte[] encoded = Png.EncodeToBytes(image);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
    }

    [Test]
    public void AutomaticAnalysis_TrimsAndPacksSuppliedIndexedColorsLosslessly()
    {
        PngImage image = Png.CreateImage(4, 1, PngPixelFormat.Indexed8, new byte[] { 0, 1, 2, 1 });
        image.Metadata.Palette = CreatePalette(6);

        byte[] encoded = Png.EncodeToBytes(image);
        PngIdentifyResult identify = Png.IdentifyBytes(encoded);
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.PreferredPixelFormat = PngPixelFormat.Rgb8;
        PngImage decoded = Png.DecodeBytes(encoded, decoderOptions);

        Assert.That(identify.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(identify.Info.BitDepth, Is.EqualTo(PngBitDepth.Two));
        Assert.That(identify.Metadata.Palette.Entries, Has.Length.EqualTo(3));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[]
        {
            1, 10, 20,
            2, 11, 21,
            3, 12, 22,
            2, 11, 21
        }));
    }

    [Test]
    public void Analysis_HonorsExplicitColorTypeAndBitDepthConstraints()
    {
        PngImage rgba = Png.CreateImage(2, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 });
        PngEncoderOptions truecolor = new PngEncoderOptions();
        truecolor.UseAutomaticColorType = false;
        truecolor.ColorType = PngColorType.Truecolor;
        PngIdentifyResult truecolorIdentify = Png.IdentifyBytes(Png.EncodeToBytes(rgba, truecolor));

        PngImage grayscale = Png.CreateImage(2, 1, PngPixelFormat.Grayscale8, new byte[] { 0, 255 });
        PngEncoderOptions eightBit = new PngEncoderOptions();
        eightBit.UseAutomaticBitDepth = false;
        eightBit.BitDepth = PngBitDepth.Eight;
        PngIdentifyResult eightBitIdentify = Png.IdentifyBytes(Png.EncodeToBytes(grayscale, eightBit));

        PngEncoderOptions rgba8 = new PngEncoderOptions();
        rgba8.UseAutomaticColorType = false;
        rgba8.UseAutomaticBitDepth = false;
        rgba8.ColorType = PngColorType.TruecolorAlpha;
        rgba8.BitDepth = PngBitDepth.Eight;
        PngIdentifyResult rgbaIdentify = Png.IdentifyBytes(Png.EncodeToBytes(rgba, rgba8));

        Assert.That(truecolorIdentify.Info.ColorType, Is.EqualTo(PngColorType.Truecolor));
        Assert.That(truecolorIdentify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(eightBitIdentify.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(eightBitIdentify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(rgbaIdentify.Info.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(rgbaIdentify.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
    }

    static PngPalette CreatePalette(int count)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[count];
        int index = 0;
        while (index < count)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)(index + 1);
            entry.Green = (byte)(index + 10);
            entry.Blue = (byte)(index + 20);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static byte[] CreateRepeatedRgbaPixels(int width, int red, int green, int blue, int alpha)
    {
        byte[] pixels = new byte[width * 4];
        int index = 0;
        while (index < width)
        {
            pixels[index * 4] = (byte)red;
            pixels[index * 4 + 1] = (byte)green;
            pixels[index * 4 + 2] = (byte)blue;
            pixels[index * 4 + 3] = (byte)alpha;
            index += 1;
        }

        return pixels;
    }
}
