using geo;

namespace geo.Tests;

public class VectorAndRingMathTests
{
    [Test]
    public void VectorArithmeticPreservesAvailableOrdinatesWithoutMutatingInputs()
    {
        Coordinate first = CoordinateValue(2.0, -3.0, 5.0, double.NaN);
        Coordinate second = CoordinateValue(-4.0, 7.0, 11.0, 13.0);

        Coordinate sum = VectorMath.AddCoordinateVectors(first, second);
        Coordinate difference = VectorMath.SubtractCoordinateVectors(first, second);
        Coordinate scaled = VectorMath.ScaleCoordinateVector(first, 2.5);
        Coordinate negated = VectorMath.NegateCoordinateVector(second);

        Assert.Multiple(() =>
        {
            AssertCoordinate(sum, -2.0, 4.0, 16.0, double.NaN);
            AssertCoordinate(difference, 6.0, -10.0, -6.0, double.NaN);
            AssertCoordinate(scaled, 5.0, -7.5, 12.5, double.NaN);
            AssertCoordinate(negated, 4.0, -7.0, -11.0, -13.0);
            AssertCoordinate(first, 2.0, -3.0, 5.0, double.NaN);
            AssertCoordinate(second, -4.0, 7.0, 11.0, 13.0);
        });
    }

    [Test]
    public void DotCrossAndScaledNormsHandleDirectionAndExtremeMagnitudes()
    {
        Coordinate first = CoordinateValue(3.0, 4.0);
        Coordinate second = CoordinateValue(-4.0, 3.0);
        Coordinate large = CoordinateValue(3.0e200, 4.0e200);
        Coordinate small = CoordinateValue(3.0e-300, 4.0e-300);
        Coordinate extreme = CoordinateValue(double.MaxValue, double.MaxValue);

        Coordinate unit = VectorMath.NormalizeCoordinateVector(extreme);

        Assert.Multiple(() =>
        {
            Assert.That(VectorMath.DotProduct(first, second), Is.Zero);
            Assert.That(VectorMath.CrossProduct(first, second), Is.EqualTo(25.0));
            Assert.That(VectorMath.CrossProduct(second, first), Is.EqualTo(-25.0));
            Assert.That(VectorMath.GetVectorLength(first), Is.EqualTo(5.0));
            Assert.That(VectorMath.GetVectorLength(large), Is.EqualTo(5.0e200).Within(1.0e185));
            Assert.That(VectorMath.GetVectorLength(small), Is.EqualTo(5.0e-300).Within(1.0e-315));
            Assert.That(unit.X, Is.EqualTo(Math.Sqrt(0.5)).Within(1.0e-15));
            Assert.That(unit.Y, Is.EqualTo(Math.Sqrt(0.5)).Within(1.0e-15));
            Assert.That(VectorMath.GetVectorLength(unit), Is.EqualTo(1.0).Within(1.0e-15));
        });
    }

    [Test]
    public void QuadrantsIncludeAxesAndExposeHalfPlaneRelationships()
    {
        int east = VectorMath.GetQuadrant(1.0, 0.0);
        int north = VectorMath.GetQuadrant(0.0, 1.0);
        int west = VectorMath.GetQuadrant(-1.0, 0.0);
        int south = VectorMath.GetQuadrant(0.0, -1.0);

        Assert.Multiple(() =>
        {
            Assert.That(east, Is.EqualTo(0));
            Assert.That(north, Is.EqualTo(0));
            Assert.That(west, Is.EqualTo(1));
            Assert.That(VectorMath.GetQuadrant(-1.0, -1.0), Is.EqualTo(2));
            Assert.That(south, Is.EqualTo(3));
            Assert.That(VectorMath.AreOppositeQuadrants(east, 2), Is.True);
            Assert.That(VectorMath.AreOppositeQuadrants(west, south), Is.True);
            Assert.That(VectorMath.AreOppositeQuadrants(east, south), Is.False);
            Assert.That(VectorMath.IsNorthernQuadrant(east), Is.True);
            Assert.That(VectorMath.IsNorthernQuadrant(west), Is.True);
            Assert.That(VectorMath.IsNorthernQuadrant(south), Is.False);
            Assert.Throws<InvalidGeometryException>(() => VectorMath.GetQuadrant(0.0, 0.0));
            Assert.Throws<InvalidGeometryException>(() => VectorMath.AreOppositeQuadrants(-1, 2));
        });
    }

