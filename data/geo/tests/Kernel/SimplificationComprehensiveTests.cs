using geo;

namespace geo.Tests;

public class SimplificationComprehensiveTests
{
    [TestCase(SimplificationAlgorithm.DouglasPeucker)]
    [TestCase(SimplificationAlgorithm.TopologyPreserving)]
    [TestCase(SimplificationAlgorithm.VisvalingamWhyatt)]
    public void HigherToleranceNeverAddsLineVertices(
        SimplificationAlgorithm algorithm)
    {
        Geometry source = Read(
            "LINESTRING (0 0, 1 0.05, 2 0, 3 0.2, " +
            "4 0, 5 1, 6 1.05, 7 1)");

        Geometry low = Simplify(source, algorithm, 0.01, true);
        Geometry high = Simplify(source, algorithm, 0.3, true);

        Assert.Multiple(() =>
        {
            Assert.That(low.LineString!.Coordinates.Length,
                Is.LessThanOrEqualTo(source.LineString!.Coordinates.Length));
            Assert.That(high.LineString!.Coordinates.Length,
                Is.LessThanOrEqualTo(low.LineString.Coordinates.Length));
            AssertCoordinate(high.LineString.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(
                high.LineString.Coordinates[
                    high.LineString.Coordinates.Length - 1], 7.0, 1.0);
        });
    }

    [TestCase(SimplificationAlgorithm.DouglasPeucker)]
    [TestCase(SimplificationAlgorithm.TopologyPreserving)]
    [TestCase(SimplificationAlgorithm.VisvalingamWhyatt)]
    public void SimplificationIsStableWhenRepeated(
        SimplificationAlgorithm algorithm)
    {
        Geometry source = Read(
            "MULTILINESTRING ((0 0, 1 0.05, 2 0, 3 2), " +
            "(3 2, 4 2.05, 5 2))");

        Geometry first = Simplify(source, algorithm, 0.1, true);
        Geometry second = Simplify(first, algorithm, 0.1, true);

        Assert.That(GeometryEquality.AreExactlyEqual(first, second),
            Is.True);
    }

    [TestCase(SimplificationAlgorithm.DouglasPeucker)]
    [TestCase(SimplificationAlgorithm.TopologyPreserving)]
    [TestCase(SimplificationAlgorithm.VisvalingamWhyatt)]
    public void PreservedEndpointsKeepExactZmOrdinates(
        SimplificationAlgorithm algorithm)
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;LINESTRING ZM " +
            "(0 0 10 100, 1 0.01 20 200, 2 0 30 300)");

