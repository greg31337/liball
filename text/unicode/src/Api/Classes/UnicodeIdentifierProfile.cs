namespace unicode;

/// <summary>
/// Defines the character, normalization, and comparison rules used for identifiers.
/// </summary>
public class UnicodeIdentifierProfile : IUnicodeIdentifierProfile
{
    /// <summary>
    /// The human-readable name.
    /// </summary>
    internal string Name;
    /// <summary>
    /// Whether identifier checks use XID_Start and XID_Continue instead of ID_Start and ID_Continue.
    /// </summary>
    internal bool UseXidProperties;
    /// <summary>
    /// Whether code points with the unassigned general category are accepted.
    /// </summary>
    internal bool AllowUnassignedCodePoints;
    /// <summary>
    /// Whether input must already satisfy the selected normalization form.
    /// </summary>
    internal bool RequireNormalization;
    /// <summary>
    /// The normalization form used by the profile.
    /// </summary>
    internal UnicodeNormalizationForm NormalizationForm;
    /// <summary>
    /// Whether comparisons use a Unicode caseless comparison key.
    /// </summary>
    internal bool CaseInsensitive;
    /// <summary>
    /// The normalization and folding mode used for comparison keys.
    /// </summary>
    internal UnicodeCompareKeyMode CompareKeyMode;
    /// <summary>
    /// Whether Pattern_Syntax code points are rejected.
    /// </summary>
    internal bool DisallowPatternSyntax;
    /// <summary>
    /// Whether Pattern_White_Space code points are rejected.
    /// </summary>
    internal bool DisallowPatternWhiteSpace;
    internal UnicodeRangeSet? AllowedRanges;
    internal UnicodeRangeSet? DeniedRanges;
    internal UnicodeScript[]? PermittedScripts;
    internal bool UseImmutableRules;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <param name="useXidProperties">Whether to use XID_Start and XID_Continue instead of ID_Start and ID_Continue.</param>
    /// <param name="allowUnassignedCodePoints">Whether unassigned code points are accepted.</param>
    /// <param name="requireNormalization">Whether input must already use the selected normalization form.</param>
    /// <param name="normalizationForm">The normalization form required by the profile.</param>
    /// <param name="caseInsensitive">Whether comparison keys apply Unicode case folding.</param>
    /// <param name="compareKeyMode">The normalization and folding used for comparison keys.</param>
    /// <param name="disallowPatternSyntax">Whether Pattern_Syntax code points are rejected.</param>
    /// <param name="disallowPatternWhiteSpace">Whether Pattern_White_Space code points are rejected.</param>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or consists only of white space.</exception>
    public UnicodeIdentifierProfile(
        string name,
        bool useXidProperties,
        bool allowUnassignedCodePoints,
        bool requireNormalization,
        UnicodeNormalizationForm normalizationForm,
        bool caseInsensitive,
        UnicodeCompareKeyMode compareKeyMode,
        bool disallowPatternSyntax,
        bool disallowPatternWhiteSpace)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateNormalizationForm(normalizationForm);
        ValidateCompareKeyMode(compareKeyMode);

