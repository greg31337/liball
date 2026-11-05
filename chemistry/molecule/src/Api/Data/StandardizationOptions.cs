namespace molecule;


/// <summary>
/// Controls which cleanup stages the standardization pipeline applies.
/// </summary>
public class StandardizationOptions
{
    /// <summary>
    /// The isotope cleanup.
    /// </summary>
    public IsotopeCleanupPolicy IsotopeCleanup;
    /// <summary>
    /// The radical cleanup.
    /// </summary>
    public RadicalCleanupPolicy RadicalCleanup;
    /// <summary>
    /// The tautomer enumeration.
    /// </summary>
    public TautomerEnumerationOptions TautomerEnumeration;
    /// <summary>
    /// Options used when explicit graph hydrogens are normalized to implicit counts.
    /// </summary>
    public HydrogenOperationOptions HydrogenNormalization;
    /// <summary>
    /// Options used when aromaticity is normalized after functional-group and tautomer cleanup.
    /// </summary>
    public AromaticityOptions AromaticityNormalization;
    /// <summary>
    /// Options used when rejected aromatic systems require a Kekulé fallback.
    /// </summary>
    public KekulizationOptions KekulizationFallback;
    /// <summary>
    /// Whether salt removal is enabled.
    /// </summary>
    public bool EnableSaltRemoval;
    /// <summary>
    /// Whether metal disconnection is enabled.
    /// </summary>
    public bool EnableMetalDisconnection;
    /// <summary>
    /// Whether removable explicit graph hydrogens are converted to implicit hydrogen counts.
    /// </summary>
    public bool EnableExplicitHydrogenNormalization;
    /// <summary>
    /// Whether charge normalization is enabled.
    /// </summary>
    public bool EnableChargeNormalization;
    /// <summary>
    /// Whether reionization is enabled.
    /// </summary>
    public bool EnableReionization;
    /// <summary>
    /// Whether isotope cleanup is enabled.
    /// </summary>
    public bool EnableIsotopeCleanup;
    /// <summary>
    /// Whether radical cleanup is enabled.
    /// </summary>
    public bool EnableRadicalCleanup;
    /// <summary>
    /// Whether tautomer enumeration is enabled.
    /// </summary>
    public bool EnableTautomerEnumeration;
    /// <summary>
    /// Whether tautomer canonicalization is enabled.
    /// </summary>
    public bool EnableTautomerCanonicalization;
    /// <summary>
    /// Whether stereo cleanup is enabled.
    /// </summary>
    public bool EnableStereoCleanup;
    /// <summary>
    /// Whether aromatic atoms and bonds are normalized after tautomer processing.
    /// </summary>
    public bool EnableAromaticityNormalization;
    /// <summary>
    /// Whether rejected aromatic systems use deterministic Kekulé assignment as a recovery stage.
    /// </summary>
    public bool EnableKekulizationFallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardizationOptions"/> class.
    /// </summary>
    public StandardizationOptions()
    {
        IsotopeCleanup = new IsotopeCleanupPolicy();
        RadicalCleanup = new RadicalCleanupPolicy();
        TautomerEnumeration = new TautomerEnumerationOptions();
        HydrogenNormalization = new HydrogenOperationOptions();
        AromaticityNormalization = new AromaticityOptions();
        KekulizationFallback = new KekulizationOptions();
        EnableSaltRemoval = false;
        EnableMetalDisconnection = false;
        EnableExplicitHydrogenNormalization = false;
        EnableChargeNormalization = false;
        EnableReionization = false;
        EnableIsotopeCleanup = false;
        EnableRadicalCleanup = false;
        EnableTautomerEnumeration = false;
        EnableTautomerCanonicalization = false;
        EnableStereoCleanup = false;
        EnableAromaticityNormalization = false;
        EnableKekulizationFallback = false;
    }

    /// <summary>
    /// Enables the core cleanup.
    /// </summary>
    internal void EnableCoreCleanup()
    {
        EnableMetalDisconnection = true;
        EnableExplicitHydrogenNormalization = true;
        EnableChargeNormalization = true;
        EnableReionization = true;
        EnableIsotopeCleanup = true;
        EnableRadicalCleanup = true;
        EnableAromaticityNormalization = true;
        EnableKekulizationFallback = true;
    }

    /// <summary>
    /// Enables the parent cleanup.
    /// </summary>
    internal void EnableParentCleanup()
    {
        EnableSaltRemoval = true;
        EnableCoreCleanup();
        EnableTautomerEnumeration = true;
        EnableTautomerCanonicalization = true;
        EnableStereoCleanup = true;
    }
}
