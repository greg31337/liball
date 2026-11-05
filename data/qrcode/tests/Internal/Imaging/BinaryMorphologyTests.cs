using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BinaryMorphologyTests
{
    [Test]
    public void DenoiseRemovesOnlyFullyIsolatedInteriorPixels()
    {
        int width = 9;
        int height = 7;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        pixels[4 * width + 2] = 0;
        pixels[1 * width + 1] = 0;
        pixels[1 * width + 2] = 0;
        pixels[0 * width + 8] = 0;
        for (int row = 3; row <= 5; row++)
        {
            for (int column = 5; column <= 7; column++)
            {
                pixels[row * width + column] = 0;
            }
        }

        pixels[4 * width + 6] = 255;
        byte[] original = (byte[])pixels.Clone();
        QrDetectionOptions options = new QrDetectionOptions
        {
            EnableDenoise = true
        };

        QrRaster result = BinaryMorphology.Clean(
            CreateRaster(pixels, width, height, width),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Pixels[4 * width + 2], Is.EqualTo(255));
            Assert.That(result.Pixels[4 * width + 6], Is.EqualTo(0));
            Assert.That(result.Pixels[1 * width + 1], Is.EqualTo(0));
            Assert.That(result.Pixels[1 * width + 2], Is.EqualTo(0));
            Assert.That(result.Pixels[8], Is.EqualTo(0));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void GapClosingRepairsSingleHorizontalAndVerticalInterruptions()
    {
        int width = 9;
        int height = 7;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        pixels[1 * width + 1] = 0;
        pixels[1 * width + 3] = 0;
        pixels[1 * width + 6] = 0;
        pixels[3 * width + 6] = 0;
        pixels[5 * width + 1] = 0;
        pixels[5 * width + 4] = 0;
        QrDetectionOptions options = new QrDetectionOptions
        {
            EnableGapClosing = true
        };

        QrRaster result = BinaryMorphology.Clean(
            CreateRaster(pixels, width, height, width),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Pixels[1 * width + 2], Is.EqualTo(0));
            Assert.That(result.Pixels[2 * width + 6], Is.EqualTo(0));
            Assert.That(result.Pixels[5 * width + 2], Is.EqualTo(255));
            Assert.That(result.Pixels[5 * width + 3], Is.EqualTo(255));
            Assert.That(result.Pixels[2 * width + 2], Is.EqualTo(255));
        });
    }

    [Test]
    public void DisabledCleanupReturnsTightlyPackedIndependentCopy()
    {
        byte[] pixels = new byte[]
        {
            0, 255, 0, 17, 18,
            255, 0, 255
        };
        byte[] original = (byte[])pixels.Clone();
        QrRaster raster = CreateRaster(pixels, 3, 2, 5);

        QrRaster result = BinaryMorphology.Clean(raster, new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.Stride, Is.EqualTo(3));
            Assert.That(result.Pixels, Is.EqualTo(new byte[] { 0, 255, 0, 255, 0, 255 }));
            Assert.That(result.Pixels, Is.Not.SameAs(pixels));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void CleanupPassesDoNotCascadeWithinTheSamePass()
    {
        int width = 7;
        int height = 3;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        pixels[1 * width + 1] = 0;
        pixels[1 * width + 3] = 0;
        pixels[1 * width + 5] = 0;
        QrDetectionOptions options = new QrDetectionOptions
        {
            EnableGapClosing = true
        };

        QrRaster result = BinaryMorphology.Clean(
            CreateRaster(pixels, width, height, width),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Pixels[1 * width + 2], Is.EqualTo(0));
            Assert.That(result.Pixels[1 * width + 4], Is.EqualTo(0));
            Assert.That(result.Pixels[1 * width + 6], Is.EqualTo(255));
        });
    }

    [Test]
    public void RejectsInvalidRasterLayoutsValuesAndLimits()
    {
        QrRaster valid = CreateRaster(new byte[] { 0, 255, 255, 0 }, 2, 2, 2);
        QrRaster nullPixels = CreateRaster(null!, 1, 1, 1);
        QrRaster shortStride = CreateRaster(new byte[2], 2, 1, 1);
        QrRaster shortBuffer = CreateRaster(new byte[5], 2, 2, 4);
        QrRaster nonBinary = CreateRaster(new byte[] { 0, 127 }, 2, 1, 2);
        QrDetectionOptions limit = new QrDetectionOptions
        {
            MaximumImagePixels = 3
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => BinaryMorphology.Clean(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BinaryMorphology.Clean(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BinaryMorphology.Clean(nullPixels, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BinaryMorphology.Clean(shortStride, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BinaryMorphology.Clean(shortBuffer, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BinaryMorphology.Clean(nonBinary, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BinaryMorphology.Clean(valid, limit),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static byte[] CreateFilledPixels(int width, int height, byte value)
    {
        byte[] pixels = new byte[width * height];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = value;
        }

        return pixels;
    }

    static QrRaster CreateRaster(byte[] pixels, int width, int height, int stride)
    {
        return new QrRaster
        {
            Pixels = pixels,
            Width = width,
            Height = height,
            Stride = stride
        };
    }
}
