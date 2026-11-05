using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides ring system data and operations for molecular graphs.
/// </summary>
public class RingSystem
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The ring identifiers.
    /// </summary>
    public List<int> RingIds;
    /// <summary>
    /// The atom identifiers.
    /// </summary>
    public List<int> AtomIds;
    /// <summary>
    /// The bond identifiers.
    /// </summary>
    public List<int> BondIds;
    /// <summary>
    /// The fused bond identifiers.
    /// </summary>
    public List<int> FusedBondIds;
    /// <summary>
    /// Whether the value is fused.
    /// </summary>
    public bool IsFused;
    /// <summary>
    /// The atom identifiers that act as bridgeheads in a bridged ring system.
    /// </summary>
    public List<int> BridgeheadAtomIds;
    /// <summary>
    /// Whether the system contains rings sharing a multi-bond path or multiple non-bonded atoms.
    /// </summary>
    public bool IsBridged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RingSystem"/> class.
    /// </summary>
    public RingSystem()
    {
        Id = -1;
        RingIds = new List<int>();
        AtomIds = new List<int>();
        BondIds = new List<int>();
        FusedBondIds = new List<int>();
        IsFused = false;
        BridgeheadAtomIds = new List<int>();
        IsBridged = false;
    }
}
