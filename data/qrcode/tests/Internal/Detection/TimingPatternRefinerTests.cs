using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class TimingPatternRefinerTests
{
    [Test]
    public void ConfirmsCleanTimingPhaseForEveryModelTwoVersion()
    {
        for (int version = Model2VersionTable.MinimumVersion;
            version <= Model2VersionTable.MaximumVersion;
            version++)
        {
            QrRaster raster = CreateRaster(version, 2, 4);
            FinderGroup group = CreateGroup(version, version, 2, 4);

            TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
                raster,
                group,
                new QrDetectionOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, $"Version {version}");
                Assert.That(result.UsedFallback, Is.False, $"Version {version}");
                Assert.That(result.DimensionChanged, Is.False, $"Version {version}");
                Assert.That(result.RefinedVersion, Is.EqualTo(version));
                Assert.That(
                    result.RefinedDimension,
                    Is.EqualTo(Model2VersionTable.GetDimension(version)));
                Assert.That(result.HorizontalConfidence, Is.EqualTo(1.0));
                Assert.That(result.VerticalConfidence, Is.EqualTo(1.0));
                Assert.That(result.HorizontalPhaseMargin, Is.EqualTo(1.0));
                Assert.That(result.VerticalPhaseMargin, Is.EqualTo(1.0));
            });
        }
    }

    [TestCase(1, 2)]
    [TestCase(10, 9)]
    [TestCase(18, 19)]
    [TestCase(40, 39)]
    public void RefinesNearbyIncorrectVersionAndDimensionEstimates(
        int actualVersion,
        int estimatedVersion)
    {
        int moduleSize = 3;
        FinderGroup group = CreateGroup(
            actualVersion,
            estimatedVersion,
            moduleSize,
            4);

        TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
            CreateRaster(actualVersion, moduleSize, 4),
            group,
            new QrDetectionOptions());

        int actualDimension = Model2VersionTable.GetDimension(actualVersion);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DimensionChanged, Is.True);
            Assert.That(result.OriginalVersion, Is.EqualTo(estimatedVersion));
            Assert.That(result.RefinedVersion, Is.EqualTo(actualVersion));
            Assert.That(result.RefinedDimension, Is.EqualTo(actualDimension));
            Assert.That(result.HorizontalConfidence, Is.EqualTo(1.0));
            Assert.That(result.VerticalConfidence, Is.EqualTo(1.0));
            Assert.That(result.HorizontalModuleSize, Is.EqualTo(moduleSize).Within(0.001));
            Assert.That(result.VerticalModuleSize, Is.EqualTo(moduleSize).Within(0.001));
        });
    }

    [Test]
    public void IgnoresAlignmentPatternsThatInterruptTimingTracks()
    {
        int version = 7;

        TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
            CreateRaster(version, 2, 4),
            CreateGroup(version, version, 2, 4),
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HorizontalSampleCount, Is.EqualTo(24));
            Assert.That(result.VerticalSampleCount, Is.EqualTo(24));
            Assert.That(result.Confidence, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void AcceptsBoundedDamageOnBothTimingAxes()
    {
        int version = 1;
        int moduleSize = 3;
        int quietZone = 4;
        QrRaster raster = CreateRaster(version, moduleSize, quietZone);
        ToggleLogicalModule(raster, 6, 8, moduleSize, quietZone);
        ToggleLogicalModule(raster, 8, 6, moduleSize, quietZone);

        TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
            raster,
            CreateGroup(version, version, moduleSize, quietZone),
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HorizontalConfidence, Is.EqualTo(0.8));
            Assert.That(result.VerticalConfidence, Is.EqualTo(0.8));
            Assert.That(result.HorizontalPhaseMargin, Is.EqualTo(0.6).Within(0.001));
            Assert.That(result.VerticalPhaseMargin, Is.EqualTo(0.6).Within(0.001));
        });
    }

    [Test]
    public void RejectsOppositeTimingPhaseAndRetainsTheGeometryFallback()
    {
        int version = 10;
        int moduleSize = 2;
        int quietZone = 4;
        QrRaster raster = CreateRaster(version, moduleSize, quietZone);
        InvertTimingTracks(raster, version, moduleSize, quietZone);
        FinderGroup group = CreateGroup(version, version, moduleSize, quietZone);

        TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
            raster,
            group,
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.DimensionChanged, Is.False);
            Assert.That(result.RefinedVersion, Is.EqualTo(version));
            Assert.That(result.RefinedDimension, Is.EqualTo(group.EstimatedDimension));
            Assert.That(result.HorizontalConfidence, Is.EqualTo(0.0));
            Assert.That(result.VerticalConfidence, Is.EqualTo(0.0));
            Assert.That(result.HorizontalPhaseMargin, Is.EqualTo(-1.0));
            Assert.That(result.VerticalPhaseMargin, Is.EqualTo(-1.0));
        });
    }

    [Test]
    public void SupportsLegacyOnlyTimingChecksWithinItsVersionRange()
    {
        int version = 5;
        FinderGroup group = CreateGroup(version, version, 2, 4);
        group.Model2Possible = false;
        group.LegacyModel1Possible = true;

        TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
            CreateRaster(version, 2, 4),
            group,
            new QrDetectionOptions
            {
                AllowModel2 = false,
                AllowLegacyModel1 = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RefinedVersion, Is.EqualTo(version));
            Assert.That(result.Confidence, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void ReturnsFallbackWhenNoSquareFamilyIsEnabled()
    {
        int version = 3;
        FinderGroup group = CreateGroup(version, version, 2, 4);

        TimingPatternRefinementResult result = TimingPatternRefiner.Refine(
            CreateRaster(version, 2, 4),
            group,
            new QrDetectionOptions
            {
                AllowModel2 = false,
                AllowLegacyModel1 = false
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.VersionsEvaluated, Is.Zero);
            Assert.That(result.RefinedVersion, Is.EqualTo(version));
            Assert.That(result.RefinedDimension, Is.EqualTo(group.EstimatedDimension));
        });
    }

    [Test]
    public void RejectsInvalidRasterAndFinderGeometry()
    {
        FinderGroup valid = CreateGroup(2, 2, 2, 4);
        FinderGroup invalidDimension = CreateGroup(2, 2, 2, 4);
        invalidDimension.EstimatedDimension = 29;
        FinderGroup degenerate = CreateGroup(2, 2, 2, 4);
        degenerate.TopRight.CenterX = degenerate.TopLeft.CenterX;
        degenerate.TopRight.CenterY = degenerate.TopLeft.CenterY;
        QrRaster raster = CreateRaster(2, 2, 4);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => TimingPatternRefiner.Refine(
                    null!,
                    valid,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => TimingPatternRefiner.Refine(
                    raster,
                    null!,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => TimingPatternRefiner.Refine(
                    raster,
                    invalidDimension,
                    new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => TimingPatternRefiner.Refine(
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

    static FinderGroup CreateGroup(
        int actualVersion,
        int estimatedVersion,
        int moduleSize,
        int quietZone)
    {
        int actualDimension = Model2VersionTable.GetDimension(actualVersion);
        int estimatedDimension = Model2VersionTable.GetDimension(estimatedVersion);
        double topLeftCenter = (quietZone + 3.5) * moduleSize;
        double farCenter = (quietZone + actualDimension - 3.5) * moduleSize;
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
            LegacyModel1Possible = estimatedVersion <= LegacyModel1Table.MaximumVersion,
            EstimatedVersion = estimatedVersion,
            EstimatedDimension = estimatedDimension,
            ModuleSize = moduleSize,
            HorizontalDistance = (actualDimension - 7) * moduleSize,
            VerticalDistance = (actualDimension - 7) * moduleSize,
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

    static void InvertTimingTracks(
        QrRaster raster,
        int version,
        int moduleSize,
        int quietZone)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        for (int index = 8; index < dimension - 8; index++)
        {
            if (!IsAlignmentInterruption(version, index))
            {
                ToggleLogicalModule(raster, 6, index, moduleSize, quietZone);
                ToggleLogicalModule(raster, index, 6, moduleSize, quietZone);
            }
        }
    }

    static bool IsAlignmentInterruption(int version, int timingIndex)
    {
        int[] positions = AlignmentPositionTable.GetPositions(version);
        for (int index = 1; index < positions.Length - 1; index++)
        {
            int distance = timingIndex - positions[index];
            if (distance < 0)
            {
                distance = -distance;
            }

            if (distance <= 2)
            {
                return true;
            }
        }

        return false;
    }

    static void ToggleLogicalModule(
        QrRaster raster,
        int row,
        int column,
        int moduleSize,
        int quietZone)
    {
        int startY = (quietZone + row) * moduleSize;
        int startX = (quietZone + column) * moduleSize;
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
