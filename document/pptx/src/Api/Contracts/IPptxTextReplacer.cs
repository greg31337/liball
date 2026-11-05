using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTextReplacer"/>.
/// </summary>
public interface IPptxTextReplacer
{
    /// <summary>
    /// Replaces presentation text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    int ReplacePresentationText(PptxPresentation presentation, string search, string replacement);
    /// <summary>
    /// Replaces slide text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    int ReplaceSlideText(PptxSlide slide, string search, string replacement);
}
