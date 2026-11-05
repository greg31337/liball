namespace molecule;


/// <summary>
/// Identifies the typed form represented by a stereo record.
/// </summary>
public enum StereoKind
{
    /// <summary>
    /// The stereo form has not been identified.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Atom-centered tetrahedral stereochemistry.
    /// </summary>
    Tetrahedral = 1,
    /// <summary>
    /// Double-bond stereochemistry.
    /// </summary>
    DoubleBond = 2,
    /// <summary>
    /// Axial stereochemistry around an allene center.
    /// </summary>
    Allene = 3,
    /// <summary>
    /// Atropisomeric stereochemistry around a bond axis.
    /// </summary>
    Atropisomeric = 4,
    /// <summary>
    /// A future atom-centered non-tetrahedral stereo form.
    /// </summary>
    NonTetrahedral = 5
}
