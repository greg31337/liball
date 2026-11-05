namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgGeometry"/>.</summary>
public interface ISvgGeometry
{
    /// <summary>Tests exact finite point equality.</summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The second point.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    bool EqualWithSvgPointAndSvgPoint(SvgPoint left, SvgPoint right);
    /// <summary>Tests exact finite vector equality.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    bool EqualWithSvgVectorAndSvgVector(SvgVector left, SvgVector right);
    /// <summary>Tests exact finite size equality.</summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    bool EqualWithSvgSizeAndSvgSize(SvgSize left, SvgSize right);
    /// <summary>Tests geometric rectangle equality, treating valid explicit empty rectangles as equal.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns><c>true</c> when the values are valid and geometrically equal.</returns>
    bool EqualWithSvgRectAndSvgRect(SvgRect left, SvgRect right);
    /// <summary>Tests exact finite affine-component equality.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns><c>true</c> when the values are valid and equal.</returns>
    bool EqualWithSvgMatrixAndSvgMatrix(SvgMatrix left, SvgMatrix right);
    /// <summary>Translates a point by a vector.</summary>
    /// <param name="point">The point.</param>
    /// <param name="vector">The translation vector.</param>
    /// <returns>The finite translated point or a stable failure.</returns>
    SvgPointOperationResult AddWithPointAndVector(SvgPoint point, SvgVector vector);
    /// <summary>Subtracts two points to form a vector.</summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The point to subtract.</param>
    /// <returns>The finite difference vector or a stable failure.</returns>
    SvgVectorOperationResult SubtractWithSvgPointAndSvgPoint(SvgPoint left, SvgPoint right);
    /// <summary>Adds two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The finite sum or a stable failure.</returns>
    SvgVectorOperationResult AddWithLeftAndRight(SvgVector left, SvgVector right);
    /// <summary>Subtracts two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The vector to subtract.</param>
    /// <returns>The finite difference or a stable failure.</returns>
    SvgVectorOperationResult SubtractWithSvgVectorAndSvgVector(SvgVector left, SvgVector right);
    /// <summary>Scales a vector.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="factor">The finite scale factor.</param>
    /// <returns>The finite scaled vector or a stable failure.</returns>
    SvgVectorOperationResult Scale(SvgVector vector, double factor);
    /// <summary>Computes a vector dot product.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The finite scalar result or a stable failure.</returns>
    SvgScalarOperationResult Dot(SvgVector left, SvgVector right);
    /// <summary>Computes a signed two-dimensional vector cross product.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The finite scalar result or a stable failure.</returns>
    SvgScalarOperationResult Cross(SvgVector left, SvgVector right);
    /// <summary>Computes a vector's Euclidean length.</summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The finite length or a stable failure.</returns>
    SvgScalarOperationResult Length(SvgVector vector);
    /// <summary>Computes the smallest rectangle containing both inputs.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns>The finite union or a stable failure.</returns>
    SvgRectOperationResult Union(SvgRect left, SvgRect right);
    /// <summary>Computes the closed-set intersection of two rectangles.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns>The finite intersection or a stable failure.</returns>
    SvgRectOperationResult Intersect(SvgRect left, SvgRect right);
    /// <summary>Expands horizontal and vertical rectangle sides by nonnegative amounts.</summary>
    /// <param name="rectangle">The source rectangle.</param>
    /// <param name="horizontal">The amount applied to left and right.</param>
    /// <param name="vertical">The amount applied to top and bottom.</param>
    /// <returns>The finite expanded rectangle or a stable failure.</returns>
    SvgRectOperationResult Expand(SvgRect rectangle, double horizontal, double vertical);
    /// <summary>Tests closed-set rectangle containment of a point.</summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <param name="point">The point.</param>
    /// <returns><c>true</c> when both values are valid and the nonempty rectangle contains the point.</returns>
    bool Contains(SvgRect rectangle, SvgPoint point);
    /// <summary>Post-multiplies two affine matrices.</summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="right">The right matrix.</param>
    /// <returns>A finite product or stable failure.</returns>
    SvgMatrixOperationResult Multiply(SvgMatrix left, SvgMatrix right);
    /// <summary>Inverts an affine matrix.</summary>
    /// <param name="matrix">The matrix to invert.</param>
    /// <returns>A finite inverse or stable failure.</returns>
    SvgMatrixOperationResult Invert(SvgMatrix matrix);
    /// <summary>Transforms a point including translation.</summary>
    /// <param name="matrix">The affine matrix.</param>
    /// <param name="point">The point.</param>
    /// <returns>A transformed point or stable failure.</returns>
    SvgPointTransformResult TransformPoint(SvgMatrix matrix, SvgPoint point);
    /// <summary>Transforms a vector without translation.</summary>
    /// <param name="matrix">The affine matrix.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>A transformed vector or stable failure.</returns>
    SvgVectorTransformResult TransformVector(SvgMatrix matrix, SvgVector vector);
    /// <summary>Transforms a rectangle to its axis-aligned bounds.</summary>
    /// <param name="matrix">The affine matrix.</param>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>A transformed rectangle or stable failure.</returns>
    SvgRectTransformResult TransformRect(SvgMatrix matrix, SvgRect rectangle);
    /// <summary>Decomposes an affine matrix.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>A finite decomposition or stable failure.</returns>
    SvgMatrixDecompositionResult Decompose(SvgMatrix matrix);
    /// <summary>Recomposes affine components.</summary>
    /// <param name="decomposition">The components.</param>
    /// <returns>A finite matrix or stable failure.</returns>
    SvgMatrixOperationResult Recompose(SvgMatrixDecomposition decomposition);
    /// <summary>Interpolates between two affine matrices through decomposition.</summary>
    /// <param name="from">The starting matrix.</param>
    /// <param name="to">The ending matrix.</param>
    /// <param name="amount">The finite interpolation amount.</param>
    /// <returns>A finite interpolated matrix or stable failure.</returns>
    SvgMatrixOperationResult Interpolate(SvgMatrix from, SvgMatrix to, double amount);
}
