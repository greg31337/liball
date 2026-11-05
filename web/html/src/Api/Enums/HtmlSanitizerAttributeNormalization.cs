using System;

namespace html;
/// <summary>Selects the normalization applied to an accepted attribute value.</summary>
public enum HtmlSanitizerAttributeNormalization
{
    /// <summary>Removes leading and trailing HTML whitespace.</summary>
    Trim = 0,
    /// <summary>Converts ASCII letters to lowercase.</summary>
    LowercaseAscii = 1,
    /// <summary>Collapses each HTML-whitespace run to a single space.</summary>
    CollapseHtmlWhitespace = 2,
    /// <summary>Parses, deduplicates, and rejoins a space-separated token list.</summary>
    SpaceSeparatedTokens = 3
}
