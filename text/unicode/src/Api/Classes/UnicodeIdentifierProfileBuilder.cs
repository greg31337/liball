namespace unicode;

/// <summary>
/// Configures and creates immutable Unicode identifier profiles.
/// </summary>
public class UnicodeIdentifierProfileBuilder : IUnicodeIdentifierProfileBuilder
{
    internal string Name;
    internal bool UseXidProperties;
    internal UnicodeRangeSet? AllowedRanges;
    internal UnicodeRangeSet? DeniedRanges;
    internal UnicodeIdentifierNormalizationPolicy NormalizationPolicy;
    internal UnicodeIdentifierCasePolicy CasePolicy;
    internal bool AllowUnassignedCodePoints;
    internal UnicodeScript[]? PermittedScripts;
    internal bool DisallowPatternSyntax;
    internal bool DisallowPatternWhiteSpace;

    /// <summary>
    /// Initializes a builder with default XID-based identifier behavior.
    /// </summary>
    /// <param name="name">The nonempty human-readable profile name.</param>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or consists only of white space.</exception>
    public UnicodeIdentifierProfileBuilder(string name)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        UseXidProperties = true;
        AllowedRanges = null;
        DeniedRanges = null;
        NormalizationPolicy = UnicodeIdentifierNormalizationPolicy.None;
        CasePolicy = UnicodeIdentifierCasePolicy.Sensitive;
        AllowUnassignedCodePoints = false;
        PermittedScripts = null;
        DisallowPatternSyntax = true;
        DisallowPatternWhiteSpace = true;
    }

    /// <summary>
    /// Selects XID properties when <c>true</c>, or ID properties when <c>false</c>.
    /// </summary>
    /// <param name="useXidProperties">Whether to use XID_Start and XID_Continue.</param>
    /// <returns>This builder.</returns>
    public UnicodeIdentifierProfileBuilder SetUseXidProperties(bool useXidProperties)
    {
        UseXidProperties = useXidProperties;
        return this;
    }

    /// <summary>
    /// Replaces the ranges that are accepted in addition to the selected ID or XID properties.
    /// </summary>
    /// <param name="ranges">The inclusive Unicode code-point ranges to add.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>ranges</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an array element is <c>null</c>.</exception>
    public UnicodeIdentifierProfileBuilder SetAllowedRanges(UnicodeRange[] ranges)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        AllowedRanges = new UnicodeRangeSet(ranges);
        return this;
    }

    /// <summary>
    /// Replaces the ranges that are rejected even when another profile rule permits them.
    /// </summary>
    /// <param name="ranges">The inclusive Unicode code-point ranges to reject.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>ranges</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an array element is <c>null</c>.</exception>
    public UnicodeIdentifierProfileBuilder SetDeniedRanges(UnicodeRange[] ranges)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        DeniedRanges = new UnicodeRangeSet(ranges);
        return this;
    }

    /// <summary>
    /// Selects the profile's normalization requirement and preparation behavior.
    /// </summary>
    /// <param name="policy">The normalization policy.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is not defined.</exception>
    public UnicodeIdentifierProfileBuilder SetNormalizationPolicy(UnicodeIdentifierNormalizationPolicy policy)
    {
        ValidateNormalizationPolicy(policy);
        NormalizationPolicy = policy;
        return this;
    }

    /// <summary>
    /// Selects the profile's case-sensitive or caseless comparison behavior.
    /// </summary>
    /// <param name="policy">The case policy.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is not defined.</exception>
    public UnicodeIdentifierProfileBuilder SetCasePolicy(UnicodeIdentifierCasePolicy policy)
    {
        ValidateCasePolicy(policy);
        CasePolicy = policy;
        return this;
    }

    /// <summary>
    /// Selects whether otherwise eligible unassigned code points are accepted.
    /// </summary>
    /// <param name="allowUnassignedCodePoints">Whether to accept unassigned code points.</param>
    /// <returns>This builder.</returns>
    public UnicodeIdentifierProfileBuilder SetAllowUnassignedCodePoints(bool allowUnassignedCodePoints)
    {
        AllowUnassignedCodePoints = allowUnassignedCodePoints;
        return this;
    }

    /// <summary>
    /// Restricts assigned identifier characters to the supplied significant scripts.
    /// </summary>
    /// <param name="scripts">The permitted scripts; Common and Inherited characters remain neutral.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>scripts</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an array value is not defined.</exception>
    public UnicodeIdentifierProfileBuilder SetPermittedScripts(UnicodeScript[] scripts)
    {
        System.ArgumentNullException.ThrowIfNull(scripts);

        UnicodeScript[] copy = new UnicodeScript[scripts.Length];
        int index = 0;
        while (index < scripts.Length)
        {
            ValidateScript(scripts[index]);
            copy[index] = scripts[index];
            index = index + 1;
        }

        PermittedScripts = copy;
        return this;
    }

    /// <summary>
    /// Selects whether Pattern_Syntax and Pattern_White_Space are rejected.
    /// </summary>
    /// <param name="disallowPatternSyntax">Whether Pattern_Syntax is rejected.</param>
    /// <param name="disallowPatternWhiteSpace">Whether Pattern_White_Space is rejected.</param>
    /// <returns>This builder.</returns>
    public UnicodeIdentifierProfileBuilder SetPatternRestrictions(bool disallowPatternSyntax, bool disallowPatternWhiteSpace)
    {
        DisallowPatternSyntax = disallowPatternSyntax;
        DisallowPatternWhiteSpace = disallowPatternWhiteSpace;
        return this;
    }

    /// <summary>
    /// Creates an independent identifier profile from the current configuration.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    public UnicodeIdentifierProfile Build()
    {
        bool requireNormalization = NormalizationPolicy != UnicodeIdentifierNormalizationPolicy.None;
        UnicodeNormalizationForm normalizationForm = GetNormalizationForm(NormalizationPolicy);
        bool caseInsensitive = CasePolicy != UnicodeIdentifierCasePolicy.Sensitive;
        UnicodeCompareKeyMode compareKeyMode = GetCompareKeyMode(CasePolicy);
        UnicodeIdentifierProfile profile = new UnicodeIdentifierProfile(
            Name,
            UseXidProperties,
            AllowUnassignedCodePoints,
            requireNormalization,
            normalizationForm,
            caseInsensitive,
            compareKeyMode,
            DisallowPatternSyntax,
            DisallowPatternWhiteSpace);
        profile.ConfigureRangesAndScripts(AllowedRanges, DeniedRanges, PermittedScripts);
        return profile;
    }

    internal static UnicodeNormalizationForm GetNormalizationForm(UnicodeIdentifierNormalizationPolicy policy)
    {
        if (policy == UnicodeIdentifierNormalizationPolicy.Nfd)
        {
            return UnicodeNormalizationForm.Nfd;
        }

        if (policy == UnicodeIdentifierNormalizationPolicy.Nfkc)
        {
            return UnicodeNormalizationForm.Nfkc;
        }

        if (policy == UnicodeIdentifierNormalizationPolicy.Nfkd)
        {
            return UnicodeNormalizationForm.Nfkd;
        }

        return UnicodeNormalizationForm.Nfc;
    }

    internal static UnicodeCompareKeyMode GetCompareKeyMode(UnicodeIdentifierCasePolicy policy)
    {
        if (policy == UnicodeIdentifierCasePolicy.NfcFold)
        {
            return UnicodeCompareKeyMode.NfcFold;
        }

        if (policy == UnicodeIdentifierCasePolicy.NfkcFold)
        {
            return UnicodeCompareKeyMode.NfkcFold;
        }

        return UnicodeCompareKeyMode.OrdinalFold;
    }

    internal static void ValidateNormalizationPolicy(UnicodeIdentifierNormalizationPolicy policy)
    {
        if (policy == UnicodeIdentifierNormalizationPolicy.None || policy == UnicodeIdentifierNormalizationPolicy.Nfc || policy == UnicodeIdentifierNormalizationPolicy.Nfd || policy == UnicodeIdentifierNormalizationPolicy.Nfkc || policy == UnicodeIdentifierNormalizationPolicy.Nfkd)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("policy");
    }

    internal static void ValidateCasePolicy(UnicodeIdentifierCasePolicy policy)
    {
        if (policy == UnicodeIdentifierCasePolicy.Sensitive || policy == UnicodeIdentifierCasePolicy.OrdinalFold || policy == UnicodeIdentifierCasePolicy.NfcFold || policy == UnicodeIdentifierCasePolicy.NfkcFold)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("policy");
    }

    internal static void ValidateScript(UnicodeScript script)
    {
        if (script >= UnicodeScript.Unknown && script <= UnicodeScript.Tibetan)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("scripts");
    }
}
