using geo;

namespace geo.Tests;

public class ConcaveHullAlgorithmTests
{
    [Test]
    public void TargetRatioControlsConcavityAndConvexLimit()
    {
        Geometry source = Read(
            "MULTIPOINT ((0 0), (10 0), (10 10), (0 10), " +
            "(3 3), (5 6), (7 3))");

        Geometry tight = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 0.0, false);
        Geometry medium = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 0.5, false);
        Geometry convex = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 1.0, false);

        double tightArea = GeometryMeasureAlgorithms.GetProjectedArea(tight);
        double mediumArea = GeometryMeasureAlgorithms.GetProjectedArea(medium);
        double convexArea = GeometryMeasureAlgorithms.GetProjectedArea(convex);

        Assert.Multiple(() =>
        {
            Assert.That(tight.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(tightArea, Is.LessThanOrEqualTo(mediumArea));
            Assert.That(mediumArea, Is.LessThan(convexArea));
            Assert.That(convexArea, Is.EqualTo(100.0));
            Assert.That(tight.Polygon!.Shell.Coordinates.Length,
                Is.GreaterThan(convex.Polygon!.Shell.Coordinates.Length));
            Assert.That(tight.Polygon.Shell.Coordinates.Length,
                Is.GreaterThanOrEqualTo(
                    medium.Polygon!.Shell.Coordinates.Length));
            Assert.That(AllInputCoordinatesCovered(source, tight), Is.True);
            Assert.That(RingMath.GetExactSignedAreaSign(
                tight.Polygon.Shell.Coordinates), Is.EqualTo(1));
        });
    }

    [Test]
    public void MaximumEdgeLengthProducesNestedDeterministicHulls()
    {
        Geometry source = Read(
            "MULTIPOINT ((0 0), (12 0), (12 12), (0 12), " +
            "(2 3), (4 5), (6 7), (8 5), (10 3))");

        Geometry tight =
            ConcaveHullAlgorithm.GetConcaveHullByMaximumEdgeLength(
                source, 4.0, false);
        Geometry loose =
            ConcaveHullAlgorithm.GetConcaveHullByMaximumEdgeLength(
                source, 20.0, false);
        Geometry repeated =
            ConcaveHullAlgorithm.GetConcaveHullByMaximumEdgeLength(
                source, 4.0, false);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(tight),
                Is.LessThan(GeometryMeasureAlgorithms.GetProjectedArea(loose)));
            Assert.That(loose.Polygon!.Shell.Coordinates,
                Has.Length.EqualTo(5));
            Assert.That(tight.Polygon!.Shell.Coordinates.Length,
                Is.GreaterThan(5));
            Assert.That(AllInputCoordinatesCovered(source, tight), Is.True);
            Assert.That(Write(tight), Is.EqualTo(Write(repeated)));
            Assert.That(IsSimpleRing(tight.Polygon.Shell.Coordinates), Is.True);
        });
    }

    [Test]
    public void OptionalHolesRequireAnInteriorRingWithoutExcludedSites()
    {
        Geometry source = Read(
            "MULTIPOINT ((0 0), (10 0), (10 10), (0 10), " +
            "(4 4), (6 4), (6 6), (4 6))");

        Geometry withoutHoles = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 1.0, false);
        Geometry withHoles = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 1.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(withoutHoles.Polygon!.Holes, Is.Empty);
            Assert.That(withHoles.Polygon!.Holes, Has.Length.EqualTo(1));
            Assert.That(RingMath.GetExactSignedAreaSign(
                withHoles.Polygon.Holes[0].Coordinates), Is.EqualTo(-1));
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(withHoles),
                Is.EqualTo(96.0));
            Assert.That(AllInputCoordinatesCovered(source, withHoles), Is.True);
            Assert.That(GeometryPointLocation.LocatePoint(
                Point(5.0, 5.0), withHoles, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(GeometryPointLocation.LocatePoint(
                Point(2.0, 2.0), withHoles, CurveParameters()),
                Is.EqualTo(TopologicalLocation.Interior));
        });
    }

    [Test]
    public void InteriorSitesPreventAConvexHoleThatWouldExcludeThem()
    {
        Geometry source = Read(
            "MULTIPOINT ((0 0), (10 0), (10 10), (0 10), " +
            "(3 3), (7 3), (7 7), (3 7), (5 5))");

        Geometry hull = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 1.0, true);

        Assert.Multiple(() =>
        {
            Assert.That(hull.Polygon!.Holes, Is.Empty);
            Assert.That(AllInputCoordinatesCovered(source, hull), Is.True);
            Assert.That(GeometryMeasureAlgorithms.GetProjectedArea(hull),
                Is.EqualTo(100.0));
        });
    }

    [Test]
    public void DegenerateInputsUseNarrowestOwningResultsAndKeepMetadata()
    {
        Geometry source = ReadWithSrid(
            "SRID=3857;MULTIPOINT Z ((5 5 9), (1 1 3), (3 3 6), (1 1 2))");
        Geometry empty = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            Read("GEOMETRYCOLLECTION EMPTY"), 0.25, true);

        Geometry line = ConcaveHullAlgorithm.GetConcaveHullByRatio(
            source, 0.25, true);
        source.MultiPoint!.Points[1].Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(line.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(line.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(line.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            AssertCoordinate(line.LineString!.Coordinates[0], 1.0, 1.0);
            Assert.That(line.LineString.Coordinates[0].Z, Is.EqualTo(2.0));
            AssertCoordinate(line.LineString.Coordinates[1], 5.0, 5.0);
            Assert.That(empty.GeometryType,
                Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(GeometryInspection.IsEmptyValue(empty), Is.True);
        });
    }

    [Test]
    public void InvalidCriteriaAreRejected()
    {
        Geometry source = Read("MULTIPOINT ((0 0), (1 0), (0 1))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                ConcaveHullAlgorithm.GetConcaveHullByRatio(
                    source, -0.1, false));
            Assert.Throws<InvalidGeometryException>(() =>
                ConcaveHullAlgorithm.GetConcaveHullByRatio(
                    source, 1.1, false));
            Assert.Throws<InvalidGeometryException>(() =>
                ConcaveHullAlgorithm.GetConcaveHullByRatio(
                    source, double.NaN, false));
            Assert.Throws<InvalidGeometryException>(() =>
                ConcaveHullAlgorithm.GetConcaveHullByMaximumEdgeLength(
                    source, 0.0, false));
            Assert.Throws<InvalidGeometryException>(() =>
                ConcaveHullAlgorithm.GetConcaveHullByMaximumEdgeLength(
                    source, double.PositiveInfinity, false));
        });
    }

    private static bool AllInputCoordinatesCovered(
        Geometry source, Geometry hull)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(source);
        int index = 0;
        while (index < coordinates.Length)
        {
            if (GeometryPointLocation.LocatePoint(
                coordinates[index], hull, CurveParameters()) ==
                    TopologicalLocation.Exterior)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    private static bool IsSimpleRing(Coordinate[] ring)
    {
        List<Coordinate> open = new List<Coordinate>();
        int index = 0;
        while (index + 1 < ring.Length)
        {
            open.Add(ring[index]);
            index++;
        }
        return ConcaveHullAlgorithm.IsSimpleRing(open);
    }

    private static CurveLinearizationParameters CurveParameters()
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumAngle,
            Tolerance = Math.PI / 2.0,
            MaximumSegments = 16
        };
    }

    private static Coordinate Point(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y)
    {
        Assert.That(coordinate.X, Is.EqualTo(x));
        Assert.That(coordinate.Y, Is.EqualTo(y));
    }

    private static string Write(Geometry geometry)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.WriteWkt(geometry);
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
