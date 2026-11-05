namespace svg;

/// <summary>Wraps one line, Bézier, or arc for generic intersection queries.</summary>
public class SvgIntersectionSegment
{
    /// <summary>The active segment kind.</summary>
    public SvgIntersectionSegmentKind Kind;
    /// <summary>The line value when kind is line.</summary>
    public SvgLineSegment? Line;
    /// <summary>The quadratic value when kind is quadratic.</summary>
    public SvgQuadraticBezier? Quadratic;
    /// <summary>The cubic value when kind is cubic.</summary>
    public SvgCubicBezier? Cubic;
    /// <summary>The arc value when kind is arc.</summary>
    public SvgArc? Arc;

    /// <summary>Creates a tagged line segment.</summary>
    /// <param name="line">The finite line.</param>
    public SvgIntersectionSegment(SvgLineSegment? line) { Kind = SvgIntersectionSegmentKind.Line; Line = line; Quadratic = null; Cubic = null; Arc = null; }
    /// <summary>Creates a tagged quadratic segment.</summary>
    /// <param name="quadratic">The finite quadratic.</param>
    /// <returns>A new tagged quadratic segment.</returns>
    internal static SvgIntersectionSegment CreateQuadratic(SvgQuadraticBezier? quadratic) { SvgIntersectionSegment result = new SvgIntersectionSegment(null); result.Kind = SvgIntersectionSegmentKind.Quadratic; result.Line = null; result.Quadratic = quadratic; return result; }
    /// <summary>Creates a tagged cubic segment.</summary>
    /// <param name="cubic">The finite cubic.</param>
    /// <returns>A new tagged cubic segment.</returns>
    internal static SvgIntersectionSegment CreateCubic(SvgCubicBezier? cubic) { SvgIntersectionSegment result = new SvgIntersectionSegment(null); result.Kind = SvgIntersectionSegmentKind.Cubic; result.Line = null; result.Cubic = cubic; return result; }
    /// <summary>Creates a tagged arc segment.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <returns>A new tagged arc segment.</returns>
    internal static SvgIntersectionSegment CreateArc(SvgArc? arc) { SvgIntersectionSegment result = new SvgIntersectionSegment(null); result.Kind = SvgIntersectionSegmentKind.Arc; result.Line = null; result.Arc = arc; return result; }
}

/// <summary>Configures bounded approximate curved-segment intersection.</summary>
public class SvgIntersectionOptions
{
    /// <summary>The positive geometric flattening and duplicate tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative maximum adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum flattened segments per input.</summary>
    public int MaximumFlattenedSegments;
    /// <summary>The positive maximum tested flattened edge pairs.</summary>
    public int MaximumComparisons;
    /// <summary>The positive maximum distinct result count.</summary>
    public int MaximumIntersections;
    /// <summary>The unitless nonnegative tangent sine threshold.</summary>
    public double TangentTolerance;

    /// <summary>Creates secure deterministic intersection defaults.</summary>
    public SvgIntersectionOptions() { Tolerance = 0.001; MaximumSubdivisionDepth = 32; MaximumFlattenedSegments = 2000000; MaximumComparisons = 4000000; MaximumIntersections = 1000000; TangentTolerance = 0.001; }
}

/// <summary>Describes one distinct parameter-paired intersection.</summary>
public class SvgIntersection
{
    /// <summary>The finite intersection point.</summary>
    public SvgPoint Point;
    /// <summary>The inclusive unit parameter on the first segment.</summary>
    public double FirstAmount;
    /// <summary>The inclusive unit parameter on the second segment.</summary>
    public double SecondAmount;
    /// <summary>The crossing, tangent, or overlap classification.</summary>
    public SvgIntersectionKind Kind;
    /// <summary>The number of additional flattened candidates merged into this result.</summary>
    public int DuplicateCount;

    /// <summary>Creates one distinct intersection.</summary>
    /// <param name="point">The intersection point.</param><param name="firstAmount">The first parameter.</param><param name="secondAmount">The second parameter.</param><param name="kind">The classification.</param><param name="duplicateCount">The merged duplicate count.</param>
    public SvgIntersection(SvgPoint point, double firstAmount, double secondAmount, SvgIntersectionKind kind, int duplicateCount) { Point = point; FirstAmount = firstAmount; SecondAmount = secondAmount; Kind = kind; DuplicateCount = duplicateCount; }
}

/// <summary>Contains ordered distinct segment intersections.</summary>
public class SvgIntersectionResult
{
    /// <summary>Whether intersection completed within all limits.</summary>
    public bool Success;
    /// <summary>The stable intersection failure.</summary>
    public SvgIntersectionError Error;
    /// <summary>The ordered distinct intersections, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgIntersection>? Intersections;
    /// <summary>The number of flattened edge-pair comparisons.</summary>
    public int ComparisonCount;

    /// <summary>Creates an intersection result.</summary>
    /// <param name="success">Whether intersection succeeded.</param><param name="error">The failure kind.</param><param name="intersections">The ordered intersections.</param><param name="comparisonCount">The comparison count.</param>
    public SvgIntersectionResult(bool success, SvgIntersectionError error, System.Collections.Generic.List<SvgIntersection>? intersections, int comparisonCount) { Success = success; Error = error; Intersections = intersections; ComparisonCount = comparisonCount; }
}
