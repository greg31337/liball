namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgFillContainment"/>.</summary>
public interface ISvgFillContainment
{
    /// <summary>Classifies a point in path user coordinates using deterministic defaults.</summary>
    /// <param name="path">The valid path.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="fillRule">The fill rule.</param>
    /// <returns>The robust containment classification.</returns>
    SvgFillContainmentResult ClassifyWithPathAndPointAndFillRule(SvgPathData path, SvgPoint point, SvgFillRule fillRule);
    /// <summary>Classifies a point using explicit tolerance, limits, and output-space transform.</summary>
    /// <param name="path">The valid path.</param>
    /// <param name="point">The point in path or transformed output coordinates.</param>
    /// <param name="fillRule">The fill rule.</param>
    /// <param name="options">The bounded containment options.</param>
    /// <returns>The robust containment classification or a stable failure.</returns>
    SvgFillContainmentResult ClassifyWithPathAndPointAndFillRuleAndOptions(SvgPathData path, SvgPoint point, SvgFillRule fillRule, SvgFillContainmentOptions options);
}
