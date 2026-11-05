using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MicroFinderDetectorTests
{
    [TestCase(1, MicroVersionTable.DetectionOnly, 0)]
    [TestCase(2, EccBlockTable.Low, 1)]
    [TestCase(3, EccBlockTable.Medium, 2)]
    [TestCase(4, EccBlockTable.Quartile, 3)]
    public void DetectsEveryMicroVersionFromFormatTimingAndExtent(
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster raster = CreateMicroRaster(
            version,
            errorCorrectionIndex,
            mask,
            0,
            moduleSize,
            quietZone);
        FinderCandidate finder = CreateFinderCandidate(
            version,
            0,
            moduleSize,
            quietZone,
            0.92);

        MicroFinderDetection[] detections = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[] { finder },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].Finder, Is.SameAs(finder));
            Assert.That(detections[0].Version, Is.EqualTo(version));
            Assert.That(
                detections[0].Dimension,
                Is.EqualTo(MicroVersionTable.GetDimension(version)));
            Assert.That(
                detections[0].ErrorCorrectionIndex,
                Is.EqualTo(errorCorrectionIndex));
            Assert.That(detections[0].Mask, Is.EqualTo(mask));
            Assert.That(detections[0].QuarterTurnsClockwise, Is.Zero);
            Assert.That(detections[0].FormatDistance, Is.Zero);
            Assert.That(detections[0].TimingConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].ExtentConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].Confidence, Is.GreaterThan(0.95));
            Assert.That(detections[0].Polarity, Is.EqualTo(QrPolarity.DarkOnLight));
            Assert.That(
                detections[0].SymbolCorners.TopLeft!.X,
                Is.EqualTo(quietZone * moduleSize).Within(0.001));
            Assert.That(
                detections[0].SymbolCorners.BottomRight!.Y,
                Is.EqualTo((quietZone + MicroVersionTable.GetDimension(version)) * moduleSize)
                    .Within(0.001));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void RecoversProfileSpecificOrientationForEveryQuarterTurn(int quarterTurns)
    {
        int version = 4;
        int moduleSize = 4;
        int quietZone = 2;
        QrRaster raster = CreateMicroRaster(
            version,
            EccBlockTable.Medium,
            2,
            quarterTurns,
            moduleSize,
            quietZone);
        FinderCandidate finder = CreateFinderCandidate(
            version,
            quarterTurns,
            moduleSize,
            quietZone,
            0.9);

        MicroFinderDetection[] detections = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[] { finder },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].Version, Is.EqualTo(version));
            Assert.That(detections[0].QuarterTurnsClockwise, Is.EqualTo(quarterTurns));
            Assert.That(detections[0].TimingConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].FormatDistance, Is.Zero);
        });
    }

    [Test]
    public void CorrectsThreeFormatModulesAndReportsTheEvidenceDistance()
    {
        int version = 3;
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster raster = CreateMicroRaster(
            version,
            EccBlockTable.Low,
            1,
            0,
            moduleSize,
            quietZone);
        ToggleLogicalModule(raster, version, 0, moduleSize, quietZone, 1, 8);
        ToggleLogicalModule(raster, version, 0, moduleSize, quietZone, 2, 8);
        ToggleLogicalModule(raster, version, 0, moduleSize, quietZone, 3, 8);

        MicroFinderDetection[] detections = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[]
            {
                CreateFinderCandidate(version, 0, moduleSize, quietZone, 0.9)
            },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].Version, Is.EqualTo(version));
            Assert.That(detections[0].ErrorCorrectionIndex, Is.EqualTo(EccBlockTable.Low));
            Assert.That(detections[0].Mask, Is.EqualTo(1));
            Assert.That(detections[0].FormatDistance, Is.EqualTo(3));
            Assert.That(detections[0].FormatConfidence, Is.EqualTo(0.25));
        });
    }

    [Test]
    public void UsesRecoveredFinderAxesForNonCardinalImageRotation()
    {
        int version = 3;
        int moduleSize = 4;
        double angle = 0.37;
        QrRaster raster = CreateAngledMicroRaster(
            version,
            EccBlockTable.Medium,
            2,
            moduleSize,
            angle);
        FinderCandidate finder = CreateAngledFinderCandidate(
            moduleSize,
            angle,
            0.93);

        MicroFinderDetection[] detections = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[] { finder },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detections, Has.Length.EqualTo(1));
            Assert.That(detections[0].Version, Is.EqualTo(version));
            Assert.That(detections[0].QuarterTurnsClockwise, Is.Zero);
            Assert.That(detections[0].TimingConfidence, Is.EqualTo(1.0));
            Assert.That(detections[0].ExtentConfidence, Is.EqualTo(1.0));
            Assert.That(
                detections[0].SymbolCorners.TopLeft!.X,
                Is.LessThan(finder.CenterX));
            Assert.That(
                detections[0].SymbolCorners.TopLeft!.Y,
                Is.LessThan(finder.CenterY));
        });
    }

    [Test]
    public void RejectsIncorrectTimingPhaseAndDarkExtent()
    {
        int version = 4;
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster badTiming = CreateMicroRaster(
            version,
            EccBlockTable.Low,
            0,
            0,
            moduleSize,
            quietZone);
        for (int index = MicroPlacementTable.TimingStartIndex;
            index < MicroVersionTable.GetDimension(version);
            index++)
        {
            ToggleLogicalModule(badTiming, version, 0, moduleSize, quietZone, 0, index);
            ToggleLogicalModule(badTiming, version, 0, moduleSize, quietZone, index, 0);
        }

        QrRaster badExtent = CreateMicroRaster(
            version,
            EccBlockTable.Low,
            0,
            0,
            moduleSize,
            quietZone);
        DarkenExtent(badExtent, version, moduleSize, quietZone);
        FinderCandidate finder = CreateFinderCandidate(
            version,
            0,
            moduleSize,
            quietZone,
            0.9);

        MicroFinderDetection[] timingResult = MicroFinderDetector.Detect(
            badTiming,
            new FinderCandidate[] { finder },
            new QrDetectionOptions());
        MicroFinderDetection[] extentResult = MicroFinderDetector.Detect(
            badExtent,
            new FinderCandidate[] { finder },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(timingResult, Is.Empty);
            Assert.That(extentResult, Is.Empty);
        });
    }

    [Test]
    public void CroppedQuietZoneRequiresTheConfiguredTolerance()
    {
        int version = 2;
        int moduleSize = 3;
        QrRaster raster = CreateMicroRaster(
            version,
            EccBlockTable.Medium,
            3,
            0,
            moduleSize,
            0);
        FinderCandidate finder = CreateFinderCandidate(version, 0, moduleSize, 0, 0.9);

        MicroFinderDetection[] allowed = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[] { finder },
            new QrDetectionOptions { AllowCroppedQuietZone = true });
        MicroFinderDetection[] rejected = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[] { finder },
            new QrDetectionOptions { AllowCroppedQuietZone = false });

        Assert.Multiple(() =>
        {
            Assert.That(allowed, Has.Length.EqualTo(1));
            Assert.That(allowed[0].ExtentConfidence, Is.EqualTo(0.5));
            Assert.That(rejected, Is.Empty);
        });
    }

    [Test]
    public void HonorsFamilyMultiplicityAndCandidateLimits()
    {
        int version = 2;
        int moduleSize = 3;
        int quietZone = 2;
        QrRaster raster = CreateMicroRaster(
            version,
            EccBlockTable.Low,
            0,
            0,
            moduleSize,
            quietZone);
        FinderCandidate finder = CreateFinderCandidate(
            version,
            0,
            moduleSize,
            quietZone,
            0.9);

        MicroFinderDetection[] disabled = MicroFinderDetector.Detect(
            raster,
            new FinderCandidate[] { finder },
            new QrDetectionOptions { AllowMicro = false });

        Assert.Multiple(() =>
        {
            Assert.That(disabled, Is.Empty);
            Assert.That(
                () => MicroFinderDetector.Detect(
                    raster,
                    null!,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroFinderDetector.Detect(
                    raster,
                    new FinderCandidate[] { finder },
                    new QrDetectionOptions { MaximumCandidates = 0 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroFinderDetector.Detect(
                    raster,
                    new FinderCandidate[] { finder, finder },
                    new QrDetectionOptions { MaximumCandidates = 1 }),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrRaster CreateMicroRaster(
        int version,
        int errorCorrectionIndex,
        int mask,
        int quarterTurns,
        int moduleSize,
        int quietZone)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        int rasterDimension = (dimension + quietZone * 2) * moduleSize;
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[rasterDimension * rasterDimension],
            Width = rasterDimension,
            Height = rasterDimension,
            Stride = rasterDimension
        };
        Array.Fill(raster.Pixels, (byte)255);
        BitMatrixBuffer modules = FunctionPatternBuilder.CreateMicroFunctionPatterns(
            version,
            errorCorrectionIndex,
            mask);
        for (int row = 0; row < dimension; row++)
        {
            for (int column = 0; column < dimension; column++)
            {
                if (modules.GetModule(row, column))
                {
                    SetLogicalModule(
                        raster,
                        version,
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

    static FinderCandidate CreateFinderCandidate(
        int version,
        int quarterTurns,
        int moduleSize,
        int quietZone,
        double confidence)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        double logicalX = 3.5;
        double logicalY = 3.5;
        double physicalX = logicalX;
        double physicalY = logicalY;
        if (quarterTurns == 1)
        {
            physicalX = dimension - logicalY;
            physicalY = logicalX;
        }
        else if (quarterTurns == 2)
        {
            physicalX = dimension - logicalX;
            physicalY = dimension - logicalY;
        }
        else if (quarterTurns == 3)
        {
            physicalX = logicalY;
            physicalY = dimension - logicalX;
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

    static QrRaster CreateAngledMicroRaster(
        int version,
        int errorCorrectionIndex,
        int mask,
        int moduleSize,
        double angle)
    {
        int rasterDimension = 120;
        double center = rasterDimension / 2.0;
        double columnX = global::System.Math.Cos(angle);
        double columnY = global::System.Math.Sin(angle);
        double rowX = -columnY;
        double rowY = columnX;
        int dimension = MicroVersionTable.GetDimension(version);
        BitMatrixBuffer modules = FunctionPatternBuilder.CreateMicroFunctionPatterns(
            version,
            errorCorrectionIndex,
            mask);
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[rasterDimension * rasterDimension],
            Width = rasterDimension,
            Height = rasterDimension,
            Stride = rasterDimension
        };
        Array.Fill(raster.Pixels, (byte)255);
        for (int y = 0; y < raster.Height; y++)
        {
            for (int x = 0; x < raster.Width; x++)
            {
                double offsetX = x + 0.5 - center;
                double offsetY = y + 0.5 - center;
                double logicalColumn = (offsetX * columnX + offsetY * columnY)
                    / moduleSize + 3.5;
                double logicalRow = (offsetX * rowX + offsetY * rowY)
                    / moduleSize + 3.5;
                int column = (int)logicalColumn;
                int row = (int)logicalRow;
                if (logicalColumn >= 0.0
                    && logicalRow >= 0.0
                    && column < dimension
                    && row < dimension
                    && modules.GetModule(row, column))
                {
                    raster.Pixels[y * raster.Stride + x] = 0;
                }
            }
        }

        return raster;
    }

    static FinderCandidate CreateAngledFinderCandidate(
        int moduleSize,
        double angle,
        double confidence)
    {
        double center = 60.0;
        double columnX = global::System.Math.Cos(angle);
        double columnY = global::System.Math.Sin(angle);
        double rowX = -columnY;
        double rowY = columnX;
        double radius = 3.5 * moduleSize;
        return new FinderCandidate
        {
            CenterX = center,
            CenterY = center,
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
                TopLeft = CreateAngledPoint(
                    center,
                    columnX,
                    columnY,
                    rowX,
                    rowY,
                    -radius,
                    -radius),
                TopRight = CreateAngledPoint(
                    center,
                    columnX,
                    columnY,
                    rowX,
                    rowY,
                    radius,
                    -radius),
                BottomRight = CreateAngledPoint(
                    center,
                    columnX,
                    columnY,
                    rowX,
                    rowY,
                    radius,
                    radius),
                BottomLeft = CreateAngledPoint(
                    center,
                    columnX,
                    columnY,
                    rowX,
                    rowY,
                    -radius,
                    radius)
            }
        };
    }

    static QrPoint CreateAngledPoint(
        double center,
        double columnX,
        double columnY,
        double rowX,
        double rowY,
        double horizontal,
        double vertical)
    {
        return new QrPoint
        {
            X = center + horizontal * columnX + vertical * rowX,
            Y = center + horizontal * columnY + vertical * rowY
        };
    }

    static void ToggleLogicalModule(
        QrRaster raster,
        int version,
        int quarterTurns,
        int moduleSize,
        int quietZone,
        int row,
        int column)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        int physicalRow = row;
        int physicalColumn = column;
        if (quarterTurns == 1)
        {
            physicalRow = column;
            physicalColumn = dimension - 1 - row;
        }
        else if (quarterTurns == 2)
        {
            physicalRow = dimension - 1 - row;
            physicalColumn = dimension - 1 - column;
        }
        else if (quarterTurns == 3)
        {
            physicalRow = dimension - 1 - column;
            physicalColumn = row;
        }

        int startY = (quietZone + physicalRow) * moduleSize;
        int startX = (quietZone + physicalColumn) * moduleSize;
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
        int version,
        int quarterTurns,
        int moduleSize,
        int quietZone,
        int row,
        int column,
        byte value)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        int physicalRow = row;
        int physicalColumn = column;
        if (quarterTurns == 1)
        {
            physicalRow = column;
            physicalColumn = dimension - 1 - row;
        }
        else if (quarterTurns == 2)
        {
            physicalRow = dimension - 1 - row;
            physicalColumn = dimension - 1 - column;
        }
        else if (quarterTurns == 3)
        {
            physicalRow = dimension - 1 - column;
            physicalColumn = row;
        }

        int startY = (quietZone + physicalRow) * moduleSize;
        int startX = (quietZone + physicalColumn) * moduleSize;
        for (int y = startY; y < startY + moduleSize; y++)
        {
            for (int x = startX; x < startX + moduleSize; x++)
            {
                raster.Pixels[y * raster.Stride + x] = value;
            }
        }
    }

    static void DarkenExtent(
        QrRaster raster,
        int version,
        int moduleSize,
        int quietZone)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        int first = (quietZone - 1) * moduleSize;
        int last = (quietZone + dimension) * moduleSize;
        int start = first;
        int end = last + moduleSize;
        for (int coordinate = start; coordinate < end; coordinate++)
        {
            for (int offset = 0; offset < moduleSize; offset++)
            {
                raster.Pixels[(first + offset) * raster.Stride + coordinate] = 0;
                raster.Pixels[(last + offset) * raster.Stride + coordinate] = 0;
                raster.Pixels[coordinate * raster.Stride + first + offset] = 0;
                raster.Pixels[coordinate * raster.Stride + last + offset] = 0;
            }
        }
    }
}
