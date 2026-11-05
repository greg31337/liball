namespace ttf;

/// <summary>Represents the deterministic result of one shaping operation.</summary>
public class TextShapingResult : ITextShapingResult
{
    TextShapingRun[] runs;
    FontDiagnostic[] diagnostics;
    int sourceRuneCount;
    int normalizedRuneCount;
    int graphemeClusterCount;
    int scriptRunCount;
    int bidiRunCount;
    FontFallbackReport fallbackReport;
    FontOperationOptions operationOptions;

    internal TextShapingResult(
        TextShapingRun[] runs,
        FontDiagnostic[] diagnostics,
        int sourceRuneCount,
        int normalizedRuneCount,
        int graphemeClusterCount,
        int scriptRunCount,
        int bidiRunCount)
    {
        this.runs = CopyRuns(runs);
        this.diagnostics = CopyDiagnostics(diagnostics);
        this.sourceRuneCount = sourceRuneCount;
        this.normalizedRuneCount = normalizedRuneCount;
        this.graphemeClusterCount = graphemeClusterCount;
        this.scriptRunCount = scriptRunCount;
        this.bidiRunCount = bidiRunCount;
        fallbackReport = new FontFallbackReport(
            new FontFallbackDecision[0]);
        operationOptions = new FontOperationOptions();
    }

    /// <summary>Gets the visual shaping run count.</summary>
    /// <returns>The run count.</returns>
    public int GetRunCount() { return runs.Length; }

    /// <summary>Gets one visual shaping run.</summary>
    /// <param name="index">The visual run index.</param>
    /// <returns>The shaping run.</returns>
    public TextShapingRun GetRun(int index)
    {
        ValidateIndex(index, runs.Length);
        return runs[index];
    }

    /// <summary>Gets the diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount() { return diagnostics.Length; }

    /// <summary>Gets one deterministic shaping diagnostic.</summary>
    /// <param name="index">The diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    public FontDiagnostic GetDiagnostic(int index)
    {
        ValidateIndex(index, diagnostics.Length);
        return diagnostics[index];
    }

    /// <summary>Gets the decoded source rune count.</summary>
    /// <returns>The rune count before normalization.</returns>
    public int GetSourceRuneCount() { return sourceRuneCount; }

    /// <summary>Gets the normalized rune count.</summary>
    /// <returns>The rune count after normalization.</returns>
    public int GetNormalizedRuneCount() { return normalizedRuneCount; }

    /// <summary>Gets the configured source cluster count.</summary>
    /// <returns>The cluster count.</returns>
    public int GetGraphemeClusterCount() { return graphemeClusterCount; }

    /// <summary>Gets the Unicode script run count.</summary>
    /// <returns>The itemized script run count.</returns>
    public int GetScriptRunCount() { return scriptRunCount; }

    /// <summary>Gets the Unicode bidi run count.</summary>
    /// <returns>The bidi run count.</returns>
    public int GetBidiRunCount() { return bidiRunCount; }

    /// <summary>Gets the deterministic fallback decisions for this operation.</summary>
    /// <returns>The immutable fallback report.</returns>
    public FontFallbackReport GetFallbackReport()
    {
        return fallbackReport;
    }

    /// <summary>Gets the shared operation controls used for shaping.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }

    /// <summary>Measures every visual run and returns total advance and bounds.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The aggregate scaled measurement.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is invalid.</exception>
    public TextMeasurement Measure(double pointSize)
    {
        return TextMeasurer.MeasureResult(this, pointSize);
    }

    /// <summary>Measures every visual run with style adjustments.</summary>
    /// <param name="style">The shaping measurement style.</param>
    /// <returns>The aggregate adjusted measurement.</returns>
    /// <exception cref="System.ArgumentNullException"><c>style</c> is null.</exception>
    public TextMeasurement MeasureWithStyle(TextStyle style)
    {
        return TextMeasurer.MeasureResult(this, style);
    }

    internal void SetFallbackReport(FontFallbackReport value)
    {
        fallbackReport = value;
    }

    internal void SetOperationOptions(FontOperationOptions value)
    {
        operationOptions = value.Copy();
    }

    static TextShapingRun[] CopyRuns(TextShapingRun[] values)
    {
        TextShapingRun[] copy = new TextShapingRun[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
