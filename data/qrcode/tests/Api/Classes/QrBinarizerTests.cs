using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrBinarizerTests
{
    [Test]
    public void CreatesIndependentTightlyPackedLuminanceThroughPublicContract()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        byte[] pixels = new byte[]
        {
            255, 0, 0, 0, 255, 0, 19, 20,
            0, 0, 255, 0, 0, 0
        };
        byte[] original = (byte[])pixels.Clone();
        QrImage image = new QrImage
        {
            Pixels = pixels,
            Width = 2,
            Height = 2,
            Stride = 8,
            PixelFormat = QrPixelFormat.Rgb24
        };

        QrImage result = binarizer.CreateLuminanceImage(
            image,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.Stride, Is.EqualTo(2));
            Assert.That(result.PixelFormat, Is.EqualTo(QrPixelFormat.Gray8));
            Assert.That(result.Pixels, Is.EqualTo(new byte[] { 76, 150, 29, 0 }));
            Assert.That(result.Pixels, Is.Not.SameAs(pixels));
            Assert.That(pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void OptionalContrastNormalizationStretchesLuminanceRange()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        QrImage image = CreateGrayImage(new byte[] { 50, 100, 150 }, 3, 1, 3);
        QrDetectionOptions options = new QrDetectionOptions
        {
            NormalizeContrast = true
        };

        QrImage result = binarizer.CreateLuminanceImage(image, options);

        Assert.That(result.Pixels, Is.EqualTo(new byte[] { 0, 128, 255 }));
    }

    [Test]
    public void AutomaticPolarityReturnsBothNormalizedGlobalMatrices()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        QrImage image = CreateGrayImage(
            new byte[] { 20, 20, 220, 220, 220, 20, 220, 20 },
            4,
            2,
            4);
        QrDetectionOptions options = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.GlobalHistogram
        };

        QrMatrix[] matrices = binarizer.BinarizeImage(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(matrices, Has.Length.EqualTo(2));
            Assert.That(matrices[0].Width, Is.EqualTo(4));
            Assert.That(matrices[0].Height, Is.EqualTo(2));
            Assert.That(matrices[0].Values, Is.EqualTo(new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Light,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark
            }));
            Assert.That(matrices[1].Values, Is.EqualTo(new QrModuleValue[]
            {
                QrModuleValue.Light,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Dark,
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light
            }));
        });
    }

    [Test]
    public void ExplicitPolarityReturnsOnlyTheRequestedNormalization()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        QrDetectionOptions options = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.GlobalHistogram,
            Polarity = QrPolarity.LightOnDark
        };

        QrMatrix[] matrices = binarizer.BinarizeImage(
            CreateGrayImage(new byte[] { 20, 220 }, 2, 1, 2),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(matrices, Has.Length.EqualTo(1));
            Assert.That(matrices[0].Values, Is.EqualTo(new QrModuleValue[]
            {
                QrModuleValue.Light,
                QrModuleValue.Dark
            }));
        });
    }

    [Test]
    public void HybridModeHandlesOverlappingGradientLuminanceRanges()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        int width = 48;
        int height = 40;
        byte[] pixels = new byte[width * height];
        QrModuleValue[] expected = new QrModuleValue[pixels.Length];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                bool dark = (row + column) % 2 == 0;
                int illumination = 80 + column * 2;
                int index = row * width + column;
                pixels[index] = (byte)(dark ? illumination - 60 : illumination);
                expected[index] = dark ? QrModuleValue.Dark : QrModuleValue.Light;
            }
        }

        QrDetectionOptions options = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.HybridLocal,
            Polarity = QrPolarity.DarkOnLight
        };
        QrDetectionOptions automatic = new QrDetectionOptions
        {
            Polarity = QrPolarity.DarkOnLight
        };

        QrMatrix[] matrices = binarizer.BinarizeImage(
            CreateGrayImage(pixels, width, height, width),
            options);
        QrMatrix[] automaticMatrices = binarizer.BinarizeImage(
            CreateGrayImage(pixels, width, height, width),
            automatic);

        Assert.Multiple(() =>
        {
            Assert.That(matrices[0].Values, Is.EqualTo(expected));
            Assert.That(automaticMatrices[0].Values, Is.EqualTo(expected));
        });
    }

    [Test]
    public void SuppliedBinaryModeValidatesValuesAndAppliesCleanupAfterPolarity()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        int width = 5;
        int height = 5;
        byte[] pixels = new byte[width * height];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = 255;
        }

        pixels[2 * width + 2] = 0;
        QrDetectionOptions options = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.SuppliedBinary,
            Polarity = QrPolarity.DarkOnLight,
            EnableDenoise = true,
            NormalizeContrast = true
        };

        QrMatrix[] matrices = binarizer.BinarizeImage(
            CreateGrayImage(pixels, width, height, width),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(matrices[0].Values[2 * width + 2], Is.EqualTo(QrModuleValue.Light));
            Assert.That(pixels[2 * width + 2], Is.EqualTo(0));
            Assert.That(() => binarizer.BinarizeImage(
                CreateGrayImage(new byte[] { 0, 127 }, 2, 1, 2),
                options), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void ReusableInstancesReturnIndependentMatrices()
    {
        IQrBinarizer first = new QrBinarizer();
        IQrBinarizer second = new QrBinarizer();
        QrImage image = CreateGrayImage(new byte[] { 0, 255 }, 2, 1, 2);
        QrDetectionOptions options = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.SuppliedBinary,
            Polarity = QrPolarity.DarkOnLight
        };

        QrMatrix[] firstResult = first.BinarizeImage(image, options);
        QrMatrix[] secondResult = second.BinarizeImage(image, options);
        firstResult[0].Values[0] = QrModuleValue.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(secondResult[0].Values,
                Is.EqualTo(new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light }));
            Assert.That(firstResult[0].Values, Is.Not.SameAs(secondResult[0].Values));
            Assert.That(image.Pixels, Is.EqualTo(new byte[] { 0, 255 }));
        });
    }

    [Test]
    public void RejectsInvalidPublicModesPolaritiesAndSuppliedLayouts()
    {
        IQrBinarizer binarizer = new QrBinarizer();
        QrImage valid = CreateGrayImage(new byte[] { 0 }, 1, 1, 1);
        QrImage color = new QrImage
        {
            Pixels = new byte[] { 0, 0, 0 },
            Width = 1,
            Height = 1,
            Stride = 3,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrDetectionOptions invalidMode = new QrDetectionOptions
        {
            BinarizationMode = (QrBinarizationMode)4
        };
        QrDetectionOptions invalidPolarity = new QrDetectionOptions
        {
            Polarity = (QrPolarity)3
        };
        QrDetectionOptions supplied = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.SuppliedBinary
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => binarizer.CreateLuminanceImage(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => binarizer.CreateLuminanceImage(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => binarizer.BinarizeImage(valid, invalidMode),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => binarizer.BinarizeImage(valid, invalidPolarity),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => binarizer.BinarizeImage(color, supplied),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrImage CreateGrayImage(byte[] pixels, int width, int height, int stride)
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
