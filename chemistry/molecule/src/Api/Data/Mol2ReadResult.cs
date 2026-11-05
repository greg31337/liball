using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by mol 2 read.
/// </summary>
public class Mol2ReadResult
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
    /// The atom count declared by the MOL2 molecule section.
    /// </summary>
    public int DeclaredAtomCount;
    /// <summary>
    /// The bond count declared by the MOL2 molecule section.
    /// </summary>
    public int DeclaredBondCount;
    /// <summary>
    /// The substructure count declared by the MOL2 molecule section.
    /// </summary>
    public int DeclaredSubstructureCount;
    /// <summary>
    /// The parsed Tripos molecule type.
    /// </summary>
    public string MoleculeType;
    /// <summary>
    /// The parsed Tripos charge type.
    /// </summary>
    public string ChargeType;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mol2ReadResult"/> class.
    /// </summary>
    public Mol2ReadResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        Residues = new List<Residue>();
        DeclaredAtomCount = 0;
        DeclaredBondCount = 0;
        DeclaredSubstructureCount = 0;
        MoleculeType = "";
        ChargeType = "";
    }
}
