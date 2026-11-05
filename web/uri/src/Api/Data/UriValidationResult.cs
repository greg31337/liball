namespace uri;

/// <summary>
/// Describes the exception-less outcome of validating URI text or components.
/// </summary>
public class UriValidationResult
{
    /// <summary>Whether the reference satisfies the selected grammar and requirements.</summary>
    public bool Success;

    /// <summary>The validated reference, or an empty reference when no value was produced.</summary>
    public UriReference Reference;

    /// <summary>The first diagnostic, or <c>null</c> after successful validation.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>All diagnostics requested by the operation, in source order.</summary>
    public UriDiagnostic[] Diagnostics;

    /// <summary>
    /// Initializes a URI validation result.
    /// </summary>
    /// <param name="success">Whether validation succeeded.</param>
    /// <param name="reference">The validated reference outcome.</param>
    /// <param name="diagnostic">The first diagnostic.</param>
    /// <param name="diagnostics">All requested diagnostics.</param>
    public UriValidationResult(
        bool success,
        UriReference reference,
        UriDiagnostic? diagnostic,
        UriDiagnostic[] diagnostics)
    {
        Success = success;
        Reference = reference;
        Diagnostic = diagnostic;
        Diagnostics = diagnostics;
    }
}
