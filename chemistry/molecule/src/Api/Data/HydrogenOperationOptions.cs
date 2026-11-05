namespace molecule;


/// <summary>
/// Controls public hydrogen graph operations.
/// </summary>
public class HydrogenOperationOptions
{
    /// <summary>
    /// Whether graph hydrogen atoms with an isotope mass number are preserved during removal and normalization.
    /// </summary>
    public bool PreserveIsotopicHydrogens;

    /// <summary>
    /// Initializes options that preserve isotopic hydrogen atoms.
    /// </summary>
    public HydrogenOperationOptions()
    {
        PreserveIsotopicHydrogens = true;
    }
}
