namespace ttf;

/// <summary>Defines the nearest shaping-safe caret returned by a hit test.</summary>
public interface ITextHitTestResult
{
    /// <summary>Gets the nearest caret stop.</summary><returns>The caret stop.</returns>
    TextCaretStopResult GetCaretStop();
    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    int GetLineIndex();
    /// <summary>Gets the nearest source-cluster boundary.</summary><returns>The cluster boundary.</returns>
    int GetClusterBoundary();
    /// <summary>Gets the nearest source UTF-16 boundary.</summary><returns>The UTF-16 index.</returns>
    int GetUtf16Index();
    /// <summary>Gets the snapped visual x coordinate.</summary><returns>The x coordinate.</returns>
    double GetX();
    /// <summary>Gets the snapped visual y coordinate.</summary><returns>The y coordinate.</returns>
    double GetY();
    /// <summary>Gets the Euclidean distance from the query point.</summary><returns>The nonnegative distance.</returns>
    double GetDistance();
}
