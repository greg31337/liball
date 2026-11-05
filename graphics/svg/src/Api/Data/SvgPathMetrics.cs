namespace svg;

/// <summary>Configures bounded path length, lookup, and extraction.</summary>
public class SvgPathMeasureOptions
{
    /// <summary>The positive absolute user-space flattening tolerance.</summary>
    public double Tolerance;
    /// <summary>The nonnegative maximum adaptive subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum total flattened segment count.</summary>
    public int MaximumFlattenedSegments;
    /// <summary>The optional authored pathLength calibration.</summary>
    public SvgPathLength? PathLength;

    /// <summary>Creates secure deterministic measurement defaults.</summary>
    public SvgPathMeasureOptions()
    {
        Tolerance = 0.01;
        MaximumSubdivisionDepth = 32;
        MaximumFlattenedSegments = 2000000;
        PathLength = null;
    }
}

/// <summary>Contains measured whole-path and per-subpath lengths.</summary>
public class SvgPathMetricResult
{
    /// <summary>Whether bounded measurement succeeded.</summary>
    public bool Success;
    /// <summary>The stable metric failure.</summary>
    public SvgPathMetricError Error;
    /// <summary>The measured actual user-space length.</summary>
    public double ActualLength;
    /// <summary>The authored calibrated length, equal to actual length without calibration.</summary>
    public double CalibratedLength;
    /// <summary>Actual length divided by calibrated length, or one without calibration.</summary>
    public double Scale;
    /// <summary>The measured lengths of ordered subpaths.</summary>
    public System.Collections.Generic.List<double>? SubpathLengths;

    /// <summary>Creates a path metric result.</summary>
    /// <param name="success">Whether measurement succeeded.</param><param name="error">The failure kind.</param>
    /// <param name="actualLength">The actual length.</param><param name="calibratedLength">The calibrated length.</param>
    /// <param name="scale">The actual-to-calibrated scale.</param><param name="subpathLengths">The ordered subpath lengths.</param>
    public SvgPathMetricResult(bool success, SvgPathMetricError error, double actualLength, double calibratedLength, double scale, System.Collections.Generic.List<double>? subpathLengths)
    {
        Success = success; Error = error; ActualLength = actualLength; CalibratedLength = calibratedLength; Scale = scale; SubpathLengths = subpathLengths;
    }
}

/// <summary>Contains point, tangent, and normal data at one path distance.</summary>
public class SvgPathLocationResult
{
    /// <summary>Whether lookup succeeded.</summary>
    public bool Success;
    /// <summary>The stable metric failure.</summary>
    public SvgPathMetricError Error;
    /// <summary>The exact curve point.</summary>
    public SvgPoint? Point;
    /// <summary>The normalized forward tangent, or zero for an entirely zero-length path.</summary>
    public SvgVector? Tangent;
    /// <summary>The normalized left-hand normal.</summary>
    public SvgVector? Normal;
    /// <summary>The clamped actual user-space distance.</summary>
    public double ActualDistance;
    /// <summary>The zero-based drawable segment index, or <c>-1</c>.</summary>
    public int SegmentIndex;

    /// <summary>Creates a path location result.</summary>
    /// <param name="success">Whether lookup succeeded.</param><param name="error">The failure kind.</param>
    /// <param name="point">The exact point.</param><param name="tangent">The unit tangent.</param><param name="normal">The unit normal.</param>
    /// <param name="actualDistance">The actual distance.</param><param name="segmentIndex">The segment index.</param>
    public SvgPathLocationResult(bool success, SvgPathMetricError error, SvgPoint? point, SvgVector? tangent, SvgVector? normal, double actualDistance, int segmentIndex)
    {
        Success = success; Error = error; Point = point; Tangent = tangent; Normal = normal; ActualDistance = actualDistance; SegmentIndex = segmentIndex;
    }
}

/// <summary>Contains a path extracted between two distances.</summary>
public class SvgPathExtractResult
{
    /// <summary>Whether extraction succeeded.</summary>
    public bool Success;
    /// <summary>The stable metric failure.</summary>
    public SvgPathMetricError Error;
    /// <summary>The normalized extracted path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;

    /// <summary>Creates a path extraction result.</summary>
    /// <param name="success">Whether extraction succeeded.</param><param name="error">The failure kind.</param><param name="pathData">The extracted path.</param>
    public SvgPathExtractResult(bool success, SvgPathMetricError error, SvgPathData? pathData) { Success = success; Error = error; PathData = pathData; }
}
