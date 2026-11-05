namespace svg;

/// <summary>Represents a finite two-dimensional point.</summary>
public class SvgPoint
{
    /// <summary>The horizontal coordinate.</summary>
    public double X;
    /// <summary>The vertical coordinate.</summary>
    public double Y;

    /// <summary>Creates a finite point.</summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a coordinate is not finite.</exception>
    public SvgPoint(double x, double y)
    {
        SvgGeometryValidation.RequireFinite(x, "x");
        SvgGeometryValidation.RequireFinite(y, "y");
        X = SvgFiniteMath.NormalizeZero(x);
        Y = SvgFiniteMath.NormalizeZero(y);
    }

    /// <summary>Tests exact finite coordinate equality.</summary>
    /// <param name="other">The other point.</param>
    /// <returns><c>true</c> when both points are valid and have equal coordinates.</returns>
    internal bool ValueEquals(SvgPoint other)
    {
        return new SvgGeometry().EqualWithSvgPointAndSvgPoint(this, other);
    }

    /// <summary>Translates this point by a vector.</summary>
    /// <param name="vector">The translation vector.</param>
    /// <returns>The finite translated point or a stable failure.</returns>
    internal SvgPointOperationResult Add(SvgVector vector)
    {
        return new SvgGeometry().AddWithPointAndVector(this, vector);
    }

    /// <summary>Subtracts another point to form a vector.</summary>
    /// <param name="other">The point to subtract.</param>
    /// <returns>The finite difference vector or a stable failure.</returns>
    internal SvgVectorOperationResult Subtract(SvgPoint other)
    {
        return new SvgGeometry().SubtractWithSvgPointAndSvgPoint(this, other);
    }
}

/// <summary>Represents a finite two-dimensional vector.</summary>
public class SvgVector
{
    /// <summary>The horizontal component.</summary>
    public double X;
    /// <summary>The vertical component.</summary>
    public double Y;

    /// <summary>Creates a finite vector.</summary>
    /// <param name="x">The horizontal component.</param>
    /// <param name="y">The vertical component.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a component is not finite.</exception>
    public SvgVector(double x, double y)
    {
        SvgGeometryValidation.RequireFinite(x, "x");
        SvgGeometryValidation.RequireFinite(y, "y");
        X = SvgFiniteMath.NormalizeZero(x);
        Y = SvgFiniteMath.NormalizeZero(y);
    }

    /// <summary>Tests exact finite component equality.</summary>
    /// <param name="other">The other vector.</param>
    /// <returns><c>true</c> when both vectors are valid and have equal components.</returns>
    internal bool ValueEquals(SvgVector other)
    {
        return new SvgGeometry().EqualWithSvgVectorAndSvgVector(this, other);
    }

    /// <summary>Adds another vector.</summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The finite sum or a stable failure.</returns>
    internal SvgVectorOperationResult Add(SvgVector other)
    {
        return new SvgGeometry().AddWithLeftAndRight(this, other);
    }

    /// <summary>Subtracts another vector.</summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The finite difference or a stable failure.</returns>
    internal SvgVectorOperationResult Subtract(SvgVector other)
    {
        return new SvgGeometry().SubtractWithSvgVectorAndSvgVector(this, other);
    }

    /// <summary>Multiplies both components by a scalar.</summary>
    /// <param name="factor">The finite scale factor.</param>
    /// <returns>The finite scaled vector or a stable failure.</returns>
    internal SvgVectorOperationResult Scale(double factor)
    {
        return new SvgGeometry().Scale(this, factor);
    }

    /// <summary>Computes the dot product with another vector.</summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The finite scalar result or a stable failure.</returns>
    internal SvgScalarOperationResult Dot(SvgVector other)
    {
        return new SvgGeometry().Dot(this, other);
    }

    /// <summary>Computes the two-dimensional signed cross product with another vector.</summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The finite scalar result or a stable failure.</returns>
    internal SvgScalarOperationResult Cross(SvgVector other)
    {
        return new SvgGeometry().Cross(this, other);
    }

    /// <summary>Computes the Euclidean vector length without avoidable intermediate overflow.</summary>
    /// <returns>The finite length or a stable failure.</returns>
    internal SvgScalarOperationResult Length()
    {
        return new SvgGeometry().Length(this);
    }
}

/// <summary>Represents finite nonnegative two-dimensional dimensions.</summary>
public class SvgSize
{
    /// <summary>The nonnegative width.</summary>
    public double Width;
    /// <summary>The nonnegative height.</summary>
    public double Height;

    /// <summary>Creates a finite nonnegative size.</summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a negative or non-finite dimension.</exception>
    public SvgSize(double width, double height)
    {
        SvgGeometryValidation.RequireNonnegative(width, "width");
        SvgGeometryValidation.RequireNonnegative(height, "height");
        Width = SvgFiniteMath.NormalizeZero(width);
        Height = SvgFiniteMath.NormalizeZero(height);
    }

    /// <summary>Tests exact finite dimension equality.</summary>
    /// <param name="other">The other size.</param>
    /// <returns><c>true</c> when both sizes are valid and have equal dimensions.</returns>
    internal bool ValueEquals(SvgSize other)
    {
        return new SvgGeometry().EqualWithSvgSizeAndSvgSize(this, other);
    }
}

