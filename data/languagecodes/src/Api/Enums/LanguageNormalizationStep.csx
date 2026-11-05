namespace languagecodes;

/// <summary>Identifies a deterministic change applied during normalization.</summary>
public enum LanguageNormalizationStep
{
    /// <summary>Surrounding whitespace was removed.</summary>
    TrimWhitespace = 0,
    /// <summary>Letter casing was normalized.</summary>
    NormalizeCase = 1,
    /// <summary>The value was converted to another ISO code form.</summary>
    ConvertCodeForm = 2,
    /// <summary>A deprecated value was replaced.</summary>
    ReplaceDeprecated = 3,
    /// <summary>A retired value was replaced.</summary>
    ReplaceRetired = 4,
    /// <summary>An individual language was folded to its macrolanguage.</summary>
    FoldMacrolanguage = 5,
    /// <summary>A grandfathered tag was replaced.</summary>
    ReplaceGrandfathered = 6,
    /// <summary>A deprecated language-tag subtag was replaced.</summary>
    ReplaceSubtag = 7,
    /// <summary>A redundant suppress-script subtag was removed.</summary>
    RemoveSuppressScript = 8,
    /// <summary>Language-tag component casing was normalized.</summary>
    NormalizeTagCasing = 9,
    /// <summary>Language-tag separators were normalized.</summary>
    NormalizeSeparators = 10
}
