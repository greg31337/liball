namespace chart.Tests;

[TestFixture]
public class ChartGeometryTests
{
    [Test]
    public void ChartPoint_SupportsVectorMathAndInterpolation()
    {
        ChartPoint first = new ChartPoint(1.0, 2.0);
        ChartPoint second = new ChartPoint(4.0, 6.0);

        ChartPoint added = ChartPoint.Add(first, second);
        ChartPoint subtracted = ChartPoint.Subtract(second, first);
        ChartPoint scaled = ChartPoint.Scale(first, 3.0);
        ChartPoint interpolated = ChartPoint.Lerp(first, second, 0.25);

        Assert.That(added.X, Is.EqualTo(5.0));
        Assert.That(added.Y, Is.EqualTo(8.0));
        Assert.That(subtracted.X, Is.EqualTo(3.0));
        Assert.That(subtracted.Y, Is.EqualTo(4.0));
        Assert.That(scaled.X, Is.EqualTo(3.0));
        Assert.That(scaled.Y, Is.EqualTo(6.0));
        Assert.That(interpolated.X, Is.EqualTo(1.75));
        Assert.That(interpolated.Y, Is.EqualTo(3.0));
        Assert.That(first.DistanceTo(second), Is.EqualTo(5.0).Within(0.0000001));
    }

    [Test]
    public void ChartRect_ComputesEdgesContainmentIntersectionAndUnion()
    {
        ChartRect rect = new ChartRect(10.0, 20.0, 30.0, 40.0);
        ChartRect overlap = new ChartRect(25.0, 10.0, 30.0, 25.0);
        ChartRect disjoint = new ChartRect(100.0, 100.0, 10.0, 10.0);

        ChartRect intersection = rect.Intersection(overlap);
        ChartRect emptyIntersection = rect.Intersection(disjoint);
        ChartRect union = rect.Union(overlap);

        Assert.That(rect.Left(), Is.EqualTo(10.0));
        Assert.That(rect.Right(), Is.EqualTo(40.0));
        Assert.That(rect.Top(), Is.EqualTo(20.0));
        Assert.That(rect.Bottom(), Is.EqualTo(60.0));
        Assert.That(rect.ContainsPoint(new ChartPoint(10.0, 20.0)), Is.True);
        Assert.That(rect.ContainsPoint(new ChartPoint(40.0, 60.0)), Is.True);
        Assert.That(rect.ContainsPoint(new ChartPoint(41.0, 60.0)), Is.False);
        Assert.That(rect.Intersects(overlap), Is.True);
        Assert.That(rect.Intersects(disjoint), Is.False);

        Assert.That(intersection.X, Is.EqualTo(25.0));
        Assert.That(intersection.Y, Is.EqualTo(20.0));
        Assert.That(intersection.Width, Is.EqualTo(15.0));
        Assert.That(intersection.Height, Is.EqualTo(15.0));
        Assert.That(emptyIntersection.IsEmpty(), Is.True);
        Assert.That(union.X, Is.EqualTo(10.0));
        Assert.That(union.Y, Is.EqualTo(10.0));
        Assert.That(union.Width, Is.EqualTo(45.0));
        Assert.That(union.Height, Is.EqualTo(50.0));
    }

    [Test]
    public void ChartPadding_DeflatesAndInflatesRectangles()
    {
        ChartRect rect = new ChartRect(10.0, 20.0, 100.0, 80.0);
        ChartPadding padding = new ChartPadding(5.0, 10.0, 15.0, 20.0);

        ChartRect inner = padding.DeflateRect(rect);
        ChartRect outer = padding.InflateRect(inner);

        Assert.That(padding.Horizontal(), Is.EqualTo(20.0));
        Assert.That(padding.Vertical(), Is.EqualTo(30.0));
        Assert.That(inner.X, Is.EqualTo(15.0));
        Assert.That(inner.Y, Is.EqualTo(30.0));
        Assert.That(inner.Width, Is.EqualTo(80.0));
        Assert.That(inner.Height, Is.EqualTo(50.0));
        Assert.That(outer.X, Is.EqualTo(10.0));
        Assert.That(outer.Y, Is.EqualTo(20.0));
        Assert.That(outer.Width, Is.EqualTo(100.0));
        Assert.That(outer.Height, Is.EqualTo(80.0));
    }

