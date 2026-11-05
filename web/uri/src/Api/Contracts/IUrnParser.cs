namespace uri;

/// <summary>
/// Parses reusable RFC 8141 URN models.
/// </summary>
public interface IUrnParser
{
    /// <summary>Parses a URN and throws when parsing fails.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed URN model.</returns>
    Urn ParseUrn(string text, UrnParseOptions options);

    /// <summary>Parses a URN without throwing for expected failure.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UrnParseResult TryParseUrn(string text, UrnParseOptions options);

    /// <summary>Parses a URN span without requiring a source string.</summary>
    /// <param name="text">The complete URN span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    UrnParseResult TryParseUrnSpan(
        System.ReadOnlySpan<char> text,
        UrnParseOptions options);
}
