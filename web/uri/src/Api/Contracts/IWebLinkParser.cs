using System;

namespace uri;

/// <summary>
/// Parses strict or recovery-mode Web Link field values.
/// </summary>
public interface IWebLinkParser
{
    /// <summary>Parses one or more combined Link field values.</summary>
    /// <param name="fieldValues">The field values in received order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed ordered Web Link set.</returns>
    /// <exception cref="WebLinkSyntaxException">Syntax or a configured limit fails.</exception>
    WebLinkSet ParseLinkFieldValues(
        string[] fieldValues,
        WebLinkParseOptions options);

    /// <summary>Parses Link field values without throwing for expected failure.</summary>
    /// <param name="fieldValues">The field values in received order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    WebLinkParseResult TryParseLinkFieldValues(
        string[] fieldValues,
        WebLinkParseOptions options);

    /// <summary>Parses one Link field value span without requiring a source string.</summary>
    /// <param name="fieldValue">The complete field value.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    WebLinkParseResult TryParseLinkFieldValueSpan(
        ReadOnlySpan<char> fieldValue,
        WebLinkParseOptions options);
}
