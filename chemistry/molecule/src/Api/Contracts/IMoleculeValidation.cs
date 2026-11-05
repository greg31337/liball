namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="MoleculeValidation"/>.
/// </summary>
public interface IMoleculeValidation
{
    /// <summary>
    /// Returns the report severity corresponding to a diagnostic.
    /// </summary>
    /// <param name="error">The diagnostic to inspect.</param>
    /// <returns>The corresponding report severity.</returns>
    ReportSeverity GetReportSeverity(MoleculeError? error);

    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    MoleculeReport Validate(Molecule? molecule);

    /// <summary>
    /// Checks a molecule using configurable empty policy and optional parser-originated diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The combined parser and structural validation diagnostics.</returns>
    MoleculeReport ValidateWithOptions(Molecule? molecule, MoleculeValidationOptions? options);

    /// <summary>
    /// Checks a query graph and its disconnected component-group constraints.
    /// </summary>
    /// <param name="query">The query graph to process.</param>
    /// <returns>The query validation diagnostics.</returns>
    MoleculeReport ValidateQuery(QueryMolecule? query);
}
