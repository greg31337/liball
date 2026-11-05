namespace molecule;


/// <summary>
/// Contains the outcome of one copy-based stereo editing operation.
/// </summary>
public class StereoEditResult
{
    /// <summary>
    /// The independent molecule copy containing any accepted change.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced by the editing operation.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether the requested edit completed successfully.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Whether the returned molecule differs from the input molecule.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// The created or targeted stereo record identifier, or <c>-1</c> when no record was resolved.
    /// </summary>
    public int StereoRecordId;
    /// <summary>
    /// The typed stereo form created or edited.
    /// </summary>
    public StereoKind StereoKind;

    /// <summary>
    /// Initializes an unsuccessful result containing an empty molecule and report.
    /// </summary>
    public StereoEditResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        Changed = false;
        StereoRecordId = -1;
        StereoKind = StereoKind.Unknown;
    }
}