        Name = name;
        UseXidProperties = useXidProperties;
        AllowUnassignedCodePoints = allowUnassignedCodePoints;
        RequireNormalization = requireNormalization;
        NormalizationForm = normalizationForm;
        CaseInsensitive = caseInsensitive;
        CompareKeyMode = compareKeyMode;
        DisallowPatternSyntax = disallowPatternSyntax;
        DisallowPatternWhiteSpace = disallowPatternWhiteSpace;
        AllowedRanges = null;
        DeniedRanges = null;
        PermittedScripts = null;
        UseImmutableRules = false;
    }

    /// <summary>
    /// Creates the default XID-based identifier profile without a normalization requirement.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodeIdentifierProfile CreateDefault()
    {
        return new UnicodeIdentifierProfile(
            "Default",
            true,
            false,
            false,
            UnicodeNormalizationForm.Nfc,
            false,
            UnicodeCompareKeyMode.OrdinalFold,
            true,
            true);
    }

    /// <summary>
    /// Creates an immutable UAX #31 identifier profile.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodeIdentifierProfile CreateImmutable()
    {
        UnicodeIdentifierProfile profile = new UnicodeIdentifierProfile(
            "Immutable",
            false,
            true,
            false,
            UnicodeNormalizationForm.Nfc,
            false,
            UnicodeCompareKeyMode.OrdinalFold,
            true,
            true);
        profile.UseImmutableRules = true;
        return profile;
    }

    /// <summary>
    /// Creates an XID-based identifier profile that requires NFC input.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodeIdentifierProfile CreateNormalized()
    {
        return new UnicodeIdentifierProfile(
            "Normalized",
            true,
            false,
            true,
            UnicodeNormalizationForm.Nfc,
            false,
            UnicodeCompareKeyMode.NfcFold,
            true,
            true);
    }

    /// <summary>
    /// Creates an XID-based identifier profile using NFKC case-fold comparison keys.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    internal static UnicodeIdentifierProfile CreateCaseInsensitive()
    {
        return new UnicodeIdentifierProfile(
            "CaseInsensitive",
            true,
            false,
            true,
            UnicodeNormalizationForm.Nfkc,
            true,
            UnicodeCompareKeyMode.NfkcFold,
            true,
            true);
    }

    /// <summary>
    /// Reports whether a code point may start an identifier under this profile.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AllowsStart(int codePoint)
    {
        if (!IsCustomizedCodePointAllowed(codePoint))
        {
            return false;
        }

        if (UseImmutableRules)
        {
            return IsImmutableCodePointAllowed(codePoint);
        }

        if (UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.OtherNotAssigned)
        {
            return AllowUnassignedCodePoints;
        }

        if (AllowedRanges != null)
        {
            if (AllowedRanges.Contains(codePoint))
            {
                return true;
            }
        }

        if (UseXidProperties)
        {
            return UnicodeIdentifiers.Instance.IsXidStart(codePoint);
        }

        return UnicodeIdentifiers.Instance.IsIdStart(codePoint);
    }

    /// <summary>
    /// Reports whether a code point may continue an identifier under this profile.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AllowsContinue(int codePoint)
    {
        if (!IsCustomizedCodePointAllowed(codePoint))
        {
            return false;
        }

        if (UseImmutableRules)
        {
            return IsImmutableCodePointAllowed(codePoint);
        }

        if (UnicodeProperties.Instance.GetGeneralCategory(codePoint) == UnicodeGeneralCategory.OtherNotAssigned)
        {
            return AllowUnassignedCodePoints;
        }

        if (AllowedRanges != null)
        {
            if (AllowedRanges.Contains(codePoint))
            {
                return true;
            }
        }

        if (UseXidProperties)
        {
            return UnicodeIdentifiers.Instance.IsXidContinue(codePoint);
        }

        return UnicodeIdentifiers.Instance.IsIdContinue(codePoint);
    }

    /// <summary>
    /// Reports whether text meets this profile's normalization requirement.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsNormalizationSatisfied(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (!RequireNormalization)
        {
            return true;
        }

        return UnicodeNormalization.Instance.IsNormalized(text, NormalizationForm);
    }

    /// <summary>
    /// Reports whether unassigned Unicode code points are accepted when other character rules reject them.
    /// </summary>
    /// <returns><c>true</c> when unassigned code points are accepted; otherwise, <c>false</c>.</returns>
    public bool AllowsUnassignedCodePoints()
    {
        if (DeniedRanges != null)
        {
            return false;
        }

        if (PermittedScripts != null)
        {
            return false;
        }

        return AllowUnassignedCodePoints;
    }

    /// <summary>
    /// Reports whether code points with the Pattern_Syntax property are rejected.
    /// </summary>
    /// <returns><c>true</c> when Pattern_Syntax code points are rejected; otherwise, <c>false</c>.</returns>
    public bool DisallowsPatternSyntax()
    {
        return DisallowPatternSyntax;
    }

    /// <summary>
    /// Reports whether code points with the Pattern_White_Space property are rejected.
    /// </summary>
    /// <returns><c>true</c> when Pattern_White_Space code points are rejected; otherwise, <c>false</c>.</returns>
    public bool DisallowsPatternWhiteSpace()
    {
        return DisallowPatternWhiteSpace;
    }

    /// <summary>
    /// Applies the normalization policy configured for this profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to prepare.</param>
    /// <returns>The prepared UTF-16 text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string Normalize(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (RequireNormalization)
        {
            return UnicodeNormalization.Instance.Normalize(text, NormalizationForm);
        }

        return text;
    }

    /// <summary>
    /// Produces the comparison key configured for this profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to prepare for comparison.</param>
    /// <returns>The comparison key.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string GetCompareKey(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (CaseInsensitive)
        {
            return UnicodeCase.Instance.CompareKey(text, CompareKeyMode);
        }

        return Normalize(text);
    }

    internal void ConfigureRangesAndScripts(UnicodeRangeSet? allowedRanges, UnicodeRangeSet? deniedRanges, UnicodeScript[]? permittedScripts)
    {
        AllowedRanges = allowedRanges;
        DeniedRanges = deniedRanges;

        if (permittedScripts == null)
        {
            PermittedScripts = null;
            return;
        }

        UnicodeScript[] copy = new UnicodeScript[permittedScripts.Length];
        int index = 0;
        while (index < permittedScripts.Length)
        {
            copy[index] = permittedScripts[index];
            index = index + 1;
        }

        PermittedScripts = copy;
    }

    internal bool IsCustomizedCodePointAllowed(int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF)
        {
            return false;
        }

        if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
        {
            return false;
        }

        if (DeniedRanges != null)
        {
            if (DeniedRanges.Contains(codePoint))
            {
                return false;
            }
        }

        return IsScriptPermitted(codePoint);
    }

    internal bool IsImmutableCodePointAllowed(int codePoint)
    {
        UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(codePoint);
        if (category == UnicodeGeneralCategory.PrivateUse)
        {
            return false;
        }

        if (category == UnicodeGeneralCategory.Surrogate)
        {
            return false;
        }

        if (category == UnicodeGeneralCategory.Control)
        {
            return false;
        }

        if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.NoncharacterCodePoint))
        {
            return false;
        }

        if (UnicodeIdentifiers.Instance.IsPatternSyntax(codePoint))
        {
            return false;
        }

        return !UnicodeIdentifiers.Instance.IsPatternWhiteSpace(codePoint);
    }

    internal bool IsScriptPermitted(int codePoint)
    {
        if (PermittedScripts == null)
        {
            return true;
        }

        UnicodeScript script = UnicodeProperties.Instance.GetScript(codePoint);
        if (script == UnicodeScript.Common || script == UnicodeScript.Inherited)
        {
            return true;
        }

        int index = 0;
        while (index < PermittedScripts.Length)
        {
            if (PermittedScripts[index] == script)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateNormalizationForm(UnicodeNormalizationForm form)
    {
        if (form == UnicodeNormalizationForm.Nfc)
        {
            return;
        }

        if (form == UnicodeNormalizationForm.Nfd)
        {
            return;
        }

        if (form == UnicodeNormalizationForm.Nfkc)
        {
            return;
        }

        if (form == UnicodeNormalizationForm.Nfkd)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("form");
    }

    internal static void ValidateCompareKeyMode(UnicodeCompareKeyMode mode)
    {
        if (mode == UnicodeCompareKeyMode.OrdinalFold)
        {
            return;
        }

        if (mode == UnicodeCompareKeyMode.NfcFold)
        {
            return;
        }

        if (mode == UnicodeCompareKeyMode.NfkcFold)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("mode");
    }
}
