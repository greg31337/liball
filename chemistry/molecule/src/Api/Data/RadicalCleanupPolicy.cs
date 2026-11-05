namespace molecule;


/// <summary>
/// Controls radical cleanup policy behavior.
/// </summary>
public class RadicalCleanupPolicy
{
    /// <summary>
    /// Whether all radicals is removed.
    /// </summary>
    public bool RemoveAllRadicals;
    /// <summary>
    /// Whether invalid radicals is removed.
    /// </summary>
    public bool RemoveInvalidRadicals;
    /// <summary>
    /// Whether radical valence is validated.
    /// </summary>
    public bool ValidateRadicalValence;
    /// <summary>
    /// The maximum radical electron count.
    /// </summary>
    public int MaximumRadicalElectronCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadicalCleanupPolicy"/> class.
    /// </summary>
    public RadicalCleanupPolicy()
    {
        RemoveAllRadicals = false;
        RemoveInvalidRadicals = true;
        ValidateRadicalValence = true;
        MaximumRadicalElectronCount = 3;
    }

    /// <summary>
    /// Configures the remove all.
    /// </summary>
    internal void ConfigureRemoveAll()
    {
        RemoveAllRadicals = true;
        RemoveInvalidRadicals = true;
        ValidateRadicalValence = true;
        MaximumRadicalElectronCount = 3;
    }

    /// <summary>
    /// Configures the validate only.
    /// </summary>
    internal void ConfigureValidateOnly()
    {
        RemoveAllRadicals = false;
        RemoveInvalidRadicals = false;
        ValidateRadicalValence = true;
        MaximumRadicalElectronCount = 3;
    }
}

