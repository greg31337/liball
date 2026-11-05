namespace icc;

/// <summary>Represents the decoded components of an ICC XYZNumber.</summary>
public class IccXyzNumber
{
    /// <summary>Gets or sets the X component.</summary>
    public double X;
    /// <summary>Gets or sets the Y component.</summary>
    public double Y;
    /// <summary>Gets or sets the Z component.</summary>
    public double Z;

    /// <summary>Initializes a zero XYZNumber.</summary>
    public IccXyzNumber()
    {
        X = 0.0;
        Y = 0.0;
        Z = 0.0;
    }
}
