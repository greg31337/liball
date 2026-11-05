namespace xml;

/// <summary>
/// Specifies the assessed XML Schema validity.
/// </summary>
public enum XmlSchemaValidity
{
    /// <summary>
    /// Indicates that validity is not known.
    /// </summary>
    NotKnown = 0,
    /// <summary>
    /// Indicates valid content.
    /// </summary>
    Valid = 1,
    /// <summary>
    /// Indicates invalid content.
    /// </summary>
    Invalid = 2
}
