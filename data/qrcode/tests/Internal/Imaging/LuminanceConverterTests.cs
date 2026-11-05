using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class LuminanceConverterTests
{
    [Test]
    public void CopiesGray8RowsWithoutIncludingStridePadding()
    {
        byte[] pixels = new byte[]
        {
            0, 128, 255, 201, 202,
            10, 20, 30
        };
        byte[] original = (byte[])pixels.Clone();
        QrImage image = new QrImage
        {
            Pixels = pixels,
            Width = 3,
            Height = 2,
            Stride = 5,
            PixelFormat = QrPixelFormat.Gray8
        };

        QrImage result = LuminanceConverter.ConvertToGray8(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.Stride, Is.EqualTo(3));
            Assert.That(result.PixelFormat, Is.EqualTo(QrPixelFormat.Gray8));
            Assert.That(result.Pixels, Is.EqualTo(new byte[] { 0, 128, 255, 10, 20, 30 }));
            Assert.That(result.Pixels, Is.Not.SameAs(pixels));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void ConvertsRgb24UsingDeterministicRec601Weights()
    {
        QrImage image = new QrImage
        {
            Pixels = new byte[]
            {
                0, 0, 0,
                255, 255, 255,
                255, 0, 0,
                0, 255, 0,
                0, 0, 255
            },
            Width = 5,
            Height = 1,
            Stride = 15,
            PixelFormat = QrPixelFormat.Rgb24
        };

        QrImage result = LuminanceConverter.ConvertToGray8(
            image,
            new QrDetectionOptions());

        Assert.That(result.Pixels, Is.EqualTo(new byte[] { 0, 255, 76, 150, 29 }));
    }

    [Test]
    public void Rgba32CompositesAlphaOverWhite()
    {
        QrImage image = new QrImage
        {
            Pixels = new byte[]
            {
                255, 0, 0, 255,
                0, 0, 0, 0,
                0, 0, 0, 128,
                255, 0, 0, 128
            },
            Width = 4,
            Height = 1,
            Stride = 16,
            PixelFormat = QrPixelFormat.Rgba32
        };

        QrImage result = LuminanceConverter.ConvertToGray8(
            image,
            new QrDetectionOptions());

        Assert.That(result.Pixels, Is.EqualTo(new byte[] { 76, 255, 127, 165 }));
    }

    [Test]
    public void Bgra32UsesBlueFirstLayoutAndIgnoresPadding()
    {
        QrImage image = new QrImage
        {
            Pixels = new byte[]
            {
                0, 0, 255, 255,
                255, 0, 0, 255,
                10, 20, 30, 0,
                99, 98, 97, 96
            },
            Width = 3,
            Height = 1,
            Stride = 16,
            PixelFormat = QrPixelFormat.Bgra32
        };

        QrImage result = LuminanceConverter.ConvertToGray8(
            image,
            new QrDetectionOptions());

        Assert.That(result.Pixels, Is.EqualTo(new byte[] { 76, 29, 255 }));
    }

    [Test]
    public void EnforcesConfiguredDimensionsAndPixelCount()
    {
        QrImage image = new QrImage
        {
            Pixels = new byte[12],
            Width = 4,
            Height = 3,
            Stride = 4,
            PixelFormat = QrPixelFormat.Gray8
        };
        QrDetectionOptions widthLimit = new QrDetectionOptions
        {
            MaximumImageWidth = 3
        };
        QrDetectionOptions heightLimit = new QrDetectionOptions
        {
            MaximumImageHeight = 2
        };
        QrDetectionOptions areaLimit = new QrDetectionOptions
        {
            MaximumImagePixels = 11
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => LuminanceConverter.ConvertToGray8(image, widthLimit),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(image, heightLimit),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(image, areaLimit),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    [Test]
    public void RejectsInvalidBuffersFormatsDimensionsAndStrides()
    {
        QrImage valid = new QrImage
        {
            Pixels = new byte[6],
            Width = 2,
            Height = 1,
            Stride = 6,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrImage nullPixels = new QrImage
        {
            Pixels = null!,
            Width = 1,
            Height = 1,
            Stride = 1,
            PixelFormat = QrPixelFormat.Gray8
        };
        QrImage zeroWidth = new QrImage
        {
            Pixels = new byte[1],
            Height = 1,
            Stride = 1,
            PixelFormat = QrPixelFormat.Gray8
        };
        QrImage invalidFormat = new QrImage
        {
            Pixels = new byte[1],
            Width = 1,
            Height = 1,
            Stride = 1,
            PixelFormat = (QrPixelFormat)4
        };
        QrImage shortStride = new QrImage
        {
            Pixels = new byte[5],
            Width = 2,
            Height = 1,
            Stride = 5,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrImage shortBuffer = new QrImage
        {
            Pixels = new byte[13],
            Width = 2,
            Height = 2,
            Stride = 8,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrDetectionOptions invalidLimit = new QrDetectionOptions
        {
            MaximumImageWidth = 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => LuminanceConverter.ConvertToGray8(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(nullPixels, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(zeroWidth, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(invalidFormat, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(shortStride, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(shortBuffer, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => LuminanceConverter.ConvertToGray8(valid, invalidLimit),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsLayoutArithmeticOverflowBeforeBufferAccess()
    {
        QrImage image = new QrImage
        {
            Pixels = new byte[0],
            Width = int.MaxValue,
            Height = 2,
            Stride = int.MaxValue,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrDetectionOptions options = new QrDetectionOptions
        {
            MaximumImageWidth = int.MaxValue,
            MaximumImageHeight = int.MaxValue,
            MaximumImagePixels = int.MaxValue
        };

        Assert.That(() => LuminanceConverter.ConvertToGray8(image, options),
            Throws.TypeOf<QrResourceLimitException>());
    }
}
