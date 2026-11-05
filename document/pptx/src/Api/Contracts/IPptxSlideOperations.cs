using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxSlideOperations"/>.
/// </summary>
public interface IPptxSlideOperations
{
    /// <summary>
    /// Returns the notes without changing the slide.
    /// </summary>
    /// <returns>The notes slide, or <c>null</c> when the slide has no notes.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxNotesSlide? GetNotes(PptxSlide target);
    /// <summary>
    /// Returns the slide's notes model, creating the notes part and relationship when they do not already exist.
    /// </summary>
    /// <returns>The resulting notes slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxNotesSlide GetOrCreateNotes(PptxSlide target);
    /// <summary>
    /// Returns the notes text without changing the slide.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetNotesText(PptxSlide target);
    /// <summary>
    /// Updates the notes text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetNotesText(PptxSlide target, string text);
    /// <summary>
    /// Removes the notes part related to the slide.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <returns><c>true</c> when a notes part was removed; otherwise, <c>false</c>.</returns>
    bool RemoveNotes(PptxSlide target);
    /// <summary>
    /// Returns the comments without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxComment> GetComments(PptxSlide target);
    /// <summary>
    /// Adds the comment and returns the created model when applicable.
    /// </summary>
    /// <param name="authorId">The author id value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting comment value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxComment AddComment(PptxSlide target, int authorId, string text);
    /// <summary>
    /// Adds the comment and returns the created model when applicable.
    /// </summary>
    /// <param name="authorId">The author id value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="dateTime">The date time value to use.</param>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <returns>The resulting comment value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxComment AddCommentWithDetails(PptxSlide target, int authorId, string text, string dateTime, long x, long y);
    /// <summary>
    /// Returns the modern comments without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxModernComment> GetModernComments(PptxSlide target);
    /// <summary>
    /// Reports whether the slide contains or enables modern comments.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasModernComments(PptxSlide target);
    /// <summary>
    /// Returns the tags without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxTag> GetTags(PptxSlide target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int TagCount(PptxSlide target);
    /// <summary>
    /// Returns the tag value without changing the slide.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetTagValue(PptxSlide target, string name);
    /// <summary>
    /// Updates the tag and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTag(PptxSlide target, string name, string value);
    /// <summary>
    /// Removes the tag and updates dependent presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveTag(PptxSlide target, string name);
    /// <summary>
    /// Removes all tags while preserving other content in the tag part.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    void ClearTags(PptxSlide target);
    /// <summary>
    /// Removes the slide tag part, relationship, and relationship marker.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <returns><c>true</c> when a tag relationship was removed; otherwise, <c>false</c>.</returns>
    bool RemoveTagPart(PptxSlide target);
    /// <summary>
    /// Reports whether the slide contains or enables timing.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasTiming(PptxSlide target);
    /// <summary>
    /// Returns the animations without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxAnimationEffect> GetAnimations(PptxSlide target);
    /// <summary>
    /// Returns all shape- and run-level hyperlinks on the slide.
    /// </summary>
    /// <returns>The hyperlinks in shape and text order.</returns>
    /// <param name="target">The slide to inspect.</param>
    List<PptxHyperlink> GetHyperlinks(PptxSlide target);
    /// <summary>
    /// Audits every shape- and run-level hyperlink on the slide.
    /// </summary>
    /// <returns>Aggregated hyperlink safety diagnostics.</returns>
    /// <param name="target">The slide to audit.</param>
    PptxResult AuditHyperlinks(PptxSlide target);
    /// <summary>
    /// Returns the transition without changing the slide.
    /// </summary>
    /// <returns>The resulting slide transition value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxSlideTransition GetTransition(PptxSlide target);
    /// <summary>
    /// Reports whether the slide contains or enables transition.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasTransition(PptxSlide target);
    /// <summary>
    /// Updates the transition and the underlying presentation data.
    /// </summary>
    /// <param name="transitionType">The transition type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTransition(PptxSlide target, string transitionType);
    /// <summary>
    /// Updates the transition and the underlying presentation data.
    /// </summary>
    /// <param name="transitionType">The transition type value to use.</param>
    /// <param name="speed">The speed value to use.</param>
    /// <param name="advanceOnClick">The advance on click value to use.</param>
    /// <param name="advanceAfterTime">The advance after time value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTransitionOptions(PptxSlide target, string transitionType, string speed, bool advanceOnClick, string advanceAfterTime);
    /// <summary>
    /// Removes the transition and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void RemoveTransition(PptxSlide target);
    /// <summary>
    /// Adds the animation and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeId">The shape id value to use.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="effect">The effect value to use.</param>
    /// <returns>The resulting animation effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAnimationEffect AddAnimation(PptxSlide target, int shapeId, string type, string effect);
    /// <summary>
    /// Adds the animation and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeId">The shape id value to use.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="effect">The effect value to use.</param>
    /// <param name="trigger">The trigger value to use.</param>
    /// <param name="duration">The duration value to use.</param>
    /// <param name="delay">The delay value to use.</param>
    /// <param name="repeatCount">The repeat count value to use.</param>
    /// <returns>The resulting animation effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAnimationEffect AddAnimationWithTiming(PptxSlide target, int shapeId, string type, string effect, string trigger, string duration, string delay, string repeatCount);
    /// <summary>
    /// Adds a common entrance animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="effect">The entrance filter name, such as <c>fade</c>.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    PptxAnimationEffect AddEntranceAnimation(PptxSlide target, int shapeId, string effect);
    /// <summary>
    /// Adds a common emphasis animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="effect">The emphasis filter name.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    PptxAnimationEffect AddEmphasisAnimation(PptxSlide target, int shapeId, string effect);
    /// <summary>
    /// Adds a common exit animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="effect">The exit filter name, such as <c>fade</c>.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    PptxAnimationEffect AddExitAnimation(PptxSlide target, int shapeId, string effect);
    /// <summary>
    /// Adds a motion-path animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="path">The PresentationML motion path.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    PptxAnimationEffect AddMotionPathAnimation(PptxSlide target, int shapeId, string path);
    /// <summary>
    /// Adds a media command animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target media shape identifier.</param>
    /// <param name="command">The media command, such as <c>playFrom(0.0)</c>.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    PptxAnimationEffect AddMediaAnimation(PptxSlide target, int shapeId, string command);
    /// <summary>
    /// Removes the animation and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveAnimation(PptxSlide target, int index);
    /// <summary>
    /// Moves animation while preserving its related data.
    /// </summary>
    /// <param name="oldIndex">The old index value to use.</param>
    /// <param name="newIndex">The new index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void MoveAnimation(PptxSlide target, int oldIndex, int newIndex);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int PlaceholderCount(PptxSlide target);
    /// <summary>
    /// Finds the placeholder; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <returns>The matching placeholder, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPlaceholder? FindPlaceholder(PptxSlide target, string type);
    /// <summary>
    /// Finds the placeholder; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <returns>The matching placeholder, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPlaceholder? FindPlaceholderByIndex(PptxSlide target, string type, int placeholderIndex);
    /// <summary>
    /// Reports whether the slide contains or enables placeholder.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasPlaceholder(PptxSlide target, string type);
    /// <summary>
    /// Returns the shape tree without changing the slide.
    /// </summary>
    /// <returns>The resulting shape tree value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShapeTree GetShapeTree(PptxSlide target);
    /// <summary>
    /// Adds the shape and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeType">The shape type value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddShape(PptxSlide target, string shapeType, PptxBounds bounds);
    /// <summary>
    /// Adds the auto shape and returns the created model when applicable.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddAutoShape(PptxSlide target, string presetGeometry, PptxBounds bounds);
    /// <summary>
    /// Adds the text box and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddTextBox(PptxSlide target, string text, PptxBounds bounds);
    /// <summary>
    /// Adds the connector and returns the created model when applicable.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <param name="start">The start value to use.</param>
    /// <param name="end">The end value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddConnector(PptxSlide target, string connectorType, PptxPoint start, PptxPoint end);
    /// <summary>
    /// Adds the freeform and returns the created model when applicable.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="points">The points value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddFreeform(PptxSlide target, PptxBounds bounds, List<PptxPoint> points);
    /// <summary>
    /// Adds the image and returns the created model when applicable.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddImage(PptxSlide target, byte[] bytes, PptxBounds bounds);
    /// <summary>
    /// Adds the table and returns the created model when applicable.
    /// </summary>
    /// <param name="rowCount">The number of rows. The value must be positive where a new table or range is created.</param>
    /// <param name="columnCount">The number of columns. The value must be positive where a new table or range is created.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting table value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTable AddTable(PptxSlide target, int rowCount, int columnCount, PptxBounds bounds);
    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChart AddChart(PptxSlide target, string chartType, PptxChartData data, PptxBounds bounds);
    /// <summary>
    /// Returns the tables without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxTable> GetTables(PptxSlide target);
    /// <summary>
    /// Returns the table without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting table value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTable GetTable(PptxSlide target, int index);
    /// <summary>
    /// Returns the charts without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxChart> GetCharts(PptxSlide target);
    /// <summary>
    /// Returns the chart without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChart GetChart(PptxSlide target, int index);
    /// <summary>
    /// Extracts chart data tables from the slide without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<List<List<string>>> ExtractChartDataTables(PptxSlide target);
    /// <summary>
    /// Returns the diagrams without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxDiagram> GetDiagrams(PptxSlide target);
    /// <summary>
    /// Returns the diagram without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting diagram value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxDiagram GetDiagram(PptxSlide target, int index);
    /// <summary>
    /// Returns the embedded objects without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxEmbeddedObject> GetEmbeddedObjects(PptxSlide target);
    /// <summary>
    /// Returns the embedded object without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting embedded object value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxEmbeddedObject GetEmbeddedObject(PptxSlide target, int index);
    /// <summary>
    /// Returns the ActiveX controls on the slide without activating them.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <returns>The ActiveX control inventory.</returns>
    List<PptxActiveXControl> GetActiveXControls(PptxSlide target);
    /// <summary>
    /// Returns one ActiveX control without activating it.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <param name="index">The zero-based control index.</param>
    /// <returns>The selected ActiveX control.</returns>
    PptxActiveXControl GetActiveXControl(PptxSlide target, int index);
    /// <summary>
    /// Extracts images from the slide without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxImageFile> ExtractImages(PptxSlide target);
    /// <summary>
    /// Extracts images to directory from the slide without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAssetExtractionResult ExtractImagesToDirectory(PptxSlide target, string directoryPath);
    /// <summary>
    /// Returns the media without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxMediaFile> GetMedia(PptxSlide target);
    /// <summary>
    /// Extracts media to directory from the slide without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxAssetExtractionResult ExtractMediaToDirectory(PptxSlide target, string directoryPath);
    /// <summary>
    /// Extracts text from the slide without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractText(PptxSlide target);
    /// <summary>
    /// Exports text in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportText(PptxSlide target);
    /// <summary>
    /// Exports outline in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportOutline(PptxSlide target);
    /// <summary>
    /// Exports html in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportHtml(PptxSlide target);
    /// <summary>
    /// Exports svg in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExportSvg(PptxSlide target);
    /// <summary>
    /// Returns the outline entries without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxOutlineEntry> GetOutlineEntries(PptxSlide target);
    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ReplaceText(PptxSlide target, string search, string replacement);
    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillTemplate(PptxSlide target, PptxTemplateData data);
    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillTemplateFromDictionary(PptxSlide target, Dictionary<string, string> values);
    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillTemplateFromLists(PptxSlide target, List<string> keys, List<string> values);
    /// <summary>
    /// Returns the background fill without changing the slide.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxFill GetBackgroundFill(PptxSlide target);
    /// <summary>
    /// Updates the background and the underlying presentation data.
    /// </summary>
    /// <param name="fill">The fill value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBackground(PptxSlide target, PptxFill fill);
    /// <summary>
    /// Applies a slide layout and updates the slide relationship.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="layout">The layout to apply.</param>
    void ApplyLayout(PptxSlide target, PptxSlideLayout layout);
    /// <summary>
    /// Inspects matching placeholder declarations on the slide, layout, and master.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <param name="placeholderType">The placeholder type.</param>
    /// <param name="placeholderIndex">The placeholder index, or <c>-1</c> for an unindexed placeholder.</param>
    /// <returns>The matching placeholder inheritance chain.</returns>
    PptxPlaceholderInheritance InspectPlaceholderInheritance(PptxSlide target, string placeholderType, int placeholderIndex);
    /// <summary>
    /// Resolves the slide background through its layout and master.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <returns>The effective background and its source.</returns>
    PptxInheritedBackground GetEffectiveBackground(PptxSlide target);
    /// <summary>
    /// Finds the shape by id; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindShapeById(PptxSlide target, int id);
    /// <summary>
    /// Finds the shape by name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindShapeByName(PptxSlide target, string name);
    /// <summary>
    /// Fills title with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulateTitle(PptxSlide target, string text);
    /// <summary>
    /// Fills body with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulateBody(PptxSlide target, string text);
    /// <summary>
    /// Fills picture with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulatePicture(PptxSlide target, string text);
    /// <summary>
    /// Fills table with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulateTable(PptxSlide target, string text);
    /// <summary>
    /// Fills chart with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulateChart(PptxSlide target, string text);
    /// <summary>
    /// Fills media with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulateMedia(PptxSlide target, string text);
    /// <summary>
    /// Fills content with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulateContent(PptxSlide target, string text);
    /// <summary>
    /// Fills placeholder with the supplied values.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulatePlaceholder(PptxSlide target, string type, string text);
    /// <summary>
    /// Updates the placeholder text and the underlying presentation data.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetPlaceholderText(PptxSlide target, string type, string text);
    /// <summary>
    /// Updates the placeholder text and the underlying presentation data.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetPlaceholderTextByIndex(PptxSlide target, string type, int placeholderIndex, string text);
    /// <summary>
    /// Fills placeholder with the supplied values.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void PopulatePlaceholderByIndex(PptxSlide target, string type, int placeholderIndex, string text);
    /// <summary>
    /// Replaces a picture or generic content placeholder with an image while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>picture</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <returns>The populated picture shape.</returns>
    PptxShape PopulatePlaceholderImage(PptxSlide target, string type, int placeholderIndex, byte[] bytes);
    /// <summary>
    /// Replaces a table or generic content placeholder with a table while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>table</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="rowCount">The number of rows.</param>
    /// <param name="columnCount">The number of columns.</param>
    /// <returns>The populated table.</returns>
    PptxTable PopulatePlaceholderTable(PptxSlide target, string type, int placeholderIndex, int rowCount, int columnCount);
    /// <summary>
    /// Replaces a chart or generic content placeholder with a chart while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>chart</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="chartType">The chart type.</param>
    /// <param name="data">The chart data.</param>
    /// <returns>The populated chart.</returns>
    PptxChart PopulatePlaceholderChart(PptxSlide target, string type, int placeholderIndex, string chartType, PptxChartData data);
    /// <summary>
    /// Replaces a media or generic content placeholder with embedded audio or video while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>media</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="bytes">The media bytes.</param>
    /// <param name="contentType">The media MIME content type.</param>
    /// <param name="mediaType">Either <c>audio</c> or <c>video</c>.</param>
    /// <returns>The populated media file.</returns>
    PptxMediaFile PopulatePlaceholderMedia(PptxSlide target, string type, int placeholderIndex, byte[] bytes, string contentType, string mediaType);
    /// <summary>
    /// Replaces an object or generic content placeholder with an embedded object while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>object</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="bytes">The embedded object bytes.</param>
    /// <param name="contentType">The embedded object MIME content type.</param>
    /// <param name="programId">The OLE program identifier or package role.</param>
    /// <param name="objectName">The display name.</param>
    /// <returns>The populated embedded object.</returns>
    PptxEmbeddedObject PopulatePlaceholderObject(PptxSlide target, string type, int placeholderIndex, byte[] bytes, string contentType, string programId, string objectName);
    /// <summary>
    /// Updates the table cell text and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTableCellText(PptxSlide target, int rowIndex, int columnIndex, string text);
    /// <summary>
    /// Updates the table cell text and the underlying presentation data.
    /// </summary>
    /// <param name="tableIndex">The table index value to use.</param>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTableCellTextInTable(PptxSlide target, int tableIndex, int rowIndex, int columnIndex, string text);
    /// <summary>
    /// Returns the table cell text without changing the slide.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetTableCellText(PptxSlide target, int rowIndex, int columnIndex);
    /// <summary>
    /// Returns the table cell text without changing the slide.
    /// </summary>
    /// <param name="tableIndex">The table index value to use.</param>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetTableCellTextInTable(PptxSlide target, int tableIndex, int rowIndex, int columnIndex);
    /// <summary>
    /// Returns the placeholder text without changing the slide.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetPlaceholderText(PptxSlide target, string type);
    /// <summary>
    /// Returns the placeholder text without changing the slide.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetPlaceholderTextByIndex(PptxSlide target, string type, int placeholderIndex);
}
