using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SaltRemover"/>.
/// </summary>
public interface ISaltRemover
{
    /// <summary>
    /// Removes the salts.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    Molecule RemoveSalts(Molecule? molecule);

    /// <summary>
    /// Remove saltss the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    SaltRemovalResult RemoveSaltsWithResult(Molecule? molecule);
}

