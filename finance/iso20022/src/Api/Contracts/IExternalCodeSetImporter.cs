using System.IO;

namespace iso20022;

/// <summary>Normalizes and cross-checks official JSON, XSD, and XLSX external-code artifacts.</summary>
public interface IExternalCodeSetImporter
{
    /// <summary>Imports one raw or archived official JSON external-code artifact.</summary>
    /// <param name="input">The source content stream.</param><param name="request">The import provenance and limits.</param><returns>The normalized import result.</returns>
    ExternalCodeImportResult ImportJsonStream(Stream? input, ExternalCodeImportRequest? request);
    /// <summary>Imports one raw or archived official XSD external-code artifact.</summary>
    /// <param name="input">The source content stream.</param><param name="request">The import provenance and limits.</param><returns>The normalized import result.</returns>
    ExternalCodeImportResult ImportXsdStream(Stream? input, ExternalCodeImportRequest? request);
    /// <summary>Imports one raw or archived official XLSX external-code artifact.</summary>
    /// <param name="input">The source content stream.</param><param name="request">The import provenance and limits.</param><returns>The normalized import result.</returns>
    ExternalCodeImportResult ImportXlsxStream(Stream? input, ExternalCodeImportRequest? request);
    /// <summary>Cross-checks normalized JSON, XSD, and XLSX catalogues without selecting a preferred format.</summary>
    /// <param name="request">The three normalized catalogues and comparison limits.</param><returns>The deterministic comparison result.</returns>
    ExternalCodeCatalogComparisonResult CompareCatalogs(ExternalCodeCatalogComparisonRequest? request);
}
