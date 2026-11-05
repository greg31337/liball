using geo;

namespace geo.Tests;

public class DistanceAlgorithmTests
{
    [Test]
    public void PointDistanceReturnsOwningWitnessCopies()
    {
        Geometry first = Read("POINT ZM (1 2 3 4)");
        Geometry second = Read("POINT ZM (4 6 7 8)");

        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            first, second);
        first.Point!.Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(5.0));
            AssertCoordinate(result.FirstCoordinate, 1.0, 2.0, 3.0, 4.0);
            AssertCoordinate(result.SecondCoordinate, 4.0, 6.0, 7.0, 8.0);
            Assert.That(result.FirstLocation, Is.Null);
            Assert.That(result.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void PointToMultiLineReportsComponentSegmentAndFraction()
    {
        Geometry point = Read("POINT Z (3 4 99)");
        Geometry lines = Read(
            "MULTILINESTRING Z ((100 100 1, 101 100 2), (0 0 10, 10 0 30))");

        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            point, lines);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(4.0));
            AssertCoordinate(result.FirstCoordinate,
                3.0, 4.0, 99.0, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                3.0, 0.0, 16.0, double.NaN);
            Assert.That(result.FirstLocation, Is.Null);
            AssertLocation(result.SecondLocation, 1, 0, 0.3);
        });
    }

    [Test]
    public void SegmentPairLocationsCountEmptyMultiLineComponents()
    {
        Geometry first = Read(
            "MULTILINESTRING (EMPTY, (0 0, 4 0))");
        Geometry second = Read(
            "MULTILINESTRING (EMPTY, (6 3, 6 -3))");

        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(2.0));
            AssertCoordinate(result.FirstCoordinate,
                4.0, 0.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                6.0, 0.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 1, 0, 1.0);
            AssertLocation(result.SecondLocation, 1, 0, 0.5);
        });
    }

    [Test]
    public void IntersectingGeometrySegmentsReportBothWitnessLocations()
    {
        Geometry first = Read(
            "MULTILINESTRING Z ((20 20 1, 21 20 1), (0 0 0, 10 10 10))");
        Geometry second = Read(
            "MULTILINESTRING Z ((30 30 2, 31 30 2), (0 10 100, 10 0 200))");

        DistanceResult result = DistanceAlgorithm.GetIndexedMinimumDistance(
            first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.Zero);
            AssertCoordinate(result.FirstCoordinate,
                5.0, 5.0, 5.0, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                5.0, 5.0, 150.0, double.NaN);
            AssertLocation(result.FirstLocation, 1, 0, 0.5);
            AssertLocation(result.SecondLocation, 1, 0, 0.5);
        });
    }

    [Test]
    public void PointInsidePolygonHasZeroDistanceWithIndependentOrdinates()
    {
        Geometry point = Read("POINT Z (5 5 7)");
        Geometry polygon = Read(
            "POLYGON Z ((0 0 10, 10 0 10, 10 10 10, 0 10 10, 0 0 10))");

        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            point, polygon);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.Zero);
            AssertCoordinate(result.FirstCoordinate,
                5.0, 5.0, 7.0, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                5.0, 5.0, double.NaN, double.NaN);
            Assert.That(result.FirstLocation, Is.Null);
            Assert.That(result.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void PointInsideHoleMeasuresToHoleBoundary()
    {
        Geometry point = Read("POINT (5 5)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (4 4, 6 4, 6 6, 4 6, 4 4))");

        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            point, polygon);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(1.0));
            AssertCoordinate(result.FirstCoordinate,
                5.0, 5.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                5.0, 4.0, double.NaN, double.NaN);
            AssertLocation(result.SecondLocation, 1, 0, 0.5);
        });
    }

    [Test]
    public void ContainedLineHasZeroDistanceAndKeepsItsLinearLocation()
    {
        Geometry line = Read("LINESTRING (2 2, 8 2)");
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");

        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            line, polygon);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.Zero);
            AssertCoordinate(result.FirstCoordinate,
                2.0, 2.0, double.NaN, double.NaN);
            AssertCoordinate(result.SecondCoordinate,
                2.0, 2.0, double.NaN, double.NaN);
            AssertLocation(result.FirstLocation, 0, 0, 0.0);
            Assert.That(result.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void IndexedAndBruteForcePathsReturnIdenticalResults()
    {
        string[] firstValues =
        [
            "POINT (3 4)",
            "LINESTRING (0 0, 4 0, 4 8)",
            "MULTILINESTRING ((0 0, 2 2), (10 0, 10 10))",
            "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0), (2 2, 6 2, 6 6, 2 6, 2 2))",
            "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((20 0, 24 0, 24 4, 20 4, 20 0)))"
        ];
        string[] secondValues =
        [
            "LINESTRING (0 0, 10 0)",
            "POINT (8 3)",
            "MULTILINESTRING ((3 0, 3 5), (14 2, 14 8))",
            "POINT (4 4)",
            "POLYGON ((3 0, 8 0, 8 5, 3 5, 3 0))"
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < firstValues.Length)
            {
                DistanceResult brute = DistanceAlgorithm.GetMinimumDistance(
                    Read(firstValues[index]), Read(secondValues[index]));
                DistanceResult indexed =
                    DistanceAlgorithm.GetIndexedMinimumDistance(
                        Read(firstValues[index]), Read(secondValues[index]));
                AssertEquivalent(brute, indexed);
                index++;
            }
        });
    }

    [Test]
    public void ExtremeMagnitudeProjectionRemainsFiniteWhenDistanceIsFinite()
    {
        Geometry point = new Geometry
        {
            Point = new Point
            {
                Coordinate = CoordinateValue(0.0, 2.0)
            }
        };
        Geometry line = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates =
                [
                    CoordinateValue(-double.MaxValue, 0.0),
                    CoordinateValue(double.MaxValue, 0.0)
                ]
            }
        };

        DistanceResult brute = DistanceAlgorithm.GetMinimumDistance(
            point, line);
        DistanceResult indexed = DistanceAlgorithm.GetIndexedMinimumDistance(
            point, line);

        Assert.Multiple(() =>
        {
            Assert.That(brute.Distance, Is.EqualTo(2.0));
            AssertCoordinate(brute.SecondCoordinate,
                0.0, 0.0, double.NaN, double.NaN);
            AssertLocation(brute.SecondLocation, 0, 0, 0.5);
            AssertEquivalent(brute, indexed);
        });
    }

    [Test]
    public void SegmentSpatialIndexFiltersAndRestoresSourceOrder()
    {
        DistanceSegmentFacet first = SegmentFacet(10.0, 12.0, 0);
        DistanceSegmentFacet second = SegmentFacet(0.0, 2.0, 1);
        DistanceSegmentFacet third = SegmentFacet(5.0, 7.0, 2);
        SegmentSpatialIndex index = new SegmentSpatialIndex(
            [first, second, third]);

        DistanceSegmentFacet[] matches = index.Query(
            EnvelopeAlgorithms.CreateBoundsEnvelope(1.0, -1.0, 11.0, 1.0));
        DistanceSegmentFacet[] all = index.GetAllSegments();

        Assert.Multiple(() =>
        {
            Assert.That(matches, Is.EqualTo(new[] { first, second, third }));
            Assert.That(all, Is.EqualTo(new[] { first, second, third }));
            Assert.That(index.Query(
                EnvelopeAlgorithms.CreateBoundsEnvelope(
                    3.0, -1.0, 4.0, 1.0)), Is.Empty);
        });
    }

    [Test]
    public void EmptyInvalidSridAndDeferredGeometryTypesAreHandled()
    {
        Geometry empty = Read("GEOMETRYCOLLECTION EMPTY");
        Geometry point = Read("POINT (1 2)");
        Geometry firstSrid = Read("POINT (0 0)");
        Geometry secondSrid = Read("POINT (0 0)");
        firstSrid.SpatialReferenceId = 100;
        secondSrid.SpatialReferenceId = 200;
        Geometry triangle = Read("TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
            empty, point);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.False);
            Assert.That(result.Distance, Is.EqualTo(double.PositiveInfinity));
            Assert.That(result.FirstCoordinate, Is.Null);
            Assert.That(result.SecondCoordinate, Is.Null);
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.GetMinimumDistance(firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.GetIndexedMinimumDistance(triangle, point));
            Assert.Throws<InvalidGeometryException>(() =>
                new SegmentSpatialIndex(null!));
        });
    }

    private static DistanceSegmentFacet SegmentFacet(
        double startX, double endX, int order)
    {
        return new DistanceSegmentFacet(
            CoordinateValue(startX, 0.0), CoordinateValue(endX, 0.0),
            0, order, order);
    }

    private static void AssertEquivalent(
        DistanceResult expected, DistanceResult actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.HasResult, Is.EqualTo(expected.HasResult));
            Assert.That(actual.Distance, Is.EqualTo(expected.Distance));
            AssertOptionalCoordinate(expected.FirstCoordinate,
                actual.FirstCoordinate);
            AssertOptionalCoordinate(expected.SecondCoordinate,
                actual.SecondCoordinate);
            AssertOptionalLocation(expected.FirstLocation,
                actual.FirstLocation);
            AssertOptionalLocation(expected.SecondLocation,
                actual.SecondLocation);
        });
    }

    private static void AssertOptionalCoordinate(
        Coordinate? expected, Coordinate? actual)
    {
        if (expected == null)
        {
            Assert.That(actual, Is.Null);
            return;
        }
        AssertCoordinate(actual, expected.X, expected.Y, expected.Z, expected.M);
    }

    private static void AssertOptionalLocation(
        LinearLocation? expected, LinearLocation? actual)
    {
        if (expected == null)
        {
            Assert.That(actual, Is.Null);
            return;
        }
        AssertLocation(actual, expected.ComponentIndex,
            expected.SegmentIndex, expected.SegmentFraction);
    }

    private static void AssertLocation(
        LinearLocation? location, int componentIndex,
        int segmentIndex, double segmentFraction)
    {
        Assert.That(location, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(location!.IsValid, Is.True);
            Assert.That(location.ComponentIndex, Is.EqualTo(componentIndex));
            Assert.That(location.SegmentIndex, Is.EqualTo(segmentIndex));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(segmentFraction).Within(1.0e-15));
        });
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double x, double y, double z, double m)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X, Is.EqualTo(x));
            Assert.That(coordinate.Y, Is.EqualTo(y));
            AssertOrdinate(coordinate.Z, z);
            AssertOrdinate(coordinate.M, m);
        });
    }

    private static void AssertOrdinate(double actual, double expected)
    {
        if (double.IsNaN(expected))
        {
            Assert.That(double.IsNaN(actual), Is.True);
            return;
        }
        Assert.That(actual, Is.EqualTo(expected));
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
}
