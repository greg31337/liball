namespace svg.Tests;

public class SvgArcGeometryTests
{
    [Test]
    public void EndpointFlagsSelectEachSmallAndLargeSweep()
    {
        SvgPoint start = new SvgPoint(1, 0);
        SvgPoint end = new SvgPoint(0, 1);

        SvgArc smallSweep = Arc(start, end, 1, 1, 0, false, true);
        SvgArc largeSweep = Arc(start, end, 1, 1, 0, true, true);
        SvgArc smallReverse = Arc(start, end, 1, 1, 0, false, false);
        SvgArc largeReverse = Arc(start, end, 1, 1, 0, true, false);

        Assert.Multiple(() =>
        {
            AssertArc(smallSweep, 0, 0, 1, 1, System.Math.PI / 2);
            AssertArc(largeSweep, 1, 1, 1, 1, System.Math.PI * 3 / 2);
            AssertArc(smallReverse, 1, 1, 1, 1, -System.Math.PI / 2);
            AssertArc(largeReverse, 0, 0, 1, 1, -System.Math.PI * 3 / 2);
        });
    }

    [Test]
    public void EndpointConversionCorrectsSmallRadiiAndAppliesRotation()
    {
        SvgArc corrected = Arc(new SvgPoint(-10, 0), new SvgPoint(10, 0), 5, 5, 0, false, true);
        SvgArc rotated = Arc(new SvgPoint(10, 24), new SvgPoint(8, 20), 4, 2, 90, false, true);

        Assert.Multiple(() =>
        {
            AssertArc(corrected, 0, 0, 10, 10, System.Math.PI);
            AssertArc(rotated, 10, 20, 4, 2, System.Math.PI / 2);
            Assert.That(rotated.Rotation, Is.EqualTo(System.Math.PI / 2).Within(1e-12));
        });
    }

    [Test]
    public void ArcEvaluationReturnsExactEndpointsAndParametricInterior()
    {
        SvgArc arc = Arc(new SvgPoint(1, 0), new SvgPoint(0, 1), 1, 1, 0, false, true);

        SvgPointOperationResult start = arc.Evaluate(0);
        SvgPointOperationResult middle = new SvgArcGeometry().Evaluate(arc, 0.5);
        SvgPointOperationResult end = arc.Evaluate(1);

        Assert.Multiple(() =>
        {
            AssertPoint(start, 1, 0);
            AssertPoint(middle, System.Math.Sqrt(0.5), System.Math.Sqrt(0.5));
            AssertPoint(end, 0, 1);
        });
    }

