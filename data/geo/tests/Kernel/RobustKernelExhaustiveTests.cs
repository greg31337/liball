using geo;

namespace geo.Tests;

public class RobustKernelExhaustiveTests
{
    [Test]
    public void CoordinateVectorAndInterpolationMatrixMatchesIntegerArithmetic()
    {
        ICoordinateAlgorithms algorithms = new CoordinateAlgorithms();
        double[] factors = [0.0, 0.25, 0.5, 0.75, 1.0];
        int firstX = -4;
        while (firstX <= 4)
        {
            int firstY = -4;
            while (firstY <= 4)
            {
                int secondX = -4;
                while (secondX <= 4)
                {
                    int secondY = -4;
                    while (secondY <= 4)
                    {
                        Coordinate first = CoordinateValue(firstX, firstY);
                        Coordinate second = CoordinateValue(secondX, secondY);
                        Assert.That(algorithms.DotProduct(first, second),
                            Is.EqualTo(firstX * secondX + firstY * secondY));
                        Assert.That(algorithms.CrossProduct(first, second),
                            Is.EqualTo(firstX * secondY - firstY * secondX));
                        int factorIndex = 0;
                        while (factorIndex < factors.Length)
                        {
                            double factor = factors[factorIndex];
                            Coordinate interpolated = algorithms.InterpolateCoordinate(
                                first, second, factor);
                            Assert.That(interpolated.X,
                                Is.EqualTo((1.0 - factor) * firstX + factor * secondX));
                            Assert.That(interpolated.Y,
                                Is.EqualTo((1.0 - factor) * firstY + factor * secondY));
                            factorIndex++;
                        }
                        secondY++;
                    }
                    secondX++;
                }
                firstY++;
            }
            firstX++;
        }
    }

    [Test]
    public void OrientationExhaustiveSmallGridMatchesExactIntegerOracle()
    {
        Coordinate[] points = GridPoints(-2, 2);
        int first = 0;
        while (first < points.Length)
        {
            int second = 0;
            while (second < points.Length)
            {
                int third = 0;
                while (third < points.Length)
                {
                    int expected = IntegerOrientation(
                        points[first], points[second], points[third]);
                    int actual = RobustOrientation.GetOrientationIndex(
                        points[first], points[second], points[third]);
                    Assert.That(actual, Is.EqualTo(expected));
                    Assert.That(RobustOrientation.GetOrientationIndex(
                        points[first], points[third], points[second]), Is.EqualTo(-expected));
                    third++;
                }
                second++;
            }
            first++;
        }
    }

    [Test]
    public void DeterminantExhaustiveIntegerGridAndExponentRegressionsHaveExactSigns()
    {
        int firstX = -6;
        while (firstX <= 6)
        {
            int firstY = -6;
            while (firstY <= 6)
            {
                int secondX = -6;
                while (secondX <= 6)
                {
                    int secondY = -6;
                    while (secondY <= 6)
                    {
                        long determinant = (long)firstX * secondY -
                            (long)firstY * secondX;
                        int expected = Sign(determinant);
                        Assert.That(RobustDeterminant.GetSign2x2(
                            firstX, firstY, secondX, secondY), Is.EqualTo(expected));
                        secondY++;
                    }
                    secondX++;
                }
                firstY++;
            }
            firstX++;
        }

        int[] exponents = [-1073, -900, -540, -500, 0, 500, 1000];
        int index = 0;
        while (index < exponents.Length)
        {
            double scale = Math.ScaleB(1.0, exponents[index]);
            Assert.That(RobustDeterminant.GetSign2x2(
                scale * 3.0, scale * 2.0, scale, scale * 4.0), Is.EqualTo(1));
            Assert.That(RobustDeterminant.GetSign2x2(
                scale, scale * 4.0, scale * 3.0, scale * 2.0), Is.EqualTo(-1));
            index++;
        }

        double basis = 134217728.0;
        Assert.That(RobustDeterminant.GetSign2x2(
            basis + 1.0, basis, basis + 2.0, basis + 1.0), Is.EqualTo(1));
        Assert.That(RobustDeterminant.GetSign2x2(
            double.MaxValue, Math.BitDecrement(double.MaxValue),
            double.Epsilon, double.Epsilon), Is.EqualTo(1));
    }

