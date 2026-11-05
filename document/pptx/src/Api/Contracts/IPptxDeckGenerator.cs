using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Defines deterministic default-deck and common slide generation operations.
/// </summary>
public interface IPptxDeckGenerator
{
    /// <summary>
    /// Creates an empty presentation with a default theme, master, and common built-in layouts.
    /// </summary>
    /// <returns>A valid editable presentation document.</returns>
    PptxDocument CreateDefaultDeck();
    /// <summary>
    /// Adds a title and subtitle slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="subtitle">The slide subtitle.</param>
    /// <returns>The created slide.</returns>
    PptxSlide AddTitleSlide(PptxPresentation presentation, string title, string subtitle);
    /// <summary>
    /// Adds a title and plain content slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="content">The slide body text.</param>
    /// <returns>The created slide.</returns>
    PptxSlide AddContentSlide(PptxPresentation presentation, string title, string content);
    /// <summary>
    /// Adds a title and bulleted-content slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="items">The bullet items in paragraph order.</param>
    /// <returns>The created slide.</returns>
    PptxSlide AddBulletSlide(PptxPresentation presentation, string title, List<string> items);
    /// <summary>
    /// Adds a title and embedded-image slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    /// <returns>The created slide.</returns>
    PptxSlide AddImageSlide(PptxPresentation presentation, string title, byte[] bytes, string contentType);
    /// <summary>
    /// Adds a title and table slide populated from rectangular text data.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="data">The rectangular table data.</param>
    /// <returns>The created slide.</returns>
    PptxSlide AddTableSlide(PptxPresentation presentation, string title, List<List<string>> data);
    /// <summary>
    /// Adds a title and chart slide.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="title">The slide title.</param>
    /// <param name="chartType">The supported chart type.</param>
    /// <param name="data">The chart data.</param>
    /// <returns>The created slide.</returns>
    PptxSlide AddChartSlide(PptxPresentation presentation, string title, string chartType, PptxChartData data);
}
