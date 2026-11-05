using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by metal disconnection.
/// </summary>
public class MetalDisconnectionResult
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
    /// The removed bond identifiers.
    /// </summary>
    public List<int> RemovedBondIds;
    /// <summary>
    /// The metal atom identifiers.
    /// </summary>
    public List<int> MetalAtomIds;
    /// <summary>
    /// The ligand atom identifiers.
    /// </summary>
    public List<int> LigandAtomIds;
    /// <summary>
    /// The disconnection count.
    /// </summary>
    public int DisconnectionCount;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetalDisconnectionResult"/> class.
    /// </summary>
    public MetalDisconnectionResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        RemovedBondIds = new List<int>();
        MetalAtomIds = new List<int>();
        LigandAtomIds = new List<int>();
        DisconnectionCount = 0;
        Changed = false;
        Success = true;
    }
}

