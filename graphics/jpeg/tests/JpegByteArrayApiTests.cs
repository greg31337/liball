namespace jpeg.Tests;

internal class JpegByteArrayApiTests
{
    [Test]
    public void IdentifyBytes_AndDecode_AreByteArrayAliases()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.IdentifyBytes(data);
        global::jpeg.JpegImage image = global::jpeg.Jpeg.Decode(data);

        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void Encode_IsByteArrayAliasForWrite()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);

        byte[] data = global::jpeg.Jpeg.Encode(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Decode(data);

        Assert.That(data.Length, Is.GreaterThan(0));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void EncodePixels_CreatesAndEncodesImageFromByteArray()
    {
        byte[] pixels = CreateFilledPixels(64, 143);

        byte[] data = global::jpeg.Jpeg.EncodePixels(pixels, 8, 8, global::jpeg.JpegPixelFormat.L8);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Decode(data);

        Assert.That(decoded.Width, Is.EqualTo(8));
        Assert.That(decoded.Height, Is.EqualTo(8));
        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(decoded.Pixels[0], Is.EqualTo(143));
        Assert.That(decoded.Pixels[63], Is.EqualTo(143));
    }

    [Test]
    public void CreateImageFromPixels_CopiesPixelBytesAndSetsFormatMetadata()
    {
        byte[] pixels = new byte[] { 10, 20, 30, 40, 50, 60 };

        global::jpeg.JpegImage image = global::jpeg.Jpeg.CreateImageFromPixels(pixels, 2, 1, global::jpeg.JpegPixelFormat.Rgb24);
        pixels[0] = 99;

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(1));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(image.Pixels[0], Is.EqualTo(10));
    }

    [Test]
    public void EncodePixels_RejectsInvalidByteArrays()
    {
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.EncodePixels(null!, 8, 8, global::jpeg.JpegPixelFormat.L8);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.EncodePixels(new byte[63], 8, 8, global::jpeg.JpegPixelFormat.L8);
            });
        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                global::jpeg.Jpeg.EncodePixels(new byte[16], 2, 2, global::jpeg.JpegPixelFormat.Rgba32);
            });
    }

    public static byte[] CreateFilledPixels(int count, int value)
    {
        byte[] pixels = new byte[count];
        int index = 0;
        while (index < count)
        {
            pixels[index] = (byte)value;
            index = index + 1;
        }

        return pixels;
    }
}
