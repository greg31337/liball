namespace icc;

/// <summary>Represents an ICC LUT8 or LUT16 pipeline.</summary>
public class IccLut
{
    /// <summary>Gets or sets the input channel count.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the output channel count.</summary>
    public int OutputChannels;
    /// <summary>Gets or sets the three-by-three matrix.</summary>
    public IccMatrix Matrix;
    /// <summary>Gets or sets per-channel input tables.</summary>
    public double[][] InputTables;
    /// <summary>Gets or sets the multidimensional lookup table.</summary>
    public IccClut Clut;
    /// <summary>Gets or sets per-channel output tables.</summary>
    public double[][] OutputTables;
    /// <summary>Gets or sets the PCS encoding.</summary>
    public IccPcsEncodingKind PcsEncoding;

    /// <summary>Initializes an empty LUT pipeline.</summary>
    public IccLut()
    {
        InputChannels = 0;
        OutputChannels = 0;
        Matrix = new IccMatrix();
        InputTables = new double[0][];
        Clut = new IccClut();
        OutputTables = new double[0][];
        PcsEncoding = IccPcsEncodingKind.Normalized;
    }
}
