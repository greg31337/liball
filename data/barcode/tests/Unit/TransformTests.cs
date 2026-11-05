namespace barcode.Tests;

public class TransformTests
{
    [Test]
    public void AffineFactoriesTransformPointsAsExpected()
    {
        BarcodePoint point = GeometryMath.CreatePoint(2.0, 3.0);
        BarcodePoint identity = AffineTransform.CreateIdentity().TransformPoint(point);
        BarcodePoint translated = AffineTransform.CreateTranslation(5.0, -1.0).TransformPoint(point);
        BarcodePoint scaled = AffineTransform.CreateScaling(2.0, 4.0).TransformPoint(point);
        BarcodePoint rotated = AffineTransform.CreateRotationDegrees(90.0).TransformPoint(GeometryMath.CreatePoint(2.0, 0.0));

        Assert.Multiple(() =>
        {
            AssertPoint(identity, 2.0, 3.0);
            AssertPoint(translated, 7.0, 2.0);
            AssertPoint(scaled, 4.0, 12.0);
            AssertPoint(rotated, 0.0, 2.0);
        });
    }

    [Test]
    public void AffineCompositionAndInverseRoundTrip()
    {
        AffineTransform scaling = AffineTransform.CreateScaling(2.0, 3.0);
        AffineTransform translation = AffineTransform.CreateTranslation(5.0, -1.0);
        AffineTransform combined = scaling.ComposeAfter(translation);
        BarcodePoint source = GeometryMath.CreatePoint(2.0, 3.0);
        BarcodePoint transformed = combined.TransformPoint(source);
        BarcodePoint recovered = combined.InvertTransform().TransformPoint(transformed);

        Assert.Multiple(() =>
        {
            AssertPoint(transformed, 9.0, 8.0);
            AssertPoint(recovered, 2.0, 3.0);
        });
    }

    [Test]
    public void AffinePointPairsMapCompleteSourceTriangle()
    {
        BarcodePoint sourceFirst = GeometryMath.CreatePoint(0.0, 0.0);
        BarcodePoint sourceSecond = GeometryMath.CreatePoint(2.0, 0.0);
        BarcodePoint sourceThird = GeometryMath.CreatePoint(0.0, 4.0);
        BarcodePoint destinationFirst = GeometryMath.CreatePoint(1.0, 2.0);
        BarcodePoint destinationSecond = GeometryMath.CreatePoint(5.0, 2.0);
        BarcodePoint destinationThird = GeometryMath.CreatePoint(1.0, 10.0);

        AffineTransform transform = AffineTransform.CreateFromPointPairs(
            sourceFirst,
            sourceSecond,
            sourceThird,
            destinationFirst,
            destinationSecond,
            destinationThird);
        BarcodePoint[] mapped = transform.TransformPointArray(new BarcodePoint[]
        {
            sourceFirst,
            sourceSecond,
            sourceThird,
            GeometryMath.CreatePoint(1.0, 2.0)
        });

        Assert.Multiple(() =>
        {
            AssertPoint(mapped[0], 1.0, 2.0);
            AssertPoint(mapped[1], 5.0, 2.0);
            AssertPoint(mapped[2], 1.0, 10.0);
            AssertPoint(mapped[3], 3.0, 6.0);
        });
    }

