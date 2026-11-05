namespace http;

/// <summary>
/// Identifies the HPACK indexing representation used for a header field.
/// </summary>
public enum HpackIndexingMode
{
    /// <summary>
    /// Insert the field into the dynamic table.
    /// </summary>
    Incremental = 0,

    /// <summary>
    /// Encode the field without changing the dynamic table.
    /// </summary>
    WithoutIndexing = 1,

    /// <summary>
    /// Encode the field as never indexed.
    /// </summary>
    NeverIndexed = 2,

    /// <summary>
    /// Let the encoder choose the representation.
    /// </summary>
    Automatic = 3
}
