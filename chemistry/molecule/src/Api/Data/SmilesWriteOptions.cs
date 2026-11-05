namespace molecule;


/// <summary>
/// Controls independent SMILES traversal and representation policies.
/// </summary>
public class SmilesWriteOptions
{
    /// <summary>
    /// Whether atoms and components are emitted in canonical order.
    /// </summary>
    public bool Canonical;
    /// <summary>
    /// Whether supported Kekulé systems are converted to aromatic atoms and bonds before writing.
    /// </summary>
    public bool Aromatic;
    /// <summary>
    /// Whether supported aromatic systems are converted to deterministic Kekulé bond orders before writing.
    /// </summary>
    public bool Kekule;
    /// <summary>
    /// Whether supported atom and bond stereochemistry is emitted.
    /// </summary>
    public bool Isomeric;
    /// <summary>
    /// Whether assigned atom-map numbers are emitted.
    /// </summary>
    public bool IncludeAtomMaps;
    /// <summary>
    /// Whether atoms carrying hydrogen counts are emitted with bracket hydrogen annotations.
    /// </summary>
    public bool ExplicitHydrogens;

    /// <summary>
    /// Initializes traversal-order, structure-preserving SMILES options.
    /// </summary>
    public SmilesWriteOptions()
    {
        Canonical = false;
        Aromatic = false;
        Kekule = false;
        Isomeric = false;
        IncludeAtomMaps = true;
        ExplicitHydrogens = false;
    }
}
