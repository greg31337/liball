using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by tautomer enumeration.
/// </summary>
public class TautomerEnumerationResult
{
    /// <summary>
    /// The tautomers.
    /// </summary>
    public List<Molecule> Tautomers;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// The applied rule names.
    /// </summary>
    public List<string> AppliedRuleNames;
    /// <summary>
    /// The transform count.
    /// </summary>
    public int TransformCount;
    /// <summary>
    /// Whether enumeration stopped at the configured tautomer limit.
    /// </summary>
    public bool TautomerLimitReached;
    /// <summary>
    /// Whether enumeration stopped at the configured transform limit.
    /// </summary>
    public bool TransformLimitReached;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="TautomerEnumerationResult"/> class.
    /// </summary>
    public TautomerEnumerationResult()
    {
        Tautomers = new List<Molecule>();
        Report = new MoleculeReport();
        AppliedRuleNames = new List<string>();
        TransformCount = 0;
        TautomerLimitReached = false;
        TransformLimitReached = false;
        Success = true;
    }
}

