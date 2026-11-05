namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPlanarGeometry"/>.</summary>
public interface ISvgPlanarGeometry
{
    /// <summary>Finds non-adjacent self-intersections in a path using deterministic defaults.</summary>
    /// <param name="path">The valid path.</param>
    /// <returns>The ordered self-intersections or a stable failure.</returns>
    SvgPathSelfIntersectionResult FindSelfIntersectionsWithPath(SvgPathData path);
    /// <summary>Finds non-adjacent self-intersections with explicit tolerance and limits.</summary>
    /// <param name="path">The valid path.</param><param name="options">The bounded planar options.</param>
    /// <returns>The ordered self-intersections or a stable failure.</returns>
    SvgPathSelfIntersectionResult FindSelfIntersectionsWithPathAndOptions(SvgPathData? path, SvgPlanarOptions options);
    /// <summary>Flattens a path and splits its directed edges using deterministic defaults.</summary>
    /// <param name="path">The valid path whose open fill subpaths are implicitly closed.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    SvgPlanarSplitResult SplitPathWithPath(SvgPathData path);
    /// <summary>Flattens a path and splits its directed edges at every planar intersection.</summary>
    /// <param name="path">The valid path whose open fill subpaths are implicitly closed.</param><param name="options">The bounded planar options.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    SvgPlanarSplitResult SplitPathWithPathAndOptions(SvgPathData? path, SvgPlanarOptions options);
    /// <summary>Splits directed line edges using deterministic defaults.</summary>
    /// <param name="edges">The finite directed edges.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    SvgPlanarSplitResult SplitEdgesWithEdges(System.Collections.Generic.List<SvgPlanarEdge> edges);
    /// <summary>Splits directed line edges at all pairwise crossings and overlap endpoints.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="options">The bounded planar options.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    SvgPlanarSplitResult SplitEdgesWithEdgesAndOptions(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPlanarOptions options);
    /// <summary>Classifies a point against directed edges using deterministic defaults.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="point">The finite query point.</param><param name="fillRule">The winding rule.</param>
    /// <returns>The winding classification or a stable failure.</returns>
    SvgPlanarWindingResult ClassifyWindingWithEdgesAndPointAndFillRule(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPoint point, SvgFillRule fillRule);
    /// <summary>Classifies a point against directed planar edges.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="point">The finite query point.</param><param name="fillRule">The winding rule.</param><param name="options">The bounded planar options.</param>
    /// <returns>The winding classification or a stable failure.</returns>
    SvgPlanarWindingResult ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPoint point, SvgFillRule fillRule, SvgPlanarOptions options);
    /// <summary>Reconstructs closed contours from directed, endpoint-connected edges.</summary>
    /// <param name="edges">The finite directed edges.</param><param name="options">The snapping and edge limits.</param>
    /// <returns>Deterministically ordered closed contours or a stable failure.</returns>
    SvgContourReconstructionResult ReconstructContoursWithEdgesAndOptions(System.Collections.Generic.List<SvgPlanarEdge> edges, SvgPlanarOptions options);
    /// <summary>Reconstructs closed contours using deterministic defaults.</summary>
    /// <param name="edges">The finite directed edges.</param>
    /// <returns>Deterministically ordered closed contours or a stable failure.</returns>
    SvgContourReconstructionResult ReconstructContoursWithEdges(System.Collections.Generic.List<SvgPlanarEdge> edges);
}
