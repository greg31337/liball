using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by stereo cleanup.
/// </summary>
public class StereoCleanupResult
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
    /// The removed record count.
    /// </summary>
    public int RemovedRecordCount;
    /// <summary>
    /// The removed group count.
    /// </summary>
    public int RemovedGroupCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="StereoCleanupResult"/> class.
    /// </summary>
    public StereoCleanupResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        RemovedRecordCount = 0;
        RemovedGroupCount = 0;
    }
}

