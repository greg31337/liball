using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxPresentation values.
/// </summary>
public class PptxPresentationOperations : IPptxPresentationOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxPresentationOperations()
    {
    }

    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Succeeded(PptxPresentation target)
    {
        return target.Succeeded();
    }

    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Failed(PptxPresentation target)
    {
        return target.Failed();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int SlideCount(PptxPresentation target)
    {
        return target.SlideCount();
    }

    /// <summary>
    /// Returns the slide without changing the presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide GetSlide(PptxPresentation target, int index)
    {
        return target.GetSlide(index);
    }

    /// <summary>
    /// Adds the image and returns the created model when applicable.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddImage(PptxPresentation target, int slideIndex, byte[] bytes, PptxBounds bounds)
    {
        return target.AddImage(slideIndex, bytes, bounds);
    }

    /// <summary>
    /// Adds the image and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddImageToSlide(PptxPresentation target, PptxSlide slide, byte[] bytes, PptxBounds bounds)
    {
        return target.AddImageToSlide(slide, bytes, bounds);
    }

    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChart AddChart(PptxPresentation target, int slideIndex, string chartType, PptxChartData data, PptxBounds bounds)
    {
        return target.AddChart(slideIndex, chartType, data, bounds);
    }

    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChart AddChartToSlide(PptxPresentation target, PptxSlide slide, string chartType, PptxChartData data, PptxBounds bounds)
    {
        return target.AddChartToSlide(slide, chartType, data, bounds);
    }

    /// <summary>
    /// Reports whether the presentation is macro enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsMacroEnabled(PptxPresentation target)
    {
        return target.IsMacroEnabled();
    }

    /// <summary>
    /// Reports whether the presentation contains or enables macros.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasMacros(PptxPresentation target)
    {
        return target.HasMacros();
    }

    /// <summary>
    /// Returns the vba project without changing the presentation.
    /// </summary>
    /// <returns>The VBA project, or <c>null</c> when the presentation has none.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxVbaProject? GetVbaProject(PptxPresentation target)
    {
        return target.GetVbaProject();
    }

    /// <summary>
    /// Updates the vba project bytes and the underlying presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting vba project value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxVbaProject SetVbaProjectBytes(PptxPresentation target, byte[] bytes)
    {
        return target.SetVbaProjectBytes(bytes);
    }

    /// <summary>
    /// Returns the embedded objects without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxEmbeddedObject> GetEmbeddedObjects(PptxPresentation target)
    {
        return target.GetEmbeddedObjects();
    }

    /// <summary>
    /// Returns all ActiveX controls without activating them.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The ActiveX control inventory in slide order.</returns>
    public List<PptxActiveXControl> GetActiveXControls(PptxPresentation target)
    {
        return target.GetActiveXControls();
    }

    /// <summary>
    /// Extracts images from the presentation without removing it.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxImageFile> ExtractImages(PptxPresentation target, PptxSlide slide)
    {
        return target.ExtractImages(slide);
    }

    /// <summary>
    /// Extracts images to directory from the presentation without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxAssetExtractionResult ExtractImagesToDirectory(PptxPresentation target, string directoryPath)
    {
        return target.ExtractImagesToDirectory(directoryPath);
    }

    /// <summary>
    /// Extracts media to directory from the presentation without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxAssetExtractionResult ExtractMediaToDirectory(PptxPresentation target, string directoryPath)
    {
        return target.ExtractMediaToDirectory(directoryPath);
    }

    /// <summary>
    /// Returns the media without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxMediaFile> GetMedia(PptxPresentation target)
    {
        return target.GetMedia();
    }

    /// <summary>
    /// Returns the media without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxMediaFile> GetSlideMedia(PptxPresentation target, PptxSlide slide)
    {
        return target.GetSlideMedia(slide);
    }

    /// <summary>
    /// Extracts image from the presentation without removing it.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting image file value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxImageFile ExtractImage(PptxPresentation target, PptxShape shape)
    {
        return target.ExtractImage(shape);
    }

    /// <summary>
    /// Replaces image while preserving unrelated presentation data.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ReplaceImage(PptxPresentation target, PptxShape shape, byte[] bytes)
    {
        target.ReplaceImage(shape, bytes);
    }

    /// <summary>
    /// Updates the slide notes text and the underlying presentation data.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideNotesText(PptxPresentation target, int slideIndex, string text)
    {
        target.SetSlideNotesText(slideIndex, text);
    }

    /// <summary>
    /// Returns the slide notes text without changing the presentation.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetSlideNotesText(PptxPresentation target, int slideIndex)
    {
        return target.GetSlideNotesText(slideIndex);
    }

    /// <summary>
    /// Extracts text from the presentation without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractText(PptxPresentation target)
    {
        return target.ExtractText();
    }

    /// <summary>
    /// Exports text in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportText(PptxPresentation target)
    {
        return target.ExportText();
    }

    /// <summary>
    /// Exports outline in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportOutline(PptxPresentation target)
    {
        return target.ExportOutline();
    }

    /// <summary>
    /// Exports html in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportHtml(PptxPresentation target)
    {
        return target.ExportHtml();
    }

    /// <summary>
    /// Exports svg in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportSvg(PptxPresentation target)
    {
        return target.ExportSvg();
    }

    /// <summary>
    /// Returns the outline entries without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxOutlineEntry> GetOutlineEntries(PptxPresentation target)
    {
        return target.GetOutlineEntries();
    }

    /// <summary>
    /// Returns a JSON summary of presentation structure and content.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ToSummaryJson(PptxPresentation target)
    {
        return target.ToSummaryJson();
    }

    /// <summary>
    /// Extracts slide text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractSlideText(PptxPresentation target, int slideIndex)
    {
        return target.ExtractSlideText(slideIndex);
    }

    /// <summary>
    /// Extracts notes text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractNotesText(PptxPresentation target, int slideIndex)
    {
        return target.ExtractNotesText(slideIndex);
    }

    /// <summary>
    /// Extracts comments text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractCommentsText(PptxPresentation target, int slideIndex)
    {
        return target.ExtractCommentsText(slideIndex);
    }

    /// <summary>
    /// Extracts modern comments text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractModernCommentsText(PptxPresentation target, int slideIndex)
    {
        return target.ExtractModernCommentsText(slideIndex);
    }

    /// <summary>
    /// Returns the modern comments without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxModernComment> GetModernComments(PptxPresentation target)
    {
        return target.GetModernComments();
    }

    /// <summary>
    /// Reports whether the presentation contains or enables modern comments.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasModernComments(PptxPresentation target)
    {
        return target.HasModernComments();
    }

    /// <summary>
    /// Returns the animations without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxAnimationEffect> GetAnimations(PptxPresentation target)
    {
        return target.GetAnimations();
    }
    /// <summary>
    /// Returns all shape- and run-level hyperlinks in the presentation.
    /// </summary>
    /// <returns>The hyperlinks in slide, shape, and text order.</returns>
    /// <param name="target">The presentation to inspect.</param>
    public List<PptxHyperlink> GetHyperlinks(PptxPresentation target)
    {
        return target.GetHyperlinks();
    }
    /// <summary>
    /// Audits every shape- and run-level hyperlink in the presentation.
    /// </summary>
    /// <returns>Aggregated hyperlink safety diagnostics.</returns>
    /// <param name="target">The presentation to audit.</param>
    public PptxResult AuditHyperlinks(PptxPresentation target)
    {
        return target.AuditHyperlinks();
    }

    /// <summary>
    /// Returns the transitions without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxSlideTransition> GetTransitions(PptxPresentation target)
    {
        return target.GetTransitions();
    }

    /// <summary>
    /// Returns the slide modern comments without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxModernComment> GetSlideModernComments(PptxPresentation target, PptxSlide slide)
    {
        return target.GetSlideModernComments(slide);
    }

    /// <summary>
    /// Returns the comment authors without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxCommentAuthor> GetCommentAuthors(PptxPresentation target)
    {
        return target.GetCommentAuthors();
    }

    /// <summary>
    /// Adds the comment author and returns the created model when applicable.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="initials">The initials value to use.</param>
    /// <returns>The resulting comment author value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCommentAuthor AddCommentAuthor(PptxPresentation target, string name, string initials)
    {
        return target.AddCommentAuthor(name, initials);
    }

    /// <summary>
    /// Returns the slide comments without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxComment> GetSlideComments(PptxPresentation target, PptxSlide slide)
    {
        return target.GetSlideComments(slide);
    }

    /// <summary>
    /// Adds the slide comment and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="authorId">The author id value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting comment value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxComment AddSlideComment(PptxPresentation target, PptxSlide slide, int authorId, string text)
    {
        return target.AddSlideComment(slide, authorId, text);
    }

    /// <summary>
    /// Adds the slide comment and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="authorId">The author id value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="dateTime">The date time value to use.</param>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <returns>The resulting comment value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxComment AddSlideCommentWithDetails(PptxPresentation target, PptxSlide slide, int authorId, string text, string dateTime, long x, long y)
    {
        return target.AddSlideCommentWithDetails(slide, authorId, text, dateTime, x, y);
    }

    /// <summary>
    /// Extracts charts text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractChartsText(PptxPresentation target, int slideIndex)
    {
        return target.ExtractChartsText(slideIndex);
    }

    /// <summary>
    /// Extracts diagrams text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractDiagramsText(PptxPresentation target, int slideIndex)
    {
        return target.ExtractDiagramsText(slideIndex);
    }

    /// <summary>
    /// Returns the slide tags without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxTag> GetSlideTags(PptxPresentation target, PptxSlide slide)
    {
        return target.GetSlideTags(slide);
    }

    /// <summary>
    /// Returns the slide tag value without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetSlideTagValue(PptxPresentation target, PptxSlide slide, string name)
    {
        return target.GetSlideTagValue(slide, name);
    }

    /// <summary>
    /// Updates the slide tag and the underlying presentation data.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideTag(PptxPresentation target, PptxSlide slide, string name, string value)
    {
        target.SetSlideTag(slide, name, value);
    }

    /// <summary>
    /// Removes the slide tag and updates dependent presentation data.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveSlideTag(PptxPresentation target, PptxSlide slide, string name)
    {
        target.RemoveSlideTag(slide, name);
    }
    /// <summary>
    /// Removes all tags from a slide while preserving other content in its tag part.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ClearSlideTags(PptxPresentation target, PptxSlide slide)
    {
        target.ClearSlideTags(slide);
    }
    /// <summary>
    /// Removes a slide's tag part, relationship, and relationship marker.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when a tag relationship was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveSlideTagsPart(PptxPresentation target, PptxSlide slide)
    {
        return target.RemoveSlideTagsPart(slide);
    }

    /// <summary>
    /// Returns the custom xml parts without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxCustomXmlPart> GetCustomXmlParts(PptxPresentation target)
    {
        return target.GetCustomXmlParts();
    }

    /// <summary>
    /// Adds the custom xml part and returns the created model when applicable.
    /// </summary>
    /// <param name="xml">The xml value to use.</param>
    /// <returns>The resulting custom xml part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomXmlPart AddCustomXmlPart(PptxPresentation target, string xml)
    {
        return target.AddCustomXmlPart(xml);
    }

    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ReplaceText(PptxPresentation target, string search, string replacement)
    {
        return target.ReplaceText(search, replacement);
    }

    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int FillTemplate(PptxPresentation target, PptxTemplateData data)
    {
        return target.FillTemplate(data);
    }

    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int FillTemplateFromDictionary(PptxPresentation target, Dictionary<string, string> values)
    {
        return target.FillTemplateFromDictionary(values);
    }

    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int FillTemplateFromLists(PptxPresentation target, List<string> keys, List<string> values)
    {
        return target.FillTemplateFromLists(keys, values);
    }

    /// <summary>
    /// Finds the notes slide; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The matching notes slide, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxNotesSlide? FindNotesSlide(PptxPresentation target, PptxSlide slide)
    {
        return target.FindNotesSlide(slide);
    }
    /// <summary>
    /// Returns the number of slides that have notes parts.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The number of notes slides.</returns>
    public int NotesSlideCount(PptxPresentation target)
    {
        return target.NotesSlideCount();
    }
    /// <summary>
    /// Returns the notes part for a slide index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The notes slide, or <c>null</c> when the slide has no notes part.</returns>
    public PptxNotesSlide? GetNotesSlide(PptxPresentation target, int slideIndex)
    {
        return target.GetNotesSlide(slideIndex);
    }
    /// <summary>
    /// Returns or creates the notes part for a slide.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="slide">The owning slide.</param>
    /// <returns>The existing or created notes slide.</returns>
    public PptxNotesSlide GetOrCreateNotesForSlide(PptxPresentation target, PptxSlide slide)
    {
        target.ValidateSlide(slide);
        return target.GetOrCreateNotesSlide(slide);
    }
    /// <summary>
    /// Removes the notes part for a slide.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="slide">The owning slide.</param>
    /// <returns><c>true</c> when a notes part was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveNotesSlide(PptxPresentation target, PptxSlide slide)
    {
        return target.RemoveNotesSlide(slide);
    }
    /// <summary>
    /// Returns the presentation notes master.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The notes master, or <c>null</c> when none exists.</returns>
    public PptxNotesMaster? GetNotesMaster(PptxPresentation target)
    {
        return target.GetNotesMaster();
    }
    /// <summary>
    /// Returns or creates the presentation notes master.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns>The existing or created notes master.</returns>
    public PptxNotesMaster GetOrCreateNotesMaster(PptxPresentation target)
    {
        return target.GetOrCreateNotesMaster();
    }
    /// <summary>
    /// Removes the presentation notes master and its notes-slide relationships.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns><c>true</c> when a notes master was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveNotesMaster(PptxPresentation target)
    {
        return target.RemoveNotesMaster();
    }
    /// <summary>
    /// Returns the presentation handout master.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The handout master, or <c>null</c> when none exists.</returns>
    public PptxHandoutMaster? GetHandoutMaster(PptxPresentation target)
    {
        return target.GetHandoutMaster();
    }
    /// <summary>
    /// Returns or creates the presentation handout master.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns>The existing or created handout master.</returns>
    public PptxHandoutMaster GetOrCreateHandoutMaster(PptxPresentation target)
    {
        return target.GetOrCreateHandoutMaster();
    }
    /// <summary>
    /// Removes the presentation handout master.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns><c>true</c> when a handout master was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveHandoutMaster(PptxPresentation target)
    {
        return target.RemoveHandoutMaster();
    }

    /// <summary>
    /// Finds the slide by id; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <returns>The matching slide, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide? FindSlideById(PptxPresentation target, int slideId)
    {
        return target.FindSlideById(slideId);
    }

    /// <summary>
    /// Adds the slide and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide AddSlide(PptxPresentation target)
    {
        return target.AddSlide();
    }

    /// <summary>
    /// Adds the slide and returns the created model when applicable.
    /// </summary>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide AddSlideWithLayout(PptxPresentation target, string layoutName)
    {
        return target.AddSlideWithLayout(layoutName);
    }

    /// <summary>
    /// Inserts the slide at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide InsertSlide(PptxPresentation target, int index)
    {
        return target.InsertSlide(index);
    }

    /// <summary>
    /// Inserts the slide at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide InsertSlideWithLayout(PptxPresentation target, int index, string layoutName)
    {
        return target.InsertSlideWithLayout(index, layoutName);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide CloneSlide(PptxPresentation target, int index)
    {
        return target.CloneSlide(index);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="insertIndex">The zero-based destination index.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide CloneSlideToIndex(PptxPresentation target, int index, int insertIndex)
    {
        return target.CloneSlideToIndex(index, insertIndex);
    }

    /// <summary>
    /// Imports a source slide and its dependent parts into this presentation.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide ImportSlide(PptxPresentation target, PptxPresentation sourcePresentation, int sourceIndex)
    {
        return target.ImportSlide(sourcePresentation, sourceIndex);
    }

    /// <summary>
    /// Imports a source slide and its dependent parts into this presentation.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <param name="insertIndex">The zero-based destination index.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide ImportSlideToIndex(PptxPresentation target, PptxPresentation sourcePresentation, int sourceIndex, int insertIndex)
    {
        return target.ImportSlideToIndex(sourcePresentation, sourceIndex, insertIndex);
    }

    /// <summary>
    /// Imports a source slide and its dependent parts into this presentation.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide ImportDocumentSlide(PptxPresentation target, PptxDocument sourceDocument, int sourceIndex)
    {
        return target.ImportDocumentSlide(sourceDocument, sourceIndex);
    }

    /// <summary>
    /// Imports a slide supplied through a presentation source and copies its dependent parts.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The imported slide.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide ImportSourceSlide(PptxPresentation target, IPptxPresentationSource source, int sourceIndex)
    {
        return target.ImportSourceSlide(source, sourceIndex);
    }

    /// <summary>
    /// Combines the supplied presentation content with this presentation.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Merge(PptxPresentation target, PptxPresentation sourcePresentation)
    {
        return target.Merge(sourcePresentation);
    }

    /// <summary>
    /// Combines the supplied presentation content with this presentation.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int MergeDocument(PptxPresentation target, PptxDocument sourceDocument)
    {
        return target.MergeDocument(sourceDocument);
    }

    /// <summary>
    /// Combines content supplied through a presentation source with this presentation.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <returns>The number of imported slides.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int MergeSource(PptxPresentation target, IPptxPresentationSource source)
    {
        return target.MergeSource(source);
    }

    /// <summary>
    /// Creates independent documents from the selected slides in this presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxDocument> Split(PptxPresentation target)
    {
        return target.Split();
    }

    /// <summary>
    /// Creates independent documents from the selected slides in this presentation.
    /// </summary>
    /// <param name="startIndex">The start index value to use.</param>
    /// <param name="count">The count value to use.</param>
    /// <returns>The resulting document value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxDocument SplitRange(PptxPresentation target, int startIndex, int count)
    {
        return target.SplitRange(startIndex, count);
    }

    /// <summary>
    /// Removes the slide at and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveSlideAt(PptxPresentation target, int index)
    {
        target.RemoveSlideAt(index);
    }

    /// <summary>
    /// Removes the slide by id and updates dependent presentation data.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool RemoveSlideById(PptxPresentation target, int slideId)
    {
        return target.RemoveSlideById(slideId);
    }

    /// <summary>
    /// Moves slide while preserving its related data.
    /// </summary>
    /// <param name="fromIndex">The from index value to use.</param>
    /// <param name="toIndex">The to index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MoveSlide(PptxPresentation target, int fromIndex, int toIndex)
    {
        target.MoveSlide(fromIndex, toIndex);
    }

    /// <summary>
    /// Returns the number of slide masters.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int SlideMasterCount(PptxPresentation target)
    {
        return target.SlideMasterCount();
    }

    /// <summary>
    /// Returns the number of slide layouts.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int SlideLayoutCount(PptxPresentation target)
    {
        return target.SlideLayoutCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ThemeCount(PptxPresentation target)
    {
        return target.ThemeCount();
    }

    /// <summary>
    /// Returns the theme without changing the presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting theme value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTheme GetTheme(PptxPresentation target, int index)
    {
        return target.GetTheme(index);
    }

    /// <summary>
    /// Returns the core properties without changing the presentation.
    /// </summary>
    /// <returns>The resulting core properties value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCoreProperties GetCoreProperties(PptxPresentation target)
    {
        return target.GetCoreProperties();
    }

    /// <summary>
    /// Returns the app properties without changing the presentation.
    /// </summary>
    /// <returns>The resulting app properties value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxAppProperties GetAppProperties(PptxPresentation target)
    {
        return target.GetAppProperties();
    }

    /// <summary>
    /// Returns the custom properties without changing the presentation.
    /// </summary>
    /// <returns>The resulting custom properties value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomProperties GetCustomProperties(PptxPresentation target)
    {
        return target.GetCustomProperties();
    }

    /// <summary>
    /// Returns presentation show properties, creating their package part when necessary.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The presentation properties.</returns>
    public PptxPresentationProperties GetPresentationProperties(PptxPresentation target)
    {
        return target.GetPresentationProperties();
    }

    /// <summary>
    /// Reads the package-level document thumbnail.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The thumbnail, or <c>null</c> when none exists.</returns>
    public PptxThumbnail? GetThumbnail(PptxPresentation target)
    {
        return target.GetThumbnail();
    }

    /// <summary>
    /// Adds a thumbnail after detecting its supported image format.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The thumbnail image bytes.</param>
    /// <returns>The added thumbnail.</returns>
    public PptxThumbnail AddThumbnail(PptxPresentation target, byte[] bytes)
    {
        return target.AddThumbnail(bytes);
    }

    /// <summary>
    /// Adds a thumbnail with an explicit supported image content type.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The thumbnail image bytes.</param>
    /// <param name="contentType">The thumbnail MIME content type.</param>
    /// <returns>The added thumbnail.</returns>
    public PptxThumbnail AddThumbnailWithContentType(PptxPresentation target, byte[] bytes, string contentType)
    {
        return target.AddThumbnailWithContentType(bytes, contentType);
    }

    /// <summary>
    /// Replaces the current thumbnail after detecting its supported image format.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The replacement thumbnail bytes.</param>
    /// <returns>The replaced thumbnail.</returns>
    public PptxThumbnail ReplaceThumbnail(PptxPresentation target, byte[] bytes)
    {
        return target.ReplaceThumbnail(bytes);
    }

    /// <summary>
    /// Replaces the current thumbnail with an explicit supported image content type.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The replacement thumbnail bytes.</param>
    /// <param name="contentType">The thumbnail MIME content type.</param>
    /// <returns>The replaced thumbnail.</returns>
    public PptxThumbnail ReplaceThumbnailWithContentType(PptxPresentation target, byte[] bytes, string contentType)
    {
        return target.ReplaceThumbnailWithContentType(bytes, contentType);
    }

    /// <summary>
    /// Removes all package-level thumbnail relationships and their internal thumbnail parts.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns><c>true</c> when a thumbnail relationship was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveThumbnail(PptxPresentation target)
    {
        return target.RemoveThumbnail();
    }

    /// <summary>
    /// Reads a detached snapshot of presentation-wide view, numbering, text, timing, orientation, and slide-show settings.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The presentation settings snapshot.</returns>
    public PptxPresentationSettings GetSettings(PptxPresentation target)
    {
        return PptxPresentationSettingsCore.Read(target);
    }

    /// <summary>
    /// Applies presentation-wide view, numbering, text, timing, orientation, and slide-show settings.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="settings">The settings snapshot to apply.</param>
    public void SetSettings(PptxPresentation target, PptxPresentationSettings settings)
    {
        PptxPresentationSettingsCore.Write(target, settings);
    }

    /// <summary>
    /// Returns the slide orientation without changing the presentation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetSlideOrientation(PptxPresentation target)
    {
        return target.GetSlideOrientation();
    }

    /// <summary>
    /// Updates the slide size and the underlying presentation data.
    /// </summary>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideSize(PptxPresentation target, long width, long height)
    {
        target.SetSlideSize(width, height);
    }

    /// <summary>
    /// Updates the slide size and the underlying presentation data.
    /// </summary>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideSizeWithType(PptxPresentation target, long width, long height, string type)
    {
        target.SetSlideSizeWithType(width, height, type);
    }

    /// <summary>
    /// Updates the slide orientation and the underlying presentation data.
    /// </summary>
    /// <param name="orientation">The orientation value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideOrientation(PptxPresentation target, string orientation)
    {
        target.SetSlideOrientation(orientation);
    }

    /// <summary>
    /// Updates the slide hidden and the underlying presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="hidden">The hidden value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideHidden(PptxPresentation target, int index, bool hidden)
    {
        target.SetSlideHidden(index, hidden);
    }

    /// <summary>
    /// Adds the section and returns the created model when applicable.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting section value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSection AddSection(PptxPresentation target, string name)
    {
        return target.AddSection(name);
    }

    /// <summary>
    /// Adds the slide to section and returns the created model when applicable.
    /// </summary>
    /// <param name="sectionIndex">The section index value to use.</param>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void AddSlideToSection(PptxPresentation target, int sectionIndex, int slideId)
    {
        target.AddSlideToSection(sectionIndex, slideId);
    }

    /// <summary>
    /// Returns the number of sections in the presentation.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The number of sections.</returns>
    public int SectionCount(PptxPresentation target)
    {
        return target.SectionCount();
    }

    /// <summary>
    /// Returns the section at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The section at the requested index.</returns>
    public PptxSection GetSection(PptxPresentation target, int index)
    {
        return target.GetSection(index);
    }

    /// <summary>
    /// Finds a section by name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="name">The section name.</param>
    /// <returns>The matching section, or <c>null</c> when no match exists.</returns>
    public PptxSection? FindSectionByName(PptxPresentation target, string name)
    {
        return target.FindSectionByName(name);
    }

    /// <summary>
    /// Removes the section at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    public void RemoveSectionAt(PptxPresentation target, int index)
    {
        target.RemoveSectionAt(index);
    }

    /// <summary>
    /// Adds the custom show and returns the created model when applicable.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting custom show value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomShow AddCustomShow(PptxPresentation target, string name)
    {
        return target.AddCustomShow(name);
    }

    /// <summary>
    /// Adds the slide to custom show and returns the created model when applicable.
    /// </summary>
    /// <param name="customShowIndex">The custom show index value to use.</param>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void AddSlideToCustomShow(PptxPresentation target, int customShowIndex, int slideId)
    {
        target.AddSlideToCustomShow(customShowIndex, slideId);
    }

    /// <summary>
    /// Returns the number of custom shows in the presentation.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The number of custom shows.</returns>
    public int CustomShowCount(PptxPresentation target)
    {
        return target.CustomShowCount();
    }

    /// <summary>
    /// Returns the custom show at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The custom show at the requested index.</returns>
    public PptxCustomShow GetCustomShow(PptxPresentation target, int index)
    {
        return target.GetCustomShow(index);
    }

    /// <summary>
    /// Finds a custom show by name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="name">The custom-show name.</param>
    /// <returns>The matching custom show, or <c>null</c> when no match exists.</returns>
    public PptxCustomShow? FindCustomShowByName(PptxPresentation target, string name)
    {
        return target.FindCustomShowByName(name);
    }

    /// <summary>
    /// Removes the custom show at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    public void RemoveCustomShowAt(PptxPresentation target, int index)
    {
        target.RemoveCustomShowAt(index);
    }

    /// <summary>
    /// Finds the slide layout by name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The matching slide layout, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlideLayout? FindSlideLayoutByName(PptxPresentation target, string layoutName)
    {
        return target.FindSlideLayoutByName(layoutName);
    }

    /// <summary>
    /// Returns the slide master at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The slide master at the requested index.</returns>
    public PptxSlideMaster GetSlideMaster(PptxPresentation target, int index)
    {
        return target.GetSlideMaster(index);
    }

    /// <summary>
    /// Returns the slide layout at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The slide layout at the requested index.</returns>
    public PptxSlideLayout GetSlideLayout(PptxPresentation target, int index)
    {
        return target.GetSlideLayout(index);
    }

    /// <summary>
    /// Finds a slide master by its declared name or part name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="masterName">The declared master name or part name.</param>
    /// <returns>The matching master, or <c>null</c> when no match exists.</returns>
    public PptxSlideMaster? FindSlideMasterByName(PptxPresentation target, string masterName)
    {
        return target.FindSlideMasterByName(masterName);
    }

    /// <summary>
    /// Finds a slide layout by its declared type.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="layoutType">The declared layout type.</param>
    /// <returns>The matching layout, or <c>null</c> when no match exists.</returns>
    public PptxSlideLayout? FindSlideLayoutByType(PptxPresentation target, string layoutType)
    {
        return target.FindSlideLayoutByType(layoutType);
    }

    /// <summary>
    /// Finds a theme by its declared name or part name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="themeName">The declared theme name or part name.</param>
    /// <returns>The matching theme, or <c>null</c> when no match exists.</returns>
    public PptxTheme? FindThemeByName(PptxPresentation target, string themeName)
    {
        return target.FindThemeByName(themeName);
    }

    /// <summary>
    /// Returns the theme bound to a slide master.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="master">The slide master to inspect.</param>
    /// <returns>The bound theme, or <c>null</c> when the master has no theme relationship.</returns>
    public PptxTheme? GetThemeForMaster(PptxPresentation target, PptxSlideMaster master)
    {
        return target.GetThemeForMaster(master);
    }

    /// <summary>
    /// Binds the supplied theme to a slide master and updates its package relationship.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="master">The slide master to update.</param>
    /// <param name="theme">The theme to bind.</param>
    public void BindTheme(PptxPresentation target, PptxSlideMaster master, PptxTheme theme)
    {
        target.BindTheme(master, theme);
    }

    /// <summary>
    /// Replaces a layout on every slide currently using it.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="currentLayout">The layout currently assigned to slides.</param>
    /// <param name="replacementLayout">The replacement layout.</param>
    /// <returns>The number of slides updated.</returns>
    public int ReplaceSlideLayout(PptxPresentation target, PptxSlideLayout currentLayout, PptxSlideLayout replacementLayout)
    {
        return target.ReplaceSlideLayout(currentLayout, replacementLayout);
    }
}
