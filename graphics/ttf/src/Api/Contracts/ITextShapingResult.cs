namespace ttf;

/// <summary>Defines the deterministic result of one shaping operation.</summary>
public interface ITextShapingResult
{
    /// <summary>Gets the visual shaping run count.</summary>
    /// <returns>The run count.</returns>
    int GetRunCount();

    /// <summary>Gets one visual shaping run.</summary>
    /// <param name="index">The visual run index.</param>
    /// <returns>The shaping run.</returns>
    TextShapingRun GetRun(int index);

    /// <summary>Gets the diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();

    /// <summary>Gets one deterministic shaping diagnostic.</summary>
    /// <param name="index">The diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    FontDiagnostic GetDiagnostic(int index);

    /// <summary>Gets the decoded source rune count.</summary>
    /// <returns>The rune count before normalization.</returns>
    int GetSourceRuneCount();

    /// <summary>Gets the normalized rune count.</summary>
    /// <returns>The rune count after normalization.</returns>
    int GetNormalizedRuneCount();

    /// <summary>Gets the configured source cluster count.</summary>
    /// <returns>The cluster count.</returns>
    int GetGraphemeClusterCount();

    /// <summary>Gets the Unicode script run count.</summary>
    /// <returns>The itemized script run count.</returns>
    int GetScriptRunCount();

    /// <summary>Gets the Unicode bidi run count.</summary>
    /// <returns>The bidi run count.</returns>
    int GetBidiRunCount();

    /// <summary>Gets the deterministic fallback decisions for this operation.</summary>
    /// <returns>The immutable fallback report.</returns>
    FontFallbackReport GetFallbackReport();

    /// <summary>Gets the shared operation controls used for shaping.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Measures every visual run and returns total advance and bounds.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The aggregate scaled measurement.</returns>
    TextMeasurement Measure(double pointSize);

    /// <summary>Measures every visual run with style adjustments.</summary>
    /// <param name="style">The shaping measurement style.</param>
    /// <returns>The aggregate adjusted measurement.</returns>
    TextMeasurement MeasureWithStyle(TextStyle style);
}
