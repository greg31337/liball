namespace uri;

/// <summary>
/// Parses URI references using strict RFC 3986 productions.
/// </summary>
public interface IUriParser
{
    /// <summary>Parses a complete URI-reference or throws on failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed reference.</returns>
    UriReference ParseReference(string text, UriParseOptions options);

    /// <summary>Parses a complete URI-reference without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UriParseResult TryParseReference(string text, UriParseOptions options);

    /// <summary>Parses a URI-reference span without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UriParseResult TryParseReferenceSpan(System.ReadOnlySpan<char> text, UriParseOptions options);

    /// <summary>Parses a complete absolute-URI or throws on failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed absolute URI.</returns>
    UriReference ParseAbsoluteUri(string text, UriParseOptions options);

    /// <summary>Parses a complete absolute-URI without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UriParseResult TryParseAbsoluteUri(string text, UriParseOptions options);

    /// <summary>Parses a complete relative-ref without throwing for expected syntax failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UriParseResult TryParseRelativeReference(string text, UriParseOptions options);
}
