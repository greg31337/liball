namespace barcode;

/// <summary>
/// Holds one parsed GS1 Application Identifier element and its source context.
/// </summary>
public class Gs1Element
{
    /// <summary>
    /// The recognized Application Identifier.
    /// </summary>
    public string? ApplicationIdentifier;

    /// <summary>
    /// The element data following the Application Identifier.
    /// </summary>
    public string? Value;

    /// <summary>
    /// The component values after dictionary-driven component slicing.
    /// </summary>
    public string[]? Components;

    /// <summary>
    /// The matching public Application Identifier definition.
    /// </summary>
    public Gs1ApplicationIdentifierInfo? Definition;

    /// <summary>
    /// The zero-based source offset of the Application Identifier.
    /// </summary>
    public int SourceOffset;

    /// <summary>
    /// The number of source characters occupied by the complete element.
    /// </summary>
    public int SourceLength;

    /// <summary>
    /// Indicates whether the element uses variable-length data.
    /// </summary>
    public bool VariableLength;
}
