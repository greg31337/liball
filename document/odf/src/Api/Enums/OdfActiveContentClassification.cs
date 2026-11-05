namespace odf;

/// <summary>
/// Classifies inert descriptions that could activate code or document actions.
/// </summary>
public enum OdfActiveContentClassification
{
    /// <summary>The content could not be classified.</summary>
    Unknown = 0,
    /// <summary>The description has no executable target or action.</summary>
    Inert = 1,
    /// <summary>Code is embedded in the XML document or package.</summary>
    EmbeddedCode = 2,
    /// <summary>An application macro may be invoked.</summary>
    MacroInvocation = 3,
    /// <summary>A package-relative or fragment script target may be invoked.</summary>
    LinkedScript = 4,
    /// <summary>An external script or action target may be invoked.</summary>
    ExternalReference = 5,
    /// <summary>A presentation action may be performed.</summary>
    PresentationAction = 6
}
