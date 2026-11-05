namespace icc;

/// <summary>Represents the CIECAM02 viewing-condition parameters stored by ICC.2 appearance conversion elements.</summary>
public class IccCamViewingConditions
{
    /// <summary>Gets or sets the adopted white point as XYZ tristimulus values.</summary>
    public IccXyzColor WhitePoint;
    /// <summary>Gets or sets the adapting-field luminance in candelas per square metre.</summary>
    public double Luminance;
    /// <summary>Gets or sets the background luminance in candelas per square metre.</summary>
    public double BackgroundLuminance;
    /// <summary>Gets or sets the impact-of-surround parameter.</summary>
    public double ImpactOfSurround;
    /// <summary>Gets or sets the chromatic-induction factor.</summary>
    public double ChromaticInductionFactor;
    /// <summary>Gets or sets the degree-of-adaptation factor.</summary>
    public double AdaptationFactor;

    /// <summary>Initializes zero-valued CIECAM02 viewing conditions.</summary>
    public IccCamViewingConditions()
    {
        WhitePoint = new IccXyzColor();
        Luminance = 0.0;
        BackgroundLuminance = 0.0;
        ImpactOfSurround = 0.0;
        ChromaticInductionFactor = 0.0;
        AdaptationFactor = 0.0;
    }
}
