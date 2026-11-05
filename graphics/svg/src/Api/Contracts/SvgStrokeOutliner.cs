namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgStrokeDashes"/>.</summary>
public interface ISvgStrokeDashes
{
    /// <summary>Copies and normalizes a dash array, repeating odd-length arrays.</summary>
    /// <param name="dashArray">The finite nonnegative dash lengths.</param><param name="dashOffset">The finite signed offset.</param>
    /// <returns>The normalized even pattern or a stable failure.</returns>
    SvgStrokeDashPatternResult Normalize(System.Collections.Generic.List<double>? dashArray, double dashOffset);
}

/// <summary>Defines the public operations exposed by <see cref="SvgStrokeOutliner"/>.</summary>
public interface ISvgStrokeOutliner
{
    /// <summary>Constructs a closed outline for a path and explicit stroke style.</summary>
    /// <param name="path">The valid source path.</param><param name="style">The stroke geometry and bounds.</param>
    /// <returns>The outline and dash placements or a stable failure.</returns>
    SvgStrokeOutlineResult Create(SvgPathData path, SvgStrokeStyle style);
}
