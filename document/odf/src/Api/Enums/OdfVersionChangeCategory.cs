namespace odf;

/// <summary>
/// Identifies the schema construct affected by an ODF version change.
/// </summary>
public enum OdfVersionChangeCategory
{
    /// <summary>
    /// No schema category is known.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A named grammar definition changed.
    /// </summary>
    Definition = 1,
    /// <summary>
    /// An element rule changed.
    /// </summary>
    Element = 2,
    /// <summary>
    /// An attribute rule changed.
    /// </summary>
    Attribute = 3,
    /// <summary>
    /// A datatype rule changed.
    /// </summary>
    Datatype = 4
}
