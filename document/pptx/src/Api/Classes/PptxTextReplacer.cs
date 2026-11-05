using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTextReplacer values.
/// </summary>
public class PptxTextReplacer : IPptxTextReplacer
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTextReplacer()
    {
    }

    /// <summary>
    /// Replaces presentation text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int ReplacePresentationText(PptxPresentation presentation, string search, string replacement)
    {
        return PptxTextReplacerCore.ReplacePresentationText(presentation, search, replacement);
    }

    /// <summary>
    /// Replaces slide text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int ReplaceSlideText(PptxSlide slide, string search, string replacement)
    {
        return PptxTextReplacerCore.ReplaceSlideText(slide, search, replacement);
    }
}