    [Test]
    public void ChartRange_NormalizesClampsPadsAndIncludesValues()
    {
        ChartRange range = new ChartRange(10.0, 20.0);
        ChartRange padded = range.Pad(2.5);
        ChartRange includedLow = range.Include(5.0);
        ChartRange includedHigh = range.Include(25.0);

        Assert.That(range.IsValid(), Is.True);
        Assert.That(range.Length(), Is.EqualTo(10.0));
        Assert.That(range.Contains(15.0), Is.True);
        Assert.That(range.Contains(25.0), Is.False);
        Assert.That(range.Clamp(5.0), Is.EqualTo(10.0));
        Assert.That(range.Clamp(25.0), Is.EqualTo(20.0));
        Assert.That(range.Normalize(15.0), Is.EqualTo(0.5));
        Assert.That(range.Lerp(0.25), Is.EqualTo(12.5));
        Assert.That(padded.Minimum, Is.EqualTo(7.5));
        Assert.That(padded.Maximum, Is.EqualTo(22.5));
        Assert.That(includedLow.Minimum, Is.EqualTo(5.0));
        Assert.That(includedLow.Maximum, Is.EqualTo(20.0));
        Assert.That(includedHigh.Minimum, Is.EqualTo(10.0));
        Assert.That(includedHigh.Maximum, Is.EqualTo(25.0));
        Assert.That(new ChartRange(double.NaN, 1.0).IsValid(), Is.False);
    }

    [Test]
    public void ChartMatrix_TransformsComposesAndInvertsAffineCoordinates()
    {
        ChartMatrix scale = ChartMatrix.Scale(2.0, 3.0);
        ChartMatrix translate = ChartMatrix.Translation(5.0, 7.0);
        ChartMatrix combined = scale.Multiply(translate);
        ChartPoint transformed = combined.TransformPoint(new ChartPoint(4.0, 6.0));
        ChartMatrix? inverse = combined.Invert();

        Assert.That(transformed.X, Is.EqualTo(13.0).Within(0.0000001));
        Assert.That(transformed.Y, Is.EqualTo(25.0).Within(0.0000001));
        Assert.That(combined.Determinant(), Is.EqualTo(6.0).Within(0.0000001));
        Assert.That(inverse, Is.Not.Null);

        ChartMatrix inverseMatrix = inverse ?? throw new System.InvalidOperationException("Expected an invertible matrix.");
        ChartPoint restored = inverseMatrix.TransformPoint(transformed);

        Assert.That(restored.X, Is.EqualTo(4.0).Within(0.0000001));
        Assert.That(restored.Y, Is.EqualTo(6.0).Within(0.0000001));
        Assert.That(new ChartMatrix(1.0, 2.0, 2.0, 4.0, 0.0, 0.0).Invert(), Is.Null);
    }

    [Test]
    public void ChartMatrix_InvertReturnsNullForSingularMatrix()
    {
        ChartMatrix singular = new ChartMatrix(1.0, 2.0, 2.0, 4.0, 3.0, 6.0);

        ChartMatrix? inverse = singular.Invert();

        Assert.That(singular.Determinant(), Is.EqualTo(0.0));
        Assert.That(inverse, Is.Null);
    }

    [Test]
    public void ChartMatrix_RotatesAroundOrigin()
    {
        ChartMatrix rotation = ChartMatrix.Rotation(System.Math.PI * 0.5);
        ChartPoint transformed = rotation.TransformPoint(new ChartPoint(2.0, 0.0));

        Assert.That(transformed.X, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(transformed.Y, Is.EqualTo(2.0).Within(0.0000001));
    }
}
