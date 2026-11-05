using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by salt removal.
/// </summary>
public class SaltRemovalResult
{
    /// <summary>
    /// The parent.
    /// </summary>
    public Molecule Parent;
    /// <summary>
    /// The removed fragments.
    /// </summary>
    public List<Molecule> RemovedFragments;
    /// <summary>
    /// The removed fragment names.
    /// </summary>
    public List<string> RemovedFragmentNames;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// The input fragment count.
    /// </summary>
    public int InputFragmentCount;
    /// <summary>
    /// The removed fragment count.
    /// </summary>
    public int RemovedFragmentCount;
    /// <summary>
    /// The matched salt fragment count.
    /// </summary>
    public int MatchedSaltFragmentCount;
    /// <summary>
    /// The retained fragment count.
    /// </summary>
    public int RetainedFragmentCount;
    /// <summary>
    /// Whether all fragments matched salts.
    /// </summary>
    public bool AllFragmentsMatchedSalts;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaltRemovalResult"/> class.
    /// </summary>
    public SaltRemovalResult()
    {
        Parent = new Molecule();
        RemovedFragments = new List<Molecule>();
        RemovedFragmentNames = new List<string>();
        Report = new MoleculeReport();
        InputFragmentCount = 0;
        RemovedFragmentCount = 0;
        MatchedSaltFragmentCount = 0;
        RetainedFragmentCount = 0;
        AllFragmentsMatchedSalts = false;
        Changed = false;
        Success = true;
    }
}

