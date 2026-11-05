using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by reionization.
/// </summary>
public class ReionizationResult
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
    /// The transfer names.
    /// </summary>
    public List<string> TransferNames;
    /// <summary>
    /// The transfer count.
    /// </summary>
    public int TransferCount;
    /// <summary>
    /// Whether the operation changed the molecular graph.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReionizationResult"/> class.
    /// </summary>
    public ReionizationResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        TransferNames = new List<string>();
        TransferCount = 0;
        Changed = false;
        Success = true;
    }
}