    [Test]
    public void AffineTransformsRejectDegenerateAndInvalidInputs()
    {
        BarcodePoint zero = GeometryMath.CreatePoint(0.0, 0.0);
        BarcodePoint one = GeometryMath.CreatePoint(1.0, 1.0);
        BarcodePoint two = GeometryMath.CreatePoint(2.0, 2.0);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => AffineTransform.CreateFromPointPairs(zero, one, two, zero, one, two),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => new AffineTransform(1.0, 2.0, 0.0, 2.0, 4.0, 0.0).InvertTransform(),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => AffineTransform.CreateIdentity().TransformPointArray(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => AffineTransform.CreateIdentity().ComposeAfter(null!), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void PerspectiveTransformMapsSquareCornersAndRoundTripsInteriorPoint()
    {
        BarcodeQuadrilateral trapezoid = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(2.0, 1.0),
            GeometryMath.CreatePoint(8.0, 2.0),
            GeometryMath.CreatePoint(7.0, 9.0),
            GeometryMath.CreatePoint(1.0, 7.0));
        PerspectiveTransform transform = PerspectiveTransform.CreateSquareToQuadrilateral(trapezoid);
        BarcodePoint[] mapped = transform.TransformPointArray(new BarcodePoint[]
        {
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(1.0, 0.0),
            GeometryMath.CreatePoint(1.0, 1.0),
            GeometryMath.CreatePoint(0.0, 1.0)
        });
        BarcodePoint source = GeometryMath.CreatePoint(0.35, 0.6);
        BarcodePoint recovered = transform.InvertTransform().TransformPoint(transform.TransformPoint(source));

        Assert.Multiple(() =>
        {
            AssertPoint(mapped[0], 2.0, 1.0);
            AssertPoint(mapped[1], 8.0, 2.0);
            AssertPoint(mapped[2], 7.0, 9.0);
            AssertPoint(mapped[3], 1.0, 7.0);
            AssertPoint(recovered, source.X, source.Y);
        });
    }

    [Test]
    public void PerspectiveQuadrilateralMappingAndCompositionUseApplicationOrder()
    {
        BarcodeQuadrilateral source = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(2.0, 0.0),
            GeometryMath.CreatePoint(2.0, 2.0),
            GeometryMath.CreatePoint(0.0, 2.0));
        BarcodeQuadrilateral destination = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(1.0, 1.0),
            GeometryMath.CreatePoint(5.0, 1.0),
            GeometryMath.CreatePoint(4.0, 6.0),
            GeometryMath.CreatePoint(0.0, 5.0));
        PerspectiveTransform mapping = PerspectiveTransform.CreateQuadrilateralMapping(source, destination);
        PerspectiveTransform identityThenMapping = PerspectiveTransform.CreateIdentity().ComposeAfter(mapping);

