namespace email;
/// <summary>
/// Specifies the supported transfer encoding constraint values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTransferEncodingConstraint
{
    /// <summary>
    /// Represents the seven bit choice or state.
    /// </summary>
    SevenBit = 0,
    /// <summary>
    /// Represents the eight bit choice or state.
    /// </summary>
    EightBit = 1,
    /// <summary>
    /// Represents the binary choice or state.
    /// </summary>
    Binary = 2
}
