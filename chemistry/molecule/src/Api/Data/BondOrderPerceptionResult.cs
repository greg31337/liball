using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains a perceived molecule, per-bond confidence data, and diagnostics.
/// </summary>
public class BondOrderPerceptionResult
{
    /// <summary>
    /// The independent molecule copy containing accepted assignments.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced by perception.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Bond-order proposals in bond identifier order.
    /// </summary>
    public List<BondOrderAssignment> Assignments;
    /// <summary>
    /// Whether perception completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Whether at least one bond order changed in the returned molecule.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// The coordinate dimension actually used.
    /// </summary>
    public BondOrderCoordinateDimension UsedCoordinateDimension;
    /// <summary>
    /// The number of proposals applied to the returned molecule.
    /// </summary>
    public int AppliedCount;
    /// <summary>
    /// The number of evaluated bonds for which no proposal was applied.
    /// </summary>
    public int UnresolvedCount;

    /// <summary>
    /// Initializes an unsuccessful result containing an empty molecule and report.
    /// </summary>
    public BondOrderPerceptionResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Assignments = new List<BondOrderAssignment>();
        Success = false;
        Changed = false;
        UsedCoordinateDimension = BondOrderCoordinateDimension.Automatic;
        AppliedCount = 0;
        UnresolvedCount = 0;
    }
}
