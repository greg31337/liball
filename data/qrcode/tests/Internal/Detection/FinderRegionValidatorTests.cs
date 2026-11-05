using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class FinderRegionValidatorTests
{
    [Test]
    public void ValidatesSeparatedCenterAndCapstoneAndRecoversSourceCorners()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);
        FinderCandidate[] scanned = RunRatioScanner.Scan(
            raster,
            2,
            new QrDetectionOptions());

        FinderCandidate[] validated = FinderRegionValidator.Validate(
            raster,
            scanned,
            new QrDetectionOptions());
        FinderCandidate closest = FindClosest(validated, 22.0, 22.0);

        Assert.Multiple(() =>
        {
            Assert.That(validated, Is.Not.Empty);
            Assert.That(closest.RegionValidated, Is.True);
            Assert.That(closest.RegionConfidence, Is.EqualTo(1.0).Within(0.01));
            Assert.That(closest.CenterRegionArea, Is.EqualTo(36));
            Assert.That(closest.OuterRegionArea, Is.EqualTo(96));
            Assert.That(closest.FinderCorners, Is.Not.Null);
            Assert.That(closest.FinderCorners!.TopLeft!.X, Is.EqualTo(8.0));
            Assert.That(closest.FinderCorners.TopLeft.Y, Is.EqualTo(8.0));
            Assert.That(closest.FinderCorners.TopRight!.X, Is.EqualTo(36.0));
            Assert.That(closest.FinderCorners.TopRight.Y, Is.EqualTo(8.0));
            Assert.That(closest.FinderCorners.BottomRight!.X, Is.EqualTo(36.0));
            Assert.That(closest.FinderCorners.BottomRight.Y, Is.EqualTo(36.0));
            Assert.That(closest.FinderCorners.BottomLeft!.X, Is.EqualTo(8.0));
            Assert.That(closest.FinderCorners.BottomLeft.Y, Is.EqualTo(36.0));
        });
    }

    [Test]
    public void RejectsCapstoneConnectedToAnUnboundedDarkRegion()
    {
        QrRaster raster = CreateFinderRaster(32, 32, 8, 8, 2);
        for (int row = 0; row <= 8; row++)
        {
            raster.Pixels[row * raster.Stride + 12] = 0;
        }

        FinderCandidate[] scanned = RunRatioScanner.Scan(
            raster,
            1,
            new QrDetectionOptions());
        FinderCandidate[] validated = FinderRegionValidator.Validate(
            raster,
            scanned,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(scanned, Is.Not.Empty);
            Assert.That(validated, Is.Empty);
        });
    }

    [Test]
    public void RejectsCenterRegionBridgedToOuterRing()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);
        FinderCandidate[] scanned = RunRatioScanner.Scan(
            raster,
            1,
            new QrDetectionOptions());
        raster.Pixels[11 * raster.Stride + 6] = 0;
        raster.Pixels[11 * raster.Stride + 7] = 0;

        FinderCandidate[] validated = FinderRegionValidator.Validate(
            raster,
            scanned,
            new QrDetectionOptions());

        Assert.That(validated, Is.Empty);
    }

    [Test]
    public void DirectionalExtremaRecoverADistortedCapstoneCorner()
    {
        QrRaster raster = CreateFinderRaster(30, 30, 6, 6, 2);
        FinderCandidate[] scanned = RunRatioScanner.Scan(
            raster,
            1,
            new QrDetectionOptions());
        raster.Pixels[6 * raster.Stride + 20] = 0;
        raster.Pixels[6 * raster.Stride + 21] = 0;
        raster.Pixels[5 * raster.Stride + 21] = 0;

        FinderCandidate[] validated = FinderRegionValidator.Validate(
            raster,
            scanned,
            new QrDetectionOptions());
        FinderCandidate closest = FindClosest(validated, 13.0, 13.0);

        Assert.Multiple(() =>
        {
            Assert.That(validated, Is.Not.Empty);
            Assert.That(closest.FinderCorners!.TopLeft!.X, Is.EqualTo(6.0));
            Assert.That(closest.FinderCorners.TopLeft.Y, Is.EqualTo(6.0));
            Assert.That(closest.FinderCorners.TopRight!.X, Is.EqualTo(22.0));
            Assert.That(closest.FinderCorners.TopRight.Y, Is.EqualTo(5.0));
            Assert.That(closest.RegionConfidence, Is.LessThan(1.0));
            Assert.That(closest.RegionConfidence, Is.GreaterThan(0.75));
        });
    }

    [Test]
    public void RejectsInvalidRasterCandidateArraysAndGeometry()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);
        FinderCandidate invalidScale = new FinderCandidate
        {
            CenterX = 11,
            CenterY = 11,
            ModuleSize = 2,
            ScaleToSource = 0
        };
        FinderCandidate outside = new FinderCandidate
        {
            CenterX = 100,
            CenterY = 100,
            ModuleSize = 2
        };
        FinderCandidate nonFinite = new FinderCandidate
        {
            CenterX = double.NaN,
            CenterY = 11,
            ModuleSize = 2
        };
        QrDetectionOptions oneCandidate = new QrDetectionOptions
        {
            MaximumCandidates = 1
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => FinderRegionValidator.Validate(
                null!,
                new FinderCandidate[0],
                new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderRegionValidator.Validate(
                raster,
                null!,
                new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderRegionValidator.Validate(
                raster,
                new FinderCandidate[] { null! },
                new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderRegionValidator.Validate(
                raster,
                new FinderCandidate[] { invalidScale },
                new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderRegionValidator.Validate(
                raster,
                new FinderCandidate[] { nonFinite },
                new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderRegionValidator.Validate(
                raster,
                new FinderCandidate[] { outside, outside },
                oneCandidate), Throws.TypeOf<QrResourceLimitException>());
            Assert.That(FinderRegionValidator.Validate(
                raster,
                new FinderCandidate[] { outside },
                new QrDetectionOptions()), Is.Empty);
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

    static QrRaster CreateFinderRaster(
        int width,
        int height,
        int left,
        int top,
        int moduleSize)
    {
        byte[] pixels = new byte[width * height];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = 255;
        }

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
                            pixels[row * width + column] = 0;
                        }
                    }
                }
            }
        }

        return new QrRaster
        {
            Pixels = pixels,
            Width = width,
            Height = height,
            Stride = width
        };
    }
}
