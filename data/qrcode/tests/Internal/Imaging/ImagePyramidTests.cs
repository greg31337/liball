using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ImagePyramidTests
{
    [Test]
    public void BuildsBoundedFineToCoarseLevelsWithSourceScaleMetadata()
    {
        int width = 64;
        int height = 64;
        byte[] pixels = new byte[width * height];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                bool dark = (row / 8 + column / 8) % 2 == 0;
                pixels[row * width + column] = dark ? (byte)0 : (byte)255;
            }
        }

        ImagePyramidLevel[] levels = ImagePyramid.Build(
            CreateRaster(pixels, width, height, width),
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(levels, Has.Length.EqualTo(4));
            Assert.That(levels[0].ScaleToSource, Is.EqualTo(1));
            Assert.That(levels[0].Raster.Width, Is.EqualTo(64));
            Assert.That(levels[1].ScaleToSource, Is.EqualTo(2));
            Assert.That(levels[1].Raster.Width, Is.EqualTo(32));
            Assert.That(levels[2].ScaleToSource, Is.EqualTo(4));
            Assert.That(levels[2].Raster.Width, Is.EqualTo(16));
            Assert.That(levels[3].ScaleToSource, Is.EqualTo(8));
            Assert.That(levels[3].Raster.Width, Is.EqualTo(8));
            Assert.That(levels[3].Raster.Height, Is.EqualTo(8));
            Assert.That(levels[3].Raster.Pixels[0], Is.EqualTo(0));
            Assert.That(levels[3].Raster.Pixels[1], Is.EqualTo(255));
            Assert.That(levels[3].Raster.Pixels[8], Is.EqualTo(255));
            Assert.That(levels[3].Raster.Pixels[9], Is.EqualTo(0));
        });
    }

    [Test]
    public void PreservesFullResolutionAndStopsBeforeATooSmallLevel()
    {
        byte[] pixels = new byte[15 * 15];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = index % 2 == 0 ? (byte)0 : (byte)255;
        }

        QrDetectionOptions options = new QrDetectionOptions
        {
            MaximumPyramidLevels = int.MaxValue
        };

        ImagePyramidLevel[] levels = ImagePyramid.Build(
            CreateRaster(pixels, 15, 15, 15),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(levels, Has.Length.EqualTo(2));
            Assert.That(levels[0].Raster.Pixels, Is.EqualTo(pixels));
            Assert.That(levels[0].Raster.Pixels, Is.Not.SameAs(pixels));
            Assert.That(levels[1].Raster.Width, Is.EqualTo(8));
            Assert.That(levels[1].Raster.Height, Is.EqualTo(8));
            Assert.That(levels[1].ScaleToSource, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReductionUsesDarkPreservingMajorityAndHandlesOddEdges()
    {
        int width = 15;
        int height = 15;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        pixels[0] = 0;
        pixels[1] = 0;
        pixels[width] = 255;
        pixels[width + 1] = 255;
        pixels[2] = 0;
        pixels[3] = 255;
        pixels[width + 2] = 255;
        pixels[width + 3] = 255;
        pixels[(height - 1) * width + width - 1] = 0;
        QrDetectionOptions options = new QrDetectionOptions
        {
            MaximumPyramidLevels = 2
        };

        ImagePyramidLevel[] levels = ImagePyramid.Build(
            CreateRaster(pixels, width, height, width),
            options);
        QrRaster reduced = levels[1].Raster;

        Assert.Multiple(() =>
        {
            Assert.That(reduced.Pixels[0], Is.EqualTo(0));
            Assert.That(reduced.Pixels[1], Is.EqualTo(255));
            Assert.That(reduced.Pixels[reduced.Pixels.Length - 1], Is.EqualTo(0));
        });
    }

    [Test]
    public void LevelLimitOneCopiesLogicalPixelsWithoutPaddingOrMutation()
    {
        byte[] pixels = new byte[]
        {
            0, 255, 0, 17, 18,
            255, 0, 255
        };
        byte[] original = (byte[])pixels.Clone();
        QrDetectionOptions options = new QrDetectionOptions
        {
            MaximumPyramidLevels = 1
        };

        ImagePyramidLevel[] levels = ImagePyramid.Build(
            CreateRaster(pixels, 3, 2, 5),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(levels, Has.Length.EqualTo(1));
            Assert.That(levels[0].Raster.Width, Is.EqualTo(3));
            Assert.That(levels[0].Raster.Height, Is.EqualTo(2));
            Assert.That(levels[0].Raster.Stride, Is.EqualTo(3));
            Assert.That(levels[0].Raster.Pixels,
                Is.EqualTo(new byte[] { 0, 255, 0, 255, 0, 255 }));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void RejectsInvalidLevelsRasterLayoutsValuesAndLimits()
    {
        QrRaster valid = CreateRaster(new byte[] { 0, 255, 255, 0 }, 2, 2, 2);
        QrRaster nullPixels = CreateRaster(null!, 1, 1, 1);
        QrRaster shortStride = CreateRaster(new byte[2], 2, 1, 1);
        QrRaster shortBuffer = CreateRaster(new byte[5], 2, 2, 4);
        QrRaster nonBinary = CreateRaster(new byte[] { 0, 127 }, 2, 1, 2);
        QrDetectionOptions zeroLevels = new QrDetectionOptions
        {
            MaximumPyramidLevels = 0
        };
        QrDetectionOptions limit = new QrDetectionOptions
        {
            MaximumImagePixels = 3
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => ImagePyramid.Build(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(valid, zeroLevels),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(nullPixels, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(shortStride, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(shortBuffer, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(nonBinary, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ImagePyramid.Build(valid, limit),
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
