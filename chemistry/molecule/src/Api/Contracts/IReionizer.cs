using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Reionizer"/>.
/// </summary>
public interface IReionizer
{
    /// <summary>
    /// Returns a copy after deterministic proton transfers between recognized acid and base sites.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Reionize(Molecule? molecule);

    /// <summary>
    /// Reionizes the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    ReionizationResult ReionizeWithResult(Molecule? molecule);
}

