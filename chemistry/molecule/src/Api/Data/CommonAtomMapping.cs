namespace molecule;


/// <summary>
/// Maps one atom in each input molecule to an atom in the returned common substructure.
/// </summary>
public class CommonAtomMapping
{
    /// <summary>
    /// The atom identifier in the left input molecule.
    /// </summary>
    public int LeftAtomId;
    /// <summary>
    /// The atom identifier in the right input molecule.
    /// </summary>
    public int RightAtomId;
    /// <summary>
    /// The atom identifier in the returned common substructure.
    /// </summary>
    public int CommonAtomId;

    /// <summary>
    /// Initializes an unset atom mapping.
    /// </summary>
    public CommonAtomMapping()
    {
        LeftAtomId = -1;
        RightAtomId = -1;
        CommonAtomId = -1;
    }
}
