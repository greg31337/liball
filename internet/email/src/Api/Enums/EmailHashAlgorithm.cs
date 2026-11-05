namespace email;
/// <summary>
/// Specifies the supported hash values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHashAlgorithm
{
    /// <summary>
    /// Represents the sha 256 choice or state.
    /// </summary>
    Sha256 = 0,
    /// <summary>
    /// Represents the sha 384 choice or state.
    /// </summary>
    Sha384 = 1,
    /// <summary>
    /// Represents the sha 512 choice or state.
    /// </summary>
    Sha512 = 2,
    /// <summary>
    /// Represents the sha 1 choice or state.
    /// </summary>
    Sha1 = 3
}
