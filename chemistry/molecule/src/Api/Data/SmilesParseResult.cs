using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by smiles parse.
/// </summary>
public class SmilesParseResult
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
    /// The error message.
    /// </summary>
    public string ErrorMessage;
    /// <summary>
    /// The error index.
    /// </summary>
    public int ErrorIndex;
    /// <summary>
    /// Whether recoverable input defects were repaired.
    /// </summary>
    public bool Recovered;
    /// <summary>
    /// The recovered issue count.
    /// </summary>
    public int RecoveredIssueCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmilesParseResult"/> class.
    /// </summary>
    public SmilesParseResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        ErrorMessage = "";
        ErrorIndex = -1;
        Recovered = false;
        RecoveredIssueCount = 0;
    }
}

