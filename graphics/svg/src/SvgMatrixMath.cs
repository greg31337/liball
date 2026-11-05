namespace svg;

/// <summary>Implements finite affine geometry algorithms.</summary>
internal static class SvgMatrixMath
{
    /// <summary>Post-multiplies two matrices.</summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="right">The right matrix.</param>
    /// <returns>A finite product or stable failure.</returns>
    public static SvgMatrixOperationResult Multiply(SvgMatrix left, SvgMatrix right)
    {
        if (!ValidMatrix(left) || !ValidMatrix(right)) return MatrixFailure(SvgGeometryError.InvalidArgument);
        return Matrix(left.A * right.A + left.C * right.B, left.B * right.A + left.D * right.B, left.A * right.C + left.C * right.D, left.B * right.C + left.D * right.D, left.A * right.E + left.C * right.F + left.E, left.B * right.E + left.D * right.F + left.F);
    }

    /// <summary>Inverts one affine matrix with scaled determinant evaluation.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>A finite inverse or stable failure.</returns>
    public static SvgMatrixOperationResult Invert(SvgMatrix matrix)
    {
        if (!ValidMatrix(matrix)) return MatrixFailure(SvgGeometryError.InvalidArgument);
        double inverseA;
        double inverseB;
        double inverseC;
        double inverseD;
        if (System.Math.Abs(matrix.A) >= System.Math.Abs(matrix.B))
        {
            if (matrix.A == 0) return MatrixFailure(SvgGeometryError.SingularMatrix);
            double ratio = matrix.B / matrix.A;
            double pivot = System.Math.FusedMultiplyAdd(-ratio, matrix.C, matrix.D);
            if (pivot == 0) return MatrixFailure(SvgGeometryError.SingularMatrix);
            if (!SvgFiniteMath.IsFinite(pivot)) return MatrixFailure(SvgGeometryError.NonFiniteResult);
            inverseA = DivideTwice(matrix.D, matrix.A, pivot);
            inverseB = DivideTwice(-matrix.B, matrix.A, pivot);
            inverseC = DivideTwice(-matrix.C, matrix.A, pivot);
            inverseD = 1 / pivot;
        }
        else
        {
            double ratio = matrix.A / matrix.B;
            double pivot = System.Math.FusedMultiplyAdd(-ratio, matrix.D, matrix.C);
            if (pivot == 0) return MatrixFailure(SvgGeometryError.SingularMatrix);
            if (!SvgFiniteMath.IsFinite(pivot)) return MatrixFailure(SvgGeometryError.NonFiniteResult);
            inverseA = DivideTwice(-matrix.D, matrix.B, pivot);
            inverseB = 1 / pivot;
            inverseC = DivideTwice(matrix.C, matrix.B, pivot);
            inverseD = DivideTwice(-matrix.A, matrix.B, pivot);
        }
        double inverseE = -(inverseA * matrix.E + inverseC * matrix.F);
        double inverseF = -(inverseB * matrix.E + inverseD * matrix.F);
        return Matrix(inverseA, inverseB, inverseC, inverseD, inverseE, inverseF);
    }

    /// <summary>Transforms one point.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="point">The point.</param>
    /// <returns>A finite transformed point or stable failure.</returns>
    public static SvgPointTransformResult TransformPoint(SvgMatrix matrix, SvgPoint point)
    {
        if (!ValidMatrix(matrix) || !ValidPoint(point)) return new SvgPointTransformResult(false, SvgGeometryError.InvalidArgument, null);
        double x = matrix.A * point.X + matrix.C * point.Y + matrix.E;
        double y = matrix.B * point.X + matrix.D * point.Y + matrix.F;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return new SvgPointTransformResult(false, SvgGeometryError.NonFiniteResult, null);
        return new SvgPointTransformResult(true, SvgGeometryError.None, new SvgPoint(x, y));
    }

