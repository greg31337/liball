namespace avif;

/// <summary>Contains a one-call container inspection result.</summary>
public class AvifReadResult : IAvifReadResult, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifDocument? document;
    AvifCapabilityReport capabilities;

    /// <summary>Creates an inspection result for an adapter or test implementation.</summary>
    /// <param name="code">The inspection outcome.</param>
    /// <param name="document">The parsed document, or null when unavailable.</param>
    public AvifReadResult(AvifResultCode code, AvifDocument? document)
    {
        this.code = code;
        this.document = document;
        capabilities = new AvifCapabilityReport(null, document);
    }

    /// <summary>Returns the inspection outcome.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the parsed document, including findings on failure.</summary>
    /// <returns>The document, or null when no source could be read.</returns>
    public AvifDocument? GetDocument()
    {
        return document;
    }

    /// <summary>Returns detected document features and their supported operations.</summary>
    /// <returns>The capability report, which is empty when no document is available.</returns>
    public AvifCapabilityReport GetCapabilities()
    {
        return capabilities;
    }

    /// <summary>Returns diagnostics produced while inspecting the source.</summary>
    /// <returns>The structured inspection diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromDocument(
            document,
            code,
            AvifDiagnosticCategory.Validation,
            "The AVIF source inspection did not complete successfully.");
    }
}
