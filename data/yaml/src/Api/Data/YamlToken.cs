namespace yaml;

/// <summary>
/// Represents one low-level token emitted by the YAML scanner.
/// </summary>
public class YamlToken
{
    /// <summary>
    /// The lexical role of this token.
    /// </summary>
    public YamlTokenKind Kind;
    /// <summary>
    /// The decoded token text, or the original marker for structural tokens.
    /// </summary>
    public string Value;
    /// <summary>
    /// The block scalar header text, including chomping or indentation indicators when present.
    /// </summary>
    public string Header;
    /// <summary>
    /// The indentation width carried by an indent, dedent, or block scalar token.
    /// </summary>
    public int Indent;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a token with unknown kind, empty text, zero indentation, and an empty source range.
    /// </summary>
    public YamlToken()
    {
        Kind = YamlTokenKind.Unknown;
        Value = "";
        Header = "";
        Indent = 0;
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Creates a token with no indentation or block scalar header.
    /// </summary>
    /// <param name="kind">The kind assigned to the new value.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML token.</returns>
    internal static YamlToken Create(YamlTokenKind kind, string? value, YamlSourceSpan? span)
    {
        return Create(kind, value, 0, span);
    }

    /// <summary>
    /// Creates a token with the supplied kind, value, indentation, and source range.
    /// </summary>
    /// <param name="kind">The kind assigned to the new value.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="indent">The indentation level in spaces.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML token.</returns>
    internal static YamlToken Create(YamlTokenKind kind, string? value, int indent, YamlSourceSpan? span)
    {
        YamlToken token = new YamlToken();
        token.Kind = kind;
        token.Value = value == null ? "" : value;
        token.Indent = indent;
        token.Span = span == null ? YamlSourceSpan.Empty() : span;
        return token;
    }

    /// <summary>
    /// Creates an indentation token with the supplied indentation width.
    /// </summary>
    /// <param name="kind">The kind assigned to the new value.</param>
    /// <param name="indent">The indentation level in spaces.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML token.</returns>
    internal static YamlToken CreateIndent(YamlTokenKind kind, int indent, YamlSourceSpan span)
    {
        return Create(kind, indent.ToString(), indent, span);
    }

    /// <summary>
    /// Creates a literal or folded block scalar token with its header, content, and detected indentation.
    /// </summary>
    /// <param name="kind">The kind assigned to the new value.</param>
    /// <param name="header">The block scalar header text.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="indent">The indentation level in spaces.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML token.</returns>
    internal static YamlToken CreateBlockScalar(YamlTokenKind kind, string header, string value, int indent, YamlSourceSpan span)
    {
        YamlToken token = Create(kind, value, indent, span);
        token.Header = header == null ? "" : header;
        return token;
    }
}
