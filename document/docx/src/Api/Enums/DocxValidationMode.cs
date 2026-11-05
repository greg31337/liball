namespace docx;

/// <summary>
/// Specifies the validation performed after a package is loaded.
/// </summary>
public enum DocxValidationMode
{
    /// <summary>
    /// Skips load-time validation.
    /// </summary>
    None = 0,
    /// <summary>
    /// Validates OPC package structure, content types, relationships, and XML.
    /// </summary>
    Package = 1,
    /// <summary>
    /// Validates the package and supported WordprocessingML requirements.
    /// </summary>
    Full = 2
}
