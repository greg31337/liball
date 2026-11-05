using System;

namespace uri;

/// <summary>
/// Provides strict or recovery-mode bounded Web Link parsing.
/// </summary>
public class WebLinkParser : IWebLinkParser
{
    /// <summary>Initializes a stateless Web Link parser.</summary>
    public WebLinkParser()
    {
    }

    /// <summary>Parses one or more combined Link field values.</summary>
    /// <param name="fieldValues">The field values in received order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed ordered Web Link set.</returns>
    /// <exception cref="WebLinkSyntaxException">Syntax or a configured limit fails.</exception>
    public WebLinkSet ParseLinkFieldValues(
        string[] fieldValues,
        WebLinkParseOptions options)
    {
        WebLinkParseResult result = ParseNullableFieldValues(
            fieldValues,
            options);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.LinkSet;
    }

    /// <summary>Parses Link field values without throwing for expected failure.</summary>
    /// <param name="fieldValues">The field values in received order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public WebLinkParseResult TryParseLinkFieldValues(
        string[] fieldValues,
        WebLinkParseOptions options)
    {
        return ParseNullableFieldValues(fieldValues, options);
    }

    /// <summary>Parses one Link field value span without requiring a source string.</summary>
    /// <param name="fieldValue">The complete field value.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public WebLinkParseResult TryParseLinkFieldValueSpan(
        ReadOnlySpan<char> fieldValue,
        WebLinkParseOptions options)
    {
        return ParseSpanWithOptions(fieldValue, options);
    }

    internal WebLinkParseResult ParseNullableFieldValues(
        string[]? fieldValues,
        WebLinkParseOptions? options)
    {
        if (options == null)
        {
            return WebLinkParserCore.CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        if (options.Mode == WebLinkParseMode.Strict)
        {
            return WebLinkParserCore.ParseStrictFieldValues(
                fieldValues,
                options);
        }

        if (options.Mode == WebLinkParseMode.Recovery)
        {
            return WebLinkRecoveryParserCore.ParseRecoveryFieldValues(
                fieldValues,
                options);
        }

        return WebLinkParserCore.CreateParseFailure(
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink));
    }

    internal WebLinkParseResult ParseSpanWithOptions(
        ReadOnlySpan<char> fieldValue,
        WebLinkParseOptions? options)
    {
        if (options == null)
        {
            return WebLinkParserCore.CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        if (options.Mode == WebLinkParseMode.Strict)
        {
            return WebLinkParserCore.ParseStrictFieldValueSpan(
                fieldValue,
                options);
        }

        if (options.Mode == WebLinkParseMode.Recovery)
        {
            return WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                fieldValue,
                options);
        }

        return WebLinkParserCore.CreateParseFailure(
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink));
    }

    internal WebLinkSyntaxException CreateSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkSyntaxException(failure);
    }
}
