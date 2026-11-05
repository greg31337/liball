namespace molecule;


/// <summary>
/// Maps a coordinate in a moving conformer to a coordinate in a reference conformer.
/// </summary>
public class ConformerAtomMapping
{
    /// <summary>
    /// The atom identifier in the moving conformer.
    /// </summary>
    public int MovingAtomId;
    /// <summary>
    /// The atom identifier in the reference conformer.
    /// </summary>
    public int ReferenceAtomId;

    /// <summary>
    /// Initializes one moving-to-reference atom mapping.
    /// </summary>
    /// <param name="movingAtomId">The moving conformer atom identifier.</param>
    /// <param name="referenceAtomId">The reference conformer atom identifier.</param>
    public ConformerAtomMapping(int movingAtomId, int referenceAtomId)
    {
        MovingAtomId = movingAtomId;
        ReferenceAtomId = referenceAtomId;
    }
}
