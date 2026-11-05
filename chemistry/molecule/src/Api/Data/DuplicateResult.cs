using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by duplicate.
/// </summary>
public class DuplicateResult
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
    /// The key.
    /// </summary>
    public string Key;
    /// <summary>
    /// The input index.
    /// </summary>
    public int InputIndex;
    /// <summary>
    /// The record index.
    /// </summary>
    public int RecordIndex;
    /// <summary>
    /// The zero-based index of the first input with this identity key, or <c>-1</c> when absent.
    /// </summary>
    public int FirstIndex;
    /// <summary>
    /// Whether the value is duplicate.
    /// </summary>
    public bool IsDuplicate;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The policy.
    /// </summary>
    public IdentityPolicy Policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateResult"/> class.
    /// </summary>
    public DuplicateResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Key = "";
        InputIndex = -1;
        RecordIndex = -1;
        FirstIndex = -1;
        IsDuplicate = false;
        Success = true;
        Policy = IdentityPolicy.Exact;
    }
}

