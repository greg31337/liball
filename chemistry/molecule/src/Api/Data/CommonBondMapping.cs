namespace molecule;


/// <summary>
/// Maps one bond in each input molecule to a bond in the returned common substructure.
/// </summary>
public class CommonBondMapping
{
    /// <summary>
    /// The bond identifier in the left input molecule.
    /// </summary>
    public int LeftBondId;
    /// <summary>
    /// The bond identifier in the right input molecule.
    /// </summary>
    public int RightBondId;
    /// <summary>
    /// The bond identifier in the returned common substructure.
    /// </summary>
    public int CommonBondId;

    /// <summary>
    /// Initializes an unset bond mapping.
    /// </summary>
    public CommonBondMapping()
    {
        LeftBondId = -1;
        RightBondId = -1;
        CommonBondId = -1;
    }
}
