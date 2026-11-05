using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by stereo perception.
/// </summary>
public class StereoPerceptionResult
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
    /// The records.
    /// </summary>
    public List<StereoRecord> Records;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="StereoPerceptionResult"/> class.
    /// </summary>
    public StereoPerceptionResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Records = new List<StereoRecord>();
        Success = false;
    }
}

