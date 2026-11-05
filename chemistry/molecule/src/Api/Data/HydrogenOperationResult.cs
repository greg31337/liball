using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the molecule, calculations, changes, and diagnostics produced by a hydrogen operation.
/// </summary>
public class HydrogenOperationResult
{
    /// <summary>
    /// The independently owned result molecule.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced while processing the molecule.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Per-atom hydrogen calculations in atom identifier order before mutation.
    /// </summary>
    public List<HydrogenCalculationResult> Calculations;
    /// <summary>
    /// The number of explicit graph hydrogen atoms added.
    /// </summary>
    public int AddedHydrogenCount;
    /// <summary>
    /// The number of explicit graph hydrogen atoms removed.
    /// </summary>
    public int RemovedHydrogenCount;
    /// <summary>
    /// The number of atoms whose implicit hydrogen count changed.
    /// </summary>
    public int UpdatedImplicitHydrogenCount;
    /// <summary>
    /// The number of isotopic graph hydrogen atoms preserved by the operation.
    /// </summary>
    public int PreservedIsotopicHydrogenCount;
    /// <summary>
    /// Whether the result molecule differs from the input molecule.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes an empty successful hydrogen operation result.
    /// </summary>
    public HydrogenOperationResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Calculations = new List<HydrogenCalculationResult>();
        AddedHydrogenCount = 0;
        RemovedHydrogenCount = 0;
        UpdatedImplicitHydrogenCount = 0;
        PreservedIsotopicHydrogenCount = 0;
        Changed = false;
        Success = true;
    }
}
