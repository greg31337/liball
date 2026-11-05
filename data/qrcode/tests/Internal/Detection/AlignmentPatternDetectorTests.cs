using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class AlignmentPatternDetectorTests
{
    [Test]
    public void FindsEveryStandardAlignmentPatternAcrossAllVersions()
    {
        for (int version = Model2VersionTable.MinimumVersion;
            version <= Model2VersionTable.MaximumVersion;
            version++)
        {
            QrRaster raster = CreateRaster(version, 1, 4);
            FinderGroup group = CreateGroup(version, 1, 4);

            AlignmentPatternSearchResult result = AlignmentPatternDetector.Detect(
                raster,
                group,
                new QrDetectionOptions());

            int positionCount = AlignmentPositionTable.GetPositions(version).Length;
            int expectedCount = positionCount == 0
                ? 0
                : positionCount * positionCount - 3;
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, $"Version {version}");
                Assert.That(result.ExpectedCount, Is.EqualTo(expectedCount), $"Version {version}");
                Assert.That(result.FoundCount, Is.EqualTo(expectedCount), $"Version {version}");
                Assert.That(result.Patterns, Has.Length.EqualTo(expectedCount), $"Version {version}");
                Assert.That(result.UsedFallback, Is.False, $"Version {version}");
                Assert.That(result.Confidence, Is.EqualTo(1.0), $"Version {version}");
            });
        }
    }

    [Test]
    public void ReportsLogicalAndImageGeometryForVersionTwo()
    {
        int version = 2;
        int moduleSize = 3;
        int quietZone = 4;
        FinderGroup group = CreateGroup(version, moduleSize, quietZone);

        AlignmentPatternSearchResult result = AlignmentPatternDetector.Detect(
            CreateRaster(version, moduleSize, quietZone),
            group,
            new QrDetectionOptions());

        double expectedCenter = (quietZone + 18.5) * moduleSize;
        Assert.Multiple(() =>
        {
            Assert.That(result.Patterns, Has.Length.EqualTo(1));
            Assert.That(result.Patterns[0].LogicalRow, Is.EqualTo(18));
            Assert.That(result.Patterns[0].LogicalColumn, Is.EqualTo(18));
            Assert.That(result.Patterns[0].PredictedCenter.X,
                Is.EqualTo(expectedCenter).Within(0.001));
            Assert.That(result.Patterns[0].PredictedCenter.Y,
                Is.EqualTo(expectedCenter).Within(0.001));
            Assert.That(result.Patterns[0].Center.X,
                Is.EqualTo(expectedCenter).Within(0.001));
            Assert.That(result.Patterns[0].Center.Y,
                Is.EqualTo(expectedCenter).Within(0.001));
            Assert.That(result.Patterns[0].Found, Is.True);
            Assert.That(result.Patterns[0].UsedFallback, Is.False);
            Assert.That(result.Patterns[0].SearchRadius, Is.Zero);
            Assert.That(result.Patterns[0].Confidence, Is.EqualTo(1.0));
            Assert.That(result.RefinedBottomRight.X,
                Is.EqualTo(group.EstimatedBottomRight.X));
            Assert.That(result.RefinedBottomRight.Y,
                Is.EqualTo(group.EstimatedBottomRight.Y));
        });
    }

    [Test]
    public void ExpandsTheSearchAndRefinesGeometryForADisplacedPattern()
    {
        int version = 2;
        int moduleSize = 3;
        int quietZone = 4;
        int shiftX = 6;
        int shiftY = -5;
        QrRaster raster = CreateRaster(version, moduleSize, quietZone);
        ShiftAlignmentPattern(
            raster,
            18,
            18,
            moduleSize,
            quietZone,
            shiftX,
            shiftY);
        FinderGroup group = CreateGroup(version, moduleSize, quietZone);

        AlignmentPatternSearchResult result = AlignmentPatternDetector.Detect(
            raster,
            group,
            new QrDetectionOptions());

        AlignmentPatternDetection pattern = result.Patterns[0];
        Assert.Multiple(() =>
        {
            Assert.That(pattern.Found, Is.True);
            Assert.That(pattern.SearchRadius, Is.EqualTo(6));
            Assert.That(pattern.Confidence, Is.EqualTo(1.0));
            Assert.That(pattern.Center.X,
                Is.EqualTo(pattern.PredictedCenter.X + shiftX).Within(0.001));
            Assert.That(pattern.Center.Y,
                Is.EqualTo(pattern.PredictedCenter.Y + shiftY).Within(0.001));
            Assert.That(result.RefinedBottomRight.X,
                Is.EqualTo(group.EstimatedBottomRight.X + shiftX).Within(0.001));
            Assert.That(result.RefinedBottomRight.Y,
                Is.EqualTo(group.EstimatedBottomRight.Y + shiftY).Within(0.001));
        });
    }

    [Test]
    public void FallsBackToPredictionWhenThePatternIsMissing()
    {
        int version = 2;
        int moduleSize = 3;
        int quietZone = 4;
        QrRaster raster = CreateRaster(version, moduleSize, quietZone);
        EraseAlignmentPattern(raster, 18, 18, moduleSize, quietZone);
        FinderGroup group = CreateGroup(version, moduleSize, quietZone);

        AlignmentPatternSearchResult result = AlignmentPatternDetector.Detect(
            raster,
            group,
            new QrDetectionOptions());

        AlignmentPatternDetection pattern = result.Patterns[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FoundCount, Is.Zero);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(pattern.Found, Is.False);
            Assert.That(pattern.UsedFallback, Is.True);
            Assert.That(pattern.SearchRadius, Is.EqualTo(12));
            Assert.That(pattern.Center.X, Is.EqualTo(pattern.PredictedCenter.X));
            Assert.That(pattern.Center.Y, Is.EqualTo(pattern.PredictedCenter.Y));
            Assert.That(result.RefinedBottomRight.X,
                Is.EqualTo(group.EstimatedBottomRight.X));
            Assert.That(result.RefinedBottomRight.Y,
                Is.EqualTo(group.EstimatedBottomRight.Y));
        });
    }

    [Test]
    public void ReturnsNoSearchForDisabledOrNonModelTwoGroups()
    {
        int version = 2;
        FinderGroup model2 = CreateGroup(version, 2, 4);
        FinderGroup legacy = CreateGroup(version, 2, 4);
        legacy.Model2Possible = false;
        legacy.LegacyModel1Possible = true;
        QrRaster raster = CreateRaster(version, 2, 4);

        AlignmentPatternSearchResult disabled = AlignmentPatternDetector.Detect(
            raster,
            model2,
            new QrDetectionOptions { AllowModel2 = false });
        AlignmentPatternSearchResult notApplicable = AlignmentPatternDetector.Detect(
            raster,
            legacy,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Success, Is.False);
            Assert.That(disabled.Patterns, Is.Empty);
            Assert.That(notApplicable.Success, Is.False);
            Assert.That(notApplicable.Patterns, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidRasterGroupAndVersionGeometry()
    {
        FinderGroup valid = CreateGroup(2, 2, 4);
        FinderGroup invalidVersion = CreateGroup(2, 2, 4);
        invalidVersion.EstimatedVersion = 41;
        FinderGroup invalidDimension = CreateGroup(2, 2, 4);
        invalidDimension.EstimatedDimension = 29;
        FinderGroup degenerate = CreateGroup(2, 2, 4);
        degenerate.TopRight.CenterX = degenerate.TopLeft.CenterX;
        degenerate.TopRight.CenterY = degenerate.TopLeft.CenterY;
        QrRaster raster = CreateRaster(2, 2, 4);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => AlignmentPatternDetector.Detect(
                    null!,
                    valid,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => AlignmentPatternDetector.Detect(
                    raster,
                    null!,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => AlignmentPatternDetector.Detect(
                    raster,
                    invalidVersion,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => AlignmentPatternDetector.Detect(
                    raster,
                    invalidDimension,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => AlignmentPatternDetector.Detect(
                    raster,
                    degenerate,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrRaster CreateRaster(int version, int moduleSize, int quietZone)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        int rasterDimension = (dimension + quietZone * 2) * moduleSize;
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[rasterDimension * rasterDimension],
            Width = rasterDimension,
            Height = rasterDimension,
            Stride = rasterDimension
        };
        Array.Fill(raster.Pixels, (byte)255);
        BitMatrixBuffer modules = FunctionPatternBuilder.CreateModel2FunctionPatterns(
            version,
            EccBlockTable.Low,
            0);
        for (int row = 0; row < dimension; row++)
        {
            for (int column = 0; column < dimension; column++)
            {
                if (modules.GetModule(row, column))
                {
                    SetLogicalModule(
                        raster,
                        row,
                        column,
                        moduleSize,
                        quietZone,
                        0);
                }
            }
        }

        return raster;
    }

    static FinderGroup CreateGroup(int version, int moduleSize, int quietZone)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        double topLeftCenter = (quietZone + 3.5) * moduleSize;
        double farCenter = (quietZone + dimension - 3.5) * moduleSize;
        FinderCandidate topLeft = CreateCandidate(
            topLeftCenter,
            topLeftCenter,
            moduleSize);
        FinderCandidate topRight = CreateCandidate(
            farCenter,
            topLeftCenter,
            moduleSize);
        FinderCandidate bottomLeft = CreateCandidate(
            topLeftCenter,
            farCenter,
            moduleSize);
        return new FinderGroup(topLeft, topRight, bottomLeft)
        {
            EstimatedBottomRight = new QrPoint { X = farCenter, Y = farCenter },
            EstimatedSymbolKind = QrSymbolKind.Model2,
            Model2Possible = true,
            LegacyModel1Possible = version <= LegacyModel1Table.MaximumVersion,
            EstimatedVersion = version,
            EstimatedDimension = dimension,
            ModuleSize = moduleSize,
            HorizontalDistance = (dimension - 7) * moduleSize,
            VerticalDistance = (dimension - 7) * moduleSize,
            Confidence = 0.95,
            Polarity = QrPolarity.DarkOnLight
        };
    }

    static FinderCandidate CreateCandidate(
        double centerX,
        double centerY,
        int moduleSize)
    {
        return new FinderCandidate
        {
            CenterX = centerX,
            CenterY = centerY,
            ModuleSize = moduleSize,
            Confidence = 0.95,
            RegionValidated = true,
            RegionConfidence = 0.95,
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

    static void ShiftAlignmentPattern(
        QrRaster raster,
        int logicalRow,
        int logicalColumn,
        int moduleSize,
        int quietZone,
        int shiftX,
        int shiftY)
    {
        int startX = (quietZone + logicalColumn - 2) * moduleSize;
        int startY = (quietZone + logicalRow - 2) * moduleSize;
        int patternSize = 5 * moduleSize;
        byte[] source = new byte[patternSize * patternSize];
        for (int y = 0; y < patternSize; y++)
        {
            for (int x = 0; x < patternSize; x++)
            {
                source[y * patternSize + x] =
                    raster.Pixels[(startY + y) * raster.Stride + startX + x];
                raster.Pixels[(startY + y) * raster.Stride + startX + x] = 255;
            }
        }

        for (int y = 0; y < patternSize; y++)
        {
            for (int x = 0; x < patternSize; x++)
            {
                raster.Pixels[(startY + shiftY + y) * raster.Stride
                    + startX + shiftX + x] = source[y * patternSize + x];
            }
        }
    }

    static void EraseAlignmentPattern(
        QrRaster raster,
        int logicalRow,
        int logicalColumn,
        int moduleSize,
        int quietZone)
    {
        for (int rowOffset = -2; rowOffset <= 2; rowOffset++)
        {
            for (int columnOffset = -2; columnOffset <= 2; columnOffset++)
            {
                SetLogicalModule(
                    raster,
                    logicalRow + rowOffset,
                    logicalColumn + columnOffset,
                    moduleSize,
                    quietZone,
                    255);
            }
        }
    }

    static void SetLogicalModule(
        QrRaster raster,
        int row,
        int column,
        int moduleSize,
        int quietZone,
        byte value)
    {
        int startY = (quietZone + row) * moduleSize;
        int startX = (quietZone + column) * moduleSize;
        for (int y = startY; y < startY + moduleSize; y++)
        {
            for (int x = startX; x < startX + moduleSize; x++)
            {
                raster.Pixels[y * raster.Stride + x] = value;
            }
        }
    }
}
