namespace molecule;


/// <summary>
/// Controls fingerprint size and which molecular features contribute to fingerprint bits.
/// </summary>
public class FingerprintOptions
{
    /// <summary>
    /// The bit count.
    /// </summary>
    public int BitCount;
    /// <summary>
    /// The minimum path length.
    /// </summary>
    public int MinimumPathLength;
    /// <summary>
    /// The maximum path length.
    /// </summary>
    public int MaximumPathLength;
    /// <summary>
    /// The circular radius.
    /// </summary>
    public int CircularRadius;
    /// <summary>
    /// The size of an explicitly folded bit fingerprint.
    /// </summary>
    public int FoldedBitCount;
    /// <summary>
    /// Whether bond order contributes to the operation.
    /// </summary>
    public bool IncludeBondOrder;
    /// <summary>
    /// Whether aromaticity contributes to the operation.
    /// </summary>
    public bool IncludeAromaticity;
    /// <summary>
    /// Whether formal charge contributes to the operation.
    /// </summary>
    public bool IncludeFormalCharge;

    /// <summary>
    /// Initializes a new instance of the <see cref="FingerprintOptions"/> class.
    /// </summary>
    public FingerprintOptions()
    {
        BitCount = 1024;
        MinimumPathLength = 0;
        MaximumPathLength = 7;
        CircularRadius = 2;
        FoldedBitCount = 128;
        IncludeBondOrder = true;
        IncludeAromaticity = true;
        IncludeFormalCharge = true;
    }
}