    [Test]
    public void InterpolationAndProjectionAreStableForExtremeCoordinates()
    {
        Coordinate start = CoordinateValue(-double.MaxValue, 0.0, 10.0, double.NaN);
        Coordinate end = CoordinateValue(double.MaxValue, 10.0, 20.0, 40.0);

        Coordinate midpoint = VectorMath.InterpolateCoordinate(start, end, 0.5);
        Coordinate firstEndpoint = VectorMath.InterpolateCoordinate(start, end, 0.0);
        Coordinate lastEndpoint = VectorMath.InterpolateCoordinate(start, end, 1.0);
        double middleFactor = VectorMath.GetProjectionFactor(
            CoordinateValue(0.0, 0.0), CoordinateValue(-double.MaxValue, 0.0),
            CoordinateValue(double.MaxValue, 0.0));
        double beforeFactor = VectorMath.GetProjectionFactor(
            CoordinateValue(-5.0, 2.0), CoordinateValue(0.0, 0.0), CoordinateValue(10.0, 0.0));
        double degenerate = VectorMath.GetProjectionFactor(
            CoordinateValue(2.0, 3.0), CoordinateValue(1.0, 1.0), CoordinateValue(1.0, 1.0));

        Assert.Multiple(() =>
        {
            Assert.That(midpoint.X, Is.Zero);
            Assert.That(midpoint.Y, Is.EqualTo(5.0));
            Assert.That(midpoint.Z, Is.EqualTo(15.0));
            Assert.That(double.IsNaN(midpoint.M), Is.True);
            Assert.That(firstEndpoint.X, Is.EqualTo(-double.MaxValue));
            Assert.That(double.IsNaN(firstEndpoint.M), Is.True);
            Assert.That(lastEndpoint.X, Is.EqualTo(double.MaxValue));
            Assert.That(lastEndpoint.M, Is.EqualTo(40.0));
            Assert.That(middleFactor, Is.EqualTo(0.5));
            Assert.That(beforeFactor, Is.EqualTo(-0.5));
            Assert.That(double.IsNaN(degenerate), Is.True);
        });
    }

