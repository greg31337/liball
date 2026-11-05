namespace molecule;


/// <summary>
/// Defines copy-based stereo editing operations provided by <see cref="StereoEditor"/>.
/// </summary>
public interface IStereoEditor
{
    /// <summary>
    /// Creates a validated tetrahedral stereo record on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="centerAtomId">The tetrahedral center atom identifier.</param>
    /// <param name="ligandAtomIds">Exactly four ligand identifiers, with at most one negative identifier for an implicit ligand.</param>
    /// <param name="chirality">The specified clockwise or counterclockwise chirality.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult CreateTetrahedralStereo(Molecule? molecule, int centerAtomId, int[]? ligandAtomIds, Chirality chirality);

    /// <summary>
    /// Creates a validated double-bond stereo record on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoBondId">The double-bond identifier.</param>
    /// <param name="beginLigandAtomId">The begin-side ligand identifier, or a negative value for an implicit hydrogen.</param>
    /// <param name="endLigandAtomId">The end-side ligand identifier, or a negative value for an implicit hydrogen.</param>
    /// <param name="configuration">The specified E, Z, cis, or trans configuration.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult CreateDoubleBondStereo(Molecule? molecule, int stereoBondId, int beginLigandAtomId, int endLigandAtomId, DoubleBondConfiguration configuration);

    /// <summary>
    /// Creates an unspecified typed allene stereo placeholder on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="centerAtomId">The central allene atom identifier.</param>
    /// <param name="carrierAtomIds">Known carrier atom identifiers in caller-defined order.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult CreateAlleneStereoPlaceholder(Molecule? molecule, int centerAtomId, int[]? carrierAtomIds);

    /// <summary>
    /// Creates an unspecified typed atropisomeric stereo placeholder on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="axisBondId">The atropisomeric axis bond identifier.</param>
    /// <param name="carrierAtomIds">Known carrier atom identifiers in caller-defined order.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult CreateAtropisomericStereoPlaceholder(Molecule? molecule, int axisBondId, int[]? carrierAtomIds);

    /// <summary>
    /// Creates an unspecified typed placeholder for a future non-tetrahedral atom-centered stereo form.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="centerAtomId">The stereo focus atom identifier.</param>
    /// <param name="carrierAtomIds">Known carrier atom identifiers in caller-defined order.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult CreateNonTetrahedralStereoPlaceholder(Molecule? molecule, int centerAtomId, int[]? carrierAtomIds);

    /// <summary>
    /// Inverts a specified tetrahedral, double-bond, or supported axial orientation on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult InvertStereo(Molecule? molecule, int stereoRecordId);

    /// <summary>
    /// Marks a tetrahedral, double-bond, or typed placeholder record as unspecified on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult MarkStereoUnspecified(Molecule? molecule, int stereoRecordId);

    /// <summary>
    /// Removes a stereo record and repairs enhanced stereo group identifiers on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to copy and edit.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The edited copy and diagnostics.</returns>
    StereoEditResult RemoveStereo(Molecule? molecule, int stereoRecordId);
}
