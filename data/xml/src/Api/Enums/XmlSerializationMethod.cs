namespace xml;

/// <summary>
/// Specifies a Serialization 3.1 output method.
/// </summary>
public enum XmlSerializationMethod
{
    /// <summary>
    /// Uses the XML output method.
    /// </summary>
    Xml = 0,
    /// <summary>
    /// Uses the XHTML output method.
    /// </summary>
    Xhtml = 1,
    /// <summary>
    /// Uses the HTML output method.
    /// </summary>
    Html = 2,
    /// <summary>
    /// Uses the text output method.
    /// </summary>
    Text = 3,
    /// <summary>
    /// Uses the JSON output method.
    /// </summary>
    Json = 4,
    /// <summary>
    /// Uses the adaptive output method.
    /// </summary>
    Adaptive = 5,
    /// <summary>
    /// Uses a caller-provided output method.
    /// </summary>
    Extension = 6
}
