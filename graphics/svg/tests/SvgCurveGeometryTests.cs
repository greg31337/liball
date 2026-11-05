namespace svg.Tests;

public class SvgCurveGeometryTests
{
    [Test]
    public void LineQuadraticAndCubicEvaluationAndDerivativesAreAnalytic()
    {
        SvgLineSegment line = new SvgLineSegment(Point(0, 0), Point(10, 20));
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(0, 0), Point(10, 20), Point(20, 0));
        SvgCubicBezier cubic = new SvgCubicBezier(Point(0, 0), Point(0, 12), Point(12, 12), Point(12, 0));

        Assert.Multiple(() =>
        {
            AssertPoint(line.Evaluate(0.25), 2.5, 5);
            AssertVector(line.Derivative(0.75), 10, 20);
            AssertPoint(quadratic.Evaluate(0.5), 10, 10);
            AssertVector(quadratic.Derivative(0), 20, 40);
            AssertVector(quadratic.Derivative(0.5), 20, 0);
            AssertVector(quadratic.Derivative(1), 20, -40);
            AssertPoint(cubic.Evaluate(0.5), 6, 9);
            AssertVector(cubic.Derivative(0), 0, 36);
            AssertVector(cubic.Derivative(0.5), 18, 0);
            AssertVector(cubic.Derivative(1), 0, -36);
        });
    }

    [Test]
    public void ArcDerivativeUsesUnitParameterAndHandlesDegeneracies()
    {
        SvgArc quarter = Arc(Point(1, 0), Point(0, 1), 1, 1, false, true);
        SvgArc line = Arc(Point(0, 0), Point(6, 3), 0, 2, false, true);
        SvgArc point = Arc(Point(2, 3), Point(2, 3), 4, 5, false, true);

        Assert.Multiple(() =>
        {
            AssertVector(quarter.Derivative(0), 0, System.Math.PI / 2);
            AssertVector(quarter.Derivative(1), -System.Math.PI / 2, 0);
            AssertVector(line.Derivative(0.4), 6, 3);
            AssertVector(point.Derivative(0.4), 0, 0);
        });
    }

    [Test]
    public void PolynomialRootsAreScaledOrderedAndDistinct()
    {
        SvgPolynomialRootResult linear = new SvgPolynomialRoots().SolveLinear(2, -6);
        SvgPolynomialRootResult quadratic = new SvgPolynomialRoots().SolveQuadratic(1e300, -5e300, 6e300);
        SvgPolynomialRootResult repeated = new SvgPolynomialRoots().SolveQuadratic(1, -2, 1);
        SvgPolynomialRootResult none = new SvgPolynomialRoots().SolveQuadratic(1, 0, 1);
        SvgPolynomialRootResult indeterminate = new SvgPolynomialRoots().SolveLinear(0, 0);
        SvgPolynomialRootResult invalid = new SvgPolynomialRoots().SolveQuadratic(double.NaN, 1, 1);

        Assert.Multiple(() =>
        {
            AssertRoots(linear, 3);
            AssertRoots(quadratic, 2, 3);
            AssertRoots(repeated, 1);
            AssertRoots(none);
            AssertRoots(indeterminate);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalid.Roots, Is.Null);
        });
    }

    [Test]
    public void DeCasteljauSplittingProducesExactSharedControlNets()
    {
        SvgLineSplitResult line = new SvgLineSegment(Point(0, 0), Point(8, 4)).Split(0.5);
        SvgQuadraticSplitResult quadratic = new SvgQuadraticBezier(Point(0, 0), Point(10, 20), Point(20, 0)).Split(0.5);
        SvgCubicSplitResult cubic = new SvgCubicBezier(Point(0, 0), Point(0, 12), Point(12, 12), Point(12, 0)).Split(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(line.Success, Is.True);
            AssertPoint(line.First.End, 4, 2);
            Assert.That(line.First.End.ValueEquals(line.Second.Start), Is.True);
            Assert.That(quadratic.Success, Is.True);
            AssertPoint(quadratic.First.Control, 5, 10);
            AssertPoint(quadratic.First.End, 10, 10);
            AssertPoint(quadratic.Second.Control, 15, 10);
            Assert.That(quadratic.First.End.ValueEquals(quadratic.Second.Start), Is.True);
            Assert.That(cubic.Success, Is.True);
            AssertPoint(cubic.First.Control1, 0, 6);
            AssertPoint(cubic.First.Control2, 3, 9);
            AssertPoint(cubic.First.End, 6, 9);
            AssertPoint(cubic.Second.Control1, 9, 9);
            AssertPoint(cubic.Second.Control2, 12, 6);
            Assert.That(cubic.First.End.ValueEquals(cubic.Second.Start), Is.True);
        });
    }

    [Test]
    public void ReversalPreservesLocusAndInvertsParameterDirection()
    {
        SvgLineSegment line = new SvgLineSegment(Point(0, 1), Point(8, 5));
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(0, 0), Point(10, 20), Point(20, 0));
        SvgCubicBezier cubic = new SvgCubicBezier(Point(0, 0), Point(0, 12), Point(12, 12), Point(12, 0));
        SvgArc arc = Arc(Point(1, 0), Point(0, 1), 1, 1, true, true);

        SvgLineOperationResult reversedLine = line.Reverse();
        SvgQuadraticOperationResult reversedQuadratic = quadratic.Reverse();
        SvgCubicOperationResult reversedCubic = cubic.Reverse();
        SvgArcOperationResult reversedArc = arc.Reverse();

        Assert.Multiple(() =>
        {
            Assert.That(reversedLine.Success, Is.True);
            AssertEquivalent(line.Evaluate(0.25), reversedLine.Line.Evaluate(0.75));
            Assert.That(reversedQuadratic.Success, Is.True);
            AssertEquivalent(quadratic.Evaluate(0.25), reversedQuadratic.Quadratic.Evaluate(0.75));
            Assert.That(reversedCubic.Success, Is.True);
            AssertEquivalent(cubic.Evaluate(0.25), reversedCubic.Cubic.Evaluate(0.75));
            Assert.That(reversedArc.Success, Is.True);
            AssertEquivalent(arc.Evaluate(0.25), reversedArc.Arc.Evaluate(0.75));
            Assert.That(reversedArc.Arc.SweepAngle, Is.EqualTo(-arc.SweepAngle));
            Assert.That(reversedArc.Arc.Start.ValueEquals(arc.End), Is.True);
            Assert.That(reversedArc.Arc.End.ValueEquals(arc.Start), Is.True);
        });
    }

    [Test]
    public void LineQuadraticAndCubicBoundsIncludeOnlyReachedExtrema()
    {
        SvgRectOperationResult line = new SvgLineSegment(Point(10, -2), Point(-5, 7)).GetBounds();
        SvgRectOperationResult quadratic = new SvgQuadraticBezier(Point(0, 0), Point(10, 20), Point(20, 0)).GetBounds();
        SvgRectOperationResult cubic = new SvgCubicBezier(Point(0, 0), Point(0, 12), Point(12, 12), Point(12, 0)).GetBounds();

        Assert.Multiple(() =>
        {
            AssertRect(line, -5, -2, 15, 9);
            AssertRect(quadratic, 0, 0, 20, 10);
            AssertRect(cubic, 0, 0, 12, 9);
        });
    }

    [Test]
    public void ArcBoundsSelectOnlyExtremaInsideTheSignedSweep()
    {
        SvgArc quarter = Arc(Point(1, 0), Point(0, 1), 1, 1, false, true);
        SvgArc large = Arc(Point(1, 0), Point(0, 1), 1, 1, true, true);
        SvgArc reverse = Arc(Point(1, 0), Point(0, 1), 1, 1, false, false);

        Assert.Multiple(() =>
        {
            AssertRect(quarter.GetBounds(), 0, 0, 1, 1);
            AssertRect(large.GetBounds(), 0, 0, 2, 2);
            AssertRect(reverse.GetBounds(), 0, 0, 1, 1);
        });
    }

    [Test]
    public void DegenerateSegmentsHaveNonemptyZeroAreaBounds()
    {
        SvgLineSegment line = new SvgLineSegment(Point(2, 3), Point(2, 3));
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(2, 3), Point(2, 3), Point(2, 3));
        SvgCubicBezier cubic = new SvgCubicBezier(Point(2, 3), Point(2, 3), Point(2, 3), Point(2, 3));
        SvgArc arc = Arc(Point(2, 3), Point(2, 3), 4, 5, false, true);

        Assert.Multiple(() =>
        {
            AssertRect(line.GetBounds(), 2, 3, 0, 0);
            AssertRect(quadratic.GetBounds(), 2, 3, 0, 0);
            AssertRect(cubic.GetBounds(), 2, 3, 0, 0);
            AssertRect(arc.GetBounds(), 2, 3, 0, 0);
            Assert.That(line.GetBounds().Rectangle.IsEmpty, Is.False);
        });
    }

    [Test]
    public void CurveOperationsRejectMutationParametersAndOverflowStably()
    {
        SvgLineSegment line = new SvgLineSegment(Point(0, 0), Point(1, 1));
        SvgPointOperationResult badAmount = line.Evaluate(-0.1);
        line.End.X = double.NaN;
        SvgVectorOperationResult invalidDerivative = line.Derivative(0.5);
        SvgLineSplitResult invalidSplit = line.Split(0.5);
        SvgRectOperationResult invalidBounds = line.GetBounds();
        SvgLineSegment enormous = new SvgLineSegment(Point(-double.MaxValue, 0), Point(double.MaxValue, 0));
        SvgVectorOperationResult derivativeOverflow = enormous.Derivative(0.5);
        SvgRectOperationResult boundsOverflow = enormous.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(badAmount.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidDerivative.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidSplit.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidBounds.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(derivativeOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(boundsOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(boundsOverflow.Rectangle, Is.Null);
        });
    }

    static SvgPoint Point(double x, double y)
    {
        return new SvgPoint(x, y);
    }

    static SvgArc Arc(SvgPoint start, SvgPoint end, double radiusX, double radiusY, bool largeArc, bool sweep)
    {
        SvgArcOperationResult result = new SvgArcGeometry().FromEndpoint(start, end, radiusX, radiusY, 0, largeArc, sweep);
        Assert.That(result.Success, Is.True);
        return result.Arc;
    }

    static void AssertRoots(SvgPolynomialRootResult result, params double[] roots)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
        Assert.That(result.Roots, Has.Count.EqualTo(roots.Length));
        int index = 0;
        while (index < roots.Length)
        {
            Assert.That(result.Roots[index], Is.EqualTo(roots[index]).Within(1e-12));
            index++;
        }
    }

    static void AssertEquivalent(SvgPointOperationResult expected, SvgPointOperationResult actual)
    {
        Assert.That(expected.Success, Is.True);
        Assert.That(actual.Success, Is.True);
        AssertPoint(actual.Point, expected.Point.X, expected.Point.Y);
    }

    static void AssertPoint(SvgPointOperationResult result, double x, double y)
    {
        Assert.That(result.Success, Is.True);
        AssertPoint(result.Point, x, y);
    }

    static void AssertPoint(SvgPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(1e-11));
        Assert.That(point.Y, Is.EqualTo(y).Within(1e-11));
    }

    static void AssertVector(SvgVectorOperationResult result, double x, double y)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Vector.X, Is.EqualTo(x).Within(1e-11));
        Assert.That(result.Vector.Y, Is.EqualTo(y).Within(1e-11));
    }

    static void AssertRect(SvgRectOperationResult result, double x, double y, double width, double height)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Rectangle.X, Is.EqualTo(x).Within(1e-11));
        Assert.That(result.Rectangle.Y, Is.EqualTo(y).Within(1e-11));
        Assert.That(result.Rectangle.Width, Is.EqualTo(width).Within(1e-11));
        Assert.That(result.Rectangle.Height, Is.EqualTo(height).Within(1e-11));
    }
}
