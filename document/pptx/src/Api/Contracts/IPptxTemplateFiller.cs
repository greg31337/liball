using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTemplateFiller"/>.
/// </summary>
public interface IPptxTemplateFiller
{
    /// <summary>
    /// Fills presentation with the supplied values.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    int FillPresentation(PptxPresentation presentation, PptxTemplateData data);
    /// <summary>
    /// Fills presentation with the supplied values.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    int FillPresentationFromDictionary(PptxPresentation presentation, Dictionary<string, string> values);
    /// <summary>
    /// Fills presentation with the supplied values.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    int FillPresentationFromLists(PptxPresentation presentation, List<string> keys, List<string> values);
    /// <summary>
    /// Fills slide with the supplied values.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    int FillSlide(PptxSlide slide, PptxTemplateData data);
    /// <summary>
    /// Fills slide with the supplied values.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    int FillSlideFromDictionary(PptxSlide slide, Dictionary<string, string> values);
    /// <summary>
    /// Fills slide with the supplied values.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    int FillSlideFromLists(PptxSlide slide, List<string> keys, List<string> values);
}
