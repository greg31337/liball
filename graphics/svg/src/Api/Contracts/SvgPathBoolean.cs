namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathBoolean"/>.</summary>
public interface ISvgPathBoolean
{
    /// <summary>Applies a nonzero-filled Boolean operation with deterministic defaults.</summary>
    /// <param name="first">The first filled path.</param><param name="second">The second filled path.</param><param name="operation">The Boolean operation.</param>
    /// <returns>The deterministic flattened result or a stable failure.</returns>
    SvgPathBooleanResult ApplyWithFirstAndSecondAndOperation(SvgPathData first, SvgPathData second, SvgPathBooleanOperation operation);
    /// <summary>Applies a filled-path Boolean operation with explicit rules and limits.</summary>
    /// <param name="first">The first filled path.</param><param name="second">The second filled path.</param><param name="operation">The Boolean operation.</param><param name="firstRule">The first fill rule.</param><param name="secondRule">The second fill rule.</param><param name="options">The bounded Boolean options.</param>
    /// <returns>The deterministic flattened result or a stable failure.</returns>
    SvgPathBooleanResult ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(SvgPathData first, SvgPathData second, SvgPathBooleanOperation operation, SvgFillRule firstRule, SvgFillRule secondRule, SvgPathBooleanOptions options);
    /// <summary>Computes a nonzero-filled union with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The second path.</param>
    /// <returns>The union result.</returns>
    SvgPathBooleanResult Union(SvgPathData first, SvgPathData second);
    /// <summary>Computes a nonzero-filled intersection with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The second path.</param>
    /// <returns>The intersection result.</returns>
    SvgPathBooleanResult Intersection(SvgPathData first, SvgPathData second);
    /// <summary>Computes a nonzero-filled first-minus-second difference with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The subtracted path.</param>
    /// <returns>The difference result.</returns>
    SvgPathBooleanResult Difference(SvgPathData first, SvgPathData second);
    /// <summary>Computes a nonzero-filled exclusive-or with deterministic defaults.</summary>
    /// <param name="first">The first path.</param><param name="second">The second path.</param>
    /// <returns>The exclusive-or result.</returns>
    SvgPathBooleanResult ExclusiveOr(SvgPathData first, SvgPathData second);
    /// <summary>Clips a subject path to a clip path with explicit fill rules and limits.</summary>
    /// <param name="subject">The subject geometry.</param><param name="clipPath">The clipping geometry.</param><param name="subjectRule">The subject fill rule.</param><param name="clipRule">The clip fill rule.</param><param name="options">The bounded Boolean options.</param>
    /// <returns>The clipped intersection result.</returns>
    SvgPathBooleanResult Clip(SvgPathData subject, SvgPathData clipPath, SvgFillRule subjectRule, SvgFillRule clipRule, SvgPathBooleanOptions options);
}
