using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="TautomerCanonicalizer"/>.
/// </summary>
public interface ITautomerCanonicalizer
{
    /// <summary>
    /// Selects the highest-scoring tautomer from the bounded enumeration.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Canonicalize(Molecule? molecule, TautomerEnumerationOptions? options);

    /// <summary>
    /// Canonicalizes the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    TautomerCanonicalizationResult CanonicalizeWithResult(Molecule? molecule, TautomerEnumerationOptions? options);
}

