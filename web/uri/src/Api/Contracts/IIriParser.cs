namespace uri;

/// <summary>
/// Parses RFC 3987 IRI references and their absolute and relative forms.
/// </summary>
public interface IIriParser
{
    /// <summary>Parses an IRI reference and throws when parsing fails.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed IRI reference.</returns>
    IriReference ParseIriReference(string text, IriParseOptions options);

    /// <summary>Parses an IRI reference without throwing for expected failure.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    IriParseResult TryParseIriReference(string text, IriParseOptions options);

    /// <summary>Parses an IRI-reference span without requiring a source string.</summary>
    /// <param name="text">The complete IRI-reference span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    IriParseResult TryParseIriReferenceSpan(
        System.ReadOnlySpan<char> text,
        IriParseOptions options);

    /// <summary>Parses an absolute IRI, requiring a scheme and forbidding a fragment.</summary>
    /// <param name="text">The complete absolute-IRI text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    IriParseResult TryParseAbsoluteIri(string text, IriParseOptions options);
}
