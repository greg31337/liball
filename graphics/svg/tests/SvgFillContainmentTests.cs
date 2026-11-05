namespace svg.Tests;

public class SvgFillContainmentTests
{
    [Test]
    public void ClosedAndImplicitlyClosedSubpathsClassifyInsideOutsideAndBoundary()
    {
        SvgPathData closed = Parse("M0 0 L10 0 L10 10 L0 10 Z");
        SvgPathData open = Parse("M0 0 L10 0 L10 10 L0 10");

        SvgFillContainmentResult inside = closed.ClassifyFill(Point(5, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult outside = closed.ClassifyFill(Point(15, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult edge = open.ClassifyFill(Point(0, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult openInside = open.ClassifyFill(Point(5, 5), SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertKind(inside, SvgFillContainmentKind.Inside, true);
            Assert.That(inside.WindingNumber, Is.EqualTo(1));
            AssertKind(outside, SvgFillContainmentKind.Outside, false);
            AssertKind(edge, SvgFillContainmentKind.Boundary, true);
            AssertKind(openInside, SvgFillContainmentKind.Inside, true);
        });
    }

    [Test]
    public void NonzeroAndEvenOddDifferForNestedSameDirectionContours()
    {
        SvgPathData nested = Parse("M0 0 L10 0 L10 10 L0 10 Z M2 2 L8 2 L8 8 L2 8 Z");

        SvgFillContainmentResult nonzero = nested.ClassifyFill(Point(5, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult evenOdd = nested.ClassifyFill(Point(5, 5), SvgFillRule.EvenOdd);
        SvgFillContainmentResult ring = nested.ClassifyFill(Point(1, 1), SvgFillRule.EvenOdd);

        Assert.Multiple(() =>
        {
            AssertKind(nonzero, SvgFillContainmentKind.Inside, true);
            Assert.That(nonzero.WindingNumber, Is.EqualTo(2));
            Assert.That(nonzero.CrossingCount, Is.EqualTo(2));
            AssertKind(evenOdd, SvgFillContainmentKind.Outside, false);
            Assert.That(evenOdd.CrossingCount, Is.EqualTo(2));
            AssertKind(ring, SvgFillContainmentKind.Inside, true);
        });
    }

    [Test]
    public void OppositeContourCreatesANonzeroHoleAndVertexRaysRemainStable()
    {
        SvgPathData hole = Parse("M0 0 L10 0 L10 10 L0 10 Z M2 2 L2 8 L8 8 L8 2 Z");
        SvgPathData square = Parse("M0 0 L10 0 L10 10 L0 10 Z");

        SvgFillContainmentResult center = hole.ClassifyFill(Point(5, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult alignedOutside = square.ClassifyFillWithOptions(Point(15, 0), SvgFillRule.Nonzero, Options(0.000001));
        SvgFillContainmentResult vertex = square.ClassifyFill(Point(10, 0), SvgFillRule.EvenOdd);

        Assert.Multiple(() =>
        {
            AssertKind(center, SvgFillContainmentKind.Outside, false);
            Assert.That(center.WindingNumber, Is.Zero);
            AssertKind(alignedOutside, SvgFillContainmentKind.Outside, false);
            AssertKind(vertex, SvgFillContainmentKind.Boundary, true);
        });
    }

    [Test]
    public void CurvesAndArcsUseAdaptiveBoundaryClassification()
    {
        SvgFillContainmentOptions options = Options(0.0001);
        SvgPathData circle = Parse("M10 0 A10 10 0 1 1 -10 0 A10 10 0 1 1 10 0 Z");
        double diagonal = 10 / System.Math.Sqrt(2);

        SvgFillContainmentResult center = circle.ClassifyFillWithOptions(Point(0, 0), SvgFillRule.Nonzero, options);
        SvgFillContainmentResult curvedBoundary = circle.ClassifyFillWithOptions(Point(diagonal, diagonal), SvgFillRule.Nonzero, options);
        SvgFillContainmentResult outside = circle.ClassifyFillWithOptions(Point(11, 0), SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertKind(center, SvgFillContainmentKind.Inside, true);
            AssertKind(curvedBoundary, SvgFillContainmentKind.Boundary, true);
            AssertKind(outside, SvgFillContainmentKind.Outside, false);
        });
    }

    [Test]
    public void QueriesOperateInTransformedCoordinatesIncludingReflection()
    {
        SvgPathData square = Parse("M0 0 L10 0 L10 10 L0 10 Z");
        SvgFillContainmentOptions scaled = Options(0.0001);
        scaled.Transform = new SvgMatrix(2, 0, 0, 3, 5, 5);
        SvgFillContainmentOptions reflected = Options(0.0001);
        reflected.Transform = new SvgMatrix(-1, 0, 0, 1, 0, 0);

        SvgFillContainmentResult inside = square.ClassifyFillWithOptions(Point(15, 20), SvgFillRule.Nonzero, scaled);
        SvgFillContainmentResult boundary = square.ClassifyFillWithOptions(Point(5, 20), SvgFillRule.Nonzero, scaled);
        SvgFillContainmentResult reflectedInside = square.ClassifyFillWithOptions(Point(-5, 5), SvgFillRule.Nonzero, reflected);

        Assert.Multiple(() =>
        {
            AssertKind(inside, SvgFillContainmentKind.Inside, true);
            AssertKind(boundary, SvgFillContainmentKind.Boundary, true);
            AssertKind(reflectedInside, SvgFillContainmentKind.Inside, true);
            Assert.That(reflectedInside.WindingNumber, Is.EqualTo(-1));
        });
    }

    [Test]
    public void SingularTransformsCollapseFilledAreaToBoundaryOnly()
    {
        SvgPathData square = Parse("M0 0 L10 0 L10 10 L0 10 Z");
        SvgFillContainmentOptions options = Options(0.0001);
        options.Transform = new SvgMatrix(1, 0, 0, 0, 0, 5);

        SvgFillContainmentResult onCollapsedPath = square.ClassifyFillWithOptions(Point(5, 5), SvgFillRule.Nonzero, options);
        SvgFillContainmentResult away = square.ClassifyFillWithOptions(Point(5, 6), SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertKind(onCollapsedPath, SvgFillContainmentKind.Boundary, true);
            AssertKind(away, SvgFillContainmentKind.Outside, false);
        });
    }

    [Test]
    public void BoundaryToleranceAndDegenerateSegmentsAreDeterministic()
    {
        SvgPathData square = Parse("M0 0 L10 0 L10 10 L0 10 Z");
        SvgFillContainmentOptions options = Options(0.001);

        SvgFillContainmentResult near = square.ClassifyFillWithOptions(Point(-0.0005, 5), SvgFillRule.Nonzero, options);
        SvgFillContainmentResult beyond = square.ClassifyFillWithOptions(Point(-0.002, 5), SvgFillRule.Nonzero, options);
        SvgFillContainmentResult zeroLine = Parse("M2 3 L2 3").ClassifyFillWithOptions(Point(2, 3), SvgFillRule.Nonzero, options);
        SvgFillContainmentResult moveOnly = Parse("M2 3").ClassifyFillWithOptions(Point(2, 3), SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertKind(near, SvgFillContainmentKind.Boundary, true);
            AssertKind(beyond, SvgFillContainmentKind.Outside, false);
            AssertKind(zeroLine, SvgFillContainmentKind.Boundary, true);
            AssertKind(moveOnly, SvgFillContainmentKind.Outside, false);
        });
    }

    [Test]
    public void InvalidInputsAndBoundedFlatteningReturnStableFailures()
    {
        SvgPathData square = Parse("M0 0 L10 0 L10 10 L0 10 Z");
        SvgFillContainmentOptions invalidTolerance = Options(1);
        invalidTolerance.Tolerance = double.NaN;
        SvgFillContainmentOptions invalidTransform = Options(1);
        invalidTransform.Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidTransform.Transform.A = double.PositiveInfinity;
        SvgPoint invalidPoint = Point(0, 0);
        invalidPoint.X = double.NaN;
        SvgFillContainmentOptions edgeLimit = Options(1);
        edgeLimit.MaximumSegments = 2;
        SvgFillContainmentOptions depthLimit = Options(0.0001);
        depthLimit.MaximumSubdivisionDepth = 0;

        Assert.Multiple(() =>
        {
            AssertFailure(new SvgFillContainment().ClassifyWithPathAndPointAndFillRule(null, Point(0, 0), SvgFillRule.Nonzero), SvgFillContainmentError.InvalidArgument);
            AssertFailure(square.ClassifyFill(invalidPoint, SvgFillRule.Nonzero), SvgFillContainmentError.InvalidArgument);
            AssertFailure(square.ClassifyFill(Point(0, 0), (SvgFillRule)99), SvgFillContainmentError.InvalidArgument);
            AssertFailure(square.ClassifyFillWithOptions(Point(0, 0), SvgFillRule.Nonzero, invalidTolerance), SvgFillContainmentError.InvalidArgument);
            AssertFailure(square.ClassifyFillWithOptions(Point(0, 0), SvgFillRule.Nonzero, invalidTransform), SvgFillContainmentError.InvalidArgument);
            AssertFailure(Parse("M0 0 L10 0 L0 10").ClassifyFillWithOptions(Point(1, 1), SvgFillRule.Nonzero, edgeLimit), SvgFillContainmentError.SegmentLimitExceeded);
            AssertFailure(Parse("M0 0 Q5 10 10 0").ClassifyFillWithOptions(Point(5, 2), SvgFillRule.Nonzero, depthLimit), SvgFillContainmentError.SubdivisionLimitExceeded);
        });
    }

    [Test]
    public void InvalidMutationAndTransformOverflowDoNotReturnPartialClassification()
    {
        SvgPathData invalid = Parse("M0 0 L1 0");
        invalid.Commands[1].Values[0] = double.NaN;
        SvgFillContainmentOptions overflow = Options(0.01);
        overflow.Transform = new SvgMatrix(double.MaxValue, 0, 0, 1, 0, 0);

        SvgFillContainmentResult invalidResult = invalid.ClassifyFill(Point(0, 0), SvgFillRule.Nonzero);
        SvgFillContainmentResult overflowResult = Parse("M0 0 L2 0 L2 1 Z").ClassifyFillWithOptions(Point(0, 0), SvgFillRule.Nonzero, overflow);

        Assert.Multiple(() =>
        {
            AssertFailure(invalidResult, SvgFillContainmentError.InvalidPath);
            AssertFailure(overflowResult, SvgFillContainmentError.NonFiniteResult);
        });
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPoint Point(double x, double y)
    {
        return new SvgPoint(x, y);
    }

    static SvgFillContainmentOptions Options(double tolerance)
    {
        SvgFillContainmentOptions options = new SvgFillContainmentOptions();
        options.Tolerance = tolerance;
        return options;
    }

    static void AssertKind(SvgFillContainmentResult result, SvgFillContainmentKind kind, bool contains)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgFillContainmentError.None));
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.Contains, Is.EqualTo(contains));
    }

    static void AssertFailure(SvgFillContainmentResult result, SvgFillContainmentError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Kind, Is.EqualTo(SvgFillContainmentKind.Outside));
        Assert.That(result.Contains, Is.False);
        Assert.That(result.WindingNumber, Is.Zero);
        Assert.That(result.CrossingCount, Is.Zero);
    }
}
