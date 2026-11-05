namespace unicode;

/// <summary>
/// Contains consolidated Unicode metadata for PDF text layout.
/// </summary>
public class UnicodePdfLayoutInfo
{
    /// <summary>Grapheme-safe layout clusters.</summary>
    public UnicodePdfLayoutCluster[] GraphemeClusters;
    /// <summary>Default Unicode word segments.</summary>
    public UnicodeSegmentRecord[] WordSegments;
    /// <summary>Optional and mandatory line-break positions.</summary>
    public UnicodeLineBreakRecord[] LineBreaks;
    /// <summary>Resolved script runs.</summary>
    public UnicodePdfScriptRun[] ScriptRuns;
    /// <summary>Resolved simple bidi runs.</summary>
    public UnicodePdfBidiRun[] BidiRuns;
    /// <summary>Scalar-level bracket, mirror, and width metadata.</summary>
    public UnicodePdfScalarLayout[] Scalars;

    /// <summary>
    /// Initializes consolidated PDF layout metadata.
    /// </summary>
    /// <param name="graphemeClusters">The grapheme-safe clusters.</param>
    /// <param name="wordSegments">The word segments.</param>
    /// <param name="lineBreaks">The line-break positions.</param>
    /// <param name="scriptRuns">The script runs.</param>
    /// <param name="bidiRuns">The bidi runs.</param>
    /// <param name="scalars">The scalar metadata.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an array is <c>null</c>.</exception>
    public UnicodePdfLayoutInfo(
        UnicodePdfLayoutCluster[] graphemeClusters,
        UnicodeSegmentRecord[] wordSegments,
        UnicodeLineBreakRecord[] lineBreaks,
        UnicodePdfScriptRun[] scriptRuns,
        UnicodePdfBidiRun[] bidiRuns,
        UnicodePdfScalarLayout[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(graphemeClusters);
        System.ArgumentNullException.ThrowIfNull(wordSegments);
        System.ArgumentNullException.ThrowIfNull(lineBreaks);
        System.ArgumentNullException.ThrowIfNull(scriptRuns);
        System.ArgumentNullException.ThrowIfNull(bidiRuns);
        System.ArgumentNullException.ThrowIfNull(scalars);
        GraphemeClusters = graphemeClusters;
        WordSegments = wordSegments;
        LineBreaks = lineBreaks;
        ScriptRuns = scriptRuns;
        BidiRuns = bidiRuns;
        Scalars = scalars;
    }
}
