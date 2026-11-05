namespace icc;

/// <summary>Represents spectral metadata shared by ICC.2 processing elements.</summary>
public class IccSpectralProcessingData
{
    /// <summary>Gets or sets the reference-white spectrum and its wavelength range.</summary>
    public IccSpectralData White;
    /// <summary>Gets or sets element flags, including the absolute-processing flag where defined.</summary>
    public uint Flags;

    /// <summary>Initializes empty spectral processing data.</summary>
    public IccSpectralProcessingData()
    {
        White = new IccSpectralData();
        Flags = 0;
    }
}
