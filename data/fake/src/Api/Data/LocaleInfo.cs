namespace fake;

/// <summary>
/// Describes the native data available for one canonical locale tag.
/// </summary>
public class LocaleInfo
{
    /// <summary>
    /// The canonical supported locale tag.
    /// </summary>
    public string Tag = string.Empty;

    /// <summary>
    /// The canonical primary language subtag, or null for a private-use-only tag.
    /// </summary>
    public string? Language;

    /// <summary>
    /// The canonical script subtag, or null when absent.
    /// </summary>
    public string? Script;

    /// <summary>
    /// The canonical territory subtag, or null when absent.
    /// </summary>
    public string? Territory;

    /// <summary>
    /// Whether a locale profile was resolved for this metadata result.
    /// </summary>
    public bool HasProfile;

    /// <summary>
    /// The locale's English display name, or an empty string when no profile is available.
    /// </summary>
    public string EnglishName = string.Empty;

    /// <summary>
    /// The locale's native display name, or an empty string when no profile is available.
    /// </summary>
    public string NativeName = string.Empty;

    /// <summary>
    /// The native display name of the locale's primary language.
    /// </summary>
    public string LanguageName = string.Empty;

    /// <summary>
    /// The default script code declared by the locale profile.
    /// </summary>
    public string DefaultScript = string.Empty;

    /// <summary>
    /// The English display name of the default script.
    /// </summary>
    public string ScriptName = string.Empty;

    /// <summary>
    /// The native display name of the locale's territory.
    /// </summary>
    public string TerritoryName = string.Empty;

    /// <summary>
    /// The locale's default writing direction.
    /// </summary>
    public WritingDirection Direction;

    /// <summary>
    /// The locale's default ISO 4217 currency code.
    /// </summary>
    public string DefaultCurrencyCode = string.Empty;

    /// <summary>
    /// The English registry name of the locale's default currency.
    /// </summary>
    public string DefaultCurrencyName = string.Empty;

    /// <summary>
    /// The locale's international calling code including the leading plus sign.
    /// </summary>
    public string CallingCode = string.Empty;

    /// <summary>
    /// The representative IANA time-zone identifiers declared by the locale profile.
    /// </summary>
    public string[] TimeZoneCodes = new string[0];

    /// <summary>
    /// The display names derived from the representative IANA time-zone identifiers.
    /// </summary>
    public string[] TimeZoneNames = new string[0];

    /// <summary>
    /// The locale's conventional numeric date-component order.
    /// </summary>
    public DateOrder DateOrder;

    /// <summary>
    /// The locale's decimal separator.
    /// </summary>
    public string DecimalSeparator = string.Empty;

    /// <summary>
    /// The resolved multiline address-format template, or an empty string when unavailable.
    /// </summary>
    public string AddressFormat = string.Empty;

    /// <summary>
    /// Provider names backed by native fields according to the locale profile.
    /// </summary>
    public string[] NativeProviders = new string[0];

    /// <summary>
    /// Provider names intentionally available through fallback according to the locale profile.
    /// </summary>
    public string[] FallbackProviders = new string[0];

    /// <summary>
    /// All provider names available natively or through fallback.
    /// </summary>
    public string[] AvailableProviders = new string[0];

    /// <summary>
    /// The locale source that supplied the profile.
    /// </summary>
    public string ProfileSourceLocale = string.Empty;

    /// <summary>
    /// Whether the profile source differs from <see cref="Tag"/>.
    /// </summary>
    public bool ProfileUsedFallback;

    /// <summary>
    /// The data version attached to the resolved profile.
    /// </summary>
    public string ProfileDataVersion = string.Empty;

    /// <summary>
    /// The provenance description attached to the resolved profile.
    /// </summary>
    public string ProfileProvenance = string.Empty;

    /// <summary>
    /// The locale source that supplied address formatting data.
    /// </summary>
    public string AddressFormatSourceLocale = string.Empty;

    /// <summary>
    /// Whether address formatting data came from a fallback locale.
    /// </summary>
    public bool AddressFormatUsedFallback;

    /// <summary>
    /// The data version attached to the address formatting source.
    /// </summary>
    public string AddressFormatDataVersion = string.Empty;

    /// <summary>
    /// The provenance description attached to the address formatting source.
    /// </summary>
    public string AddressFormatProvenance = string.Empty;

    /// <summary>
    /// The native field names available for this locale.
    /// </summary>
    public string[] Fields = new string[0];

    /// <summary>
    /// The version shared by the locale's field data.
    /// </summary>
    public string DataVersion = string.Empty;

    /// <summary>
    /// The distinct provenance descriptions for the locale's field data.
    /// </summary>
    public string[] Provenance = new string[0];

    internal LocaleInfo()
    {
        Tag = string.Empty;
        Language = null;
        Script = null;
        Territory = null;
        HasProfile = false;
        EnglishName = string.Empty;
        NativeName = string.Empty;
        LanguageName = string.Empty;
        DefaultScript = string.Empty;
        ScriptName = string.Empty;
        TerritoryName = string.Empty;
        Direction = WritingDirection.Neutral;
        DefaultCurrencyCode = string.Empty;
        DefaultCurrencyName = string.Empty;
        CallingCode = string.Empty;
        TimeZoneCodes = new string[0];
        TimeZoneNames = new string[0];
        DateOrder = global::fake.DateOrder.MonthDayYear;
        DecimalSeparator = string.Empty;
        AddressFormat = string.Empty;
        NativeProviders = new string[0];
        FallbackProviders = new string[0];
        AvailableProviders = new string[0];
        ProfileSourceLocale = string.Empty;
        ProfileUsedFallback = false;
        ProfileDataVersion = string.Empty;
        ProfileProvenance = string.Empty;
        AddressFormatSourceLocale = string.Empty;
        AddressFormatUsedFallback = false;
        AddressFormatDataVersion = string.Empty;
        AddressFormatProvenance = string.Empty;
        Fields = new string[0];
        DataVersion = string.Empty;
        Provenance = new string[0];
    }
}
