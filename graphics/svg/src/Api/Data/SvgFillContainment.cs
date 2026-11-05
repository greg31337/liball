namespace svg;

/// <summary>Configures bounded fill containment in path or transformed output coordinates.</summary>
public class SvgFillContainmentOptions
{
    /// <summary>The positive flattening and boundary-classification tolerance.</summary>
    public double Tolerance;
    /// <summary>The optional transform from path user coordinates to query coordinates.</summary>
    public SvgMatrix? Transform;
    /// <summary>The nonnegative maximum adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum flattened and implicitly closing edge count.</summary>
    public int MaximumSegments;

    /// <summary>Creates secure deterministic fill-containment defaults.</summary>
    public SvgFillContainmentOptions()
    {
        Tolerance = 0.01;
        Transform = null;
        MaximumSubdivisionDepth = 32;
        MaximumSegments = 2000000;
    }
}

/// <summary>Contains a robust fill-containment classification and crossing details.</summary>
public class SvgFillContainmentResult
{
    /// <summary>Whether containment classification succeeded.</summary>
    public bool Success;
    /// <summary>The stable containment failure.</summary>
    public SvgFillContainmentError Error;
    /// <summary>The boundary, inside, or outside classification.</summary>
    public SvgFillContainmentKind Kind;
    /// <summary>Whether the point belongs to the closed filled region, including its boundary.</summary>
    public bool Contains;
    /// <summary>The signed winding number, or zero for a boundary result.</summary>
    public int WindingNumber;
    /// <summary>The half-open positive-ray crossing count, or zero for a boundary result.</summary>
    public int CrossingCount;

    /// <summary>Creates a fill-containment result.</summary>
    /// <param name="success">Whether classification succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="kind">The containment classification.</param>
    /// <param name="contains">Whether the closed fill contains the point.</param>
    /// <param name="windingNumber">The signed winding number.</param>
    /// <param name="crossingCount">The positive-ray crossing count.</param>
    public SvgFillContainmentResult(bool success, SvgFillContainmentError error, SvgFillContainmentKind kind, bool contains, int windingNumber, int crossingCount)
    {
        Success = success;
        Error = error;
        Kind = kind;
        Contains = contains;
        WindingNumber = windingNumber;
        CrossingCount = crossingCount;
    }
}
