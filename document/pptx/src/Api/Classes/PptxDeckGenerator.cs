using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Generates deterministic default decks and common slide types.
/// </summary>
public class PptxDeckGenerator : IPptxDeckGenerator
{
    /// <summary>
    /// Creates a deck generator instance.
    /// </summary>
    public PptxDeckGenerator()
    {
    }

    /// <summary>
    /// Creates an empty presentation with a default theme, master, and common built-in layouts.
    /// </summary>
    /// <returns>A valid editable presentation document.</returns>
    public PptxDocument CreateDefaultDeck()
    {
        return PptxDeckGeneratorCore.CreateDefaultDeck();
    }

    /// <summary>
    /// Adds a title and subtitle slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="subtitle">The slide subtitle.</param>
    /// <returns>The created slide.</returns>
    public PptxSlide AddTitleSlide(PptxPresentation presentation, string title, string subtitle)
    {
        return PptxDeckGeneratorCore.AddTitleSlide(presentation, title, subtitle);
    }

    /// <summary>
    /// Adds a title and plain content slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="content">The slide body text.</param>
    /// <returns>The created slide.</returns>
    public PptxSlide AddContentSlide(PptxPresentation presentation, string title, string content)
    {
        return PptxDeckGeneratorCore.AddContentSlide(presentation, title, content);
    }

    /// <summary>
    /// Adds a title and bulleted-content slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="items">The bullet items in paragraph order.</param>
    /// <returns>The created slide.</returns>
    public PptxSlide AddBulletSlide(PptxPresentation presentation, string title, List<string> items)
    {
        return PptxDeckGeneratorCore.AddBulletSlide(presentation, title, items);
    }

    /// <summary>
    /// Adds a title and embedded-image slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    /// <returns>The created slide.</returns>
    public PptxSlide AddImageSlide(PptxPresentation presentation, string title, byte[] bytes, string contentType)
    {
        return PptxDeckGeneratorCore.AddImageSlide(presentation, title, bytes, contentType);
    }

    /// <summary>
    /// Adds a title and table slide populated from rectangular text data.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="data">The rectangular table data.</param>
    /// <returns>The created slide.</returns>
    public PptxSlide AddTableSlide(PptxPresentation presentation, string title, List<List<string>> data)
    {
        return PptxDeckGeneratorCore.AddTableSlide(presentation, title, data);
    }

    /// <summary>
    /// Adds a title and chart slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="chartType">The supported chart type.</param>
    /// <param name="data">The chart data.</param>
    /// <returns>The created slide.</returns>
    public PptxSlide AddChartSlide(PptxPresentation presentation, string title, string chartType, PptxChartData data)
    {
        return PptxDeckGeneratorCore.AddChartSlide(presentation, title, chartType, data);
    }
}
