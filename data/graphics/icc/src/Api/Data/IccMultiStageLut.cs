namespace icc;

/// <summary>Represents an ICC lutAToB or lutBToA multi-stage pipeline.</summary>
public class IccMultiStageLut
{
    /// <summary>Gets or sets the input channel count.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the output channel count.</summary>
    public int OutputChannels;
    /// <summary>Gets or sets A curves.</summary>
    public IccCurve[] ACurves;
    /// <summary>Gets or sets B curves.</summary>
    public IccCurve[] BCurves;
    /// <summary>Gets or sets M curves.</summary>
    public IccCurve[] MCurves;
    /// <summary>Gets or sets the optional matrix.</summary>
    public IccMatrix? Matrix;
    /// <summary>Gets or sets the optional lookup table.</summary>
    public IccClut? Clut;
    /// <summary>Gets or sets the PCS encoding.</summary>
    public IccPcsEncodingKind PcsEncoding;

    /// <summary>Initializes an empty normalized multi-stage pipeline.</summary>
    public IccMultiStageLut()
    {
        InputChannels = 0;
        OutputChannels = 0;
        ACurves = new IccCurve[0];
        BCurves = new IccCurve[0];
        MCurves = new IccCurve[0];
        Matrix = null;
        Clut = null;
        PcsEncoding = IccPcsEncodingKind.Normalized;
    }
}
