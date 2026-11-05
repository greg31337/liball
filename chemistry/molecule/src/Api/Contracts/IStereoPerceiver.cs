using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="StereoPerceiver"/>.
/// </summary>
public interface IStereoPerceiver
{
    /// <summary>
    /// Perceives the from coordinates.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    StereoPerceptionResult PerceiveFromCoordinates(Molecule? molecule);

    /// <summary>
    /// Perceives the from molfile wedges.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    StereoPerceptionResult PerceiveFromMolfileWedges(Molecule? molecule);

    /// <summary>
    /// Perceives the from SMILES.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    StereoPerceptionResult PerceiveFromSmiles(Molecule? molecule, string? smiles);
}

