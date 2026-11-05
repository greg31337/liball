using System;

namespace uri;

/// <summary>
/// Provides strict, bounded RFC 3987 IRI parsing operations.
/// </summary>
public class IriParser : IIriParser
{
    /// <summary>Initializes a stateless IRI parser.</summary>
    public IriParser()
    {
    }

    /// <summary>Parses an IRI reference and throws when parsing fails.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed IRI reference.</returns>
    /// <exception cref="IriSyntaxException">The input or options are invalid.</exception>
    public IriReference ParseIriReference(string text, IriParseOptions options)
    {
        IriParseResult result = TryParseIriReference(text, options);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Reference;
    }

    /// <summary>Parses an IRI reference without throwing for expected failure.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public IriParseResult TryParseIriReference(
        string text,
        IriParseOptions options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        return TryParseIriReferenceSpan(text.AsSpan(), options);
    }

    /// <summary>Parses an IRI-reference span without requiring a source string.</summary>
    /// <param name="text">The complete IRI-reference span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public IriParseResult TryParseIriReferenceSpan(
        ReadOnlySpan<char> text,
        IriParseOptions options)
    {
        UriDiagnostic? precondition = CheckParsePreconditions(
            text.Length,
            options);
        if (precondition != null)
        {
            return CreateParseFailure(precondition);
        }

        return IriParserCore.ParseReference(text);
    }

    /// <summary>Parses an absolute IRI, requiring a scheme and forbidding a fragment.</summary>
    /// <param name="text">The complete absolute-IRI text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public IriParseResult TryParseAbsoluteIri(
        string text,
        IriParseOptions options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        UriDiagnostic? precondition = CheckParsePreconditions(
            text.Length,
            options);
        if (precondition != null)
        {
            return CreateParseFailure(precondition);
        }

        return IriParserCore.ParseAbsoluteIri(text.AsSpan());
    }

    internal UriDiagnostic? CheckParsePreconditions(
        int inputLength,
        IriParseOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        return LimitGuard.CheckInputCharacters(
            inputLength,
            options.Limits,
            DiagnosticDomain.Iri);
    }

    internal IriParseResult CreateParseFailure(UriDiagnostic diagnostic)
    {
        return new IriParseResult(false, new IriReference(), diagnostic);
    }

    internal IriSyntaxException CreateSyntaxException(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }
        else
        {
            failure = IriComponentScanner.MapDiagnostic(diagnostic);
        }

        return new IriSyntaxException(failure);
    }
}
