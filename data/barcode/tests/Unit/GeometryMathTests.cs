namespace barcode.Tests;

public class GeometryMathTests
{
    [Test]
    public void PointOperationsCreateCopyMeasureAndInterpolate()
    {
        BarcodePoint first = GeometryMath.CreatePoint(1.0, 2.0);
        BarcodePoint second = GeometryMath.CreatePoint(4.0, 6.0);
        BarcodePoint copy = GeometryMath.CopyPointValue(first);
        BarcodePoint midpoint = GeometryMath.InterpolatePoint(first, second, 0.5);
        first.X = 9.0;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(first));
            Assert.That(copy.X, Is.EqualTo(1.0));
            Assert.That(copy.Y, Is.EqualTo(2.0));
            Assert.That(GeometryMath.DistanceBetweenPoints(copy, second), Is.EqualTo(5.0));
            Assert.That(midpoint.X, Is.EqualTo(2.5));
            Assert.That(midpoint.Y, Is.EqualTo(4.0));
        });
    }

    [Test]
    public void PointOperationsRejectMissingAndNonFiniteCoordinates()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => GeometryMath.CreatePoint(double.NaN, 0.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.CreatePoint(0.0, double.PositiveInfinity), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.CopyPointValue(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => GeometryMath.InterpolatePoint(new BarcodePoint(), new BarcodePoint(), double.NegativeInfinity),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void RectangleOperationsIncludeEdgesAndReturnExactIntersection()
    {
        BarcodeRectangle first = GeometryMath.CreateRectangle(1.0, 2.0, 5.0, 4.0);
        BarcodeRectangle second = GeometryMath.CreateRectangle(4.0, 0.0, 3.0, 5.0);
        BarcodeRectangle? intersection = GeometryMath.IntersectRectangles(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMath.IsPointInsideRectangle(GeometryMath.CreatePoint(1.0, 2.0), first), Is.True);
            Assert.That(GeometryMath.IsPointInsideRectangle(GeometryMath.CreatePoint(6.0, 6.0), first), Is.True);
            Assert.That(GeometryMath.IsPointInsideRectangle(GeometryMath.CreatePoint(6.1, 6.0), first), Is.False);
            Assert.That(intersection, Is.Not.Null);
            Assert.That(intersection!.X, Is.EqualTo(4.0));
            Assert.That(intersection.Y, Is.EqualTo(2.0));
            Assert.That(intersection.Width, Is.EqualTo(2.0));
            Assert.That(intersection.Height, Is.EqualTo(3.0));
            Assert.That(
                GeometryMath.IntersectRectangles(first, GeometryMath.CreateRectangle(8.0, 8.0, 1.0, 1.0)),
                Is.Null);
        });
    }

    [Test]
    public void RectangleOperationsRejectInvalidDimensionsAndCoordinates()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => GeometryMath.CreateRectangle(0.0, 0.0, -1.0, 1.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.CreateRectangle(0.0, 0.0, 1.0, -1.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.CreateRectangle(double.NaN, 0.0, 1.0, 1.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.CopyRectangleValue(null!), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void RectangleCopyIsIndependentAndPreservesZeroDimensions()
    {
        BarcodeRectangle source = GeometryMath.CreateRectangle(2.0, 3.0, 0.0, 0.0);

        BarcodeRectangle copy = GeometryMath.CopyRectangleValue(source);
        source.X = 9.0;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.X, Is.EqualTo(2.0));
            Assert.That(copy.Y, Is.EqualTo(3.0));
            Assert.That(copy.Width, Is.Zero);
            Assert.That(copy.Height, Is.Zero);
        });
    }

    [Test]
    public void QuadrilateralOperationsCopyCornersCalculateBoundsAreaAndContainment()
    {
        BarcodePoint topLeft = GeometryMath.CreatePoint(1.0, 2.0);
        BarcodeQuadrilateral quadrilateral = GeometryMath.CreateQuadrilateral(
            topLeft,
            GeometryMath.CreatePoint(5.0, 2.0),
            GeometryMath.CreatePoint(6.0, 5.0),
            GeometryMath.CreatePoint(0.0, 5.0));
        topLeft.X = 99.0;
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(quadrilateral);

        Assert.Multiple(() =>
        {
            Assert.That(quadrilateral.TopLeft!.X, Is.EqualTo(1.0));
            Assert.That(bounds.X, Is.EqualTo(0.0));
            Assert.That(bounds.Y, Is.EqualTo(2.0));
            Assert.That(bounds.Width, Is.EqualTo(6.0));
            Assert.That(bounds.Height, Is.EqualTo(3.0));
            Assert.That(GeometryMath.CalculateQuadrilateralSignedArea(quadrilateral), Is.EqualTo(15.0));
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(3.0, 3.0), quadrilateral), Is.True);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(1.0, 2.0), quadrilateral), Is.True);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(7.0, 3.0), quadrilateral), Is.False);
        });
    }

    [Test]
    public void QuadrilateralOperationsRejectMissingCorners()
    {
        BarcodeQuadrilateral incomplete = new BarcodeQuadrilateral();
        incomplete.TopLeft = new BarcodePoint();

        Assert.Multiple(() =>
        {
            Assert.That(() => GeometryMath.CalculateQuadrilateralBounds(incomplete), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.CalculateQuadrilateralSignedArea(null!), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void QuadrilateralContainmentHandlesBothWindingsAndEveryOutsideEdge()
    {
        BarcodeQuadrilateral clockwise = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(4.0, 0.0),
            GeometryMath.CreatePoint(4.0, 4.0),
            GeometryMath.CreatePoint(0.0, 4.0));
        BarcodeQuadrilateral counterClockwise = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(0.0, 4.0),
            GeometryMath.CreatePoint(4.0, 4.0),
            GeometryMath.CreatePoint(4.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(2.0, 2.0), clockwise), Is.True);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(2.0, 2.0), counterClockwise), Is.True);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(2.0, -1.0), clockwise), Is.False);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(5.0, 2.0), clockwise), Is.False);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(2.0, 5.0), clockwise), Is.False);
            Assert.That(GeometryMath.IsPointInsideQuadrilateral(GeometryMath.CreatePoint(-1.0, 2.0), clockwise), Is.False);
        });
    }

    [Test]
    public void LineOperationsMeasureInterpolateAndIntersectSegments()
    {
        LineSegment diagonal = new LineSegment(GeometryMath.CreatePoint(0.0, 0.0), GeometryMath.CreatePoint(3.0, 4.0));
        LineSegment descending = new LineSegment(GeometryMath.CreatePoint(0.0, 4.0), GeometryMath.CreatePoint(4.0, 0.0));
        LineSegment ascending = new LineSegment(GeometryMath.CreatePoint(0.0, 0.0), GeometryMath.CreatePoint(4.0, 4.0));
        BarcodePoint? intersection = GeometryMath.IntersectLineSegments(descending, ascending);
        BarcodePoint point = diagonal.CalculatePointAt(0.4);

        Assert.Multiple(() =>
        {
            Assert.That(diagonal.CalculateLength(), Is.EqualTo(5.0));
            Assert.That(point.X, Is.EqualTo(1.2).Within(0.000000001));
            Assert.That(point.Y, Is.EqualTo(1.6).Within(0.000000001));
            Assert.That(intersection, Is.Not.Null);
            Assert.That(intersection!.X, Is.EqualTo(2.0));
            Assert.That(intersection.Y, Is.EqualTo(2.0));
            Assert.That(
                GeometryMath.IntersectLineSegments(
                    ascending,
                    new LineSegment(GeometryMath.CreatePoint(0.0, 1.0), GeometryMath.CreatePoint(4.0, 5.0))),
                Is.Null);
        });
    }

    [Test]
    public void SegmentIntersectionRejectsMissingAndEveryOutOfRangeIntersection()
    {
        LineSegment horizontal = new LineSegment(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(1.0, 0.0));
        LineSegment beforeFirst = new LineSegment(
            GeometryMath.CreatePoint(-1.0, -1.0),
            GeometryMath.CreatePoint(-1.0, 1.0));
        LineSegment afterFirst = new LineSegment(
            GeometryMath.CreatePoint(2.0, -1.0),
            GeometryMath.CreatePoint(2.0, 1.0));
        LineSegment beforeSecond = new LineSegment(
            GeometryMath.CreatePoint(0.5, 1.0),
            GeometryMath.CreatePoint(0.5, 2.0));
        LineSegment afterSecond = new LineSegment(
            GeometryMath.CreatePoint(0.5, -2.0),
            GeometryMath.CreatePoint(0.5, -1.0));

        Assert.Multiple(() =>
        {
            Assert.That(() => GeometryMath.IntersectLineSegments(null!, horizontal), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GeometryMath.IntersectLineSegments(horizontal, null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(GeometryMath.IntersectLineSegments(horizontal, beforeFirst), Is.Null);
            Assert.That(GeometryMath.IntersectLineSegments(horizontal, afterFirst), Is.Null);
            Assert.That(GeometryMath.IntersectLineSegments(horizontal, beforeSecond), Is.Null);
            Assert.That(GeometryMath.IntersectLineSegments(horizontal, afterSecond), Is.Null);
        });
    }

    [Test]
    public void LineClippingHandlesCrossingContainedAndDisjointSegments()
    {
        BarcodeRectangle bounds = GeometryMath.CreateRectangle(0.0, 0.0, 10.0, 10.0);
        LineSegment crossing = new LineSegment(GeometryMath.CreatePoint(-5.0, 5.0), GeometryMath.CreatePoint(15.0, 5.0));
        LineSegment contained = new LineSegment(GeometryMath.CreatePoint(2.0, 3.0), GeometryMath.CreatePoint(8.0, 9.0));
        LineSegment disjoint = new LineSegment(GeometryMath.CreatePoint(-5.0, 12.0), GeometryMath.CreatePoint(15.0, 12.0));

        LineSegment? clippedCrossing = GeometryMath.ClipLineToRectangle(crossing, bounds);
        LineSegment? clippedContained = GeometryMath.ClipLineToRectangle(contained, bounds);

        Assert.Multiple(() =>
        {
            Assert.That(clippedCrossing, Is.Not.Null);
            Assert.That(clippedCrossing!.Start.X, Is.EqualTo(0.0));
            Assert.That(clippedCrossing.End.X, Is.EqualTo(10.0));
            Assert.That(clippedCrossing.Start.Y, Is.EqualTo(5.0));
            Assert.That(clippedContained!.Start.X, Is.EqualTo(2.0));
            Assert.That(clippedContained.End.Y, Is.EqualTo(9.0));
            Assert.That(GeometryMath.ClipLineToRectangle(disjoint, bounds), Is.Null);
        });
    }

    [Test]
    public void LineClippingRejectsMissingAndNonParallelDisjointSegments()
    {
        BarcodeRectangle bounds = GeometryMath.CreateRectangle(0.0, 0.0, 10.0, 10.0);
        LineSegment left = new LineSegment(
            GeometryMath.CreatePoint(-20.0, 5.0),
            GeometryMath.CreatePoint(-10.0, 5.0));
        LineSegment right = new LineSegment(
            GeometryMath.CreatePoint(20.0, 5.0),
            GeometryMath.CreatePoint(30.0, 5.0));

        Assert.Multiple(() =>
        {
            Assert.That(() => GeometryMath.ClipLineToRectangle(null!, bounds), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(GeometryMath.ClipLineToRectangle(left, bounds), Is.Null);
            Assert.That(GeometryMath.ClipLineToRectangle(right, bounds), Is.Null);
        });
    }
}
