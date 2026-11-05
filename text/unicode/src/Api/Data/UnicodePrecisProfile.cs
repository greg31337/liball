namespace unicode;

/// <summary>
/// Defines mapping, normalization, spacing, and character-class rules for PRECIS-style strings.
/// </summary>
public class UnicodePrecisProfile
{
    /// <summary>
    /// The human-readable name.
    /// </summary>
    public string Name;
    /// <summary>
    /// Whether the profile applies identifier-class restrictions.
    /// </summary>
    public bool IdentifierClass;
    /// <summary>
    /// Whether preparation maps text to lowercase.
    /// </summary>
    public bool CaseMapped;
    /// <summary>
    /// Whether comparisons use a Unicode caseless comparison key.
    /// </summary>
    public bool CaseInsensitive;
    /// <summary>
    /// Whether preparation applies the selected Unicode normalization form.
    /// </summary>
    public bool Normalize;
    /// <summary>
    /// The normalization form used by the profile.
    /// </summary>
    public UnicodeNormalizationForm NormalizationForm;
    /// <summary>
    /// Whether non-ASCII space characters are mapped to U+0020.
    /// </summary>
    public bool MapSpaces;
    /// <summary>
    /// Whether leading and trailing ASCII spaces are removed.
    /// </summary>
    public bool TrimSpaces;
    /// <summary>
    /// Whether adjacent ASCII spaces are collapsed to one.
    /// </summary>
    public bool CollapseSpaces;
    /// <summary>
    /// Whether the prepared value may contain spaces.
    /// </summary>
    public bool AllowSpaces;
    /// <summary>
    /// Whether the prepared value may be empty.
    /// </summary>
    public bool AllowEmpty;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <param name="identifierClass">Whether identifier-class character restrictions apply.</param>
    /// <param name="caseMapped">Whether preparation maps text to lowercase.</param>
    /// <param name="caseInsensitive">Whether comparison keys apply Unicode case folding.</param>
    /// <param name="normalize">Whether preparation applies Unicode normalization.</param>
    /// <param name="normalizationForm">The normalization form used during preparation.</param>
    /// <param name="mapSpaces">Whether non-ASCII spaces map to U+0020.</param>
    /// <param name="trimSpaces">Whether leading and trailing ASCII spaces are removed.</param>
    /// <param name="collapseSpaces">Whether adjacent ASCII spaces collapse to one.</param>
    /// <param name="allowSpaces">Whether spaces are allowed after preparation.</param>
    /// <param name="allowEmpty">Whether an empty prepared value is valid.</param>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or consists only of white space.</exception>
    public UnicodePrecisProfile(
        string name,
        bool identifierClass,
        bool caseMapped,
        bool caseInsensitive,
        bool normalize,
        UnicodeNormalizationForm normalizationForm,
        bool mapSpaces,
        bool trimSpaces,
        bool collapseSpaces,
        bool allowSpaces,
        bool allowEmpty)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UnicodeIdentifierProfile.ValidateNormalizationForm(normalizationForm);

        Name = name;
        IdentifierClass = identifierClass;
        CaseMapped = caseMapped;
        CaseInsensitive = caseInsensitive;
        Normalize = normalize;
        NormalizationForm = normalizationForm;
        MapSpaces = mapSpaces;
        TrimSpaces = trimSpaces;
        CollapseSpaces = collapseSpaces;
        AllowSpaces = allowSpaces;
        AllowEmpty = allowEmpty;
    }

    /// <summary>
    /// Creates a username profile that lowercases and normalizes input.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodePrecisProfile CreateUsernameCaseMapped()
    {
        return new UnicodePrecisProfile(
            "UsernameCaseMapped",
            true,
            true,
            true,
            true,
            UnicodeNormalizationForm.Nfc,
            false,
            false,
            false,
            false,
            false);
    }

    /// <summary>
    /// Creates a username profile that preserves case while normalizing input.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodePrecisProfile CreateUsernameCasePreserved()
    {
        return new UnicodePrecisProfile(
            "UsernameCasePreserved",
            true,
            false,
            true,
            true,
            UnicodeNormalizationForm.Nfc,
            false,
            false,
            false,
            false,
            false);
    }

    /// <summary>
    /// Creates a nickname profile that maps, trims, and collapses spaces before normalization.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodePrecisProfile CreateNickname()
    {
        return new UnicodePrecisProfile(
            "Nickname",
            false,
            false,
            true,
            true,
            UnicodeNormalizationForm.Nfkc,
            true,
            true,
            true,
            true,
            false);
    }

    /// <summary>
    /// Creates a case-preserving opaque-string profile that permits spaces.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodePrecisProfile CreateOpaqueString()
    {
        return new UnicodePrecisProfile(
            "OpaqueString",
            false,
            false,
            false,
            true,
            UnicodeNormalizationForm.Nfc,
            true,
            false,
            false,
            true,
            false);
    }
}
