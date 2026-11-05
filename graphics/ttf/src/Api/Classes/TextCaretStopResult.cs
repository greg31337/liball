namespace ttf;

/// <summary>Represents one shaping-safe logical boundary at a visual caret position.</summary>
public class TextCaretStopResult : ITextCaretStopResult
{
    TextCaretStop stop;
    TextLine line;

    internal TextCaretStopResult(TextCaretStop stop, TextLine line)
    {
        this.stop = stop;
        this.line = line;
    }

    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    public int GetLineIndex() { return stop.GetLineIndex(); }
    /// <summary>Gets the original grapheme-cluster boundary.</summary><returns>The cluster boundary.</returns>
    public int GetClusterBoundary() { return stop.GetClusterBoundary(); }
    /// <summary>Gets the original UTF-16 boundary.</summary><returns>The UTF-16 index.</returns>
    public int GetUtf16Index() { return stop.GetUtf16Index(); }
    /// <summary>Gets the visual x coordinate.</summary><returns>The x coordinate.</returns>
    public double GetX() { return stop.GetX(); }
    /// <summary>Gets the visual y coordinate.</summary><returns>The y coordinate.</returns>
    public double GetY() { return stop.GetY(); }
    /// <summary>Reports whether the stop belongs to a positioned run.</summary><returns>True when run mapping is available.</returns>
    public bool HasRunMapping() { return stop.GetGroupIndex() >= 0; }

    /// <summary>Gets the logical run index on the line.</summary><returns>The logical run index.</returns>
    /// <exception cref="System.InvalidOperationException">The stop has no run mapping.</exception>
    public int GetLogicalRunIndex()
    {
        ValidateRunMapping();
        return stop.GetGroupIndex();
    }

    /// <summary>Gets the corresponding visual run index on the line.</summary><returns>The visual run index.</returns>
    /// <exception cref="System.InvalidOperationException">The stop has no run mapping.</exception>
    public int GetVisualRunIndex()
    {
        ValidateRunMapping();
        return line.GetVisualRunIndex(stop.GetGroupIndex());
    }

    void ValidateRunMapping()
    {
        if (!HasRunMapping())
        {
            throw new System.InvalidOperationException(
                "This caret stop is not attached to a positioned run.");
        }
    }
}
