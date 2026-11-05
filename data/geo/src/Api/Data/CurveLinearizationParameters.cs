namespace geo;

/// <summary>Specifies how curved geometries are converted to linear segments.</summary>
public class CurveLinearizationParameters
{
    /// <summary>The quantity bounded by the configured tolerance.</summary>
    public CurveLinearizationMode Mode;

    /// <summary>The positive bound selected by the linearization mode.</summary>
    public double Tolerance = 0.01;

    /// <summary>The maximum number of generated segments per source arc.</summary>
    public int MaximumSegments = 100000;
}
