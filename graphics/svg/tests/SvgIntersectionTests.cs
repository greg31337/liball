namespace svg.Tests;

public class SvgIntersectionTests
{
    [Test]
    public void CrossingLinesReportExactPointParametersAndClassification()
    {
        SvgIntersectionResult result = Find(Line(0, 0, 10, 10), Line(0, 10, 10, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 1);
            AssertIntersection(result.Intersections[0], 5, 5, 0.5, 0.5, SvgIntersectionKind.Crossing);
            Assert.That(result.ComparisonCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ParallelDisjointAndCollinearOverlapAreDistinguished()
    {
        SvgIntersectionResult parallel = Find(Line(0, 0, 10, 0), Line(0, 2, 10, 2));
        SvgIntersectionResult overlap = Find(Line(0, 0, 10, 0), Line(4, 0, 8, 0));
        SvgIntersectionResult endpoint = Find(Line(0, 0, 5, 0), Line(5, 0, 10, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(parallel, 0);
            AssertSuccess(overlap, 2);
            AssertIntersection(overlap.Intersections[0], 4, 0, 0.4, 0, SvgIntersectionKind.Overlap);
            AssertIntersection(overlap.Intersections[1], 8, 0, 0.8, 1, SvgIntersectionKind.Overlap);
            AssertSuccess(endpoint, 1);
            AssertIntersection(endpoint.Intersections[0], 5, 0, 1, 0, SvgIntersectionKind.Overlap);
        });
    }

    [Test]
    public void LineQuadraticFindsTwoOrderedIntersections()
    {
        SvgQuadraticBezier curve = new SvgQuadraticBezier(Point(0, 0), Point(5, 10), Point(10, 0));
        SvgIntersectionOptions options = Options(0.00001);

        SvgIntersectionResult result = new SvgIntersections().Find(new SvgIntersectionSegment(Line(0, 3, 10, 3)), SvgIntersectionSegment.CreateQuadratic(curve), options);

        double firstParameter = (1 - System.Math.Sqrt(0.4)) / 2;
        Assert.Multiple(() =>
        {
            AssertSuccess(result, 2);
            Assert.That(result.Intersections[0].SecondAmount, Is.EqualTo(firstParameter).Within(0.0001));
            Assert.That(result.Intersections[0].Point.Y, Is.EqualTo(3).Within(0.0001));
            Assert.That(result.Intersections[1].SecondAmount, Is.EqualTo(1 - firstParameter).Within(0.0001));
            Assert.That(result.Intersections.All(item => item.Kind == SvgIntersectionKind.Crossing), Is.True);
        });
    }

    [Test]
    public void TangentContactMergesAdjacentFlattenedCandidates()
    {
        SvgQuadraticBezier curve = new SvgQuadraticBezier(Point(0, 0), Point(5, 10), Point(10, 0));
        SvgIntersectionOptions options = Options(0.00001);

        SvgIntersectionResult result = new SvgIntersections().Find(new SvgIntersectionSegment(Line(0, 5, 10, 5)), SvgIntersectionSegment.CreateQuadratic(curve), options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 1);
            AssertIntersection(result.Intersections[0], 5, 5, 0.5, 0.5, SvgIntersectionKind.Tangent, 0.0001);
            Assert.That(result.Intersections[0].DuplicateCount, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public void QuadraticCubicAndCurveCurveQueriesUseSourceParameters()
    {
        SvgQuadraticBezier first = new SvgQuadraticBezier(Point(0, 0), Point(5, 10), Point(10, 0));
        SvgCubicBezier vertical = new SvgCubicBezier(Point(5, -2), Point(5, 1), Point(5, 4), Point(5, 7));
        SvgQuadraticBezier second = new SvgQuadraticBezier(Point(0, 5), Point(5, -5), Point(10, 5));
        SvgIntersectionOptions options = Options(0.0001);

        SvgIntersectionResult quadraticCubic = new SvgIntersections().Find(SvgIntersectionSegment.CreateQuadratic(first), SvgIntersectionSegment.CreateCubic(vertical), options);
        SvgIntersectionResult curveCurve = new SvgIntersections().Find(SvgIntersectionSegment.CreateQuadratic(first), SvgIntersectionSegment.CreateQuadratic(second), options);

        Assert.Multiple(() =>
        {
            AssertSuccess(quadraticCubic, 1);
            Assert.That(quadraticCubic.Intersections[0].FirstAmount, Is.EqualTo(0.5).Within(0.001));
            Assert.That(quadraticCubic.Intersections[0].Point, Is.Not.Null);
            AssertSuccess(curveCurve, 2);
            Assert.That(curveCurve.Intersections[0].FirstAmount, Is.LessThan(curveCurve.Intersections[1].FirstAmount));
        });
    }

    [Test]
    public void NormalizedArcIntersectsLinesAndOtherArcs()
    {
        SvgArc upper = Arc(Point(-1, 0), Point(1, 0), false, false);
        SvgArc offsetLeft = Arc(Point(0.5, -1), Point(0.5, 1), false, false);
        SvgIntersectionOptions options = Options(0.00001);

        SvgIntersectionResult lineArc = new SvgIntersections().Find(new SvgIntersectionSegment(Line(-2, 0.5, 2, 0.5)), SvgIntersectionSegment.CreateArc(upper), options);
        SvgIntersectionResult arcArc = new SvgIntersections().Find(SvgIntersectionSegment.CreateArc(upper), SvgIntersectionSegment.CreateArc(offsetLeft), options);

        Assert.Multiple(() =>
        {
            AssertSuccess(lineArc, 2);
            Assert.That(lineArc.Intersections[0].Point.Y, Is.EqualTo(0.5).Within(0.0001));
            AssertSuccess(arcArc, 1);
            Assert.That(arcArc.Intersections[0].Point.X, Is.EqualTo(0.25).Within(0.001));
            Assert.That(arcArc.Intersections[0].Point.Y, Is.EqualTo(System.Math.Sqrt(15) / 4).Within(0.001));
        });
    }

    [Test]
    public void DegeneratePointSegmentsIntersectDeterministically()
    {
        SvgIntersectionResult onLine = Find(Line(2, 0, 2, 0), Line(0, 0, 4, 0));
        SvgIntersectionResult away = Find(Line(2, 1, 2, 1), Line(0, 0, 4, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(onLine, 1);
            Assert.That(onLine.Intersections[0].Point.X, Is.EqualTo(2));
            AssertSuccess(away, 0);
        });
    }

    [Test]
    public void InvalidMutationAndAdaptiveLimitsReturnStableFailures()
    {
        SvgIntersectionSegment line = new SvgIntersectionSegment(Line(0, 0, 10, 0));
        SvgIntersectionOptions invalidTolerance = Options(1);
        invalidTolerance.Tolerance = double.NaN;
        SvgIntersectionOptions depth = Options(0.00001);
        depth.MaximumSubdivisionDepth = 0;
        SvgIntersectionOptions segmentLimit = Options(0.00001);
        segmentLimit.MaximumFlattenedSegments = 1;
        SvgIntersectionOptions comparisonLimit = Options(0.01);
        comparisonLimit.MaximumComparisons = 1;
        SvgIntersectionOptions intersectionLimit = Options(0.00001);
        intersectionLimit.MaximumIntersections = 1;
        SvgLineSegment mutated = Line(0, 0, 1, 1);
        mutated.End.X = double.NaN;
        SvgQuadraticBezier curve = new SvgQuadraticBezier(Point(0, 0), Point(5, 10), Point(10, 0));

        Assert.Multiple(() =>
        {
            AssertFailure(new SvgIntersections().Find(null, line, Options(1)), SvgIntersectionError.InvalidArgument);
            AssertFailure(new SvgIntersections().Find(line, line, invalidTolerance), SvgIntersectionError.InvalidArgument);
            AssertFailure(new SvgIntersections().Find(new SvgIntersectionSegment(mutated), line, Options(1)), SvgIntersectionError.InvalidArgument);
            AssertFailure(new SvgIntersections().Find(SvgIntersectionSegment.CreateQuadratic(curve), line, depth), SvgIntersectionError.SubdivisionLimitExceeded);
            AssertFailure(new SvgIntersections().Find(SvgIntersectionSegment.CreateQuadratic(curve), line, segmentLimit), SvgIntersectionError.SegmentLimitExceeded);
            AssertFailure(new SvgIntersections().Find(SvgIntersectionSegment.CreateQuadratic(curve), SvgIntersectionSegment.CreateQuadratic(curve), comparisonLimit), SvgIntersectionError.ComparisonLimitExceeded);
            AssertFailure(new SvgIntersections().Find(new SvgIntersectionSegment(Line(0, 3, 10, 3)), SvgIntersectionSegment.CreateQuadratic(curve), intersectionLimit), SvgIntersectionError.IntersectionLimitExceeded);
        });
    }

    static SvgIntersectionResult Find(SvgLineSegment first, SvgLineSegment second)
    {
        return new SvgIntersections().Find(new SvgIntersectionSegment(first), new SvgIntersectionSegment(second), Options(0.000001));
    }

    static SvgIntersectionOptions Options(double tolerance)
    {
        SvgIntersectionOptions options = new SvgIntersectionOptions();
        options.Tolerance = tolerance;
        return options;
    }

    static SvgLineSegment Line(double x1, double y1, double x2, double y2)
    {
        return new SvgLineSegment(Point(x1, y1), Point(x2, y2));
    }

    static SvgPoint Point(double x, double y)
    {
        return new SvgPoint(x, y);
    }

    static SvgArc Arc(SvgPoint start, SvgPoint end, bool large, bool sweep)
    {
        SvgArcOperationResult result = new SvgArcGeometry().FromEndpoint(start, end, 1, 1, 0, large, sweep);
        Assert.That(result.Success, Is.True);
        return result.Arc;
    }

    static void AssertSuccess(SvgIntersectionResult result, int count)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgIntersectionError.None));
        Assert.That(result.Intersections, Has.Count.EqualTo(count));
    }

    static void AssertFailure(SvgIntersectionResult result, SvgIntersectionError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Intersections, Is.Null);
    }

    static void AssertIntersection(SvgIntersection intersection, double x, double y, double firstAmount, double secondAmount, SvgIntersectionKind kind, double tolerance = 1e-10)
    {
        Assert.That(intersection.Point.X, Is.EqualTo(x).Within(tolerance));
        Assert.That(intersection.Point.Y, Is.EqualTo(y).Within(tolerance));
        Assert.That(intersection.FirstAmount, Is.EqualTo(firstAmount).Within(tolerance));
        Assert.That(intersection.SecondAmount, Is.EqualTo(secondAmount).Within(tolerance));
        Assert.That(intersection.Kind, Is.EqualTo(kind));
    }
}
