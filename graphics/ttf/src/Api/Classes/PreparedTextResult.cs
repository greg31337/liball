namespace ttf;

/// <summary>Represents immutable shaped and measured text reusable across widths.</summary>
public class PreparedTextResult : IPreparedTextResult
{
    PreparedText prepared;
    string sourceText;
    int styleCount;
    int clusterCount;
    int shapingRunCount;
    TextLayoutOptions layoutOptions;
    FontOperationOptions operationOptions;
    FontDiagnostic[] diagnostics;

    internal PreparedTextResult(
        PreparedText prepared,
        string sourceText,
        int styleCount,
        int clusterCount,
        int shapingRunCount,
        TextLayoutOptions layoutOptions,
        FontOperationOptions operationOptions,
        FontDiagnostic[] diagnostics)
    {
        this.prepared = prepared;
        this.sourceText = sourceText;
        this.styleCount = styleCount;
        this.clusterCount = clusterCount;
        this.shapingRunCount = shapingRunCount;
        this.layoutOptions = layoutOptions.Copy();
        this.operationOptions = operationOptions.Copy();
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    /// <summary>Gets the retained source text.</summary>
    /// <returns>The immutable source string.</returns>
    public string GetSourceText() { return sourceText; }

    /// <summary>Gets the prepared style count.</summary>
    /// <returns>The style count.</returns>
    public int GetStyleCount() { return styleCount; }

    /// <summary>Gets the prepared grapheme-cluster count.</summary>
    /// <returns>The cluster count.</returns>
    public int GetClusterCount() { return clusterCount; }

    /// <summary>Gets the resolved shaping-run count.</summary>
    /// <returns>The shaping-run count.</returns>
    public int GetShapingRunCount() { return shapingRunCount; }

    /// <summary>Gets the wrapping, alignment, and line controls retained for reflow.</summary>
    /// <returns>An independent mutable copy.</returns>
    public TextLayoutOptions GetLayoutOptions()
    {
        return layoutOptions.Copy();
    }

    /// <summary>Gets the shared operation controls used during preparation.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }

    /// <summary>Gets the deterministic preparation diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount() { return diagnostics.Length; }

    /// <summary>Gets one preparation diagnostic.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    public FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }

    /// <summary>Reflows the prepared snapshot at one inline width.</summary>
    /// <param name="maximumAdvance">The nonnegative finite maximum inline advance.</param>
    /// <returns>An independent immutable layout snapshot.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>maximumAdvance</c> is negative or non-finite.
    /// </exception>
    public TextLayoutResult Reflow(double maximumAdvance)
    {
        return new TextLayoutResult(prepared.Layout(maximumAdvance));
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
