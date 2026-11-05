namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontBuilder"/>.</summary>
public interface IPdfFontBuilder
{
    /// <summary>Gets the immutable font instance bound to this builder.</summary>
    /// <returns>The constructor-supplied source instance.</returns>
    FontInstance GetSourceInstance();

    /// <summary>Gets a copy of the effective PDF font options.</summary>
    /// <returns>A defensive copy of the constructor-time option snapshot.</returns>
    PdfFontOptions GetFontOptions();

    /// <summary>Adds cmap-mapped Unicode text to the resource usage.</summary>
    /// <param name="text">The strict UTF-16 source text.</param>
    void AddText(string text);

    /// <summary>Adds one source glyph and optional original Unicode sequence.</summary>
    /// <param name="glyphId">The source-font glyph identifier.</param>
    /// <param name="unicodeSequence">Zero or more valid Unicode scalars.</param>
    void AddGlyph(int glyphId, int[] unicodeSequence);

    /// <summary>Encodes and adds one shaped run with zero baseline shift.</summary>
    /// <param name="run">The immutable shaped glyph run.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    PdfEncodedRun AddShapedRun(ShapedRun run);

    /// <summary>Encodes and adds one shaped run with a baseline shift.</summary>
    /// <param name="run">The immutable shaped glyph run.</param>
    /// <param name="baselineShift">A finite user-space point shift.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    PdfEncodedRun AddShapedRunWithBaselineShift(ShapedRun run, double baselineShift);

    /// <summary>Encodes one fallback-resolved run from this builder's instance.</summary>
    /// <param name="run">The resolved shaping run.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run was resolved to a different font instance.
    /// </exception>
    PdfEncodedRun AddResolvedRun(TextShapingRun run);

    /// <summary>Encodes one fallback-resolved run with a baseline shift.</summary>
    /// <param name="run">The resolved shaping run.</param>
    /// <param name="baselineShift">A finite user-space point shift.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run was resolved to a different font instance.
    /// </exception>
    PdfEncodedRun AddResolvedRunWithBaselineShift(
        TextShapingRun run,
        double baselineShift);

    /// <summary>Builds and freezes the selected PDF font resource.</summary>
    /// <returns>The immutable finalized resource.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This builder has already finalized successfully.
    /// </exception>
    PdfFontResource FinalizeFont();

    /// <summary>Reports whether finalization completed successfully.</summary>
    /// <returns>True after the one successful finalization.</returns>
    bool IsFinalized();

    /// <summary>Gets the successfully finalized resource.</summary>
    /// <returns>The same immutable object returned by <see cref="FinalizeFont"/>.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Finalization has not completed successfully.
    /// </exception>
    PdfFontResource GetFinalizedResource();

    /// <summary>Gets the current number of allocated character codes.</summary>
    /// <returns>The dense code count including reserved code zero.</returns>
    int GetCharacterCodeCount();
}
