namespace icc;

/// <summary>Represents ICC.2 profile connection conditions.</summary>
public class IccProfileConnectionConditions
{
    /// <summary>Gets or sets spectral viewing conditions.</summary>
    public IccSpectralViewingConditions ViewingConditions;
    /// <summary>Gets or sets the media white point.</summary>
    public IccXyzColor MediaWhitePoint;
    /// <summary>Gets or sets the optional spectral white-point samples.</summary>
    public double[] SpectralWhitePoint;
    /// <summary>Gets or sets a custom-to-standard transform.</summary>
    public IccMultiProcessElements? ToStandardPcs;
    /// <summary>Gets or sets a standard-to-custom transform.</summary>
    public IccMultiProcessElements? FromStandardPcs;

    /// <summary>Initializes empty profile connection conditions.</summary>
    public IccProfileConnectionConditions()
    {
        ViewingConditions = new IccSpectralViewingConditions();
        MediaWhitePoint = new IccXyzColor();
        SpectralWhitePoint = new double[0];
        ToStandardPcs = null;
        FromStandardPcs = null;
    }
}
