using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Conjugation"/>.
/// </summary>
public interface IConjugation
{
    /// <summary>
    /// Assign.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    ConjugationResult Assign(Molecule? molecule);
}

