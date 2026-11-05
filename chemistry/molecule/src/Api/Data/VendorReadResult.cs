using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by vendor read.
/// </summary>
public class VendorReadResult
{
    /// <summary>
    /// The molecules.
    /// </summary>
    public List<Molecule> Molecules;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Names, comments, format fields, and arbitrary string metadata associated with the molecule.
    /// </summary>
    public VendorMetadata Metadata;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="VendorReadResult"/> class.
    /// </summary>
    public VendorReadResult()
    {
        Molecules = new List<Molecule>();
        Report = new MoleculeReport();
        Metadata = new VendorMetadata();
        Success = true;
    }
}

