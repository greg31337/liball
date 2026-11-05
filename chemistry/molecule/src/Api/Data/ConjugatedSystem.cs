using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides conjugated system data and operations for molecular graphs.
/// </summary>
public class ConjugatedSystem
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The atom identifiers.
    /// </summary>
    public List<int> AtomIds;
    /// <summary>
    /// The bond identifiers.
    /// </summary>
    public List<int> BondIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConjugatedSystem"/> class.
    /// </summary>
    public ConjugatedSystem()
    {
        Id = -1;
        AtomIds = new List<int>();
        BondIds = new List<int>();
    }
}

