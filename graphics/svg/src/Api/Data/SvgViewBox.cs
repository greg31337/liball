namespace svg;

/// <summary>Represents a finite SVG affine matrix in a, b, c, d, e, f order.</summary>
public class SvgMatrix
{
    /// <summary>The horizontal x scale or rotation component.</summary>
    public double A;

    /// <summary>The vertical x rotation or skew component.</summary>
    public double B;

    /// <summary>The horizontal y rotation or skew component.</summary>
    public double C;

    /// <summary>The vertical y scale or rotation component.</summary>
    public double D;

    /// <summary>The horizontal translation component.</summary>
    public double E;

    /// <summary>The vertical translation component.</summary>
    public double F;

    /// <summary>Creates a finite affine matrix.</summary>
    /// <param name="a">The horizontal x component.</param>
    /// <param name="b">The vertical x component.</param>
    /// <param name="c">The horizontal y component.</param>
    /// <param name="d">The vertical y component.</param>
    /// <param name="e">The horizontal translation.</param>
    /// <param name="f">The vertical translation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a component is not finite.</exception>
    public SvgMatrix(double a, double b, double c, double d, double e, double f)
    {
        SvgViewBoxValidation.RequireFinite(a, "a");
        SvgViewBoxValidation.RequireFinite(b, "b");
        SvgViewBoxValidation.RequireFinite(c, "c");
        SvgViewBoxValidation.RequireFinite(d, "d");
        SvgViewBoxValidation.RequireFinite(e, "e");
        SvgViewBoxValidation.RequireFinite(f, "f");
        A = SvgFiniteMath.NormalizeZero(a);
        B = SvgFiniteMath.NormalizeZero(b);
        C = SvgFiniteMath.NormalizeZero(c);
        D = SvgFiniteMath.NormalizeZero(d);
        E = SvgFiniteMath.NormalizeZero(e);
        F = SvgFiniteMath.NormalizeZero(f);
    }

    /// <summary>Tests exact finite affine-component equality.</summary>
    /// <param name="other">The other matrix.</param>
    /// <returns><c>true</c> when both matrices are valid and have equal components.</returns>
    internal bool ValueEquals(SvgMatrix other)
    {
        return new SvgGeometry().EqualWithSvgMatrixAndSvgMatrix(this, other);
    }

    /// <summary>Post-multiplies this matrix by another affine matrix.</summary>
    /// <param name="right">The right-hand matrix.</param>
    /// <returns>A finite product or stable failure.</returns>
    internal SvgMatrixOperationResult Multiply(SvgMatrix right)
    {
        return SvgMatrixMath.Multiply(this, right);
    }

    /// <summary>Computes the inverse affine matrix.</summary>
    /// <returns>A finite inverse or a singular/non-finite failure.</returns>
    internal SvgMatrixOperationResult Invert()
    {
        return SvgMatrixMath.Invert(this);
    }

    /// <summary>Transforms a point, including translation.</summary>
    /// <param name="point">The point to transform.</param>
    /// <returns>A finite transformed point or stable failure.</returns>
    internal SvgPointTransformResult TransformPoint(SvgPoint point)
    {
        return SvgMatrixMath.TransformPoint(this, point);
    }

    /// <summary>Transforms a vector without translation.</summary>
    /// <param name="vector">The vector to transform.</param>
    /// <returns>A finite transformed vector or stable failure.</returns>
    internal SvgVectorTransformResult TransformVector(SvgVector vector)
    {
        return SvgMatrixMath.TransformVector(this, vector);
    }

    /// <summary>Transforms a rectangle to its axis-aligned bounding rectangle.</summary>
    /// <param name="rectangle">The rectangle to transform.</param>
    /// <returns>A finite transformed rectangle or stable failure.</returns>
    internal SvgRectTransformResult TransformRect(SvgRect rectangle)
    {
        return SvgMatrixMath.TransformRect(this, rectangle);
    }

    /// <summary>Decomposes this matrix into translation, rotation, scale, and skew.</summary>
    /// <returns>A stable affine decomposition result.</returns>
    internal SvgMatrixDecompositionResult Decompose()
    {
        return SvgMatrixMath.Decompose(this);
    }

    /// <summary>Interpolates this matrix toward another through affine decomposition.</summary>
    /// <param name="target">The target matrix.</param>
    /// <param name="amount">The finite interpolation amount.</param>
    /// <returns>A finite interpolated matrix or stable failure.</returns>
    internal SvgMatrixOperationResult Interpolate(SvgMatrix target, double amount)
    {
        return SvgMatrixMath.Interpolate(this, target, amount);
    }
}

/// <summary>Represents a finite SVG view box with nonnegative dimensions.</summary>
public class SvgViewBox
{
    /// <summary>The minimum horizontal coordinate.</summary>
    public double X;

