using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RectangularMicroFinderDetectorTests
{
    [Test]
    public void DetectsAllThirtyTwoSizesAtBothErrorCorrectionLevels()
    {
        for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
            versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
            versionIndex++)
        {
            for (int errorCorrectionIndex = RectangularMicroVersionTable.Medium;
                errorCorrectionIndex <= RectangularMicroVersionTable.High;
                errorCorrectionIndex++)
            {
                QrRaster raster = CreateRaster(
                    versionIndex,
                    errorCorrectionIndex,
                    0,
                    1,
                    2);
                FinderCandidate finder = CreateFinder(
                    versionIndex,
                    0,
                    1,
                    2,
                    0.9);

                RectangularMicroFinderDetection[] detections =
                    RectangularMicroFinderDetector.Detect(
                        raster,
                        new FinderCandidate[] { finder },
                        new QrDetectionOptions());

                Assert.That(
                    detections,
                    Has.Length.EqualTo(1),
                    $"{RectangularMicroVersionTable.GetVersionName(versionIndex)} level {errorCorrectionIndex}");
                Assert.That(detections[0].VersionIndex, Is.EqualTo(versionIndex));
                Assert.That(
                    detections[0].ErrorCorrectionIndex,
                    Is.EqualTo(errorCorrectionIndex));
            }
        }
    }

    [TestCase(0, RectangularMicroVersionTable.Medium)]
    [TestCase(5, RectangularMicroVersionTable.High)]
    [TestCase(10, RectangularMicroVersionTable.Medium)]
    [TestCase(16, RectangularMicroVersionTable.High)]
    [TestCase(22, RectangularMicroVersionTable.Medium)]
    [TestCase(27, RectangularMicroVersionTable.High)]
    [TestCase(31, RectangularMicroVersionTable.High)]
    public void DetectsRepresentativeStandardizedSizes(
        int versionIndex,
        int errorCorrectionIndex)
    {
        int moduleSize = 2;
        int quietZone = 2;
        QrRaster raster = CreateRaster(
            versionIndex,
            errorCorrectionIndex,
            0,
            moduleSize,
            quietZone);
        FinderCandidate finder = CreateFinder(
            versionIndex,
            0,
            moduleSize,
            quietZone,
            0.94);

        RectangularMicroFinderDetection[] detections =
            RectangularMicroFinderDetector.Detect(
                raster,
                new FinderCandidate[] { finder },
                new QrDetectionOptions());

        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].Finder, Is.SameAs(finder));
            Assert.That(detections[0].VersionIndex, Is.EqualTo(versionIndex));
            Assert.That(detections[0].Width, Is.EqualTo(width));
            Assert.That(detections[0].Height, Is.EqualTo(height));
            Assert.That(
                detections[0].ErrorCorrectionIndex,
                Is.EqualTo(errorCorrectionIndex));
            Assert.That(detections[0].QuarterTurnsClockwise, Is.Zero);
            Assert.That(detections[0].SubfinderConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].FormatConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].FormatCopiesAgree, Is.True);
            Assert.That(detections[0].FinderFormatDistance, Is.Zero);
            Assert.That(detections[0].SubfinderFormatDistance, Is.Zero);
            Assert.That(detections[0].Confidence, Is.GreaterThan(0.95));
            Assert.That(
                detections[0].SubfinderCenter.X,
                Is.EqualTo((quietZone + width - 2.5) * moduleSize).Within(0.001));
            Assert.That(
                detections[0].SubfinderCenter.Y,
                Is.EqualTo((quietZone + height - 2.5) * moduleSize).Within(0.001));
            Assert.That(
                detections[0].SymbolCorners.TopLeft!.X,
                Is.EqualTo(quietZone * moduleSize).Within(0.001));
            Assert.That(
                detections[0].SymbolCorners.BottomRight!.X,
                Is.EqualTo((quietZone + width) * moduleSize).Within(0.001));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void DiscriminatesEveryQuarterTurn(int quarterTurns)
    {
        int versionIndex = 18;
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster raster = CreateRaster(
            versionIndex,
            RectangularMicroVersionTable.High,
            quarterTurns,
            moduleSize,
            quietZone);
        FinderCandidate finder = CreateFinder(
            versionIndex,
            quarterTurns,
            moduleSize,
            quietZone,
            0.91);

        RectangularMicroFinderDetection[] detections =
            RectangularMicroFinderDetector.Detect(
                raster,
                new FinderCandidate[] { finder },
                new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].VersionIndex, Is.EqualTo(versionIndex));
            Assert.That(detections[0].QuarterTurnsClockwise, Is.EqualTo(quarterTurns));
            Assert.That(detections[0].SubfinderConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].FormatCopiesAgree, Is.True);
        });
    }

    [Test]
    public void CorrectsIndependentDamageInBothFormatCopies()
    {
        int versionIndex = 17;
        int errorCorrectionIndex = RectangularMicroVersionTable.Medium;
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster raster = CreateRaster(
            versionIndex,
            errorCorrectionIndex,
            0,
            moduleSize,
            quietZone);
        for (int bitIndex = 0; bitIndex < 2; bitIndex++)
        {
            ToggleLogicalModule(
                raster,
                versionIndex,
                0,
                moduleSize,
                quietZone,
                RectangularMicroPlacementTable.GetFinderSideFormatRow(bitIndex),
                RectangularMicroPlacementTable.GetFinderSideFormatColumn(bitIndex));
        }

        for (int bitIndex = 0; bitIndex < 3; bitIndex++)
        {
            ToggleLogicalModule(
                raster,
                versionIndex,
                0,
                moduleSize,
                quietZone,
                RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                    versionIndex,
                    bitIndex),
                RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                    versionIndex,
                    bitIndex));
        }

        RectangularMicroFinderDetection[] detections =
            RectangularMicroFinderDetector.Detect(
                raster,
                new FinderCandidate[]
                {
                    CreateFinder(versionIndex, 0, moduleSize, quietZone, 0.9)
                },
                new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].VersionIndex, Is.EqualTo(versionIndex));
            Assert.That(detections[0].FinderFormatDistance, Is.EqualTo(2));
            Assert.That(detections[0].SubfinderFormatDistance, Is.EqualTo(3));
            Assert.That(detections[0].FormatCopiesAgree, Is.True);
            Assert.That(detections[0].FormatConfidence, Is.EqualTo(0.375));
        });
    }

    [Test]
    public void LocallyGroupsAShiftedSubfinderAndItsFormatEvidence()
    {
        int versionIndex = 20;
        int moduleSize = 4;
        int quietZone = 2;
        QrRaster raster = CreateRaster(
            versionIndex,
            RectangularMicroVersionTable.High,
            0,
            moduleSize,
            quietZone);
        ShiftSubfinderEvidence(
            raster,
            versionIndex,
            moduleSize,
            quietZone,
            3,
            3);

        RectangularMicroFinderDetection[] detections =
            RectangularMicroFinderDetector.Detect(
                raster,
                new FinderCandidate[]
                {
                    CreateFinder(versionIndex, 0, moduleSize, quietZone, 0.9)
                },
                new QrDetectionOptions());

        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].SubfinderConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].FormatCopiesAgree, Is.True);
            Assert.That(
                detections[0].SubfinderCenter.X,
                Is.GreaterThan((quietZone + width - 2.5) * moduleSize));
            Assert.That(
                detections[0].SubfinderCenter.Y,
                Is.GreaterThan((quietZone + height - 2.5) * moduleSize));
            Assert.That(
                detections[0].SubfinderCenter.X,
                Is.LessThanOrEqualTo((quietZone + width - 2.5) * moduleSize + 3));
            Assert.That(
                detections[0].SubfinderCenter.Y,
                Is.LessThanOrEqualTo((quietZone + height - 2.5) * moduleSize + 3));
        });
    }

    [Test]
    public void RejectsMissingSubfinderAndConflictingFormatCopies()
    {
        int versionIndex = 19;
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster missing = CreateRaster(
            versionIndex,
            RectangularMicroVersionTable.Medium,
            0,
            moduleSize,
            quietZone);
        EraseSubfinder(missing, versionIndex, moduleSize, quietZone);
        QrRaster conflicting = CreateRaster(
            versionIndex,
            RectangularMicroVersionTable.Medium,
            0,
            moduleSize,
            quietZone);
        WriteSubfinderFormat(
            conflicting,
            versionIndex,
            moduleSize,
            quietZone,
            FormatInformationCodec.CreateRectangularMicroSubfinderCode(
                versionIndex + 1,
                RectangularMicroVersionTable.Medium));
        FinderCandidate finder = CreateFinder(
            versionIndex,
            0,
            moduleSize,
            quietZone,
            0.9);

        RectangularMicroFinderDetection[] missingResult =
            RectangularMicroFinderDetector.Detect(
                missing,
                new FinderCandidate[] { finder },
                new QrDetectionOptions());
        RectangularMicroFinderDetection[] conflictingResult =
            RectangularMicroFinderDetector.Detect(
                conflicting,
                new FinderCandidate[] { finder },
                new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missingResult, Is.Empty);
            Assert.That(conflictingResult, Is.Empty);
        });
    }

    [Test]
    public void HonorsFamilyMultiplicityAndCandidateLimits()
    {
        int versionIndex = 11;
        int moduleSize = 2;
        int quietZone = 2;
        QrRaster raster = CreateRaster(
            versionIndex,
            RectangularMicroVersionTable.Medium,
            0,
            moduleSize,
            quietZone);
        FinderCandidate finder = CreateFinder(
            versionIndex,
            0,
            moduleSize,
            quietZone,
            0.9);

        RectangularMicroFinderDetection[] disabled =
            RectangularMicroFinderDetector.Detect(
                raster,
                new FinderCandidate[] { finder },
                new QrDetectionOptions { AllowRectangularMicro = false });

        Assert.Multiple(() =>
        {
            Assert.That(disabled, Is.Empty);
            Assert.That(
                () => RectangularMicroFinderDetector.Detect(
                    raster,
                    null!,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroFinderDetector.Detect(
                    raster,
                    new FinderCandidate[] { finder },
                    new QrDetectionOptions { MaximumCandidates = 0 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroFinderDetector.Detect(
                    raster,
                    new FinderCandidate[] { finder, finder },
                    new QrDetectionOptions { MaximumCandidates = 1 }),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrRaster CreateRaster(
        int versionIndex,
        int errorCorrectionIndex,
        int quarterTurns,
        int moduleSize,
        int quietZone)
    {
        int logicalWidth = RectangularMicroVersionTable.GetWidth(versionIndex);
        int logicalHeight = RectangularMicroVersionTable.GetHeight(versionIndex);
        int physicalWidth = quarterTurns % 2 == 0 ? logicalWidth : logicalHeight;
        int physicalHeight = quarterTurns % 2 == 0 ? logicalHeight : logicalWidth;
        int rasterWidth = (physicalWidth + quietZone * 2) * moduleSize;
        int rasterHeight = (physicalHeight + quietZone * 2) * moduleSize;
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[rasterWidth * rasterHeight],
            Width = rasterWidth,
            Height = rasterHeight,
            Stride = rasterWidth
        };
        Array.Fill(raster.Pixels, (byte)255);
        BitMatrixBuffer modules =
            FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(
                versionIndex,
                errorCorrectionIndex);
        for (int row = 0; row < logicalHeight; row++)
        {
            for (int column = 0; column < logicalWidth; column++)
            {
                if (modules.GetModule(row, column))
                {
                    SetLogicalModule(
                        raster,
                        versionIndex,
                        quarterTurns,
                        moduleSize,
                        quietZone,
                        row,
                        column,
                        0);
                }
            }
        }

        return raster;
    }

    static FinderCandidate CreateFinder(
        int versionIndex,
        int quarterTurns,
        int moduleSize,
        int quietZone,
        double confidence)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        double logicalX = 3.5;
        double logicalY = 3.5;
        double physicalX = logicalX;
        double physicalY = logicalY;
        if (quarterTurns == 1)
        {
            physicalX = height - logicalY;
            physicalY = logicalX;
        }
        else if (quarterTurns == 2)
        {
            physicalX = width - logicalX;
            physicalY = height - logicalY;
        }
        else if (quarterTurns == 3)
        {
            physicalX = logicalY;
            physicalY = width - logicalX;
        }

        double centerX = (quietZone + physicalX) * moduleSize;
        double centerY = (quietZone + physicalY) * moduleSize;
        return new FinderCandidate
        {
            CenterX = centerX,
            CenterY = centerY,
            ModuleSize = moduleSize,
            Confidence = confidence,
            ScaleToSource = 1,
            Polarity = QrPolarity.DarkOnLight,
            RegionValidated = true,
            RegionConfidence = confidence,
            CenterRegionArea = 9 * moduleSize * moduleSize,
            OuterRegionArea = 24 * moduleSize * moduleSize,
            FinderCorners = new QrQuadrilateral
            {
                TopLeft = new QrPoint
                {
                    X = centerX - 3.5 * moduleSize,
                    Y = centerY - 3.5 * moduleSize
                },
                TopRight = new QrPoint
                {
                    X = centerX + 3.5 * moduleSize,
                    Y = centerY - 3.5 * moduleSize
                },
                BottomRight = new QrPoint
                {
                    X = centerX + 3.5 * moduleSize,
                    Y = centerY + 3.5 * moduleSize
                },
                BottomLeft = new QrPoint
                {
                    X = centerX - 3.5 * moduleSize,
                    Y = centerY + 3.5 * moduleSize
                }
            }
        };
    }

    static void EraseSubfinder(
        QrRaster raster,
        int versionIndex,
        int moduleSize,
        int quietZone)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int rowOffset = -2; rowOffset <= 2; rowOffset++)
        {
            for (int columnOffset = -2; columnOffset <= 2; columnOffset++)
            {
                SetLogicalModule(
                    raster,
                    versionIndex,
                    0,
                    moduleSize,
                    quietZone,
                    height - 3 + rowOffset,
                    width - 3 + columnOffset,
                    255);
            }
        }
    }

    static void WriteSubfinderFormat(
        QrRaster raster,
        int versionIndex,
        int moduleSize,
        int quietZone,
        int code)
    {
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            SetLogicalModule(
                raster,
                versionIndex,
                0,
                moduleSize,
                quietZone,
                RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                    versionIndex,
                    bitIndex),
                RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                    versionIndex,
                    bitIndex),
                ((code >> bitIndex) & 1) != 0 ? (byte)0 : (byte)255);
        }
    }

    static void ShiftSubfinderEvidence(
        QrRaster raster,
        int versionIndex,
        int moduleSize,
        int quietZone,
        int shiftX,
        int shiftY)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        int startX = (quietZone + width - 8) * moduleSize;
        int startY = (quietZone + height - 6) * moduleSize;
        int regionWidth = 8 * moduleSize;
        int regionHeight = 6 * moduleSize;
        byte[] source = new byte[regionWidth * regionHeight];
        for (int y = 0; y < regionHeight; y++)
        {
            for (int x = 0; x < regionWidth; x++)
            {
                source[y * regionWidth + x] =
                    raster.Pixels[(startY + y) * raster.Stride + startX + x];
                raster.Pixels[(startY + y) * raster.Stride + startX + x] = 255;
            }
        }

        for (int y = 0; y < regionHeight; y++)
        {
            for (int x = 0; x < regionWidth; x++)
            {
                raster.Pixels[(startY + shiftY + y) * raster.Stride
                    + startX + shiftX + x] = source[y * regionWidth + x];
            }
        }
    }

    static void ToggleLogicalModule(
        QrRaster raster,
        int versionIndex,
        int quarterTurns,
        int moduleSize,
        int quietZone,
        int row,
        int column)
    {
        int[] physical = MapModule(versionIndex, quarterTurns, row, column);
        int startY = (quietZone + physical[0]) * moduleSize;
        int startX = (quietZone + physical[1]) * moduleSize;
        for (int y = startY; y < startY + moduleSize; y++)
        {
            for (int x = startX; x < startX + moduleSize; x++)
            {
                int offset = y * raster.Stride + x;
                raster.Pixels[offset] = raster.Pixels[offset] == 0
                    ? (byte)255
                    : (byte)0;
            }
        }
    }

    static void SetLogicalModule(
        QrRaster raster,
        int versionIndex,
        int quarterTurns,
        int moduleSize,
        int quietZone,
        int row,
        int column,
        byte value)
    {
        int[] physical = MapModule(versionIndex, quarterTurns, row, column);
        int startY = (quietZone + physical[0]) * moduleSize;
        int startX = (quietZone + physical[1]) * moduleSize;
        for (int y = startY; y < startY + moduleSize; y++)
        {
            for (int x = startX; x < startX + moduleSize; x++)
            {
                raster.Pixels[y * raster.Stride + x] = value;
            }
        }
    }

    static int[] MapModule(
        int versionIndex,
        int quarterTurns,
        int row,
        int column)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        if (quarterTurns == 1)
        {
            return new int[] { column, height - 1 - row };
        }

        if (quarterTurns == 2)
        {
            return new int[] { height - 1 - row, width - 1 - column };
        }

        if (quarterTurns == 3)
        {
            return new int[] { width - 1 - column, row };
        }

        return new int[] { row, column };
    }
}
