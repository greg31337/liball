namespace languagecodes;

/// <summary>Controls ISO code conversion and BCP 47 language-tag normalization.</summary>
public class LanguageNormalizationOptions
{
    /// <summary>The requested output code form, or <see cref="LanguageCodeForm.Unknown"/> to preserve the recognized form.</summary>
    public LanguageCodeForm TargetForm;

    /// <summary>Whether authoritative replacements are applied to deprecated codes and subtags.</summary>
    public bool ReplaceDeprecated;

    /// <summary>Whether an unambiguous authoritative replacement is applied to a retired code.</summary>
    public bool ReplaceRetired;

    /// <summary>How individual-language and macrolanguage relationships are treated.</summary>
    public MacrolanguageNormalizationPolicy MacrolanguagePolicy;

    /// <summary>Whether surrounding whitespace may be removed.</summary>
    public bool TrimWhitespace;

    /// <summary>Whether ISO language-code casing may be normalized.</summary>
    public bool NormalizeCase;

    /// <summary>Whether language-tag separators may be normalized to hyphens.</summary>
    public bool NormalizeTagSeparators;

    /// <summary>Whether language-tag components may be changed to their recommended casing.</summary>
    public bool NormalizeTagCasing;

    /// <summary>Whether grandfathered tags may be replaced by their IANA preferred values.</summary>
    public bool ReplaceGrandfatheredTags;

    /// <summary>Whether a redundant suppress-script subtag may be removed.</summary>
    public bool RemoveSuppressScript;
}
