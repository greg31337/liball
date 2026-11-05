namespace odf;

/// <summary>
/// Analyzes ODF version gates, migrations, and downgrade loss.
/// </summary>
public interface IOdfVersionService
{
    /// <summary>
    /// Validates document constructs against the version selected by the options.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="options">The validation and target-version options.</param>
    /// <returns>The version analysis.</returns>
    public OdfVersionAnalysis ValidateDocumentVersion(
        OdfDocument document,
        OdfValidationOptions options);
    /// <summary>
    /// Analyzes migration from the document version to a target version.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="targetVersion">The requested target version.</param>
    /// <returns>The ordered migration analysis and loss diagnostics.</returns>
    public OdfVersionAnalysis AnalyzeVersionMigration(
        OdfDocument document,
        OdfVersion targetVersion);
}
