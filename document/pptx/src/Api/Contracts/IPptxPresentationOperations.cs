using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxPresentationOperations"/>.
/// </summary>
public interface IPptxPresentationOperations
{
    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Succeeded(PptxPresentation target);
    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Failed(PptxPresentation target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int SlideCount(PptxPresentation target);
    /// <summary>
    /// Returns the slide without changing the presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide GetSlide(PptxPresentation target, int index);
    /// <summary>
    /// Adds the image and returns the created model when applicable.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddImage(PptxPresentation target, int slideIndex, byte[] bytes, PptxBounds bounds);
    /// <summary>
    /// Adds the image and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddImageToSlide(PptxPresentation target, PptxSlide slide, byte[] bytes, PptxBounds bounds);
    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChart AddChart(PptxPresentation target, int slideIndex, string chartType, PptxChartData data, PptxBounds bounds);
    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChart AddChartToSlide(PptxPresentation target, PptxSlide slide, string chartType, PptxChartData data, PptxBounds bounds);
    /// <summary>
    /// Reports whether the presentation is macro enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsMacroEnabled(PptxPresentation target);
    /// <summary>
    /// Reports whether the presentation contains or enables macros.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasMacros(PptxPresentation target);
    /// <summary>
    /// Returns the vba project without changing the presentation.
    /// </summary>
    /// <returns>The VBA project, or <c>null</c> when the presentation has none.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxVbaProject? GetVbaProject(PptxPresentation target);
    /// <summary>
    /// Updates the vba project bytes and the underlying presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting vba project value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxVbaProject SetVbaProjectBytes(PptxPresentation target, byte[] bytes);
    /// <summary>
    /// Returns the embedded objects without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxEmbeddedObject> GetEmbeddedObjects(PptxPresentation target);
    /// <summary>
    /// Returns all ActiveX controls without activating them.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The ActiveX control inventory in slide order.</returns>
    List<PptxActiveXControl> GetActiveXControls(PptxPresentation target);
    /// <summary>
    /// Extracts images from the presentation without removing it.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxImageFile> ExtractImages(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Extracts images to directory from the presentation without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAssetExtractionResult ExtractImagesToDirectory(PptxPresentation target, string directoryPath);
    /// <summary>
    /// Extracts media to directory from the presentation without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAssetExtractionResult ExtractMediaToDirectory(PptxPresentation target, string directoryPath);
    /// <summary>
    /// Returns the media without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxMediaFile> GetMedia(PptxPresentation target);
    /// <summary>
    /// Returns the media without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxMediaFile> GetSlideMedia(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Extracts image from the presentation without removing it.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting image file value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxImageFile ExtractImage(PptxPresentation target, PptxShape shape);
    /// <summary>
    /// Replaces image while preserving unrelated presentation data.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void ReplaceImage(PptxPresentation target, PptxShape shape, byte[] bytes);
    /// <summary>
    /// Updates the slide notes text and the underlying presentation data.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideNotesText(PptxPresentation target, int slideIndex, string text);
    /// <summary>
    /// Returns the slide notes text without changing the presentation.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetSlideNotesText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Extracts text from the presentation without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractText(PptxPresentation target);
    /// <summary>
    /// Exports text in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportText(PptxPresentation target);
    /// <summary>
    /// Exports outline in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportOutline(PptxPresentation target);
    /// <summary>
    /// Exports html in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportHtml(PptxPresentation target);
    /// <summary>
    /// Exports svg in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportSvg(PptxPresentation target);
    /// <summary>
    /// Returns the outline entries without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxOutlineEntry> GetOutlineEntries(PptxPresentation target);
    /// <summary>
    /// Returns a JSON summary of presentation structure and content.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ToSummaryJson(PptxPresentation target);
    /// <summary>
    /// Extracts slide text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractSlideText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Extracts notes text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractNotesText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Extracts comments text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractCommentsText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Extracts modern comments text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractModernCommentsText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Returns the modern comments without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxModernComment> GetModernComments(PptxPresentation target);
    /// <summary>
    /// Reports whether the presentation contains or enables modern comments.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasModernComments(PptxPresentation target);
    /// <summary>
    /// Returns the animations without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxAnimationEffect> GetAnimations(PptxPresentation target);
    /// <summary>
    /// Returns all shape- and run-level hyperlinks in the presentation.
    /// </summary>
    /// <returns>The hyperlinks in slide, shape, and text order.</returns>
    /// <param name="target">The presentation to inspect.</param>
    List<PptxHyperlink> GetHyperlinks(PptxPresentation target);
    /// <summary>
    /// Audits every shape- and run-level hyperlink in the presentation.
    /// </summary>
    /// <returns>Aggregated hyperlink safety diagnostics.</returns>
    /// <param name="target">The presentation to audit.</param>
    PptxResult AuditHyperlinks(PptxPresentation target);
    /// <summary>
    /// Returns the transitions without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxSlideTransition> GetTransitions(PptxPresentation target);
    /// <summary>
    /// Returns the slide modern comments without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxModernComment> GetSlideModernComments(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Returns the comment authors without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxCommentAuthor> GetCommentAuthors(PptxPresentation target);
    /// <summary>
    /// Adds the comment author and returns the created model when applicable.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="initials">The initials value to use.</param>
    /// <returns>The resulting comment author value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCommentAuthor AddCommentAuthor(PptxPresentation target, string name, string initials);
    /// <summary>
    /// Returns the slide comments without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxComment> GetSlideComments(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Adds the slide comment and returns the created model when applicable.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="authorId">The author id value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting comment value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxComment AddSlideComment(PptxPresentation target, PptxSlide slide, int authorId, string text);
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
    PptxComment AddSlideCommentWithDetails(PptxPresentation target, PptxSlide slide, int authorId, string text, string dateTime, long x, long y);
    /// <summary>
    /// Extracts charts text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractChartsText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Extracts diagrams text from the presentation without removing it.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractDiagramsText(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Returns the slide tags without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxTag> GetSlideTags(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Returns the slide tag value without changing the presentation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetSlideTagValue(PptxPresentation target, PptxSlide slide, string name);
    /// <summary>
    /// Updates the slide tag and the underlying presentation data.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideTag(PptxPresentation target, PptxSlide slide, string name, string value);
    /// <summary>
    /// Removes the slide tag and updates dependent presentation data.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveSlideTag(PptxPresentation target, PptxSlide slide, string name);
    /// <summary>
    /// Removes all tags from a slide while preserving other content in its tag part.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="target">The data value to operate on.</param>
    void ClearSlideTags(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Removes a slide's tag part, relationship, and relationship marker.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when a tag relationship was removed; otherwise, <c>false</c>.</returns>
    bool RemoveSlideTagsPart(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Returns the custom xml parts without changing the presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxCustomXmlPart> GetCustomXmlParts(PptxPresentation target);
    /// <summary>
    /// Adds the custom xml part and returns the created model when applicable.
    /// </summary>
    /// <param name="xml">The xml value to use.</param>
    /// <returns>The resulting custom xml part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomXmlPart AddCustomXmlPart(PptxPresentation target, string xml);
    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ReplaceText(PptxPresentation target, string search, string replacement);
    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillTemplate(PptxPresentation target, PptxTemplateData data);
    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillTemplateFromDictionary(PptxPresentation target, Dictionary<string, string> values);
    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillTemplateFromLists(PptxPresentation target, List<string> keys, List<string> values);
    /// <summary>
    /// Finds the notes slide; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The matching notes slide, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxNotesSlide? FindNotesSlide(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Returns the number of slides that have notes parts.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The number of notes slides.</returns>
    int NotesSlideCount(PptxPresentation target);
    /// <summary>
    /// Returns the notes part for a slide index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The notes slide, or <c>null</c> when the slide has no notes part.</returns>
    PptxNotesSlide? GetNotesSlide(PptxPresentation target, int slideIndex);
    /// <summary>
    /// Returns or creates the notes part for a slide.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="slide">The owning slide.</param>
    /// <returns>The existing or created notes slide.</returns>
    PptxNotesSlide GetOrCreateNotesForSlide(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Removes the notes part for a slide.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="slide">The owning slide.</param>
    /// <returns><c>true</c> when a notes part was removed; otherwise, <c>false</c>.</returns>
    bool RemoveNotesSlide(PptxPresentation target, PptxSlide slide);
    /// <summary>
    /// Returns the presentation notes master.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The notes master, or <c>null</c> when none exists.</returns>
    PptxNotesMaster? GetNotesMaster(PptxPresentation target);
    /// <summary>
    /// Returns or creates the presentation notes master.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns>The existing or created notes master.</returns>
    PptxNotesMaster GetOrCreateNotesMaster(PptxPresentation target);
    /// <summary>
    /// Removes the presentation notes master and its notes-slide relationships.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns><c>true</c> when a notes master was removed; otherwise, <c>false</c>.</returns>
    bool RemoveNotesMaster(PptxPresentation target);
    /// <summary>
    /// Returns the presentation handout master.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The handout master, or <c>null</c> when none exists.</returns>
    PptxHandoutMaster? GetHandoutMaster(PptxPresentation target);
    /// <summary>
    /// Returns or creates the presentation handout master.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns>The existing or created handout master.</returns>
    PptxHandoutMaster GetOrCreateHandoutMaster(PptxPresentation target);
    /// <summary>
    /// Removes the presentation handout master.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns><c>true</c> when a handout master was removed; otherwise, <c>false</c>.</returns>
    bool RemoveHandoutMaster(PptxPresentation target);
    /// <summary>
    /// Finds the slide by id; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <returns>The matching slide, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide? FindSlideById(PptxPresentation target, int slideId);
    /// <summary>
    /// Adds the slide and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide AddSlide(PptxPresentation target);
    /// <summary>
    /// Adds the slide and returns the created model when applicable.
    /// </summary>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide AddSlideWithLayout(PptxPresentation target, string layoutName);
    /// <summary>
    /// Inserts the slide at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide InsertSlide(PptxPresentation target, int index);
    /// <summary>
    /// Inserts the slide at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide InsertSlideWithLayout(PptxPresentation target, int index, string layoutName);
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide CloneSlide(PptxPresentation target, int index);
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="insertIndex">The zero-based destination index.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide CloneSlideToIndex(PptxPresentation target, int index, int insertIndex);
    /// <summary>
    /// Imports a source slide and its dependent parts into this presentation.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide ImportSlide(PptxPresentation target, PptxPresentation sourcePresentation, int sourceIndex);
    /// <summary>
    /// Imports a source slide and its dependent parts into this presentation.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <param name="insertIndex">The zero-based destination index.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide ImportSlideToIndex(PptxPresentation target, PptxPresentation sourcePresentation, int sourceIndex, int insertIndex);
    /// <summary>
    /// Imports a source slide and its dependent parts into this presentation.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide ImportDocumentSlide(PptxPresentation target, PptxDocument sourceDocument, int sourceIndex);
    /// <summary>
    /// Imports a slide supplied through a presentation source and copies its dependent parts.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The imported slide.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlide ImportSourceSlide(PptxPresentation target, IPptxPresentationSource source, int sourceIndex);
    /// <summary>
    /// Combines the supplied presentation content with this presentation.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Merge(PptxPresentation target, PptxPresentation sourcePresentation);
    /// <summary>
    /// Combines the supplied presentation content with this presentation.
    /// </summary>
    /// <param name="sourceDocument">The source document value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int MergeDocument(PptxPresentation target, PptxDocument sourceDocument);
    /// <summary>
    /// Combines content supplied through a presentation source with this presentation.
    /// </summary>
    /// <param name="source">The source that supplies the presentation content.</param>
    /// <returns>The number of imported slides.</returns>
    /// <param name="target">The data value to operate on.</param>
    int MergeSource(PptxPresentation target, IPptxPresentationSource source);
    /// <summary>
    /// Creates independent documents from the selected slides in this presentation.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxDocument> Split(PptxPresentation target);
    /// <summary>
    /// Creates independent documents from the selected slides in this presentation.
    /// </summary>
    /// <param name="startIndex">The start index value to use.</param>
    /// <param name="count">The count value to use.</param>
    /// <returns>The resulting document value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxDocument SplitRange(PptxPresentation target, int startIndex, int count);
    /// <summary>
    /// Removes the slide at and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveSlideAt(PptxPresentation target, int index);
    /// <summary>
    /// Removes the slide by id and updates dependent presentation data.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool RemoveSlideById(PptxPresentation target, int slideId);
    /// <summary>
    /// Moves slide while preserving its related data.
    /// </summary>
    /// <param name="fromIndex">The from index value to use.</param>
    /// <param name="toIndex">The to index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void MoveSlide(PptxPresentation target, int fromIndex, int toIndex);
    /// <summary>
    /// Returns the number of slide masters.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int SlideMasterCount(PptxPresentation target);
    /// <summary>
    /// Returns the number of slide layouts.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int SlideLayoutCount(PptxPresentation target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ThemeCount(PptxPresentation target);
    /// <summary>
    /// Returns the theme without changing the presentation.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting theme value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTheme GetTheme(PptxPresentation target, int index);
    /// <summary>
    /// Returns the core properties without changing the presentation.
    /// </summary>
    /// <returns>The resulting core properties value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCoreProperties GetCoreProperties(PptxPresentation target);
    /// <summary>
    /// Returns the app properties without changing the presentation.
    /// </summary>
    /// <returns>The resulting app properties value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAppProperties GetAppProperties(PptxPresentation target);
    /// <summary>
    /// Returns the custom properties without changing the presentation.
    /// </summary>
    /// <returns>The resulting custom properties value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomProperties GetCustomProperties(PptxPresentation target);
    /// <summary>
    /// Returns presentation show properties, creating their package part when necessary.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The presentation properties.</returns>
    PptxPresentationProperties GetPresentationProperties(PptxPresentation target);
    /// <summary>
    /// Reads the package-level document thumbnail.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The thumbnail, or <c>null</c> when none exists.</returns>
    PptxThumbnail? GetThumbnail(PptxPresentation target);
    /// <summary>
    /// Adds a thumbnail after detecting its supported image format.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The thumbnail image bytes.</param>
    /// <returns>The added thumbnail.</returns>
    PptxThumbnail AddThumbnail(PptxPresentation target, byte[] bytes);
    /// <summary>
    /// Adds a thumbnail with an explicit supported image content type.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The thumbnail image bytes.</param>
    /// <param name="contentType">The thumbnail MIME content type.</param>
    /// <returns>The added thumbnail.</returns>
    PptxThumbnail AddThumbnailWithContentType(PptxPresentation target, byte[] bytes, string contentType);
    /// <summary>
    /// Replaces the current thumbnail after detecting its supported image format.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The replacement thumbnail bytes.</param>
    /// <returns>The replaced thumbnail.</returns>
    PptxThumbnail ReplaceThumbnail(PptxPresentation target, byte[] bytes);
    /// <summary>
    /// Replaces the current thumbnail with an explicit supported image content type.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="bytes">The replacement thumbnail bytes.</param>
    /// <param name="contentType">The thumbnail MIME content type.</param>
    /// <returns>The replaced thumbnail.</returns>
    PptxThumbnail ReplaceThumbnailWithContentType(PptxPresentation target, byte[] bytes, string contentType);
    /// <summary>
    /// Removes all package-level thumbnail relationships and their internal thumbnail parts.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <returns><c>true</c> when a thumbnail relationship was removed; otherwise, <c>false</c>.</returns>
    bool RemoveThumbnail(PptxPresentation target);
    /// <summary>
    /// Reads a detached snapshot of presentation-wide view, numbering, text, timing, orientation, and slide-show settings.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The presentation settings snapshot.</returns>
    PptxPresentationSettings GetSettings(PptxPresentation target);
    /// <summary>
    /// Applies presentation-wide view, numbering, text, timing, orientation, and slide-show settings.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="settings">The settings snapshot to apply.</param>
    void SetSettings(PptxPresentation target, PptxPresentationSettings settings);
    /// <summary>
    /// Returns the slide orientation without changing the presentation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetSlideOrientation(PptxPresentation target);
    /// <summary>
    /// Updates the slide size and the underlying presentation data.
    /// </summary>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideSize(PptxPresentation target, long width, long height);
    /// <summary>
    /// Updates the slide size and the underlying presentation data.
    /// </summary>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideSizeWithType(PptxPresentation target, long width, long height, string type);
    /// <summary>
    /// Updates the slide orientation and the underlying presentation data.
    /// </summary>
    /// <param name="orientation">The orientation value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideOrientation(PptxPresentation target, string orientation);
    /// <summary>
    /// Updates the slide hidden and the underlying presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="hidden">The hidden value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideHidden(PptxPresentation target, int index, bool hidden);
    /// <summary>
    /// Adds the section and returns the created model when applicable.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting section value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSection AddSection(PptxPresentation target, string name);
    /// <summary>
    /// Adds the slide to section and returns the created model when applicable.
    /// </summary>
    /// <param name="sectionIndex">The section index value to use.</param>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void AddSlideToSection(PptxPresentation target, int sectionIndex, int slideId);
    /// <summary>
    /// Returns the number of sections in the presentation.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The number of sections.</returns>
    int SectionCount(PptxPresentation target);
    /// <summary>
    /// Returns the section at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The section at the requested index.</returns>
    PptxSection GetSection(PptxPresentation target, int index);
    /// <summary>
    /// Finds a section by name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="name">The section name.</param>
    /// <returns>The matching section, or <c>null</c> when no match exists.</returns>
    PptxSection? FindSectionByName(PptxPresentation target, string name);
    /// <summary>
    /// Removes the section at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    void RemoveSectionAt(PptxPresentation target, int index);
    /// <summary>
    /// Adds the custom show and returns the created model when applicable.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting custom show value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomShow AddCustomShow(PptxPresentation target, string name);
    /// <summary>
    /// Adds the slide to custom show and returns the created model when applicable.
    /// </summary>
    /// <param name="customShowIndex">The custom show index value to use.</param>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void AddSlideToCustomShow(PptxPresentation target, int customShowIndex, int slideId);
    /// <summary>
    /// Returns the number of custom shows in the presentation.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <returns>The number of custom shows.</returns>
    int CustomShowCount(PptxPresentation target);
    /// <summary>
    /// Returns the custom show at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The custom show at the requested index.</returns>
    PptxCustomShow GetCustomShow(PptxPresentation target, int index);
    /// <summary>
    /// Finds a custom show by name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="name">The custom-show name.</param>
    /// <returns>The matching custom show, or <c>null</c> when no match exists.</returns>
    PptxCustomShow? FindCustomShowByName(PptxPresentation target, string name);
    /// <summary>
    /// Removes the custom show at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    void RemoveCustomShowAt(PptxPresentation target, int index);
    /// <summary>
    /// Finds the slide layout by name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The matching slide layout, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlideLayout? FindSlideLayoutByName(PptxPresentation target, string layoutName);
    /// <summary>
    /// Returns the slide master at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The slide master at the requested index.</returns>
    PptxSlideMaster GetSlideMaster(PptxPresentation target, int index);
    /// <summary>
    /// Returns the slide layout at the specified collection index.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The slide layout at the requested index.</returns>
    PptxSlideLayout GetSlideLayout(PptxPresentation target, int index);
    /// <summary>
    /// Finds a slide master by its declared name or part name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="masterName">The declared master name or part name.</param>
    /// <returns>The matching master, or <c>null</c> when no match exists.</returns>
    PptxSlideMaster? FindSlideMasterByName(PptxPresentation target, string masterName);
    /// <summary>
    /// Finds a slide layout by its declared type.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="layoutType">The declared layout type.</param>
    /// <returns>The matching layout, or <c>null</c> when no match exists.</returns>
    PptxSlideLayout? FindSlideLayoutByType(PptxPresentation target, string layoutType);
    /// <summary>
    /// Finds a theme by its declared name or part name.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="themeName">The declared theme name or part name.</param>
    /// <returns>The matching theme, or <c>null</c> when no match exists.</returns>
    PptxTheme? FindThemeByName(PptxPresentation target, string themeName);
    /// <summary>
    /// Returns the theme bound to a slide master.
    /// </summary>
    /// <param name="target">The presentation to inspect.</param>
    /// <param name="master">The slide master to inspect.</param>
    /// <returns>The bound theme, or <c>null</c> when the master has no theme relationship.</returns>
    PptxTheme? GetThemeForMaster(PptxPresentation target, PptxSlideMaster master);
    /// <summary>
    /// Binds the supplied theme to a slide master and updates its package relationship.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="master">The slide master to update.</param>
    /// <param name="theme">The theme to bind.</param>
    void BindTheme(PptxPresentation target, PptxSlideMaster master, PptxTheme theme);
    /// <summary>
    /// Replaces a layout on every slide currently using it.
    /// </summary>
    /// <param name="target">The presentation to update.</param>
    /// <param name="currentLayout">The layout currently assigned to slides.</param>
    /// <param name="replacementLayout">The replacement layout.</param>
    /// <returns>The number of slides updated.</returns>
    int ReplaceSlideLayout(PptxPresentation target, PptxSlideLayout currentLayout, PptxSlideLayout replacementLayout);
}