    [Test]
    public void AnglesUseNormalizedVectorsAndPreserveTurnDirection()
    {
        Coordinate east = CoordinateValue(double.MaxValue, 0.0);
        Coordinate north = CoordinateValue(0.0, double.MaxValue);
        Coordinate south = CoordinateValue(0.0, -double.MaxValue);
        Coordinate west = CoordinateValue(-double.MaxValue, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(VectorMath.GetAngleBetween(east, north), Is.EqualTo(Math.PI / 2.0));
            Assert.That(VectorMath.GetAngleBetween(east, west), Is.EqualTo(Math.PI));
            Assert.That(VectorMath.GetAngleBetween(east, east), Is.Zero);
            Assert.That(VectorMath.GetOrientedAngle(east, north), Is.EqualTo(Math.PI / 2.0));
            Assert.That(VectorMath.GetOrientedAngle(east, south), Is.EqualTo(-Math.PI / 2.0));
            Assert.Throws<InvalidGeometryException>(() =>
                VectorMath.GetAngleBetween(east, CoordinateValue(0.0, 0.0)));
        });
    }

    [Test]
    public void CoordinateOrderingIsLexicographicWithMissingOrdinatesFirst()
    {
        Coordinate xy = CoordinateValue(1.0, 2.0);
        Coordinate xyz = CoordinateValue(1.0, 2.0, 0.0, double.NaN);
        Coordinate xyzm = CoordinateValue(1.0, 2.0, 0.0, 5.0);
        Coordinate laterY = CoordinateValue(1.0, 3.0);
        Coordinate laterX = CoordinateValue(2.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(VectorMath.CompareCoordinateValues(xy, xyz), Is.LessThan(0));
            Assert.That(VectorMath.CompareCoordinateValues(xyz, xyzm), Is.LessThan(0));
            Assert.That(VectorMath.CompareCoordinateValues(xyzm, laterY), Is.LessThan(0));
            Assert.That(VectorMath.CompareCoordinateValues(laterY, laterX), Is.LessThan(0));
            Assert.That(VectorMath.CompareCoordinateValues(xy, CoordinateValue(1.0, 2.0)), Is.Zero);
            Assert.That(VectorMath.CompareOrdinateValues(double.NaN, -double.MaxValue), Is.LessThan(0));
            Assert.That(VectorMath.CompareOrdinateValues(-0.0, 0.0), Is.Zero);
        });
    }

    [Test]
    public void SignedAreaHandlesOpenClosedTranslatedAndDegenerateRings()
    {
        Coordinate[] counterClockwise =
        [
            CoordinateValue(0.0, 0.0),
            CoordinateValue(4.0, 0.0),
            CoordinateValue(4.0, 4.0),
            CoordinateValue(0.0, 4.0),
            CoordinateValue(0.0, 0.0)
        ];
        Coordinate[] clockwise = counterClockwise.Reverse().ToArray();
        Coordinate[] open = counterClockwise.Take(4).ToArray();
        Coordinate[] collinear =
        [CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0), CoordinateValue(2.0, 2.0)];
        double origin = 1.0e150;
        double side = 1.0e140;
        Coordinate[] translated =
        [
            CoordinateValue(origin, origin),
            CoordinateValue(origin + side, origin),
            CoordinateValue(origin + side, origin + side),
            CoordinateValue(origin, origin + side),
            CoordinateValue(origin, origin)
        ];
        double representedSide = translated[1].X - translated[0].X;

        Assert.Multiple(() =>
        {
            Assert.That(RingMath.GetSignedArea(counterClockwise), Is.EqualTo(16.0));
            Assert.That(RingMath.GetSignedArea(clockwise), Is.EqualTo(-16.0));
            Assert.That(RingMath.GetSignedArea(open), Is.EqualTo(16.0));
            Assert.That(RingMath.GetRingOrientation(counterClockwise),
                Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(RingMath.GetRingOrientation(clockwise), Is.EqualTo(RingOrientation.Clockwise));
            Assert.That(RingMath.GetSignedArea(collinear), Is.Zero);
            Assert.That(RingMath.GetRingOrientation(collinear), Is.EqualTo(RingOrientation.Undefined));
            Assert.That(RingMath.GetSignedArea([]), Is.Zero);
            Assert.That(RingMath.GetSignedArea(translated),
                Is.EqualTo(representedSide * representedSide).Within(representedSide * representedSide * 1.0e-15));
            Assert.That(GeometryNormalization.GetSignedRingArea(counterClockwise), Is.EqualTo(16.0));
        });
    }

    [Test]
    public void InvalidInputsAndNonFiniteResultsFailDeterministically()
    {
        Coordinate finite = CoordinateValue(1.0, 2.0);
        Coordinate infinite = CoordinateValue(double.PositiveInfinity, 2.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => VectorMath.AddCoordinateVectors(null!, finite));
            Assert.Throws<InvalidGeometryException>(() => VectorMath.DotProduct(infinite, finite));
            Assert.Throws<InvalidGeometryException>(() => VectorMath.ScaleCoordinateVector(finite, double.NaN));
            Assert.Throws<InvalidGeometryException>(() => VectorMath.ScaleCoordinateVector(
                CoordinateValue(double.MaxValue, 1.0), 2.0));
            Assert.Throws<InvalidGeometryException>(() => VectorMath.InterpolateCoordinate(
                finite, CoordinateValue(3.0, 4.0), double.PositiveInfinity));
            Assert.Throws<InvalidGeometryException>(() => RingMath.GetSignedArea(null!));
            Assert.Throws<InvalidGeometryException>(() => RingMath.GetSignedArea([finite, null!]));
            Assert.Throws<InvalidGeometryException>(() => RingMath.GetSignedArea([finite, infinite]));
        });
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static void AssertCoordinate(
        Coordinate coordinate, double x, double y, double z, double m)
    {
        Assert.That(coordinate.X, Is.EqualTo(x));
        Assert.That(coordinate.Y, Is.EqualTo(y));
        Assert.That(OrdinatesEqual(coordinate.Z, z), Is.True);
        Assert.That(OrdinatesEqual(coordinate.M, m), Is.True);
    }

    private static bool OrdinatesEqual(double first, double second)
    {
        if (double.IsNaN(first) && double.IsNaN(second)) return true;
        return first.Equals(second);
    }
}
