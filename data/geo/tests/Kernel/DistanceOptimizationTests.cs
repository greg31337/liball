using geo;

namespace geo.Tests;

public class DistanceOptimizationTests
{
    [Test]
    public void GeometryEnvelopeDistanceHandlesSeparationOverlapAndEmptyInputs()
    {
        Geometry point = Read("POINT (0 0)");
        Geometry polygon = Read(
            "POLYGON ((3 4, 6 4, 6 8, 3 8, 3 4))");
        Geometry crossingEnvelope = Read(
            "LINESTRING (0 4, 4 0)");
        Geometry empty = Read("POINT EMPTY");

        Assert.Multiple(() =>
        {
            Assert.That(DistanceAlgorithm.GetEnvelopeDistance(point, polygon),
                Is.EqualTo(5.0));
            Assert.That(DistanceAlgorithm.GetEnvelopeDistance(
                polygon, crossingEnvelope), Is.Zero);
            Assert.That(double.IsNaN(DistanceAlgorithm.GetEnvelopeDistance(
                empty, polygon)), Is.True);
        });
    }

    [Test]
    public void EnvelopeDistanceReturnsInfinityForDiagonalExtremeSeparation()
    {
        Geometry first = PointGeometry(-double.MaxValue, -double.MaxValue);
        Geometry second = PointGeometry(double.MaxValue, double.MaxValue);

        double distance = DistanceAlgorithm.GetEnvelopeDistance(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(double.IsPositiveInfinity(distance), Is.True);
            Assert.That(EnvelopeAlgorithms.ScaledDistance(
                double.PositiveInfinity, double.PositiveInfinity),
                Is.EqualTo(double.PositiveInfinity));
        });
    }

    [Test]
    public void EarlyExitWithinDistanceMatchesMinimumDistanceAtTheLimit()
    {
        Geometry[] first =
        [
            Read("POINT (0 0)"),
            Read("POINT (2 3)"),
            Read("LINESTRING (0 0, 10 10)"),
            Read("POINT (5 5)"),
            Read("POINT (5 5)")
        ];
        Geometry[] second =
        [
            Read("POINT (3 4)"),
            Read("LINESTRING (0 0, 4 0)"),
            Read("LINESTRING (0 10, 10 0)"),
            Read("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))"),
            Read("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), (4 4, 6 4, 6 6, 4 6, 4 4))")
        ];
        double[] limits = [5.0, 3.0, 0.0, 0.0, 1.0];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < first.Length)
            {
                DistanceResult result = DistanceAlgorithm.GetMinimumDistance(
                    first[index], second[index]);
                Assert.That(DistanceAlgorithm.IsWithinDistance(
                    first[index], second[index], limits[index]), Is.True);
                if (limits[index] > 0.0)
                {
                    double smaller = limits[index] - 0.001;
                    Assert.That(DistanceAlgorithm.IsWithinDistance(
                        first[index], second[index], smaller),
                        Is.EqualTo(result.Distance <= smaller));
                }
                index++;
            }
        });
    }

    [Test]
    public void PublicWithinDistanceUsesTheBoundedFacetPath()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry first = Read("MULTILINESTRING ((0 0, 4 0), (20 20, 30 20))");
        Geometry second = Read("POINT (2 3)");

        Assert.Multiple(() =>
        {
            Assert.That(predicates.WithinDistance(first, second, 3.0), Is.True);
            Assert.That(predicates.WithinDistance(first, second, 2.999), Is.False);
            Assert.That(GeometryPredicateAlgorithms.WithinDistance(
                second, first, 3.0), Is.True);
        });
    }

    [Test]
    public void PreparedFacetsAreSnapshotIsolatedAndReuseTheirSegmentIndexes()
    {
        Geometry line = Read("LINESTRING (0 0, 10 0)");
        Geometry point = Read("POINT (5 3)");
        PreparedDistanceFacets preparedLine =
            DistanceAlgorithm.PrepareDistanceFacets(line);
        PreparedDistanceFacets preparedPoint =
            DistanceAlgorithm.PrepareDistanceFacets(point);
        SegmentSpatialIndex lineIndex = preparedLine.SegmentIndex;
        SegmentSpatialIndex pointIndex = preparedPoint.SegmentIndex;

        line.LineString!.Coordinates[0].Y = 100.0;
        line.LineString.Coordinates[1].Y = 100.0;
        point.Point!.Coordinate!.Y = 200.0;
        DistanceResult firstResult =
            DistanceAlgorithm.GetPreparedMinimumDistance(
                preparedLine, preparedPoint);
        DistanceResult secondResult =
            DistanceAlgorithm.GetPreparedMinimumDistance(
                preparedLine, preparedPoint);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Distance, Is.EqualTo(3.0));
            Assert.That(secondResult.Distance, Is.EqualTo(3.0));
            Assert.That(firstResult.FirstCoordinate!.X, Is.EqualTo(5.0));
            Assert.That(firstResult.FirstCoordinate.Y, Is.Zero);
            Assert.That(firstResult.SecondCoordinate!.Y, Is.EqualTo(3.0));
            Assert.That(preparedLine.SegmentIndex, Is.SameAs(lineIndex));
            Assert.That(preparedPoint.SegmentIndex, Is.SameAs(pointIndex));
            Assert.That(DistanceAlgorithm.ArePreparedWithinDistance(
                preparedLine, preparedPoint, 3.0), Is.True);
            Assert.That(DistanceAlgorithm.ArePreparedWithinDistance(
                preparedLine, preparedPoint, 2.999), Is.False);
            Assert.That(DistanceAlgorithm.GetPreparedEnvelopeDistance(
                preparedLine, preparedPoint), Is.EqualTo(3.0));
        });
    }

    [Test]
    public void PreparedContainmentAndEmptySemanticsMatchOrdinaryDistance()
    {
        PreparedDistanceFacets polygon =
            DistanceAlgorithm.PrepareDistanceFacets(Read(
                "POLYGON ((0 0, 8 0, 8 8, 0 8, 0 0))"));
        PreparedDistanceFacets point =
            DistanceAlgorithm.PrepareDistanceFacets(Read("POINT (4 4)"));
        PreparedDistanceFacets empty =
            DistanceAlgorithm.PrepareDistanceFacets(Read("POINT EMPTY"));

        DistanceResult contained =
            DistanceAlgorithm.GetPreparedMinimumDistance(point, polygon);
        DistanceResult missing =
            DistanceAlgorithm.GetPreparedMinimumDistance(empty, polygon);

        Assert.Multiple(() =>
        {
            Assert.That(contained.Distance, Is.Zero);
            Assert.That(DistanceAlgorithm.ArePreparedWithinDistance(
                point, polygon, 0.0), Is.True);
            Assert.That(missing.HasResult, Is.False);
            Assert.That(DistanceAlgorithm.ArePreparedWithinDistance(
                empty, polygon, 100.0), Is.False);
            Assert.That(double.IsNaN(
                DistanceAlgorithm.GetPreparedEnvelopeDistance(
                    empty, polygon)), Is.True);
        });
    }

    [Test]
    public void DistanceLimitsPreparedPairsAndSpatialReferencesAreValidated()
    {
        Geometry point = Read("POINT (0 0)");
        Geometry firstSrid = Read("POINT (0 0)");
        Geometry secondSrid = Read("POINT (0 0)");
        firstSrid.SpatialReferenceId = 100;
        secondSrid.SpatialReferenceId = 200;
        PreparedDistanceFacets firstPrepared =
            DistanceAlgorithm.PrepareDistanceFacets(firstSrid);
        PreparedDistanceFacets secondPrepared =
            DistanceAlgorithm.PrepareDistanceFacets(secondSrid);
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.IsWithinDistance(point, point, -1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.IsWithinDistance(
                    point, point, double.NaN));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.IsWithinDistance(
                    point, point, double.PositiveInfinity));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.GetEnvelopeDistance(
                    firstSrid, secondSrid));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.GetPreparedMinimumDistance(
                    firstPrepared, secondPrepared));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.ArePreparedWithinDistance(
                    null!, firstPrepared, 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                DistanceAlgorithm.PrepareDistanceFacets(triangle));
        });
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

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
