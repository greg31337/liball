namespace icc;

/// <summary>Represents the primary and optional bi-spectral ranges of an ICC.2 spectral-range payload.</summary>
public class IccSpectralRanges
{
    /// <summary>Gets or sets the primary spectral range.</summary>
    public IccSpectralRange SpectralRange;

    /// <summary>Gets or sets the optional secondary range used by bi-spectral data.</summary>
    public IccSpectralRange BiSpectralRange;

    /// <summary>Initializes empty primary and secondary spectral ranges.</summary>
    public IccSpectralRanges()
    {
        SpectralRange = new IccSpectralRange();
        BiSpectralRange = new IccSpectralRange();
    }
}
