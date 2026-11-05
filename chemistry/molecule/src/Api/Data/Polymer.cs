using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Groups chains and residues that form a polymer independently of any file format.
/// </summary>
public class Polymer
{
    /// <summary>
    /// The polymer identifier.
    /// </summary>
    public int Id;
    /// <summary>
    /// The polymer name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The polymer type.
    /// </summary>
    public string Type;
    /// <summary>
    /// The chain identifiers belonging to the polymer.
    /// </summary>
    public List<string> ChainIds;
    /// <summary>
    /// The residue identifiers belonging to the polymer.
    /// </summary>
    public List<int> ResidueIds;

    /// <summary>
    /// Initializes an empty polymer.
    /// </summary>
    public Polymer()
    {
        Id = -1;
        Name = "";
        Type = "";
        ChainIds = new List<string>();
        ResidueIds = new List<int>();
    }
}
