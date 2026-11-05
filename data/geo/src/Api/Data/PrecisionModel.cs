namespace geo;

/// <summary>Stores a coordinate precision representation and fixed-grid parameters.</summary>
public class PrecisionModel
{
    /// <summary>The precision representation.</summary>
    public PrecisionModelType Type = PrecisionModelType.Floating;

    /// <summary>The fixed-grid scale, where one represents unit spacing.</summary>
    public double Scale = 1.0;

    /// <summary>The fixed-grid X origin offset.</summary>
    public double OffsetX;

    /// <summary>The fixed-grid Y origin offset.</summary>
    public double OffsetY;
}
