namespace svg.Tests;

public class SvgMatrixMathTests
{
    [Test]
    public void GeometryValueConstructorsValidateAndNormalizeComponents()
    {
        SvgPoint point = new SvgPoint(-0.0, 2);
        SvgVector vector = new SvgVector(3, -4);
        SvgSize size = new SvgSize(5, 0);
        SvgRect rectangle = SvgRect.Create(1, 2, 3, 4, true);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToInt64Bits(point.X), Is.Zero);
            Assert.That(point.Y, Is.EqualTo(2));
            Assert.That(vector.X, Is.EqualTo(3));
            Assert.That(vector.Y, Is.EqualTo(-4));
            Assert.That(size.Width, Is.EqualTo(5));
            Assert.That(size.Height, Is.Zero);
            Assert.That(rectangle.IsEmpty, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPoint(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgVector(0, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSize(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgRect.Create(0, 0, 1, double.NegativeInfinity));
        });
    }

    [Test]
    public void MultiplicationPostMultipliesAndPreservesOperationOrder()
    {
        SvgMatrix translation = new SvgMatrix(1, 0, 0, 1, 10, 20);
        SvgMatrix scale = new SvgMatrix(2, 0, 0, 3, 0, 0);

        SvgMatrixOperationResult translationThenScale = translation.Multiply(scale);
        SvgMatrixOperationResult scaleThenTranslation = new SvgGeometry().Multiply(scale, translation);

        AssertMatrix(translationThenScale, 2, 0, 0, 3, 10, 20);
        AssertMatrix(scaleThenTranslation, 2, 0, 0, 3, 20, 60);
    }

    [Test]
    public void InversionProducesAnIdentityProductAcrossVeryDifferentScales()
    {
        SvgMatrix matrix = new SvgMatrix(2, 1, 0.5, 3, 10, -4);
        SvgMatrixOperationResult inverse = matrix.Invert();
        SvgMatrixOperationResult identity = matrix.Multiply(inverse.Matrix);
        SvgMatrix secondPivot = new SvgMatrix(1, 3, 2, 4, 5, 6);
        SvgMatrixOperationResult secondPivotIdentity = secondPivot.Multiply(secondPivot.Invert().Matrix);
        SvgMatrix extreme = new SvgMatrix(1e300, 0, 0, 1e-300, 0, 0);
        SvgMatrixOperationResult extremeInverse = new SvgGeometry().Invert(extreme);

        AssertMatrix(identity, 1, 0, 0, 1, 0, 0);
        AssertMatrix(secondPivotIdentity, 1, 0, 0, 1, 0, 0);
        Assert.Multiple(() =>
        {
            Assert.That(inverse.Success, Is.True);
            Assert.That(extremeInverse.Success, Is.True);
            Assert.That(extremeInverse.Matrix.A, Is.EqualTo(1e-300).Within(1e-315));
            Assert.That(extremeInverse.Matrix.D, Is.EqualTo(1e300).Within(1e285));
        });
    }

