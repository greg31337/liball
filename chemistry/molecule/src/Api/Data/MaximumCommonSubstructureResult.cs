using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the best common graph, deterministic mappings, bounds, and diagnostics from an MCS search.
/// </summary>
public class MaximumCommonSubstructureResult
{
    /// <summary>
    /// The independently owned common substructure copied from the left input.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Atom mappings ordered by left atom identifier and then right atom identifier.
    /// </summary>
    public List<CommonAtomMapping> AtomMappings;
    /// <summary>
    /// Bond mappings ordered by left bond identifier.
    /// </summary>
    public List<CommonBondMapping> BondMappings;
    /// <summary>
    /// Diagnostics produced by validation or bounded search termination.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// The number of mapped atoms.
    /// </summary>
    public int AtomCount;
    /// <summary>
    /// The number of mapped bonds.
    /// </summary>
    public int BondCount;
    /// <summary>
    /// The number of backtracking states visited.
    /// </summary>
    public int SearchStepCount;
    /// <summary>
    /// The elapsed search time in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds;
    /// <summary>
    /// Whether a mapping meeting the configured minimum atom count was found.
    /// </summary>
    public bool Found;
    /// <summary>
    /// Whether the exhaustive bounded search completed without reaching a bound.
    /// </summary>
    public bool SearchCompleted;
    /// <summary>
    /// Whether either the work or elapsed-time limit stopped the search.
    /// </summary>
    public bool LimitReached;
    /// <summary>
    /// Whether the work-step limit stopped the search.
    /// </summary>
    public bool WorkLimitReached;
    /// <summary>
    /// Whether the elapsed-time limit stopped the search.
    /// </summary>
    public bool TimedOut;
    /// <summary>
    /// Whether the operation accepted its inputs and produced a valid best-so-far result.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes an empty unsuccessful result.
    /// </summary>
    public MaximumCommonSubstructureResult()
    {
        Molecule = new Molecule();
        AtomMappings = new List<CommonAtomMapping>();
        BondMappings = new List<CommonBondMapping>();
        Report = new MoleculeReport();
        AtomCount = 0;
        BondCount = 0;
        SearchStepCount = 0;
        ElapsedMilliseconds = 0;
        Found = false;
        SearchCompleted = false;
        LimitReached = false;
        WorkLimitReached = false;
        TimedOut = false;
        Success = false;
    }
}
