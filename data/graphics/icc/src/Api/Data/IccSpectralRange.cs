namespace icc;

/// <summary>Represents a uniformly sampled spectral wavelength range.</summary>
public class IccSpectralRange
{
    /// <summary>Gets or sets the starting wavelength in nanometres.</summary>
    public double StartWavelength;
    /// <summary>Gets or sets the ending wavelength in nanometres.</summary>
    public double EndWavelength;
    /// <summary>Gets or sets the number of wavelength steps.</summary>
    public int Steps;

    /// <summary>Initializes an empty spectral range.</summary>
    public IccSpectralRange()
    {
        StartWavelength = 0.0;
        EndWavelength = 0.0;
        Steps = 0;
    }
}
