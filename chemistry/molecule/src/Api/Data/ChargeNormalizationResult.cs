using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by charge normalization.
/// </summary>
public class ChargeNormalizationResult
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
    /// The applied rule names.
    /// </summary>
    public List<string> AppliedRuleNames;
    /// <summary>
    /// The applied rule count.
    /// </summary>
    public int AppliedRuleCount;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChargeNormalizationResult"/> class.
    /// </summary>
    public ChargeNormalizationResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        AppliedRuleNames = new List<string>();
        AppliedRuleCount = 0;
        Changed = false;
        Success = true;
    }
}

