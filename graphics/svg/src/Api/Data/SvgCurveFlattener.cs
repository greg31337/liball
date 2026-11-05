namespace svg;

/// <summary>Configures adaptive curve flattening and measurement space.</summary>
public class SvgFlattenOptions
{
    /// <summary>The positive maximum deviation in user or device units.</summary>
    public double Tolerance;
    /// <summary>The optional affine transform used only to measure device-space deviation.</summary>
    public SvgMatrix? DeviceTransform;
    /// <summary>The nonnegative maximum binary subdivision depth.</summary>
    public int MaximumSubdivisionDepth;
    /// <summary>The positive maximum emitted line-segment count.</summary>
    public int MaximumSegments;

    /// <summary>Creates secure deterministic flattening defaults.</summary>
    public SvgFlattenOptions()
    {
        Tolerance = 0.25;
        DeviceTransform = null;
        MaximumSubdivisionDepth = 32;
        MaximumSegments = 2000000;
    }
}

/// <summary>Associates one flattened point with its source unit parameter.</summary>
public class SvgFlattenedPoint
{
    /// <summary>The finite point in original user coordinates.</summary>
    public SvgPoint Point;
    /// <summary>The monotonic source parameter from zero through one.</summary>
    public double Amount;

    /// <summary>Creates a flattened point by copying a finite point.</summary>
    /// <param name="point">The finite user-space point.</param>
    /// <param name="amount">The unit source parameter.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for invalid point or parameter state.</exception>
    public SvgFlattenedPoint(SvgPoint point, double amount)
    {
        if (!SvgCurveMath.ValidPoint(point) || !SvgFiniteMath.IsFinite(amount) || amount < 0 || amount > 1) throw new System.ArgumentOutOfRangeException("point");
        Point = new SvgPoint(point.X, point.Y);
        Amount = SvgFiniteMath.NormalizeZero(amount);
    }
}

/// <summary>Contains parameter-ordered points from adaptive segment flattening.</summary>
public class SvgFlattenResult
{
    /// <summary>Whether flattening met its tolerance and limits.</summary>
    public bool Success;
    /// <summary>The stable flattening failure.</summary>
    public SvgFlattenError Error;
    /// <summary>The ordered points, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgFlattenedPoint>? Points;
    /// <summary>The emitted line-segment count.</summary>
    public int SegmentCount;

    /// <summary>Creates an adaptive flattening result.</summary>
    /// <param name="success">Whether flattening succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="points">The ordered points.</param>
    /// <param name="segmentCount">The emitted segment count.</param>
    public SvgFlattenResult(bool success, SvgFlattenError error, System.Collections.Generic.List<SvgFlattenedPoint>? points, int segmentCount)
    {
        Success = success;
        Error = error;
        Points = points;
        SegmentCount = segmentCount;
    }
}
