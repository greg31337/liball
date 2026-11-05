namespace barcode;

/// <summary>
/// Holds an ordered GS1 message, detected syntax, symbology identifier, and issues.
/// </summary>
public class Gs1Message
{
    /// <summary>
    /// The parsed elements in source order.
    /// </summary>
    public Gs1Element[]? Elements;

    /// <summary>
    /// The detected source syntax.
    /// </summary>
    public Gs1Syntax Syntax;

    /// <summary>
    /// The scanner symbology identifier when one was present.
    /// </summary>
    public string? SymbologyIdentifier;

    /// <summary>
    /// The original source text when available.
    /// </summary>
    public string? SourceText;

    /// <summary>
    /// The syntax and semantic issues in stable source order.
    /// </summary>
    public BarcodeValidationIssue[]? Issues;
}
