namespace svg;

/// <summary>Configures bounded flattened path Boolean operations.</summary>
public class SvgPathBooleanOptions
{
    /// <summary>The positive flattening, snapping, and classification tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative adaptive subdivision limit.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive flattened, arrangement, and output edge limit.</summary>
    public int MaximumEdges;
    /// <summary>The positive arrangement edge-pair comparison limit.</summary>
    public int MaximumComparisons;
    /// <summary>The positive arrangement intersection limit.</summary>
    public int MaximumIntersections;

    /// <summary>Creates secure deterministic Boolean defaults.</summary>
    public SvgPathBooleanOptions() { Tolerance = 0.01; MaximumSubdivisionDepth = 32; MaximumEdges = 2000000; MaximumComparisons = 4000000; MaximumIntersections = 1000000; }
}

/// <summary>Contains a deterministic flattened path Boolean result.</summary>
public class SvgPathBooleanResult
{
    /// <summary>Whether the Boolean operation succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPathBooleanError Error;
    /// <summary>The closed line-normalized result path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;
    /// <summary>The reconstructed contour count.</summary>
    public int ContourCount;
    /// <summary>The retained atomic boundary edge count.</summary>
    public int EdgeCount;
    /// <summary>The arrangement edge-pair comparison count.</summary>
    public int ComparisonCount;

    /// <summary>Creates a path Boolean result.</summary>
    /// <param name="success">Whether the operation succeeded.</param><param name="error">The failure.</param><param name="pathData">The result path.</param><param name="contourCount">The contour count.</param><param name="edgeCount">The edge count.</param><param name="comparisonCount">The comparison count.</param>
    public SvgPathBooleanResult(bool success, SvgPathBooleanError error, SvgPathData? pathData, int contourCount, int edgeCount, int comparisonCount) { Success = success; Error = error; PathData = pathData; ContourCount = contourCount; EdgeCount = edgeCount; ComparisonCount = comparisonCount; }
}
