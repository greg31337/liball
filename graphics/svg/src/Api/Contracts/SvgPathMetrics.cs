namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathMetrics"/>.</summary>
public interface ISvgPathMetrics
{
    /// <summary>Measures whole-path and ordered subpath lengths with deterministic defaults.</summary>
    /// <param name="path">The valid path.</param>
    /// <returns>The measured metrics or a stable failure.</returns>
    SvgPathMetricResult MeasureWithPath(SvgPathData path);
    /// <summary>Measures whole-path and ordered subpath lengths.</summary>
    /// <param name="path">The valid path.</param><param name="options">The measurement options.</param>
    /// <returns>The measured metrics or a stable failure.</returns>
    SvgPathMetricResult MeasureWithPathAndOptions(SvgPathData path, SvgPathMeasureOptions options);
    /// <summary>Finds the exact point and unit frame at a clamped actual or calibrated distance.</summary>
    /// <param name="path">The valid path.</param><param name="distance">The nonnegative distance.</param><param name="options">The measurement and calibration options.</param>
    /// <returns>The point, tangent, and normal or a stable failure.</returns>
    SvgPathLocationResult GetLocationWithPathAndDistanceAndOptions(SvgPathData path, double distance, SvgPathMeasureOptions options);
    /// <summary>Finds the point and unit frame at a clamped actual distance.</summary>
    /// <param name="path">The valid path.</param><param name="distance">The nonnegative actual distance.</param>
    /// <returns>The point, tangent, and normal or a stable failure.</returns>
    SvgPathLocationResult GetLocationWithPathAndDistance(SvgPathData path, double distance);
    /// <summary>Extracts and type-preserves path geometry between two clamped distances.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param><param name="options">The measurement and calibration options.</param>
    /// <returns>The extracted normalized path or a stable failure.</returns>
    SvgPathExtractResult ExtractWithPathAndStartDistanceAndEndDistanceAndOptions(SvgPathData path, double startDistance, double endDistance, SvgPathMeasureOptions options);
    /// <summary>Extracts and type-preserves path geometry between two clamped actual distances.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param>
    /// <returns>The extracted normalized path or a stable failure.</returns>
    SvgPathExtractResult ExtractWithPathAndStartDistanceAndEndDistance(SvgPathData path, double startDistance, double endDistance);
    /// <summary>Trims a path to the same type-preserving interval used by extraction.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param><param name="options">The measurement and calibration options.</param>
    /// <returns>The trimmed normalized path or a stable failure.</returns>
    SvgPathExtractResult TrimWithPathAndStartDistanceAndEndDistanceAndOptions(SvgPathData path, double startDistance, double endDistance, SvgPathMeasureOptions options);
    /// <summary>Trims a path between two clamped actual distances.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param>
    /// <returns>The trimmed normalized path or a stable failure.</returns>
    SvgPathExtractResult TrimWithPathAndStartDistanceAndEndDistance(SvgPathData path, double startDistance, double endDistance);
}
