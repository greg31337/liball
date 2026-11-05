namespace email;
/// <summary>
/// Specifies the supported normalization change values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailNormalizationChangeKind
{
    /// <summary>
    /// Represents the line ending choice or state.
    /// </summary>
    LineEnding = 0,
    /// <summary>
    /// Represents the header folding choice or state.
    /// </summary>
    HeaderFolding = 1,
    /// <summary>
    /// Represents the MIME version choice or state.
    /// </summary>
    MimeVersion = 2,
    /// <summary>
    /// Represents the content type choice or state.
    /// </summary>
    ContentType = 3,
    /// <summary>
    /// Represents the transfer encoding choice or state.
    /// </summary>
    TransferEncoding = 4,
    /// <summary>
    /// Represents the boundary choice or state.
    /// </summary>
    Boundary = 5,
    /// <summary>
    /// Represents the charset choice or state.
    /// </summary>
    Charset = 6,
    /// <summary>
    /// Represents the filename choice or state.
    /// </summary>
    Filename = 7
}
