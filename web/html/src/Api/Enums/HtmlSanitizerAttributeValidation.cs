namespace html;

/// <summary>Selects a built-in validation applied to an attribute value.</summary>
public enum HtmlSanitizerAttributeValidation
{
    /// <summary>Accepts values containing at least one non-whitespace character.</summary>
    NonEmpty = 0,
    /// <summary>Accepts one token containing no HTML whitespace or control characters.</summary>
    SingleToken = 1,
    /// <summary>Accepts a signed decimal integer.</summary>
    Integer = 2,
    /// <summary>Accepts a structurally valid language tag.</summary>
    LanguageTag = 3
}
