using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides standardization mutation data and operations for molecular graphs.
/// </summary>
public class StandardizationMutation
{
    /// <summary>
    /// The step name.
    /// </summary>
    public string StepName;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The detail.
    /// </summary>
    public string Detail;
    /// <summary>
    /// Zero-based atom positions affected by this stage, in ascending order.
    /// </summary>
    public List<int> AtomIds;
    /// <summary>
    /// Zero-based bond positions affected by this stage, in ascending order.
    /// </summary>
    public List<int> BondIds;
    /// <summary>
    /// Ordered normalization rule names applied by this stage.
    /// </summary>
    public List<string> RuleNames;
    /// <summary>
    /// The number of affected atom positions in this stage.
    /// </summary>
    public int AtomMutationCount;
    /// <summary>
    /// The number of affected bond positions in this stage.
    /// </summary>
    public int BondMutationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardizationMutation"/> class.
    /// </summary>
    public StandardizationMutation()
    {
        StepName = "";
        Changed = false;
        Success = true;
        Detail = "";
        AtomIds = new List<int>();
        BondIds = new List<int>();
        RuleNames = new List<string>();
        AtomMutationCount = 0;
        BondMutationCount = 0;
    }
}
