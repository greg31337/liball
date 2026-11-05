using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Identifies one model in a multi-model molecular structure.
/// </summary>
public class MoleculeModel
{
    /// <summary>
    /// The model identifier.
    /// </summary>
    public int Id;
    /// <summary>
    /// The model name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The atom identifiers belonging to the model.
    /// </summary>
    public List<int> AtomIds;
    /// <summary>
    /// The conformer identifiers belonging to the model.
    /// </summary>
    public List<int> ConformerIds;

    /// <summary>
    /// Initializes an empty molecular model.
    /// </summary>
    public MoleculeModel()
    {
        Id = -1;
        Name = "";
        AtomIds = new List<int>();
        ConformerIds = new List<int>();
    }
}
