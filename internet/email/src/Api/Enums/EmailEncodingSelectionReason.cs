namespace email;
/// <summary>
/// Specifies the supported encoding selection values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailEncodingSelectionReason
{
    /// <summary>
    /// Represents the seven bit safe choice or state.
    /// </summary>
    SevenBitSafe = 0,
    /// <summary>
    /// Represents the eight bit safe choice or state.
    /// </summary>
    EightBitSafe = 1,
    /// <summary>
    /// Represents the binary transport available choice or state.
    /// </summary>
    BinaryTransportAvailable = 2,
    /// <summary>
    /// Represents the mostly text choice or state.
    /// </summary>
    MostlyText = 3,
    /// <summary>
    /// Represents the binary content choice or state.
    /// </summary>
    BinaryContent = 4
}
