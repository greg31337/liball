namespace barcode;

/// <summary>
/// Parses, validates, converts, and looks up GS1 data.
/// </summary>
public class Gs1DataProcessor : IGs1DataProcessor
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a GS1 data processor backed by the shared operation dispatcher.
    /// </summary>
    public Gs1DataProcessor()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Parses and converts GS1 data according to the requested syntaxes and policy.
    /// </summary>
    /// <param name="request">The source data, syntaxes, and processing policy.</param>
    /// <returns>The parsed message and requested serialization.</returns>
    /// <exception cref="Gs1SyntaxException">GS1 processing is unavailable or the source cannot be processed.</exception>
    public Gs1ProcessResult ProcessGs1Data(Gs1ProcessRequest request)
    {
        return Dispatcher.ProcessGs1Data(request);
    }

    /// <summary>
    /// Looks up a GS1 Application Identifier definition.
    /// </summary>
    /// <param name="applicationIdentifier">The exact Application Identifier to look up.</param>
    /// <returns>The matching definition, or null when no definition is assigned.</returns>
    public Gs1ApplicationIdentifierInfo? LookupGs1ApplicationIdentifier(string applicationIdentifier)
    {
        return Dispatcher.LookupGs1ApplicationIdentifier(applicationIdentifier);
    }
}
