namespace fake;

/// <summary>
/// Groups native and fallback fields for one provider in a locale coverage report.
/// </summary>
public class LocaleProviderCoverage
{
    /// <summary>
    /// The provider name derived from field-name prefixes.
    /// </summary>
    public string Provider = string.Empty;

    /// <summary>
    /// True when the requested locale supplies at least one field for this provider.
    /// </summary>
    public bool HasNativeCoverage;

    /// <summary>
    /// True when at least one provider field is available only through fallback.
    /// </summary>
    public bool HasFallbackCoverage;

    /// <summary>
    /// The provider fields supplied by the requested locale itself.
    /// </summary>
    public string[] NativeFields = new string[0];

    /// <summary>
    /// The provider fields supplied through fallback.
    /// </summary>
    public string[] FallbackFields = new string[0];

    /// <summary>
    /// The winning source locale for each corresponding fallback field.
    /// </summary>
    public string[] FallbackSources = new string[0];

    /// <summary>
    /// The number of provider fields supplied natively.
    /// </summary>
    public int NativeFieldCount;

    /// <summary>
    /// The number of provider fields supplied through fallback.
    /// </summary>
    public int FallbackFieldCount;

    internal LocaleProviderCoverage()
    {
        Provider = string.Empty;
        HasNativeCoverage = false;
        HasFallbackCoverage = false;
        NativeFields = new string[0];
        FallbackFields = new string[0];
        FallbackSources = new string[0];
        NativeFieldCount = 0;
        FallbackFieldCount = 0;
    }
}
