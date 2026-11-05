namespace icc;

/// <summary>Defines an ICC.2 colour-encoding or abbreviated profile.</summary>
public class IccColorEncodingProfileDefinition
{
    /// <summary>Gets or sets the registered colour-encoding reference name.</summary>
    public string ReferenceName;
    /// <summary>Gets or sets the associated colour-space name.</summary>
    public string ColorSpaceName;
    /// <summary>Gets or sets optional colour-encoding parameters.</summary>
    public IccColorEncodingParameters? Parameters;
    /// <summary>Gets or sets optional profile connection conditions and spectral data.</summary>
    public IccProfileConnectionConditions? ProfileConnectionConditions;
    /// <summary>Gets or sets an optional spectral PCS signature.</summary>
    public uint SpectralPcsSignature;
    /// <summary>Gets or sets the primary spectral PCS wavelength range.</summary>
    public IccSpectralRange SpectralPcsRange;
    /// <summary>Gets or sets the optional bi-spectral PCS wavelength range.</summary>
    public IccSpectralRange BiSpectralPcsRange;
    /// <summary>Gets or sets an optional device-to-colorimetric-PCS fallback transform.</summary>
    public IccMultiProcessElements? ForwardFallbackTransform;
    /// <summary>Gets or sets an optional colorimetric-PCS-to-device fallback transform.</summary>
    public IccMultiProcessElements? BackwardFallbackTransform;

    /// <summary>Initializes an empty colour-encoding profile definition.</summary>
    public IccColorEncodingProfileDefinition()
    {
        ReferenceName = string.Empty;
        ColorSpaceName = string.Empty;
        Parameters = null;
        ProfileConnectionConditions = null;
        SpectralPcsSignature = 0;
        SpectralPcsRange = new IccSpectralRange();
        BiSpectralPcsRange = new IccSpectralRange();
        ForwardFallbackTransform = null;
        BackwardFallbackTransform = null;
    }
}
