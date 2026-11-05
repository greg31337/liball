using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Controls mapped deterministic conformer alignment.
/// </summary>
public class ConformerAlignmentOptions
{
    /// <summary>
    /// The explicit moving-to-reference atom mappings; an empty list maps common assigned indices.
    /// </summary>
    public List<ConformerAtomMapping> AtomMappings;
    /// <summary>
    /// The uniform scale policy used during alignment.
    /// </summary>
    public ConformerAlignmentScalePolicy ScalePolicy;

    /// <summary>
    /// Initializes identity-index mapping with rotation and translation only.
    /// </summary>
    public ConformerAlignmentOptions()
    {
        AtomMappings = new List<ConformerAtomMapping>();
        ScalePolicy = ConformerAlignmentScalePolicy.Preserve;
    }
}
