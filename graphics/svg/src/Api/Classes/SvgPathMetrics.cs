namespace svg;

/// <summary>Provides bounded path/subpath length, location, calibration, and extraction algorithms.</summary>
public class SvgPathMetrics : ISvgPathMetrics
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathMetrics()
    {
    }

    /// <summary>Measures whole-path and ordered subpath lengths with deterministic defaults.</summary>
    /// <param name="path">The valid path.</param>
    /// <returns>The measured metrics or a stable failure.</returns>
    public SvgPathMetricResult MeasureWithPath(SvgPathData path)
    {
        return new SvgPathMetrics().MeasureWithPathAndOptions(path, new SvgPathMeasureOptions());
    }

    /// <summary>Measures whole-path and ordered subpath lengths.</summary>
    /// <param name="path">The valid path.</param><param name="options">The measurement options.</param>
    /// <returns>The measured metrics or a stable failure.</returns>
    public SvgPathMetricResult MeasureWithPathAndOptions(SvgPathData path, SvgPathMeasureOptions options)
    {
        SvgPathMeasureState state = SvgPathMeasureState.Create(path, options);
        if (state.Error != SvgPathMetricError.None) return new SvgPathMetrics().MetricFailure(state.Error);
        return state.MetricResult();
    }

    /// <summary>Finds the exact point and unit frame at a clamped actual or calibrated distance.</summary>
    /// <param name="path">The valid path.</param><param name="distance">The nonnegative distance.</param><param name="options">The measurement and calibration options.</param>
    /// <returns>The point, tangent, and normal or a stable failure.</returns>
    public SvgPathLocationResult GetLocationWithPathAndDistanceAndOptions(SvgPathData path, double distance, SvgPathMeasureOptions options)
    {
        SvgPathMeasureState state = SvgPathMeasureState.Create(path, options);
        if (state.Error != SvgPathMetricError.None) return new SvgPathMetrics().LocationFailure(state.Error);
        return state.Location(distance);
    }

    /// <summary>Finds the point and unit frame at a clamped actual distance.</summary>
    /// <param name="path">The valid path.</param><param name="distance">The nonnegative actual distance.</param>
    /// <returns>The point, tangent, and normal or a stable failure.</returns>
    public SvgPathLocationResult GetLocationWithPathAndDistance(SvgPathData path, double distance)
    {
        return new SvgPathMetrics().GetLocationWithPathAndDistanceAndOptions(path, distance, new SvgPathMeasureOptions());
    }

    /// <summary>Extracts and type-preserves path geometry between two clamped distances.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param><param name="options">The measurement and calibration options.</param>
    /// <returns>The extracted normalized path or a stable failure.</returns>
    public SvgPathExtractResult ExtractWithPathAndStartDistanceAndEndDistanceAndOptions(SvgPathData path, double startDistance, double endDistance, SvgPathMeasureOptions options)
    {
        SvgPathMeasureState state = SvgPathMeasureState.Create(path, options);
        if (state.Error != SvgPathMetricError.None) return new SvgPathMetrics().ExtractFailure(state.Error);
        return state.Extract(startDistance, endDistance);
    }

    /// <summary>Extracts and type-preserves path geometry between two clamped actual distances.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param>
    /// <returns>The extracted normalized path or a stable failure.</returns>
    public SvgPathExtractResult ExtractWithPathAndStartDistanceAndEndDistance(SvgPathData path, double startDistance, double endDistance)
    {
        return new SvgPathMetrics().ExtractWithPathAndStartDistanceAndEndDistanceAndOptions(path, startDistance, endDistance, new SvgPathMeasureOptions());
    }

    /// <summary>Trims a path to the same type-preserving interval used by extraction.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param><param name="options">The measurement and calibration options.</param>
    /// <returns>The trimmed normalized path or a stable failure.</returns>
    public SvgPathExtractResult TrimWithPathAndStartDistanceAndEndDistanceAndOptions(SvgPathData path, double startDistance, double endDistance, SvgPathMeasureOptions options)
    {
        return new SvgPathMetrics().ExtractWithPathAndStartDistanceAndEndDistanceAndOptions(path, startDistance, endDistance, options);
    }

    /// <summary>Trims a path between two clamped actual distances.</summary>
    /// <param name="path">The valid path.</param><param name="startDistance">The nonnegative start distance.</param><param name="endDistance">The nonnegative end distance.</param>
    /// <returns>The trimmed normalized path or a stable failure.</returns>
    public SvgPathExtractResult TrimWithPathAndStartDistanceAndEndDistance(SvgPathData path, double startDistance, double endDistance)
    {
        return new SvgPathMetrics().TrimWithPathAndStartDistanceAndEndDistanceAndOptions(path, startDistance, endDistance, new SvgPathMeasureOptions());
    }

    internal SvgPathMetricResult MetricFailure(SvgPathMetricError error) { return new SvgPathMetricResult(false, error, 0, 0, 0, null); }
    internal SvgPathLocationResult LocationFailure(SvgPathMetricError error) { return new SvgPathLocationResult(false, error, null, null, null, 0, -1); }
    internal SvgPathExtractResult ExtractFailure(SvgPathMetricError error) { return new SvgPathExtractResult(false, error, null); }
}
