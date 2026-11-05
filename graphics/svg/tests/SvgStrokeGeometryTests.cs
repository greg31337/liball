namespace svg.Tests;

public class SvgStrokeGeometryTests
{
    [Test]
    public void ButtStrokeContainmentIncludesTheClosedBandOnly()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgStrokeGeometryOptions options = Options(2);

        SvgStrokeContainmentResult interior = line.ContainsStroke(Point(5, 0.9), options);
        SvgStrokeContainmentResult boundary = line.ContainsStroke(Point(5, 1), options);
        SvgStrokeContainmentResult beyondSide = line.ContainsStroke(Point(5, 1.1), options);
        SvgStrokeContainmentResult beyondCap = line.ContainsStroke(Point(-0.1, 0), options);

        Assert.Multiple(() =>
        {
            AssertContains(interior, true);
            AssertContains(boundary, true);
            AssertContains(beyondSide, false);
            AssertContains(beyondCap, false);
        });
    }

    [Test]
    public void DashedStrokeContainmentAndBoundsHonorOnAndOffIntervals()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgStrokeGeometryOptions options = Options(2);
        options.Style.DashArray.Add(2);
        options.Style.DashArray.Add(2);

        SvgStrokeContainmentResult firstDash = line.ContainsStroke(Point(1, 0), options);
        SvgStrokeContainmentResult firstGap = line.ContainsStroke(Point(3, 0), options);
        SvgStrokeContainmentResult secondDash = line.ContainsStroke(Point(5, 0), options);
        SvgStrokeBoundsResult bounds = line.GetStrokeBounds(options);

        Assert.Multiple(() =>
        {
            AssertContains(firstDash, true);
            AssertContains(firstGap, false);
            AssertContains(secondDash, true);
            AssertBounds(bounds, 0, -1, 10, 1);
        });
    }

    [Test]
    public void CapAndZeroLengthGeometryContributeToStrokeBounds()
    {
        SvgStrokeGeometryOptions square = Options(2);
        square.Style.LineCap = SvgStrokeLineCap.Square;
        SvgStrokeGeometryOptions roundZero = Options(4);
        roundZero.Style.LineCap = SvgStrokeLineCap.Round;
        SvgStrokeGeometryOptions buttZero = Options(4);

        SvgStrokeBoundsResult squareBounds = Parse("M0 0 L10 0").GetStrokeBounds(square);
        SvgStrokeBoundsResult roundBounds = Parse("M2 3 L2 3").GetStrokeBounds(roundZero);
        SvgStrokeBoundsResult emptyBounds = Parse("M2 3 L2 3").GetStrokeBounds(buttZero);
        SvgStrokeContainmentResult roundCenter = Parse("M2 3 L2 3").ContainsStroke(Point(2, 3), roundZero);

        Assert.Multiple(() =>
        {
            AssertBounds(squareBounds, -1, -1, 11, 1);
            AssertBounds(roundBounds, 0, 1, 4, 5, 0.000001);
            Assert.That(emptyBounds.Success, Is.True);
            Assert.That(emptyBounds.Bounds.IsEmpty, Is.True);
            AssertContains(roundCenter, true);
        });
    }

    [Test]
    public void OrdinaryStrokeScalesWithANonuniformTransform()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgStrokeGeometryOptions options = Options(2);
        options.Transform = new SvgMatrix(2, 0, 0, 3, 5, 7);

        SvgStrokeBoundsResult bounds = line.GetStrokeBounds(options);
        SvgStrokeContainmentResult inside = line.ContainsStroke(Point(15, 9.5), options);
        SvgStrokeContainmentResult outside = line.ContainsStroke(Point(15, 10.5), options);

        Assert.Multiple(() =>
        {
            AssertBounds(bounds, 5, 4, 25, 10);
            AssertContains(inside, true);
            AssertContains(outside, false);
        });
    }

    [Test]
    public void NonScalingStrokePreservesOutputSpaceWidth()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgStrokeGeometryOptions options = Options(2);
        options.Transform = new SvgMatrix(2, 0, 0, 3, 5, 7);
        options.VectorEffect = SvgStrokeVectorEffect.NonScalingStroke;

        SvgStrokeBoundsResult bounds = line.GetStrokeBounds(options);
        SvgStrokeContainmentResult inside = line.ContainsStroke(Point(15, 7.9), options);
        SvgStrokeContainmentResult outside = line.ContainsStroke(Point(15, 8.1), options);

        Assert.Multiple(() =>
        {
            AssertBounds(bounds, 5, 6, 25, 8);
            AssertContains(inside, true);
            AssertContains(outside, false);
        });
    }

    [Test]
    public void SingularTransformsCollapseOrdinaryStrokesButNotNonScalingStrokes()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgMatrix collapse = new SvgMatrix(2, 0, 0, 0, 5, 7);
        SvgStrokeGeometryOptions ordinary = Options(2);
        ordinary.Transform = collapse;
        SvgStrokeGeometryOptions nonScaling = Options(2);
        nonScaling.Transform = collapse;
        nonScaling.VectorEffect = SvgStrokeVectorEffect.NonScalingStroke;

        SvgStrokeBoundsResult ordinaryBounds = line.GetStrokeBounds(ordinary);
        SvgStrokeBoundsResult nonScalingBounds = line.GetStrokeBounds(nonScaling);
        SvgStrokeContainmentResult ordinaryLine = line.ContainsStroke(Point(15, 7), ordinary);
        SvgStrokeContainmentResult ordinaryAway = line.ContainsStroke(Point(15, 7.5), ordinary);
        SvgStrokeContainmentResult nonScalingAway = line.ContainsStroke(Point(15, 7.5), nonScaling);

        Assert.Multiple(() =>
        {
            AssertBounds(ordinaryBounds, 5, 7, 25, 7);
            AssertBounds(nonScalingBounds, 5, 6, 25, 8);
            AssertContains(ordinaryLine, true);
            AssertContains(ordinaryAway, false);
            AssertContains(nonScalingAway, true);
        });
    }

    [Test]
    public void ReflectionsAndDashLengthsFollowTheSelectedVectorEffect()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgMatrix reflectedScale = new SvgMatrix(-2, 0, 0, 3, 5, 7);
        SvgStrokeGeometryOptions ordinary = Options(2);
        ordinary.Transform = reflectedScale;
        ordinary.Style.DashArray.Add(2);
        ordinary.Style.DashArray.Add(2);
        SvgStrokeGeometryOptions nonScaling = Options(2);
        nonScaling.Transform = reflectedScale;
        nonScaling.VectorEffect = SvgStrokeVectorEffect.NonScalingStroke;
        nonScaling.Style.DashArray.Add(2);
        nonScaling.Style.DashArray.Add(2);

        SvgStrokeBoundsResult ordinaryBounds = line.GetStrokeBounds(ordinary);
        SvgStrokeContainmentResult ordinaryGap = line.ContainsStroke(Point(-1, 7), ordinary);
        SvgStrokeContainmentResult nonScalingDash = line.ContainsStroke(Point(-1, 7), nonScaling);

        Assert.Multiple(() =>
        {
            AssertBounds(ordinaryBounds, -15, 4, 5, 10);
            AssertContains(ordinaryGap, false);
            AssertContains(nonScalingDash, true);
        });
    }

    [Test]
    public void CurveToleranceProducesFiniteDashedContainmentAndBounds()
    {
        SvgPathData curve = Parse("M0 0 Q5 10 10 0");
        SvgStrokeGeometryOptions options = Options(1);
        options.Style.Tolerance = 0.01;
        options.Style.LineCap = SvgStrokeLineCap.Round;
        options.Style.LineJoin = SvgStrokeLineJoin.Round;
        options.Style.DashArray.Add(3);
        options.Style.DashArray.Add(1);

        SvgStrokeBoundsResult bounds = curve.GetStrokeBounds(options);
        SvgStrokeContainmentResult start = curve.ContainsStroke(Point(0, 0), options);

        Assert.Multiple(() =>
        {
            Assert.That(bounds.Success, Is.True);
            Assert.That(bounds.Error, Is.EqualTo(SvgStrokeError.None));
            Assert.That(bounds.Bounds.IsEmpty, Is.False);
            Assert.That(bounds.Bounds.X, Is.LessThanOrEqualTo(-0.49));
            Assert.That(bounds.Bounds.Width, Is.GreaterThan(10));
            AssertContains(start, true);
        });
    }

    [Test]
    public void InvalidInputsMutationOverflowAndLimitsReturnStableFailures()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgStrokeGeometryOptions invalidVectorEffect = Options(1);
        invalidVectorEffect.VectorEffect = (SvgStrokeVectorEffect)99;
        SvgStrokeGeometryOptions invalidMatrix = Options(1);
        invalidMatrix.Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidMatrix.Transform.A = double.NaN;
        SvgPoint invalidPoint = Point(0, 0);
        invalidPoint.Y = double.PositiveInfinity;
        SvgStrokeGeometryOptions invalidStyle = Options(1);
        invalidStyle.Style.Width = -1;
        SvgPathData invalidPath = Parse("M0 0 L1 0");
        invalidPath.Commands[1].Values[0] = double.NaN;
        SvgStrokeGeometryOptions depth = Options(1);
        depth.Style.Tolerance = 0.0001;
        depth.Style.MaximumSubdivisionDepth = 0;
        SvgStrokeGeometryOptions overflow = Options(2);
        overflow.Transform = new SvgMatrix(double.MaxValue, 0, 0, 1, 0, 0);

        Assert.Multiple(() =>
        {
            AssertContainmentFailure(new SvgStrokeGeometry().Contains(null, Point(0, 0), Options(1)), SvgStrokeError.InvalidArgument);
            AssertContainmentFailure(line.ContainsStroke(null, Options(1)), SvgStrokeError.InvalidArgument);
            AssertContainmentFailure(line.ContainsStroke(invalidPoint, Options(1)), SvgStrokeError.InvalidArgument);
            AssertBoundsFailure(line.GetStrokeBounds(null), SvgStrokeError.InvalidArgument);
            AssertBoundsFailure(line.GetStrokeBounds(invalidVectorEffect), SvgStrokeError.InvalidArgument);
            AssertBoundsFailure(line.GetStrokeBounds(invalidMatrix), SvgStrokeError.InvalidArgument);
            AssertBoundsFailure(line.GetStrokeBounds(invalidStyle), SvgStrokeError.InvalidArgument);
            AssertBoundsFailure(invalidPath.GetStrokeBounds(Options(1)), SvgStrokeError.InvalidPath);
            AssertBoundsFailure(Parse("M0 0 Q5 10 10 0").GetStrokeBounds(depth), SvgStrokeError.SubdivisionLimitExceeded);
            AssertBoundsFailure(line.GetStrokeBounds(overflow), SvgStrokeError.NonFiniteResult);
        });
    }

    static SvgStrokeGeometryOptions Options(double width)
    {
        SvgStrokeGeometryOptions options = new SvgStrokeGeometryOptions();
        options.Style.Width = width;
        options.Style.Tolerance = 0.0001;
        return options;
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

    static void AssertContains(SvgStrokeContainmentResult result, bool contains)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgStrokeError.None));
        Assert.That(result.Contains, Is.EqualTo(contains));
    }

    static void AssertBounds(SvgStrokeBoundsResult result, double minimumX, double minimumY, double maximumX, double maximumY, double tolerance = 1e-10)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgStrokeError.None));
        Assert.That(result.Bounds.IsEmpty, Is.False);
        Assert.That(result.Bounds.X, Is.EqualTo(minimumX).Within(tolerance));
        Assert.That(result.Bounds.Y, Is.EqualTo(minimumY).Within(tolerance));
        Assert.That(result.Bounds.Width, Is.EqualTo(maximumX - minimumX).Within(tolerance));
        Assert.That(result.Bounds.Height, Is.EqualTo(maximumY - minimumY).Within(tolerance));
    }

    static void AssertContainmentFailure(SvgStrokeContainmentResult result, SvgStrokeError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Contains, Is.False);
    }

    static void AssertBoundsFailure(SvgStrokeBoundsResult result, SvgStrokeError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Bounds, Is.Null);
    }
}
