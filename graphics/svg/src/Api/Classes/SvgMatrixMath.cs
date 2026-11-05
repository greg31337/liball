namespace svg;

/// <summary>Provides stateless affine matrix and geometry operations.</summary>
public class SvgGeometry : ISvgGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgGeometry()
    {
    }

    /// <summary>Tests exact finite point equality.</summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The second point.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    public bool EqualWithSvgPointAndSvgPoint(SvgPoint left, SvgPoint right)
    {
        return SvgGeometryOperations.Equal(left, right);
    }

    /// <summary>Tests exact finite vector equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    public bool EqualWithSvgVectorAndSvgVector(SvgVector left, SvgVector right)
    {
        return SvgGeometryOperations.Equal(left, right);
    }

    /// <summary>Tests exact finite size equality.</summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    public bool EqualWithSvgSizeAndSvgSize(SvgSize left, SvgSize right)
    {
        return SvgGeometryOperations.Equal(left, right);
    }

    /// <summary>Tests geometric rectangle equality, treating valid explicit empty rectangles as equal.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns><c>true</c> when the values are valid and geometrically equal.</returns>
    public bool EqualWithSvgRectAndSvgRect(SvgRect left, SvgRect right)
    {
        return SvgGeometryOperations.Equal(left, right);
    }

    /// <summary>Tests exact finite affine-component equality.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    public bool EqualWithSvgMatrixAndSvgMatrix(SvgMatrix left, SvgMatrix right)
    {
        return SvgGeometryOperations.Equal(left, right);
    }

    /// <summary>Translates a point by a vector.</summary>
    /// <param name="point">The point.</param>
    /// <param name="vector">The translation vector.</param>
    /// <returns>The finite translated point or a stable failure.</returns>
    public SvgPointOperationResult AddWithPointAndVector(SvgPoint point, SvgVector vector)
    {
        return SvgGeometryOperations.Add(point, vector);
    }

    /// <summary>Subtracts two points to form a vector.</summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The point to subtract.</param>
    /// <returns>The finite difference vector or a stable failure.</returns>
    public SvgVectorOperationResult SubtractWithSvgPointAndSvgPoint(SvgPoint left, SvgPoint right)
    {
        return SvgGeometryOperations.Subtract(left, right);
    }

    /// <summary>Adds two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The finite sum or a stable failure.</returns>
    public SvgVectorOperationResult AddWithLeftAndRight(SvgVector left, SvgVector right)
    {
        return SvgGeometryOperations.Add(left, right);
    }

    /// <summary>Subtracts two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The vector to subtract.</param>
    /// <returns>The finite difference or a stable failure.</returns>
    public SvgVectorOperationResult SubtractWithSvgVectorAndSvgVector(SvgVector left, SvgVector right)
    {
        return SvgGeometryOperations.Subtract(left, right);
    }

    /// <summary>Scales a vector.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="factor">The finite scale factor.</param>
    /// <returns>The finite scaled vector or a stable failure.</returns>
    public SvgVectorOperationResult Scale(SvgVector vector, double factor)
    {
        return SvgGeometryOperations.Scale(vector, factor);
    }

    /// <summary>Computes a vector dot product.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The finite scalar result or a stable failure.</returns>
    public SvgScalarOperationResult Dot(SvgVector left, SvgVector right)
    {
        return SvgGeometryOperations.Dot(left, right);
    }

    /// <summary>Computes a signed two-dimensional vector cross product.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The finite scalar result or a stable failure.</returns>
    public SvgScalarOperationResult Cross(SvgVector left, SvgVector right)
    {
        return SvgGeometryOperations.Cross(left, right);
    }

    /// <summary>Computes a vector's Euclidean length.</summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The finite length or a stable failure.</returns>
    public SvgScalarOperationResult Length(SvgVector vector)
    {
        return SvgGeometryOperations.Length(vector);
    }

    /// <summary>Computes the smallest rectangle containing both inputs.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns>The finite union or a stable failure.</returns>
    public SvgRectOperationResult Union(SvgRect left, SvgRect right)
    {
        return SvgGeometryOperations.Union(left, right);
    }

    /// <summary>Computes the closed-set intersection of two rectangles.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns>The finite intersection or a stable failure.</returns>
    public SvgRectOperationResult Intersect(SvgRect left, SvgRect right)
    {
        return SvgGeometryOperations.Intersect(left, right);
    }

    /// <summary>Expands horizontal and vertical rectangle sides by nonnegative amounts.</summary>
    /// <param name="rectangle">The source rectangle.</param>
    /// <param name="horizontal">The amount applied to left and right.</param>
    /// <param name="vertical">The amount applied to top and bottom.</param>
    /// <returns>The finite expanded rectangle or a stable failure.</returns>
    public SvgRectOperationResult Expand(SvgRect rectangle, double horizontal, double vertical)
    {
        return SvgGeometryOperations.Expand(rectangle, horizontal, vertical);
    }

    /// <summary>Tests closed-set rectangle containment of a point.</summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <param name="point">The point.</param>
    /// <returns><c>true</c> when both values are valid and the nonempty rectangle contains the point.</returns>
    public bool Contains(SvgRect rectangle, SvgPoint point)
    {
        return SvgGeometryOperations.Contains(rectangle, point);
    }

    /// <summary>Post-multiplies two affine matrices.</summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="right">The right matrix.</param>
    /// <returns>A finite product or stable failure.</returns>
    public SvgMatrixOperationResult Multiply(SvgMatrix left, SvgMatrix right)
    {
        return SvgMatrixMath.Multiply(left, right);
    }

    /// <summary>Inverts an affine matrix.</summary>
    /// <param name="matrix">The matrix to invert.</param>
    /// <returns>A finite inverse or stable failure.</returns>
    public SvgMatrixOperationResult Invert(SvgMatrix matrix)
    {
        return SvgMatrixMath.Invert(matrix);
    }

    /// <summary>Transforms a point including translation.</summary>
    /// <param name="matrix">The affine matrix.</param>
    /// <param name="point">The point.</param>
    /// <returns>A transformed point or stable failure.</returns>
    public SvgPointTransformResult TransformPoint(SvgMatrix matrix, SvgPoint point)
    {
        return SvgMatrixMath.TransformPoint(matrix, point);
    }

    /// <summary>Transforms a vector without translation.</summary>
    /// <param name="matrix">The affine matrix.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>A transformed vector or stable failure.</returns>
    public SvgVectorTransformResult TransformVector(SvgMatrix matrix, SvgVector vector)
    {
        return SvgMatrixMath.TransformVector(matrix, vector);
    }

    /// <summary>Transforms a rectangle to its axis-aligned bounds.</summary>
    /// <param name="matrix">The affine matrix.</param>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>A transformed rectangle or stable failure.</returns>
    public SvgRectTransformResult TransformRect(SvgMatrix matrix, SvgRect rectangle)
    {
        return SvgMatrixMath.TransformRect(matrix, rectangle);
    }

    /// <summary>Decomposes an affine matrix.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>A finite decomposition or stable failure.</returns>
    public SvgMatrixDecompositionResult Decompose(SvgMatrix matrix)
    {
        return SvgMatrixMath.Decompose(matrix);
    }

    /// <summary>Recomposes affine components.</summary>
    /// <param name="decomposition">The components.</param>
    /// <returns>A finite matrix or stable failure.</returns>
    public SvgMatrixOperationResult Recompose(SvgMatrixDecomposition decomposition)
    {
        return SvgMatrixMath.Recompose(decomposition);
    }

    /// <summary>Interpolates between two affine matrices through decomposition.</summary>
    /// <param name="from">The starting matrix.</param>
    /// <param name="to">The ending matrix.</param>
    /// <param name="amount">The finite interpolation amount.</param>
    /// <returns>A finite interpolated matrix or stable failure.</returns>
    public SvgMatrixOperationResult Interpolate(SvgMatrix from, SvgMatrix to, double amount)
    {
        return SvgMatrixMath.Interpolate(from, to, amount);
    }
}
