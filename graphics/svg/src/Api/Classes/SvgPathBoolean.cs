namespace svg;

/// <summary>Provides bounded union, intersection, difference, xor, and clipping for filled paths.</summary>
public class SvgPathBoolean : ISvgPathBoolean
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathBoolean()
    {
    }

    /// <summary>Applies a nonzero-filled Boolean operation with deterministic defaults.</summary>
    /// <param name="first">The first filled path.</param><param name="second">The second filled path.</param><param name="operation">The Boolean operation.</param>
    /// <returns>The deterministic flattened result or a stable failure.</returns>
    public SvgPathBooleanResult ApplyWithFirstAndSecondAndOperation(SvgPathData first, SvgPathData second, SvgPathBooleanOperation operation) { return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(first, second, operation, SvgFillRule.Nonzero, SvgFillRule.Nonzero, new SvgPathBooleanOptions()); }

    /// <summary>Applies a filled-path Boolean operation with explicit rules and limits.</summary>
    /// <param name="first">The first filled path.</param><param name="second">The second filled path.</param><param name="operation">The Boolean operation.</param><param name="firstRule">The first fill rule.</param><param name="secondRule">The second fill rule.</param><param name="options">The bounded Boolean options.</param>
    /// <returns>The deterministic flattened result or a stable failure.</returns>
    public SvgPathBooleanResult ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(SvgPathData first, SvgPathData second, SvgPathBooleanOperation operation, SvgFillRule firstRule, SvgFillRule secondRule, SvgPathBooleanOptions options)
    {
        if (!new SvgPathBoolean().Valid(first, second, operation, firstRule, secondRule, options)) return new SvgPathBoolean().Failure(SvgPathBooleanError.InvalidArgument, 0);
        SvgPathNormalizeResult firstNormalized = first.Normalize(); if (!firstNormalized.Success) return new SvgPathBoolean().Failure(new SvgPathBoolean().MapWithSvgPathNormalizeError(firstNormalized.Error), 0);
        SvgPathNormalizeResult secondNormalized = second.Normalize(); if (!secondNormalized.Success) return new SvgPathBoolean().Failure(new SvgPathBoolean().MapWithSvgPathNormalizeError(secondNormalized.Error), 0);
        SvgPlanarOptions planar = new SvgPathBoolean().Planar(options); SvgPlanarFlattenResult firstFlat = new SvgPlanarPathFlattener(firstNormalized.PathData, planar).Run(); if (!firstFlat.Success) return new SvgPathBoolean().Failure(new SvgPathBoolean().MapWithSvgPlanarError(firstFlat.Error), 0); SvgPlanarFlattenResult secondFlat = new SvgPlanarPathFlattener(secondNormalized.PathData, planar).Run(); if (!secondFlat.Success) return new SvgPathBoolean().Failure(new SvgPathBoolean().MapWithSvgPlanarError(secondFlat.Error), 0);
        return new SvgPathBooleanState(firstFlat, secondFlat, operation, firstRule, secondRule, options, planar).Run();
    }

    /// <summary>Computes a nonzero-filled union with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The second path.</param>
    /// <returns>The union result.</returns>
    public SvgPathBooleanResult Union(SvgPathData first, SvgPathData second) { return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(first, second, SvgPathBooleanOperation.Union); }

    /// <summary>Computes a nonzero-filled intersection with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The second path.</param>
    /// <returns>The intersection result.</returns>
    public SvgPathBooleanResult Intersection(SvgPathData first, SvgPathData second) { return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(first, second, SvgPathBooleanOperation.Intersection); }

    /// <summary>Computes a nonzero-filled first-minus-second difference with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The subtracted path.</param>
    /// <returns>The difference result.</returns>
    public SvgPathBooleanResult Difference(SvgPathData first, SvgPathData second) { return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(first, second, SvgPathBooleanOperation.Difference); }

    /// <summary>Computes a nonzero-filled exclusive-or with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The second path.</param>
    /// <returns>The exclusive-or result.</returns>
    public SvgPathBooleanResult ExclusiveOr(SvgPathData first, SvgPathData second) { return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(first, second, SvgPathBooleanOperation.Xor); }

    /// <summary>Clips a subject path to a clip path with explicit fill rules and limits.</summary>
    /// <param name="subject">The subject geometry.</param><param name="clipPath">The clipping geometry.</param><param name="subjectRule">The subject fill rule.</param><param name="clipRule">The clip fill rule.</param><param name="options">The bounded Boolean options.</param>
    /// <returns>The clipped intersection result.</returns>
    public SvgPathBooleanResult Clip(SvgPathData subject, SvgPathData clipPath, SvgFillRule subjectRule, SvgFillRule clipRule, SvgPathBooleanOptions options) { return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(subject, clipPath, SvgPathBooleanOperation.Intersection, subjectRule, clipRule, options); }

    internal SvgPathBooleanResult Failure(SvgPathBooleanError error, int comparisons) { return new SvgPathBooleanResult(false, error, null, 0, 0, comparisons); }
    internal SvgPlanarOptions Planar(SvgPathBooleanOptions options) { SvgPlanarOptions result = new SvgPlanarOptions(); result.Tolerance = options.Tolerance; result.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth; result.MaximumEdges = options.MaximumEdges; result.MaximumComparisons = options.MaximumComparisons; result.MaximumIntersections = options.MaximumIntersections; return result; }
    internal SvgPathBooleanError MapWithSvgPlanarError(SvgPlanarError error) { if (error == SvgPlanarError.InvalidArgument) return SvgPathBooleanError.InvalidArgument; if (error == SvgPlanarError.InvalidPath) return SvgPathBooleanError.InvalidPath; if (error == SvgPlanarError.NonFiniteResult) return SvgPathBooleanError.NonFiniteResult; if (error == SvgPlanarError.EdgeLimitExceeded) return SvgPathBooleanError.EdgeLimitExceeded; if (error == SvgPlanarError.SubdivisionLimitExceeded) return SvgPathBooleanError.SubdivisionLimitExceeded; if (error == SvgPlanarError.ComparisonLimitExceeded) return SvgPathBooleanError.ComparisonLimitExceeded; if (error == SvgPlanarError.IntersectionLimitExceeded) return SvgPathBooleanError.IntersectionLimitExceeded; return SvgPathBooleanError.ReconstructionFailed; }
bool Valid(SvgPathData? first, SvgPathData? second, SvgPathBooleanOperation operation, SvgFillRule firstRule, SvgFillRule secondRule, SvgPathBooleanOptions? options) { if (first == null || second == null || options == null) return false; if (operation < SvgPathBooleanOperation.Union || operation > SvgPathBooleanOperation.Xor) return false; if (firstRule != SvgFillRule.Nonzero && firstRule != SvgFillRule.EvenOdd || secondRule != SvgFillRule.Nonzero && secondRule != SvgFillRule.EvenOdd) return false; return SvgFiniteMath.IsFinite(options.Tolerance) && options.Tolerance > 0 && options.MaximumSubdivisionDepth >= 0 && options.MaximumEdges > 0 && options.MaximumComparisons > 0 && options.MaximumIntersections > 0; }
SvgPathBooleanError MapWithSvgPathNormalizeError(SvgPathNormalizeError error) { if (error == SvgPathNormalizeError.InvalidPath) return SvgPathBooleanError.InvalidPath; if (error == SvgPathNormalizeError.NonFiniteResult) return SvgPathBooleanError.NonFiniteResult; if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgPathBooleanError.EdgeLimitExceeded; return SvgPathBooleanError.InvalidArgument; }
}
