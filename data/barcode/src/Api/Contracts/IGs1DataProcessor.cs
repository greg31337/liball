namespace barcode;

/// <summary>
/// Parses, validates, converts, and looks up GS1 data.
/// </summary>
public interface IGs1DataProcessor
{
    /// <summary>
    /// Parses and converts GS1 data according to the requested syntaxes and policy.
    /// </summary>
    /// <param name="request">The source data, syntaxes, and processing policy.</param>
    /// <returns>The parsed message and requested serialization.</returns>
    Gs1ProcessResult ProcessGs1Data(Gs1ProcessRequest request);

    /// <summary>
    /// Looks up a GS1 Application Identifier definition.
    /// </summary>
    /// <param name="applicationIdentifier">The exact Application Identifier to look up.</param>
    /// <returns>The matching definition, or null when no definition is assigned.</returns>
    Gs1ApplicationIdentifierInfo? LookupGs1ApplicationIdentifier(string applicationIdentifier);
}
