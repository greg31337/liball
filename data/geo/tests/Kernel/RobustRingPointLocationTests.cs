using geo;

namespace geo.Tests;

public class RobustRingPointLocationTests
{
    [Test]
    public void ExactRingOrientationSurvivesCancellationUnderflowAndOverflow()
    {
        double basis = 134217728.0;
        Coordinate[] cancellation =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(basis + 1.0, basis),
            CoordinateValue(basis + 2.0, basis + 1.0),
            CoordinateValue(0.0, 0.0)
        ];
        Coordinate[] underflow =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(double.Epsilon * 2.0, double.Epsilon),
            CoordinateValue(double.Epsilon, double.Epsilon),
            CoordinateValue(0.0, 0.0)
        ];
        Coordinate[] overflow =
        [
            CoordinateValue(-double.MaxValue, -double.MaxValue),
            CoordinateValue(double.MaxValue, -double.MaxValue),
            CoordinateValue(double.MaxValue, double.MaxValue),
            CoordinateValue(-double.MaxValue, double.MaxValue),
            CoordinateValue(-double.MaxValue, -double.MaxValue)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(RingMath.GetSignedArea(cancellation), Is.Zero);
            Assert.That(RingMath.GetRingOrientation(cancellation),
                Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(RingMath.GetRingOrientation(cancellation.Reverse().ToArray()),
                Is.EqualTo(RingOrientation.Clockwise));
            Assert.That(RingMath.GetSignedArea(underflow), Is.Zero);
            Assert.That(RingMath.GetRingOrientation(underflow),
                Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(RingMath.GetRingOrientation(overflow),
                Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(RingMath.GetExactSignedAreaSign(overflow), Is.EqualTo(1));
        });
    }

    [Test]
    public void RayCrossingClassifiesOpenAndClosedSquareInteriorBoundaryAndExterior()
    {
        Coordinate[] open = Square(false);
        Coordinate[] closed = Square(true);
        Coordinate interior = CoordinateValue(2.0, 2.0);
        Coordinate exterior = CoordinateValue(5.0, 2.0);
        Coordinate edge = CoordinateValue(4.0, 2.0);
        Coordinate vertex = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(RingPointLocation.LocatePointByRayCrossing(interior, open),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(RingPointLocation.LocatePointByRayCrossing(interior, closed),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(RingPointLocation.LocatePointByRayCrossing(exterior, open),
                Is.EqualTo(TopologicalLocation.Exterior));
            Assert.That(RingPointLocation.LocatePointByRayCrossing(edge, open),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(RingPointLocation.LocatePointByRayCrossing(vertex, closed),
                Is.EqualTo(TopologicalLocation.Boundary));
        });
    }

    [Test]
    public void WindingAndRayCrossingAgreeForEitherRingDirection()
    {
        Coordinate[] counterClockwise = Square(true);
        Coordinate[] clockwise = counterClockwise.Reverse().ToArray();
        Coordinate[] points =
        [
            CoordinateValue(1.0, 1.0),
            CoordinateValue(4.0, 1.0),
            CoordinateValue(7.0, 1.0),
            CoordinateValue(2.0, 4.0),
            CoordinateValue(2.0, -1.0)
        ];

        int index = 0;
        while (index < points.Length)
        {
            TopologicalLocation expected = RingPointLocation.LocatePointByRayCrossing(
                points[index], counterClockwise);
            Assert.That(RingPointLocation.LocatePointByWinding(
                points[index], counterClockwise), Is.EqualTo(expected));
            Assert.That(RingPointLocation.LocatePointByRayCrossing(
                points[index], clockwise), Is.EqualTo(expected));
            Assert.That(RingPointLocation.LocatePointByWinding(
                points[index], clockwise), Is.EqualTo(expected));
            index++;
        }
    }

    [Test]
    public void ConcaveRingDistinguishesFilledArmsNotchAndBoundary()
    {
        Coordinate[] ring =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(6.0, 0.0),
            CoordinateValue(6.0, 2.0),
            CoordinateValue(2.0, 2.0),
            CoordinateValue(2.0, 6.0),
            CoordinateValue(0.0, 6.0),
            CoordinateValue(0.0, 0.0)
        ];

        Assert.Multiple(() =>
        {
            AssertLocationBoth(CoordinateValue(5.0, 1.0), ring, TopologicalLocation.Interior);
            AssertLocationBoth(CoordinateValue(1.0, 5.0), ring, TopologicalLocation.Interior);
            AssertLocationBoth(CoordinateValue(4.0, 4.0), ring, TopologicalLocation.Exterior);
            AssertLocationBoth(CoordinateValue(2.0, 4.0), ring, TopologicalLocation.Boundary);
            AssertLocationBoth(CoordinateValue(2.0, 2.0), ring, TopologicalLocation.Boundary);
        });
    }

    [Test]
    public void HalfOpenCrossingRuleHandlesHorizontalEdgesAndVertexScanlines()
    {
        Coordinate[] diamond =
        [
            CoordinateValue(0.0, 2.0),
            CoordinateValue(2.0, 4.0),
            CoordinateValue(4.0, 2.0),
            CoordinateValue(2.0, 0.0),
            CoordinateValue(0.0, 2.0)
        ];

        Assert.Multiple(() =>
        {
            AssertLocationBoth(CoordinateValue(2.0, 2.0), diamond, TopologicalLocation.Interior);
            AssertLocationBoth(CoordinateValue(3.0, 2.0), diamond, TopologicalLocation.Interior);
            AssertLocationBoth(CoordinateValue(5.0, 2.0), diamond, TopologicalLocation.Exterior);
            AssertLocationBoth(CoordinateValue(2.0, 4.0), diamond, TopologicalLocation.Boundary);
            AssertLocationBoth(CoordinateValue(3.0, 3.0), diamond, TopologicalLocation.Boundary);
        });
    }

    [Test]
    public void ExtremeMagnitudeRingLocationAvoidsDifferenceAndProductOverflow()
    {
        double right = double.MaxValue / 2.0;
        Coordinate[] ring =
        [
            CoordinateValue(-double.MaxValue, -double.MaxValue),
            CoordinateValue(right, -double.MaxValue),
            CoordinateValue(right, double.MaxValue),
            CoordinateValue(-double.MaxValue, double.MaxValue),
            CoordinateValue(-double.MaxValue, -double.MaxValue)
        ];

        Assert.Multiple(() =>
        {
            AssertLocationBoth(CoordinateValue(0.0, 0.0), ring, TopologicalLocation.Interior);
            AssertLocationBoth(CoordinateValue(right, 0.0), ring, TopologicalLocation.Boundary);
            AssertLocationBoth(CoordinateValue(double.MaxValue, 0.0), ring,
                TopologicalLocation.Exterior);
        });
    }

    [Test]
    public void ExactCrossingsLocatePointInsideNearCollinearParallelogram()
    {
        double basis = 134217728.0;
        Coordinate[] ring =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(basis + 1.0, basis),
            CoordinateValue(1.0, basis * 2.0 + 1.0),
            CoordinateValue(-basis, basis + 1.0),
            CoordinateValue(0.0, 0.0)
        ];
        Coordinate point = CoordinateValue(
            (basis + 1.0) / 2.0 + 1.0, basis / 2.0 + 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(ring[0], ring[1], point),
                Is.EqualTo(1));
            AssertLocationBoth(point, ring, TopologicalLocation.Interior);
        });
    }

    [Test]
    public void EmptyRepeatedAndDegenerateRingsHaveDefinedLocations()
    {
        Coordinate repeated = CoordinateValue(1.0, 1.0);
        Coordinate[] repeatedRing =
        [
            CoordinateValue(0.0, 0.0), repeated, repeated,
            CoordinateValue(2.0, 0.0), CoordinateValue(0.0, 0.0)
        ];
        Coordinate[] collinear =
        [
            CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 0.0),
            CoordinateValue(4.0, 0.0)
        ];

        Assert.Multiple(() =>
        {
            AssertLocationBoth(CoordinateValue(0.0, 0.0), [], TopologicalLocation.Exterior);
            AssertLocationBoth(CoordinateValue(1.0, 1.0), [repeated],
                TopologicalLocation.Boundary);
            AssertLocationBoth(CoordinateValue(1.0, 0.0), collinear,
                TopologicalLocation.Boundary);
            AssertLocationBoth(CoordinateValue(1.0, 1.0), collinear,
                TopologicalLocation.Exterior);
            AssertLocationBoth(CoordinateValue(1.0, 0.25), repeatedRing,
                TopologicalLocation.Interior);
            AssertLocationBoth(repeated, repeatedRing, TopologicalLocation.Boundary);
            Assert.That(RingMath.GetRingOrientation(collinear),
                Is.EqualTo(RingOrientation.Undefined));
        });
    }

    [Test]
    public void RingAlgorithmsRejectNullAndNonFiniteInputs()
    {
        Coordinate valid = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RingMath.GetRingOrientation(null!));
            Assert.Throws<InvalidGeometryException>(() =>
                RingMath.GetRingOrientation([valid, null!, valid]));
            Assert.Throws<InvalidGeometryException>(() =>
                RingPointLocation.LocatePointByRayCrossing(null!, [valid]));
            Assert.Throws<InvalidGeometryException>(() =>
                RingPointLocation.LocatePointByWinding(
                    CoordinateValue(double.NaN, 0.0), [valid]));
            Assert.Throws<InvalidGeometryException>(() =>
                RingPointLocation.LocatePointByRayCrossing(
                    valid, [CoordinateValue(double.PositiveInfinity, 0.0)]));
        });
    }

    private static Coordinate[] Square(bool close)
    {
        Coordinate[] open =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(0.0, 4.0)
        ];
        if (!close) return open;
        return [open[0], open[1], open[2], open[3], CoordinateValue(0.0, 0.0)];
    }

    private static void AssertLocationBoth(
        Coordinate point, Coordinate[] ring, TopologicalLocation expected)
    {
        Assert.That(RingPointLocation.LocatePointByRayCrossing(point, ring),
            Is.EqualTo(expected));
        Assert.That(RingPointLocation.LocatePointByWinding(point, ring),
            Is.EqualTo(expected));
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
