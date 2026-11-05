using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxShapeOperations"/>.
/// </summary>
public interface IPptxShapeOperations
{
    /// <summary>
    /// Returns the transform without changing the shape.
    /// </summary>
    /// <returns>The resulting transform value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTransform GetTransform(PptxShape target);
    /// <summary>
    /// Updates the transform and the underlying presentation data.
    /// </summary>
    /// <param name="transform">The transform value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTransform(PptxShape target, PptxTransform transform);
    /// <summary>
    /// Returns the bounds without changing the shape.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds GetBounds(PptxShape target);
    /// <summary>
    /// Returns the axis-aligned bounds occupied by the shape after its own rotation.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds GetAnchorBounds(PptxShape target);
    /// <summary>
    /// Returns the effective bounds without changing the shape.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds GetEffectiveBounds(PptxShape target);
    /// <summary>
    /// Returns axis-aligned anchor bounds after shape and ancestor-group transforms.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds GetEffectiveAnchorBounds(PptxShape target);
    /// <summary>
    /// Returns the effective transform without changing the shape.
    /// </summary>
    /// <returns>The resulting transform value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTransform GetEffectiveTransform(PptxShape target);
    /// <summary>
    /// Returns the clockwise shape rotation in degrees.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    double GetRotationDegrees(PptxShape target);
    /// <summary>
    /// Updates the clockwise shape rotation in degrees.
    /// </summary>
    /// <param name="degrees">The degrees value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetRotationDegrees(PptxShape target, double degrees);
    /// <summary>
    /// Updates the horizontal and vertical shape flips.
    /// </summary>
    /// <param name="horizontal"><c>true</c> to flip horizontally.</param>
    /// <param name="vertical"><c>true</c> to flip vertically.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFlips(PptxShape target, bool horizontal, bool vertical);
    /// <summary>
    /// Reports whether the shape is auto shape.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsAutoShape(PptxShape target);
    /// <summary>
    /// Reports whether the shape is custom geometry.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsCustomGeometry(PptxShape target);
    /// <summary>
    /// Reports whether the shape is freeform.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsFreeform(PptxShape target);
    /// <summary>
    /// Returns the preset geometry without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetPresetGeometry(PptxShape target);
    /// <summary>
    /// Returns shape-level custom data names in XML order.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<string> GetCustomDataNames(PptxShape target);
    /// <summary>
    /// Returns a shape-level custom data value, or an empty value when absent.
    /// </summary>
    /// <param name="name">The custom data name.</param>
    /// <returns>The stored custom data value.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetCustomData(PptxShape target, string name);
    /// <summary>
    /// Adds or updates shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="name">The custom data name.</param>
    /// <param name="value">The custom data value.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetCustomData(PptxShape target, string name, string value);
    /// <summary>
    /// Removes shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="name">The custom data name.</param>
    /// <returns><c>true</c> when an entry was removed; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool RemoveCustomData(PptxShape target, string name);
    /// <summary>
    /// Returns preserved non-visual shape extension XML in document order.
    /// </summary>
    /// <returns>The preserved extension XML values.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<string> GetExtensionXml(PptxShape target);
    /// <summary>
    /// Reports whether the shape is marked as a background shape.
    /// </summary>
    /// <returns><c>true</c> when the shape is a background shape; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsBackgroundShape(PptxShape target);
    /// <summary>
    /// Updates the background-shape marker.
    /// </summary>
    /// <param name="background"><c>true</c> to mark the shape as background.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBackgroundShape(PptxShape target, bool background);
    /// <summary>
    /// Returns constant adjustment values from the preset geometry.
    /// </summary>
    /// <returns>The adjustment values in XML order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxGeometryAdjustment> GetPresetAdjustments(PptxShape target);
    /// <summary>
    /// Adds or updates a named constant preset adjustment.
    /// </summary>
    /// <param name="name">The DrawingML adjustment name.</param>
    /// <param name="value">The constant adjustment value.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetPresetAdjustment(PptxShape target, string name, int value);
    /// <summary>
    /// Removes a named preset adjustment.
    /// </summary>
    /// <param name="name">The DrawingML adjustment name.</param>
    /// <returns><c>true</c> when an adjustment was removed; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool RemovePresetAdjustment(PptxShape target, string name);
    /// <summary>
    /// Returns the fill without changing the shape.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxFill GetFill(PptxShape target);
    /// <summary>
    /// Returns the line without changing the shape.
    /// </summary>
    /// <returns>The resulting line value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxLine GetLine(PptxShape target);
    /// <summary>
    /// Returns the effect without changing the shape.
    /// </summary>
    /// <returns>The resulting effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxEffect GetEffect(PptxShape target);
    /// <summary>
    /// Returns the text frame without changing the shape.
    /// </summary>
    /// <returns>The resulting text frame value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTextFrame GetTextFrame(PptxShape target);
    /// <summary>
    /// Returns the text without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetText(PptxShape target);
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxShape target, string text);
    /// <summary>
    /// Adds the text paragraph and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting paragraph value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxParagraph AddTextParagraph(PptxShape target, string text);
    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ReplaceText(PptxShape target, string search, string replacement);
    /// <summary>
    /// Reports whether the shape is picture.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsPicture(PptxShape target);
    /// <summary>
    /// Reports whether the shape is table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsTable(PptxShape target);
    /// <summary>
    /// Returns the table view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting table value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTable AsTable(PptxShape target);
    /// <summary>
    /// Reports whether the shape is chart.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsChart(PptxShape target);
    /// <summary>
    /// Returns the chart relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetChartRelationshipId(PptxShape target);
    /// <summary>
    /// Returns the chart view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChart AsChart(PptxShape target);
    /// <summary>
    /// Reports whether the shape is diagram.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsDiagram(PptxShape target);
    /// <summary>
    /// Returns the diagram relationship id without changing the shape.
    /// </summary>
    /// <param name="relationshipName">The relationship name value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetDiagramRelationshipId(PptxShape target, string relationshipName);
    /// <summary>
    /// Returns the diagram view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting diagram value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxDiagram AsDiagram(PptxShape target);
    /// <summary>
    /// Reports whether the shape is embedded object.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsEmbeddedObject(PptxShape target);
    /// <summary>
    /// Returns the embedded object relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetEmbeddedObjectRelationshipId(PptxShape target);
    /// <summary>
    /// Returns the embedded object program id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetEmbeddedObjectProgramId(PptxShape target);
    /// <summary>
    /// Returns the embedded object name without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetEmbeddedObjectName(PptxShape target);
    /// <summary>
    /// Returns the embedded object show as icon without changing the shape.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetEmbeddedObjectShowAsIcon(PptxShape target);
    /// <summary>
    /// Returns the embedded object view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting embedded object value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxEmbeddedObject AsEmbeddedObject(PptxShape target);
    /// <summary>
    /// Returns the image relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetImageRelationshipId(PptxShape target);
    /// <summary>
    /// Extracts image from the shape without removing it.
    /// </summary>
    /// <returns>The resulting image file value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxImageFile ExtractImage(PptxShape target);
    /// <summary>
    /// Replaces image while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void ReplaceImage(PptxShape target, byte[] bytes);
    /// <summary>
    /// Reports whether the shape is media shape.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsMediaShape(PptxShape target);
    /// <summary>
    /// Returns the media relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetMediaRelationshipId(PptxShape target);
    /// <summary>
    /// Reports whether the shape contains or enables poster frame.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasPosterFrame(PptxShape target);
    /// <summary>
    /// Extracts poster frame from the shape without removing it.
    /// </summary>
    /// <returns>The resulting image file value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxImageFile ExtractPosterFrame(PptxShape target);
    /// <summary>
    /// Replaces poster frame while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void ReplacePosterFrame(PptxShape target, byte[] bytes);
    /// <summary>
    /// Updates the image crop and the underlying presentation data.
    /// </summary>
    /// <param name="left">The left value to use.</param>
    /// <param name="top">The top value to use.</param>
    /// <param name="right">The right value to use.</param>
    /// <param name="bottom">The bottom value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetImageCrop(PptxShape target, int left, int top, int right, int bottom);
    /// <summary>
    /// Updates the picture crop from a typed percentage rectangle.
    /// </summary>
    /// <param name="crop">The crop rectangle value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetImageCropRectangle(PptxShape target, PptxImageCrop crop);
    /// <summary>
    /// Returns the image crop without changing the shape.
    /// </summary>
    /// <returns>The resulting image crop value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxImageCrop GetImageCrop(PptxShape target);
    /// <summary>
    /// Updates the image stretch and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void SetImageStretch(PptxShape target);
    /// <summary>
    /// Updates the image tile and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void SetImageTile(PptxShape target);
    /// <summary>
    /// Reports whether the shape is image tiled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsImageTiled(PptxShape target);
    /// <summary>
    /// Reports whether the shape is image stretched.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsImageStretched(PptxShape target);
    /// <summary>
    /// Reports whether the shape is text box.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsTextBox(PptxShape target);
    /// <summary>
    /// Updates the text box and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTextBox(PptxShape target, bool enabled);
    /// <summary>
    /// Updates the preset geometry and the underlying presentation data.
    /// </summary>
    /// <param name="geometry">The geometry value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetPresetGeometry(PptxShape target, string geometry);
    /// <summary>
    /// Updates the freeform geometry and the underlying presentation data.
    /// </summary>
    /// <param name="points">The points value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFreeformGeometry(PptxShape target, List<PptxPoint> points);
    /// <summary>
    /// Returns the supported commands from every custom-geometry path.
    /// </summary>
    /// <returns>The paths in XML order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxFreeformPath> GetFreeformPaths(PptxShape target);
    /// <summary>
    /// Replaces custom geometry with the supplied paths and commands.
    /// </summary>
    /// <param name="paths">The freeform paths to write.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFreeformPaths(PptxShape target, List<PptxFreeformPath> paths);
    /// <summary>
    /// Updates the fill and the underlying presentation data.
    /// </summary>
    /// <param name="fill">The fill value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFill(PptxShape target, PptxFill fill);
    /// <summary>
    /// Updates the line and the underlying presentation data.
    /// </summary>
    /// <param name="line">The line value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLine(PptxShape target, PptxLine line);
    /// <summary>
    /// Updates the effect and the underlying presentation data.
    /// </summary>
    /// <param name="effect">The effect value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetEffect(PptxShape target, PptxEffect effect);
    /// <summary>
    /// Updates the bounds and the underlying presentation data.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBounds(PptxShape target, PptxBounds bounds);
    /// <summary>
    /// Reports whether the shape is group.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsGroup(PptxShape target);
    /// <summary>
    /// Returns the group view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting group shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxGroupShape AsGroup(PptxShape target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ChildCount(PptxShape target);
    /// <summary>
    /// Returns the child without changing the shape.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape GetChild(PptxShape target, int index);
    /// <summary>
    /// Finds the descendant by id; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindDescendantById(PptxShape target, int id);
    /// <summary>
    /// Finds the descendant by name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindDescendantByName(PptxShape target, string name);
    /// <summary>
    /// Reports whether the shape is connector.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsConnector(PptxShape target);
    /// <summary>
    /// Returns the connector type without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetConnectorType(PptxShape target);
    /// <summary>
    /// Updates the connector type and the underlying presentation data.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetConnectorType(PptxShape target, string connectorType);
    /// <summary>
    /// Updates the connector ends and the underlying presentation data.
    /// </summary>
    /// <param name="start">The start value to use.</param>
    /// <param name="end">The end value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetConnectorEnds(PptxShape target, PptxPoint start, PptxPoint end);
    /// <summary>
    /// Updates the connector ends and the underlying presentation data.
    /// </summary>
    /// <param name="startX">The start x value to use.</param>
    /// <param name="startY">The start y value to use.</param>
    /// <param name="endX">The end x value to use.</param>
    /// <param name="endY">The end y value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetConnectorEndCoordinates(PptxShape target, long startX, long startY, long endX, long endY);
    /// <summary>
    /// Returns the connector start position and optional shape attachment.
    /// </summary>
    /// <returns>The resulting connection point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxConnectionPoint GetConnectorStartConnection(PptxShape target);
    /// <summary>
    /// Returns the connector end position and optional shape attachment.
    /// </summary>
    /// <returns>The resulting connection point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxConnectionPoint GetConnectorEndConnection(PptxShape target);
    /// <summary>
    /// Updates connector endpoint positions and optional shape attachments.
    /// </summary>
    /// <param name="start">The start connection point.</param>
    /// <param name="end">The end connection point.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetConnectorConnections(PptxShape target, PptxConnectionPoint start, PptxConnectionPoint end);
    /// <summary>
    /// Updates the name and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetName(PptxShape target, string name);
    /// <summary>
    /// Reports whether the shape is visible.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsVisible(PptxShape target);
    /// <summary>
    /// Updates the visible and the underlying presentation data.
    /// </summary>
    /// <param name="visible">The visible value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetVisible(PptxShape target, bool visible);
    /// <summary>
    /// Updates the hidden and the underlying presentation data.
    /// </summary>
    /// <param name="hidden">The hidden value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetHidden(PptxShape target, bool hidden);
    /// <summary>
    /// Updates the alternative text and the underlying presentation data.
    /// </summary>
    /// <param name="alternativeText">The alternative text value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetAlternativeText(PptxShape target, string alternativeText);
    /// <summary>
    /// Updates the title and the underlying presentation data.
    /// </summary>
    /// <param name="title">The title value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTitle(PptxShape target, string title);
    /// <summary>
    /// Updates the lock and the underlying presentation data.
    /// </summary>
    /// <param name="lockName">The lock name value to use.</param>
    /// <param name="locked">The locked value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLock(PptxShape target, string lockName, bool locked);
    /// <summary>
    /// Returns the lock without changing the shape.
    /// </summary>
    /// <param name="lockName">The lock name value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool GetLock(PptxShape target, string lockName);
    /// <summary>
    /// Updates the locks and the underlying presentation data.
    /// </summary>
    /// <param name="noMove">The no move value to use.</param>
    /// <param name="noResize">The no resize value to use.</param>
    /// <param name="noRotate">The no rotate value to use.</param>
    /// <param name="noSelect">The no select value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLocks(PptxShape target, bool noMove, bool noResize, bool noRotate, bool noSelect);
    /// <summary>
    /// Updates the hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="url">The url value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetHyperlink(PptxShape target, string url);
    /// <summary>
    /// Returns the hyperlink without changing the shape.
    /// </summary>
    /// <returns>The resulting hyperlink value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxHyperlink GetHyperlink(PptxShape target);
    /// <summary>
    /// Returns the shape-level hyperlink followed by all run-level hyperlinks in text order.
    /// </summary>
    /// <returns>The hyperlinks owned by the shape and its text runs.</returns>
    /// <param name="target">The shape to inspect.</param>
    List<PptxHyperlink> GetHyperlinks(PptxShape target);
    /// <summary>
    /// Updates the external hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="url">The url value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetExternalHyperlink(PptxShape target, string url);
    /// <summary>
    /// Updates the file hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="fileUri">The file uri value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFileHyperlink(PptxShape target, string fileUri);
    /// <summary>
    /// Updates the slide hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSlideHyperlink(PptxShape target, int slideIndex);
    /// <summary>
    /// Updates the next slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void SetNextSlideAction(PptxShape target);
    /// <summary>
    /// Updates the previous slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void SetPreviousSlideAction(PptxShape target);
    /// <summary>
    /// Updates the first slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void SetFirstSlideAction(PptxShape target);
    /// <summary>
    /// Updates the last slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void SetLastSlideAction(PptxShape target);
    /// <summary>
    /// Updates the custom show action and the underlying presentation data.
    /// </summary>
    /// <param name="customShowName">The custom show name value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetCustomShowAction(PptxShape target, string customShowName);
    /// <summary>
    /// Updates the action and the underlying presentation data.
    /// </summary>
    /// <param name="action">The action value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetAction(PptxShape target, string action);
    /// <summary>
    /// Removes the hyperlink and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void RemoveHyperlink(PptxShape target);
    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Remove(PptxShape target);
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original shape.
    /// </summary>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape Clone(PptxShape target);
}
