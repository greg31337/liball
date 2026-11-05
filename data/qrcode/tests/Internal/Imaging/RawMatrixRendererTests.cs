using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RawMatrixRendererTests
{
    [Test]
    public void RawRenderingUsesConfiguredGrayValuesAndTightStride()
    {
        QrSymbol symbol = CreateSymbol(
            QrSymbolKind.Model2,
            3,
            2,
            new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light
            });
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false,
            DarkValue = 17,
            LightValue = 231
        };

        QrRaster result = RawMatrixRenderer.RenderSymbol(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.Stride, Is.EqualTo(3));
            Assert.That(result.Pixels, Is.EqualTo(new byte[] { 17, 231, 17, 231, 17, 231 }));
        });
    }

    [TestCase(QrSymbolKind.Model2, 4)]
    [TestCase(QrSymbolKind.Micro, 2)]
    [TestCase(QrSymbolKind.rMQR, 2)]
    [TestCase(QrSymbolKind.LegacyModel1, 4)]
    public void ProfileDefaultAddsTheStandardQuietZone(
        QrSymbolKind symbolKind,
        int quietZone)
    {
        QrSymbol symbol = CreateSymbol(
            symbolKind,
            2,
            1,
            new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light });

        QrRaster result = RawMatrixRenderer.RenderSymbol(symbol, new QrRenderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(2 + quietZone * 2));
            Assert.That(result.Height, Is.EqualTo(1 + quietZone * 2));
            Assert.That(result.Stride, Is.EqualTo(result.Width));
            Assert.That(result.Pixels[0], Is.EqualTo(255));
            Assert.That(result.Pixels[quietZone * result.Stride + quietZone], Is.Zero);
            Assert.That(result.Pixels[quietZone * result.Stride + quietZone + 1], Is.EqualTo(255));
            Assert.That(result.Pixels[^1], Is.EqualTo(255));
        });
    }

    [Test]
    public void CustomQuietZoneAndScaleExpandEveryModuleExactly()
    {
        QrSymbol symbol = CreateSymbol(
            QrSymbolKind.Model2,
            2,
            1,
            new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light });
        QrRenderOptions options = new QrRenderOptions
        {
            Scale = 2,
            QuietZoneModules = 1,
            DarkValue = 10,
            LightValue = 240
        };

        QrRaster result = RawMatrixRenderer.RenderSymbol(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(8));
            Assert.That(result.Height, Is.EqualTo(6));
            Assert.That(result.Stride, Is.EqualTo(8));
            Assert.That(result.Pixels, Is.EqualTo(new byte[]
            {
                240, 240, 240, 240, 240, 240, 240, 240,
                240, 240, 240, 240, 240, 240, 240, 240,
                240, 240, 10, 10, 240, 240, 240, 240,
                240, 240, 10, 10, 240, 240, 240, 240,
                240, 240, 240, 240, 240, 240, 240, 240,
                240, 240, 240, 240, 240, 240, 240, 240
            }));
        });
    }

    [Test]
    public void InversionExchangesConfiguredDarkAndLightValuesIncludingTheQuietZone()
    {
        QrSymbol symbol = CreateSymbol(
            QrSymbolKind.Micro,
            2,
            1,
            new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light });
        QrRenderOptions options = new QrRenderOptions
        {
            QuietZoneModules = 1,
            DarkValue = 25,
            LightValue = 220,
            Invert = true
        };

        QrRaster result = RawMatrixRenderer.RenderSymbol(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Pixels[0], Is.EqualTo(25));
            Assert.That(result.Pixels[result.Stride + 1], Is.EqualTo(220));
            Assert.That(result.Pixels[result.Stride + 2], Is.EqualTo(25));
            Assert.That(result.Pixels[^1], Is.EqualTo(25));
        });
    }

    [Test]
    public void ExistingAsymmetricQuietZoneIsReplacedAndInputRemainsUnchanged()
    {
        QrModuleValue[] values = new QrModuleValue[]
        {
            QrModuleValue.Unknown, QrModuleValue.Unknown, QrModuleValue.Unknown, QrModuleValue.Unknown, QrModuleValue.Unknown,
            QrModuleValue.Unknown, QrModuleValue.Dark, QrModuleValue.Light, QrModuleValue.Unknown, QrModuleValue.Unknown,
            QrModuleValue.Unknown, QrModuleValue.Light, QrModuleValue.Dark, QrModuleValue.Unknown, QrModuleValue.Unknown,
            QrModuleValue.Unknown, QrModuleValue.Unknown, QrModuleValue.Unknown, QrModuleValue.Unknown, QrModuleValue.Unknown
        };
        QrMatrix matrix = new QrMatrix
        {
            Width = 5,
            Height = 4,
            Values = values,
            QuietZoneLeft = 1,
            QuietZoneTop = 1,
            QuietZoneRight = 2,
            QuietZoneBottom = 1
        };
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = QrSymbolKind.Model2,
            Width = 2,
            Height = 2,
            Matrix = matrix
        };
        QrModuleValue[] original = (QrModuleValue[])values.Clone();
        QrRenderOptions options = new QrRenderOptions
        {
            QuietZoneModules = 1,
            DarkValue = 3,
            LightValue = 250
        };

        QrRaster result = RawMatrixRenderer.RenderSymbol(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(4));
            Assert.That(result.Height, Is.EqualTo(4));
            Assert.That(result.Pixels, Is.EqualTo(new byte[]
            {
                250, 250, 250, 250,
                250, 3, 250, 250,
                250, 250, 3, 250,
                250, 250, 250, 250
            }));
            Assert.That(matrix.Values, Is.EqualTo(original));
            Assert.That(matrix.QuietZoneLeft, Is.EqualTo(1));
            Assert.That(matrix.QuietZoneRight, Is.EqualTo(2));
        });
    }

    [Test]
    public void InvalidInputsAndIncompleteEncodingValuesAreRejected()
    {
        QrSymbol valid = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        QrSymbol missingMatrix = new QrSymbol();
        QrSymbol unknownValue = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Unknown });
        QrSymbol invalidValue = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { (QrModuleValue)99 });
        QrSymbol shortValues = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        shortValues.Matrix!.Values = new QrModuleValue[0];
        QrRenderOptions invalidScale = new QrRenderOptions { Scale = 0 };
        QrRenderOptions invalidQuietZone = new QrRenderOptions { QuietZoneModules = -2 };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(null!, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(missingMatrix, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(unknownValue, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(invalidValue, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(shortValues, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(valid, invalidScale),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(valid, invalidQuietZone),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void InvalidProfileMatrixMetadataAndOversizedOutputAreRejected()
    {
        QrSymbol invalidProfile = CreateSymbol(
            (QrSymbolKind)99,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        QrSymbol badRoles = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        badRoles.Matrix!.Roles = new QrModuleRole[0];
        QrSymbol badConfidence = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        badConfidence.Matrix!.Confidence = new byte[0];
        QrSymbol negativeExtent = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        negativeExtent.Matrix!.QuietZoneLeft = -1;
        QrSymbol emptyEncoding = CreateSymbol(
            QrSymbolKind.Model2,
            1,
            1,
            new QrModuleValue[] { QrModuleValue.Dark });
        emptyEncoding.Matrix!.QuietZoneLeft = 1;
        QrRenderOptions oversizedScale = new QrRenderOptions
        {
            IncludeQuietZone = false,
            Scale = QrLimits.DefaultMaximumImageDimension + 1
        };
        QrRenderOptions oversizedQuietZone = new QrRenderOptions
        {
            QuietZoneModules = QrLimits.DefaultMaximumImageDimension
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(invalidProfile, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(badRoles, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(badConfidence, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(negativeExtent, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(emptyEncoding, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(
                    CreateSymbol(
                        QrSymbolKind.Model2,
                        1,
                        1,
                        new QrModuleValue[] { QrModuleValue.Dark }),
                    oversizedScale),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(
                () => RawMatrixRenderer.RenderSymbol(
                    CreateSymbol(
                        QrSymbolKind.Model2,
                        1,
                        1,
                        new QrModuleValue[] { QrModuleValue.Dark }),
                    oversizedQuietZone),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrSymbol CreateSymbol(
        QrSymbolKind symbolKind,
        int width,
        int height,
        QrModuleValue[] values)
    {
        return new QrSymbol
        {
            SymbolKind = symbolKind,
            Width = width,
            Height = height,
            Matrix = new QrMatrix
            {
                Width = width,
                Height = height,
                Values = values
            }
        };
    }
}
