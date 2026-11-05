namespace icc;

/// <summary>Represents ICC colorimetric measurement conditions.</summary>
public class IccMeasurement
{
    /// <summary>Gets or sets the standard observer.</summary>
    public IccMeasurementObserver Observer;
    /// <summary>Gets or sets the measurement backing tristimulus values.</summary>
    public IccXyzColor Backing;
    /// <summary>Gets or sets the measurement geometry.</summary>
    public IccMeasurementGeometry Geometry;
    /// <summary>Gets or sets measurement flare in the normalized range.</summary>
    public double Flare;
    /// <summary>Gets or sets the standard illuminant.</summary>
    public IccStandardIlluminant Illuminant;

    /// <summary>Initializes unknown measurement conditions.</summary>
    public IccMeasurement()
    {
        Observer = IccMeasurementObserver.Unknown;
        Backing = new IccXyzColor();
        Geometry = IccMeasurementGeometry.Unknown;
        Flare = 0.0;
        Illuminant = IccStandardIlluminant.Unknown;
    }
}
