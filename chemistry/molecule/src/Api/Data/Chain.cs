using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides chain data and operations for molecular graphs.
/// </summary>
public class Chain
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public string Id;
    /// <summary>
    /// The residue identifiers.
    /// </summary>
    public List<int> ResidueIds;
    /// <summary>
    /// The model identifier, or <c>-1</c> when the chain is not model-specific.
    /// </summary>
    public int ModelId;

    /// <summary>
    /// Initializes a new instance of the <see cref="Chain"/> class.
    /// </summary>
    public Chain()
    {
        Id = "";
        ResidueIds = new List<int>();
        ModelId = -1;
    }
}
