namespace email;
/// <summary>
/// Specifies the supported query security values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailQuerySecurityState
{
    /// <summary>
    /// No query security is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the signed choice or state.
    /// </summary>
    Signed = 1,
    /// <summary>
    /// Represents the encrypted choice or state.
    /// </summary>
    Encrypted = 2,
    /// <summary>
    /// Represents the signed and encrypted choice or state.
    /// </summary>
    SignedAndEncrypted = 3,
    /// <summary>
    /// Represents the report choice or state.
    /// </summary>
    Report = 4
}
