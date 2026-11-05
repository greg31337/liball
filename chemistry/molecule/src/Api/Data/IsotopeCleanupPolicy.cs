namespace molecule;


/// <summary>
/// Controls isotope cleanup policy behavior.
/// </summary>
public class IsotopeCleanupPolicy
{
    /// <summary>
    /// Whether all isotopes is removed.
    /// </summary>
    public bool RemoveAllIsotopes;
    /// <summary>
    /// Whether hydrogen isotopes is preserved.
    /// </summary>
    public bool PreserveHydrogenIsotopes;
    /// <summary>
    /// Whether invalid isotopes is removed.
    /// </summary>
    public bool RemoveInvalidIsotopes;
    /// <summary>
    /// Whether major isotopes is removed.
    /// </summary>
    public bool RemoveMajorIsotopes;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsotopeCleanupPolicy"/> class.
    /// </summary>
    public IsotopeCleanupPolicy()
    {
        RemoveAllIsotopes = false;
        PreserveHydrogenIsotopes = true;
        RemoveInvalidIsotopes = true;
        RemoveMajorIsotopes = false;
    }

    /// <summary>
    /// Configures the remove all.
    /// </summary>
    internal void ConfigureRemoveAll()
    {
        RemoveAllIsotopes = true;
        PreserveHydrogenIsotopes = false;
        RemoveInvalidIsotopes = true;
        RemoveMajorIsotopes = true;
    }

    /// <summary>
    /// Configures the remove all preserving hydrogen.
    /// </summary>
    internal void ConfigureRemoveAllPreservingHydrogen()
    {
        RemoveAllIsotopes = true;
        PreserveHydrogenIsotopes = true;
        RemoveInvalidIsotopes = true;
        RemoveMajorIsotopes = true;
    }
}