/// <summary>Represents a finite axis-aligned rectangle with an explicit empty state.</summary>
public class SvgRect
{
    /// <summary>The minimum horizontal coordinate.</summary>
    public double X;
    /// <summary>The minimum vertical coordinate.</summary>
    public double Y;
    /// <summary>The nonnegative width.</summary>
    public double Width;
    /// <summary>The nonnegative height.</summary>
    public double Height;
    /// <summary>Whether the rectangle represents no points.</summary>
    public bool IsEmpty;

    /// <summary>Creates a finite nonempty-state rectangle.</summary>
    /// <param name="x">The minimum horizontal coordinate.</param>
    /// <param name="y">The minimum vertical coordinate.</param>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for non-finite coordinates or negative dimensions.</exception>
    public SvgRect(double x, double y, double width, double height)
    {
        SvgGeometryValidation.RequireFinite(x, "x");
        SvgGeometryValidation.RequireFinite(y, "y");
        SvgGeometryValidation.RequireNonnegative(width, "width");
        SvgGeometryValidation.RequireNonnegative(height, "height");
        X = SvgFiniteMath.NormalizeZero(x);
        Y = SvgFiniteMath.NormalizeZero(y);
        Width = SvgFiniteMath.NormalizeZero(width);
        Height = SvgFiniteMath.NormalizeZero(height);
        IsEmpty = false;
    }

    /// <summary>Creates a finite nonempty-state rectangle.</summary>
    /// <param name="x">The minimum horizontal coordinate.</param>
    /// <param name="y">The minimum vertical coordinate.</param>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <returns>A new nonempty-state rectangle.</returns>
    internal static SvgRect Create(double x, double y, double width, double height)
    {
        return new SvgRect(x, y, width, height);
    }

    /// <summary>Creates a finite rectangle with an explicit empty state.</summary>
    /// <param name="x">The horizontal anchor coordinate.</param>
    /// <param name="y">The vertical anchor coordinate.</param>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <param name="isEmpty">Whether the rectangle represents no points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for non-finite coordinates or negative dimensions.</exception>
    /// <returns>A new rectangle with the requested empty state.</returns>
    internal static SvgRect Create(double x, double y, double width, double height, bool isEmpty)
    {
        SvgRect result = new SvgRect(x, y, width, height);
        result.IsEmpty = isEmpty;
        return result;
    }

    /// <summary>Tests geometric equality, treating every valid explicit empty rectangle as equal.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><c>true</c> when both rectangles are valid and geometrically equal.</returns>
    internal bool ValueEquals(SvgRect other)
    {
        return new SvgGeometry().EqualWithSvgRectAndSvgRect(this, other);
    }

    /// <summary>Computes the smallest rectangle containing this rectangle and another.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The finite union or a stable failure.</returns>
    internal SvgRectOperationResult Union(SvgRect other)
    {
        return new SvgGeometry().Union(this, other);
    }

    /// <summary>Computes the closed-set intersection with another rectangle.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The finite intersection or a stable failure.</returns>
    internal SvgRectOperationResult Intersect(SvgRect other)
    {
        return new SvgGeometry().Intersect(this, other);
    }

    /// <summary>Expands every side by one nonnegative amount.</summary>
    /// <param name="amount">The amount applied to every side.</param>
    /// <returns>The finite expanded rectangle or a stable failure.</returns>
    internal SvgRectOperationResult Expand(double amount)
    {
        return new SvgGeometry().Expand(this, amount, amount);
    }

    /// <summary>Expands horizontal and vertical sides by nonnegative amounts.</summary>
    /// <param name="horizontal">The amount applied to the left and right sides.</param>
    /// <param name="vertical">The amount applied to the top and bottom sides.</param>
    /// <returns>The finite expanded rectangle or a stable failure.</returns>
    internal SvgRectOperationResult ExpandAxes(double horizontal, double vertical)
    {
        return new SvgGeometry().Expand(this, horizontal, vertical);
    }

    /// <summary>Tests closed-set containment of a finite point.</summary>
    /// <param name="point">The point to test.</param>
    /// <returns><c>true</c> when this valid nonempty rectangle contains the point.</returns>
    internal bool Contains(SvgPoint point)
    {
        return new SvgGeometry().Contains(this, point);
    }
}

/// <summary>Contains a non-throwing affine matrix operation result.</summary>
public class SvgMatrixOperationResult
{
    /// <summary>Indicates whether the operation produced a finite matrix.</summary>
    public bool Success;
    /// <summary>The geometry failure kind.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite matrix, or <c>null</c> after failure.</summary>
    public SvgMatrix? Matrix;

    /// <summary>Creates an affine matrix operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="matrix">The finite matrix, or <c>null</c>.</param>
    public SvgMatrixOperationResult(bool success, SvgGeometryError error, SvgMatrix? matrix)
    {
        Success = success;
        Error = error;
        Matrix = matrix;
    }
}

