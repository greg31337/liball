namespace jpeg.Tests;

internal class RgbOutputPackerTests
{
    [Test]
    public void Packer_PacksRgbInputToAllRgbOutputFormats()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };

        Assert.That(packer.PackRgb(source, 2, 1, global::jpeg.JpegPixelFormat.Rgb24), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(packer.PackRgb(source, 2, 1, global::jpeg.JpegPixelFormat.Bgr24), Is.EqualTo(new byte[] { 3, 2, 1, 6, 5, 4 }));
        Assert.That(packer.PackRgb(source, 2, 1, global::jpeg.JpegPixelFormat.Rgba32), Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }));
        Assert.That(packer.PackRgb(source, 2, 1, global::jpeg.JpegPixelFormat.Bgra32), Is.EqualTo(new byte[] { 3, 2, 1, 255, 6, 5, 4, 255 }));
    }

    [Test]
    public void Packer_CopySupportsSourceAndDestinationStrides()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        byte[] source = new byte[]
        {
            1, 2, 3, 4, 5, 6, 99, 99,
            7, 8, 9, 10, 11, 12, 98, 98
        };
        byte[] destination = new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 77,
            0, 0, 0, 0, 0, 0, 0, 0, 88
        };

        packer.CopyRgbToFormat(source, 2, 2, 8, destination, 9, global::jpeg.JpegPixelFormat.Bgra32);

        Assert.That(
            destination,
            Is.EqualTo(new byte[] { 3, 2, 1, 255, 6, 5, 4, 255, 77, 9, 8, 7, 255, 12, 11, 10, 255, 88 }));
    }

    [Test]
    public void Packer_PacksRgbImageToRequestedBuffer()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        global::jpeg.JpegImage source = CreateRgbImage();

        global::jpeg.JpegInterleavedPixelBuffer buffer = packer.PackImage(source, global::jpeg.JpegPixelFormat.Rgba32);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(4));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgba32));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(4));
        Assert.That(buffer.Stride, Is.EqualTo(8));
        Assert.That(buffer.Pixels, Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255, 7, 8, 9, 255, 10, 11, 12, 255 }));
    }

    [Test]
    public void Packer_PacksRgbImageToImage()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        global::jpeg.JpegImage source = CreateRgbImage();

        global::jpeg.JpegImage image = packer.PackImageToImage(source, global::jpeg.JpegPixelFormat.Bgr24);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Bgr24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(image.Pixels, Is.EqualTo(new byte[] { 3, 2, 1, 6, 5, 4, 9, 8, 7, 12, 11, 10 }));
    }

    [Test]
    public void Packer_WritePackedPixelUsesOpaqueAlpha()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        byte[] rgba = new byte[4];
        byte[] bgra = new byte[4];

        packer.WritePackedPixel(10, 20, 30, rgba, 0, global::jpeg.JpegPixelFormat.Rgba32);
        packer.WritePackedPixel(10, 20, 30, bgra, 0, global::jpeg.JpegPixelFormat.Bgra32);

        Assert.That(rgba, Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
        Assert.That(bgra, Is.EqualTo(new byte[] { 30, 20, 10, 255 }));
    }

    [Test]
    public void Packer_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 4;
        source.Height = 0;
        source.Precision = 8;
        source.ComponentCount = 3;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        source.Pixels = new byte[0];

        global::jpeg.JpegInterleavedPixelBuffer buffer = packer.PackImage(source, global::jpeg.JpegPixelFormat.Bgra32);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(16));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Packer_RejectsInvalidImageInputs()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();
        global::jpeg.JpegImage source = CreateRgbImage();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                packer.PackImage(null!, global::jpeg.JpegPixelFormat.Rgb24);
            });

        source.PixelFormat = global::jpeg.JpegPixelFormat.Bgr24;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                packer.PackImage(source, global::jpeg.JpegPixelFormat.Rgb24);
            });

        source = CreateRgbImage();
        source.Pixels = new byte[3];
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                packer.PackImage(source, global::jpeg.JpegPixelFormat.Rgb24);
            });
    }

    [Test]
    public void Packer_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegRgbOutputPacker packer = new global::jpeg.JpegRgbOutputPacker();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                packer.CopyRgbToFormat(null!, 1, 1, 3, new byte[3], 3, global::jpeg.JpegPixelFormat.Rgb24);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                packer.CopyRgbToFormat(new byte[3], 1, 1, 2, new byte[3], 3, global::jpeg.JpegPixelFormat.Rgb24);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                packer.CopyRgbToFormat(new byte[3], 1, 1, 3, new byte[2], 3, global::jpeg.JpegPixelFormat.Rgb24);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                packer.PackRgb(new byte[3], 1, 1, global::jpeg.JpegPixelFormat.L8);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                packer.WritePackedPixel(1, 2, 3, null!, 0, global::jpeg.JpegPixelFormat.Rgb24);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                packer.WritePackedPixel(-1, 2, 3, new byte[3], 0, global::jpeg.JpegPixelFormat.Rgb24);
            });
    }

    public static global::jpeg.JpegImage CreateRgbImage()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        image.Pixels = new byte[]
        {
            1, 2, 3,
            4, 5, 6,
            7, 8, 9,
            10, 11, 12
        };
        return image;
    }
}
