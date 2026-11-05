namespace molecule;


/// <summary>
/// Controls molfile version selection, feature preservation, and structure limits.
/// </summary>
public class MolfileReadOptions
{
    /// <summary>
    /// The required version, or automatic marker detection.
    /// </summary>
    public MolfileVersion Version;
    /// <summary>
    /// Whether atom-list query records are preserved.
    /// </summary>
    public bool ReadAtomLists;
    /// <summary>
    /// Whether atom aliases are preserved.
    /// </summary>
    public bool ReadAliases;
    /// <summary>
    /// Whether charge, isotope, and radical properties are applied.
    /// </summary>
    public bool ReadCommonProperties;
    /// <summary>
    /// Whether V3000 enhanced stereo collections are preserved.
    /// </summary>
    public bool ReadEnhancedStereo;
    /// <summary>
    /// The maximum accepted atom count.
    /// </summary>
    public int MaximumAtomCount;
    /// <summary>
    /// The maximum accepted bond count.
    /// </summary>
    public int MaximumBondCount;

    /// <summary>
    /// Initializes automatic, feature-preserving options without a practical count restriction.
    /// </summary>
    public MolfileReadOptions()
    {
        Version = MolfileVersion.Auto;
        ReadAtomLists = true;
        ReadAliases = true;
        ReadCommonProperties = true;
        ReadEnhancedStereo = true;
        MaximumAtomCount = int.MaxValue;
        MaximumBondCount = int.MaxValue;
    }
}
