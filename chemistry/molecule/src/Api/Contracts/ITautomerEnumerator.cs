using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="TautomerEnumerator"/>.
/// </summary>
public interface ITautomerEnumerator
{
    /// <summary>
    /// Generates unique tautomers up to the configured candidate and transform limits.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<Molecule> Enumerate(Molecule? molecule, TautomerEnumerationOptions? options);

    /// <summary>
    /// Enumerates the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    TautomerEnumerationResult EnumerateWithResult(Molecule? molecule, TautomerEnumerationOptions? options);
}

