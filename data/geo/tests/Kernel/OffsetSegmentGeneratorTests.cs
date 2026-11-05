using geo;

namespace geo.Tests;

public class OffsetSegmentGeneratorTests
{
    [Test]
    public void OffsetSegmentsUseNormalizedLeftAndRightNormals()
    {
        Coordinate start = Point(0.0, 0.0, 7.0, 11.0);
        Coordinate end = Point(3.0, 4.0, 8.0, 12.0);
        Coordinate[] left = OffsetSegmentGenerator.CreateOffsetSegment(
            start, end, OffsetSegmentGenerator.LeftSide, 5.0,
            FloatingPrecision());
        Coordinate[] right = OffsetSegmentGenerator.CreateOffsetSegment(
            start, end, OffsetSegmentGenerator.RightSide, 5.0,
            FloatingPrecision());

        Assert.Multiple(() =>
        {
            AssertCoordinate(left[0], -4.0, 3.0, 1e-14);
            AssertCoordinate(left[1], -1.0, 7.0, 1e-14);
            AssertCoordinate(right[0], 4.0, -3.0, 1e-14);
            AssertCoordinate(right[1], 7.0, 1.0, 1e-14);
            Assert.That(left[0].Z, Is.EqualTo(7.0));
            Assert.That(left[0].M, Is.EqualTo(11.0));
            Assert.That(left[1].Z, Is.EqualTo(8.0));
            Assert.That(left[1].M, Is.EqualTo(12.0));
            AssertCoordinate(start, 0.0, 0.0);
            AssertCoordinate(end, 3.0, 4.0);
        });
    }

    [Test]
    public void RoundJoinUsesConfiguredQuadrantResolution()
    {
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 4, 5.0,
            FloatingPrecision());