    [Test]
    public void ArcSplittingPreservesSharedPointCenterAndTotalSweep()
    {
        SvgArc arc = Arc(new SvgPoint(1, 0), new SvgPoint(0, 1), 1, 1, 0, false, true);

        SvgArcSplitResult split = arc.Split(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(split.Success, Is.True);
            Assert.That(split.Error, Is.EqualTo(SvgGeometryError.None));
            Assert.That(split.First.End.ValueEquals(split.Second.Start), Is.True);
            Assert.That(split.First.End.X, Is.EqualTo(System.Math.Sqrt(0.5)).Within(1e-12));
            Assert.That(split.First.End.Y, Is.EqualTo(System.Math.Sqrt(0.5)).Within(1e-12));
            Assert.That(split.First.CenterX, Is.EqualTo(arc.CenterX));
            Assert.That(split.Second.CenterY, Is.EqualTo(arc.CenterY));
            Assert.That(split.First.SweepAngle, Is.EqualTo(System.Math.PI / 4).Within(1e-12));
            Assert.That(split.Second.SweepAngle, Is.EqualTo(System.Math.PI / 4).Within(1e-12));
            Assert.That(split.First.SweepAngle + split.Second.SweepAngle, Is.EqualTo(arc.SweepAngle).Within(1e-12));
        });
    }

    [Test]
    public void ArcToCubicUsesQuarterSegmentsAndExactBoundaryEndpoints()
    {
        SvgArc quarter = Arc(new SvgPoint(1, 0), new SvgPoint(0, 1), 1, 1, 0, false, true);
        SvgArc large = Arc(new SvgPoint(1, 0), new SvgPoint(0, 1), 1, 1, 0, true, true);

        SvgArcCubicResult quarterCubics = quarter.ToCubics();
        SvgArcCubicResult largeCubics = new SvgArcGeometry().ToCubics(large);
        double control = 4.0 / 3.0 * System.Math.Tan(System.Math.PI / 8);

        Assert.Multiple(() =>
        {
            Assert.That(quarterCubics.Success, Is.True);
            Assert.That(quarterCubics.Curves, Has.Count.EqualTo(1));
            AssertPoint(quarterCubics.Curves[0].Start, 1, 0);
            AssertPoint(quarterCubics.Curves[0].Control1, 1, control);
            AssertPoint(quarterCubics.Curves[0].Control2, control, 1);
            AssertPoint(quarterCubics.Curves[0].End, 0, 1);
            Assert.That(largeCubics.Success, Is.True);
            Assert.That(largeCubics.Curves, Has.Count.EqualTo(3));
            Assert.That(largeCubics.Curves[0].Start.ValueEquals(large.Start), Is.True);
            Assert.That(largeCubics.Curves[2].End.ValueEquals(large.End), Is.True);
            Assert.That(largeCubics.Curves[0].End.ValueEquals(largeCubics.Curves[1].Start), Is.True);
        });
    }

    [Test]
    public void CoincidentAndZeroRadiusArcsHaveExplicitDegenerateBehavior()
    {
        SvgArc point = Arc(new SvgPoint(2, 3), new SvgPoint(2, 3), 4, 5, 30, true, true);
        SvgArc line = Arc(new SvgPoint(0, 0), new SvgPoint(6, 3), 0, 5, 0, false, true);

        SvgPointOperationResult pointMiddle = point.Evaluate(0.5);
        SvgPointOperationResult lineMiddle = line.Evaluate(0.5);
        SvgArcCubicResult pointCubics = point.ToCubics();
        SvgArcCubicResult lineCubics = line.ToCubics();

        Assert.Multiple(() =>
        {
            Assert.That(point.IsPoint, Is.True);
            Assert.That(point.IsLine, Is.False);
            AssertPoint(pointMiddle, 2, 3);
            Assert.That(pointCubics.Success, Is.True);
            Assert.That(pointCubics.Curves, Is.Empty);
            Assert.That(line.IsLine, Is.True);
            Assert.That(line.IsPoint, Is.False);
            AssertPoint(lineMiddle, 3, 1.5);
            Assert.That(lineCubics.Curves, Has.Count.EqualTo(1));
            AssertPoint(lineCubics.Curves[0].Control1, 2, 1);
            AssertPoint(lineCubics.Curves[0].Control2, 4, 2);
        });
    }

    [Test]
    public void NormalizedArcCommandsConvertUsingTrackedEndpoints()
    {
        SvgPathData source = new SvgPathParser().Parse("M10 20 a5 6 45 1 0 7 8", 4, 32).PathData;
        SvgPathNormalizeResult normalized = source.Normalize();

        SvgArcOperationResult converted = new SvgArcGeometry().FromCommand(normalized.PathData.Commands[1]);

        Assert.Multiple(() =>
        {
            Assert.That(converted.Success, Is.True);
            Assert.That(converted.Arc.Start.X, Is.EqualTo(10));
            Assert.That(converted.Arc.Start.Y, Is.EqualTo(20));
            Assert.That(converted.Arc.End.X, Is.EqualTo(17));
            Assert.That(converted.Arc.End.Y, Is.EqualTo(28));
            Assert.That(converted.Arc.SweepAngle, Is.LessThan(0));
            Assert.That(System.Math.Abs(converted.Arc.SweepAngle), Is.GreaterThan(System.Math.PI));
        });
    }

    [Test]
    public void ArcOperationsRejectInvalidInputsMutationAndNonFiniteCorrection()
    {
        SvgArcOperationResult negativeRadius = new SvgArcGeometry().FromEndpoint(new SvgPoint(0, 0), new SvgPoint(1, 1), -1, 2, 0, false, true);
        SvgArcOperationResult correctionOverflow = new SvgArcGeometry().FromEndpoint(new SvgPoint(0, -1), new SvgPoint(0, 1), double.MaxValue, double.Epsilon, 0, false, true);
        SvgArc arc = Arc(new SvgPoint(1, 0), new SvgPoint(0, 1), 1, 1, 0, false, true);
        SvgPointOperationResult badAmount = arc.Evaluate(2);
        arc.CenterX = double.NaN;
        SvgPointOperationResult invalidArc = arc.Evaluate(0.5);
        SvgArcSplitResult invalidSplit = arc.Split(0.5);
        SvgArcCubicResult invalidCubics = arc.ToCubics();

        Assert.Multiple(() =>
        {
            AssertArcFailure(negativeRadius, SvgGeometryError.InvalidArgument);
            AssertArcFailure(correctionOverflow, SvgGeometryError.NonFiniteResult);
            Assert.That(badAmount.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidArc.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidSplit.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidSplit.First, Is.Null);
            Assert.That(invalidCubics.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidCubics.Curves, Is.Null);
            AssertArcFailure(new SvgArcGeometry().FromCommand(null), SvgGeometryError.InvalidArgument);
        });
    }

    static SvgArc Arc(SvgPoint start, SvgPoint end, double radiusX, double radiusY, double rotation, bool largeArc, bool sweep)
    {
        SvgArcOperationResult result = new SvgArcGeometry().FromEndpoint(start, end, radiusX, radiusY, rotation, largeArc, sweep);
        Assert.That(result.Success, Is.True);
        return result.Arc;
    }

    static void AssertArc(SvgArc arc, double centerX, double centerY, double radiusX, double radiusY, double sweep)
    {
        Assert.That(arc.CenterX, Is.EqualTo(centerX).Within(1e-12));
        Assert.That(arc.CenterY, Is.EqualTo(centerY).Within(1e-12));
        Assert.That(arc.RadiusX, Is.EqualTo(radiusX).Within(1e-12));
        Assert.That(arc.RadiusY, Is.EqualTo(radiusY).Within(1e-12));
        Assert.That(arc.SweepAngle, Is.EqualTo(sweep).Within(1e-12));
        Assert.That(arc.IsLine, Is.False);
        Assert.That(arc.IsPoint, Is.False);
    }

    static void AssertArcFailure(SvgArcOperationResult result, SvgGeometryError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Arc, Is.Null);
    }

    static void AssertPoint(SvgPointOperationResult result, double x, double y)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
        AssertPoint(result.Point, x, y);
    }

    static void AssertPoint(SvgPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(point.Y, Is.EqualTo(y).Within(1e-12));
    }
}
