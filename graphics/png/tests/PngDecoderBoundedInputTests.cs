using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngDecoderBoundedInputTests
{
    [Test]
    public void DecodeBytesAtOffset_DecodesEmbeddedDatastreamThroughContract()
    {
        byte[] png = CreatePng();
        byte[] embedded = Join(new byte[] { 90, 91, 92 }, png);
        IPngDecoder decoder = new PngCodec();
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgb8;

        PngImage image = decoder.DecodeBytesAtOffset(embedded, 3, options);

        AssertImage(image, PngPixelFormat.Rgb8, 3, new byte[] { 10, 20, 30 });
        Assert.That(image.Metadata.Chunks[0].Offset, Is.EqualTo(11));
    }

    [Test]
    public void DecodeSpan_UsesExactBoundsWithinLargerArray()
    {
        byte[] png = CreatePng();
        byte[] embedded = Join(new byte[] { 70, 71 }, png, new byte[] { 80, 81, 82 });
        IPngDecoder decoder = new PngCodec();

        PngImage image = decoder.DecodeSpan(embedded.AsSpan(2, png.Length), new PngDecoderOptions());

        AssertImage(image, PngPixelFormat.Rgba8, 4, new byte[] { 10, 20, 30, 40 });
        Assert.That(image.Metadata.Chunks[0].Offset, Is.EqualTo(PngConstants.SignatureLength));
        Assert.Throws<InvalidDataException>(() => DecodeSpanWithTrailingBytes(decoder, embedded, 2));
    }

    [Test]
    public void DecodeSpan_AcceptsStackBackedReadOnlySpan()
    {
        byte[] png = CreatePng();
        Span<byte> storage = stackalloc byte[png.Length];
        png.CopyTo(storage);
        ReadOnlySpan<byte> input = storage;
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgba16;

        PngImage image = new PngCodec().DecodeSpan(input, options);

        AssertImage(image, PngPixelFormat.Rgba16, 8, new byte[] { 10, 10, 20, 20, 30, 30, 40, 40 });
    }

    [Test]
    public void TopLevelBoundedEntryPoints_MatchCodecResults()
    {
        byte[] png = CreatePng();
        byte[] embedded = Join(new byte[] { 1 }, png, new byte[] { 2 });
        PngDecoderOptions offsetOptions = new PngDecoderOptions();
        offsetOptions.IgnoreTrailingBytes = true;

        PngImage offset = Png.DecodeBytesAtOffset(embedded, 1, offsetOptions);
        PngImage span = Png.DecodeSpan(embedded.AsSpan(1, png.Length), new PngDecoderOptions());

        Assert.That(offset.Pixels.Data, Is.EqualTo(span.Pixels.Data));
        Assert.That(offset.Pixels.Format, Is.EqualTo(span.Pixels.Format));
    }

    [Test]
    public void BoundedEntryPoints_RejectInvalidOffsetsBoundsAndOptions()
    {
        byte[] png = CreatePng();
        IPngDecoder decoder = new PngCodec();

        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeBytesAtOffset(png, -1, new PngDecoderOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeBytesAtOffset(png, png.Length + 1, new PngDecoderOptions()));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeBytesAtOffset(null!, 0, new PngDecoderOptions()));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeBytesAtOffset(png, 0, null!));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeSpan(png, null!));
        Assert.Throws<EndOfStreamException>(() => DecodeTruncatedSpan(decoder, png));
        Assert.Throws<InvalidDataException>(() => DecodeEmptySpan(decoder));
    }

    static PngImage DecodeTruncatedSpan(IPngDecoder decoder, byte[] png)
    {
        return decoder.DecodeSpan(png.AsSpan(0, png.Length - 1), new PngDecoderOptions());
    }

    static PngImage DecodeEmptySpan(IPngDecoder decoder)
    {
        return decoder.DecodeSpan(ReadOnlySpan<byte>.Empty, new PngDecoderOptions());
    }

    static PngImage DecodeSpanWithTrailingBytes(IPngDecoder decoder, byte[] data, int offset)
    {
        return decoder.DecodeSpan(data.AsSpan(offset), new PngDecoderOptions());
    }

    static byte[] CreatePng()
    {
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 40 });
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        return Png.EncodeToBytes(image, options);
    }

    static byte[] Join(params byte[][] arrays)
    {
        int length = arrays.Sum(value => value.Length);
        byte[] result = new byte[length];
        int offset = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            Array.Copy(arrays[index], 0, result, offset, arrays[index].Length);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }

    static void AssertImage(PngImage image, PngPixelFormat format, int stride, byte[] data)
    {
        Assert.That(image.Info.PixelFormat, Is.EqualTo(format));
        Assert.That(image.Pixels.Format, Is.EqualTo(format));
        Assert.That(image.Pixels.Stride, Is.EqualTo(stride));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }
}
