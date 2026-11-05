namespace ttf;

/// <summary>Defines one font request and measured style over a source range.</summary>
public interface ITextLayoutStyle
{
    /// <summary>Gets the source UTF-16 start.</summary>
    /// <returns>The zero-based offset.</returns>
    int GetUtf16Start();

    /// <summary>Gets the source UTF-16 length.</summary>
    /// <returns>The positive code-unit length.</returns>
    int GetUtf16Length();

    /// <summary>Gets the font request used to shape the range.</summary>
    /// <returns>The caller-supplied request.</returns>
    FontRequest GetFontRequest();

    /// <summary>Gets the measurement style used for the range.</summary>
    /// <returns>The caller-supplied style.</returns>
    TextStyle GetTextStyle();
}
