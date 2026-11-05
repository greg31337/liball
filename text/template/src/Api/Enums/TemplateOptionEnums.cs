using System;
using System.Globalization;

namespace template;

/// <summary>
/// Defines the supported whitespace mode values used by the template API.
/// </summary>
public enum TemplateWhitespaceMode
{
    /// <summary>
    /// Preserves source whitespace except where template trim markers remove it.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Normalizes every newline sequence to a line feed.
    /// </summary>
    NormalizeLineFeeds = 1,
    /// <summary>
    /// Normalizes every newline sequence to a carriage return followed by a line feed.
    /// </summary>
    NormalizeCarriageReturnLineFeeds = 2,
    /// <summary>
    /// Trims line endings and collapses repeated whitespace outside template tags.
    /// </summary>
    Minimize = 3
}

/// <summary>
/// Defines the supported escaping mode values used by the template API.
/// </summary>
public enum TemplateEscapingMode
{
    /// <summary>
    /// Writes text without output escaping.
    /// </summary>
    None = 0,
    /// <summary>
    /// Escapes characters that are significant in HTML text.
    /// </summary>
    Html = 1,
    /// <summary>
    /// Escapes characters that are significant in XML text.
    /// </summary>
    Xml = 2,
    /// <summary>
    /// Escapes text for use inside a JSON string value, without adding quotes.
    /// </summary>
    JsonString = 3,
    /// <summary>
    /// Percent-encodes text as a URL data component.
    /// </summary>
    Url = 4,
    /// <summary>
    /// Escapes text for use inside a JavaScript string, without adding quotes.
    /// </summary>
    JavaScriptString = 5,
    /// <summary>
    /// Quotes and escapes text when required for one CSV field.
    /// </summary>
    CsvField = 6
}

/// <summary>
/// Defines the supported truthiness mode values used by the template API.
/// </summary>
public enum TemplateTruthinessMode
{
    /// <summary>
    /// Derives truthiness behavior from <c>SyntaxProfile</c>.
    /// </summary>
    ProfileDefault = 0,
    /// <summary>
    /// Uses native rules in which missing, null, false, zero, empty strings, and empty collections are false.
    /// </summary>
    Native = 1,
    /// <summary>
    /// Uses Mustache rules in which false, missing, null, and empty lists are false.
    /// </summary>
    Mustache = 2,
    /// <summary>
    /// Uses Handlebars rules, including false values, missing values, null, zero, and empty collections.
    /// </summary>
    Handlebars = 3,
    /// <summary>
    /// Uses Liquid rules in which only false, missing, and null are false.
    /// </summary>
    Liquid = 4,
    /// <summary>
    /// Uses Jinja rules in which false, missing, null, zero, empty strings, and empty collections are false.
    /// </summary>
    Jinja = 5
}

/// <summary>
/// Selects whether compatibility profiles favor permissive interoperability or strict runtime behavior.
/// </summary>
public enum TemplateCompatibilityMode
{
    /// <summary>
    /// Accepts supported compatibility extensions and uses permissive missing-value and conversion behavior.
    /// </summary>
    Pragmatic = 0,
    /// <summary>
    /// Enables strict missing-value, conversion, callable, and unsafe-member behavior for compatibility rendering.
    /// </summary>
    Strict = 1
}

/// <summary>
/// Selects how binary operators of equal or differing kinds associate within one expression.
/// </summary>
public enum TemplateExpressionPrecedenceMode
{
    /// <summary>
    /// Uses conventional precedence with multiplication before addition, comparisons before logical conjunction, and conjunction before disjunction.
    /// </summary>
    Standard = 0,
    /// <summary>
    /// Uses Liquid-compatible right-to-left binary operator association.
    /// </summary>
    LiquidRightToLeft = 1
}
