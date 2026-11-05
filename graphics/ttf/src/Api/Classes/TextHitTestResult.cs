namespace ttf;

/// <summary>Represents the nearest shaping-safe caret returned by a hit test.</summary>
public class TextHitTestResult : ITextHitTestResult
{
    TextHitTestData hit;
    TextCaretStopResult caretStop;

    internal TextHitTestResult(TextHitTestData hit, TextLine line)
    {
        this.hit = hit;
        caretStop = new TextCaretStopResult(hit.GetCaretStop(), line);
    }

    /// <summary>Gets the nearest caret stop.</summary><returns>The caret stop.</returns>
    public TextCaretStopResult GetCaretStop() { return caretStop; }
    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    public int GetLineIndex() { return hit.GetLineIndex(); }
    /// <summary>Gets the nearest source-cluster boundary.</summary><returns>The cluster boundary.</returns>
    public int GetClusterBoundary() { return hit.GetClusterBoundary(); }
    /// <summary>Gets the nearest source UTF-16 boundary.</summary><returns>The UTF-16 index.</returns>
    public int GetUtf16Index() { return hit.GetUtf16Index(); }
    /// <summary>Gets the snapped visual x coordinate.</summary><returns>The x coordinate.</returns>
    public double GetX() { return hit.GetX(); }
    /// <summary>Gets the snapped visual y coordinate.</summary><returns>The y coordinate.</returns>
    public double GetY() { return hit.GetY(); }
    /// <summary>Gets the Euclidean distance from the query point.</summary><returns>The nonnegative distance.</returns>
    public double GetDistance() { return hit.GetDistance(); }
}
