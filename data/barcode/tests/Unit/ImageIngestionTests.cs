namespace barcode.Tests;

public class ImageIngestionTests
{
    [TestCase(BarcodePixelFormat.Gray8)]
    [TestCase(BarcodePixelFormat.Rgb24)]
    [TestCase(BarcodePixelFormat.Bgr24)]
    [TestCase(BarcodePixelFormat.Rgba32)]
    [TestCase(BarcodePixelFormat.Bgra32)]
    public void EveryPixelFormatSupportsBothStrideOrientations(
        BarcodePixelFormat format)
    {
        BarcodeImage positive = CreateOrientedImage(format, false);
        BarcodeImage negative = CreateOrientedImage(format, true);

        ManagedGrayImage positiveGray =
            ImageIngestion.ConvertToGray8(positive);
        ManagedGrayImage negativeGray =
            ImageIngestion.ConvertToGray8(negative);
        int[] expected = format == BarcodePixelFormat.Gray8
            ? new int[] { 17, 83, 149, 241 }
            : new int[] { 77, 149, 29, 255 };
        positive.Pixels![positive.Offset] = 0;
        negative.Pixels![negative.Offset] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(positiveGray.Width, Is.EqualTo(2));
            Assert.That(positiveGray.Height, Is.EqualTo(2));
            Assert.That(positiveGray.CopySamples(), Is.EqualTo(expected));
            Assert.That(negativeGray.CopySamples(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void PaddingOffsetsAndSingleRowZeroStrideAreAccepted()
    {
        BarcodeImage padded = new BarcodeImage();
        padded.Pixels = new byte[] { 99, 12, 34, 88 };
        padded.Width = 2;
        padded.Height = 1;
        padded.Stride = 0;
        padded.Offset = 1;
        padded.PixelFormat = BarcodePixelFormat.Gray8;
        padded.DpiX = 300.0;
        padded.DpiY = 200.0;

        ManagedGrayImage gray = ImageIngestion.ConvertToGray8(padded);
        int[] copied = gray.CopySamples();
        copied[0] = 255;

        Assert.Multiple(() =>
        {
            Assert.That(gray.CopySamples(), Is.EqualTo(new int[] { 12, 34 }));
            Assert.That(gray.ReadSample(0, 0), Is.EqualTo(12));
            Assert.That(gray.ReadSample(1, 0), Is.EqualTo(34));
        });
    }

    [Test]
    public void AlphaChannelsDoNotChangeSourceColorLuminance()
    {
        BarcodeImage transparent = CreateSingleColorImage(
            BarcodePixelFormat.Rgba32,
            20,
            100,
            220,
            0);
        BarcodeImage opaque = CreateSingleColorImage(
            BarcodePixelFormat.Bgra32,
            20,
            100,
            220,
            255);

        ManagedGrayImage transparentGray =
            ImageIngestion.ConvertToGray8(transparent);
        ManagedGrayImage opaqueGray =
            ImageIngestion.ConvertToGray8(opaque);

        Assert.That(
            transparentGray.ReadSample(0, 0),
            Is.EqualTo(opaqueGray.ReadSample(0, 0)).And.EqualTo(90));
    }

    [Test]
    public void InvalidDimensionsFormatsStridesOffsetsAndRowsAreRejected()
    {
        BarcodeImage missingPixels = new BarcodeImage();
        missingPixels.Width = 1;
        missingPixels.Height = 1;

        BarcodeImage zeroWidth = CreateGrayImage(new byte[1], 0, 1, 1, 0);
        BarcodeImage shortPositive = CreateGrayImage(
            new byte[4],
            2,
            2,
            1,
            0);
        BarcodeImage shortNegative = CreateGrayImage(
            new byte[4],
            2,
            2,
            -1,
            2);
        BarcodeImage beforeBuffer = CreateGrayImage(
            new byte[2],
            1,
            2,
            -1,
            0);
        BarcodeImage afterBuffer = CreateGrayImage(
            new byte[2],
            1,
            2,
            1,
            1);
        BarcodeImage negativeOffset = CreateGrayImage(
            new byte[1],
            1,
            1,
            0,
            -1);
        BarcodeImage invalidFormat = CreateGrayImage(
            new byte[1],
            1,
            1,
            1,
            0);
        invalidFormat.PixelFormat = (BarcodePixelFormat)99;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ImageIngestion.ConvertToGray8(null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(missingPixels),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(zeroWidth),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(shortPositive),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(shortNegative),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(beforeBuffer),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(afterBuffer),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(negativeOffset),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(invalidFormat),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void InvalidImageResolutionAndManagedSampleAccessAreRejected()
    {
        BarcodeImage negativeDpi = CreateGrayImage(
            new byte[1],
            1,
            1,
            1,
            0);
        negativeDpi.DpiX = -1.0;
        BarcodeImage infiniteDpi = CreateGrayImage(
            new byte[1],
            1,
            1,
            1,
            0);
        infiniteDpi.DpiY = double.PositiveInfinity;
        BarcodeImage nanDpi = CreateGrayImage(
            new byte[1],
            1,
            1,
            1,
            0);
        nanDpi.DpiX = double.NaN;
        ManagedGrayImage gray = new ManagedGrayImage(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ImageIngestion.ConvertToGray8(negativeDpi),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(infiniteDpi),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(nanDpi),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => gray.ReadSample(-1, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => gray.ReadSample(0, 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => gray.WriteSample(0, 0, -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => gray.WriteSample(0, 0, 256),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BarcodeImage CreateOrientedImage(
        BarcodePixelFormat format,
        bool negativeStride)
    {
        int bytesPerPixel = ResolveBytesPerPixel(format);
        int rowBytes = 2 * bytesPerPixel;
        int absoluteStride = rowBytes + 3;
        int prefix = 2;
        int offset = negativeStride ? prefix + absoluteStride : prefix;
        int stride = negativeStride ? -absoluteStride : absoluteStride;
        byte[] pixels = new byte[prefix + absoluteStride + rowBytes + 2];
        int firstRow = offset;
        int secondRow = offset + stride;
        if (format == BarcodePixelFormat.Gray8)
        {
            pixels[firstRow] = 17;
            pixels[firstRow + 1] = 83;
            pixels[secondRow] = 149;
            pixels[secondRow + 1] = 241;
        }
        else
        {
            WriteColorPixel(pixels, firstRow, format, 255, 0, 0, 3);
            WriteColorPixel(
                pixels,
                firstRow + bytesPerPixel,
                format,
                0,
                255,
                0,
                67);
            WriteColorPixel(pixels, secondRow, format, 0, 0, 255, 129);
            WriteColorPixel(
                pixels,
                secondRow + bytesPerPixel,
                format,
                255,
                255,
                255,
                0);
        }

        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = 2;
        image.Height = 2;
        image.Stride = stride;
        image.Offset = offset;
        image.PixelFormat = format;
        return image;
    }

    static BarcodeImage CreateSingleColorImage(
        BarcodePixelFormat format,
        int red,
        int green,
        int blue,
        int alpha)
    {
        int bytesPerPixel = ResolveBytesPerPixel(format);
        byte[] pixels = new byte[bytesPerPixel];
        WriteColorPixel(
            pixels,
            0,
            format,
            red,
            green,
            blue,
            alpha);
        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = 1;
        image.Height = 1;
        image.Stride = bytesPerPixel;
        image.PixelFormat = format;
        return image;
    }

    static BarcodeImage CreateGrayImage(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int offset)
    {
        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = width;
        image.Height = height;
        image.Stride = stride;
        image.Offset = offset;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        return image;
    }

    static int ResolveBytesPerPixel(BarcodePixelFormat format)
    {
        if (format == BarcodePixelFormat.Gray8)
        {
            return 1;
        }

        if (format == BarcodePixelFormat.Rgb24
            || format == BarcodePixelFormat.Bgr24)
        {
            return 3;
        }

        return 4;
    }

    static void WriteColorPixel(
        byte[] pixels,
        int index,
        BarcodePixelFormat format,
        int red,
        int green,
        int blue,
        int alpha)
    {
        bool blueFirst = format == BarcodePixelFormat.Bgr24
            || format == BarcodePixelFormat.Bgra32;
        pixels[index] = (byte)(blueFirst ? blue : red);
        pixels[index + 1] = (byte)green;
        pixels[index + 2] = (byte)(blueFirst ? red : blue);
        if (format == BarcodePixelFormat.Rgba32
            || format == BarcodePixelFormat.Bgra32)
        {
            pixels[index + 3] = (byte)alpha;
        }
    }
}
