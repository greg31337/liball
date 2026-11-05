namespace cbor;

/// <summary>
/// Holds a CBOR floating-point value and its exact encoded representation.
/// </summary>
public class CborFloatValue
{
    /// <summary>The numeric value converted to binary64.</summary>
    public double Value;
    /// <summary>The original encoded floating-point width.</summary>
    public CborFloatWidth Width;
    /// <summary>The original bits in the low 16, 32, or 64 bits according to <see cref="Width"/>.</summary>
    public ulong RawBits;
    /// <summary>Indicates that the original bits encode a NaN.</summary>
    public bool IsNaN;
    /// <summary>Indicates that the original bits encode a signaling NaN.</summary>
    public bool IsSignalingNaN;
}
