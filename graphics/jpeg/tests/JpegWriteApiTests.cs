namespace jpeg.Tests;

internal class JpegWriteApiTests
{
    [Test]
    public void Write_EncodesBaselineGrayscaleByteArray()
    {
        global::jpeg.JpegImage source = CreateGrayscaleImage(8, 8, 138);
        source.Metadata.Comments.Add("encoded note");

        byte[] data = global::jpeg.Jpeg.Write(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);

        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(2));
        Assert.That(info.CommentCount, Is.EqualTo(1));
        Assert.That(info.Metadata.Comments[0], Is.EqualTo("encoded note"));
        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[63], Is.EqualTo(138));
    }

    [Test]
    public void Write_EncodesBaselineGrayscaleStreamWithOptions()
    {
        global::jpeg.JpegImage source = CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.RestartInterval = 1;
        System.IO.MemoryStream stream = new System.IO.MemoryStream();

        global::jpeg.Jpeg.Write(source, stream, options);
        byte[] data = stream.ToArray();
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);

        Assert.That(data.Length, Is.GreaterThan(0));
        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoded.Width, Is.EqualTo(16));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[8], Is.EqualTo(143));
    }

    [Test]
    public void Write_RejectsUnsupportedPixelFormat()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 1;
        image.Height = 1;
        image.Precision = 8;
        image.ComponentCount = 4;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgba32;
        image.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        image.Pixels = new byte[] { 1, 2, 3, 255 };

        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                global::jpeg.Jpeg.Write(image);
            });
    }

    [Test]
    public void Write_EncodesProgressiveOption()
    {
        global::jpeg.JpegImage image = CreateGrayscaleImage(8, 8, 128);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Progressive = true;

        byte[] data = global::jpeg.Jpeg.Write(image, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(info.Progressive, Is.True);
        Assert.That(info.ScanCount, Is.EqualTo(2));
    }

    [Test]
    public void Write_RejectsNullOptions()
    {
        global::jpeg.JpegImage image = CreateGrayscaleImage(8, 8, 128);
        System.IO.MemoryStream stream = new System.IO.MemoryStream();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Write(image, (global::jpeg.JpegWriteOptions)null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Write(image, stream, null!);
            });
    }

    public static global::jpeg.JpegImage CreateGrayscaleImage(int width, int height, int value)
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.ComponentCount = 1;
        image.PixelFormat = global::jpeg.JpegPixelFormat.L8;
        image.ColorSpace = global::jpeg.JpegColorSpace.Grayscale;
        image.Subsampling = global::jpeg.JpegSubsampling.Grayscale;
        image.Pixels = new byte[width * height];

        int index = 0;
        while (index < image.Pixels.Length)
        {
            image.Pixels[index] = (byte)value;
            index = index + 1;
        }

        return image;
    }

    public static global::jpeg.JpegImage CreateTwoBlockGrayscaleImage()
    {
        global::jpeg.JpegImage image = CreateGrayscaleImage(16, 8, 138);
        int y = 0;
        while (y < image.Height)
        {
            int x = 8;
            while (x < image.Width)
            {
                image.Pixels[y * image.Width + x] = 143;
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }
}
