using geo;
using NtsDiscreteFrechetDistance = NetTopologySuite.Algorithm.Distance.DiscreteFrechetDistance;
using NtsDiscreteHausdorffDistance = NetTopologySuite.Algorithm.Distance.DiscreteHausdorffDistance;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsMinimumClearance = NetTopologySuite.Precision.MinimumClearance;
using NtsWktReader = NetTopologySuite.IO.WKTReader;

namespace geo.Tests;

public class MetricConformanceTests
{
    [Test]
    public void ExactFixturesCoverScalarAndWitnessMetrics()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry polygon = Read(
            "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (1 1, 3 1, 3 5, 1 5, 1 1))");
        Geometry pointInHole = Read("POINT (2 3)");
        Geometry line = Read("LINESTRING (0 0, 5 0, 10 0)");
        Geometry endpoints = Read("LINESTRING (0 0, 10 0)");

        DistanceResult nearest = measures.NearestPoints(
            pointInHole, polygon);
        DistanceResult hausdorff = measures.HausdorffDistance(
            line, endpoints);
        DistanceResult frechet = measures.FrechetDistance(
            line, endpoints);
        DistanceResult clearance = measures.MinimumClearance(
            Read("LINESTRING (0 0, 10 0, 5 1)"));

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(polygon), Is.EqualTo(92.0));
            Assert.That(measures.Length(polygon), Is.EqualTo(52.0));
            Assert.That(measures.Perimeter(polygon), Is.EqualTo(52.0));
            AssertCoordinate(measures.Centroid(polygon),
                121.0 / 23.0, 119.0 / 23.0);
            Assert.That(nearest.Distance, Is.EqualTo(1.0));
            AssertCoordinate(nearest.FirstCoordinate, 2.0, 3.0);
            AssertCoordinate(nearest.SecondCoordinate, 3.0, 3.0);
            Assert.That(hausdorff.Distance, Is.Zero);
            Assert.That(frechet.Distance, Is.EqualTo(5.0));
            Assert.That(clearance.Distance, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void EmptyAndDegenerateMetricsHaveDefinedFallbacks()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry empty = Read(
            "GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING EMPTY, POLYGON EMPTY)");
        Geometry collapsedLine = Read("LINESTRING (2 3, 2 3)");
        Geometry collapsedSurface = Read(
            "POLYGON ((0 0, 2 0, 4 0, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(empty), Is.Zero);
            Assert.That(measures.Length(empty), Is.Zero);
            Assert.That(measures.Centroid(empty), Is.Null);
            Assert.That(measures.PointOnSurface(empty), Is.Null);
            Assert.That(measures.Distance(empty, collapsedLine),
                Is.EqualTo(double.PositiveInfinity));
            Assert.That(double.IsNaN(measures.EnvelopeDistance(
                empty, collapsedLine)), Is.True);
            Assert.That(measures.NearestPoints(empty, collapsedLine).HasResult,
                Is.False);
            Assert.That(measures.HausdorffDistance(
                empty, collapsedLine).HasResult, Is.False);
            Assert.That(measures.FrechetDistance(
                collapsedLine, empty).HasResult, Is.False);
            Assert.That(measures.MinimumClearance(collapsedLine).HasResult,
                Is.False);
            AssertCoordinate(measures.Centroid(collapsedLine), 2.0, 3.0);
            AssertCoordinate(measures.Centroid(collapsedSurface), 2.0, 0.0);
            AssertCoordinate(measures.PointOnSurface(collapsedLine), 2.0, 3.0);
        });
    }

    [Test]
    public void MixedDimensionHolesAndMultipartsUseCorrectComponents()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry collection = Read(
            "GEOMETRYCOLLECTION (POINT (100 100), MULTILINESTRING ((0 0, 3 4), (0 0, 0 2)), POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1)))");
        Geometry polygonWithHole = Read(
            "POLYGON ((0 0, 6 0, 6 4, 0 4, 0 0), (1 1, 3 1, 3 3, 1 3, 1 1))");
        Geometry pointInHole = Read("POINT (2 2)");
        Geometry lines = Read(
            "MULTILINESTRING (EMPTY, (20 0, 25 0), (0 0, 10 0))");
        Geometry point = Read("POINT (3 4)");

        DistanceResult holeDistance = measures.NearestPoints(
            pointInHole, polygonWithHole);
        DistanceResult multipartDistance = measures.NearestPoints(
            point, lines);
        Coordinate? representative = measures.PointOnSurface(collection);

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(collection), Is.EqualTo(20.0));
            Assert.That(measures.Length(collection), Is.EqualTo(35.0));
            Assert.That(measures.Perimeter(collection), Is.EqualTo(28.0));
            AssertCoordinate(measures.Centroid(collection), 3.2, 2.0);
            Assert.That(representative, Is.Not.Null);
            Assert.That(new GeometryRelations().LocatePoint(
                representative!, collection), Is.Not.EqualTo(
                    TopologicalLocation.Exterior));
            Assert.That(holeDistance.Distance, Is.EqualTo(1.0));
            Assert.That(multipartDistance.Distance, Is.EqualTo(4.0));
            AssertLocation(multipartDistance.SecondLocation, 2, 0, 0.3);
        });
    }

    [Test]
    public void ConstructedMetricWitnessesInterpolateZAndM()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry point = Read("POINT ZM (3 4 90 100)");
        Geometry segment = Read(
            "LINESTRING ZM (0 0 0 10, 10 0 20 30)");
        Geometry endpoints = Read(
            "MULTIPOINT ZM ((0 0 1 2), (10 0 3 4))");
        Geometry clearanceLine = Read(
            "LINESTRING ZM (0 0 0 10, 10 0 20 30, 5 1 50 70)");

        DistanceResult nearest = measures.NearestPoints(point, segment);
        DistanceResult hausdorff = measures.DensifiedHausdorffDistance(
            segment, endpoints, 0.5);
        DistanceResult frechet = measures.FrechetDistance(
            segment, Read("LINESTRING ZM (0 3 5 7, 10 3 9 11)"));
        DistanceResult clearance = measures.MinimumClearance(clearanceLine);

        Assert.Multiple(() =>
        {
            AssertCoordinate(nearest.SecondCoordinate,
                3.0, 0.0, 6.0, 16.0);
            AssertCoordinate(hausdorff.FirstCoordinate,
                5.0, 0.0, 10.0, 20.0);
            Assert.That(frechet.Distance, Is.EqualTo(3.0));
            AssertCoordinate(frechet.FirstCoordinate,
                0.0, 0.0, 0.0, 10.0);
            AssertCoordinate(frechet.SecondCoordinate,
                0.0, 3.0, 5.0, 7.0);
            AssertCoordinate(clearance.FirstCoordinate,
                5.0, 1.0, 50.0, 70.0);
            AssertCoordinate(clearance.SecondCoordinate,
                5.0, 0.0, 10.0, 20.0);
        });
    }

    [Test]
    public void ExtremeMagnitudesRemainStableWhenResultsAreRepresentable()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry translatedPolygon = Read(
            "POLYGON ((1000000000000000 1000000000000000, 1000000000000003 1000000000000000, 1000000000000003 1000000000000004, 1000000000000000 1000000000000004, 1000000000000000 1000000000000000))");
        Geometry point = PointGeometry(0.0, 2.0);
        Geometry extremeSegment = LineGeometry(
            -double.MaxValue, 0.0, double.MaxValue, 0.0);
        Geometry farFirst = PointGeometry(-double.MaxValue, 0.0);
        Geometry farSecond = PointGeometry(double.MaxValue, 0.0);

        DistanceResult nearest = measures.NearestPoints(
            point, extremeSegment);

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(translatedPolygon), Is.EqualTo(12.0));
            Assert.That(measures.Perimeter(translatedPolygon), Is.EqualTo(14.0));
            AssertCoordinate(measures.Centroid(translatedPolygon),
                1000000000000001.5, 1000000000000002.0);
            Assert.That(nearest.Distance, Is.EqualTo(2.0));
            AssertCoordinate(nearest.SecondCoordinate, 0.0, 0.0);
            Assert.That(measures.Distance(farFirst, farSecond),
                Is.EqualTo(double.PositiveInfinity));
            Assert.That(measures.HausdorffDistance(
                farFirst, farSecond).Distance,
                Is.EqualTo(double.PositiveInfinity));
            Assert.That(measures.FrechetDistance(
                farFirst, farSecond).Distance,
                Is.EqualTo(double.PositiveInfinity));
        });
    }

    [Test]
    public void IndexedAndBruteForceDistanceAgreeAcrossCoreGeometryKinds()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        string[] firstValues =
        [
            "POINT (3 4)",
            "LINESTRING (0 0, 4 0, 4 8)",
            "MULTIPOINT ((0 0), (20 20))",
            "MULTILINESTRING (EMPTY, (0 0, 2 2), (10 0, 10 10))",
            "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0), (2 2, 6 2, 6 6, 2 6, 2 2))",
            "MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((20 0, 24 0, 24 4, 20 4, 20 0)))",
            "GEOMETRYCOLLECTION (POINT (9 9), LINESTRING (0 0, 5 0))"
        ];
        string[] secondValues =
        [
            "LINESTRING (0 0, 10 0)",
            "POINT (8 3)",
            "POLYGON ((3 0, 8 0, 8 5, 3 5, 3 0))",
            "MULTILINESTRING ((3 0, 3 5), (14 2, 14 8))",
            "POINT (4 4)",
            "POLYGON ((3 0, 8 0, 8 5, 3 5, 3 0))",
            "GEOMETRYCOLLECTION (POINT (8 8), LINESTRING (7 2, 7 6))"
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < firstValues.Length)
            {
                Geometry first = Read(firstValues[index]);
                Geometry second = Read(secondValues[index]);
                double brute = measures.Distance(first, second);
                double indexed = measures.IndexedDistance(first, second);
                DistanceResult witness = measures.NearestPoints(first, second);
                string description = firstValues[index] + " versus " +
                    secondValues[index];
                Assert.That(indexed, Is.EqualTo(brute), description);
                Assert.That(witness.Distance, Is.EqualTo(brute), description);
                if (witness.HasResult)
                {
                    Assert.That(SegmentDistanceAlgorithms.GetPlanarDistance(
                        witness.FirstCoordinate!, witness.SecondCoordinate!),
                        Is.EqualTo(brute), description);
                }
                index++;
            }
        });
    }

    [Test]
    public void FixedCoreFixturesMatchNetTopologySuiteMetrics()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        NtsWktReader ntsReader = new NtsWktReader();
        string[] firstValues =
        [
            "POINT (3 4)",
            "LINESTRING (0 0, 5 2, 10 0)",
            "POLYGON ((0 0, 10 0, 10 8, 0 8, 0 0), (2 2, 4 2, 4 5, 2 5, 2 2))",
            "MULTILINESTRING ((0 0, 4 0), (10 2, 10 8))",
            "MULTIPOLYGON (((0 0, 3 0, 3 3, 0 3, 0 0)), ((8 0, 12 0, 12 2, 8 2, 8 0)))"
        ];
        string[] secondValues =
        [
            "LINESTRING (0 0, 10 0)",
            "LINESTRING (0 1, 10 1)",
            "POINT (3 3)",
            "MULTIPOINT ((2 3), (12 6))",
            "POLYGON ((4 0, 7 0, 7 4, 4 4, 4 0))"
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < firstValues.Length)
            {
                Geometry first = Read(firstValues[index]);
                Geometry second = Read(secondValues[index]);
                NtsGeometry ntsFirst = ntsReader.Read(firstValues[index]);
                NtsGeometry ntsSecond = ntsReader.Read(secondValues[index]);
                string description = "fixture " + index;
                Assert.That(measures.Area(first),
                    Is.EqualTo(ntsFirst.Area).Within(1.0e-12),
                    description + " area");
                Assert.That(measures.Length(first),
                    Is.EqualTo(ntsFirst.Length).Within(1.0e-12),
                    description + " length");
                Assert.That(measures.Distance(first, second),
                    Is.EqualTo(ntsFirst.Distance(ntsSecond)).Within(1.0e-12),
                    description + " distance");
                Assert.That(measures.IndexedDistance(first, second),
                    Is.EqualTo(ntsFirst.Distance(ntsSecond)).Within(1.0e-12),
                    description + " indexed distance");
                Assert.That(measures.HausdorffDistance(first, second).Distance,
                    Is.EqualTo(NtsDiscreteHausdorffDistance.Distance(
                        ntsFirst, ntsSecond)).Within(1.0e-12),
                    description + " Hausdorff");
                AssertCentroidEquivalent(
                    measures.Centroid(first), ntsFirst, description);
                index++;
            }
        });
    }

    [Test]
    public void LineAndClearanceFixturesMatchNetTopologySuite()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        NtsWktReader ntsReader = new NtsWktReader();
        string[] firstLines =
        [
            "LINESTRING (0 0, 10 0)",
            "LINESTRING (0 0, 4 2, 8 -1, 12 3)",
            "LINESTRING (-5 2, 0 7, 3 1, 9 5)",
            "LINESTRING (0 0, 3 0, 3 4, 8 4)"
        ];
        string[] secondLines =
        [
            "LINESTRING (10 0, 0 0)",
            "LINESTRING (0 1, 5 3, 10 0)",
            "LINESTRING (-4 4, 1 5, 5 0, 10 2)",
            "LINESTRING (0 0, 4 1, 6 4, 8 4)"
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < firstLines.Length)
            {
                Geometry first = Read(firstLines[index]);
                Geometry second = Read(secondLines[index]);
                NtsGeometry ntsFirst = ntsReader.Read(firstLines[index]);
                NtsGeometry ntsSecond = ntsReader.Read(secondLines[index]);
                string description = "line fixture " + index;
                Assert.That(measures.FrechetDistance(first, second).Distance,
                    Is.EqualTo(NtsDiscreteFrechetDistance.Distance(
                        ntsFirst, ntsSecond)).Within(1.0e-12),
                    description + " Frechet");
                Assert.That(measures.DensifiedHausdorffDistance(
                    first, second, 0.25).Distance,
                    Is.EqualTo(NtsDiscreteHausdorffDistance.Distance(
                        ntsFirst, ntsSecond, 0.25)).Within(1.0e-12),
                    description + " densified Hausdorff");
                Assert.That(measures.MinimumClearance(first).Distance,
                    Is.EqualTo(NtsMinimumClearance.GetDistance(
                        ntsFirst)).Within(1.0e-12),
                    description + " clearance");
                index++;
            }
        });
    }

    [Test]
    public void SeededRandomLineMetricsMatchNetTopologySuite()
    {
        const int seed = 730071;
        const int caseCount = 80;
        Random random = new Random(seed);
        IGeometryMeasures measures = new GeometryMeasures();
        NtsWktReader ntsReader = new NtsWktReader();

        Assert.Multiple(() =>
        {
            int caseIndex = 0;
            while (caseIndex < caseCount)
            {
                string firstText = CreateRandomLineText(random);
                string secondText = CreateRandomLineText(random);
                Geometry first = Read(firstText);
                Geometry second = Read(secondText);
                NtsGeometry ntsFirst = ntsReader.Read(firstText);
                NtsGeometry ntsSecond = ntsReader.Read(secondText);
                string description =
                    $"seed {seed}, case {caseIndex}: {firstText} versus {secondText}";
                double ntsDistance = ntsFirst.Distance(ntsSecond);
                Assert.That(measures.Distance(first, second),
                    Is.EqualTo(ntsDistance).Within(1.0e-11),
                    description + " distance");
                Assert.That(measures.IndexedDistance(first, second),
                    Is.EqualTo(ntsDistance).Within(1.0e-11),
                    description + " indexed distance");
                Assert.That(measures.HausdorffDistance(first, second).Distance,
                    Is.EqualTo(NtsDiscreteHausdorffDistance.Distance(
                        ntsFirst, ntsSecond)).Within(1.0e-11),
                    description + " Hausdorff");
                Assert.That(measures.FrechetDistance(first, second).Distance,
                    Is.EqualTo(NtsDiscreteFrechetDistance.Distance(
                        ntsFirst, ntsSecond)).Within(1.0e-11),
                    description + " Frechet");
                Assert.That(measures.MinimumClearance(first).Distance,
                    Is.EqualTo(NtsMinimumClearance.GetDistance(
                        ntsFirst)).Within(1.0e-11),
                    description + " clearance");
                caseIndex++;
            }
        });
    }

    private static string CreateRandomLineText(Random random)
    {
        int coordinateCount = random.Next(2, 7);
        int x = random.Next(-20, 21);
        int y = random.Next(-20, 21);
        string text = "LINESTRING (" + x + " " + y;
        int index = 1;
        while (index < coordinateCount)
        {
            x += random.Next(1, 7);
            y += random.Next(-6, 7);
            text += ", " + x + " " + y;
            index++;
        }
        return text + ")";
    }

    private static void AssertCentroidEquivalent(
        Coordinate? actual, NtsGeometry expectedGeometry,
        string description)
    {
        NetTopologySuite.Geometries.Coordinate? expected =
            expectedGeometry.Centroid.Coordinate;
        if (expected == null)
        {
            Assert.That(actual, Is.Null, description + " centroid");
            return;
        }
        Assert.That(actual, Is.Not.Null, description + " centroid");
        Assert.That(actual!.X,
            Is.EqualTo(expected.X).Within(1.0e-12),
            description + " centroid X");
        Assert.That(actual.Y,
            Is.EqualTo(expected.Y).Within(1.0e-12),
            description + " centroid Y");
    }

    private static Geometry PointGeometry(double x, double y)
    {
        return new Geometry
        {
            Point = new Point
            {
                Coordinate = new Coordinate { X = x, Y = y }
            }
        };
    }

    private static Geometry LineGeometry(
        double firstX, double firstY, double secondX, double secondY)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates =
                [
                    new Coordinate { X = firstX, Y = firstY },
                    new Coordinate { X = secondX, Y = secondY }
                ]
            }
        };
    }

    private static void AssertLocation(
        LinearLocation? location, int componentIndex,
        int segmentIndex, double segmentFraction)
    {
        Assert.That(location, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(location!.ComponentIndex, Is.EqualTo(componentIndex));
            Assert.That(location.SegmentIndex, Is.EqualTo(segmentIndex));
            Assert.That(location.SegmentFraction,
                Is.EqualTo(segmentFraction).Within(1.0e-14));
        });
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double x, double y)
    {
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(coordinate!.X, Is.EqualTo(x).Within(1.0e-12));
            Assert.That(coordinate.Y, Is.EqualTo(y).Within(1.0e-12));
        });
    }

    private static void AssertCoordinate(
        Coordinate? coordinate, double x, double y, double z, double m)
    {
        AssertCoordinate(coordinate, x, y);
        Assert.That(coordinate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            AssertOrdinate(coordinate!.Z, z);
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
        Assert.That(actual, Is.EqualTo(expected).Within(1.0e-12));
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
