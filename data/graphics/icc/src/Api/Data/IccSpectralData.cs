namespace icc;

/// <summary>Represents sampled or sparse spectral data.</summary>
public class IccSpectralData
{
    /// <summary>Gets or sets the spectral range.</summary>
    public IccSpectralRange Range;
    /// <summary>Gets or sets ordered spectral values.</summary>
    public double[] Values;
    /// <summary>Gets or sets sparse spectral values when applicable.</summary>
    public IccSparseMatrix? SparseValues;
    /// <summary>Gets or sets the number of spectra represented.</summary>
    public int SpectrumCount;

    /// <summary>Initializes empty spectral data.</summary>
    public IccSpectralData()
    {
        Range = new IccSpectralRange();
        Values = new double[0];
        SparseValues = null;
        SpectrumCount = 0;
    }
}
