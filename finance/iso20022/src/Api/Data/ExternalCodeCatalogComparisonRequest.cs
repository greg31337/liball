using System.Threading;

namespace iso20022;

/// <summary>Requests a deterministic cross-check of one JSON, XSD, and XLSX external-code edition.</summary>
public class ExternalCodeCatalogComparisonRequest
{
    /// <summary>The normalized official JSON catalogue.</summary>
    public ExternalCodeCatalog? JsonCatalog;
    /// <summary>The normalized official XSD catalogue.</summary>
    public ExternalCodeCatalog? XsdCatalog;
    /// <summary>The normalized official XLSX catalogue.</summary>
    public ExternalCodeCatalog? XlsxCatalog;
    /// <summary>The maximum number of disagreements retained in the result.</summary>
    public int MaxDisagreementCount = Iso20022Limits.MaxDiagnosticCount;
    /// <summary>The cancellation request observed during comparison.</summary>
    public CancellationToken CancellationToken;
}
