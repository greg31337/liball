namespace uri;

/// <summary>
/// Parses reusable RFC 6570 URI Template models.
/// </summary>
public interface IUriTemplateParser
{
    /// <summary>Parses a URI Template and throws when parsing fails.</summary>
    /// <param name="text">The complete URI Template text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed reusable template model.</returns>
    UriTemplate ParseTemplate(
        string text,
        UriTemplateParseOptions options);

    /// <summary>Parses a URI Template without throwing for expected failure.</summary>
    /// <param name="text">The complete URI Template text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UriTemplateParseResult TryParseTemplate(
        string text,
        UriTemplateParseOptions options);

    /// <summary>Parses a URI Template span without requiring a source string.</summary>
    /// <param name="text">The complete URI Template span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UriTemplateParseResult TryParseTemplateSpan(
        System.ReadOnlySpan<char> text,
        UriTemplateParseOptions options);
}
