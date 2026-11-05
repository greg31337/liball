using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class FinderClustererTests
{
    [Test]
    public void MergesRowAndColumnEvidenceIntoOneIndependentCandidate()
    {
        QrRaster raster = CreateFinderRaster(24, 24, 4, 4, 2);
        FinderCandidate[] scanned = RunRatioScanner.Scan(
            raster,
            1,
            new QrDetectionOptions());
        FinderCandidate[] validated = FinderRegionValidator.Validate(
            raster,
            scanned,
            new QrDetectionOptions());

        FinderCandidate[] merged = FinderClusterer.Merge(
            validated,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(merged, Has.Length.EqualTo(1));
            Assert.That(merged[0], Is.Not.SameAs(validated[0]));
            Assert.That(merged[0].CenterX, Is.EqualTo(11.0).Within(0.01));
            Assert.That(merged[0].CenterY, Is.EqualTo(11.0).Within(0.01));
            Assert.That(merged[0].ModuleSize, Is.EqualTo(2.0).Within(0.01));
            Assert.That(merged[0].EvidenceCount, Is.EqualTo(validated.Length));
            Assert.That(merged[0].HorizontalEvidenceCount, Is.GreaterThan(0));
            Assert.That(merged[0].VerticalEvidenceCount, Is.GreaterThan(0));
            Assert.That(merged[0].RegionValidated, Is.True);
            Assert.That(merged[0].FinderCorners, Is.Not.Null);
        });
    }

    [Test]
    public void MergesEquivalentSourceGeometryAcrossPyramidScales()
    {
        QrRaster fullRaster = CreateFinderRaster(24, 24, 4, 4, 2);
        QrRaster reducedRaster = CreateFinderRaster(12, 12, 2, 2, 1);
        FinderCandidate[] full = ScanAndValidate(fullRaster, 1);
        FinderCandidate[] reduced = ScanAndValidate(reducedRaster, 2);
        FinderCandidate[] combined = Combine(full, reduced);

        FinderCandidate[] merged = FinderClusterer.Merge(
            combined,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(merged, Has.Length.EqualTo(1));
            Assert.That(merged[0].CenterX, Is.EqualTo(11.0).Within(0.01));
            Assert.That(merged[0].CenterY, Is.EqualTo(11.0).Within(0.01));
            Assert.That(merged[0].MinimumScaleToSource, Is.EqualTo(1));
            Assert.That(merged[0].MaximumScaleToSource, Is.EqualTo(2));
            Assert.That(merged[0].EvidenceCount, Is.EqualTo(combined.Length));
            Assert.That(merged[0].FinderCorners!.TopLeft!.X, Is.EqualTo(4.0).Within(0.01));
            Assert.That(merged[0].FinderCorners!.BottomRight!.X, Is.EqualTo(18.0).Within(0.01));
        });
    }

    [Test]
    public void UsesFinestAvailableCornersWhileRetainingEvidenceFromEveryScale(
        [Values(1, 2)] int finestScale, [Values(9.9, 10.1)] double coarseCenterY)
    {
        // Put the coarse observation before or after the fine observations in
        // geometric order. Its stronger evidence must not skew their corners.
        FinderCandidate fine = CreateValidatedCandidate(finestScale, 10, 3, 3, 0.5, 1);
        FinderCandidate fineShifted = CreateValidatedCandidate(finestScale, 10, 4, 4, 0.75, 2);
        FinderCandidate coarse = CreateValidatedCandidate(finestScale * 2, coarseCenterY, 6, 0, 0.95, 10);

        FinderCandidate[] merged = FinderClusterer.Merge(
            [coarse, fineShifted, fine], new QrDetectionOptions());

        Assert.That(merged, Has.Length.EqualTo(1));
        QrQuadrilateral corners = merged[0].FinderCorners!;
        Assert.Multiple(() =>
        {
            Assert.That(corners.TopLeft!.X, Is.EqualTo(3.75).Within(1e-10));
            Assert.That(corners.TopLeft.Y, Is.EqualTo(3.75).Within(1e-10));
            Assert.That(corners.TopRight!.X, Is.EqualTo(17.75).Within(1e-10));
            Assert.That(corners.TopRight.Y, Is.EqualTo(3.75).Within(1e-10));
            Assert.That(corners.BottomRight!.X, Is.EqualTo(17.75).Within(1e-10));
            Assert.That(corners.BottomRight.Y, Is.EqualTo(17.75).Within(1e-10));
            Assert.That(corners.BottomLeft!.X, Is.EqualTo(3.75).Within(1e-10));
            Assert.That(corners.BottomLeft.Y, Is.EqualTo(17.75).Within(1e-10));
            Assert.That(merged[0].EvidenceCount, Is.EqualTo(13));
            Assert.That(merged[0].MinimumScaleToSource, Is.EqualTo(finestScale));
            Assert.That(merged[0].MaximumScaleToSource, Is.EqualTo(finestScale * 2));
            Assert.That(merged[0].Confidence, Is.EqualTo(11.5 / 13).Within(1e-10));
        });

        corners.TopLeft!.X = -100;
        Assert.That(fine.FinderCorners!.TopLeft!.X, Is.EqualTo(3));
        Assert.That(fineShifted.FinderCorners!.TopLeft!.X, Is.EqualTo(4));
        Assert.That(coarse.FinderCorners!.TopLeft!.X, Is.EqualTo(6));
    }

    [Test]
    public void MergesPolarityPassesAndRetainsStrongestExplicitPolarity()
    {
        FinderCandidate dark = CreateCandidate(10, 10, 2, 0.9);
        dark.Polarity = QrPolarity.DarkOnLight;
        FinderCandidate light = CreateCandidate(10.2, 9.9, 2.1, 0.7);
        light.Polarity = QrPolarity.LightOnDark;

        FinderCandidate[] merged = FinderClusterer.Merge(
            new FinderCandidate[] { light, dark },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(merged, Has.Length.EqualTo(1));
            Assert.That(merged[0].Polarity, Is.EqualTo(QrPolarity.DarkOnLight));
            Assert.That(merged[0].MixedPolarityEvidence, Is.True);
            Assert.That(merged[0].EvidenceCount, Is.EqualTo(2));
            Assert.That(merged[0].CenterX, Is.GreaterThan(10.0));
            Assert.That(merged[0].CenterX, Is.LessThan(10.2));
        });
    }

    [Test]
    public void KeepsDistinctFindersSeparateAndOrdersThemDeterministically()
    {
        FinderCandidate lowerConfidence = CreateCandidate(30, 8, 2, 0.6);
        FinderCandidate higherConfidence = CreateCandidate(8, 8, 2, 0.9);

        FinderCandidate[] first = FinderClusterer.Merge(
            new FinderCandidate[] { lowerConfidence, higherConfidence },
            new QrDetectionOptions());
        FinderCandidate[] second = FinderClusterer.Merge(
            new FinderCandidate[] { higherConfidence, lowerConfidence },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(2));
            Assert.That(first[0].CenterX, Is.EqualTo(8));
            Assert.That(first[1].CenterX, Is.EqualTo(30));
            Assert.That(second[0].CenterX, Is.EqualTo(first[0].CenterX));
            Assert.That(second[1].CenterX, Is.EqualTo(first[1].CenterX));
        });
    }

    [Test]
    public void RanksAndTruncatesClustersAtTheConfiguredLimit()
    {
        FinderCandidate low = CreateCandidate(5, 5, 1, 0.2);
        FinderCandidate high = CreateCandidate(20, 5, 1, 0.9);
        FinderCandidate medium = CreateCandidate(35, 5, 1, 0.7);
        QrDetectionOptions options = new QrDetectionOptions
        {
            MaximumCandidates = 2
        };

        FinderCandidate[] merged = FinderClusterer.Merge(
            new FinderCandidate[] { low, high, medium },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(merged, Has.Length.EqualTo(2));
            Assert.That(merged[0].Confidence, Is.EqualTo(0.9));
            Assert.That(merged[1].Confidence, Is.EqualTo(0.7));
        });
    }

    [Test]
    public void RejectsInvalidCandidatesOptionsAndCombinedPassBounds()
    {
        FinderCandidate valid = CreateCandidate(5, 5, 1, 0.8);
        FinderCandidate invalid = CreateCandidate(5, 5, 1, 0.8);
        invalid.Confidence = double.NaN;
        FinderCandidate invalidPolarity = CreateCandidate(5, 5, 1, 0.8);
        invalidPolarity.Polarity = (QrPolarity)3;
        FinderCandidate invalidRegion = CreateCandidate(5, 5, 1, 0.8);
        invalidRegion.RegionValidated = true;
        invalidRegion.RegionConfidence = 0.9;
        invalidRegion.CenterRegionArea = 9;
        invalidRegion.OuterRegionArea = 24;
        QrDetectionOptions noCandidates = new QrDetectionOptions
        {
            MaximumCandidates = 0
        };
        QrDetectionOptions noLevels = new QrDetectionOptions
        {
            MaximumPyramidLevels = 0
        };
        QrDetectionOptions bounded = new QrDetectionOptions
        {
            MaximumCandidates = 1,
            MaximumPyramidLevels = 1
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => FinderClusterer.Merge(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[0], null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[] { valid }, noCandidates),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[] { valid }, noLevels),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[] { null! }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[] { invalid }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[] { invalidPolarity }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(new FinderCandidate[] { invalidRegion }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderClusterer.Merge(
                new FinderCandidate[] { valid, valid, valid },
                bounded), Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static FinderCandidate CreateValidatedCandidate(
        int scale, double centerY, double left, double top, double confidence, int evidence)
    {
        FinderCandidate candidate = CreateCandidate(10, centerY, 2, confidence);
        candidate.ScaleToSource = scale;
        candidate.EvidenceCount = evidence;
        candidate.RegionValidated = true;
        candidate.RegionConfidence = confidence;
        candidate.CenterRegionArea = 36;
        candidate.OuterRegionArea = 96;
        candidate.FinderCorners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = left, Y = top },
            TopRight = new QrPoint { X = left + 14, Y = top },
            BottomRight = new QrPoint { X = left + 14, Y = top + 14 },
            BottomLeft = new QrPoint { X = left, Y = top + 14 }
        };
        return candidate;
    }

    static FinderCandidate[] ScanAndValidate(QrRaster raster, int scale)
    {
        FinderCandidate[] scanned = RunRatioScanner.Scan(
            raster,
            scale,
            new QrDetectionOptions());
        return FinderRegionValidator.Validate(
            raster,
            scanned,
            new QrDetectionOptions());
    }

    static FinderCandidate[] Combine(FinderCandidate[] first, FinderCandidate[] second)
    {
        FinderCandidate[] combined = new FinderCandidate[first.Length + second.Length];
        for (int index = 0; index < first.Length; index++)
        {
            combined[index] = first[index];
        }

        for (int index = 0; index < second.Length; index++)
        {
            combined[first.Length + index] = second[index];
        }

        return combined;
    }

    static FinderCandidate CreateCandidate(
        double centerX,
        double centerY,
        double moduleSize,
        double confidence)
    {
        return new FinderCandidate
        {
            CenterX = centerX,
            CenterY = centerY,
            ModuleSize = moduleSize,
            Confidence = confidence
        };
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
