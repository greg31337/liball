namespace markdown;

/// <summary>
/// Specifies how a raw HTML literal is handled during HTML rendering.
/// </summary>
public enum MarkdownRawHtmlDecision
{
    /// <summary>Escapes the literal as text.</summary>
    Escape = 0,
    /// <summary>Writes the literal as raw HTML.</summary>
    Render = 1,
    /// <summary>Escapes GFM-disallowed tag openings while retaining other raw HTML.</summary>
    Filter = 2,
    /// <summary>Emits no output for the literal.</summary>
    Suppress = 3
}
