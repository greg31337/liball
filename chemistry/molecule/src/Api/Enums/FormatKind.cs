namespace molecule;


/// <summary>
/// Defines the supported values for format kind.
/// </summary>
public enum FormatKind
{
    /// <summary>
    /// Specifies unknown.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Specifies smiles.
    /// </summary>
    Smiles = 1,
    /// <summary>
    /// Specifies smarts.
    /// </summary>
    Smarts = 2,
    /// <summary>
    /// Specifies mol v 2000.
    /// </summary>
    MolV2000 = 3,
    /// <summary>
    /// Specifies mol v 3000.
    /// </summary>
    MolV3000 = 4,
    /// <summary>
    /// Specifies sdf.
    /// </summary>
    Sdf = 5,
    /// <summary>
    /// Specifies mol 2.
    /// </summary>
    Mol2 = 6,
    /// <summary>
    /// Specifies pdb.
    /// </summary>
    Pdb = 7,
    /// <summary>
    /// Specifies inchi.
    /// </summary>
    InChI = 8,
    /// <summary>
    /// Specifies inchikey.
    /// </summary>
    InChIKey = 9,
    /// <summary>
    /// Specifies vendor delimited.
    /// </summary>
    VendorDelimited = 10,
    /// <summary>
    /// Specifies deterministic text.
    /// </summary>
    DeterministicText = 11
}

