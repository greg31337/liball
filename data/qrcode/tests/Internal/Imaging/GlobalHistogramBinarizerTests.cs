using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class GlobalHistogramBinarizerTests
{
    [Test]
    public void SelectsMidpointOfBimodalHistogramAndCreatesBothPolarities()
    {
        byte[] pixels = new byte[]
        {
            20, 20, 220, 220, 91,
            220, 20, 220, 20
        };
        byte[] original = (byte[])pixels.Clone();
        QrImage image = new QrImage
        {
            Pixels = pixels,
            Width = 4,
            Height = 2,
            Stride = 5,
            PixelFormat = QrPixelFormat.Gray8
        };

        GlobalHistogramResult result = GlobalHistogramBinarizer.Binarize(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Threshold, Is.EqualTo(120));
            Assert.That(result.LowContrast, Is.False);
            Assert.That(result.DarkOnLight.Width, Is.EqualTo(4));
            Assert.That(result.DarkOnLight.Height, Is.EqualTo(2));
            Assert.That(result.DarkOnLight.Stride, Is.EqualTo(4));
            Assert.That(result.DarkOnLight.Pixels, Is.EqualTo(new byte[]
            {
                0, 0, 255, 255,
                255, 0, 255, 0
            }));
            Assert.That(result.LightOnDark.Pixels, Is.EqualTo(new byte[]
            {
                255, 255, 0, 0,
                0, 255, 0, 255
            }));
            Assert.That(result.DarkOnLight.Pixels, Is.Not.SameAs(result.LightOnDark.Pixels));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void DetectsUniformAndNarrowRangeImagesAsLowContrast()
    {
        QrImage uniform = CreateImage(new byte[] { 100, 100, 100, 100 }, 4, 1, 4);
        QrImage narrow = CreateImage(new byte[] { 100, 120, 100, 120 }, 4, 1, 4);

        GlobalHistogramResult uniformResult = GlobalHistogramBinarizer.Binarize(
            uniform,
            new QrDetectionOptions());
        GlobalHistogramResult narrowResult = GlobalHistogramBinarizer.Binarize(
            narrow,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(uniformResult.LowContrast, Is.True);
            Assert.That(uniformResult.Threshold, Is.EqualTo(100));
            Assert.That(narrowResult.LowContrast, Is.True);
            Assert.That(narrowResult.Threshold, Is.EqualTo(110));
        });
    }

    [Test]
    public void TreatsMinimumContrastSpanAsUsable()
    {
        QrImage image = CreateImage(new byte[] { 80, 104, 80, 104 }, 4, 1, 4);

        GlobalHistogramResult result = GlobalHistogramBinarizer.Binarize(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.LowContrast, Is.False);
            Assert.That(result.Threshold, Is.EqualTo(92));
        });
    }

    [Test]
    public void IgnoresOnePercentTailsWhenDetectingLowContrast()
    {
        byte[] pixels = new byte[200];
        pixels[0] = 0;
        pixels[1] = 255;
        for (int index = 2; index < 101; index++)
        {
            pixels[index] = 100;
        }

        for (int index = 101; index < pixels.Length; index++)
        {
            pixels[index] = 110;
        }

        QrImage image = CreateImage(pixels, 200, 1, 200);

        GlobalHistogramResult result = GlobalHistogramBinarizer.Binarize(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.LowContrast, Is.True);
            Assert.That(result.Threshold, Is.EqualTo(105));
        });
    }

    [Test]
    public void EnforcesConfiguredImageLimits()
    {
        QrImage image = CreateImage(new byte[12], 4, 3, 4);
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
            Assert.That(() => GlobalHistogramBinarizer.Binarize(image, widthLimit),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(image, heightLimit),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(image, areaLimit),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    [Test]
    public void RejectsInvalidImagesAndOptions()
    {
        QrImage valid = CreateImage(new byte[] { 0 }, 1, 1, 1);
        QrImage nullPixels = CreateImage(null!, 1, 1, 1);
        QrImage color = new QrImage
        {
            Pixels = new byte[3],
            Width = 1,
            Height = 1,
            Stride = 3,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrImage zeroWidth = CreateImage(new byte[1], 0, 1, 1);
        QrImage shortStride = CreateImage(new byte[2], 2, 1, 1);
        QrImage shortBuffer = CreateImage(new byte[5], 2, 2, 4);

        Assert.Multiple(() =>
        {
            Assert.That(() => GlobalHistogramBinarizer.Binarize(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(nullPixels, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(color, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(zeroWidth, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(shortStride, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GlobalHistogramBinarizer.Binarize(shortBuffer, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrImage CreateImage(byte[] pixels, int width, int height, int stride)
    {
        return new QrImage
        {
            Pixels = pixels,
            Width = width,
            Height = height,
            Stride = stride,
            PixelFormat = QrPixelFormat.Gray8
        };
    }
}
