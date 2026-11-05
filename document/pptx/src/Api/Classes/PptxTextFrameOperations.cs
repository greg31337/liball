using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTextFrame values.
/// </summary>
public class PptxTextFrameOperations : IPptxTextFrameOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTextFrameOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ParagraphCount(PptxTextFrame target)
    {
        return target.ParagraphCount();
    }

    /// <summary>
    /// Returns the paragraph without changing the text frame.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting paragraph value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxParagraph GetParagraph(PptxTextFrame target, int index)
    {
        return target.GetParagraph(index);
    }

    /// <summary>
    /// Reports whether the text frame contains or enables text.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasText(PptxTextFrame target)
    {
        return target.HasText();
    }

    /// <summary>
    /// Returns the text without changing the text frame.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetText(PptxTextFrame target)
    {
        return target.GetText();
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxTextFrame target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Clears the editable content while leaving the owning object available for reuse.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Clear(PptxTextFrame target)
    {
        target.Clear();
    }

    /// <summary>
    /// Adds the paragraph and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting paragraph value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxParagraph AddParagraph(PptxTextFrame target, string text)
    {
        return target.AddParagraph(text);
    }

    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ReplaceText(PptxTextFrame target, string search, string replacement)
    {
        return target.ReplaceText(search, replacement);
    }

    /// <summary>
    /// Inspects typed body properties without changing the text frame.
    /// </summary>
    /// <param name="target">The text frame to inspect.</param>
    /// <returns>The typed body settings, including DrawingML defaults.</returns>
    public PptxTextFrameBody GetBody(PptxTextFrame target)
    {
        return target.GetBodySettings();
    }

    /// <summary>
    /// Updates all text-frame margins in English Metric Units.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="margins">The nonnegative text insets.</param>
    public void SetMargins(PptxTextFrame target, PptxMargins margins)
    {
        target.SetBodyMargins(margins);
    }

    /// <summary>
    /// Updates vertical and centered anchoring.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="verticalAlignment">The vertical anchor value or a supported friendly name.</param>
    /// <param name="centered">Whether to center text around its anchor.</param>
    public void SetAnchoring(PptxTextFrame target, string verticalAlignment, bool centered)
    {
        target.SetBodyAnchoring(verticalAlignment, centered);
    }

    /// <summary>
    /// Updates automatic fitting and its normal-autofit percentages.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="mode"><c>none</c>, <c>normal</c>, <c>shape</c>, or an empty inherited value.</param>
    /// <param name="fontScale">The font scale from zero through 100000.</param>
    /// <param name="lineSpacingReduction">The line-spacing reduction from zero through 100000.</param>
    public void SetAutofit(PptxTextFrame target, string mode, int fontScale, int lineSpacingReduction)
    {
        target.SetBodyAutofit(mode, fontScale, lineSpacingReduction);
    }

    /// <summary>
    /// Updates text wrapping.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="wrapping"><c>square</c>, <c>none</c>, or an empty inherited value.</param>
    public void SetWrapping(PptxTextFrame target, string wrapping)
    {
        target.SetBodyWrapping(wrapping);
    }

    /// <summary>
    /// Updates horizontal and vertical overflow behavior.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="horizontal">Horizontal <c>overflow</c>, <c>clip</c>, or an empty inherited value.</param>
    /// <param name="vertical">Vertical <c>overflow</c>, <c>ellipsis</c>, <c>clip</c>, or an empty inherited value.</param>
    public void SetOverflow(PptxTextFrame target, string horizontal, string vertical)
    {
        target.SetBodyOverflow(horizontal, vertical);
    }

    /// <summary>
    /// Updates the vertical text mode.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="mode">A DrawingML vertical text value or supported friendly name.</param>
    public void SetVerticalText(PptxTextFrame target, string mode)
    {
        target.SetBodyVerticalText(mode);
    }

    /// <summary>
    /// Updates text rotation and upright-glyph behavior.
    /// </summary>
    /// <param name="target">The text frame to update.</param>
    /// <param name="rotation">Rotation in DrawingML angle units.</param>
    /// <param name="upright">Whether glyphs remain upright.</param>
    public void SetOrientation(PptxTextFrame target, int rotation, bool upright)
    {
        target.SetBodyOrientation(rotation, upright);
    }

    /// <summary>
    /// Estimates whether the text fits inside its shape without platform font dependencies.
    /// </summary>
    /// <param name="target">The text frame to inspect.</param>
    /// <returns>The basic fitting estimate.</returns>
    public PptxTextFit InspectFit(PptxTextFrame target)
    {
        return PptxTextFitCore.Inspect(target);
    }
}
