namespace molecule;


/// <summary>
/// Controls empty-molecule validation and supplies parser-originated diagnostics for propagation.
/// </summary>
public class MoleculeValidationOptions
{
    /// <summary>
    /// The policy applied when the molecule contains no atoms.
    /// </summary>
    public EmptyMoleculePolicy EmptyMoleculePolicy;
    /// <summary>
    /// Parser-originated diagnostics to copy into the validation report, or <c>null</c> when unavailable.
    /// </summary>
    public MoleculeReport? ParserReport;

    /// <summary>
    /// Initializes validation options that report empty molecules as errors.
    /// </summary>
    public MoleculeValidationOptions()
    {
        EmptyMoleculePolicy = EmptyMoleculePolicy.Error;
        ParserReport = null;
    }
}