        Assert.Multiple(() =>
        {
            AssertPoint(mapping.TransformPoint(source.TopLeft!), 1.0, 1.0);
            AssertPoint(mapping.TransformPoint(source.TopRight!), 5.0, 1.0);
            AssertPoint(mapping.TransformPoint(source.BottomRight!), 4.0, 6.0);
            AssertPoint(mapping.TransformPoint(source.BottomLeft!), 0.0, 5.0);
            AssertPoint(identityThenMapping.TransformPoint(GeometryMath.CreatePoint(1.0, 1.0)), mapping.TransformPoint(GeometryMath.CreatePoint(1.0, 1.0)).X, mapping.TransformPoint(GeometryMath.CreatePoint(1.0, 1.0)).Y);
        });
    }

    [Test]
    public void PerspectiveTransformsRejectDegenerateMappingsAndPointsAtInfinity()
    {
        BarcodeQuadrilateral degenerate = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(1.0, 0.0),
            GeometryMath.CreatePoint(2.0, 0.0),
            GeometryMath.CreatePoint(3.0, 0.0));
        PerspectiveTransform pointAtInfinity = new PerspectiveTransform(
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            1.0, 0.0, 0.0);
        BarcodeQuadrilateral affineDegenerate = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(1.0, 0.0),
            GeometryMath.CreatePoint(2.0, 0.0),
            GeometryMath.CreatePoint(1.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(() => PerspectiveTransform.CreateSquareToQuadrilateral(degenerate), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PerspectiveTransform.CreateSquareToQuadrilateral(affineDegenerate), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => pointAtInfinity.TransformPoint(GeometryMath.CreatePoint(0.0, 1.0)), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => new PerspectiveTransform(1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0).InvertTransform(),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PerspectiveTransform.CreateIdentity().TransformPointArray(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PerspectiveTransform.CreateIdentity().ComposeAfter(null!), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void ImageCoordinateTransformRoundTripsPointsAndRectangles()
    {
        ImageCoordinateTransform transform = new ImageCoordinateTransform(
            2.5,
            4.0,
            10.0,
            -6.0);
        BarcodePoint sourcePoint = GeometryMath.CreatePoint(15.0, 14.0);
        BarcodeRectangle sourceRectangle = GeometryMath.CreateRectangle(
            15.0,
            14.0,
            20.0,
            12.0);

        BarcodePoint mappedPoint = transform.MapSourcePoint(sourcePoint);
        BarcodePoint restoredPoint = transform.RestorePoint(mappedPoint);
        BarcodeRectangle mappedRectangle = transform.MapSourceRectangle(
            sourceRectangle);
        BarcodeRectangle restoredRectangle = transform.RestoreRectangle(
            mappedRectangle);

        Assert.Multiple(() =>
        {
            AssertPoint(mappedPoint, 2.0, 5.0);
            AssertPoint(restoredPoint, sourcePoint.X, sourcePoint.Y);
            Assert.That(mappedRectangle.X, Is.EqualTo(2.0));
            Assert.That(mappedRectangle.Y, Is.EqualTo(5.0));
            Assert.That(mappedRectangle.Width, Is.EqualTo(8.0));
            Assert.That(mappedRectangle.Height, Is.EqualTo(3.0));
            Assert.That(restoredRectangle.X, Is.EqualTo(sourceRectangle.X));
            Assert.That(restoredRectangle.Y, Is.EqualTo(sourceRectangle.Y));
            Assert.That(restoredRectangle.Width, Is.EqualTo(sourceRectangle.Width));
            Assert.That(restoredRectangle.Height, Is.EqualTo(sourceRectangle.Height));
        });
    }

    [Test]
    public void ImageCoordinateTransformRestoresEveryQuadrilateralCorner()
    {
        ImageCoordinateTransform transform = new ImageCoordinateTransform(
            2.0,
            3.0,
            5.0,
            7.0);
        BarcodeQuadrilateral quadrilateral = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(4.0, 0.0),
            GeometryMath.CreatePoint(4.0, 2.0),
            GeometryMath.CreatePoint(0.0, 2.0));

        BarcodeQuadrilateral restored = transform.RestoreQuadrilateral(
            quadrilateral);

        Assert.Multiple(() =>
        {
            AssertPoint(restored.TopLeft!, 5.0, 7.0);
            AssertPoint(restored.TopRight!, 13.0, 7.0);
            AssertPoint(restored.BottomRight!, 13.0, 13.0);
            AssertPoint(restored.BottomLeft!, 5.0, 13.0);
            Assert.That(restored.Bounds!.X, Is.EqualTo(5.0));
            Assert.That(restored.Bounds.Y, Is.EqualTo(7.0));
            Assert.That(restored.Bounds.Width, Is.EqualTo(8.0));
            Assert.That(restored.Bounds.Height, Is.EqualTo(6.0));
        });
    }

    [Test]
    public void ImageCoordinateTransformRejectsInvalidScalesAndMissingValues()
    {
        ImageCoordinateTransform transform = new ImageCoordinateTransform(
            1.0,
            1.0,
            0.0,
            0.0);

        Assert.Multiple(() =>
        {
            Assert.That(() => new ImageCoordinateTransform(0.0, 1.0, 0.0, 0.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ImageCoordinateTransform(1.0, -1.0, 0.0, 0.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ImageCoordinateTransform(double.NaN, 1.0, 0.0, 0.0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => transform.RestorePoint(null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => transform.MapSourcePoint(null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => transform.RestoreRectangle(null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => transform.MapSourceRectangle(null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => transform.RestoreQuadrilateral(null), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static void AssertPoint(BarcodePoint point, double expectedX, double expectedY)
    {
        Assert.Multiple(() =>
        {
            Assert.That(point.X, Is.EqualTo(expectedX).Within(0.000000001));
            Assert.That(point.Y, Is.EqualTo(expectedY).Within(0.000000001));
        });
    }
}
