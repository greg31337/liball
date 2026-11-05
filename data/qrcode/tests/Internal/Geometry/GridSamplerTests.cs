using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class GridSamplerTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void RecoversRenderedSymbolModules(QrSymbolKind kind, bool multiPoint)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions
        {
            SymbolKind = kind,
            ForceSymbolKind = true,
            QuietZoneModules = 0
        });
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions
        {
            Scale = 4,
            IncludeQuietZone = true,
            QuietZoneModules = 2
        });
        QrMatrix result = Sample(raster, Affine(4, 4, 8, 8),
            symbol.Width, symbol.Height, multiPoint);

        Assert.That(result.Width, Is.EqualTo(symbol.Width));
        Assert.That(result.Height, Is.EqualTo(symbol.Height));
        Assert.That(result.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(result.Confidence, Is.All.EqualTo(255));
        Assert.That(result.Roles, Is.Null);
        Assert.That(result.QuietZoneLeft + result.QuietZoneTop
            + result.QuietZoneRight + result.QuietZoneBottom, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HonorsStrideAndOnePixelModulesWithoutMutatingOrSharingBuffers(bool multiPoint)
    {
        QrRaster raster = new QrRaster
        {
            Width = 3, Height = 2, Stride = 5,
            Pixels = [0, 255, 0, 42, 17, 255, 255, 0]
        };
        byte[] original = (byte[])raster.Pixels.Clone();
        PerspectiveTransform transform = Affine(1, 1, 0, 0);
        QrMatrix result = Sample(raster, transform, 3, 2, multiPoint);
        Assert.That(result.Values, Is.EqualTo(new[]
        {
            QrModuleValue.Dark, QrModuleValue.Light, QrModuleValue.Dark,
            QrModuleValue.Light, QrModuleValue.Light, QrModuleValue.Dark
        }));
        Assert.That(result.Confidence, Is.All.EqualTo(255));
        QrMatrix repeated = Sample(raster, transform, 3, 2, multiPoint);
        Assert.That(repeated.Values, Is.EqualTo(result.Values));
        Assert.That(repeated.Values, Is.Not.SameAs(result.Values));
        Assert.That(repeated.Confidence, Is.Not.SameAs(result.Confidence));
        result.Values[0] = QrModuleValue.Light;
        result.Confidence![0] = 0;
        Assert.That(raster.Pixels, Is.EqualTo(original));
        Assert.That(transform.M00, Is.EqualTo(1));
        Assert.That(transform.M02, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HandlesRectangularRotationAndMirroring(bool multiPoint)
    {
        QrRaster raster = new QrRaster
        {
            Width = 3, Height = 2, Stride = 3,
            Pixels = [0, 255, 255, 0, 0, 255]
        };
        QrMatrix rotated = Sample(raster, new PerspectiveTransform
        {
            M01 = -1, M02 = 3, M10 = 1, M22 = 1
        }, 2, 3, multiPoint);
        Assert.That(rotated.Values, Is.EqualTo(new[]
        {
            QrModuleValue.Light, QrModuleValue.Light,
            QrModuleValue.Light, QrModuleValue.Dark,
            QrModuleValue.Dark, QrModuleValue.Dark
        }));
        QrMatrix mirrored = Sample(raster, Affine(-1, 1, 3, 0), 3, 2, multiPoint);
        Assert.That(mirrored.Values, Is.EqualTo(new[]
        {
            QrModuleValue.Light, QrModuleValue.Light, QrModuleValue.Dark,
            QrModuleValue.Light, QrModuleValue.Dark, QrModuleValue.Dark
        }));
        Assert.That(rotated.Confidence, Is.All.EqualTo(255));
        Assert.That(mirrored.Confidence, Is.All.EqualTo(255));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SamplesPerspectiveDistortionAgainstAnAnalyticImage(bool multiPoint)
    {
        QrRaster raster = SolidRaster(60, 44, 255);
        for (int y = 0; y < raster.Height; y++)
        {
            for (int x = 0; x < raster.Width; x++)
            {
                // Inverse of image = (4,4) + 8 * (u,v) / (1 + 0.05*u).
                double denominator = 8 - 0.05 * (x + 0.5 - 4);
                double u = (x + 0.5 - 4) / denominator;
                double v = (y + 0.5 - 4) / denominator;
                if (u >= 0 && v >= 0 && u < 6 && v < 4)
                    raster.Pixels[y * raster.Stride + x] = ((int)u + (int)v) % 2 == 0
                        ? (byte)0 : (byte)255;
            }
        }
        PerspectiveTransform transform = new PerspectiveTransform
        {
            M00 = 8.2, M02 = 4, M10 = 0.2, M11 = 8, M12 = 4,
            M20 = 0.05, M22 = 1
        };
        QrMatrix result = Sample(raster, transform, 6, 4, multiPoint);
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 6; column++)
                Assert.That(result.Values[row * 6 + column], Is.EqualTo(
                    (row + column) % 2 == 0 ? QrModuleValue.Dark : QrModuleValue.Light));
        Assert.That(result.Confidence, Is.All.EqualTo(255));
    }

    [TestCase(0.0, 0.0, true)]
    [TestCase(1.999999, 1.999999, true)]
    [TestCase(-0.000001, 0.5, false)]
    [TestCase(0.5, -0.000001, false)]
    [TestCase(2.0, 0.5, false)]
    [TestCase(0.5, 2.0, false)]
    [TestCase(1E20, 0.5, false)]
    [TestCase(0.5, -1E20, false)]
    public void ChecksContinuousBoundsBeforeConvertingToPixelIndices(double x, double y, bool inside)
    {
        QrMatrix result = Sample(SolidRaster(2, 2, 0),
            Affine(1, 1, x - 0.5, y - 0.5), 1, 1, false);
        Assert.That(result.Values[0], Is.EqualTo(inside ? QrModuleValue.Dark : QrModuleValue.Unknown));
        Assert.That(result.Confidence![0], Is.EqualTo(inside ? 255 : 0));
    }

    [Test]
    public void LeavesCroppedModulesUnknownWithoutClampingToBorderPixels()
    {
        QrMatrix result = Sample(SolidRaster(1, 1, 0), Affine(1, 1, -1, -1), 3, 3, true);
        for (int index = 0; index < 9; index++)
        {
            Assert.That(result.Values[index], Is.EqualTo(index == 4
                ? QrModuleValue.Dark : QrModuleValue.Unknown));
            Assert.That(result.Confidence![index], Is.EqualTo(index == 4 ? 255 : 0));
        }
    }

    [TestCase(0, QrModuleValue.Light, 255)]
    [TestCase(1, QrModuleValue.Light, 153)]
    [TestCase(2, QrModuleValue.Light, 51)]
    [TestCase(3, QrModuleValue.Dark, 51)]
    [TestCase(4, QrModuleValue.Dark, 153)]
    [TestCase(5, QrModuleValue.Dark, 255)]
    public void CombinesFiveVotesWithConfidenceFromTheVoteMargin(
        int darkCount, QrModuleValue expected, int confidence)
    {
        QrRaster raster = SolidRaster(4, 4, 255);
        int[] offsets = [10, 9, 11, 6, 14];
        for (int index = 0; index < darkCount; index++)
            raster.Pixels[offsets[index]] = 0;
        QrMatrix result = Sample(raster, Affine(4, 4, 0, 0), 1, 1, true);
        Assert.That(result.Values[0], Is.EqualTo(expected));
        Assert.That(result.Confidence![0], Is.EqualTo(confidence));
    }

    [Test]
    public void MultiPointVotingRecoversAnIsolatedDamagedCenter()
    {
        QrRaster raster = SolidRaster(4, 4, 0);
        raster.Pixels[10] = 255;
        QrMatrix center = Sample(raster, Affine(4, 4, 0, 0), 1, 1, false);
        QrMatrix voted = Sample(raster, Affine(4, 4, 0, 0), 1, 1, true);
        Assert.That(center.Values[0], Is.EqualTo(QrModuleValue.Light));
        Assert.That(center.Confidence![0], Is.EqualTo(255));
        Assert.That(voted.Values[0], Is.EqualTo(QrModuleValue.Dark));
        Assert.That(voted.Confidence![0], Is.EqualTo(153));
    }

    [Test]
    public void MissingVotesReduceConfidenceAndTiesRemainUnknown()
    {
        QrRaster raster = SolidRaster(3, 3, 0);
        // Center=(0,1): the left vote is outside, leaving four observations.
        PerspectiveTransform transform = Affine(4, 4, -2, -1);
        QrMatrix unanimous = Sample(raster, transform, 1, 1, true);
        Assert.That(unanimous.Values[0], Is.EqualTo(QrModuleValue.Dark));
        Assert.That(unanimous.Confidence![0], Is.EqualTo(204));
        raster.Pixels[0] = 255;
        raster.Pixels[6] = 255;
        QrMatrix tied = Sample(raster, transform, 1, 1, true);
        Assert.That(tied.Values[0], Is.EqualTo(QrModuleValue.Unknown));
        Assert.That(tied.Confidence![0], Is.Zero);

        // Only the right vote reaches the raster; do not call that full confidence.
        QrMatrix partial = Sample(SolidRaster(3, 3, 0), Affine(4, 4, -2.5, -1), 1, 1, true);
        Assert.That(partial.Values[0], Is.EqualTo(QrModuleValue.Dark));
        Assert.That(partial.Confidence![0], Is.EqualTo(51));
    }

    [Test]
    public void AProjectiveHorizonProducesUnknownWithoutDiscardingOtherModules()
    {
        QrMatrix result = Sample(SolidRaster(3, 1, 255), new PerspectiveTransform
        {
            M00 = 1, M11 = 1, M20 = 1, M22 = -0.5
        }, 2, 1, false);
        Assert.That(result.Values, Is.EqualTo(new[] { QrModuleValue.Unknown, QrModuleValue.Light }));
        Assert.That(result.Confidence, Is.EqualTo(new byte[] { 0, 255 }));
    }

    [Test]
    public void RejectsInvalidRasterLayoutAndBinaryValues()
    {
        QrRaster[] invalid =
        [
            null!,
            new QrRaster { Width = 1, Height = 1, Stride = 1, Pixels = null! },
            new QrRaster { Width = 0, Height = 1, Stride = 1, Pixels = [0] },
            new QrRaster { Width = 1, Height = -1, Stride = 1, Pixels = [0] },
            new QrRaster { Width = 2, Height = 1, Stride = 1, Pixels = [0, 0] },
            new QrRaster { Width = 1, Height = 2, Stride = 3, Pixels = [0, 0, 0] },
            new QrRaster { Width = 1, Height = 1, Stride = 1, Pixels = [128] }
        ];
        foreach (QrRaster raster in invalid)
            Assert.Throws<QrInputException>(() => Sample(raster, Affine(1, 1, 0, 0), 1, 1, false));
    }

    [Test]
    public void RejectsInvalidTransformsAndOptions()
    {
        QrRaster raster = SolidRaster(1, 1, 0);
        foreach (PerspectiveTransform transform in new PerspectiveTransform[]
        {
            null!, new PerspectiveTransform(),
            new PerspectiveTransform { M00 = double.NaN, M11 = 1, M22 = 1 },
            new PerspectiveTransform { M00 = 1, M11 = 1, M22 = double.PositiveInfinity }
        })
            Assert.Throws<QrInputException>(() => Sample(raster, transform, 1, 1, false));
        Assert.Throws<QrInputException>(() => GridSampler.Sample(raster, Affine(1, 1, 0, 0), 1, 1, null!));
        Assert.Throws<QrInputException>(() => GridSampler.Sample(raster, Affine(1, 1, 0, 0), 1, 1,
            new QrDetectionOptions { MaximumImagePixels = 0 }));
    }

    [TestCase(0, 1)]
    [TestCase(1, 0)]
    [TestCase(-1, 1)]
    public void RejectsNonPositiveOutputDimensions(int width, int height)
    {
        Assert.Throws<QrInputException>(() => Sample(SolidRaster(1, 1, 0),
            Affine(1, 1, 0, 0), width, height, false));
    }

    [Test]
    public void EnforcesImageOutputAndAllocationLimitsBeforeSampling()
    {
        PerspectiveTransform transform = Affine(1, 1, 0, 0);
        QrRaster raster = SolidRaster(2, 2, 0);
        Assert.Throws<QrResourceLimitException>(() => GridSampler.Sample(raster, transform, 1, 1,
            new QrDetectionOptions { MaximumImageWidth = 1 }));
        Assert.Throws<QrResourceLimitException>(() => GridSampler.Sample(raster, transform, 1, 1,
            new QrDetectionOptions { MaximumImageHeight = 1 }));
        Assert.Throws<QrResourceLimitException>(() => GridSampler.Sample(raster, transform, 1, 1,
            new QrDetectionOptions { MaximumImagePixels = 3 }));
        Assert.Throws<QrResourceLimitException>(() => Sample(raster, transform, int.MaxValue, 2, false));
        Assert.Throws<QrResourceLimitException>(() => Sample(raster, transform, 2048, 2048, false));
        Assert.Throws<QrResourceLimitException>(() => GridSampler.Sample(raster, transform, int.MaxValue, 2,
            new QrDetectionOptions
            {
                MaximumImageWidth = int.MaxValue,
                MaximumImageHeight = int.MaxValue,
                MaximumImagePixels = int.MaxValue
            }));
    }

    static QrMatrix Sample(QrRaster raster, PerspectiveTransform transform, int width, int height, bool multiPoint)
    {
        return GridSampler.Sample(raster, transform, width, height,
            new QrDetectionOptions { UseMultiPointSampling = multiPoint });
    }

    static PerspectiveTransform Affine(double scaleX, double scaleY, double x, double y)
    {
        return new PerspectiveTransform { M00 = scaleX, M11 = scaleY, M02 = x, M12 = y, M22 = 1 };
    }

    static QrRaster SolidRaster(int width, int height, byte value)
    {
        byte[] pixels = new byte[width * height];
        Array.Fill(pixels, value);
        return new QrRaster { Width = width, Height = height, Stride = width, Pixels = pixels };
    }
}
