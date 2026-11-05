namespace svg;

/// <summary>Contains a non-throwing point arithmetic result.</summary>
public class SvgPointOperationResult
{
    /// <summary>Whether the operation produced a finite point.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite point, or <c>null</c> after failure.</summary>
    public SvgPoint? Point;

    /// <summary>Creates a point arithmetic result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="point">The finite point, or <c>null</c>.</param>
    public SvgPointOperationResult(bool success, SvgGeometryError error, SvgPoint? point)
    {
        Success = success;
        Error = error;
        Point = point;
    }
}

/// <summary>Contains a non-throwing vector arithmetic result.</summary>
public class SvgVectorOperationResult
{
    /// <summary>Whether the operation produced a finite vector.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite vector, or <c>null</c> after failure.</summary>
    public SvgVector? Vector;

    /// <summary>Creates a vector arithmetic result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="vector">The finite vector, or <c>null</c>.</param>
    public SvgVectorOperationResult(bool success, SvgGeometryError error, SvgVector? vector)
    {
        Success = success;
        Error = error;
        Vector = vector;
    }
}

/// <summary>Contains a non-throwing scalar geometry result.</summary>
public class SvgScalarOperationResult
{
    /// <summary>Whether the operation produced a finite scalar.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The normalized finite value, or zero after failure.</summary>
    public double Value;

    /// <summary>Creates a scalar geometry result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The finite value, or zero after failure.</param>
    public SvgScalarOperationResult(bool success, SvgGeometryError error, double value)
    {
        Success = success;
        Error = error;
        Value = value;
    }
}

/// <summary>Contains a non-throwing rectangle operation result.</summary>
public class SvgRectOperationResult
{
    /// <summary>Whether the operation produced a finite rectangle.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite rectangle, or <c>null</c> after failure.</summary>
    public SvgRect? Rectangle;

    /// <summary>Creates a rectangle operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="rectangle">The finite rectangle, or <c>null</c>.</param>
    public SvgRectOperationResult(bool success, SvgGeometryError error, SvgRect? rectangle)
    {
        Success = success;
        Error = error;
        Rectangle = rectangle;
    }
}
