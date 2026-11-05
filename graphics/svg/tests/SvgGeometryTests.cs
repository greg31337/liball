namespace svg.Tests;

public class SvgGeometryTests
{
    [Test]
    public void ValueEqualityIsExactAndRejectsInvalidMutableState()
    {
        SvgPoint point = new SvgPoint(1, -0.0);
        SvgVector vector = new SvgVector(2, 3);
        SvgSize size = new SvgSize(4, 5);
        SvgMatrix matrix = new SvgMatrix(1, 2, 3, 4, 5, 6);
        SvgPoint invalid = new SvgPoint(1, 0);
        invalid.Y = double.NaN;

        Assert.Multiple(() =>
        {
            Assert.That(point.ValueEquals(new SvgPoint(1, 0)), Is.True);
            Assert.That(point.ValueEquals(new SvgPoint(1, double.Epsilon)), Is.False);
            Assert.That(vector.ValueEquals(new SvgVector(2, 3)), Is.True);
            Assert.That(size.ValueEquals(new SvgSize(4, 5)), Is.True);
            Assert.That(matrix.ValueEquals(new SvgMatrix(1, 2, 3, 4, 5, 6)), Is.True);
            Assert.That(new SvgGeometry().EqualWithSvgPointAndSvgPoint(point, invalid), Is.False);
            Assert.That(new SvgGeometry().EqualWithSvgPointAndSvgPoint(point, (SvgPoint)null), Is.False);
        });
    }

