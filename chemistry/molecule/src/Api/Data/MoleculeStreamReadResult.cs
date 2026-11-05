using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by molecule stream read.
/// </summary>
public class MoleculeStreamReadResult
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
    /// Whether no additional records are available.
    /// </summary>
    public bool EndOfStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeStreamReadResult"/> class.
    /// </summary>
    public MoleculeStreamReadResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        EndOfStream = false;
    }
}

