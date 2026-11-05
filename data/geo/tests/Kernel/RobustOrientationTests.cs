using geo;

namespace geo.Tests;

public class RobustOrientationTests
{
    [Test]
    public void ClassifiesCounterClockwiseClockwiseCollinearAndCoincidentTriples()
    {
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate east = CoordinateValue(1.0, 0.0);
        Coordinate north = CoordinateValue(0.0, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(origin, east, north), Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(origin, north, east), Is.EqualTo(-1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                origin, east, CoordinateValue(2.0, 0.0)), Is.Zero);
            Assert.That(RobustOrientation.GetOrientationIndex(origin, origin, north), Is.Zero);
            Assert.That(RobustOrientation.GetOrientationIndex(origin, east, east), Is.Zero);
            Assert.That(RobustOrientation.GetOrientationIndex(
                CoordinateValue(-0.0, 0.0), CoordinateValue(0.0, -0.0), origin), Is.Zero);
        });
    }

    [Test]
    public void ExactFallbackResolvesNearCollinearRoundedProducts()
    {
        double baseValue = 134217728.0;
        Coordinate origin = CoordinateValue(0.0, 0.0);
        Coordinate second = CoordinateValue(baseValue + 1.0, baseValue);
        Coordinate third = CoordinateValue(baseValue + 2.0, baseValue + 1.0);
        double ordinary = second.X * third.Y - second.Y * third.X;

        int orientation = RobustOrientation.GetOrientationIndex(origin, second, third);
        int reversed = RobustOrientation.GetOrientationIndex(origin, third, second);
        int exact = RobustOrientation.GetExactOrientationSign(
            origin.X, origin.Y, second.X, second.Y, third.X, third.Y);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary, Is.Zero);
            Assert.That(orientation, Is.EqualTo(1));
            Assert.That(reversed, Is.EqualTo(-1));
            Assert.That(exact, Is.EqualTo(1));
        });
    }

    [Test]
    public void HandlesDifferenceAndProductOverflowAtFiniteCoordinates()
    {
        Coordinate lowerLeft = CoordinateValue(-double.MaxValue, -double.MaxValue);
        Coordinate lowerRight = CoordinateValue(double.MaxValue, -double.MaxValue);
        Coordinate upperLeft = CoordinateValue(-double.MaxValue, double.MaxValue);
        double previousMaximum = Math.BitDecrement(double.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(
                lowerLeft, lowerRight, upperLeft), Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                lowerLeft, upperLeft, lowerRight), Is.EqualTo(-1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                CoordinateValue(double.MaxValue, double.MaxValue),
                CoordinateValue(previousMaximum, double.MaxValue),
                CoordinateValue(double.MaxValue, previousMaximum)), Is.EqualTo(1));
        });
    }

    [Test]
    public void HandlesUnderflowAndExtremeExponentCancellationExactly()
    {
        double smallest = double.Epsilon;
        double previousMaximum = Math.BitDecrement(double.MaxValue);
        Coordinate origin = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(
                origin, CoordinateValue(smallest * 2.0, smallest),
                CoordinateValue(smallest, smallest)), Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                origin, CoordinateValue(smallest, smallest * 2.0),
                CoordinateValue(smallest, smallest)), Is.EqualTo(-1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                origin, CoordinateValue(double.MaxValue, previousMaximum),
                CoordinateValue(smallest, smallest)), Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                origin, CoordinateValue(previousMaximum, double.MaxValue),
                CoordinateValue(smallest, smallest)), Is.EqualTo(-1));
        });
    }

    [Test]
    public void OrientationIsTranslationInvariantForRepresentableOffsets()
    {
        double translation = 1.0e150;
        double side = 1.0e140;
        Coordinate first = CoordinateValue(translation, translation);
        Coordinate second = CoordinateValue(translation + side, translation);
        Coordinate third = CoordinateValue(translation, translation + side);
        Coordinate translatedFirst = CoordinateValue(-translation, -translation);
        Coordinate translatedSecond = CoordinateValue(-translation + side, -translation);
        Coordinate translatedThird = CoordinateValue(-translation, -translation + side);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(first, second, third), Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(
                translatedFirst, translatedSecond, translatedThird), Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(first, third, second), Is.EqualTo(-1));
        });
    }

    [Test]
    public void MatchesExactIntegerArithmeticAcrossDeterministicRandomTriples()
    {
        Random random = new Random(8142);
        int iteration = 0;
        while (iteration < 2000)
        {
            int firstX = random.Next(-1000000, 1000001);
            int firstY = random.Next(-1000000, 1000001);
            int secondX = random.Next(-1000000, 1000001);
            int secondY = random.Next(-1000000, 1000001);
            int thirdX = random.Next(-1000000, 1000001);
            int thirdY = random.Next(-1000000, 1000001);
            long determinant = (long)(secondX - firstX) * (thirdY - firstY) -
                (long)(secondY - firstY) * (thirdX - firstX);
            int expected = determinant < 0 ? -1 : determinant > 0 ? 1 : 0;
            int actual = RobustOrientation.GetOrientationIndex(
                CoordinateValue(firstX, firstY), CoordinateValue(secondX, secondY),
                CoordinateValue(thirdX, thirdY));
            Assert.That(actual, Is.EqualTo(expected),
                firstX + "," + firstY + " / " + secondX + "," + secondY +
                " / " + thirdX + "," + thirdY);
            iteration++;
        }
    }

    [Test]
    public void CyclicPermutationsPreserveAndSwapsReverseTheSign()
    {
        Coordinate first = CoordinateValue(-7.0, 3.0);
        Coordinate second = CoordinateValue(11.0, -5.0);
        Coordinate third = CoordinateValue(2.0, 13.0);
        int orientation = RobustOrientation.GetOrientationIndex(first, second, third);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(second, third, first),
                Is.EqualTo(orientation));
            Assert.That(RobustOrientation.GetOrientationIndex(third, first, second),
                Is.EqualTo(orientation));
            Assert.That(RobustOrientation.GetOrientationIndex(first, third, second),
                Is.EqualTo(-orientation));
            Assert.That(RobustOrientation.GetOrientationIndex(second, first, third),
                Is.EqualTo(-orientation));
        });
    }

    [Test]
    public void RejectsNullAndNonFiniteCoordinatesDeterministically()
    {
        Coordinate valid = CoordinateValue(0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                RobustOrientation.GetOrientationIndex(null!, valid, valid));
            Assert.Throws<InvalidGeometryException>(() => RobustOrientation.GetOrientationIndex(
                CoordinateValue(double.NaN, 0.0), valid, valid));
            Assert.Throws<InvalidGeometryException>(() => RobustOrientation.GetOrientationIndex(
                CoordinateValue(0.0, double.PositiveInfinity), valid, valid));
            Assert.Throws<InvalidGeometryException>(() => RobustOrientation.GetOrientationIndex(
                CoordinateValue(0.0, 0.0, double.NegativeInfinity), valid, valid));
            Assert.Throws<InvalidGeometryException>(() => RobustOrientation.GetOrientationIndexValues(
                0.0, 0.0, 1.0, 0.0, double.NegativeInfinity, 1.0));
        });
    }

    private static Coordinate CoordinateValue(
        double x, double y, double z = double.NaN, double m = double.NaN)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }
}
