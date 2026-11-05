namespace icc;

/// <summary>Represents spectral illuminant and observer viewing conditions.</summary>
public class IccSpectralViewingConditions
{
    /// <summary>Gets or sets the standard illuminant selector.</summary>
    public IccStandardIlluminant Illuminant;
    /// <summary>Gets or sets a custom illuminant spectrum.</summary>
    public IccSpectralData? IlluminantSpectrum;
    /// <summary>Gets or sets the illuminant correlated colour temperature in kelvins.</summary>
    public double ColorTemperature;
    /// <summary>Gets or sets the standard observer selector.</summary>
    public IccMeasurementObserver Observer;
    /// <summary>Gets or sets custom observer colour-matching functions.</summary>
    public IccSpectralData? ObserverFunctions;
    /// <summary>Gets or sets computed illuminant XYZ values.</summary>
    public IccXyzColor IlluminantColor;
    /// <summary>Gets or sets the surround XYZ values.</summary>
    public IccXyzColor SurroundColor;

    /// <summary>Initializes unspecified spectral viewing conditions.</summary>
    public IccSpectralViewingConditions()
    {
        Illuminant = IccStandardIlluminant.Unknown;
        IlluminantSpectrum = null;
        ColorTemperature = 0.0;
        Observer = IccMeasurementObserver.Unknown;
        ObserverFunctions = null;
        IlluminantColor = new IccXyzColor();
        SurroundColor = new IccXyzColor();
    }
}
