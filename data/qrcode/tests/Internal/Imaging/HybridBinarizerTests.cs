using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class HybridBinarizerTests
{
    [Test]
    public void RecoversAlternatingSamplesAcrossStrongIlluminationGradient()
    {
        int width = 48;
        int height = 40;
        int stride = 51;
        byte[] pixels = new byte[stride * height];
        byte[] expected = new byte[width * height];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                bool dark = (column + row) % 2 == 0;
                int illumination = 80 + column * 2;
                pixels[row * stride + column] = (byte)(dark
                    ? illumination - 60
                    : illumination);
                expected[row * width + column] = dark ? (byte)0 : (byte)255;
            }

            pixels[row * stride + width] = 17;
            pixels[row * stride + width + 1] = 18;
            pixels[row * stride + width + 2] = 19;
        }

        byte[] original = (byte[])pixels.Clone();
        QrImage image = CreateImage(pixels, width, height, stride);

        HybridBinarizerResult result = HybridBinarizer.Binarize(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedGlobalHistogram, Is.False);
            Assert.That(result.BlockColumns, Is.EqualTo(6));
            Assert.That(result.BlockRows, Is.EqualTo(5));
            Assert.That(result.Thresholds, Has.Length.EqualTo(30));
            Assert.That(result.Thresholds[0], Is.LessThan(result.Thresholds[29]));
            Assert.That(result.DarkOnLight.Width, Is.EqualTo(width));
            Assert.That(result.DarkOnLight.Height, Is.EqualTo(height));
            Assert.That(result.DarkOnLight.Stride, Is.EqualTo(width));
            Assert.That(result.DarkOnLight.Pixels, Is.EqualTo(expected));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void ProducesComplementaryPolarityRasters()
    {
        QrImage image = CreateGradientImage(40, 40);

        HybridBinarizerResult result = HybridBinarizer.Binarize(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.DarkOnLight.Pixels, Is.Not.SameAs(result.LightOnDark.Pixels));
            Assert.That(result.LightOnDark.Width, Is.EqualTo(40));
            Assert.That(result.LightOnDark.Height, Is.EqualTo(40));
            Assert.That(result.LightOnDark.Stride, Is.EqualTo(40));
        });
        for (int index = 0; index < result.DarkOnLight.Pixels.Length; index++)
        {
            Assert.That(
                result.DarkOnLight.Pixels[index] + result.LightOnDark.Pixels[index],
                Is.EqualTo(255));
        }
    }

    [Test]
    public void FallsBackToGlobalHistogramForSmallImages()
    {
        QrImage image = CreateImage(
            new byte[] { 20, 20, 220, 220, 220, 20, 220, 20 },
            4,
            2,
            4);

        HybridBinarizerResult hybrid = HybridBinarizer.Binarize(
            image,
            new QrDetectionOptions());
        GlobalHistogramResult global = GlobalHistogramBinarizer.Binarize(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(hybrid.UsedGlobalHistogram, Is.True);
            Assert.That(hybrid.BlockColumns, Is.EqualTo(1));
            Assert.That(hybrid.BlockRows, Is.EqualTo(1));
            Assert.That(hybrid.Thresholds, Is.EqualTo(new int[] { global.Threshold }));
            Assert.That(hybrid.DarkOnLight.Pixels, Is.EqualTo(global.DarkOnLight.Pixels));
            Assert.That(hybrid.LightOnDark.Pixels, Is.EqualTo(global.LightOnDark.Pixels));
        });
    }

    [Test]
    public void LowContrastBlocksUseSurroundingThresholdEvidence()
    {
        int width = 40;
        int height = 40;
        byte[] pixels = new byte[width * height];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (column < 8)
                {
                    pixels[row * width + column] = (byte)(row % 2 == 0 ? 40 : 180);
                }
                else
                {
                    pixels[row * width + column] = 170;
                }
            }
        }

        HybridBinarizerResult result = HybridBinarizer.Binarize(
            CreateImage(pixels, width, height, width),
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Thresholds[0], Is.GreaterThan(80));
            Assert.That(result.Thresholds[4], Is.GreaterThan(80));
            Assert.That(result.DarkOnLight.Pixels[20], Is.EqualTo(255));
        });
    }

    [Test]
    public void EnforcesImageValidationAndResourceLimits()
    {
        QrImage valid = CreateGradientImage(40, 40);
        QrImage color = new QrImage
        {
            Pixels = new byte[4800],
            Width = 40,
            Height = 40,
            Stride = 120,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrImage shortBuffer = CreateImage(new byte[1599], 40, 40, 40);
        QrDetectionOptions limit = new QrDetectionOptions
        {
            MaximumImagePixels = 1599
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => HybridBinarizer.Binarize(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => HybridBinarizer.Binarize(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => HybridBinarizer.Binarize(color, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => HybridBinarizer.Binarize(shortBuffer, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => HybridBinarizer.Binarize(valid, limit),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrImage CreateGradientImage(int width, int height)
    {
        byte[] pixels = new byte[width * height];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                pixels[row * width + column] = (byte)(20 + column * 4);
            }
        }

        return CreateImage(pixels, width, height, width);
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