        Geometry result = Simplify(
            source, algorithm, 1.0, true);
        Coordinate[] coordinates = result.LineString!.Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(coordinates, Has.Length.EqualTo(2));
            AssertCoordinate(
                coordinates[0], 0.0, 0.0, 10.0, 100.0);
            AssertCoordinate(
                coordinates[1], 2.0, 0.0, 30.0, 300.0);
            Assert.That(result.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(3857));
        });

        source.LineString!.Coordinates[0].Z = 999.0;
        Assert.That(coordinates[0].Z, Is.EqualTo(10.0));
    }

    [TestCase(SimplificationAlgorithm.DouglasPeucker)]
    [TestCase(SimplificationAlgorithm.TopologyPreserving)]
    [TestCase(SimplificationAlgorithm.VisvalingamWhyatt)]
    public void PolygonShellAndHoleRemainClosedSimpleAndNested(
        SimplificationAlgorithm algorithm)
    {
        Geometry source = Read(
            "POLYGON ((0 0, 2 0.05, 4 0, 4 4, " +
            "2 3.95, 0 4, 0 0), " +
            "(1 1, 1 3, 2 2.95, 3 3, 3 1, 2 1.05, 1 1))");

        Geometry result = Simplify(
            source, algorithm, 0.1, true);
        Coordinate[] shell = result.Polygon!.Shell.Coordinates;
        Coordinate[] hole = result.Polygon.Holes[0].Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(shell.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(hole.Length, Is.GreaterThanOrEqualTo(4));
            AssertClosed(shell);
            AssertClosed(hole);
            Assert.That(VisvalingamWhyatt.ClosedRingIsSimple(shell),
                Is.True);
            Assert.That(VisvalingamWhyatt.ClosedRingIsSimple(hole),
                Is.True);
            Assert.That(RingPointLocation.LocatePointByWinding(
                hole[0], shell), Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(new GeometryMeasures().Area(result),
                Is.GreaterThan(0.0));
        });
    }

    [Test]
    public void ZeroToleranceResultsAreStableAcrossAlgorithms()
    {
        Geometry source = Read(
            "LINESTRING (0 0, 1 0, 1 0, 2 0, 3 1)");
        SimplificationAlgorithm[] algorithms =
        [
            SimplificationAlgorithm.DouglasPeucker,
            SimplificationAlgorithm.TopologyPreserving,
            SimplificationAlgorithm.VisvalingamWhyatt
        ];

        foreach (SimplificationAlgorithm algorithm in algorithms)
        {
            Geometry first = Simplify(
                source, algorithm, 0.0, true);
            Geometry second = Simplify(
                first, algorithm, 0.0, true);
            Assert.That(GeometryEquality.AreExactlyEqual(first, second),
                Is.True, algorithm.ToString());
            AssertCoordinate(first.LineString!.Coordinates[0], 0.0, 0.0);
            AssertCoordinate(
                first.LineString.Coordinates[
                    first.LineString.Coordinates.Length - 1], 3.0, 1.0);
        }
    }

    [Test]
    public void DensificationAtTheMaximumIsIdempotent()
    {
        Geometry source = ReadWithSrid(
            "SRID=4326;LINESTRING ZM " +
            "(0 0 0 0, 10 0 20 100, 10 5 30 150)");
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Floating
        };

        Geometry first = Densifier.DensifyGeometry(
            source, 2.5, precision);
        Geometry second = Densifier.DensifyGeometry(
            first, 2.5, precision);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(first, second),
                Is.True);
            Assert.That(first.LineString!.Coordinates,
                Has.Length.EqualTo(7));
            AssertMaximumSegmentLength(
                first.LineString.Coordinates, 2.5);
            AssertCoordinate(
                first.LineString.Coordinates[2],
                5.0, 0.0, 10.0, 50.0);
        });
    }

    [Test]
    public void DensificationInterpolatesZmOnShellAndHoleEdges()
    {
        Geometry source = Read(
            "POLYGON ZM ((0 0 0 0, 4 0 4 40, 4 4 8 80, " +
            "0 4 12 120, 0 0 0 0), " +
            "(1 1 2 20, 1 3 4 40, 3 3 6 60, " +
            "3 1 8 80, 1 1 2 20))");

        Geometry result = Densifier.DensifyGeometry(
            source, 1.5, new PrecisionModel());
        Coordinate[] shell = result.Polygon!.Shell.Coordinates;
        Coordinate[] hole = result.Polygon.Holes[0].Coordinates;

        Assert.Multiple(() =>
        {
            Assert.That(shell, Has.Length.EqualTo(13));
            Assert.That(hole, Has.Length.EqualTo(9));
            AssertClosed(shell);
            AssertClosed(hole);
            AssertCoordinate(
                shell[1], 4.0 / 3.0, 0.0,
                4.0 / 3.0, 40.0 / 3.0);
            AssertCoordinate(hole[1], 1.0, 2.0, 3.0, 30.0);
            AssertMaximumSegmentLength(shell, 1.5);
            AssertMaximumSegmentLength(hole, 1.5);
        });
    }

    [Test]
    public void PolygonHullBoundaryRatiosHaveStableContainment()
    {
        Geometry source = Read(
            "POLYGON ((0 0, 2 1, 4 0, 3 2, " +
            "4 4, 2 3, 0 4, 1 2, 0 0))");

        Geometry unchanged = PolygonHullSimplifier
            .SimplifyPolygonHull(source, 0.0, true);
        Geometry outer = PolygonHullSimplifier
            .SimplifyPolygonHull(source, 1.0, true);
        Geometry inner = PolygonHullSimplifier
            .SimplifyPolygonHull(source, 1.0, false);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryEquality.AreExactlyEqual(
                source, unchanged), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Covers(
                outer, source), Is.True);
            Assert.That(GeometryPredicateAlgorithms.Covers(
                source, inner), Is.True);
            Assert.That(VisvalingamWhyatt.ClosedRingIsSimple(
                outer.Polygon!.Shell.Coordinates), Is.True);
            Assert.That(VisvalingamWhyatt.ClosedRingIsSimple(
                inner.Polygon!.Shell.Coordinates), Is.True);
        });
    }

    [Test]
    public void PublicServiceRejectsEveryInvalidParameterFamily()
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry line = Read("LINESTRING (0 0, 1 0, 2 0)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))");
        SimplifyParameters unknown = Parameters(
            (SimplificationAlgorithm)99, 1.0, true);
        SimplifyParameters negativeDouglas = Parameters(
            SimplificationAlgorithm.DouglasPeucker, -1.0, true);
        SimplifyParameters infiniteTopology = Parameters(
            SimplificationAlgorithm.TopologyPreserving,
            double.PositiveInfinity, true);
        SimplifyParameters nanVisvalingam = Parameters(
            SimplificationAlgorithm.VisvalingamWhyatt,
            double.NaN, true);
        SimplifyParameters excessiveHull = Parameters(
            SimplificationAlgorithm.PolygonHull, 1.01, true);
        PrecisionModel invalidPrecision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 0.0
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Simplify(line, null!));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Simplify(line, unknown));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Simplify(line, negativeDouglas));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Simplify(line, infiniteTopology));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Simplify(line, nanVisvalingam));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.SimplifyPolygonHull(
                    polygon, excessiveHull, true));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Densify(line, 0.0, new PrecisionModel()));
            Assert.Throws<InvalidGeometryException>(() =>
                simplifier.Densify(
                    line, double.PositiveInfinity,
                    new PrecisionModel()));
            Assert.Throws<PrecisionException>(() =>
                simplifier.Densify(line, 1.0, invalidPrecision));
        });
    }

    private static Geometry Simplify(
        Geometry geometry, SimplificationAlgorithm algorithm,
        double tolerance, bool preserveEndpoints)
    {
        IGeometrySimplifier simplifier = new GeometrySimplifier();
        return simplifier.Simplify(
            geometry,
            Parameters(algorithm, tolerance, preserveEndpoints));
    }

    private static SimplifyParameters Parameters(
        SimplificationAlgorithm algorithm, double tolerance,
        bool preserveEndpoints)
    {
        return new SimplifyParameters
        {
            Algorithm = algorithm,
            Tolerance = tolerance,
            PreserveEndpoints = preserveEndpoints
        };
    }

    private static void AssertClosed(Coordinate[] coordinates)
    {
        Assert.That(coordinates[0].X,
            Is.EqualTo(coordinates[coordinates.Length - 1].X));
        Assert.That(coordinates[0].Y,
            Is.EqualTo(coordinates[coordinates.Length - 1].Y));
    }

    private static void AssertMaximumSegmentLength(
        Coordinate[] coordinates, double maximum)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                coordinates[index], coordinates[index + 1]),
                Is.LessThanOrEqualTo(maximum));
            index++;
        }
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y,
        double z = double.NaN, double m = double.NaN)
    {
        Assert.That(coordinate.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(coordinate.Y, Is.EqualTo(y).Within(1e-12));
        if (!double.IsNaN(z))
        {
            Assert.That(coordinate.Z, Is.EqualTo(z).Within(1e-12));
        }
        if (!double.IsNaN(m))
        {
            Assert.That(coordinate.M, Is.EqualTo(m).Within(1e-12));
        }
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
