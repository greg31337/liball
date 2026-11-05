using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides residue data and operations for molecular graphs.
/// </summary>
public class Residue
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The chain identifier.
    /// </summary>
    public string ChainId;
    /// <summary>
    /// The type.
    /// </summary>
    public string Type;
    /// <summary>
    /// The insertion code that distinguishes residues with the same sequence identifier.
    /// </summary>
    public string InsertionCode;
    /// <summary>
    /// The model identifier, or <c>-1</c> when the residue is not model-specific.
    /// </summary>
    public int ModelId;
    /// <summary>
    /// The root atom identifier.
    /// </summary>
    public int RootAtomId;
    /// <summary>
    /// The atom identifiers.
    /// </summary>
    public List<int> AtomIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="Residue"/> class.
    /// </summary>
    public Residue()
    {
        Id = -1;
        Name = "";
        ChainId = "";
        Type = "";
        InsertionCode = "";
        ModelId = -1;
        RootAtomId = -1;
        AtomIds = new List<int>();
    }
}
