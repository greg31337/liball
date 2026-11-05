namespace svg;

/// <summary>Provides bounded self-intersection and planar graph primitives.</summary>
public class SvgPlanarGeometry : ISvgPlanarGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPlanarGeometry()
    {
    }

    /// <summary>Finds non-adjacent self-intersections in a path using deterministic defaults.</summary>
    /// <param name="path">The valid path.</param>
    /// <returns>The ordered self-intersections or a stable failure.</returns>
    public SvgPathSelfIntersectionResult FindSelfIntersectionsWithPath(SvgPathData path) { return new SvgPlanarGeometry().FindSelfIntersectionsWithPathAndOptions(path, new SvgPlanarOptions()); }

    /// <summary>Finds non-adjacent self-intersections with explicit tolerance and limits.</summary>
    /// <param name="path">The valid path.</param><param name="options">The bounded planar options.</param>
    /// <returns>The ordered self-intersections or a stable failure.</returns>
    public SvgPathSelfIntersectionResult FindSelfIntersectionsWithPathAndOptions(SvgPathData? path, SvgPlanarOptions options)
    {
        if (path == null || !new SvgPlanarGeometry().Valid(options)) return new SvgPlanarGeometry().SelfFailure(SvgPlanarError.InvalidArgument, 0);
        SvgPathNormalizeResult normalized = path.Normalize(); if (!normalized.Success) return new SvgPlanarGeometry().SelfFailure(new SvgPlanarGeometry().Map(normalized.Error), 0);
        SvgPlanarFlattenResult flattened = new SvgPlanarPathFlattener(normalized.PathData, options).Run(); if (!flattened.Success) return new SvgPlanarGeometry().SelfFailure(flattened.Error, 0);
        return new SvgSelfIntersectionState(flattened.Edges, options).Run();
    }

    /// <summary>Flattens a path and splits its directed edges using deterministic defaults.</summary>
    /// <param name="path">The valid path whose open fill subpaths are implicitly closed.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    public SvgPlanarSplitResult SplitPathWithPath(SvgPathData path) { return new SvgPlanarGeometry().SplitPathWithPathAndOptions(path, new SvgPlanarOptions()); }

    /// <summary>Flattens a path and splits its directed edges at every planar intersection.</summary>
    /// <param name="path">The valid path whose open fill subpaths are implicitly closed.</param><param name="options">The bounded planar options.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    public SvgPlanarSplitResult SplitPathWithPathAndOptions(SvgPathData? path, SvgPlanarOptions options)
    {
        if (path == null || !new SvgPlanarGeometry().Valid(options)) return new SvgPlanarGeometry().SplitFailure(SvgPlanarError.InvalidArgument, 0);
        SvgPathNormalizeResult normalized = path.Normalize(); if (!normalized.Success) return new SvgPlanarGeometry().SplitFailure(new SvgPlanarGeometry().Map(normalized.Error), 0);
        SvgPlanarFlattenResult flattened = new SvgPlanarPathFlattener(normalized.PathData, options).Run(); if (!flattened.Success) return new SvgPlanarGeometry().SplitFailure(flattened.Error, 0);
        System.Collections.Generic.List<SvgPlanarEdge> edges = new System.Collections.Generic.List<SvgPlanarEdge>(); int index = 0; while (index < (flattened.Edges ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count) { edges.Add(flattened.Edges[index].Edge); index = index + 1; } return new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(edges, options);
    }

    /// <summary>Splits directed line edges using deterministic defaults.</summary>
    /// <param name="edges">The finite directed edges.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    public SvgPlanarSplitResult SplitEdgesWithEdges(System.Collections.Generic.List<SvgPlanarEdge> edges) { return new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(edges, new SvgPlanarOptions()); }

    /// <summary>Splits directed line edges at all pairwise crossings and overlap endpoints.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="options">The bounded planar options.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    public SvgPlanarSplitResult SplitEdgesWithEdgesAndOptions(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPlanarOptions options)
    {
        if (!new SvgPlanarGeometry().ValidEdges(edges, options)) return new SvgPlanarGeometry().SplitFailure(SvgPlanarError.InvalidArgument, 0);
        return new SvgPlanarSplitState(edges, options).Run();
    }

    /// <summary>Classifies a point against directed edges using deterministic defaults.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="point">The finite query point.</param><param name="fillRule">The winding rule.</param>
    /// <returns>The winding classification or a stable failure.</returns>
    public SvgPlanarWindingResult ClassifyWindingWithEdgesAndPointAndFillRule(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPoint point, SvgFillRule fillRule) { return new SvgPlanarGeometry().ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(edges, point, fillRule, new SvgPlanarOptions()); }

    /// <summary>Classifies a point against directed planar edges.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="point">The finite query point.</param><param name="fillRule">The winding rule.</param><param name="options">The bounded planar options.</param>
    /// <returns>The winding classification or a stable failure.</returns>
    public SvgPlanarWindingResult ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPoint point, SvgFillRule fillRule, SvgPlanarOptions options)
    {
        if (!new SvgPlanarGeometry().ValidEdges(edges, options) || !SvgPlanarMath.ValidPoint(point) || fillRule != SvgFillRule.Nonzero && fillRule != SvgFillRule.EvenOdd) return new SvgPlanarGeometry().WindingFailure(SvgPlanarError.InvalidArgument);
        int index = 0; while (index < edges.Count) { if (SvgPlanarMath.OnEdge(point, edges[index], options.Tolerance)) return new SvgPlanarWindingResult(true, SvgPlanarError.None, SvgFillContainmentKind.Boundary, true, 0, 0); index = index + 1; }
        int winding = 0; int crossings = 0; index = 0; while (index < edges.Count) { SvgPlanarEdge edge = edges[index]; double scale = SvgPlanarMath.Scale(point, edge); double startX = edge.Start.X / scale; double startY = edge.Start.Y / scale; double endX = edge.End.X / scale; double endY = edge.End.Y / scale; double pointX = point.X / scale; double pointY = point.Y / scale; double side = (endX - startX) * (pointY - startY) - (pointX - startX) * (endY - startY); if (startY <= pointY && endY > pointY && side > 0) { winding = winding + 1; crossings = crossings + 1; } else if (endY <= pointY && startY > pointY && side < 0) { winding = winding - 1; crossings = crossings + 1; } index = index + 1; }
        bool contains = fillRule == SvgFillRule.Nonzero ? winding != 0 : crossings % 2 != 0; return new SvgPlanarWindingResult(true, SvgPlanarError.None, contains ? SvgFillContainmentKind.Inside : SvgFillContainmentKind.Outside, contains, winding, crossings);
    }

    /// <summary>Reconstructs closed contours from directed, endpoint-connected edges.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="options">The snapping and edge limits.</param>
    /// <returns>Deterministically ordered closed contours or a stable failure.</returns>
    public SvgContourReconstructionResult ReconstructContoursWithEdgesAndOptions(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPlanarOptions options)
    {
        if (!new SvgPlanarGeometry().ValidEdges(edges, options)) return new SvgPlanarGeometry().ContourFailure(SvgPlanarError.InvalidArgument);
        return new SvgContourReconstructionState(edges, options).Run();
    }

    /// <summary>Reconstructs closed contours using deterministic defaults.</summary>
    /// <param name="edges">The finite directed edges.</param>
    /// <returns>Deterministically ordered closed contours or a stable failure.</returns>
    public SvgContourReconstructionResult ReconstructContoursWithEdges(System.Collections.Generic.List<SvgPlanarEdge> edges) { return new SvgPlanarGeometry().ReconstructContoursWithEdgesAndOptions(edges, new SvgPlanarOptions()); }

    internal SvgPathSelfIntersectionResult SelfFailure(SvgPlanarError error, int comparisons) { return new SvgPathSelfIntersectionResult(false, error, null, comparisons); }
    internal SvgPlanarSplitResult SplitFailure(SvgPlanarError error, int comparisons) { return new SvgPlanarSplitResult(false, error, null, comparisons); }
    internal SvgPlanarWindingResult WindingFailure(SvgPlanarError error) { return new SvgPlanarWindingResult(false, error, SvgFillContainmentKind.Outside, false, 0, 0); }
    internal SvgContourReconstructionResult ContourFailure(SvgPlanarError error) { return new SvgContourReconstructionResult(false, error, null); }
    internal SvgIntersectionOptions IntersectionOptions(SvgPlanarOptions options) { SvgIntersectionOptions result = new SvgIntersectionOptions(); result.Tolerance = options.Tolerance; result.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth; result.MaximumFlattenedSegments = options.MaximumEdges; result.MaximumComparisons = options.MaximumComparisons; result.MaximumIntersections = options.MaximumIntersections; return result; }
