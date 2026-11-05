namespace svg;

/// <summary>Configures bounded planar path operations.</summary>
public class SvgPlanarOptions
{
    /// <summary>The positive flattening, snapping, and boundary tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive input, flattened, and output edge limit.</summary>
    public int MaximumEdges;
    /// <summary>The positive edge-pair comparison limit.</summary>
    public int MaximumComparisons;
    /// <summary>The positive distinct intersection limit.</summary>
    public int MaximumIntersections;
    /// <summary>The unitless nonnegative tangent sine threshold.</summary>
    public double TangentTolerance;

    /// <summary>Creates secure deterministic planar-operation defaults.</summary>
    public SvgPlanarOptions() { Tolerance = 0.001; MaximumSubdivisionDepth = 32; MaximumEdges = 2000000; MaximumComparisons = 4000000; MaximumIntersections = 1000000; TangentTolerance = 0.001; }
}

/// <summary>Represents one directed planar edge with source provenance.</summary>
public class SvgPlanarEdge
{
    /// <summary>The copied finite starting point.</summary>
    public SvgPoint Start;
    /// <summary>The copied finite ending point.</summary>
    public SvgPoint End;
    /// <summary>The caller-defined source index.</summary>
    public int SourceIndex;
    /// <summary>The inclusive unit amount at the edge start.</summary>
    public double StartAmount;
    /// <summary>The inclusive unit amount at the edge end.</summary>
    public double EndAmount;

    /// <summary>Creates a directed edge with unit source amounts.</summary>
    /// <param name="start">The finite start.</param><param name="end">The finite end.</param><param name="sourceIndex">The source index.</param>
    public SvgPlanarEdge(SvgPoint start, SvgPoint end, int sourceIndex)
    {
        if (!SvgPlanarMath.ValidPoint(start) || !SvgPlanarMath.ValidPoint(end)) throw new System.ArgumentOutOfRangeException("start");
        Start = new SvgPoint(start.X, start.Y); End = new SvgPoint(end.X, end.Y); SourceIndex = sourceIndex; StartAmount = 0; EndAmount = 1;
    }

    /// <summary>Creates a directed edge with explicit source amounts.</summary>
    /// <param name="start">The finite start.</param><param name="end">The finite end.</param><param name="sourceIndex">The source index.</param><param name="startAmount">The start amount.</param><param name="endAmount">The end amount.</param>
    /// <returns>A new directed edge.</returns>
    internal static SvgPlanarEdge Create(SvgPoint start, SvgPoint end, int sourceIndex, double startAmount, double endAmount)
    {
        if (!SvgPlanarMath.ValidPoint(start) || !SvgPlanarMath.ValidPoint(end) || !SvgPlanarMath.Unit(startAmount) || !SvgPlanarMath.Unit(endAmount)) throw new System.ArgumentOutOfRangeException("start");
        SvgPlanarEdge result = new SvgPlanarEdge(start, end, sourceIndex);
        result.StartAmount = SvgFiniteMath.NormalizeZero(startAmount);
        result.EndAmount = SvgFiniteMath.NormalizeZero(endAmount);
        return result;
    }
}

/// <summary>Describes one non-adjacent self-intersection in a flattened path.</summary>
public class SvgPathSelfIntersection
{
    /// <summary>The finite intersection point.</summary>
    public SvgPoint Point;
    /// <summary>The first normalized command index, or minus one for an implicit closing edge.</summary>
    public int FirstCommandIndex;
    /// <summary>The second normalized command index, or minus one for an implicit closing edge.</summary>
    public int SecondCommandIndex;
    /// <summary>The source parameter on the first command.</summary>
    public double FirstAmount;
    /// <summary>The source parameter on the second command.</summary>
    public double SecondAmount;
    /// <summary>The crossing, tangent, or overlap classification.</summary>
    public SvgIntersectionKind Kind;
    /// <summary>The number of flattened candidates merged into this record.</summary>
    public int DuplicateCount;

    /// <summary>Creates one path self-intersection.</summary>
    /// <param name="point">The point.</param><param name="firstCommandIndex">The first command.</param><param name="secondCommandIndex">The second command.</param><param name="firstAmount">The first amount.</param><param name="secondAmount">The second amount.</param><param name="kind">The classification.</param>
    public SvgPathSelfIntersection(SvgPoint point, int firstCommandIndex, int secondCommandIndex, double firstAmount, double secondAmount, SvgIntersectionKind kind) { Point = point; FirstCommandIndex = firstCommandIndex; SecondCommandIndex = secondCommandIndex; FirstAmount = firstAmount; SecondAmount = secondAmount; Kind = kind; DuplicateCount = 0; }
}

