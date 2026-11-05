using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by molfile read.
/// </summary>
public class MolfileReadResult
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
    /// The parsed molfile version.
    /// </summary>
    public MolfileVersion Version;
    /// <summary>
    /// The atom count declared by the format.
    /// </summary>
    public int DeclaredAtomCount;
    /// <summary>
    /// The bond count declared by the format.
    /// </summary>
    public int DeclaredBondCount;
    /// <summary>
    /// The number of atom-list records encountered.
    /// </summary>
    public int AtomListCount;
    /// <summary>
    /// The number of alias values encountered.
    /// </summary>
    public int AliasCount;
    /// <summary>
    /// The number of common property records encountered.
    /// </summary>
    public int PropertyRecordCount;
    /// <summary>
    /// The number of enhanced stereo collections encountered.
    /// </summary>
    public int StereoCollectionCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MolfileReadResult"/> class.
    /// </summary>
    public MolfileReadResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        Version = MolfileVersion.Auto;
        DeclaredAtomCount = 0;
        DeclaredBondCount = 0;
        AtomListCount = 0;
        AliasCount = 0;
        PropertyRecordCount = 0;
        StereoCollectionCount = 0;
    }
}
