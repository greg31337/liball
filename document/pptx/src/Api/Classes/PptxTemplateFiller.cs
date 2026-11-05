using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTemplateFiller values.
/// </summary>
public class PptxTemplateFiller : IPptxTemplateFiller
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTemplateFiller()
    {
    }

    /// <summary>
    /// Fills presentation with the supplied values.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int FillPresentation(PptxPresentation presentation, PptxTemplateData data)
    {
        return PptxTemplateFillerCore.FillPresentation(presentation, data);
    }

    /// <summary>
    /// Fills presentation with the supplied values.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int FillPresentationFromDictionary(PptxPresentation presentation, Dictionary<string, string> values)
    {
        return PptxTemplateFillerCore.FillPresentation(presentation, values);
    }

    /// <summary>
    /// Fills presentation with the supplied values.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int FillPresentationFromLists(PptxPresentation presentation, List<string> keys, List<string> values)
    {
        return PptxTemplateFillerCore.FillPresentation(presentation, keys, values);
    }

    /// <summary>
    /// Fills slide with the supplied values.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int FillSlide(PptxSlide slide, PptxTemplateData data)
    {
        return PptxTemplateFillerCore.FillSlide(slide, data);
    }

    /// <summary>
    /// Fills slide with the supplied values.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int FillSlideFromDictionary(PptxSlide slide, Dictionary<string, string> values)
    {
        return PptxTemplateFillerCore.FillSlide(slide, values);
    }

    /// <summary>
    /// Fills slide with the supplied values.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    public int FillSlideFromLists(PptxSlide slide, List<string> keys, List<string> values)
    {
        return PptxTemplateFillerCore.FillSlide(slide, keys, values);
    }
}
