using geo;

namespace geo.Tests;

public class GeometryRepairEngineTests
{
    [Test]
    public void MakeValidStrategiesApplyLineworkAndStructureSemantics()
    {
        Geometry overlapping = Read(
            "MULTIPOLYGON (" +
            "((0 0, 4 0, 4 4, 0 4, 0 0)), " +
            "((2 0, 6 0, 6 4, 2 4, 2 0)))");

        Geometry linework = GeometryRepairEngine.MakeValid(
            overlapping, Parameters(RepairMethod.Linework, false));
        Geometry structure = GeometryRepairEngine.MakeValid(
            overlapping, Parameters(RepairMethod.Structure, false));

        Assert.Multiple(() =>
        {
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                linework).IsValid, Is.True);
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                structure).IsValid, Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(linework),
                Is.EqualTo(16.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(structure),
                Is.EqualTo(24.0));
        });
    }

    [Test]
    public void OutsideHolesArePromotedToPolygonalAreaByBothStrategies()
    {
        Geometry outsideHole = Read(
            "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), " +
            "(6 0, 6 1, 7 1, 7 0, 6 0))");

        Geometry linework = GeometryRepairEngine.MakeValid(
            outsideHole, Parameters(RepairMethod.Linework, false));
        Geometry structure = GeometryRepairEngine.MakeValid(
            outsideHole, Parameters(RepairMethod.Structure, false));

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(linework),
                Is.EqualTo(17.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(structure),
                Is.EqualTo(17.0));
            Assert.That(linework.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(structure.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
        });
    }

    [Test]
    public void CoordinateCleanupRemovesNonFiniteAndRepeatedPositions()
    {
        Geometry source = RawLine([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(0.0, 0.0),
            CoordinateValue(double.NaN, 5.0),
            CoordinateValue(2.0, 2.0)
        ]);
        RepairParameters parameters =
            Parameters(RepairMethod.Linework, false);

        Geometry cleaned = GeometryRepairEngine.CleanCoordinates(
            source, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(cleaned.LineString!.Coordinates, Has.Length.EqualTo(2));
            Assert.That(cleaned.LineString.Coordinates[0].X,
                Is.EqualTo(0.0));
            Assert.That(cleaned.LineString.Coordinates[1].X,
                Is.EqualTo(2.0));
            Assert.That(source.LineString!.Coordinates,
                Has.Length.EqualTo(4));
            Assert.That(double.IsNaN(
                source.LineString.Coordinates[2].X), Is.True);
        });

        parameters.RemoveNonFiniteCoordinates = false;
        Assert.Throws<InvalidGeometryException>(() =>
            GeometryRepairEngine.CleanCoordinates(source, parameters));
    }

    [Test]
    public void RingRepairClosesAndRebuildsAnOpenShell()
    {
        Geometry source = RawPolygon([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(0.0, 4.0)
        ]);

        Geometry repaired = GeometryRepairEngine.RepairRings(
            source, Parameters(RepairMethod.Linework, false));

        Assert.Multiple(() =>
        {
            Assert.That(repaired.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(repaired.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(repaired),
                Is.EqualTo(16.0));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                repaired).IsValid, Is.True);
        });
    }

    [Test]
    public void CollapsedResultPolicyDropsOrRetainsLowerDimension()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 1 0, 2 0, 0 0))");

        Geometry dropped = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, false));
        Geometry retained = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, true));

        Assert.Multiple(() =>
        {
            Assert.That(dropped.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(dropped), Is.True);
            Assert.That(retained.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(retained.LineString!.Coordinates,
                Has.Length.EqualTo(3));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                retained).IsValid, Is.True);
        });
    }

    [Test]
    public void MakeValidReturnsNarrowestMixedResultAndPreservesMetadata()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 2, 0 2, 2 0, 0 0)), " +
            "LINESTRING (5 0, 6 0), POINT (9 9))");

        Geometry repaired = GeometryRepairEngine.MakeValid(
            source, Parameters(RepairMethod.Linework, false));

        Assert.Multiple(() =>
        {
            Assert.That(repaired.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(repaired.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(repaired.GeometryCollection!.Geometries,
                Has.Length.EqualTo(4));
            Assert.That(GeometryValidityChecker.ValidateGeometry(
                repaired).IsValid, Is.True);
        });
    }

    [Test]
    public void CoverageCleaningHonorsOwnershipAndClosesSmallGaps()
    {
        Geometry overlap = Read(
            "MULTIPOLYGON (" +
            "((0 0, 4 0, 4 4, 0 4, 0 0)), " +
            "((2 0, 6 0, 6 4, 2 4, 2 0)))");
        RepairParameters keepFirst =
            Parameters(RepairMethod.Structure, false);
        RepairParameters keepLast =
            Parameters(RepairMethod.Structure, false);
        keepLast.CoverageMergeStrategy = CoverageMergeStrategy.KeepLast;

        Geometry firstResult = GeometryRepairEngine.CleanCoverage(
            overlap, keepFirst);
        Geometry lastResult = GeometryRepairEngine.CleanCoverage(
            overlap, keepLast);

        Geometry gap = Read(
            "MULTIPOLYGON (" +
            "((0 0, 2 0, 2 2, 0 2, 0 0)), " +
            "((2.1 0, 4.1 0, 4.1 2, 2.1 2, 2.1 0)))");
        keepFirst.MaximumGapWidth = 0.2;
        Geometry closedGap = GeometryRepairEngine.CleanCoverage(
            gap, keepFirst);

        Assert.Multiple(() =>
        {
            Assert.That(LargestPolygonMinimumX(firstResult),
                Is.EqualTo(0.0));
            Assert.That(LargestPolygonMinimumX(lastResult),
                Is.EqualTo(2.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                firstResult), Is.EqualTo(24.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                lastResult), Is.EqualTo(24.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(
                closedGap), Is.EqualTo(8.2).Within(1.0e-12));
        });
    }

    [Test]
    public void RepairRejectsInvalidParameters()
    {
        Geometry source = Read("POINT (0 0)");
        RepairParameters parameters =
            Parameters(RepairMethod.Linework, false);
        parameters.SnapTolerance = double.NaN;

        Assert.Throws<InvalidGeometryException>(() =>
            GeometryRepairEngine.MakeValid(source, parameters));
    }

    private static double LargestPolygonMinimumX(Geometry geometry)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        double largestArea = -1.0;
        double minimumX = double.NaN;
        int index = 0;
        while (index < leaves.Length)
        {
            if (leaves[index].GeometryType == GeometryType.Polygon)
            {
                double area = GeometryMeasureAlgorithms.GetProjectedArea(
                    leaves[index]);
                if (area > largestArea)
                {
                    largestArea = area;
                    Coordinate[] coordinates =
                        leaves[index].Polygon!.Shell.Coordinates;
                    minimumX = coordinates.Min(coordinate => coordinate.X);
                }
            }
            index++;
        }
        return minimumX;
    }

    private static RepairParameters Parameters(
        RepairMethod method, bool keepCollapsed)
    {
        return new RepairParameters
        {
            Method = method,
            KeepCollapsedComponents = keepCollapsed
        };
    }

    private static Geometry RawLine(Coordinate[] coordinates)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.LineString;
        geometry.LineString = new LineString { Coordinates = coordinates };
        return geometry;
    }

    private static Geometry RawPolygon(Coordinate[] coordinates)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.Polygon;
        geometry.Polygon = new Polygon
        {
            Shell = new LinearRing { Coordinates = coordinates }
        };
        return geometry;
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static Geometry ReadWithSrid(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
