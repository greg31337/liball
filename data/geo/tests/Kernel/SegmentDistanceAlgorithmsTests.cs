using geo;

namespace geo.Tests;

public class SegmentDistanceAlgorithmsTests
{
    [Test]
    public void PointLineAndSegmentPredicatesDistinguishBoundaryInteriorAndExterior()
    {
        Coordinate start = CoordinateValue(0.0, 0.0);
        Coordinate end = CoordinateValue(10.0, 10.0);
        Coordinate endpoint = CoordinateValue(0.0, 0.0);
        Coordinate interior = CoordinateValue(4.0, 4.0);
        Coordinate beyond = CoordinateValue(12.0, 12.0);
        Coordinate offLine = CoordinateValue(4.0, 4.000000000000001);

        Assert.Multiple(() =>
        {
            Assert.That(SegmentDistanceAlgorithms.IsPointOnLine(beyond, start, end), Is.True);
            Assert.That(SegmentDistanceAlgorithms.IsPointOnLine(offLine, start, end), Is.False);
            Assert.That(SegmentDistanceAlgorithms.LocatePointOnSegment(endpoint, start, end),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(SegmentDistanceAlgorithms.LocatePointOnSegment(interior, start, end),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(SegmentDistanceAlgorithms.LocatePointOnSegment(beyond, start, end),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(SegmentDistanceAlgorithms.LocatePointOnSegment(offLine, start, end),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(SegmentDistanceAlgorithms.IsPointOnSegment(interior, start, end), Is.True);
            Assert.That(SegmentDistanceAlgorithms.IsPointOnSegment(beyond, start, end), Is.False);
        });
    }

    [Test]
    public void ExactPointOnLinePredicateRejectsRoundedNearCollinearity()
    {
        double basis = 134217728.0;
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate direction = CoordinateValue(basis + 1.0, basis);
        Coordinate nearlyCollinear = CoordinateValue(basis + 2.0, basis + 1.0);

        double ordinary = direction.X * nearlyCollinear.Y -
            direction.Y * nearlyCollinear.X;

        Assert.Multiple(() =>
        {
            Assert.That(ordinary, Is.Zero);
            Assert.That(SegmentDistanceAlgorithms.IsPointOnLine(
                nearlyCollinear, origin, direction), Is.False);
            Assert.That(SegmentDistanceAlgorithms.IsPointOnSegment(
                nearlyCollinear, origin, direction), Is.False);
        });
    }

    [Test]
    public void DegenerateSegmentContainsOnlyItsSinglePlanarCoordinate()
    {
        Coordinate point = CoordinateValue(3.0, 4.0);
        Coordinate samePlanar = CoordinateValue(3.0, 4.0, 9.0, 10.0);
        Coordinate other = CoordinateValue(3.0, 5.0);

        Assert.Multiple(() =>
        {
            Assert.That(SegmentDistanceAlgorithms.IsPointOnLine(samePlanar, point, point), Is.True);
            Assert.That(SegmentDistanceAlgorithms.IsPointOnLine(other, point, point), Is.False);
            Assert.That(SegmentDistanceAlgorithms.LocatePointOnSegment(samePlanar, point, point),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(SegmentDistanceAlgorithms.LocatePointOnSegment(other, point, point),
                Is.EqualTo(TopologicalLocation.Exterior));
        });
    }

    [Test]
    public void SegmentEnvelopeChecksAreInclusiveAndRejectSeparatedBounds()
    {
        Coordinate firstStart = CoordinateValue(0.0, 0.0);
        Coordinate firstEnd = CoordinateValue(4.0, 4.0);
        Envelope touching = EnvelopeAlgorithms.CreateBoundsEnvelope(4.0, 1.0, 8.0, 3.0);
        Envelope separated = EnvelopeAlgorithms.CreateBoundsEnvelope(5.0, 5.0, 8.0, 8.0);

        Assert.Multiple(() =>
        {
            Assert.That(SegmentDistanceAlgorithms.IsPointInSegmentEnvelope(
                CoordinateValue(2.0, 3.0), firstStart, firstEnd), Is.True);
            Assert.That(SegmentDistanceAlgorithms.IsPointInSegmentEnvelope(
                CoordinateValue(5.0, 3.0), firstStart, firstEnd), Is.False);
            Assert.That(SegmentDistanceAlgorithms.DoSegmentEnvelopesIntersect(
                firstStart, firstEnd, CoordinateValue(4.0, -1.0),
                CoordinateValue(6.0, 2.0)), Is.True);
            Assert.That(SegmentDistanceAlgorithms.DoSegmentEnvelopesIntersect(
                firstStart, firstEnd, CoordinateValue(5.0, 0.0),
                CoordinateValue(6.0, 2.0)), Is.False);
            Assert.That(SegmentDistanceAlgorithms.CanSegmentIntersectEnvelope(
                firstStart, firstEnd, touching), Is.True);
            Assert.That(SegmentDistanceAlgorithms.CanSegmentIntersectEnvelope(
                firstStart, firstEnd, separated), Is.False);
            Assert.That(SegmentDistanceAlgorithms.CanSegmentIntersectEnvelope(
                firstStart, firstEnd, EnvelopeAlgorithms.CreateEmptyEnvelope()), Is.False);
        });
    }

    [Test]
    public void NearestPointPairReturnsOwningWitnessesAndScaledDistance()
    {
        Coordinate first = CoordinateValue(1.0, 2.0, 3.0, 4.0);
        Coordinate second = CoordinateValue(4.0, 6.0, 7.0, 8.0);
        DistanceResult result = SegmentDistanceAlgorithms.GetNearestPointPair(first, second);

        first.X = 100.0;
        second.Y = 600.0;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(5.0));
            Assert.That(result.FirstCoordinate, Is.Not.SameAs(first));
            Assert.That(result.SecondCoordinate, Is.Not.SameAs(second));
            Assert.That(result.FirstCoordinate!.X, Is.EqualTo(1.0));
            Assert.That(result.FirstCoordinate.Z, Is.EqualTo(3.0));
            Assert.That(result.SecondCoordinate!.Y, Is.EqualTo(6.0));
            Assert.That(result.SecondCoordinate.M, Is.EqualTo(8.0));
            Assert.That(result.FirstLocation, Is.Null);
            Assert.That(result.SecondLocation, Is.Null);
        });
    }

    [Test]
    public void NearestPointSegmentProjectsAndInterpolatesTheSegmentWitness()
    {
        DistanceResult result = SegmentDistanceAlgorithms.GetNearestPointSegment(
            CoordinateValue(3.0, 4.0, 99.0, 100.0),
            CoordinateValue(0.0, 0.0, 10.0, 20.0),
            CoordinateValue(10.0, 0.0, 30.0, 60.0));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(result.Distance, Is.EqualTo(4.0));
            Assert.That(result.FirstCoordinate!.X, Is.EqualTo(3.0));
            Assert.That(result.FirstCoordinate.Z, Is.EqualTo(99.0));
            Assert.That(result.SecondCoordinate!.X, Is.EqualTo(3.0));
            Assert.That(result.SecondCoordinate.Y, Is.Zero);
            Assert.That(result.SecondCoordinate.Z, Is.EqualTo(16.0));
            Assert.That(result.SecondCoordinate.M, Is.EqualTo(32.0));
            Assert.That(result.FirstLocation, Is.Null);
            Assert.That(result.SecondLocation!.SegmentFraction, Is.EqualTo(0.3));
        });
    }

