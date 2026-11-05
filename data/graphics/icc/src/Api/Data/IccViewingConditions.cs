namespace icc;

/// <summary>Represents ICC viewing-condition colorimetry.</summary>
public class IccViewingConditions
{
    /// <summary>Gets or sets the illuminant XYZ values.</summary>
    public IccXyzColor Illuminant;
    /// <summary>Gets or sets the surround XYZ values.</summary>
    public IccXyzColor Surround;
    /// <summary>Gets or sets the standard illuminant.</summary>
    public IccStandardIlluminant IlluminantKind;

    /// <summary>Initializes empty viewing conditions.</summary>
    public IccViewingConditions()
    {
        Illuminant = new IccXyzColor();
        Surround = new IccXyzColor();
        IlluminantKind = IccStandardIlluminant.Unknown;
    }
}