    [Test]
    public void RectangleRingOrientationAndPointLocationMatrixMatchesBoundsOracle()
    {
        int minimumX = -3;
        while (minimumX < 3)
        {
            int maximumX = minimumX + 1;
            while (maximumX <= 3)
            {
                int minimumY = -3;
                while (minimumY < 3)
                {
                    int maximumY = minimumY + 1;
                    while (maximumY <= 3)
                    {
                        Coordinate[] ring = RectangleRing(
                            minimumX, minimumY, maximumX, maximumY);
                        Assert.That(RingMath.GetRingOrientation(ring),
                            Is.EqualTo(RingOrientation.CounterClockwise));
                        Assert.That(RingMath.GetRingOrientation(ring.Reverse().ToArray()),
                            Is.EqualTo(RingOrientation.Clockwise));
                        int x = -4;
                        while (x <= 4)
                        {
                            int y = -4;
                            while (y <= 4)
                            {
                                TopologicalLocation expected = RectangleLocation(
                                    x, y, minimumX, minimumY, maximumX, maximumY);
                                Coordinate point = CoordinateValue(x, y);
                                Assert.That(RingPointLocation.LocatePointByRayCrossing(point, ring),
                                    Is.EqualTo(expected));
                                Assert.That(RingPointLocation.LocatePointByWinding(point, ring),
                                    Is.EqualTo(expected));
                                y++;
                            }
                            x++;
                        }
                        maximumY++;
                    }
                    minimumY++;
                }
                maximumX++;
            }
            minimumX++;
        }
    }

    [Test]
    public void PolygonPointLocationGridMatchesShellAndHoleOracle()
    {
        Geometry polygon = new Geometry
        {
            GeometryType = GeometryType.Polygon,
            Point = null,
            Polygon = new Polygon
            {
                Shell = new LinearRing { Coordinates = RectangleRing(0, 0, 10, 10) },
                Holes = [new LinearRing { Coordinates = RectangleRing(3, 3, 7, 7) }]
            }
        };
        CurveLinearizationParameters parameters = new CurveLinearizationParameters();
        int x = -1;
        while (x <= 11)
        {
            int y = -1;
            while (y <= 11)
            {
                TopologicalLocation expected = PolygonWithHoleLocation(x, y);
                Assert.That(GeometryPointLocation.LocatePoint(
                    CoordinateValue(x, y), polygon, parameters), Is.EqualTo(expected));
                y++;
            }
            x++;
        }
    }

    [Test]
    public void SegmentIntersectionExhaustiveGridMatchesIndependentIntegerOracle()
    {
        Coordinate[] points = GridPoints(-1, 1);
        int firstStart = 0;
        while (firstStart < points.Length)
        {
            int firstEnd = 0;
            while (firstEnd < points.Length)
            {
                int secondStart = 0;
                while (secondStart < points.Length)
                {
                    int secondEnd = 0;
                    while (secondEnd < points.Length)
                    {
                        SegmentIntersectionType expected = ReferenceIntersectionType(
                            points[firstStart], points[firstEnd],
                            points[secondStart], points[secondEnd]);
                        SegmentIntersectionResult result =
                            RobustLineIntersection.GetSegmentIntersection(
                                points[firstStart], points[firstEnd],
                                points[secondStart], points[secondEnd]);
                        SegmentIntersectionResult reversed =
                            RobustLineIntersection.GetSegmentIntersection(
                                points[secondStart], points[secondEnd],
                                points[firstStart], points[firstEnd]);
                        Assert.That(result.Type, Is.EqualTo(expected));
                        Assert.That(reversed.Type, Is.EqualTo(expected));
                        Assert.That(result.IsProper,
                            Is.EqualTo(expected == SegmentIntersectionType.ProperPoint));
                        if (expected == SegmentIntersectionType.None)
                        {
                            Assert.That(result.FirstIntersection, Is.Null);
                        }
                        else
                        {
                            Assert.That(result.FirstIntersection, Is.Not.Null);
                            if (expected == SegmentIntersectionType.ProperPoint)
                            {
                                Assert.That(ApproximatelyOnSegment(
                                    result.FirstIntersection!, points[firstStart], points[firstEnd]), Is.True);
                                Assert.That(ApproximatelyOnSegment(
                                    result.FirstIntersection, points[secondStart], points[secondEnd]), Is.True);
                            }
                            else
                            {
                                Assert.That(SegmentDistanceAlgorithms.IsPointOnSegment(
                                    result.FirstIntersection!, points[firstStart], points[firstEnd]), Is.True);
                                Assert.That(SegmentDistanceAlgorithms.IsPointOnSegment(
                                    result.FirstIntersection, points[secondStart], points[secondEnd]), Is.True);
                            }
                        }
                        secondEnd++;
                    }
                    secondStart++;
                }
                firstEnd++;
            }
            firstStart++;
        }
    }

