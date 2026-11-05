namespace odf;

/// <summary>
/// Provides compatibility-profile identifiers for validation and output.
/// </summary>
public static class OdfCompatibilityProfiles
{
    /// <summary>
    /// Emits only standard OASIS namespace content.
    /// </summary>
    public const string StrictOasis = "strict-oasis";
    /// <summary>
    /// Emits broadly interoperable standard ODF content.
    /// </summary>
    public const string Interoperable = "interoperable";
    /// <summary>
    /// Retains LibreOffice and compatible OpenOffice extensions.
    /// </summary>
    public const string LibreOfficeExtended = "libreoffice-extended";
    /// <summary>
    /// Retains Apache OpenOffice and legacy OpenOffice extensions.
    /// </summary>
    public const string OpenOfficeExtended = "openoffice-extended";
    /// <summary>
    /// Retains Microsoft Office ODF extensions.
    /// </summary>
    public const string MicrosoftOfficeExtended = "microsoft-office-extended";
    /// <summary>
    /// Retains all existing standard, future, foreign, and producer-specific content.
    /// </summary>
    public const string PreserveExisting = "preserve-existing";
}