    [Test]
    public void RectangleEqualityUsesExplicitEmptySemantics()
    {
        SvgRect firstEmpty = SvgRect.Create(10, 20, 30, 40, true);
        SvgRect secondEmpty = SvgRect.Create(-5, -6, 0, 0, true);
        SvgRect zeroArea = SvgRect.Create(10, 20, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(firstEmpty.ValueEquals(secondEmpty), Is.True);
            Assert.That(firstEmpty.ValueEquals(zeroArea), Is.False);
            Assert.That(zeroArea.ValueEquals(SvgRect.Create(10, 20, 0, 0)), Is.True);
            Assert.That(zeroArea.ValueEquals(SvgRect.Create(10, 21, 0, 0)), Is.False);
        });
    }

    [Test]
    public void PointAndVectorArithmeticProducesDeterministicFiniteResults()
    {
        SvgPointOperationResult translated = new SvgPoint(3, 4).Add(new SvgVector(-1, 2));
        SvgVectorOperationResult difference = new SvgPoint(3, 4).Subtract(new SvgPoint(1, -2));
        SvgVectorOperationResult sum = new SvgVector(3, 4).Add(new SvgVector(-1, 2));
        SvgVectorOperationResult vectorDifference = new SvgGeometry().SubtractWithSvgVectorAndSvgVector(new SvgVector(3, 4), new SvgVector(-1, 2));
        SvgVectorOperationResult scaled = new SvgVector(3, 4).Scale(-2);
        SvgScalarOperationResult dot = new SvgVector(3, 4).Dot(new SvgVector(-1, 2));
        SvgScalarOperationResult cross = new SvgVector(3, 4).Cross(new SvgVector(-1, 2));
        SvgScalarOperationResult length = new SvgVector(3, 4).Length();

        Assert.Multiple(() =>
        {
            AssertPoint(translated, 2, 6);
            AssertVector(difference, 2, 6);
            AssertVector(sum, 2, 6);
            AssertVector(vectorDifference, 4, 2);
            AssertVector(scaled, -6, -8);
            AssertScalar(dot, 5);
            AssertScalar(cross, 10);
            AssertScalar(length, 5);
        });
    }

    [Test]
    public void ArithmeticReportsInvalidInputsAndNonFiniteResults()
    {
        SvgVector invalid = new SvgVector(1, 2);
        invalid.X = double.PositiveInfinity;
        SvgPointOperationResult invalidPoint = new SvgGeometry().AddWithPointAndVector(new SvgPoint(0, 0), invalid);
        SvgVectorOperationResult invalidScale = new SvgGeometry().Scale(new SvgVector(1, 2), double.NaN);
        SvgVectorOperationResult overflow = new SvgVector(double.MaxValue, 0).Scale(2);
        SvgScalarOperationResult dotOverflow = new SvgGeometry().Dot(new SvgVector(double.MaxValue, 1), new SvgVector(2, 1));
        SvgScalarOperationResult lengthOverflow = new SvgVector(double.MaxValue, double.MaxValue).Length();

        Assert.Multiple(() =>
        {
            AssertFailure(invalidPoint.Success, invalidPoint.Error, invalidPoint.Point);
            AssertFailure(invalidScale.Success, invalidScale.Error, invalidScale.Vector);
            Assert.That(invalidScale.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(overflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(overflow.Vector, Is.Null);
            Assert.That(dotOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(lengthOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
        });
    }

    [Test]
    public void RectangleUnionComputesBoundsAndTreatsEmptyAsIdentity()
    {
        SvgRectOperationResult overlap = SvgRect.Create(1, 2, 4, 5).Union(SvgRect.Create(-2, 4, 5, 2));
        SvgRectOperationResult disjoint = new SvgGeometry().Union(SvgRect.Create(0, 0, 1, 1), SvgRect.Create(5, 6, 2, 3));
        SvgRectOperationResult emptyIdentity = SvgRect.Create(50, 60, 70, 80, true).Union(SvgRect.Create(2, 3, 4, 5));
        SvgRectOperationResult bothEmpty = SvgRect.Create(1, 2, 3, 4, true).Union(SvgRect.Create(5, 6, 7, 8, true));

        Assert.Multiple(() =>
        {
            AssertRect(overlap, -2, 2, 7, 5, false);
            AssertRect(disjoint, 0, 0, 7, 9, false);
            AssertRect(emptyIdentity, 2, 3, 4, 5, false);
            AssertRect(bothEmpty, 0, 0, 0, 0, true);
        });
    }

    [Test]
    public void RectangleIntersectionUsesClosedBoundariesAndCanonicalEmpty()
    {
        SvgRectOperationResult overlap = SvgRect.Create(0, 0, 5, 6).Intersect(SvgRect.Create(3, 2, 5, 2));
        SvgRectOperationResult touching = new SvgGeometry().Intersect(SvgRect.Create(0, 0, 2, 2), SvgRect.Create(2, 1, 3, 1));
        SvgRectOperationResult disjoint = SvgRect.Create(0, 0, 1, 1).Intersect(SvgRect.Create(2, 3, 1, 1));

        Assert.Multiple(() =>
        {
            AssertRect(overlap, 3, 2, 2, 2, false);
            AssertRect(touching, 2, 1, 0, 1, false);
            AssertRect(disjoint, 0, 0, 0, 0, true);
        });
    }

    [Test]
    public void RectangleExpansionAndContainmentHonorBothAxesAndBoundaries()
    {
        SvgRect rectangle = SvgRect.Create(10, 20, 30, 40);
        SvgRectOperationResult uniform = rectangle.Expand(2);
        SvgRectOperationResult asymmetric = rectangle.ExpandAxes(3, 4);
        SvgRectOperationResult empty = SvgRect.Create(10, 20, 30, 40, true).Expand(5);

        Assert.Multiple(() =>
        {
            AssertRect(uniform, 8, 18, 34, 44, false);
            AssertRect(asymmetric, 7, 16, 36, 48, false);
            AssertRect(empty, 0, 0, 0, 0, true);
            Assert.That(rectangle.Contains(new SvgPoint(10, 20)), Is.True);
            Assert.That(rectangle.Contains(new SvgPoint(40, 60)), Is.True);
            Assert.That(rectangle.Contains(new SvgPoint(System.Math.BitIncrement(40), 60)), Is.False);
            Assert.That(rectangle.Contains(new SvgPoint(41, 60)), Is.False);
            Assert.That(SvgRect.Create(10, 20, 30, 40, true).Contains(new SvgPoint(10, 20)), Is.False);
        });
    }

    [Test]
    public void RectangleOperationsReportInvalidStateAndOverflow()
    {
        SvgRect invalid = SvgRect.Create(0, 0, 1, 1);
        invalid.Width = -1;
        SvgRectOperationResult invalidUnion = new SvgGeometry().Union(invalid, SvgRect.Create(0, 0, 1, 1));
        SvgRectOperationResult invalidExpansion = SvgRect.Create(0, 0, 1, 1).Expand(-1);
        SvgRectOperationResult edgeOverflow = SvgRect.Create(double.MaxValue, 0, double.MaxValue, 1).Intersect(SvgRect.Create(0, 0, 1, 1));
        SvgRectOperationResult expansionOverflow = SvgRect.Create(double.MaxValue, 0, 1, 1).Expand(double.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(invalidUnion.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidUnion.Rectangle, Is.Null);
            Assert.That(invalidExpansion.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(edgeOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(expansionOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(new SvgGeometry().Contains(invalid, new SvgPoint(0, 0)), Is.False);
        });
    }

    static void AssertPoint(SvgPointOperationResult result, double x, double y)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
        Assert.That(result.Point.X, Is.EqualTo(x));
        Assert.That(result.Point.Y, Is.EqualTo(y));
    }

    static void AssertVector(SvgVectorOperationResult result, double x, double y)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
        Assert.That(result.Vector.X, Is.EqualTo(x));
        Assert.That(result.Vector.Y, Is.EqualTo(y));
    }

    static void AssertScalar(SvgScalarOperationResult result, double value)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
        Assert.That(result.Value, Is.EqualTo(value));
    }

    static void AssertRect(SvgRectOperationResult result, double x, double y, double width, double height, bool isEmpty)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
        Assert.That(result.Rectangle.X, Is.EqualTo(x));
        Assert.That(result.Rectangle.Y, Is.EqualTo(y));
        Assert.That(result.Rectangle.Width, Is.EqualTo(width));
        Assert.That(result.Rectangle.Height, Is.EqualTo(height));
        Assert.That(result.Rectangle.IsEmpty, Is.EqualTo(isEmpty));
    }

    static void AssertFailure(bool success, SvgGeometryError error, object value)
    {
        Assert.That(success, Is.False);
        Assert.That(error, Is.EqualTo(SvgGeometryError.InvalidArgument));
        Assert.That(value, Is.Null);
    }
}
