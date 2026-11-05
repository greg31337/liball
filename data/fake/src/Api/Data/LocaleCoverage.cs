namespace fake;

/// <summary>
/// Describes native and fallback field coverage for one requested locale.
/// </summary>
public class LocaleCoverage
{
    /// <summary>
    /// The canonical requested locale tag.
    /// </summary>
    public string Locale = string.Empty;

    /// <summary>
    /// The fields supplied natively by the requested locale.
    /// </summary>
    public string[] NativeFields = new string[0];

    /// <summary>
    /// The fields available only through the active fallback policy.
    /// </summary>
    public string[] FallbackFields = new string[0];

    /// <summary>
    /// The winning source locale for each corresponding fallback field.
    /// </summary>
    public string[] FallbackSources = new string[0];

    /// <summary>
    /// The winning source details for every available field, ordered by field name.
    /// </summary>
    public LocaleFieldCoverage[] Fields = new LocaleFieldCoverage[0];

    /// <summary>
    /// Provider-level field summaries ordered by provider name.
    /// </summary>
    /// <remarks>
    /// This includes fallback-only providers. Inspect <see cref="LocaleProviderCoverage.HasNativeCoverage"/>
    /// or <see cref="NativeProviders"/> before describing a provider as natively covered.
    /// </remarks>
    public LocaleProviderCoverage[] Providers = new LocaleProviderCoverage[0];

    /// <summary>
    /// Provider names having at least one field supplied by the requested locale itself.
    /// </summary>
    public string[] NativeProviders = new string[0];

    /// <summary>
    /// Provider names having fallback fields but no native field for the requested locale.
    /// </summary>
    public string[] FallbackOnlyProviders = new string[0];

    /// <summary>
    /// The number of native fields.
    /// </summary>
    public int NativeFieldCount;

    /// <summary>
    /// The number of fields available through fallback.
    /// </summary>
    public int FallbackFieldCount;

    /// <summary>
    /// The total number of providers having native or fallback fields.
    /// </summary>
    public int ProviderCount;

    /// <summary>
    /// The number of providers having at least one native field.
    /// </summary>
    public int NativeProviderCount;

    /// <summary>
    /// The number of providers having fields only through fallback.
    /// </summary>
    public int FallbackOnlyProviderCount;

    /// <summary>
    /// The distinct data versions contributing to the reported coverage.
    /// </summary>
    public string[] DataVersions = new string[0];

    /// <summary>
    /// The distinct provenance descriptions contributing to the reported coverage.
    /// </summary>
    public string[] Provenance = new string[0];

    internal LocaleCoverage()
    {
        Locale = string.Empty;
        NativeFields = new string[0];
        FallbackFields = new string[0];
        FallbackSources = new string[0];
        Fields = new LocaleFieldCoverage[0];
        Providers = new LocaleProviderCoverage[0];
        NativeProviders = new string[0];
        FallbackOnlyProviders = new string[0];
        NativeFieldCount = 0;
        FallbackFieldCount = 0;
        ProviderCount = 0;
        NativeProviderCount = 0;
        FallbackOnlyProviderCount = 0;
        DataVersions = new string[0];
        Provenance = new string[0];
    }
}
