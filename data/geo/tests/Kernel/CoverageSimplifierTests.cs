using geo;

namespace geo.Tests;

public class CoverageSimplifierTests
{
    [Test]
    public void SharedZigzagIsSimplifiedOnceAndReusedByBothPolygons()
    {
        Geometry source = ZigzagCoverage();
        GeometryRepairComponents sourceComponents =
            GeometryRepairEngine.CollectRepairComponents(source);
        Assert.That(CoverageValidator.BoundariesCrossProperly(
            sourceComponents.Polygons[0], sourceComponents.Polygons[1]),
            Is.False);
        Polygon firstPolygon = (Polygon)GeometryDispatch.GetSelectedPayload(
            sourceComponents.Polygons[0]);
        Polygon secondPolygon = (Polygon)GeometryDispatch.GetSelectedPayload(
            sourceComponents.Polygons[1]);
        Assert.That(CoverageValidator.PolygonBoundaryHasInteriorSample(
            firstPolygon, secondPolygon), Is.False);
        Assert.That(CoverageValidator.PolygonBoundaryHasInteriorSample(
            secondPolygon, firstPolygon), Is.False);

        Geometry result = CoverageSimplifier.SimplifyCoverage(
            source, Parameters(1.0));
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(components.Polygons, Has.Count.EqualTo(2));
            Assert.That(CountSegment(
                components.Polygons, 2.0, 0.0, 2.0, 4.0),
                Is.EqualTo(2));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0).Within(1.0e-12));
            Assert.That(CoverageValidator.ValidateCoverage(
                result, 0.0).IsValid, Is.True);
        });
    }

    [Test]
    public void ExteriorRingSimplificationPreservesMetadataAndOwnership()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;POLYGON ((0 0, 2 0, 4 0, " +
            "4 2, 4 4, 0 4, 0 0))");

        Geometry result = CoverageSimplifier.SimplifyCoverage(
            source, Parameters(0.0));
        source.Polygon!.Shell.Coordinates[0].X = 99.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(result.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(result.Polygon.Shell.Coordinates[0].X,
                Is.Not.EqualTo(99.0));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(16.0));
        });
    }

    [Test]
    public void ShellAndHoleEdgesSimplifyWithoutChangingTopology()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 5 0, 10 0, 10 5, 10 10, " +
            "5 10, 0 10, 0 5, 0 0), " +
            "(3 3, 3 5, 3 7, 5 7, 7 7, 7 5, " +
            "7 3, 5 3, 3 3))");

        Geometry result = CoverageSimplifier.SimplifyCoverage(
            source, Parameters(0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(result.Polygon.Holes, Has.Length.EqualTo(1));
            Assert.That(result.Polygon.Holes[0].Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(result),
                Is.EqualTo(84.0));
            Assert.That(GeometryValidityChecker.ValidateGeometry(result)
                .IsValid, Is.True);
        });
    }

    [Test]
    public void PointTouchJunctionRemainsFixedAcrossSimplification()
    {
        Geometry source = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 1 0, 2 0, 2 2, 0 2, 0 0)), " +
            "POLYGON ((2 2, 3 2, 4 2, 4 4, 2 4, 2 2)))");

        Geometry result = CoverageSimplifier.SimplifyCoverage(
            source, Parameters(10.0));
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(result);

        Assert.Multiple(() =>
        {
            Assert.That(components.Polygons, Has.Count.EqualTo(2));
            Assert.That(CountCoordinate(
                components.Polygons, 2.0, 2.0), Is.EqualTo(2));
            Assert.That(CoverageValidator.ValidateCoverage(
                result, 0.0).IsValid, Is.True);
        });
    }

    [Test]
    public void SimplifiedLineworkMustRemainFullyNoded()
    {
        Geometry crossing = Read(
            "MULTILINESTRING ((0 0, 2 2), (0 2, 2 0))");
        Geometry noded = Read(
            "MULTILINESTRING ((0 0, 1 1), (1 1, 2 0))");

        Assert.Multiple(() =>
        {
            Assert.That(CoverageSimplifier.LineworkIsFullyNoded(crossing),
                Is.False);
            Assert.That(CoverageSimplifier.LineworkIsFullyNoded(noded),
                Is.True);
        });
    }

    [Test]
    public void EmptyCoverageProducesAnEmptyPolygonWithSourceLayout()
    {
        Geometry source = Read("MULTIPOLYGON M EMPTY");

        Geometry result = CoverageSimplifier.SimplifyCoverage(
            source, Parameters(2.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYM));
            Assert.That(GeometryInspection.IsEmptyValue(result), Is.True);
        });
    }

    [Test]
    public void InvalidCoverageAndParametersAreRejected()
    {
        Geometry overlap = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 3 0, 3 3, 0 3, 0 0)), " +
            "POLYGON ((2 0, 5 0, 5 3, 2 3, 2 0)))");
        Geometry nonNoded = Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 2, 2 2, 2 0)))");
        SimplifyParameters wrong = Parameters(1.0);
        wrong.Algorithm = SimplificationAlgorithm.DouglasPeucker;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageSimplifier.SimplifyCoverage(
                    overlap, Parameters(1.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageSimplifier.SimplifyCoverage(
                    nonNoded, Parameters(1.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageSimplifier.SimplifyCoverage(
                    Read("POLYGON ((0 0, 2 0, 0 2, 0 0))"),
                    Parameters(-1.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                CoverageSimplifier.SimplifyCoverage(
                    Read("POLYGON ((0 0, 2 0, 0 2, 0 0))"),
                    wrong));
        });
    }

    private static Geometry ZigzagCoverage()
    {
        return Read(
            "GEOMETRYCOLLECTION (" +
            "POLYGON ((0 0, 2 0, 2.1 1, 1.9 2, " +
            "2.1 3, 2 4, 0 4, 0 0)), " +
            "POLYGON ((2 0, 4 0, 4 4, 2 4, " +
            "2.1 3, 1.9 2, 2.1 1, 2 0)))");
    }

    private static SimplifyParameters Parameters(double tolerance)
    {
        return new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.VisvalingamWhyatt,
            Tolerance = tolerance,
            PreserveEndpoints = false
        };
    }

    private static int CountSegment(
        System.Collections.Generic.List<Geometry> polygons,
        double startX, double startY, double endX, double endY)
    {
        int count = 0;
        int polygonIndex = 0;
        while (polygonIndex < polygons.Count)
        {
            Polygon polygon = (Polygon)GeometryDispatch.GetSelectedPayload(
                polygons[polygonIndex]);
            count += CountRingSegment(
                polygon.Shell.Coordinates,
                startX, startY, endX, endY);
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                count += CountRingSegment(
                    polygon.Holes[holeIndex].Coordinates,
                    startX, startY, endX, endY);
                holeIndex++;
            }
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

    private static int CountCoordinate(
        System.Collections.Generic.List<Geometry> polygons,
        double x, double y)
    {
        int count = 0;
        int polygonIndex = 0;
        while (polygonIndex < polygons.Count)
        {
            Polygon polygon = (Polygon)GeometryDispatch.GetSelectedPayload(
                polygons[polygonIndex]);
            int coordinateIndex = 0;
            while (coordinateIndex < polygon.Shell.Coordinates.Length)
            {
                Coordinate coordinate =
                    polygon.Shell.Coordinates[coordinateIndex];
                if (coordinate.X == x && coordinate.Y == y) count++;
                coordinateIndex++;
            }
            polygonIndex++;
        }
        return count;
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
