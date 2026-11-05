namespace ttf;

/// <summary>Defines one visual rectangle covering a shaping-safe source selection.</summary>
public interface ITextSelectionRectangleResult
{
    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    int GetLineIndex();
    /// <summary>Gets the covered source UTF-16 start.</summary><returns>The source start.</returns>
    int GetUtf16Start();
    /// <summary>Gets the covered source UTF-16 length.</summary><returns>The source length.</returns>
    int GetUtf16Length();
    /// <summary>Gets the rectangle x coordinate.</summary><returns>The x coordinate.</returns>
    double GetX();
    /// <summary>Gets the rectangle y coordinate.</summary><returns>The y coordinate.</returns>
    double GetY();
    /// <summary>Gets the rectangle width.</summary><returns>The nonnegative width.</returns>
    double GetWidth();
    /// <summary>Gets the rectangle height.</summary><returns>The nonnegative height.</returns>
    double GetHeight();
    /// <summary>Gets the exclusive maximum x coordinate.</summary><returns>The maximum x coordinate.</returns>
    double GetXMaximum();
    /// <summary>Gets the exclusive maximum y coordinate.</summary><returns>The maximum y coordinate.</returns>
    double GetYMaximum();
}
