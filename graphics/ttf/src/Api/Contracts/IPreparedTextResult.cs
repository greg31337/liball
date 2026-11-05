namespace ttf;

/// <summary>Defines an immutable shaped and measured text snapshot.</summary>
public interface IPreparedTextResult
{
    /// <summary>Gets the retained source text.</summary>
    /// <returns>The immutable source string.</returns>
    string GetSourceText();

    /// <summary>Gets the prepared style count.</summary>
    /// <returns>The style count.</returns>
    int GetStyleCount();

    /// <summary>Gets the prepared grapheme-cluster count.</summary>
    /// <returns>The cluster count.</returns>
    int GetClusterCount();

    /// <summary>Gets the resolved shaping-run count.</summary>
    /// <returns>The shaping-run count.</returns>
    int GetShapingRunCount();

    /// <summary>Gets the wrapping, alignment, and line controls retained for reflow.</summary>
    /// <returns>An independent mutable copy.</returns>
    TextLayoutOptions GetLayoutOptions();

    /// <summary>Gets the shared operation controls used during preparation.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Gets the deterministic preparation diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();

    /// <summary>Gets one preparation diagnostic.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    FontDiagnostic GetDiagnostic(int index);

    /// <summary>Reflows the prepared snapshot at one inline width.</summary>
    /// <param name="maximumAdvance">The nonnegative finite maximum inline advance.</param>
    /// <returns>An independent immutable layout snapshot.</returns>
    TextLayoutResult Reflow(double maximumAdvance);
}
