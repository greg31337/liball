namespace ttf;

/// <summary>Defines public preparation of reusable styled text.</summary>
public interface ITextLayoutEngine
{
    /// <summary>Shapes and measures immutable text for repeated reflow.</summary>
    /// <param name="fontSet">The application-owned font set.</param>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="styles">Contiguous styles covering the entire source.</param>
    /// <param name="options">The wrapping and layout controls.</param>
    /// <returns>An immutable prepared snapshot.</returns>
    PreparedTextResult Prepare(
        FontSet fontSet,
        string text,
        TextLayoutStyle[] styles,
        TextLayoutOptions options);
}