    /// <summary>The minimum vertical coordinate.</summary>
    public double Y;

    /// <summary>The nonnegative view-box width.</summary>
    public double Width;

    /// <summary>The nonnegative view-box height.</summary>
    public double Height;

    /// <summary>Creates a finite view box.</summary>
    /// <param name="x">The minimum horizontal coordinate.</param>
    /// <param name="y">The minimum vertical coordinate.</param>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for non-finite coordinates or negative dimensions.</exception>
    public SvgViewBox(double x, double y, double width, double height)
    {
        SvgViewBoxValidation.RequireFinite(x, "x");
        SvgViewBoxValidation.RequireFinite(y, "y");
        SvgViewBoxValidation.RequireFinite(width, "width");
        SvgViewBoxValidation.RequireFinite(height, "height");
        if (width < 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }
        if (height < 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }
        X = SvgFiniteMath.NormalizeZero(x);
        Y = SvgFiniteMath.NormalizeZero(y);
        Width = SvgFiniteMath.NormalizeZero(width);
        Height = SvgFiniteMath.NormalizeZero(height);
    }

    /// <summary>Serializes the four view-box components with canonical spacing.</summary>
    /// <returns>The canonical viewBox text.</returns>
    internal string ToText()
    {
        return SvgViewBoxText.Number(X) + " " + SvgViewBoxText.Number(Y) + " " + SvgViewBoxText.Number(Width) + " " + SvgViewBoxText.Number(Height);
    }
}

/// <summary>Represents a preserveAspectRatio alignment, scale mode, and defer flag.</summary>
public class SvgAspectRatio
{
    /// <summary>The viewport alignment mode.</summary>
    public SvgAspectRatioAlignment Alignment;

    /// <summary>The uniform scaling mode, ignored when alignment is none.</summary>
    public SvgAspectRatioScale Scale;

    /// <summary>Whether the defer keyword was present.</summary>
    public bool Defer;

    /// <summary>Creates a validated preserveAspectRatio value.</summary>
    /// <param name="alignment">The alignment mode.</param>
    /// <param name="scale">The uniform scaling mode.</param>
    /// <param name="defer">Whether the defer keyword is preserved.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is unknown.</exception>
    public SvgAspectRatio(SvgAspectRatioAlignment alignment, SvgAspectRatioScale scale, bool defer)
    {
        if (!SvgAspectRatioNames.IsAlignment(alignment))
        {
            throw new System.ArgumentOutOfRangeException("alignment");
        }
        if (!SvgAspectRatioNames.IsScale(scale))
        {
            throw new System.ArgumentOutOfRangeException("scale");
        }
        Alignment = alignment;
        Scale = scale;
        Defer = defer;
    }

    /// <summary>Computes the affine transform from a view box into a viewport.</summary>
    /// <param name="viewBox">The source view box.</param>
    /// <param name="viewportX">The finite viewport x coordinate.</param>
    /// <param name="viewportY">The finite viewport y coordinate.</param>
    /// <param name="viewportWidth">The finite nonnegative viewport width.</param>
    /// <param name="viewportHeight">The finite nonnegative viewport height.</param>
    /// <returns>A finite fitting matrix or a stable failure.</returns>
    internal SvgViewBoxFitResult Fit(SvgViewBox viewBox, double viewportX, double viewportY, double viewportWidth, double viewportHeight)
    {
        return SvgViewBoxFitter.Fit(viewBox, this, viewportX, viewportY, viewportWidth, viewportHeight);
    }

    /// <summary>Serializes this value with canonical keyword spelling and spacing.</summary>
    /// <returns>The canonical preserveAspectRatio text.</returns>
    internal string ToText()
    {
        string prefix = Defer ? "defer " : "";
        return prefix + SvgAspectRatioNames.AlignmentText(Alignment) + " " + SvgAspectRatioNames.ScaleText(Scale);
    }
}

/// <summary>Contains a non-throwing view-box fitting result.</summary>
public class SvgViewBoxFitResult
{
    /// <summary>Indicates whether fitting produced a finite matrix.</summary>
    public bool Success;

    /// <summary>The fitting failure kind.</summary>
    public SvgViewBoxFitError Error;

    /// <summary>The fitted matrix, or <c>null</c> after failure.</summary>
    public SvgMatrix? Matrix;

    /// <summary>Creates a view-box fitting result.</summary>
    /// <param name="success">Whether fitting succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="matrix">The fitted matrix, or <c>null</c>.</param>
    public SvgViewBoxFitResult(bool success, SvgViewBoxFitError error, SvgMatrix? matrix)
    {
        Success = success;
        Error = error;
        Matrix = matrix;
    }
}
