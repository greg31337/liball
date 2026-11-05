using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Kekulizer"/>.
/// </summary>
public interface IKekulizer
{
    /// <summary>
    /// Returns a copy whose supported aromatic bonds have deterministic alternating bond orders.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    KekulizationResult Kekulize(Molecule? molecule);

    /// <summary>
    /// Assigns deterministic alternating bond orders to supported aromatic systems in the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    KekulizationResult KekulizeInPlace(Molecule? molecule);

    /// <summary>
    /// Returns a copy with Kekulé bond orders assigned under the selected aromaticity model and fallback policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The Kekulization options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assigned bonds, failures, fallback state, and diagnostics.</returns>
    KekulizationResult KekulizeWithOptions(Molecule? molecule, KekulizationOptions? options);
}
