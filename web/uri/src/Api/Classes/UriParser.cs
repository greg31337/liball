using System;

namespace uri;

/// <summary>
/// Provides strict, bounded RFC 3986 URI parsing operations.
/// </summary>
public class UriParser : IUriParser
{
    /// <summary>Initializes a stateless URI parser.</summary>
    public UriParser()
    {
    }

    /// <summary>Parses a complete URI-reference or throws on failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed reference.</returns>
    /// <exception cref="UriSyntaxException">The input or options are invalid.</exception>
    public UriReference ParseReference(string text, UriParseOptions options)
    {
        UriParseResult result = TryParseReference(text, options);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Reference;
    }

    /// <summary>Parses a complete URI-reference without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UriParseResult TryParseReference(string text, UriParseOptions options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        return TryParseReferenceSpan(text.AsSpan(), options);
    }

    /// <summary>Parses a URI-reference span without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UriParseResult TryParseReferenceSpan(
        ReadOnlySpan<char> text,
        UriParseOptions options)
    {
        UriDiagnostic? precondition = CheckParsePreconditions(text.Length, options);
        if (precondition != null)
        {
            return CreateParseFailure(precondition);
        }

        return UriParserCore.ParseReference(text);
    }

    /// <summary>Parses a complete absolute-URI or throws on failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed absolute URI.</returns>
    /// <exception cref="UriSyntaxException">The input or options are invalid.</exception>
    public UriReference ParseAbsoluteUri(string text, UriParseOptions options)
    {
        UriParseResult result = TryParseAbsoluteUri(text, options);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Reference;
    }

    /// <summary>Parses a complete absolute-URI without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UriParseResult TryParseAbsoluteUri(string text, UriParseOptions options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriDiagnostic? precondition = CheckParsePreconditions(text.Length, options);
        if (precondition != null)
        {
            return CreateParseFailure(precondition);
        }

        return UriParserCore.ParseAbsoluteUri(text.AsSpan());
    }

    /// <summary>Parses a complete relative-ref without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UriParseResult TryParseRelativeReference(
        string text,
        UriParseOptions options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriDiagnostic? precondition = CheckParsePreconditions(text.Length, options);
        if (precondition != null)
        {
            return CreateParseFailure(precondition);
        }

        return UriParserCore.ParseRelativeReference(text.AsSpan());
    }

    internal UriDiagnostic? CheckParsePreconditions(
        int inputLength,
        UriParseOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        return LimitGuard.CheckInputCharacters(
            inputLength,
            options.Limits,
            DiagnosticDomain.Uri);
    }

    internal UriParseResult CreateParseFailure(UriDiagnostic diagnostic)
    {
        return new UriParseResult(false, new UriReference(), diagnostic);
    }

    internal UriSyntaxException CreateSyntaxException(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriSyntaxException(failure);
    }
}
