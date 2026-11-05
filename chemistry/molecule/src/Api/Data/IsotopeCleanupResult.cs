using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by isotope cleanup.
/// </summary>
public class IsotopeCleanupResult
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
    /// The cleared isotope count.
    /// </summary>
    public int ClearedIsotopeCount;
    /// <summary>
    /// The preserved isotope count.
    /// </summary>
    public int PreservedIsotopeCount;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsotopeCleanupResult"/> class.
    /// </summary>
    public IsotopeCleanupResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        ClearedAtomIds = new List<int>();
        PreservedAtomIds = new List<int>();
        ClearedIsotopeCount = 0;
        PreservedIsotopeCount = 0;
        Changed = false;
        Success = true;
    }
}

