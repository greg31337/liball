namespace molecule;


/// <summary>
/// Controls molfile version selection and optional feature emission.
/// </summary>
public class MolfileWriteOptions
{
    /// <summary>
    /// The requested output version; automatic mode promotes structures that exceed V2000 limits to V3000.
    /// </summary>
    public MolfileVersion Version;
    /// <summary>
    /// Whether atom-list query records are emitted.
    /// </summary>
    public bool WriteAtomLists;
    /// <summary>
    /// Whether atom aliases are emitted.
    /// </summary>
    public bool WriteAliases;
    /// <summary>
    /// Whether charge, isotope, and radical properties are emitted.
    /// </summary>
    public bool WriteCommonProperties;
    /// <summary>
    /// Whether V3000 enhanced stereo collections are emitted.
    /// </summary>
    public bool WriteEnhancedStereo;
    /// <summary>
    /// The largest atom count permitted for automatic V2000 output.
    /// </summary>
    public int V2000AtomLimit;
    /// <summary>
    /// The largest bond count permitted for automatic V2000 output.
    /// </summary>
    public int V2000BondLimit;

    /// <summary>
    /// Initializes automatic, feature-preserving output options with standard V2000 count limits.
    /// </summary>
    public MolfileWriteOptions()
    {
        Version = MolfileVersion.Auto;
        WriteAtomLists = true;
        WriteAliases = true;
        WriteCommonProperties = true;
        WriteEnhancedStereo = true;
        V2000AtomLimit = 999;
        V2000BondLimit = 999;
    }
}
