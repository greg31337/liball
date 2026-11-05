namespace icc;

/// <summary>Represents ordered xy chromaticities for colour channels.</summary>
public class IccChromaticity
{
    /// <summary>Gets or sets the colourant encoding selector.</summary>
    public int ColorantEncoding;
    /// <summary>Gets or sets ordered x coordinates.</summary>
    public double[] XCoordinates;
    /// <summary>Gets or sets ordered y coordinates.</summary>
    public double[] YCoordinates;

    /// <summary>Initializes an empty chromaticity set.</summary>
    public IccChromaticity()
    {
        ColorantEncoding = 0;
        XCoordinates = new double[0];
        YCoordinates = new double[0];
    }
}