bool Valid(SvgPlanarOptions options) { return options != null && SvgFiniteMath.IsFinite(options.Tolerance) && options.Tolerance > 0 && options.MaximumSubdivisionDepth >= 0 && options.MaximumEdges > 0 && options.MaximumComparisons > 0 && options.MaximumIntersections > 0 && SvgFiniteMath.IsFinite(options.TangentTolerance) && options.TangentTolerance >= 0 && options.TangentTolerance <= 1; }
bool ValidEdges(System.Collections.Generic.List<SvgPlanarEdge>? edges, SvgPlanarOptions options) { if (edges == null || !new SvgPlanarGeometry().Valid(options) || edges.Count > options.MaximumEdges) return false; int index = 0; while (index < edges.Count) { SvgPlanarEdge edge = edges[index]; if (edge == null || !SvgPlanarMath.ValidPoint(edge.Start) || !SvgPlanarMath.ValidPoint(edge.End) || !SvgPlanarMath.Unit(edge.StartAmount) || !SvgPlanarMath.Unit(edge.EndAmount)) return false; index = index + 1; } return true; }
SvgPlanarError Map(SvgPathNormalizeError error) { if (error == SvgPathNormalizeError.InvalidPath) return SvgPlanarError.InvalidPath; if (error == SvgPathNormalizeError.NonFiniteResult) return SvgPlanarError.NonFiniteResult; if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgPlanarError.EdgeLimitExceeded; return SvgPlanarError.InvalidArgument; }
}