        Coordinate[] join = OffsetSegmentGenerator.CreateJoin(
            Point(0.0, 0.0), Point(10.0, 0.0), Point(10.0, 10.0),
            OffsetSegmentGenerator.RightSide, 2.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(join, Has.Length.EqualTo(5));
            AssertCoordinate(join[0], 10.0, -2.0);
            AssertCoordinate(join[4], 12.0, 0.0);
            AssertCoordinate(join[2],
                10.0 + Math.Sqrt(2.0), -Math.Sqrt(2.0), 1e-12);
            Assert.That(join.All(coordinate =>
                Math.Abs(Distance(coordinate, Point(10.0, 0.0)) - 2.0)
                    < 1e-12), Is.True);
        });
    }

    [Test]
    public void MitreJoinHonorsLimitAndBevelJoinUsesOffsetEnds()
    {
        BufferParameters mitre = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Mitre, 8, 2.0,
            FloatingPrecision());
        BufferParameters limited = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Mitre, 8, 1.0,
            FloatingPrecision());
        BufferParameters bevel = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Bevel, 8, 5.0,
            FloatingPrecision());

        Coordinate[] mitreJoin = CreateRightAngleJoin(mitre);
        Coordinate[] limitedJoin = CreateRightAngleJoin(limited);
        Coordinate[] bevelJoin = CreateRightAngleJoin(bevel);

        Assert.Multiple(() =>
        {
            Assert.That(mitreJoin, Has.Length.EqualTo(1));
            AssertCoordinate(mitreJoin[0], 12.0, -2.0);
            Assert.That(limitedJoin, Has.Length.EqualTo(2));
            AssertCoordinate(limitedJoin[0], 10.0, -2.0);
            AssertCoordinate(limitedJoin[1], 12.0, 0.0);
            Assert.That(bevelJoin, Has.Length.EqualTo(2));
            AssertCoordinate(bevelJoin[0], 10.0, -2.0);
            AssertCoordinate(bevelJoin[1], 12.0, 0.0);
        });
    }

    [Test]
    public void InsideTurnsMeetAtRobustOffsetLineIntersection()
    {
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 8, 5.0,
            FloatingPrecision());

        Coordinate[] join = OffsetSegmentGenerator.CreateJoin(
            Point(0.0, 0.0), Point(10.0, 0.0), Point(10.0, 10.0),
            OffsetSegmentGenerator.LeftSide, 2.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(join, Has.Length.EqualTo(1));
            AssertCoordinate(join[0], 8.0, 2.0);
        });
    }

    [Test]
    public void EndCapsImplementRoundFlatAndSquarePolicies()
    {
        Coordinate adjacent = Point(0.0, 0.0);
        Coordinate endpoint = Point(10.0, 0.0);
        Coordinate[] round = OffsetSegmentGenerator.CreateEndCap(
            adjacent, endpoint, 2.0,
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Round,
                3, 5.0, FloatingPrecision()));
        Coordinate[] flat = OffsetSegmentGenerator.CreateEndCap(
            adjacent, endpoint, 2.0,
            Parameters(BufferCapStyle.Flat, BufferJoinStyle.Round,
                3, 5.0, FloatingPrecision()));
        Coordinate[] square = OffsetSegmentGenerator.CreateEndCap(
            adjacent, endpoint, 2.0,
            Parameters(BufferCapStyle.Square, BufferJoinStyle.Round,
                3, 5.0, FloatingPrecision()));

        Assert.Multiple(() =>
        {
            Assert.That(round, Has.Length.EqualTo(7));
            AssertCoordinate(round[0], 10.0, 2.0);
            AssertCoordinate(round[3], 12.0, 0.0, 1e-12);
            AssertCoordinate(round[6], 10.0, -2.0);
            Assert.That(flat, Has.Length.EqualTo(2));
            AssertCoordinate(flat[0], 10.0, 2.0);
            AssertCoordinate(flat[1], 10.0, -2.0);
            Assert.That(square, Has.Length.EqualTo(4));
            AssertCoordinate(square[0], 10.0, 2.0);
            AssertCoordinate(square[1], 12.0, 2.0);
            AssertCoordinate(square[2], 12.0, -2.0);
            AssertCoordinate(square[3], 10.0, -2.0);
        });
    }

    [Test]
    public void CircleAndSquarePointCurvesAreClosedOwningSequences()
    {
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 4, 5.0,
            FloatingPrecision());
        Coordinate center = Point(3.0, 4.0);

        Coordinate[] circle = OffsetSegmentGenerator.CreateCircle(
            center, 2.0, parameters);
        Coordinate[] square = OffsetSegmentGenerator.CreateSquare(
            center, 2.0, parameters);
        center.X = 30.0;

        Assert.Multiple(() =>
        {
            Assert.That(circle, Has.Length.EqualTo(17));
            AssertCoordinate(circle[0], 5.0, 4.0);
            AssertCoordinate(circle[4], 3.0, 2.0, 1e-12);
            AssertCoordinate(circle[8], 1.0, 4.0, 1e-12);
            AssertCoordinate(circle[16], 5.0, 4.0);
            Assert.That(square, Has.Length.EqualTo(5));
            AssertCoordinate(square[0], 5.0, 6.0);
            AssertCoordinate(square[4], 5.0, 6.0);
        });
    }

    [Test]
    public void FixedPrecisionUsesGridOffsetsAndRemovesRoundedDuplicates()
    {
        PrecisionModel precision = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 2.0,
            OffsetX = 0.25,
            OffsetY = 0.25
        };
        Coordinate[] segment = OffsetSegmentGenerator.CreateOffsetSegment(
            Point(0.0, 0.0), Point(10.0, 0.0),
            OffsetSegmentGenerator.LeftSide, 0.6, precision);
        Coordinate[] circle = OffsetSegmentGenerator.CreateCircle(
            Point(0.25, 0.25), 0.2,
            Parameters(BufferCapStyle.Round, BufferJoinStyle.Round,
                8, 5.0, precision));

        Assert.Multiple(() =>
        {
            AssertCoordinate(segment[0], -0.25, 0.75);
            AssertCoordinate(segment[1], 10.25, 0.75);
            Assert.That(circle, Has.Length.EqualTo(1));
            AssertCoordinate(circle[0], 0.25, 0.25);
        });
    }

    [Test]
    public void ExactTurnClassificationKeepsNearCollinearFilletsLocal()
    {
        Coordinate previous = Point(0.0, 0.0);
        Coordinate vertex = Point(134217729.0, 134217728.0);
        Coordinate next = Point(134217730.0, 134217729.0);
        BufferParameters parameters = Parameters(
            BufferCapStyle.Round, BufferJoinStyle.Round, 8, 5.0,
            FloatingPrecision());

        Coordinate[] join = OffsetSegmentGenerator.CreateJoin(
            previous, vertex, next,
            OffsetSegmentGenerator.RightSide, 1.0, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(RobustOrientation.GetOrientationIndex(
                previous, vertex, next), Is.EqualTo(1));
            Assert.That(join, Is.Not.Empty);
            Assert.That(join.All(coordinate =>
                double.IsFinite(coordinate.X) &&
                double.IsFinite(coordinate.Y)), Is.True);
            Assert.That(join.All(coordinate =>
                Distance(coordinate, vertex) < 1.000001), Is.True);
        });
    }

    [Test]
    public void InvalidParametersAndDegenerateSegmentsAreRejected()
    {
        BufferParameters invalidQuadrants = new BufferParameters
        {
            QuadrantSegments = 0
        };
        BufferParameters invalidMitre = new BufferParameters
        {
            MitreLimit = double.NaN
        };
        BufferParameters invalidPrecision = new BufferParameters
        {
            Precision = new PrecisionModel
            {
                Type = PrecisionModelType.Fixed,
                Scale = 0.0
            }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetSegmentGenerator.CreateOffsetSegment(
                    Point(0.0, 0.0), Point(1.0, 0.0),
                    0, 1.0, FloatingPrecision()));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetSegmentGenerator.CreateOffsetSegment(
                    Point(0.0, 0.0), Point(1.0, 0.0),
                    OffsetSegmentGenerator.LeftSide, 0.0,
                    FloatingPrecision()));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetSegmentGenerator.CreateOffsetSegment(
                    Point(1.0, 1.0), Point(1.0, 1.0),
                    OffsetSegmentGenerator.LeftSide, 1.0,
                    FloatingPrecision()));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetSegmentGenerator.CreateEndCap(
                    Point(0.0, 0.0), Point(1.0, 0.0), 1.0,
                    invalidQuadrants));
            Assert.Throws<InvalidGeometryException>(() =>
                OffsetSegmentGenerator.CreateEndCap(
                    Point(0.0, 0.0), Point(1.0, 0.0), 1.0,
                    invalidMitre));
            Assert.Throws<PrecisionException>(() =>
                OffsetSegmentGenerator.CreateEndCap(
                    Point(0.0, 0.0), Point(1.0, 0.0), 1.0,
                    invalidPrecision));
        });
    }

    private static Coordinate[] CreateRightAngleJoin(
        BufferParameters parameters)
    {
        return OffsetSegmentGenerator.CreateJoin(
            Point(0.0, 0.0), Point(10.0, 0.0), Point(10.0, 10.0),
            OffsetSegmentGenerator.RightSide, 2.0, parameters);
    }

    private static BufferParameters Parameters(
        BufferCapStyle capStyle, BufferJoinStyle joinStyle,
        int quadrantSegments, double mitreLimit,
        PrecisionModel precision)
    {
        return new BufferParameters
        {
            CapStyle = capStyle,
            JoinStyle = joinStyle,
            QuadrantSegments = quadrantSegments,
            MitreLimit = mitreLimit,
            Precision = precision
        };
    }

    private static PrecisionModel FloatingPrecision()
    {
        return new PrecisionModel
        {
            Type = PrecisionModelType.Floating
        };
    }

    private static Coordinate Point(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static Coordinate Point(
        double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static double Distance(Coordinate first, Coordinate second)
    {
        return Math.Sqrt(
            (first.X - second.X) * (first.X - second.X) +
            (first.Y - second.Y) * (first.Y - second.Y));
    }

    private static void AssertCoordinate(
        Coordinate actual, double x, double y)
    {
        AssertCoordinate(actual, x, y, 0.0);
    }

    private static void AssertCoordinate(
        Coordinate actual, double x, double y, double tolerance)
    {
        if (tolerance == 0.0)
        {
            Assert.That(actual.X, Is.EqualTo(x));
            Assert.That(actual.Y, Is.EqualTo(y));
            return;
        }
        Assert.That(actual.X, Is.EqualTo(x).Within(tolerance));
        Assert.That(actual.Y, Is.EqualTo(y).Within(tolerance));
    }
}
