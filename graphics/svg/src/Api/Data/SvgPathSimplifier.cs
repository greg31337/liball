namespace svg;

/// <summary>Configures bounded tolerance-based path simplification and rounding.</summary>
public class SvgPathSimplifyOptions
{
    /// <summary>The positive maximum conservative geometric error.</summary>
    public double Tolerance;
    /// <summary>The decimal places used for every output coordinate, from zero through fifteen.</summary>
    public int DecimalPlaces;
    /// <summary>The nonnegative adaptive curve subdivision limit.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive normalized and output command limit.</summary>
    public int MaximumCommands;
    /// <summary>The positive flattened source point limit.</summary>
    public int MaximumPoints;
    /// <summary>The positive reduction and measurement distance-check limit.</summary>
    public int MaximumOperations;

    /// <summary>Creates secure deterministic simplification defaults.</summary>
    public SvgPathSimplifyOptions() { Tolerance = 0.01; DecimalPlaces = 6; MaximumSubdivisionDepth = 32; MaximumCommands = 1000000; MaximumPoints = 2000000; MaximumOperations = 4000000; }
}

/// <summary>Reports the measured and bounded losses introduced by simplification.</summary>
public class SvgPathLossyReport
{
    /// <summary>Whether any curve conversion, point removal, or coordinate rounding occurred.</summary>
    public bool IsLossy;
    /// <summary>The number of quadratic, cubic, or nondegenerate arc commands flattened.</summary>
    public int FlattenedCurveCount;
    /// <summary>The flattened source point count.</summary>
    public int OriginalPointCount;
    /// <summary>The simplified output point count before close commands.</summary>
    public int OutputPointCount;
    /// <summary>The number of flattened points removed.</summary>
    public int RemovedPointCount;
    /// <summary>The number of x or y coordinates changed by rounding.</summary>
    public int RoundedCoordinateCount;
    /// <summary>The measured maximum flattened-polyline deviation.</summary>
    public double MeasuredPolylineError;
    /// <summary>The adaptive source-curve flattening error bound.</summary>
    public double CurveFlatteningErrorBound;
    /// <summary>The conservative combined maximum geometric error.</summary>
    public double MaximumErrorBound;
    /// <summary>The caller-requested maximum error.</summary>
    public double ErrorLimit;

    /// <summary>Creates an explicit lossy simplification report.</summary>
    /// <param name="isLossy">Whether geometry representation changed lossily.</param><param name="flattenedCurveCount">The curve count.</param><param name="originalPointCount">The source point count.</param><param name="outputPointCount">The output point count.</param><param name="removedPointCount">The removed point count.</param><param name="roundedCoordinateCount">The rounded coordinate count.</param><param name="measuredPolylineError">The measured polyline error.</param><param name="curveFlatteningErrorBound">The curve flattening bound.</param><param name="maximumErrorBound">The combined bound.</param><param name="errorLimit">The requested limit.</param>
    public SvgPathLossyReport(bool isLossy, int flattenedCurveCount, int originalPointCount, int outputPointCount, int removedPointCount, int roundedCoordinateCount, double measuredPolylineError, double curveFlatteningErrorBound, double maximumErrorBound, double errorLimit) { IsLossy = isLossy; FlattenedCurveCount = flattenedCurveCount; OriginalPointCount = originalPointCount; OutputPointCount = outputPointCount; RemovedPointCount = removedPointCount; RoundedCoordinateCount = roundedCoordinateCount; MeasuredPolylineError = measuredPolylineError; CurveFlatteningErrorBound = curveFlatteningErrorBound; MaximumErrorBound = maximumErrorBound; ErrorLimit = errorLimit; }
}

/// <summary>Contains a bounded line-normalized lossy simplification result.</summary>
public class SvgPathSimplifyResult
{
    /// <summary>Whether simplification succeeded within the requested error.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPathSimplifyError Error;
    /// <summary>The rounded line-normalized path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;
    /// <summary>The explicit lossy report when geometry processing occurred.</summary>
    public SvgPathLossyReport? Report;

    /// <summary>Creates a path simplification result.</summary>
    /// <param name="success">Whether simplification succeeded.</param><param name="error">The failure.</param><param name="pathData">The output path.</param><param name="report">The lossy report.</param>
    public SvgPathSimplifyResult(bool success, SvgPathSimplifyError error, SvgPathData? pathData, SvgPathLossyReport? report) { Success = success; Error = error; PathData = pathData; Report = report; }
}