/// <summary>Contains ordered path self-intersections.</summary>
public class SvgPathSelfIntersectionResult
{
    /// <summary>Whether discovery completed within all limits.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPlanarError Error;
    /// <summary>The ordered intersections, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgPathSelfIntersection>? Intersections;
    /// <summary>The tested flattened edge-pair count.</summary>
    public int ComparisonCount;

    /// <summary>Creates a self-intersection result.</summary>
    /// <param name="success">Whether discovery succeeded.</param><param name="error">The failure.</param><param name="intersections">The intersections.</param><param name="comparisonCount">The comparison count.</param>
    public SvgPathSelfIntersectionResult(bool success, SvgPlanarError error, System.Collections.Generic.List<SvgPathSelfIntersection>? intersections, int comparisonCount) { Success = success; Error = error; Intersections = intersections; ComparisonCount = comparisonCount; }
}

/// <summary>Contains edges split at every pairwise planar intersection.</summary>
public class SvgPlanarSplitResult
{
    /// <summary>Whether splitting completed within all limits.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPlanarError Error;
    /// <summary>The source-ordered atomic edges, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgPlanarEdge>? Edges;
    /// <summary>The tested edge-pair count.</summary>
    public int ComparisonCount;

    /// <summary>Creates a planar split result.</summary>
    /// <param name="success">Whether splitting succeeded.</param><param name="error">The failure.</param><param name="edges">The atomic edges.</param><param name="comparisonCount">The comparison count.</param>
    public SvgPlanarSplitResult(bool success, SvgPlanarError error, System.Collections.Generic.List<SvgPlanarEdge>? edges, int comparisonCount) { Success = success; Error = error; Edges = edges; ComparisonCount = comparisonCount; }
}

/// <summary>Contains winding classification over directed planar edges.</summary>
public class SvgPlanarWindingResult
{
    /// <summary>Whether classification succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPlanarError Error;
    /// <summary>The boundary, inside, or outside classification.</summary>
    public SvgFillContainmentKind Kind;
    /// <summary>Whether the closed region contains the point.</summary>
    public bool Contains;
    /// <summary>The signed winding number.</summary>
    public int WindingNumber;
    /// <summary>The half-open ray crossing count.</summary>
    public int CrossingCount;

    /// <summary>Creates a planar winding result.</summary>
    /// <param name="success">Whether classification succeeded.</param><param name="error">The failure.</param><param name="kind">The classification.</param><param name="contains">Whether the region contains the point.</param><param name="windingNumber">The winding number.</param><param name="crossingCount">The crossing count.</param>
    public SvgPlanarWindingResult(bool success, SvgPlanarError error, SvgFillContainmentKind kind, bool contains, int windingNumber, int crossingCount) { Success = success; Error = error; Kind = kind; Contains = contains; WindingNumber = windingNumber; CrossingCount = crossingCount; }
}

/// <summary>Represents one reconstructed closed directed contour.</summary>
public class SvgPlanarContour
{
    /// <summary>The ordered points including the repeated closing point.</summary>
    public System.Collections.Generic.List<SvgPoint> Points;
    /// <summary>The signed area, positive for counterclockwise mathematical orientation.</summary>
    public double SignedArea;

    /// <summary>Creates a reconstructed contour and copies its points.</summary>
    /// <param name="points">The closed ordered points.</param><param name="signedArea">The finite signed area.</param>
    public SvgPlanarContour(System.Collections.Generic.List<SvgPoint> points, double signedArea) { Points = new System.Collections.Generic.List<SvgPoint>(); int index = 0; while (index < points.Count) { Points.Add(new SvgPoint(points[index].X, points[index].Y)); index = index + 1; } SignedArea = signedArea; }
}

/// <summary>Contains deterministic contours reconstructed from directed edges.</summary>
public class SvgContourReconstructionResult
{
    /// <summary>Whether every edge formed a closed contour.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPlanarError Error;
    /// <summary>The deterministically ordered contours, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgPlanarContour>? Contours;

    /// <summary>Creates a contour reconstruction result.</summary>
    /// <param name="success">Whether reconstruction succeeded.</param><param name="error">The failure.</param><param name="contours">The contours.</param>
    public SvgContourReconstructionResult(bool success, SvgPlanarError error, System.Collections.Generic.List<SvgPlanarContour>? contours) { Success = success; Error = error; Contours = contours; }
}
