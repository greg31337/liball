using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="StereoValidator"/>.
/// </summary>
public interface IStereoValidator
{
    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    MoleculeReport Validate(Molecule? molecule);
}

