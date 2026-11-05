namespace barcode;

/// <summary>
/// Holds GS1 source data, source and target syntaxes, and processing policy.
/// </summary>
public class Gs1ProcessRequest
{
    /// <summary>
    /// The source text when processing a textual GS1 syntax.
    /// </summary>
    public string? SourceText;

    /// <summary>
    /// The source bytes when processing barcode or scanner data.
    /// </summary>
    public byte[]? SourceBytes;

    /// <summary>
    /// The declared source syntax.
    /// </summary>
    public Gs1Syntax SourceSyntax;

    /// <summary>
    /// The requested target syntax.
    /// </summary>
    public Gs1Syntax TargetSyntax;

    /// <summary>
    /// Indicates whether unknown or future Application Identifiers are accepted.
    /// </summary>
    public bool AllowUnknownApplicationIdentifiers;

    /// <summary>
    /// Indicates whether required, excluded, repeated, and qualifier associations are validated.
    /// </summary>
    public bool ValidateAssociations;

    /// <summary>
    /// The base URI used when producing a GS1 Digital Link URI.
    /// </summary>
    public string? DigitalLinkBaseUri;
}
