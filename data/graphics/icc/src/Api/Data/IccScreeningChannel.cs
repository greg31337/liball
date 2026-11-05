namespace icc;

/// <summary>Represents one legacy screening channel.</summary>
public class IccScreeningChannel
{
    /// <summary>Gets or sets screen frequency in the units selected by the enclosing screening data.</summary>
    public double Frequency;
    /// <summary>Gets or sets screen angle in degrees.</summary>
    public double AngleDegrees;
    /// <summary>Gets or sets the spot function.</summary>
    public IccScreeningSpotShape SpotShape;

    /// <summary>Initializes an unknown zero-frequency screening channel.</summary>
    public IccScreeningChannel()
    {
        Frequency = 0.0;
        AngleDegrees = 0.0;
        SpotShape = IccScreeningSpotShape.Unknown;
    }
}
