using geo;

namespace geo.Tests;

public class CoordinateAlgorithmsTests
{
    [Test]
    public void PublicContractProvidesOrderingInterpolationAndVectorOperations()
    {
        ICoordinateAlgorithms algorithms = new CoordinateAlgorithms();
        Coordinate first = CoordinateValue(3.0, 4.0, 10.0, 20.0);
        Coordinate second = CoordinateValue(-4.0, 3.0, 30.0, 60.0);
        Coordinate interpolation = algorithms.InterpolateCoordinate(first, second, 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(algorithms.CompareCoordinates(first, second), Is.GreaterThan(0));
            Assert.That(algorithms.CompareCoordinates(first, CoordinateValue(3.0, 4.0, 10.0, 20.0)), Is.Zero);
            Assert.That(interpolation.X, Is.EqualTo(1.25));
            Assert.That(interpolation.Y, Is.EqualTo(3.75));
            Assert.That(interpolation.Z, Is.EqualTo(15.0));
            Assert.That(interpolation.M, Is.EqualTo(30.0));
            Assert.That(interpolation, Is.Not.SameAs(first));
            Assert.That(algorithms.DotProduct(first, second), Is.Zero);
            Assert.That(algorithms.CrossProduct(first, second), Is.EqualTo(25.0));
            Assert.That(algorithms.AngleBetween(first, second), Is.EqualTo(Math.PI / 2.0));
        });
    }

    [Test]
    public void OrientationAndPointLocationExposeRobustKernelClassifications()
    {
        ICoordinateAlgorithms algorithms = new CoordinateAlgorithms();
        double basis = 134217728.0;
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate second = CoordinateValue(basis + 1.0, basis);
        Coordinate third = CoordinateValue(basis + 2.0, basis + 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(algorithms.OrientationIndex(origin, second, third), Is.EqualTo(1));
            Assert.That(algorithms.OrientationIndex(origin, third, second), Is.EqualTo(-1));
            Assert.That(algorithms.LocatePointOnSegment(
                CoordinateValue(0.0, 0.0), origin, second),
                Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(algorithms.LocatePointOnSegment(
                CoordinateValue((basis + 1.0) / 2.0, basis / 2.0), origin, second),
                Is.EqualTo(TopologicalLocation.Interior));
            Assert.That(algorithms.LocatePointOnSegment(third, origin, second),
                Is.EqualTo(TopologicalLocation.Exterior));
        });
    }

    [Test]
    public void SegmentOperationsReturnClassificationsAndOwningWitnesses()
    {
        ICoordinateAlgorithms algorithms = new CoordinateAlgorithms();
        Coordinate firstStart = CoordinateValue(0.0, 0.0, 0.0, 10.0);
        Coordinate firstEnd = CoordinateValue(10.0, 10.0, 10.0, 20.0);
        Coordinate secondStart = CoordinateValue(0.0, 10.0, 100.0, 200.0);
        Coordinate secondEnd = CoordinateValue(10.0, 0.0, 200.0, 400.0);

        SegmentIntersectionResult intersection = algorithms.IntersectSegments(
            firstStart, firstEnd, secondStart, secondEnd);
        DistanceResult nearest = algorithms.NearestSegmentPoints(
            CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 0.0),
            CoordinateValue(6.0, -2.0), CoordinateValue(6.0, 2.0));

        firstStart.X = 1000.0;

        Assert.Multiple(() =>
        {
            Assert.That(intersection.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(intersection.IsProper, Is.True);
            Assert.That(intersection.FirstIntersection!.X, Is.EqualTo(5.0));
            Assert.That(intersection.FirstIntersection.Y, Is.EqualTo(5.0));
            Assert.That(intersection.FirstIntersection.Z, Is.EqualTo(5.0));
            Assert.That(intersection.FirstIntersection.M, Is.EqualTo(15.0));
            Assert.That(nearest.HasResult, Is.True);
            Assert.That(nearest.Distance, Is.EqualTo(2.0));
            Assert.That(nearest.FirstCoordinate!.X, Is.EqualTo(4.0));
            Assert.That(nearest.SecondCoordinate!.X, Is.EqualTo(6.0));
            Assert.That(nearest.SecondCoordinate.Y, Is.Zero);
            Assert.That(nearest.FirstLocation!.SegmentFraction, Is.EqualTo(1.0));
            Assert.That(nearest.SecondLocation!.SegmentFraction, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void RingOperationsProvideAreaAndExactOrientation()
    {
        ICoordinateAlgorithms algorithms = new CoordinateAlgorithms();
        Coordinate[] square =
        [
            CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0), CoordinateValue(0.0, 4.0),
            CoordinateValue(0.0, 0.0)
        ];
        double basis = 134217728.0;
        Coordinate[] cancellation =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(basis + 1.0, basis),
            CoordinateValue(basis + 2.0, basis + 1.0),
            CoordinateValue(0.0, 0.0)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(algorithms.SignedRingArea(square), Is.EqualTo(16.0));
            Assert.That(algorithms.RingOrientationOf(square),
                Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(algorithms.SignedRingArea(cancellation), Is.Zero);
            Assert.That(algorithms.RingOrientationOf(cancellation),
                Is.EqualTo(RingOrientation.CounterClockwise));
        });
    }

    [Test]
    public void PublicSurfaceRejectsInvalidInputsDeterministically()
    {
        ICoordinateAlgorithms algorithms = new CoordinateAlgorithms();
        Coordinate valid = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                algorithms.CompareCoordinates(null!, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                algorithms.InterpolateCoordinate(valid, valid, double.NaN));
            Assert.Throws<InvalidGeometryException>(() =>
                algorithms.OrientationIndex(
                    valid, valid, CoordinateValue(double.PositiveInfinity, 0.0)));
            Assert.Throws<InvalidGeometryException>(() =>
                algorithms.IntersectSegments(valid, valid, null!, valid));
            Assert.Throws<InvalidGeometryException>(() =>
                algorithms.NearestSegmentPoints(
                    valid, valid, CoordinateValue(0.0, double.NegativeInfinity), valid));
            Assert.Throws<InvalidGeometryException>(() => algorithms.SignedRingArea(null!));
            Assert.Throws<InvalidGeometryException>(() =>
                algorithms.AngleBetween(valid, valid));
        });
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
