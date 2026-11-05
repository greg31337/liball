namespace ttf;

/// <summary>Represents one visual rectangle covering a shaping-safe source selection.</summary>
public class TextSelectionRectangleResult : ITextSelectionRectangleResult
{
    TextSelectionRectangle rectangle;

    internal TextSelectionRectangleResult(TextSelectionRectangle rectangle)
    {
        this.rectangle = rectangle;
    }

    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    public int GetLineIndex() { return rectangle.GetLineIndex(); }
    /// <summary>Gets the covered source UTF-16 start.</summary><returns>The source start.</returns>
    public int GetUtf16Start() { return rectangle.GetUtf16Start(); }
    /// <summary>Gets the covered source UTF-16 length.</summary><returns>The source length.</returns>
    public int GetUtf16Length() { return rectangle.GetUtf16Length(); }
    /// <summary>Gets the rectangle x coordinate.</summary><returns>The x coordinate.</returns>
    public double GetX() { return rectangle.GetX(); }
    /// <summary>Gets the rectangle y coordinate.</summary><returns>The y coordinate.</returns>
    public double GetY() { return rectangle.GetY(); }
    /// <summary>Gets the rectangle width.</summary><returns>The nonnegative width.</returns>
    public double GetWidth() { return rectangle.GetWidth(); }
    /// <summary>Gets the rectangle height.</summary><returns>The nonnegative height.</returns>
    public double GetHeight() { return rectangle.GetHeight(); }
    /// <summary>Gets the exclusive maximum x coordinate.</summary><returns>The maximum x coordinate.</returns>
    public double GetXMaximum()
    {
        return rectangle.GetX() + rectangle.GetWidth();
    }
    /// <summary>Gets the exclusive maximum y coordinate.</summary><returns>The maximum y coordinate.</returns>
    public double GetYMaximum()
    {
        return rectangle.GetY() + rectangle.GetHeight();
    }
}
