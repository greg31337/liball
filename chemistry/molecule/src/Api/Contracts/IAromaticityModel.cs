using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="AromaticityModel"/>.
/// </summary>
public interface IAromaticityModel
{
    /// <summary>
    /// Returns the currently assigned aromaticity state for one atom.
    /// </summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The assigned aromaticity state.</returns>
    Aromaticity GetAromaticity(Molecule? molecule, int atomId);

    /// <summary>
    /// Runs valence, aromaticity, conjugation, hybridization, hydrogen, and stereo perception in dependency order.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    AromaticityPerceptionResult Perceive(Molecule? molecule);

    /// <summary>
    /// Perceives the in place.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    AromaticityPerceptionResult PerceiveInPlace(Molecule? molecule);

    /// <summary>
    /// Perceives aromaticity on a copy using the selected model and fallback policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The aromaticity options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assignments, rejected rings, fallback state, and diagnostics.</returns>
    AromaticityPerceptionResult PerceiveWithOptions(Molecule? molecule, AromaticityOptions? options);

    /// <summary>
    /// Converts valid Kekulé ring systems to aromatic atoms and bonds on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The aromaticity options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assignments, rejected rings, fallback state, and diagnostics.</returns>
    AromaticityPerceptionResult Dekekulize(Molecule? molecule, AromaticityOptions? options);

    /// <summary>
    /// Converts valid Kekulé ring systems to aromatic atoms and bonds in the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The aromaticity options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assignments, rejected rings, fallback state, and diagnostics.</returns>
    AromaticityPerceptionResult DekekulizeInPlace(Molecule? molecule, AromaticityOptions? options);
}
