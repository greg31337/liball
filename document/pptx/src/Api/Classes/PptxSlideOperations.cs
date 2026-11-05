using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSlide values.
/// </summary>
public class PptxSlideOperations : IPptxSlideOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSlideOperations()
    {
    }

    /// <summary>
    /// Returns the notes without changing the slide.
    /// </summary>
    /// <returns>The notes slide, or <c>null</c> when the slide has no notes.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxNotesSlide? GetNotes(PptxSlide target)
    {
        return target.GetNotes();
    }

    /// <summary>
    /// Returns the slide's notes model, creating the notes part and relationship when they do not already exist.
    /// </summary>
    /// <returns>The resulting notes slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxNotesSlide GetOrCreateNotes(PptxSlide target)
    {
        return target.GetOrCreateNotes();
    }

    /// <summary>
    /// Returns the notes text without changing the slide.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetNotesText(PptxSlide target)
    {
        return target.GetNotesText();
    }

    /// <summary>
    /// Updates the notes text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetNotesText(PptxSlide target, string text)
    {
        target.SetNotesText(text);
    }
    /// <summary>
    /// Removes the notes part related to the slide.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <returns><c>true</c> when a notes part was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveNotes(PptxSlide target)
    {
        if (target.Presentation == null)
        {
            return false;
        }

        return target.Presentation.RemoveNotesSlide(target);
    }

    /// <summary>
    /// Returns the comments without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxComment> GetComments(PptxSlide target)
    {
        return target.GetComments();
    }

    /// <summary>
    /// Adds the comment and returns the created model when applicable.
    /// </summary>
    /// <param name="authorId">The author id value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting comment value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxComment AddComment(PptxSlide target, int authorId, string text)
    {
        return target.AddComment(authorId, text);
    }

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
    public PptxComment AddCommentWithDetails(PptxSlide target, int authorId, string text, string dateTime, long x, long y)
    {
        return target.AddCommentWithDetails(authorId, text, dateTime, x, y);
    }

    /// <summary>
    /// Returns the modern comments without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxModernComment> GetModernComments(PptxSlide target)
    {
        return target.GetModernComments();
    }

    /// <summary>
    /// Reports whether the slide contains or enables modern comments.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasModernComments(PptxSlide target)
    {
        return target.HasModernComments();
    }

    /// <summary>
    /// Returns the tags without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxTag> GetTags(PptxSlide target)
    {
        return target.GetTags();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int TagCount(PptxSlide target)
    {
        return target.TagCount();
    }

    /// <summary>
    /// Returns the tag value without changing the slide.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetTagValue(PptxSlide target, string name)
    {
        return target.GetTagValue(name);
    }

    /// <summary>
    /// Updates the tag and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTag(PptxSlide target, string name, string value)
    {
        target.SetTag(name, value);
    }

    /// <summary>
    /// Removes the tag and updates dependent presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveTag(PptxSlide target, string name)
    {
        target.RemoveTag(name);
    }
    /// <summary>
    /// Removes all tags while preserving other content in the tag part.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    public void ClearTags(PptxSlide target)
    {
        target.ClearTags();
    }
    /// <summary>
    /// Removes the slide tag part, relationship, and relationship marker.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <returns><c>true</c> when a tag relationship was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveTagPart(PptxSlide target)
    {
        return target.RemoveTagPart();
    }

    /// <summary>
    /// Reports whether the slide contains or enables timing.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasTiming(PptxSlide target)
    {
        return target.HasTiming();
    }

    /// <summary>
    /// Returns the animations without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxAnimationEffect> GetAnimations(PptxSlide target)
    {
        return target.GetAnimations();
    }
    /// <summary>
    /// Returns all shape- and run-level hyperlinks on the slide.
    /// </summary>
    /// <returns>The hyperlinks in shape and text order.</returns>
    /// <param name="target">The slide to inspect.</param>
    public List<PptxHyperlink> GetHyperlinks(PptxSlide target)
    {
        return target.GetHyperlinks();
    }
    /// <summary>
    /// Audits every shape- and run-level hyperlink on the slide.
    /// </summary>
    /// <returns>Aggregated hyperlink safety diagnostics.</returns>
    /// <param name="target">The slide to audit.</param>
    public PptxResult AuditHyperlinks(PptxSlide target)
    {
        return target.AuditHyperlinks();
    }

    /// <summary>
    /// Returns the transition without changing the slide.
    /// </summary>
    /// <returns>The resulting slide transition value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlideTransition GetTransition(PptxSlide target)
    {
        return target.GetTransition();
    }

    /// <summary>
    /// Reports whether the slide contains or enables transition.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasTransition(PptxSlide target)
    {
        return target.HasTransition();
    }

    /// <summary>
    /// Updates the transition and the underlying presentation data.
    /// </summary>
    /// <param name="transitionType">The transition type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTransition(PptxSlide target, string transitionType)
    {
        target.SetTransition(transitionType);
    }

    /// <summary>
    /// Updates the transition and the underlying presentation data.
    /// </summary>
    /// <param name="transitionType">The transition type value to use.</param>
    /// <param name="speed">The speed value to use.</param>
    /// <param name="advanceOnClick">The advance on click value to use.</param>
    /// <param name="advanceAfterTime">The advance after time value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTransitionOptions(PptxSlide target, string transitionType, string speed, bool advanceOnClick, string advanceAfterTime)
    {
        target.SetTransitionOptions(transitionType, speed, advanceOnClick, advanceAfterTime);
    }

    /// <summary>
    /// Removes the transition and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveTransition(PptxSlide target)
    {
        target.RemoveTransition();
    }

    /// <summary>
    /// Adds the animation and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeId">The shape id value to use.</param>
    /// <param name="type">The type value to use.</param>
    /// <param name="effect">The effect value to use.</param>
    /// <returns>The resulting animation effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxAnimationEffect AddAnimation(PptxSlide target, int shapeId, string type, string effect)
    {
        return target.AddAnimation(shapeId, type, effect);
    }

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
    public PptxAnimationEffect AddAnimationWithTiming(PptxSlide target, int shapeId, string type, string effect, string trigger, string duration, string delay, string repeatCount)
    {
        return target.AddAnimationWithTiming(shapeId, type, effect, trigger, duration, delay, repeatCount);
    }
    /// <summary>
    /// Adds a common entrance animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="effect">The entrance filter name, such as <c>fade</c>.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    public PptxAnimationEffect AddEntranceAnimation(PptxSlide target, int shapeId, string effect)
    {
        return target.AddEntranceAnimation(shapeId, effect);
    }
    /// <summary>
    /// Adds a common emphasis animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="effect">The emphasis filter name.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    public PptxAnimationEffect AddEmphasisAnimation(PptxSlide target, int shapeId, string effect)
    {
        return target.AddEmphasisAnimation(shapeId, effect);
    }
    /// <summary>
    /// Adds a common exit animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="effect">The exit filter name, such as <c>fade</c>.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    public PptxAnimationEffect AddExitAnimation(PptxSlide target, int shapeId, string effect)
    {
        return target.AddExitAnimation(shapeId, effect);
    }
    /// <summary>
    /// Adds a motion-path animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target shape identifier.</param>
    /// <param name="path">The PresentationML motion path.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    public PptxAnimationEffect AddMotionPathAnimation(PptxSlide target, int shapeId, string path)
    {
        return target.AddMotionPathAnimation(shapeId, path);
    }
    /// <summary>
    /// Adds a media command animation with click-triggered default timing.
    /// </summary>
    /// <param name="shapeId">The target media shape identifier.</param>
    /// <param name="command">The media command, such as <c>playFrom(0.0)</c>.</param>
    /// <returns>The created animation effect.</returns>
    /// <param name="target">The slide to update.</param>
    public PptxAnimationEffect AddMediaAnimation(PptxSlide target, int shapeId, string command)
    {
        return target.AddMediaAnimation(shapeId, command);
    }

    /// <summary>
    /// Removes the animation and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveAnimation(PptxSlide target, int index)
    {
        target.RemoveAnimation(index);
    }

    /// <summary>
    /// Moves animation while preserving its related data.
    /// </summary>
    /// <param name="oldIndex">The old index value to use.</param>
    /// <param name="newIndex">The new index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MoveAnimation(PptxSlide target, int oldIndex, int newIndex)
    {
        target.MoveAnimation(oldIndex, newIndex);
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int PlaceholderCount(PptxSlide target)
    {
        return target.PlaceholderCount();
    }

    /// <summary>
    /// Finds the placeholder; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <returns>The matching placeholder, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPlaceholder? FindPlaceholder(PptxSlide target, string type)
    {
        return target.FindPlaceholder(type);
    }

    /// <summary>
    /// Finds the placeholder; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <returns>The matching placeholder, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPlaceholder? FindPlaceholderByIndex(PptxSlide target, string type, int placeholderIndex)
    {
        return target.FindPlaceholderByIndex(type, placeholderIndex);
    }

    /// <summary>
    /// Reports whether the slide contains or enables placeholder.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasPlaceholder(PptxSlide target, string type)
    {
        return target.HasPlaceholder(type);
    }

    /// <summary>
    /// Returns the shape tree without changing the slide.
    /// </summary>
    /// <returns>The resulting shape tree value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShapeTree GetShapeTree(PptxSlide target)
    {
        return target.GetShapeTree();
    }

    /// <summary>
    /// Adds the shape and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeType">The shape type value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddShape(PptxSlide target, string shapeType, PptxBounds bounds)
    {
        return target.AddShape(shapeType, bounds);
    }

    /// <summary>
    /// Adds the auto shape and returns the created model when applicable.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddAutoShape(PptxSlide target, string presetGeometry, PptxBounds bounds)
    {
        return target.AddAutoShape(presetGeometry, bounds);
    }

    /// <summary>
    /// Adds the text box and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddTextBox(PptxSlide target, string text, PptxBounds bounds)
    {
        return target.AddTextBox(text, bounds);
    }

    /// <summary>
    /// Adds the connector and returns the created model when applicable.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <param name="start">The start value to use.</param>
    /// <param name="end">The end value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddConnector(PptxSlide target, string connectorType, PptxPoint start, PptxPoint end)
    {
        return target.AddConnector(connectorType, start, end);
    }

    /// <summary>
    /// Adds the freeform and returns the created model when applicable.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="points">The points value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddFreeform(PptxSlide target, PptxBounds bounds, List<PptxPoint> points)
    {
        return target.AddFreeform(bounds, points);
    }

    /// <summary>
    /// Adds the image and returns the created model when applicable.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddImage(PptxSlide target, byte[] bytes, PptxBounds bounds)
    {
        return target.AddImage(bytes, bounds);
    }

    /// <summary>
    /// Adds the table and returns the created model when applicable.
    /// </summary>
    /// <param name="rowCount">The number of rows. The value must be positive where a new table or range is created.</param>
    /// <param name="columnCount">The number of columns. The value must be positive where a new table or range is created.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting table value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTable AddTable(PptxSlide target, int rowCount, int columnCount, PptxBounds bounds)
    {
        return target.AddTable(rowCount, columnCount, bounds);
    }

    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChart AddChart(PptxSlide target, string chartType, PptxChartData data, PptxBounds bounds)
    {
        return target.AddChart(chartType, data, bounds);
    }

    /// <summary>
    /// Returns the tables without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxTable> GetTables(PptxSlide target)
    {
        return target.GetTables();
    }

    /// <summary>
    /// Returns the table without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting table value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTable GetTable(PptxSlide target, int index)
    {
        return target.GetTable(index);
    }

    /// <summary>
    /// Returns the charts without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxChart> GetCharts(PptxSlide target)
    {
        return target.GetCharts();
    }

    /// <summary>
    /// Returns the chart without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChart GetChart(PptxSlide target, int index)
    {
        return target.GetChart(index);
    }

    /// <summary>
    /// Extracts chart data tables from the slide without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<List<List<string>>> ExtractChartDataTables(PptxSlide target)
    {
        return target.ExtractChartDataTables();
    }

    /// <summary>
    /// Returns the diagrams without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxDiagram> GetDiagrams(PptxSlide target)
    {
        return target.GetDiagrams();
    }

    /// <summary>
    /// Returns the diagram without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting diagram value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxDiagram GetDiagram(PptxSlide target, int index)
    {
        return target.GetDiagram(index);
    }

    /// <summary>
    /// Returns the embedded objects without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxEmbeddedObject> GetEmbeddedObjects(PptxSlide target)
    {
        return target.GetEmbeddedObjects();
    }

    /// <summary>
    /// Returns the embedded object without changing the slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting embedded object value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxEmbeddedObject GetEmbeddedObject(PptxSlide target, int index)
    {
        return target.GetEmbeddedObject(index);
    }

    /// <summary>
    /// Returns the ActiveX controls on the slide without activating them.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <returns>The ActiveX control inventory.</returns>
    public List<PptxActiveXControl> GetActiveXControls(PptxSlide target)
    {
        return target.GetActiveXControls();
    }

    /// <summary>
    /// Returns one ActiveX control without activating it.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <param name="index">The zero-based control index.</param>
    /// <returns>The selected ActiveX control.</returns>
    public PptxActiveXControl GetActiveXControl(PptxSlide target, int index)
    {
        return target.GetActiveXControl(index);
    }

    /// <summary>
    /// Extracts images from the slide without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxImageFile> ExtractImages(PptxSlide target)
    {
        return target.ExtractImages();
    }

    /// <summary>
    /// Extracts images to directory from the slide without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxAssetExtractionResult ExtractImagesToDirectory(PptxSlide target, string directoryPath)
    {
        return target.ExtractImagesToDirectory(directoryPath);
    }

    /// <summary>
    /// Returns the media without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxMediaFile> GetMedia(PptxSlide target)
    {
        return target.GetMedia();
    }

    /// <summary>
    /// Extracts media to directory from the slide without removing it.
    /// </summary>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxAssetExtractionResult ExtractMediaToDirectory(PptxSlide target, string directoryPath)
    {
        return target.ExtractMediaToDirectory(directoryPath);
    }

    /// <summary>
    /// Extracts text from the slide without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractText(PptxSlide target)
    {
        return target.ExtractText();
    }

    /// <summary>
    /// Exports text in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportText(PptxSlide target)
    {
        return target.ExportText();
    }

    /// <summary>
    /// Exports outline in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportOutline(PptxSlide target)
    {
        return target.ExportOutline();
    }

    /// <summary>
    /// Exports html in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportHtml(PptxSlide target)
    {
        return target.ExportHtml();
    }

    /// <summary>
    /// Exports svg in a structural, non-rendered representation.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExportSvg(PptxSlide target)
    {
        return target.ExportSvg();
    }

    /// <summary>
    /// Returns the outline entries without changing the slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxOutlineEntry> GetOutlineEntries(PptxSlide target)
    {
        return target.GetOutlineEntries();
    }

    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ReplaceText(PptxSlide target, string search, string replacement)
    {
        return target.ReplaceText(search, replacement);
    }

    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int FillTemplate(PptxSlide target, PptxTemplateData data)
    {
        return target.FillTemplate(data);
    }

    /// <summary>
    /// Fills template with the supplied values.
    /// </summary>
    /// <param name="values">The values value to use.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int FillTemplateFromDictionary(PptxSlide target, Dictionary<string, string> values)
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
    public int FillTemplateFromLists(PptxSlide target, List<string> keys, List<string> values)
    {
        return target.FillTemplateFromLists(keys, values);
    }

    /// <summary>
    /// Returns the background fill without changing the slide.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxFill GetBackgroundFill(PptxSlide target)
    {
        return target.GetBackgroundFill();
    }

    /// <summary>
    /// Updates the background and the underlying presentation data.
    /// </summary>
    /// <param name="fill">The fill value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBackground(PptxSlide target, PptxFill fill)
    {
        target.SetBackground(fill);
    }

    /// <summary>
    /// Applies a slide layout and updates the slide relationship.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="layout">The layout to apply.</param>
    public void ApplyLayout(PptxSlide target, PptxSlideLayout layout)
    {
        target.ApplyLayout(layout);
    }

    /// <summary>
    /// Inspects matching placeholder declarations on the slide, layout, and master.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <param name="placeholderType">The placeholder type.</param>
    /// <param name="placeholderIndex">The placeholder index, or <c>-1</c> for an unindexed placeholder.</param>
    /// <returns>The matching placeholder inheritance chain.</returns>
    public PptxPlaceholderInheritance InspectPlaceholderInheritance(PptxSlide target, string placeholderType, int placeholderIndex)
    {
        return target.InspectPlaceholderInheritance(placeholderType, placeholderIndex);
    }

    /// <summary>
    /// Resolves the slide background through its layout and master.
    /// </summary>
    /// <param name="target">The slide to inspect.</param>
    /// <returns>The effective background and its source.</returns>
    public PptxInheritedBackground GetEffectiveBackground(PptxSlide target)
    {
        return target.GetEffectiveBackground();
    }

    /// <summary>
    /// Finds the shape by id; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape? FindShapeById(PptxSlide target, int id)
    {
        return target.FindShapeById(id);
    }

    /// <summary>
    /// Finds the shape by name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape? FindShapeByName(PptxSlide target, string name)
    {
        return target.FindShapeByName(name);
    }

    /// <summary>
    /// Fills title with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulateTitle(PptxSlide target, string text)
    {
        target.PopulateTitle(text);
    }

    /// <summary>
    /// Fills body with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulateBody(PptxSlide target, string text)
    {
        target.PopulateBody(text);
    }

    /// <summary>
    /// Fills picture with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulatePicture(PptxSlide target, string text)
    {
        target.PopulatePicture(text);
    }

    /// <summary>
    /// Fills table with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulateTable(PptxSlide target, string text)
    {
        target.PopulateTable(text);
    }

    /// <summary>
    /// Fills chart with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulateChart(PptxSlide target, string text)
    {
        target.PopulateChart(text);
    }

    /// <summary>
    /// Fills media with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulateMedia(PptxSlide target, string text)
    {
        target.PopulateMedia(text);
    }

    /// <summary>
    /// Fills content with the supplied values.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulateContent(PptxSlide target, string text)
    {
        target.PopulateContent(text);
    }

    /// <summary>
    /// Fills placeholder with the supplied values.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulatePlaceholder(PptxSlide target, string type, string text)
    {
        target.PopulatePlaceholder(type, text);
    }

    /// <summary>
    /// Updates the placeholder text and the underlying presentation data.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetPlaceholderText(PptxSlide target, string type, string text)
    {
        target.SetPlaceholderText(type, text);
    }

    /// <summary>
    /// Updates the placeholder text and the underlying presentation data.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetPlaceholderTextByIndex(PptxSlide target, string type, int placeholderIndex, string text)
    {
        target.SetPlaceholderTextByIndex(type, placeholderIndex, text);
    }

    /// <summary>
    /// Fills placeholder with the supplied values.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void PopulatePlaceholderByIndex(PptxSlide target, string type, int placeholderIndex, string text)
    {
        target.PopulatePlaceholderByIndex(type, placeholderIndex, text);
    }
    /// <summary>
    /// Replaces a picture or generic content placeholder with an image while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>picture</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="bytes">The image bytes.</param>
    /// <returns>The populated picture shape.</returns>
    public PptxShape PopulatePlaceholderImage(PptxSlide target, string type, int placeholderIndex, byte[] bytes)
    {
        return target.PopulatePlaceholderImage(type, placeholderIndex, bytes);
    }
    /// <summary>
    /// Replaces a table or generic content placeholder with a table while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>table</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="rowCount">The number of rows.</param>
    /// <param name="columnCount">The number of columns.</param>
    /// <returns>The populated table.</returns>
    public PptxTable PopulatePlaceholderTable(PptxSlide target, string type, int placeholderIndex, int rowCount, int columnCount)
    {
        return target.PopulatePlaceholderTable(type, placeholderIndex, rowCount, columnCount);
    }
    /// <summary>
    /// Replaces a chart or generic content placeholder with a chart while preserving placeholder identity.
    /// </summary>
    /// <param name="target">The slide to update.</param>
    /// <param name="type">The placeholder type, including <c>chart</c> or <c>content</c>.</param>
    /// <param name="placeholderIndex">The placeholder index.</param>
    /// <param name="chartType">The chart type.</param>
    /// <param name="data">The chart data.</param>
    /// <returns>The populated chart.</returns>
    public PptxChart PopulatePlaceholderChart(PptxSlide target, string type, int placeholderIndex, string chartType, PptxChartData data)
    {
        return target.PopulatePlaceholderChart(type, placeholderIndex, chartType, data);
    }
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
    public PptxMediaFile PopulatePlaceholderMedia(PptxSlide target, string type, int placeholderIndex, byte[] bytes, string contentType, string mediaType)
    {
        return target.PopulatePlaceholderMedia(type, placeholderIndex, bytes, contentType, mediaType);
    }
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
    public PptxEmbeddedObject PopulatePlaceholderObject(PptxSlide target, string type, int placeholderIndex, byte[] bytes, string contentType, string programId, string objectName)
    {
        return target.PopulatePlaceholderObject(type, placeholderIndex, bytes, contentType, programId, objectName);
    }

    /// <summary>
    /// Updates the table cell text and the underlying presentation data.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTableCellText(PptxSlide target, int rowIndex, int columnIndex, string text)
    {
        target.SetTableCellText(rowIndex, columnIndex, text);
    }

    /// <summary>
    /// Updates the table cell text and the underlying presentation data.
    /// </summary>
    /// <param name="tableIndex">The table index value to use.</param>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTableCellTextInTable(PptxSlide target, int tableIndex, int rowIndex, int columnIndex, string text)
    {
        target.SetTableCellTextInTable(tableIndex, rowIndex, columnIndex, text);
    }

    /// <summary>
    /// Returns the table cell text without changing the slide.
    /// </summary>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetTableCellText(PptxSlide target, int rowIndex, int columnIndex)
    {
        return target.GetTableCellText(rowIndex, columnIndex);
    }

    /// <summary>
    /// Returns the table cell text without changing the slide.
    /// </summary>
    /// <param name="tableIndex">The table index value to use.</param>
    /// <param name="rowIndex">The zero-based table row index.</param>
    /// <param name="columnIndex">The zero-based table column index.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetTableCellTextInTable(PptxSlide target, int tableIndex, int rowIndex, int columnIndex)
    {
        return target.GetTableCellTextInTable(tableIndex, rowIndex, columnIndex);
    }

    /// <summary>
    /// Returns the placeholder text without changing the slide.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetPlaceholderText(PptxSlide target, string type)
    {
        return target.GetPlaceholderText(type);
    }

    /// <summary>
    /// Returns the placeholder text without changing the slide.
    /// </summary>
    /// <param name="type">The type value to use.</param>
    /// <param name="placeholderIndex">The placeholder index value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetPlaceholderTextByIndex(PptxSlide target, string type, int placeholderIndex)
    {
        return target.GetPlaceholderTextByIndex(type, placeholderIndex);
    }
}
