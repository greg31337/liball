using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by pdb read.
/// </summary>
public class PdbReadResult
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
    /// The residues.
    /// </summary>
    public List<Residue> Residues;
    /// <summary>
    /// The chains.
    /// </summary>
    public List<Chain> Chains;
    /// <summary>
    /// The parsed model records.
    /// </summary>
    public List<MoleculeModel> Models;
    /// <summary>
    /// The number of parsed ATOM and HETATM records.
    /// </summary>
    public int AtomRecordCount;
    /// <summary>
    /// The number of parsed CONECT records.
    /// </summary>
    public int ConectRecordCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdbReadResult"/> class.
    /// </summary>
    public PdbReadResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        Residues = new List<Residue>();
        Chains = new List<Chain>();
        Models = new List<MoleculeModel>();
        AtomRecordCount = 0;
        ConectRecordCount = 0;
    }
}
