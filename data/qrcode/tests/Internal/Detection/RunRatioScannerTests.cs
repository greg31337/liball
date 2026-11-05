using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RunRatioScannerTests
{
    [Test]
    public void FindsIdealFinderFromRowsAndColumnsWithSubpixelEvidence()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);

        FinderCandidate[] candidates = RunRatioScanner.Scan(
            raster,
            1,
            new QrDetectionOptions());
        FinderCandidate closest = FindClosest(candidates, 11.0, 11.0);

        Assert.Multiple(() =>
        {
            Assert.That(candidates.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(HasScanDirection(candidates, false), Is.True);
            Assert.That(HasScanDirection(candidates, true), Is.True);
            Assert.That(closest.CenterX, Is.EqualTo(11.0).Within(0.01));
            Assert.That(closest.CenterY, Is.EqualTo(11.0).Within(0.01));
            Assert.That(closest.ModuleSize, Is.EqualTo(2.0).Within(0.01));
            Assert.That(closest.Confidence, Is.GreaterThan(0.95));
            Assert.That(closest.Confidence, Is.LessThanOrEqualTo(1.0));
        });
    }

    [Test]
    public void RejectsAHorizontalRatioWithoutCrossOrDiagonalEvidence()
    {
        int width = 24;
        int height = 24;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        int column = 4;
        int[] lengths = new int[] { 2, 2, 6, 2, 2 };
        bool dark = true;
        for (int run = 0; run < lengths.Length; run++)
        {
            for (int offset = 0; offset < lengths[run]; offset++)
            {
                pixels[11 * width + column] = dark ? (byte)0 : (byte)255;
                column++;
            }

            dark = !dark;
        }

        FinderCandidate[] candidates = RunRatioScanner.Scan(
            CreateRaster(pixels, width, height, width),
            1,
            new QrDetectionOptions());

        Assert.That(candidates, Is.Empty);
    }

    [Test]
    public void MapsCentersAndModuleSizesBackToSourceScale()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);

        FinderCandidate[] candidates = RunRatioScanner.Scan(
            raster,
            4,
            new QrDetectionOptions());
        FinderCandidate closest = FindClosest(candidates, 44.0, 44.0);

        Assert.Multiple(() =>
        {
            Assert.That(closest.CenterX, Is.EqualTo(44.0).Within(0.01));
            Assert.That(closest.CenterY, Is.EqualTo(44.0).Within(0.01));
            Assert.That(closest.ModuleSize, Is.EqualTo(8.0).Within(0.01));
            Assert.That(closest.ScaleToSource, Is.EqualTo(4));
        });
    }

    [Test]
    public void StopsDeterministicallyAtConfiguredCandidateLimit()
    {
        int width = 48;
        int height = 24;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        DrawFinder(pixels, width, 4, 4, 2);
        DrawFinder(pixels, width, 28, 4, 2);
        QrDetectionOptions options = new QrDetectionOptions
        {
            MaximumCandidates = 1
        };

        FinderCandidate[] candidates = RunRatioScanner.Scan(
            CreateRaster(pixels, width, height, width),
            1,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Length.EqualTo(1));
            Assert.That(candidates[0].CenterX, Is.EqualTo(11.0).Within(0.01));
        });
    }

    [Test]
    public void AppliesMinimumModuleSizeInSourceCoordinates()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);
        QrDetectionOptions options = new QrDetectionOptions
        {
            MinimumModuleSize = 3
        };

        FinderCandidate[] originalScale = RunRatioScanner.Scan(raster, 1, options);
        FinderCandidate[] reducedScale = RunRatioScanner.Scan(raster, 2, options);

        Assert.Multiple(() =>
        {
            Assert.That(originalScale, Is.Empty);
            Assert.That(reducedScale, Is.Not.Empty);
            Assert.That(reducedScale[0].ModuleSize, Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public void ToleratesModerateRunVariationAndReportsReducedConfidence()
    {
        int width = 25;
        int height = 25;
        byte[] pixels = CreateFilledPixels(width, height, 255);
        DrawFinder(pixels, width, 4, 4, 2);
        pixels[10 * width + 4] = 255;

        FinderCandidate[] candidates = RunRatioScanner.Scan(
            CreateRaster(pixels, width, height, width),
            1,
            new QrDetectionOptions());
        FinderCandidate closest = FindClosest(candidates, 11.0, 11.0);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Is.Not.Empty);
            Assert.That(closest.Confidence, Is.GreaterThan(0.5));
            Assert.That(closest.Confidence, Is.LessThanOrEqualTo(1.0));
        });
    }

    [Test]
    public void RejectsInvalidRasterScaleAndCandidateLimits()
    {
        QrRaster valid = CreateFinderRaster(24, 24, 4, 4, 2);
        QrRaster nonBinary = CreateRaster(new byte[] { 0, 127 }, 2, 1, 2);
        QrRaster shortBuffer = CreateRaster(new byte[5], 2, 2, 4);
        QrDetectionOptions noCandidates = new QrDetectionOptions
        {
            MaximumCandidates = 0
        };
        QrDetectionOptions invalidModuleSize = new QrDetectionOptions
        {
            MinimumModuleSize = 0
        };
        QrDetectionOptions areaLimit = new QrDetectionOptions
        {
            MaximumImagePixels = 100
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => RunRatioScanner.Scan(null!, 1, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(valid, 1, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(valid, 0, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(valid, 1, noCandidates),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(valid, 1, invalidModuleSize),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(nonBinary, 1, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(shortBuffer, 1, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RunRatioScanner.Scan(valid, 1, areaLimit),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static FinderCandidate FindClosest(
        FinderCandidate[] candidates,
        double centerX,
        double centerY)
    {
        Assert.That(candidates, Is.Not.Empty);
        FinderCandidate closest = candidates[0];
        double closestDistance = double.MaxValue;
        for (int index = 0; index < candidates.Length; index++)
        {
            double horizontal = candidates[index].CenterX - centerX;
            double vertical = candidates[index].CenterY - centerY;
            double distance = horizontal * horizontal + vertical * vertical;
            if (distance < closestDistance)
            {
                closest = candidates[index];
                closestDistance = distance;
            }
        }

        return closest;
    }

    static bool HasScanDirection(FinderCandidate[] candidates, bool vertical)
    {
        for (int index = 0; index < candidates.Length; index++)
        {
            if (candidates[index].ScannedVertically == vertical)
            {
                return true;
            }
        }

        return false;
    }

    static QrRaster CreateFinderRaster(
        int width,
        int height,
        int left,
        int top,
        int moduleSize)
    {
        byte[] pixels = CreateFilledPixels(width, height, 255);
        DrawFinder(pixels, width, left, top, moduleSize);
        return CreateRaster(pixels, width, height, width);
    }

    static void DrawFinder(
        byte[] pixels,
        int stride,
        int left,
        int top,
        int moduleSize)
    {
        for (int moduleRow = 0; moduleRow < 7; moduleRow++)
        {
            for (int moduleColumn = 0; moduleColumn < 7; moduleColumn++)
            {
                bool dark = moduleRow == 0
                    || moduleRow == 6
                    || moduleColumn == 0
                    || moduleColumn == 6
                    || (moduleRow >= 2
                        && moduleRow <= 4
                        && moduleColumn >= 2
                        && moduleColumn <= 4);
                if (dark)
                {
                    for (int rowOffset = 0; rowOffset < moduleSize; rowOffset++)
                    {
                        for (int columnOffset = 0;
                            columnOffset < moduleSize;
                            columnOffset++)
                        {
                            int row = top + moduleRow * moduleSize + rowOffset;
                            int column = left + moduleColumn * moduleSize + columnOffset;
                            pixels[row * stride + column] = 0;
                        }
                    }
                }
            }
        }
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
