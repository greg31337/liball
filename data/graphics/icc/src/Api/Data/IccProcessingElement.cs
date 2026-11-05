namespace icc;

/// <summary>Represents one ICC multi-process element.</summary>
public class IccProcessingElement
{
    /// <summary>Gets or sets the element signature.</summary>
    public uint Signature;
    /// <summary>Gets or sets the decoded element kind.</summary>
    public IccProcessingElementKind Kind;
    /// <summary>Gets or sets the input channel count.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the output channel count.</summary>
    public int OutputChannels;
    /// <summary>Gets or sets input channel ranges.</summary>
    public IccEncodingRange[] InputRanges;
    /// <summary>Gets or sets output channel ranges.</summary>
    public IccEncodingRange[] OutputRanges;
    /// <summary>Gets or sets curves carried by the element.</summary>
    public IccCurve[] Curves;
    /// <summary>Gets or sets matrix data when present.</summary>
    public IccMatrix? Matrix;
    /// <summary>Gets or sets lookup-table data when present.</summary>
    public IccClut? Clut;
    /// <summary>Gets or sets sparse-matrix LUT data when present.</summary>
    public IccSparseMatrixArray? SparseMatrixLut;
    /// <summary>Gets or sets tone-map data when present.</summary>
    public IccToneMap? ToneMap;
    /// <summary>Gets or sets CIECAM02 viewing conditions for XYZ/Jab conversion elements.</summary>
    public IccCamViewingConditions? CamViewingConditions;
    /// <summary>Gets or sets spectral metadata for emission and reflectance elements.</summary>
    public IccSpectralProcessingData? SpectralProcessingData;
    /// <summary>Gets or sets the vendor ACS signature carried by a begin or end ACS element.</summary>
    public uint AcsSignature;
    /// <summary>Gets or sets optional vendor data retained from a begin or end ACS element.</summary>
    public byte[] AcsData;
    /// <summary>Gets or sets calculator data when present.</summary>
    public IccCalculatorProgram? Calculator;
    /// <summary>Gets or sets the complete encoded element.</summary>
    public IccRawData RawData;

    /// <summary>Initializes an unknown empty processing element.</summary>
    public IccProcessingElement()
    {
        Signature = 0;
        Kind = IccProcessingElementKind.Unknown;
        InputChannels = 0;
        OutputChannels = 0;
        InputRanges = new IccEncodingRange[0];
        OutputRanges = new IccEncodingRange[0];
        Curves = new IccCurve[0];
        Matrix = null;
        Clut = null;
        SparseMatrixLut = null;
        ToneMap = null;
        CamViewingConditions = null;
        SpectralProcessingData = null;
        AcsSignature = 0;
        AcsData = new byte[0];
        Calculator = null;
        RawData = new IccRawData();
    }
}
