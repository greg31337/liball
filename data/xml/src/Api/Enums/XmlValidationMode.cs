namespace xml;

/// <summary>
/// Specifies the validation performed while processing XML.
/// </summary>
public enum XmlValidationMode
{
    /// <summary>
    /// Does not perform validation.
    /// </summary>
    None = 0,
    /// <summary>
    /// Performs DTD validation.
    /// </summary>
    Dtd = 1,
    /// <summary>
    /// Performs XML Schema validation.
    /// </summary>
    Schema = 2
}
