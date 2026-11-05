namespace ttf;

/// <summary>Shapes and measures styled text into reusable immutable snapshots.</summary>
public class TextLayoutEngine : ITextLayoutEngine
{
    /// <summary>Creates a text-layout engine.</summary>
    public TextLayoutEngine()
    {
    }

    /// <summary>Shapes and measures immutable text for repeated reflow.</summary>
    /// <param name="fontSet">The application-owned font set.</param>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="styles">Contiguous styles covering the entire source.</param>
    /// <param name="options">The wrapping and layout controls.</param>
    /// <returns>An immutable prepared snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// Styles do not cover the source exactly or split a grapheme cluster.
    /// </exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping or layout resource limit is exceeded.</exception>
    public PreparedTextResult Prepare(
        FontSet fontSet,
        string text,
        TextLayoutStyle[] styles,
        TextLayoutOptions options)
    {
        return PublicTextLayoutPreparer.Prepare(
            fontSet,
            text,
            styles,
            options);
    }
}
