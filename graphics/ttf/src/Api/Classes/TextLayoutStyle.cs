namespace ttf;

/// <summary>Associates one font request and measured style with a source range.</summary>
public class TextLayoutStyle : ITextLayoutStyle
{
    int utf16Start;
    int utf16Length;
    FontRequest fontRequest;
    TextStyle textStyle;

    /// <summary>Creates one style range.</summary>
    /// <param name="utf16Start">The nonnegative source UTF-16 start.</param>
    /// <param name="utf16Length">The positive source UTF-16 length.</param>
    /// <param name="fontRequest">The font family and instance request.</param>
    /// <param name="textStyle">The point size and measurement adjustments.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">The source range is invalid.</exception>
    /// <exception cref="System.ArgumentNullException">
    /// <c>fontRequest</c> or <c>textStyle</c> is null.
    /// </exception>
    public TextLayoutStyle(
        int utf16Start,
        int utf16Length,
        FontRequest fontRequest,
        TextStyle textStyle)
    {
        if (utf16Start < 0)
        {
            throw new System.ArgumentOutOfRangeException("utf16Start");
        }
        if (utf16Length <= 0 || utf16Start > int.MaxValue - utf16Length)
        {
            throw new System.ArgumentOutOfRangeException("utf16Length");
        }
        if (fontRequest == null)
        {
            throw new System.ArgumentNullException("fontRequest");
        }
        if (textStyle == null)
        {
            throw new System.ArgumentNullException("textStyle");
        }

        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.fontRequest = fontRequest;
        this.textStyle = textStyle;
    }

    /// <summary>Gets the source UTF-16 start.</summary>
    /// <returns>The zero-based offset.</returns>
    public int GetUtf16Start() { return utf16Start; }

    /// <summary>Gets the source UTF-16 length.</summary>
    /// <returns>The positive code-unit length.</returns>
    public int GetUtf16Length() { return utf16Length; }

    /// <summary>Gets the font request used to shape the range.</summary>
    /// <returns>The caller-supplied request.</returns>
    public FontRequest GetFontRequest() { return fontRequest; }

    /// <summary>Gets the measurement style used for the range.</summary>
    /// <returns>The caller-supplied style.</returns>
    public TextStyle GetTextStyle() { return textStyle; }

    internal TextLayoutStyle Copy()
    {
        return new TextLayoutStyle(
            utf16Start,
            utf16Length,
            fontRequest.Copy(),
            textStyle.Copy());
    }
}
