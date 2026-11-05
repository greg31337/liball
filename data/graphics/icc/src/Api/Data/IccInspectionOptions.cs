namespace icc;

/// <summary>Controls bounded structured and textual profile inspection.</summary>
public class IccInspectionOptions
{
    /// <summary>Gets or sets the requested detail level.</summary>
    public IccInspectionDetail Detail;
    /// <summary>Gets or sets the maximum displayed hierarchy depth.</summary>
    public int MaximumDepth;
    /// <summary>Gets or sets the maximum displayed child count per node.</summary>
    public int MaximumChildren;
    /// <summary>Gets or sets the maximum displayed raw byte count.</summary>
    public int MaximumRawBytes;
    /// <summary>Gets or sets whether raw bytes are included.</summary>
    public bool IncludeRawBytes;

    /// <summary>Initializes bounded normal-detail inspection.</summary>
    public IccInspectionOptions()
    {
        Detail = IccInspectionDetail.Normal;
        MaximumDepth = 32;
        MaximumChildren = 1024;
        MaximumRawBytes = 256;
        IncludeRawBytes = false;
    }
}