/// <summary>Contains a non-throwing transformed point result.</summary>
public class SvgPointTransformResult
{
    /// <summary>Indicates whether transformation produced a finite point.</summary>
    public bool Success;
    /// <summary>The geometry failure kind.</summary>
    public SvgGeometryError Error;
    /// <summary>The transformed point, or <c>null</c>.</summary>
    public SvgPoint? Point;

    /// <summary>Creates a transformed point result.</summary>
    /// <param name="success">Whether transformation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="point">The finite point, or <c>null</c>.</param>
    public SvgPointTransformResult(bool success, SvgGeometryError error, SvgPoint? point)
    {
        Success = success;
        Error = error;
        Point = point;
    }
}

/// <summary>Contains a non-throwing transformed vector result.</summary>
public class SvgVectorTransformResult
{
    /// <summary>Indicates whether transformation produced a finite vector.</summary>
    public bool Success;
    /// <summary>The geometry failure kind.</summary>
    public SvgGeometryError Error;
    /// <summary>The transformed vector, or <c>null</c>.</summary>
    public SvgVector? Vector;

    /// <summary>Creates a transformed vector result.</summary>
    /// <param name="success">Whether transformation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="vector">The finite vector, or <c>null</c>.</param>
    public SvgVectorTransformResult(bool success, SvgGeometryError error, SvgVector? vector)
    {
        Success = success;
        Error = error;
        Vector = vector;
    }
}

/// <summary>Contains a non-throwing transformed rectangle result.</summary>
public class SvgRectTransformResult
{
    /// <summary>Indicates whether transformation produced a finite rectangle.</summary>
    public bool Success;
    /// <summary>The geometry failure kind.</summary>
    public SvgGeometryError Error;
    /// <summary>The transformed rectangle, or <c>null</c>.</summary>
    public SvgRect? Rectangle;

    /// <summary>Creates a transformed rectangle result.</summary>
    /// <param name="success">Whether transformation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="rectangle">The finite rectangle, or <c>null</c>.</param>
    public SvgRectTransformResult(bool success, SvgGeometryError error, SvgRect? rectangle)
    {
        Success = success;
        Error = error;
        Rectangle = rectangle;
    }
}

/// <summary>Represents a deterministic affine translation, rotation, scale, and skew decomposition.</summary>
public class SvgMatrixDecomposition
{
    /// <summary>The horizontal translation.</summary>
    public double TranslateX;
    /// <summary>The vertical translation.</summary>
    public double TranslateY;
    /// <summary>The clockwise rotation in radians.</summary>
    public double Rotation;
    /// <summary>The horizontal scale.</summary>
    public double ScaleX;
    /// <summary>The signed vertical scale, preserving reflection.</summary>
    public double ScaleY;
    /// <summary>The horizontal skew angle in radians.</summary>
    public double SkewX;

    /// <summary>Creates finite affine decomposition components.</summary>
    /// <param name="translateX">The horizontal translation.</param>
    /// <param name="translateY">The vertical translation.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scaleX">The horizontal scale.</param>
    /// <param name="scaleY">The signed vertical scale.</param>
    /// <param name="skewX">The horizontal skew angle in radians.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a non-finite component.</exception>
    public SvgMatrixDecomposition(double translateX, double translateY, double rotation, double scaleX, double scaleY, double skewX)
    {
        SvgGeometryValidation.RequireFinite(translateX, "translateX");
        SvgGeometryValidation.RequireFinite(translateY, "translateY");
        SvgGeometryValidation.RequireFinite(rotation, "rotation");
        SvgGeometryValidation.RequireFinite(scaleX, "scaleX");
        SvgGeometryValidation.RequireFinite(scaleY, "scaleY");
        SvgGeometryValidation.RequireFinite(skewX, "skewX");
        TranslateX = SvgFiniteMath.NormalizeZero(translateX);
        TranslateY = SvgFiniteMath.NormalizeZero(translateY);
        Rotation = SvgFiniteMath.NormalizeZero(rotation);
        ScaleX = SvgFiniteMath.NormalizeZero(scaleX);
        ScaleY = SvgFiniteMath.NormalizeZero(scaleY);
        SkewX = SvgFiniteMath.NormalizeZero(skewX);
    }

    /// <summary>Recomposes these affine components into a finite matrix.</summary>
    /// <returns>A finite matrix or stable failure.</returns>
    internal SvgMatrixOperationResult Recompose()
    {
        return SvgMatrixMath.Recompose(this);
    }
}

/// <summary>Contains a non-throwing affine decomposition result.</summary>
public class SvgMatrixDecompositionResult
{
    /// <summary>Indicates whether decomposition succeeded.</summary>
    public bool Success;
    /// <summary>The geometry failure kind.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite decomposition, or <c>null</c>.</summary>
    public SvgMatrixDecomposition? Decomposition;

    /// <summary>Creates an affine decomposition result.</summary>
    /// <param name="success">Whether decomposition succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="decomposition">The finite decomposition, or <c>null</c>.</param>
    public SvgMatrixDecompositionResult(bool success, SvgGeometryError error, SvgMatrixDecomposition? decomposition)
    {
        Success = success;
        Error = error;
        Decomposition = decomposition;
    }
}
