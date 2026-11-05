using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by radical cleanup.
/// </summary>
public class RadicalCleanupResult
{
    /// <summary>
    /// The molecule.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// The cleared atom identifiers.
    /// </summary>
    public List<int> ClearedAtomIds;
    /// <summary>
    /// The preserved atom identifiers.
    /// </summary>
    public List<int> PreservedAtomIds;
    /// <summary>
    /// The invalid atom identifiers.
    /// </summary>
    public List<int> InvalidAtomIds;
    /// <summary>
    /// The cleared radical count.
    /// </summary>
    public int ClearedRadicalCount;
    /// <summary>
    /// The preserved radical count.
    /// </summary>
    public int PreservedRadicalCount;
    /// <summary>
    /// The invalid radical count.
    /// </summary>
    public int InvalidRadicalCount;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadicalCleanupResult"/> class.
    /// </summary>
    public RadicalCleanupResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        ClearedAtomIds = new List<int>();
        PreservedAtomIds = new List<int>();
        InvalidAtomIds = new List<int>();
        ClearedRadicalCount = 0;
        PreservedRadicalCount = 0;
        InvalidRadicalCount = 0;
        Changed = false;
        Success = true;
    }
}

