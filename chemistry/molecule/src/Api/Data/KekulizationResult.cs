using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by kekulization.
/// </summary>
public class KekulizationResult
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
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Whether the algorithm used its deterministic fallback path.
    /// </summary>
    public bool UsedFallback;
    /// <summary>
    /// The kekulized bond identifiers.
    /// </summary>
    public List<int> KekulizedBondIds;
    /// <summary>
    /// The aromaticity model used to validate systems before assignment.
    /// </summary>
    public AromaticityModelKind Model;
    /// <summary>
    /// Ring identifiers for which no exact Kekulé form was assigned.
    /// </summary>
    public List<int> FailedRingIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="KekulizationResult"/> class.
    /// </summary>
    public KekulizationResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        UsedFallback = false;
        KekulizedBondIds = new List<int>();
        Model = AromaticityModelKind.DaylightLike;
        FailedRingIds = new List<int>();
    }
}
