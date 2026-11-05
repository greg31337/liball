namespace avif;

/// <summary>Contains a one-call validation result and its ordered findings.</summary>
public class AvifValidationResult : IAvifValidationResult, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifDocument? document;

    /// <summary>Creates a validation result for an adapter or test implementation.</summary>
    /// <param name="code">The validation outcome.</param>
    /// <param name="document">The inspected document, or null when unavailable.</param>
    public AvifValidationResult(AvifResultCode code, AvifDocument? document)
    {
        this.code = code;
        this.document = document;
    }

    /// <summary>Returns the validation outcome.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the inspected document.</summary>
    /// <returns>The document, including findings produced before failure.</returns>
    public AvifDocument? GetDocument()
    {
        return document;
    }

    /// <summary>Returns the number of validation findings.</summary>
    /// <returns>The finding count.</returns>
    public int GetFindingCount()
    {
        return document == null ? 0 : document.GetFindingCount();
    }

    /// <summary>Returns one validation finding.</summary>
    /// <param name="index">The zero-based finding index.</param>
    /// <returns>The finding, or null for an invalid index.</returns>
    public AvifDiagnostic? GetFinding(int index)
    {
        return document == null ? null : document.GetFinding(index);
    }

    /// <summary>Returns all structured validation diagnostics.</summary>
    /// <returns>The validation diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromDocument(
            document,
            code,
            AvifDiagnosticCategory.Validation,
            "The AVIF validation operation did not complete successfully.");
    }
}
