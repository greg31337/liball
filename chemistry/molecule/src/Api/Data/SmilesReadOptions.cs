namespace molecule;


/// <summary>
/// Controls strict and recovery behavior for SMILES parsing.
/// </summary>
public class SmilesReadOptions
{
    /// <summary>
    /// Whether malformed input is repaired when a supported deterministic recovery is available.
    /// </summary>
    public bool EnableRecovery;

    /// <summary>
    /// Initializes strict SMILES parsing options.
    /// </summary>
    public SmilesReadOptions()
    {
        EnableRecovery = false;
    }
}