    [Test]
    public void NearestPointSegmentClampsEndpointsAndHandlesDegenerateSegments()
    {
        DistanceResult before = SegmentDistanceAlgorithms.GetNearestPointSegment(
            CoordinateValue(-2.0, 3.0), CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0));
        DistanceResult after = SegmentDistanceAlgorithms.GetNearestPointSegment(
            CoordinateValue(12.0, 4.0), CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0));
        DistanceResult degenerate = SegmentDistanceAlgorithms.GetNearestPointSegment(
            CoordinateValue(4.0, 6.0), CoordinateValue(1.0, 2.0), CoordinateValue(1.0, 2.0));

        Assert.Multiple(() =>
        {
            Assert.That(before.SecondCoordinate!.X, Is.Zero);
            Assert.That(before.SecondLocation!.SegmentFraction, Is.Zero);
            Assert.That(after.SecondCoordinate!.X, Is.EqualTo(10.0));
            Assert.That(after.SecondLocation!.SegmentFraction, Is.EqualTo(1.0));
            Assert.That(degenerate.Distance, Is.EqualTo(5.0));
            Assert.That(degenerate.SecondCoordinate!.X, Is.EqualTo(1.0));
            Assert.That(degenerate.SecondCoordinate.Y, Is.EqualTo(2.0));
            Assert.That(degenerate.SecondLocation!.SegmentFraction, Is.Zero);
        });
    }

    [Test]
    public void ExactProjectionHandlesExtremeCoordinateScaleSeparation()
    {
        DistanceResult middle = SegmentDistanceAlgorithms.GetNearestPointSegment(
            CoordinateValue(0.0, 2.0),
            CoordinateValue(-double.MaxValue, 0.0),
            CoordinateValue(double.MaxValue, 0.0));
        DistanceResult far = SegmentDistanceAlgorithms.GetNearestPointSegment(
            CoordinateValue(double.MaxValue, 1.0),
            CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(middle.SecondLocation!.SegmentFraction, Is.EqualTo(0.5));
            Assert.That(middle.SecondCoordinate!.X, Is.Zero);
            Assert.That(middle.Distance, Is.EqualTo(2.0));
            Assert.That(far.SecondLocation!.SegmentFraction, Is.EqualTo(1.0));
            Assert.That(far.SecondCoordinate!.X, Is.EqualTo(1.0));
            Assert.That(far.Distance, Is.EqualTo(double.MaxValue));
        });
    }

    [Test]
    public void IntersectingSegmentsReturnZeroDistanceWithIndependentWitnessOrdinates()
    {
        DistanceResult crossing = SegmentDistanceAlgorithms.GetNearestSegmentPair(
            CoordinateValue(0.0, 0.0, 0.0, 10.0),
            CoordinateValue(10.0, 10.0, 10.0, 20.0),
            CoordinateValue(0.0, 10.0, 100.0, 200.0),
            CoordinateValue(10.0, 0.0, 200.0, 400.0));
        DistanceResult overlap = SegmentDistanceAlgorithms.GetNearestSegmentPair(
            CoordinateValue(0.0, 0.0, 0.0), CoordinateValue(10.0, 0.0, 10.0),
            CoordinateValue(4.0, 0.0, 40.0), CoordinateValue(8.0, 0.0, 80.0));

        Assert.Multiple(() =>
        {
            Assert.That(crossing.Distance, Is.Zero);
            Assert.That(crossing.FirstCoordinate!.X, Is.EqualTo(5.0));
            Assert.That(crossing.FirstCoordinate.Z, Is.EqualTo(5.0));
            Assert.That(crossing.SecondCoordinate!.X, Is.EqualTo(5.0));
            Assert.That(crossing.SecondCoordinate.Z, Is.EqualTo(150.0));
            Assert.That(crossing.FirstLocation!.SegmentFraction, Is.EqualTo(0.5));
            Assert.That(crossing.SecondLocation!.SegmentFraction, Is.EqualTo(0.5));
            Assert.That(overlap.Distance, Is.Zero);
            Assert.That(overlap.FirstCoordinate!.X, Is.EqualTo(4.0));
            Assert.That(overlap.FirstCoordinate.Z, Is.EqualTo(4.0));
            Assert.That(overlap.SecondCoordinate!.X, Is.EqualTo(4.0));
            Assert.That(overlap.SecondCoordinate.Z, Is.EqualTo(40.0));
            Assert.That(overlap.FirstLocation!.SegmentFraction, Is.EqualTo(0.4));
            Assert.That(overlap.SecondLocation!.SegmentFraction, Is.Zero);
        });
    }

    [Test]
    public void DisjointSegmentsReturnNearestInteriorAndEndpointWitnesses()
    {
        Coordinate firstStart = CoordinateValue(0.0, 0.0);
        Coordinate firstEnd = CoordinateValue(4.0, 0.0);
        Coordinate secondStart = CoordinateValue(6.0, 3.0);
        Coordinate secondEnd = CoordinateValue(6.0, -3.0);

        DistanceResult result = SegmentDistanceAlgorithms.GetNearestSegmentPair(
            firstStart, firstEnd, secondStart, secondEnd);
        DistanceResult reversed = SegmentDistanceAlgorithms.GetNearestSegmentPair(
            secondStart, secondEnd, firstStart, firstEnd);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(2.0));
            Assert.That(result.FirstCoordinate!.X, Is.EqualTo(4.0));
            Assert.That(result.FirstCoordinate.Y, Is.Zero);
            Assert.That(result.SecondCoordinate!.X, Is.EqualTo(6.0));
            Assert.That(result.SecondCoordinate.Y, Is.Zero);
            Assert.That(result.FirstLocation!.SegmentFraction, Is.EqualTo(1.0));
            Assert.That(result.SecondLocation!.SegmentFraction, Is.EqualTo(0.5));
            Assert.That(reversed.Distance, Is.EqualTo(result.Distance));
            Assert.That(reversed.FirstCoordinate!.X, Is.EqualTo(result.SecondCoordinate.X));
            Assert.That(reversed.SecondCoordinate!.X, Is.EqualTo(result.FirstCoordinate.X));
        });
    }

    [Test]
    public void ExactCandidateComparisonSelectsNearestWitnessWhenDistancesOverflow()
    {
        DistanceResult result = SegmentDistanceAlgorithms.GetNearestSegmentPair(
            CoordinateValue(-double.MaxValue, 0.0),
            CoordinateValue(-double.MaxValue, 1.0),
            CoordinateValue(double.MaxValue, 100.0),
            CoordinateValue(double.MaxValue, double.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasResult, Is.True);
            Assert.That(double.IsPositiveInfinity(result.Distance), Is.True);
            Assert.That(result.FirstCoordinate!.Y, Is.EqualTo(1.0));
            Assert.That(result.SecondCoordinate!.Y, Is.EqualTo(100.0));
            Assert.That(result.FirstLocation!.SegmentFraction, Is.EqualTo(1.0));
            Assert.That(result.SecondLocation!.SegmentFraction, Is.Zero);
        });
    }

    [Test]
    public void AlgorithmsRejectNullNonFiniteAndMalformedEnvelopeInputs()
    {
        Coordinate valid = CoordinateValue(0.0, 0.0);
        Envelope malformed = new Envelope { MinX = 1.0, MinY = 0.0, MaxX = 0.0, MaxY = 1.0 };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentDistanceAlgorithms.IsPointOnLine(null!, valid, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentDistanceAlgorithms.LocatePointOnSegment(
                    CoordinateValue(double.NaN, 0.0), valid, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentDistanceAlgorithms.GetNearestPointPair(
                    valid, CoordinateValue(0.0, double.PositiveInfinity)));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentDistanceAlgorithms.GetNearestSegmentPair(
                    valid, valid, CoordinateValue(0.0, 0.0, double.NegativeInfinity), valid));
            Assert.Throws<InvalidGeometryException>(() =>
                SegmentDistanceAlgorithms.CanSegmentIntersectEnvelope(valid, valid, malformed));
        });
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
