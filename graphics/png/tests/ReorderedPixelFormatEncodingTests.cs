using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ReorderedPixelFormatEncodingTests
{
    [Test]
    public void Encode_NormalizesEveryReorderedFormatToPngChannelOrder()
    {
        PngPixelFormat[] formats = new PngPixelFormat[]
        {
            PngPixelFormat.Bgr8,
            PngPixelFormat.Bgr16,
            PngPixelFormat.Bgra8,
            PngPixelFormat.Bgra16,
            PngPixelFormat.Argb8,
            PngPixelFormat.Argb16
        };
        byte[][] sourceData = new byte[][]
        {
            new byte[] { 30, 20, 10 },
            new byte[] { 0x30, 1, 0x20, 2, 0x10, 3 },
            new byte[] { 30, 20, 10, 40 },
            new byte[] { 0x30, 1, 0x20, 2, 0x10, 3, 0x40, 4 },
            new byte[] { 40, 10, 20, 30 },
            new byte[] { 0x40, 4, 0x10, 3, 0x20, 2, 0x30, 1 }
        };
        byte[][] expectedRgba16 = new byte[][]
        {
            new byte[] { 10, 10, 20, 20, 30, 30, 255, 255 },
            new byte[] { 0x10, 3, 0x20, 2, 0x30, 1, 255, 255 },
            new byte[] { 10, 10, 20, 20, 30, 30, 40, 40 },
            new byte[] { 0x10, 3, 0x20, 2, 0x30, 1, 0x40, 4 },
            new byte[] { 10, 10, 20, 20, 30, 30, 40, 40 },
            new byte[] { 0x10, 3, 0x20, 2, 0x30, 1, 0x40, 4 }
        };
        int index = 0;
        while (index < formats.Length)
        {
            PngImage image = Png.CreateImage(1, 1, formats[index], sourceData[index]);
            byte[] encoded = Png.EncodeToBytes(image);
            PngDecoderOptions decoderOptions = new PngDecoderOptions();
            decoderOptions.PreferredPixelFormat = PngPixelFormat.Rgba16;
            PngImage decoded = Png.DecodeBytes(encoded, decoderOptions);

            Assert.That(decoded.Pixels.Data, Is.EqualTo(expectedRgba16[index]), formats[index].ToString());
            index += 1;
        }
    }

    [Test]
    public void Encode_NormalizesReorderedFormatsForAdam7()
    {
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Bgra8, new byte[] { 30, 20, 10, 40 });
        PngEncoderOptions options = new PngEncoderOptions();
        options.InterlaceMethod = PngInterlaceMethod.Adam7;

        PngImage decoded = Png.DecodeBytes(Png.EncodeToBytes(image, options));

        Assert.That(decoded.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
    }

    [Test]
    public void ConvertPixelFormat_NormalizesAndCreatesReorderedLayouts()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer argb = CreateBuffer(PngPixelFormat.Argb8, new byte[] { 40, 10, 20, 30 });
        PngPixelConversionOptions rgbOptions = new PngPixelConversionOptions();
        rgbOptions.TargetFormat = PngPixelFormat.Rgb8;
        PngPixelConversionOptions bgraOptions = new PngPixelConversionOptions();
        bgraOptions.TargetFormat = PngPixelFormat.Bgra16;

        PngPixelBuffer rgb = transformer.ConvertPixelFormat(argb, rgbOptions);
        PngPixelBuffer bgra = transformer.ConvertPixelFormat(argb, bgraOptions);

        Assert.That(rgb.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(rgb.Data, Is.EqualTo(new byte[] { 10, 20, 30 }));
        Assert.That(bgra.Format, Is.EqualTo(PngPixelFormat.Bgra16));
        Assert.That(bgra.Data, Is.EqualTo(new byte[] { 30, 30, 20, 20, 10, 10, 40, 40 }));
    }

    [Test]
    public void Encode_HonorsExplicitAlphaLayoutWhenConversionIsLossless()
    {
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Bgr8, new byte[] { 3, 2, 1 });
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.ColorType = PngColorType.TruecolorAlpha;

        PngImage decoded = Png.DecodeBytes(Png.EncodeToBytes(image, options));

        Assert.That(decoded.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = 1;
        buffer.Height = 1;
        buffer.Stride = data.Length;
        buffer.Data = data;
        return buffer;
    }
}