    [Test]
    public void FloatingPointRegressionCorpusPreservesOrientationAndIntersectionInvariants()
    {
        double[] scales =
        [
            double.Epsilon, Math.ScaleB(1.0, -900), Math.ScaleB(1.0, -540),
            1.0, Math.ScaleB(1.0, 500), Math.ScaleB(1.0, 1000)
        ];
        int index = 0;
        while (index < scales.Length)
        {
            double scale = scales[index];
            Coordinate first = CoordinateValue(-scale, -scale);
            Coordinate second = CoordinateValue(scale, -scale);
            Coordinate third = CoordinateValue(-scale, scale);
            Assert.That(RobustOrientation.GetOrientationIndex(first, second, third),
                Is.EqualTo(1));
            Assert.That(RobustOrientation.GetOrientationIndex(first, third, second),
                Is.EqualTo(-1));
            SegmentIntersectionResult crossing = RobustLineIntersection.GetSegmentIntersection(
                CoordinateValue(-scale, 0.0), CoordinateValue(scale, 0.0),
                CoordinateValue(0.0, -scale), CoordinateValue(0.0, scale));
            Assert.That(crossing.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
            Assert.That(crossing.FirstIntersection!.X, Is.Zero);
            Assert.That(crossing.FirstIntersection.Y, Is.Zero);
            index++;
        }

        SegmentIntersectionResult extreme = RobustLineIntersection.GetSegmentIntersection(
            CoordinateValue(-double.MaxValue, 0.0), CoordinateValue(double.MaxValue, 0.0),
            CoordinateValue(0.0, -double.MaxValue), CoordinateValue(0.0, double.MaxValue));
        Assert.That(extreme.Type, Is.EqualTo(SegmentIntersectionType.ProperPoint));
        Assert.That(extreme.FirstIntersection!.X, Is.Zero);
        Assert.That(extreme.FirstIntersection.Y, Is.Zero);
    }

    private static SegmentIntersectionType ReferenceIntersectionType(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        if (!ReferenceEnvelopesIntersect(firstStart, firstEnd, secondStart, secondEnd))
        {
            return SegmentIntersectionType.None;
        }
        int firstSide = IntegerOrientation(firstStart, firstEnd, secondStart);
        int secondSide = IntegerOrientation(firstStart, firstEnd, secondEnd);
        int thirdSide = IntegerOrientation(secondStart, secondEnd, firstStart);
        int fourthSide = IntegerOrientation(secondStart, secondEnd, firstEnd);
        if (firstSide == 0 && secondSide == 0 && thirdSide == 0 && fourthSide == 0)
        {
            Coordinate firstLow = ComparePlanar(firstStart, firstEnd) <= 0 ? firstStart : firstEnd;
            Coordinate firstHigh = firstLow == firstStart ? firstEnd : firstStart;
            Coordinate secondLow = ComparePlanar(secondStart, secondEnd) <= 0 ? secondStart : secondEnd;
            Coordinate secondHigh = secondLow == secondStart ? secondEnd : secondStart;
            Coordinate low = ComparePlanar(firstLow, secondLow) >= 0 ? firstLow : secondLow;
            Coordinate high = ComparePlanar(firstHigh, secondHigh) <= 0 ? firstHigh : secondHigh;
            int comparison = ComparePlanar(low, high);
            if (comparison > 0) return SegmentIntersectionType.None;
            if (comparison == 0) return SegmentIntersectionType.CollinearPoint;
            return SegmentIntersectionType.CollinearOverlap;
        }
        if (firstSide == 0 && ReferencePointInEnvelope(secondStart, firstStart, firstEnd) ||
            secondSide == 0 && ReferencePointInEnvelope(secondEnd, firstStart, firstEnd) ||
            thirdSide == 0 && ReferencePointInEnvelope(firstStart, secondStart, secondEnd) ||
            fourthSide == 0 && ReferencePointInEnvelope(firstEnd, secondStart, secondEnd))
        {
            return SegmentIntersectionType.Endpoint;
        }
        if (SignsOpposite(firstSide, secondSide) && SignsOpposite(thirdSide, fourthSide))
        {
            return SegmentIntersectionType.ProperPoint;
        }
        return SegmentIntersectionType.None;
    }

    private static bool ReferenceEnvelopesIntersect(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        return Math.Min(firstStart.X, firstEnd.X) <= Math.Max(secondStart.X, secondEnd.X) &&
            Math.Max(firstStart.X, firstEnd.X) >= Math.Min(secondStart.X, secondEnd.X) &&
            Math.Min(firstStart.Y, firstEnd.Y) <= Math.Max(secondStart.Y, secondEnd.Y) &&
            Math.Max(firstStart.Y, firstEnd.Y) >= Math.Min(secondStart.Y, secondEnd.Y);
    }

    private static bool ReferencePointInEnvelope(
        Coordinate point, Coordinate start, Coordinate end)
    {
        return point.X >= Math.Min(start.X, end.X) && point.X <= Math.Max(start.X, end.X) &&
            point.Y >= Math.Min(start.Y, end.Y) && point.Y <= Math.Max(start.Y, end.Y);
    }

    private static bool SignsOpposite(int first, int second)
    {
        return first < 0 && second > 0 || first > 0 && second < 0;
    }

    private static int ComparePlanar(Coordinate first, Coordinate second)
    {
        if (first.X < second.X) return -1;
        if (first.X > second.X) return 1;
        if (first.Y < second.Y) return -1;
        if (first.Y > second.Y) return 1;
        return 0;
    }

    private static int IntegerOrientation(
        Coordinate first, Coordinate second, Coordinate third)
    {
        long firstX = (long)second.X - (long)first.X;
        long firstY = (long)second.Y - (long)first.Y;
        long secondX = (long)third.X - (long)first.X;
        long secondY = (long)third.Y - (long)first.Y;
        return Sign(firstX * secondY - firstY * secondX);
    }

    private static int Sign(long value)
    {
        return value < 0 ? -1 : value > 0 ? 1 : 0;
    }

    private static Coordinate[] GridPoints(int minimum, int maximum)
    {
        Coordinate[] points = new Coordinate[(maximum - minimum + 1) * (maximum - minimum + 1)];
        int count = 0;
        int x = minimum;
        while (x <= maximum)
        {
            int y = minimum;
            while (y <= maximum)
            {
                points[count] = CoordinateValue(x, y);
                count++;
                y++;
            }
            x++;
        }
        return points;
    }

    private static Coordinate[] RectangleRing(
        int minimumX, int minimumY, int maximumX, int maximumY)
    {
        return
        [
            CoordinateValue(minimumX, minimumY), CoordinateValue(maximumX, minimumY),
            CoordinateValue(maximumX, maximumY), CoordinateValue(minimumX, maximumY),
            CoordinateValue(minimumX, minimumY)
        ];
    }

    private static TopologicalLocation RectangleLocation(
        int x, int y, int minimumX, int minimumY, int maximumX, int maximumY)
    {
        if (x < minimumX || x > maximumX || y < minimumY || y > maximumY)
        {
            return TopologicalLocation.Exterior;
        }
        if (x == minimumX || x == maximumX || y == minimumY || y == maximumY)
        {
            return TopologicalLocation.Boundary;
        }
        return TopologicalLocation.Interior;
    }

    private static TopologicalLocation PolygonWithHoleLocation(int x, int y)
    {
        TopologicalLocation shell = RectangleLocation(x, y, 0, 0, 10, 10);
        if (shell != TopologicalLocation.Interior) return shell;
        TopologicalLocation hole = RectangleLocation(x, y, 3, 3, 7, 7);
        if (hole == TopologicalLocation.Boundary) return TopologicalLocation.Boundary;
        if (hole == TopologicalLocation.Interior) return TopologicalLocation.Exterior;
        return TopologicalLocation.Interior;
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static bool ApproximatelyOnSegment(
        Coordinate point, Coordinate start, Coordinate end)
    {
        if (!ReferencePointInEnvelope(point, start, end)) return false;
        double deltaX = end.X - start.X;
        double deltaY = end.Y - start.Y;
        double pointX = point.X - start.X;
        double pointY = point.Y - start.Y;
        double cross = deltaX * pointY - deltaY * pointX;
        return Math.Abs(cross) <= 1.0e-12;
    }
}
