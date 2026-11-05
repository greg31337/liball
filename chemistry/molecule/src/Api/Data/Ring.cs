using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides ring data and operations for molecular graphs.
/// </summary>
public class Ring
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
    /// The size.
    /// </summary>
    public int Size;
    /// <summary>
    /// Whether the value is aromatic.
    /// </summary>
    public bool IsAromatic;
    /// <summary>
    /// The key.
    /// </summary>
    public string Key;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ring"/> class.
    /// </summary>
    public Ring()
    {
        Id = -1;
        AtomIds = new List<int>();
        BondIds = new List<int>();
        Size = 0;
        IsAromatic = false;
        Key = "";
    }
}

