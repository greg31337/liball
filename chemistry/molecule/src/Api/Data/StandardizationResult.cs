using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by standardization.
/// </summary>
public class StandardizationResult
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
    /// The mutations.
    /// </summary>
    public List<StandardizationMutation> Mutations;
    /// <summary>
    /// The step count.
    /// </summary>
    public int StepCount;
    /// <summary>
    /// The mutation count.
    /// </summary>
    public int MutationCount;
    /// <summary>
    /// The sum of affected atom positions across all executed stages.
    /// </summary>
    public int AtomMutationCount;
    /// <summary>
    /// The sum of affected bond positions across all executed stages.
    /// </summary>
    public int BondMutationCount;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardizationResult"/> class.
    /// </summary>
    public StandardizationResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Mutations = new List<StandardizationMutation>();
        StepCount = 0;
        MutationCount = 0;
        AtomMutationCount = 0;
        BondMutationCount = 0;
        Changed = false;
        Success = true;
    }
}
