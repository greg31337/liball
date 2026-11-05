using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="ChargeNormalizer"/>.
/// </summary>
public interface IChargeNormalizer
{
    /// <summary>
    /// Returns a normalized copy and leaves the input molecule unchanged.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule Normalize(Molecule? molecule);

    /// <summary>
    /// Normalizes the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    ChargeNormalizationResult NormalizeWithResult(Molecule? molecule);
}

