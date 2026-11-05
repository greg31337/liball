namespace ttf;

/// <summary>Defines one shaping-safe logical boundary at a visual caret position.</summary>
public interface ITextCaretStopResult
{
    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    int GetLineIndex();
    /// <summary>Gets the original grapheme-cluster boundary.</summary><returns>The cluster boundary.</returns>
    int GetClusterBoundary();
    /// <summary>Gets the original UTF-16 boundary.</summary><returns>The UTF-16 index.</returns>
    int GetUtf16Index();
    /// <summary>Gets the visual x coordinate.</summary><returns>The x coordinate.</returns>
    double GetX();
    /// <summary>Gets the visual y coordinate.</summary><returns>The y coordinate.</returns>
    double GetY();
    /// <summary>Reports whether the stop belongs to a positioned run.</summary><returns>True when run mapping is available.</returns>
    bool HasRunMapping();
    /// <summary>Gets the logical run index on the line.</summary><returns>The logical run index.</returns>
    /// <exception cref="System.InvalidOperationException">The stop has no run mapping.</exception>
    int GetLogicalRunIndex();
    /// <summary>Gets the corresponding visual run index on the line.</summary><returns>The visual run index.</returns>
    /// <exception cref="System.InvalidOperationException">The stop has no run mapping.</exception>
    int GetVisualRunIndex();
}