    /// <summary>Transforms one vector without translation.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>A finite transformed vector or stable failure.</returns>
    public static SvgVectorTransformResult TransformVector(SvgMatrix matrix, SvgVector vector)
    {
        if (!ValidMatrix(matrix) || !ValidVector(vector)) return new SvgVectorTransformResult(false, SvgGeometryError.InvalidArgument, null);
        double x = matrix.A * vector.X + matrix.C * vector.Y;
        double y = matrix.B * vector.X + matrix.D * vector.Y;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return new SvgVectorTransformResult(false, SvgGeometryError.NonFiniteResult, null);
        return new SvgVectorTransformResult(true, SvgGeometryError.None, new SvgVector(x, y));
    }

    /// <summary>Transforms one rectangle to axis-aligned bounds.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>A finite transformed rectangle or stable failure.</returns>
    public static SvgRectTransformResult TransformRect(SvgMatrix matrix, SvgRect rectangle)
    {
        if (!ValidMatrix(matrix) || !ValidRect(rectangle)) return new SvgRectTransformResult(false, SvgGeometryError.InvalidArgument, null);
        SvgPointTransformResult first = TransformPoint(matrix, new SvgPoint(rectangle.X, rectangle.Y));
        if (!first.Success) return new SvgRectTransformResult(false, first.Error, null);
        if (rectangle.IsEmpty) return new SvgRectTransformResult(true, SvgGeometryError.None, SvgRect.Create((first.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, first.Point.Y, 0, 0, true));
        double right = rectangle.X + rectangle.Width;
        double bottom = rectangle.Y + rectangle.Height;
        if (!SvgFiniteMath.IsFinite(right) || !SvgFiniteMath.IsFinite(bottom)) return new SvgRectTransformResult(false, SvgGeometryError.NonFiniteResult, null);
        SvgPointTransformResult second = TransformPoint(matrix, new SvgPoint(right, rectangle.Y));
        SvgPointTransformResult third = TransformPoint(matrix, new SvgPoint(rectangle.X, bottom));
        SvgPointTransformResult fourth = TransformPoint(matrix, new SvgPoint(right, bottom));
        if (!second.Success || !third.Success || !fourth.Success) return new SvgRectTransformResult(false, SvgGeometryError.NonFiniteResult, null);
        double minimumX = System.Math.Min(System.Math.Min((first.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (second.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X), System.Math.Min((third.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, (fourth.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X));
        double maximumX = System.Math.Max(System.Math.Max(first.Point.X, second.Point.X), System.Math.Max(third.Point.X, fourth.Point.X));
        double minimumY = System.Math.Min(System.Math.Min(first.Point.Y, second.Point.Y), System.Math.Min(third.Point.Y, fourth.Point.Y));
        double maximumY = System.Math.Max(System.Math.Max(first.Point.Y, second.Point.Y), System.Math.Max(third.Point.Y, fourth.Point.Y));
        double width = maximumX - minimumX;
        double height = maximumY - minimumY;
        if (!SvgFiniteMath.IsFinite(width) || !SvgFiniteMath.IsFinite(height)) return new SvgRectTransformResult(false, SvgGeometryError.NonFiniteResult, null);
        return new SvgRectTransformResult(true, SvgGeometryError.None, new SvgRect(minimumX, minimumY, width, height));
    }

    /// <summary>Decomposes one nonsingular affine matrix.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>A finite decomposition or stable failure.</returns>
    public static SvgMatrixDecompositionResult Decompose(SvgMatrix matrix)
    {
        if (!ValidMatrix(matrix)) return DecompositionFailure(SvgGeometryError.InvalidArgument);
        double scaleX = Hypotenuse(matrix.A, matrix.B);
        if (scaleX == 0) return DecompositionFailure(SvgGeometryError.SingularMatrix);
        if (!SvgFiniteMath.IsFinite(scaleX)) return DecompositionFailure(SvgGeometryError.NonFiniteResult);
        double cosine = matrix.A / scaleX;
        double sine = matrix.B / scaleX;
        double projected = cosine * matrix.C + sine * matrix.D;
        double scaleY = -sine * matrix.C + cosine * matrix.D;
        if (scaleY == 0) return DecompositionFailure(SvgGeometryError.SingularMatrix);
        double skewFactor = projected / scaleY;
        double rotation = System.Math.Atan2(sine, cosine);
        double skew = System.Math.Atan(skewFactor);
        if (!SvgFiniteMath.IsFinite(projected) || !SvgFiniteMath.IsFinite(scaleY) || !SvgFiniteMath.IsFinite(skewFactor) || !SvgFiniteMath.IsFinite(rotation) || !SvgFiniteMath.IsFinite(skew)) return DecompositionFailure(SvgGeometryError.NonFiniteResult);
        SvgMatrixDecomposition decomposition = new SvgMatrixDecomposition(matrix.E, matrix.F, rotation, scaleX, scaleY, skew);
        return new SvgMatrixDecompositionResult(true, SvgGeometryError.None, decomposition);
    }

    /// <summary>Recomposes finite affine components.</summary>
    /// <param name="decomposition">The components.</param>
    /// <returns>A finite matrix or stable failure.</returns>
    public static SvgMatrixOperationResult Recompose(SvgMatrixDecomposition decomposition)
    {
        if (!ValidDecomposition(decomposition)) return MatrixFailure(SvgGeometryError.InvalidArgument);
        double cosine = System.Math.Cos(decomposition.Rotation);
        double sine = System.Math.Sin(decomposition.Rotation);
        double tangent = System.Math.Tan(decomposition.SkewX);
        double a = cosine * decomposition.ScaleX;
        double b = sine * decomposition.ScaleX;
        double c = (cosine * tangent - sine) * decomposition.ScaleY;
        double d = (sine * tangent + cosine) * decomposition.ScaleY;
        return Matrix(a, b, c, d, decomposition.TranslateX, decomposition.TranslateY);
    }

    /// <summary>Interpolates two nonsingular matrices through affine components.</summary>
    /// <param name="from">The starting matrix.</param>
    /// <param name="to">The target matrix.</param>
    /// <param name="amount">The finite interpolation amount.</param>
    /// <returns>A finite interpolated matrix or stable failure.</returns>
    public static SvgMatrixOperationResult Interpolate(SvgMatrix from, SvgMatrix to, double amount)
    {
        if (!SvgFiniteMath.IsFinite(amount)) return MatrixFailure(SvgGeometryError.InvalidArgument);
        SvgMatrixDecompositionResult fromResult = Decompose(from);
        if (!fromResult.Success) return MatrixFailure(fromResult.Error);
        SvgMatrixDecompositionResult toResult = Decompose(to);
        if (!toResult.Success) return MatrixFailure(toResult.Error);
        SvgMatrixDecomposition? first = fromResult.Decomposition;
        SvgMatrixDecomposition? second = toResult.Decomposition;
        double deltaRotation = (second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Rotation - (first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Rotation;
        double fullTurn = System.Math.PI * 2;
        while (deltaRotation > System.Math.PI) deltaRotation = deltaRotation - fullTurn;
        while (deltaRotation < -System.Math.PI) deltaRotation = deltaRotation + fullTurn;
        SvgFiniteResult translateX = SvgFiniteMath.Lerp(first.TranslateX, second.TranslateX, amount);
        SvgFiniteResult translateY = SvgFiniteMath.Lerp(first.TranslateY, second.TranslateY, amount);
        double rotation = first.Rotation + deltaRotation * amount;
        SvgFiniteResult scaleX = SvgFiniteMath.Lerp(first.ScaleX, second.ScaleX, amount);
        SvgFiniteResult scaleY = SvgFiniteMath.Lerp(first.ScaleY, second.ScaleY, amount);
        SvgFiniteResult skewX = SvgFiniteMath.Lerp(first.SkewX, second.SkewX, amount);
        if (!translateX.Success || !translateY.Success || !SvgFiniteMath.IsFinite(rotation) || !scaleX.Success || !scaleY.Success || !skewX.Success) return MatrixFailure(SvgGeometryError.NonFiniteResult);
        return Recompose(new SvgMatrixDecomposition(translateX.Value, translateY.Value, rotation, scaleX.Value, scaleY.Value, skewX.Value));
    }

    static double Hypotenuse(double x, double y)
    {
        double absoluteX = System.Math.Abs(x);
        double absoluteY = System.Math.Abs(y);
        double maximum = System.Math.Max(absoluteX, absoluteY);
        if (maximum == 0) return 0;
        double minimum = System.Math.Min(absoluteX, absoluteY);
        double ratio = minimum / maximum;
        return maximum * System.Math.Sqrt(1 + ratio * ratio);
    }

    static double DivideTwice(double numerator, double firstDenominator, double secondDenominator)
    {
        if (numerator == 0) return 0;
        int numeratorExponent = System.Math.ILogB(numerator);
        int firstExponent = System.Math.ILogB(firstDenominator);
        int secondExponent = System.Math.ILogB(secondDenominator);
        double scaledNumerator = System.Math.ScaleB(numerator, -numeratorExponent);
        double scaledFirst = System.Math.ScaleB(firstDenominator, -firstExponent);
        double scaledSecond = System.Math.ScaleB(secondDenominator, -secondExponent);
        double quotient = scaledNumerator / scaledFirst / scaledSecond;
        return System.Math.ScaleB(quotient, numeratorExponent - firstExponent - secondExponent);
    }

    static SvgMatrixOperationResult Matrix(double a, double b, double c, double d, double e, double f)
    {
        if (!SvgFiniteMath.IsFinite(a) || !SvgFiniteMath.IsFinite(b) || !SvgFiniteMath.IsFinite(c) || !SvgFiniteMath.IsFinite(d) || !SvgFiniteMath.IsFinite(e) || !SvgFiniteMath.IsFinite(f)) return MatrixFailure(SvgGeometryError.NonFiniteResult);
        return new SvgMatrixOperationResult(true, SvgGeometryError.None, new SvgMatrix(a, b, c, d, e, f));
    }

    static SvgMatrixOperationResult MatrixFailure(SvgGeometryError error)
    {
        return new SvgMatrixOperationResult(false, error, null);
    }

    static SvgMatrixDecompositionResult DecompositionFailure(SvgGeometryError error)
    {
        return new SvgMatrixDecompositionResult(false, error, null);
    }

    static bool ValidMatrix(SvgMatrix? matrix)
    {
        return matrix != null && SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }

    static bool ValidPoint(SvgPoint point)
    {
        return point != null && SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }

    static bool ValidVector(SvgVector vector)
    {
        return vector != null && SvgFiniteMath.IsFinite(vector.X) && SvgFiniteMath.IsFinite(vector.Y);
    }

    static bool ValidRect(SvgRect? rectangle)
    {
        return rectangle != null && SvgFiniteMath.IsFinite(rectangle.X) && SvgFiniteMath.IsFinite(rectangle.Y) && SvgFiniteMath.IsFinite(rectangle.Width) && SvgFiniteMath.IsFinite(rectangle.Height) && rectangle.Width >= 0 && rectangle.Height >= 0;
    }

    static bool ValidDecomposition(SvgMatrixDecomposition value)
    {
        return value != null && SvgFiniteMath.IsFinite(value.TranslateX) && SvgFiniteMath.IsFinite(value.TranslateY) && SvgFiniteMath.IsFinite(value.Rotation) && SvgFiniteMath.IsFinite(value.ScaleX) && SvgFiniteMath.IsFinite(value.ScaleY) && SvgFiniteMath.IsFinite(value.SkewX);
    }
}

/// <summary>Provides throwing validation for programmatic geometry values.</summary>
internal static class SvgGeometryValidation
{
    /// <summary>Requires one finite value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="parameterName">The parameter name.</param>
    public static void RequireFinite(double value, string parameterName)
    {
        if (!SvgFiniteMath.IsFinite(value)) throw new System.ArgumentOutOfRangeException(parameterName);
    }

    /// <summary>Requires one finite nonnegative value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="parameterName">The parameter name.</param>
    public static void RequireNonnegative(double value, string parameterName)
    {
        if (!SvgFiniteMath.IsFinite(value) || value < 0) throw new System.ArgumentOutOfRangeException(parameterName);
    }
}
