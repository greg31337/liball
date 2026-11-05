namespace molecule;


/// <summary>
/// Specifies how validation reports a molecule with no atoms.
/// </summary>
public enum EmptyMoleculePolicy
{
    /// <summary>
    /// Reports an error.
    /// </summary>
    Error = 0,
    /// <summary>
    /// Reports a warning.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Accepts the empty molecule without a diagnostic.
    /// </summary>
    Allow = 2
}
