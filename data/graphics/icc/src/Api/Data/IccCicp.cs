namespace icc;

/// <summary>Represents coding-independent code-point metadata.</summary>
public class IccCicp
{
    /// <summary>Gets or sets the colour-primaries code point.</summary>
    public int ColorPrimaries;
    /// <summary>Gets or sets the transfer-characteristics code point.</summary>
    public int TransferCharacteristics;
    /// <summary>Gets or sets the matrix-coefficients code point.</summary>
    public int MatrixCoefficients;
    /// <summary>Gets or sets whether the signal uses full-range quantization.</summary>
    public bool FullRange;
    /// <summary>Gets or sets whether unspecified primaries resolve to the profile's RGB matrix columns under the ICC amendment.</summary>
    public bool UsesProfilePrimaries;

    /// <summary>Initializes unspecified CICP metadata.</summary>
    public IccCicp()
    {
        ColorPrimaries = 2;
        TransferCharacteristics = 2;
        MatrixCoefficients = 2;
        FullRange = false;
        UsesProfilePrimaries = false;
    }
}
