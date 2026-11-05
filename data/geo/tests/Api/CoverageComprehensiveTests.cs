using geo;

namespace geo.Tests;

public class CoverageComprehensiveTests
{
    [Test]
    public void ValidGridMosaicFlowsThroughEveryCoverageOperation()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)), " +
            "POLYGON ((4 0, 6 0, 6 2, 4 2, 4 0)), " +
            "POLYGON ((0 2, 2 2, 2 4, 0 4, 0 2)), " +
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2)), " +
            "POLYGON ((4 2, 6 2, 6 4, 4 4, 4 2)))");
        SimplifyParameters simplify = SimplifyParametersValue(0.0);

        CoverageValidationResult validation =
            operations.ValidatePolygonCoverage(source, 0.0);
        Geometry edges =
            operations.ExtractPolygonCoverageEdges(source);
        Geometry union = operations.UnionPolygonCoverage(source);
        Geometry simplified = operations.SimplifyPolygonCoverage(
            source, simplify);
        Geometry cleaned = operations.CleanPolygonCoverage(
            source, new RepairParameters());

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(edges.MultiLineString!.LineStrings,
                Has.Length.EqualTo(6));
            Assert.That(union.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(ProjectedArea(union), Is.EqualTo(24.0));
            Assert.That(ProjectedArea(simplified), Is.EqualTo(24.0));
            Assert.That(ProjectedArea(cleaned), Is.EqualTo(24.0));
            Assert.That(operations.ValidatePolygonCoverage(
                simplified, 0.0).IsValid, Is.True);
            Assert.That(operations.ValidatePolygonCoverage(
                cleaned, 0.0).IsValid, Is.True);
        });
    }

    [Test]
    public void OverlapStrategiesSelectTheConfiguredOwner()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((1 0, 5 0, 5 2, 1 2, 1 0)))");
        CoverageMergeStrategy[] strategies =
        [
            CoverageMergeStrategy.KeepFirst,
            CoverageMergeStrategy.KeepLast,
            CoverageMergeStrategy.LargestArea
        ];
        double[] expectedMinimumX = [2.0, 1.0, 1.0];

        CoverageValidationResult diagnostic =
            operations.ValidatePolygonCoverage(source, 0.0);
        int index = 0;
        while (index < strategies.Length)
        {
            RepairParameters parameters = new RepairParameters();
            parameters.CoverageMergeStrategy = strategies[index];

            Geometry result = operations.CleanPolygonCoverage(
                source, parameters);

            Assert.Multiple(() =>
            {
                Assert.That(ProjectedArea(result), Is.EqualTo(10.0),
                    strategies[index].ToString());
                Assert.That(LargestPolygonMinimumX(result),
                    Is.EqualTo(expectedMinimumX[index]),
                    strategies[index].ToString());
                Assert.That(operations.ValidatePolygonCoverage(
                    result, 0.0).IsValid, Is.True,
                    strategies[index].ToString());
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Error,
                Is.EqualTo(ValidationError.CoverageOverlap));
            Assert.That(ProjectedArea(diagnostic.InvalidGeometry!),
                Is.EqualTo(2.0));
        });
    }

    [Test]
    public void GapThresholdAndCleaningAreWidthBounded()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)), " +
            "POLYGON ((4.4 0, 6.4 0, 6.4 2, 4.4 2, 4.4 0)))");
        RepairParameters parameters = new RepairParameters();
        parameters.MaximumGapWidth = 0.2;

        CoverageValidationResult below =
            operations.ValidatePolygonCoverage(source, 0.05);
        CoverageValidationResult included =
            operations.ValidatePolygonCoverage(source, 0.2);
        Geometry result = operations.CleanPolygonCoverage(
            source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(below.IsValid, Is.True);
            Assert.That(included.Error,
                Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(ProjectedArea(result),
                Is.EqualTo(12.2).Within(1.0e-12));
            Assert.That(operations.ValidatePolygonCoverage(
                result, 0.2).IsValid, Is.True);
            Assert.That(operations.ValidatePolygonCoverage(
                result, 0.31).Error,
                Is.EqualTo(ValidationError.CoverageGap));
        });
    }

    [Test]
    public void TJunctionCleaningCreatesMatchingBoundaryNodes()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");

        CoverageValidationResult before =
            operations.ValidatePolygonCoverage(source, 0.0);
        Geometry cleaned = operations.CleanPolygonCoverage(
            source, new RepairParameters());
        CoverageValidationResult after =
            operations.ValidatePolygonCoverage(cleaned, 0.0);
        Geometry union = operations.UnionPolygonCoverage(cleaned);

        Assert.Multiple(() =>
        {
            Assert.That(before.Error,
                Is.EqualTo(ValidationError.NonNodedIntersection));
            Assert.That(before.Coordinate!.X, Is.EqualTo(2.0));
            Assert.That(before.Coordinate.Y, Is.EqualTo(2.0));
            Assert.That(after.IsValid, Is.True);
            Assert.That(CountCoordinate(cleaned, 2.0, 2.0),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(union.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(ProjectedArea(union), Is.EqualTo(12.0));
        });
    }

    [Test]
    public void SharedZigzagSimplifiesConsistentlyOnBothSides()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2.1 1, 1.9 2, " +
            "2.1 3, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 4, 2 4, " +
            "2.1 3, 1.9 2, 2.1 1, 2 0)))");

        Geometry result = operations.SimplifyPolygonCoverage(
            source, SimplifyParametersValue(1.0));
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(result);

        Assert.Multiple(() =>
        {
            Assert.That(components.Polygons, Has.Count.EqualTo(2));
            Assert.That(CountSegment(
                components.Polygons, 2.0, 0.0, 2.0, 4.0),
                Is.EqualTo(2));
            Assert.That(operations.ValidatePolygonCoverage(
                result, 0.0).IsValid, Is.True);
            Assert.That(ProjectedArea(result),
                Is.EqualTo(16.0).Within(1.0e-12));
        });
    }

    [Test]
    public void HoleFillerCoverageUnionsToCompleteShell()
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0), " +
            "(2 2, 2 4, 4 4, 4 2, 2 2)), " +
            "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2)))");

        CoverageValidationResult validation =
            operations.ValidatePolygonCoverage(source, 0.0);
        Geometry edges =
            operations.ExtractPolygonCoverageEdges(source);
        Geometry union = operations.UnionPolygonCoverage(source);
        Geometry simplified = operations.SimplifyPolygonCoverage(
            source, SimplifyParametersValue(0.0));

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(edges.MultiLineString!.LineStrings,
                Has.Length.EqualTo(3));
            Assert.That(union.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(union.Polygon!.Holes, Is.Empty);
            Assert.That(ProjectedArea(union), Is.EqualTo(36.0));
            Assert.That(operations.ValidatePolygonCoverage(
                simplified, 0.0).IsValid, Is.True);
            Assert.That(ProjectedArea(simplified), Is.EqualTo(36.0));
        });
    }

    [TestCase(
        "GEOMETRYCOLLECTION (" +
        "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))")]
    [TestCase(
        "GEOMETRYCOLLECTION (" +
        "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0)), " +
        "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)), " +
        "POLYGON ((0 2, 2 2, 2 4, 0 4, 0 2)), " +
        "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2)))")]
    [TestCase(
        "GEOMETRYCOLLECTION (" +
        "POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0), " +
        "(2 2, 2 4, 4 4, 4 2, 2 2)), " +
        "POLYGON ((2 2, 4 2, 4 4, 2 4, 2 2)), " +
        "POLYGON ((10 0, 12 0, 12 2, 10 2, 10 0)))")]
    public void CoverageUnionAreaEqualsSumOfNonOverlappingComponents(
        string text)
    {
        ICoverageOperations operations = new CoverageOperations();
        Geometry source = Read(text);
        double expected = SumPolygonAreas(source);

        Geometry result = operations.UnionPolygonCoverage(source);

        Assert.Multiple(() =>
        {
            Assert.That(ProjectedArea(result),
                Is.EqualTo(expected).Within(1.0e-12));
            Assert.That(GeometryValidityChecker.ValidateGeometry(result)
                .IsValid, Is.True);
        });
    }

    private static SimplifyParameters SimplifyParametersValue(
        double tolerance)
    {
        SimplifyParameters parameters = new SimplifyParameters();
        parameters.Algorithm =
            SimplificationAlgorithm.VisvalingamWhyatt;
        parameters.Tolerance = tolerance;
        return parameters;
    }

    private static double ProjectedArea(Geometry geometry)
    {
        return GeometryMeasureAlgorithms.GetProjectedArea(geometry);
    }

    private static double SumPolygonAreas(Geometry geometry)
    {
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(geometry);
        double area = 0.0;
        int index = 0;
        while (index < components.Polygons.Count)
        {
            area += ProjectedArea(components.Polygons[index]);
            index++;
        }
        return area;
    }

    private static double LargestPolygonMinimumX(Geometry geometry)
    {
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(geometry);
        double largestArea = -1.0;
        double minimumX = double.NaN;
        int polygonIndex = 0;
        while (polygonIndex < components.Polygons.Count)
        {
            Geometry polygon = components.Polygons[polygonIndex];
            double area = ProjectedArea(polygon);
            if (area > largestArea)
            {
                largestArea = area;
                Coordinate[] coordinates =
                    polygon.Polygon!.Shell.Coordinates;
                minimumX = coordinates[0].X;
                int coordinateIndex = 1;
                while (coordinateIndex < coordinates.Length)
                {
                    if (coordinates[coordinateIndex].X < minimumX)
                    {
                        minimumX = coordinates[coordinateIndex].X;
                    }
                    coordinateIndex++;
                }
            }
            polygonIndex++;
        }
        return minimumX;
    }

    private static int CountCoordinate(
        Geometry geometry, double x, double y)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        int count = 0;
        int index = 0;
        while (index < coordinates.Length)
        {
            if (coordinates[index].X == x && coordinates[index].Y == y)
            {
                count++;
            }
            index++;
        }
        return count;
    }

    private static int CountSegment(
        System.Collections.Generic.List<Geometry> polygons,
        double startX, double startY, double endX, double endY)
    {
        int count = 0;
        int polygonIndex = 0;
        while (polygonIndex < polygons.Count)
        {
            Polygon polygon = polygons[polygonIndex].Polygon!;
            count += CountRingSegment(
                polygon.Shell.Coordinates,
                startX, startY, endX, endY);
            polygonIndex++;
        }
        return count;
    }

    private static int CountRingSegment(
        Coordinate[] coordinates,
        double startX, double startY, double endX, double endY)
    {
        int count = 0;
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            bool forward = coordinates[index].X == startX &&
                coordinates[index].Y == startY &&
                coordinates[index + 1].X == endX &&
                coordinates[index + 1].Y == endY;
            bool reverse = coordinates[index].X == endX &&
                coordinates[index].Y == endY &&
                coordinates[index + 1].X == startX &&
                coordinates[index + 1].Y == startY;
            if (forward || reverse) count++;
            index++;
        }
        return count;
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