    [Test]
    public void InversionReportsSingularInvalidAndNonFiniteOutcomes()
    {
        SvgMatrixOperationResult singular = new SvgMatrix(1, 2, 2, 4, 0, 0).Invert();
        SvgMatrixOperationResult overflow = new SvgMatrix(double.Epsilon, 0, 0, 1, 0, 0).Invert();
        SvgMatrix mutated = new SvgMatrix(1, 0, 0, 1, 0, 0);
        mutated.A = double.NaN;
        SvgMatrixOperationResult invalid = mutated.Invert();

        Assert.Multiple(() =>
        {
            Assert.That(singular.Success, Is.False);
            Assert.That(singular.Error, Is.EqualTo(SvgGeometryError.SingularMatrix));
            Assert.That(singular.Matrix, Is.Null);
            Assert.That(overflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(invalid.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
        });
    }

    [Test]
    public void PointAndVectorTransformsDifferOnlyByTranslation()
    {
        SvgMatrix matrix = new SvgMatrix(2, 1, -1, 3, 10, 20);

        SvgPointTransformResult point = matrix.TransformPoint(new SvgPoint(4, 5));
        SvgVectorTransformResult vector = new SvgGeometry().TransformVector(matrix, new SvgVector(4, 5));

        Assert.Multiple(() =>
        {
            Assert.That(point.Success, Is.True);
            Assert.That(point.Point.X, Is.EqualTo(13));
            Assert.That(point.Point.Y, Is.EqualTo(39));
            Assert.That(vector.Success, Is.True);
            Assert.That(vector.Vector.X, Is.EqualTo(3));
            Assert.That(vector.Vector.Y, Is.EqualTo(19));
        });
    }

    [Test]
    public void RectangleTransformReturnsAxisAlignedBoundsAndPreservesEmptyState()
    {
        SvgMatrix matrix = new SvgMatrix(0, 1, -1, 0, 10, 20);

        SvgRectTransformResult bounds = matrix.TransformRect(SvgRect.Create(1, 2, 3, 4));
        SvgRectTransformResult empty = new SvgGeometry().TransformRect(matrix, SvgRect.Create(1, 2, 100, 200, true));

        Assert.Multiple(() =>
        {
            Assert.That(bounds.Success, Is.True);
            Assert.That(bounds.Rectangle.X, Is.EqualTo(4));
            Assert.That(bounds.Rectangle.Y, Is.EqualTo(21));
            Assert.That(bounds.Rectangle.Width, Is.EqualTo(4));
            Assert.That(bounds.Rectangle.Height, Is.EqualTo(3));
            Assert.That(bounds.Rectangle.IsEmpty, Is.False);
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Rectangle.X, Is.EqualTo(8));
            Assert.That(empty.Rectangle.Y, Is.EqualTo(21));
            Assert.That(empty.Rectangle.Width, Is.Zero);
            Assert.That(empty.Rectangle.Height, Is.Zero);
            Assert.That(empty.Rectangle.IsEmpty, Is.True);
        });
    }

    [Test]
    public void TransformsReportInvalidStateAndArithmeticOverflow()
    {
        SvgMatrix largeScale = new SvgMatrix(double.MaxValue, 0, 0, 1, 0, 0);
        SvgPointTransformResult pointOverflow = largeScale.TransformPoint(new SvgPoint(2, 0));
        SvgVector mutatedVector = new SvgVector(1, 1);
        mutatedVector.Y = double.NaN;
        SvgVectorTransformResult invalidVector = largeScale.TransformVector(mutatedVector);
        SvgRectTransformResult edgeOverflow = new SvgGeometry().TransformRect(new SvgMatrix(1, 0, 0, 1, 0, 0), SvgRect.Create(double.MaxValue, 0, double.MaxValue, 1));
        SvgRectTransformResult boundsOverflow = new SvgGeometry().TransformRect(new SvgMatrix(2, 0, 0, 1, 0, 0), SvgRect.Create(-double.MaxValue / 2, 0, double.MaxValue, 1));
        SvgMatrixOperationResult multiplyOverflow = largeScale.Multiply(new SvgMatrix(2, 0, 0, 1, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(pointOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(invalidVector.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(edgeOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(boundsOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
            Assert.That(multiplyOverflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
        });
    }

    [Test]
    public void DecompositionRoundTripsRotationSkewScaleReflectionAndTranslation()
    {
        SvgMatrixDecomposition components = new SvgMatrixDecomposition(5, -7, 0.6, 2, -3, 0.25);
        SvgMatrixOperationResult original = components.Recompose();

        SvgMatrixDecompositionResult decomposition = original.Matrix.Decompose();
        SvgMatrixOperationResult roundTrip = new SvgGeometry().Recompose(decomposition.Decomposition);

        AssertMatrix(roundTrip, original.Matrix.A, original.Matrix.B, original.Matrix.C, original.Matrix.D, 5, -7);
        Assert.Multiple(() =>
        {
            Assert.That(decomposition.Success, Is.True);
            Assert.That(decomposition.Decomposition.TranslateX, Is.EqualTo(5));
            Assert.That(decomposition.Decomposition.TranslateY, Is.EqualTo(-7));
            Assert.That(decomposition.Decomposition.Rotation, Is.EqualTo(0.6).Within(1e-12));
            Assert.That(decomposition.Decomposition.ScaleX, Is.EqualTo(2).Within(1e-12));
            Assert.That(decomposition.Decomposition.ScaleY, Is.EqualTo(-3).Within(1e-12));
            Assert.That(decomposition.Decomposition.SkewX, Is.EqualTo(0.25).Within(1e-12));
        });
    }

    [Test]
    public void DecompositionAndRecompositionReturnStableFailures()
    {
        SvgMatrixDecompositionResult singular = new SvgGeometry().Decompose(new SvgMatrix(1, 2, 2, 4, 0, 0));
        SvgMatrix mutated = new SvgMatrix(1, 0, 0, 1, 0, 0);
        mutated.C = double.NegativeInfinity;
        SvgMatrixDecompositionResult invalid = mutated.Decompose();
        SvgMatrixDecomposition mutatedComponents = new SvgMatrixDecomposition(0, 0, 0, 1, 1, 0);
        mutatedComponents.ScaleX = double.NaN;
        SvgMatrixOperationResult invalidComponents = mutatedComponents.Recompose();
        SvgMatrixOperationResult nonFinite = new SvgMatrixDecomposition(0, 0, 0, double.MaxValue, double.MaxValue, 1.2).Recompose();

        Assert.Multiple(() =>
        {
            Assert.That(singular.Error, Is.EqualTo(SvgGeometryError.SingularMatrix));
            Assert.That(singular.Decomposition, Is.Null);
            Assert.That(invalid.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(invalidComponents.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(nonFinite.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
        });
    }

    [Test]
    public void InterpolationBlendsComponentsAndUsesTheShortestRotation()
    {
        SvgMatrix start = new SvgMatrixDecomposition(0, 0, 0, 1, 1, 0).Recompose().Matrix;
        SvgMatrix end = new SvgMatrixDecomposition(10, -6, Math.PI / 2, 3, 5, 0.4).Recompose().Matrix;
        SvgMatrix expectedMiddle = new SvgMatrixDecomposition(5, -3, Math.PI / 4, 2, 3, 0.2).Recompose().Matrix;
        SvgMatrixOperationResult middle = start.Interpolate(end, 0.5);
        SvgMatrix firstRotation = new SvgMatrixDecomposition(0, 0, Degrees(170), 1, 1, 0).Recompose().Matrix;
        SvgMatrix secondRotation = new SvgMatrixDecomposition(0, 0, Degrees(-170), 1, 1, 0).Recompose().Matrix;
        SvgMatrixOperationResult shortest = new SvgGeometry().Interpolate(firstRotation, secondRotation, 0.5);

        AssertMatrix(middle, expectedMiddle.A, expectedMiddle.B, expectedMiddle.C, expectedMiddle.D, expectedMiddle.E, expectedMiddle.F);
        AssertMatrix(shortest, -1, 0, 0, -1, 0, 0);
    }

    [Test]
    public void InterpolationReportsInvalidSingularAndOverflowCases()
    {
        SvgMatrix identity = new SvgMatrix(1, 0, 0, 1, 0, 0);
        SvgMatrix singular = new SvgMatrix(0, 0, 0, 1, 0, 0);
        SvgMatrixOperationResult invalidAmount = identity.Interpolate(identity, double.NaN);
        SvgMatrixOperationResult singularEndpoint = identity.Interpolate(singular, 0.5);
        SvgMatrix far = new SvgMatrix(1, 0, 0, 1, double.MaxValue, 0);
        SvgMatrixOperationResult overflow = identity.Interpolate(far, double.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(invalidAmount.Error, Is.EqualTo(SvgGeometryError.InvalidArgument));
            Assert.That(singularEndpoint.Error, Is.EqualTo(SvgGeometryError.SingularMatrix));
            Assert.That(overflow.Error, Is.EqualTo(SvgGeometryError.NonFiniteResult));
        });
    }

    static double Degrees(double value)
    {
        return value * Math.PI / 180;
    }

    static void AssertMatrix(SvgMatrixOperationResult result, double a, double b, double c, double d, double e, double f)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgGeometryError.None));
            Assert.That(result.Matrix, Is.Not.Null);
            Assert.That(result.Matrix.A, Is.EqualTo(a).Within(1e-11));
            Assert.That(result.Matrix.B, Is.EqualTo(b).Within(1e-11));
            Assert.That(result.Matrix.C, Is.EqualTo(c).Within(1e-11));
            Assert.That(result.Matrix.D, Is.EqualTo(d).Within(1e-11));
            Assert.That(result.Matrix.E, Is.EqualTo(e).Within(1e-11));
            Assert.That(result.Matrix.F, Is.EqualTo(f).Within(1e-11));
        });
    }
}
