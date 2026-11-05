using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Provides component info data and operations for molecular graphs.
/// </summary>
public class ComponentInfo
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public int ComponentId;
    /// <summary>
    /// The atom identifiers.
    /// </summary>
    public List<int> AtomIds;
    /// <summary>
    /// The bond identifiers.
    /// </summary>
    public List<int> BondIds;
    /// <summary>
    /// The heavy atom count.
    /// </summary>
    public int HeavyAtomCount;
    /// <summary>
    /// The atom count.
    /// </summary>
    public int AtomCount;
    /// <summary>
    /// The bond count.
    /// </summary>
    public int BondCount;
    /// <summary>
    /// The first atom identifier.
    /// </summary>
    public int FirstAtomId;
    /// <summary>
    /// The atom key.
    /// </summary>
    public string AtomKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentInfo"/> class.
    /// </summary>
    public ComponentInfo()
    {
        ComponentId = -1;
        AtomIds = new List<int>();
        BondIds = new List<int>();
        HeavyAtomCount = 0;
        AtomCount = 0;
        BondCount = 0;
        FirstAtomId = -1;
        AtomKey = "";
    }
}

