using geo;

namespace geo.Tests;

public class RobustLineIntersectionTests
{
    [Test]
    public void ProperCrossingReturnsInteriorWitnessAndInterpolatesOptionalOrdinates()
    {
        Coordinate firstStart = CoordinateValue(0.0, 0.0, 10.0, 100.0);
        Coordinate firstEnd = CoordinateValue(10.0, 10.0, 20.0, 200.0);
        Coordinate secondStart = CoordinateValue(0.0, 10.0, 500.0, 600.0);
        Coordinate secondEnd = CoordinateValue(10.0, 0.0, 700.0, 800.0);

        SegmentIntersectionResult result = RobustLineIntersection.GetSegmentIntersection(
            firstStart, firstEnd, secondStart, secondEnd);

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(result.Location, Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(result.IsProper, Is.True);
            Assert.That(result.FirstIntersection, Is.Not.Null);
            Assert.That(result.FirstIntersection!.X, Is.EqualTo(5.0));
            Assert.That(result.FirstIntersection.Y, Is.EqualTo(5.0));
            Assert.That(result.FirstIntersection.Z, Is.EqualTo(15.0));
            Assert.That(result.FirstIntersection.M, Is.EqualTo(150.0));
            Assert.That(result.SecondIntersection, Is.Null);
        });
    }

    [Test]
    public void NonCollinearEndpointTouchReturnsOwningBoundaryWitness()
    {
        Coordinate shared = CoordinateValue(4.0, 0.0, 3.0, 7.0);
        SegmentIntersectionResult result = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(0.0, 0.0), shared,
            CoordinateValue(4.0, -2.0), CoordinateValue(4.0, 0.0, 9.0, 11.0));

        shared.X = 40.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.Endpoint));
            Assert.That(result.Location, Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(result.IsProper, Is.False);
            Assert.That(result.FirstIntersection, Is.Not.SameAs(shared));
            Assert.That(result.FirstIntersection!.X, Is.EqualTo(4.0));
            Assert.That(result.FirstIntersection.Y, Is.Zero);
            Assert.That(result.FirstIntersection.Z, Is.EqualTo(9.0));
            Assert.That(result.FirstIntersection.M, Is.EqualTo(11.0));
        });
    }

    [Test]
    public void CollinearTouchAndOverlapHaveDistinctDeterministicResults()
    {
        SegmentIntersectionResult point = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 2.0),
            CoordinateValue(4.0, 4.0), CoordinateValue(2.0, 2.0));
        SegmentIntersectionResult overlap = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(6.0, 6.0), CoordinateValue(0.0, 0.0),
            CoordinateValue(8.0, 8.0), CoordinateValue(3.0, 3.0));

        Assert.Multiple(() =>
        {
            Assert.That(point.Type, Is.EqualTo(SegmentIntersectionType.CollinearPoint));
            Assert.That(point.FirstIntersection!.X, Is.EqualTo(2.0));
            Assert.That(point.SecondIntersection, Is.Null);
            Assert.That(overlap.Type, Is.EqualTo(SegmentIntersectionType.CollinearOverlap));
            Assert.That(overlap.Location, Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(overlap.FirstIntersection!.X, Is.EqualTo(3.0));
            Assert.That(overlap.FirstIntersection.Y, Is.EqualTo(3.0));
            Assert.That(overlap.SecondIntersection!.X, Is.EqualTo(6.0));
            Assert.That(overlap.SecondIntersection.Y, Is.EqualTo(6.0));
            Assert.That(overlap.IsProper, Is.False);
        });
    }

    [Test]
    public void DisjointSegmentsReturnExteriorResultWithoutWitnesses()
    {
        SegmentIntersectionResult separated = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 0.0), CoordinateValue(3.0, 1.0));
        SegmentIntersectionResult collinear = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(3.0, 3.0));

        Assert.Multiple(() =>
        {
            AssertNoIntersection(separated);
            AssertNoIntersection(collinear);
        });
    }

    [Test]
    public void InfiniteLinesIntersectOutsideSegmentsAndRejectNonUniqueLines()
    {
        Coordinate? intersection = RobustLineIntersection.GetInfiniteLineIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0),
            CoordinateValue(2.0, 1.0), CoordinateValue(2.0, 2.0));
        Coordinate? parallel = RobustLineIntersection.GetInfiniteLineIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(0.0, 1.0), CoordinateValue(1.0, 2.0));
        Coordinate? coincident = RobustLineIntersection.GetInfiniteLineIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(3.0, 3.0));
        Coordinate? degenerate = RobustLineIntersection.GetInfiniteLineIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(0.0, 0.0),
            CoordinateValue(0.0, 1.0), CoordinateValue(1.0, 1.0));

        Assert.Multiple(() =>
        {
            Assert.That(intersection, Is.Not.Null);
            Assert.That(intersection!.X, Is.EqualTo(2.0));
            Assert.That(intersection.Y, Is.Zero);
            Assert.That(parallel, Is.Null);
            Assert.That(coincident, Is.Null);
            Assert.That(degenerate, Is.Null);
        });
    }

    [Test]
    public void ExactDirectionFallbackResolvesNearParallelProperCrossing()
    {
        double basis = 134217728.0;
        Coordinate firstStart = CoordinateValue(-(basis + 1.0), -basis);
        Coordinate firstEnd = CoordinateValue(basis + 1.0, basis);
        Coordinate secondStart = CoordinateValue(-(basis + 2.0), -(basis + 1.0));
        Coordinate secondEnd = CoordinateValue(basis + 2.0, basis + 1.0);
        double ordinaryDirectionDeterminant =
            (firstEnd.X - firstStart.X) * (secondEnd.Y - secondStart.Y) -
            (firstEnd.Y - firstStart.Y) * (secondEnd.X - secondStart.X);

        SegmentIntersectionResult result = RobustLineIntersection.GetSegmentIntersection(
            firstStart, firstEnd, secondStart, secondEnd);

        Assert.Multiple(() =>
        {
            Assert.That(ordinaryDirectionDeterminant, Is.Zero);
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(result.IsProper, Is.True);
            Assert.That(result.FirstIntersection!.X, Is.Zero);
            Assert.That(result.FirstIntersection.Y, Is.Zero);
        });
    }

    [Test]
    public void ExtremeMagnitudeCrossingAvoidsDifferenceAndProductOverflow()
    {
        SegmentIntersectionResult result = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(-double.MaxValue, 0.0),
            CoordinateValue(double.MaxValue, 0.0, 8.0, 16.0),
            CoordinateValue(0.0, -double.MaxValue),
            CoordinateValue(0.0, double.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(result.FirstIntersection!.X, Is.Zero);
            Assert.That(result.FirstIntersection.Y, Is.Zero);
            Assert.That(double.IsNaN(result.FirstIntersection.Z), Is.True);
            Assert.That(double.IsNaN(result.FirstIntersection.M), Is.True);
        });
    }

    [Test]
    public void SubnormalCrossingRetainsTheRepresentableWitnessOrdinate()
    {
        double smallest = double.Epsilon;
        SegmentIntersectionResult result = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(0.0, 0.0), CoordinateValue(smallest * 2.0, smallest),
            CoordinateValue(0.0, smallest), CoordinateValue(smallest * 2.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(result.FirstIntersection!.X, Is.EqualTo(smallest));
            Assert.That(result.FirstIntersection.Y, Is.Zero);
        });
    }

    [Test]
    public void DegenerateSegmentsProducePointOrDisjointClassifications()
    {
        SegmentIntersectionResult samePoint = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(1.0, 1.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(1.0, 1.0), CoordinateValue(1.0, 1.0));
        SegmentIntersectionResult pointOnSegment = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(2.0, 2.0), CoordinateValue(2.0, 2.0),
            CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 4.0));
        SegmentIntersectionResult separatePoints = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(1.0, 1.0), CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 2.0), CoordinateValue(2.0, 2.0));

        Assert.Multiple(() =>
        {
            Assert.That(samePoint.Type, Is.EqualTo(SegmentIntersectionType.CollinearPoint));
            Assert.That(samePoint.FirstIntersection!.X, Is.EqualTo(1.0));
            Assert.That(pointOnSegment.Type, Is.EqualTo(SegmentIntersectionType.CollinearPoint));
            Assert.That(pointOnSegment.FirstIntersection!.X, Is.EqualTo(2.0));
            AssertNoIntersection(separatePoints);
        });
    }

    [Test]
    public void IntersectionRejectsNullAndNonFiniteCoordinates()
    {
        Coordinate valid = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RobustLineIntersection.GetSegmentIntersection(null!, valid, valid, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                RobustLineIntersection.GetSegmentIntersection(
                    valid, CoordinateValue(double.NaN, 0.0), valid, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                RobustLineIntersection.GetInfiniteLineIntersection(
                    valid, CoordinateValue(1.0, double.PositiveInfinity), valid, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                RobustLineIntersection.GetSegmentIntersection(
                    valid, CoordinateValue(1.0, 1.0, double.NegativeInfinity), valid, valid));
        });
    }

    private static void AssertNoIntersection(SegmentIntersectionResult result)
    {
        Assert.That(result.Type, Is.EqualTo(SegmentIntersectionType.None));
        Assert.That(result.Location, Is.EqualTo(TopologicalLocation.Exterior));
        Assert.That(result.IsProper, Is.False);
        Assert.That(result.FirstIntersection, Is.Null);
        Assert.That(result.SecondIntersection, Is.Null);
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
