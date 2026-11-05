using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxShape values.
/// </summary>
public class PptxShapeOperations : IPptxShapeOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxShapeOperations()
    {
    }

    /// <summary>
    /// Returns the transform without changing the shape.
    /// </summary>
    /// <returns>The resulting transform value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTransform GetTransform(PptxShape target)
    {
        return target.GetTransform();
    }

    /// <summary>
    /// Updates the transform and the underlying presentation data.
    /// </summary>
    /// <param name="transform">The transform value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTransform(PptxShape target, PptxTransform transform)
    {
        target.SetTransform(transform);
    }

    /// <summary>
    /// Returns the bounds without changing the shape.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds GetBounds(PptxShape target)
    {
        return target.GetBounds();
    }

    /// <summary>
    /// Returns the axis-aligned bounds occupied by the shape after its own rotation.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds GetAnchorBounds(PptxShape target)
    {
        return target.GetAnchorBounds();
    }

    /// <summary>
    /// Returns the effective bounds without changing the shape.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds GetEffectiveBounds(PptxShape target)
    {
        return target.GetEffectiveBounds();
    }

    /// <summary>
    /// Returns axis-aligned anchor bounds after shape and ancestor-group transforms.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds GetEffectiveAnchorBounds(PptxShape target)
    {
        return target.GetEffectiveAnchorBounds();
    }

    /// <summary>
    /// Returns the effective transform without changing the shape.
    /// </summary>
    /// <returns>The resulting transform value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTransform GetEffectiveTransform(PptxShape target)
    {
        return target.GetEffectiveTransform();
    }

    /// <summary>
    /// Returns the clockwise shape rotation in degrees.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public double GetRotationDegrees(PptxShape target)
    {
        return target.GetRotationDegrees();
    }

    /// <summary>
    /// Updates the clockwise shape rotation in degrees.
    /// </summary>
    /// <param name="degrees">The degrees value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetRotationDegrees(PptxShape target, double degrees)
    {
        target.SetRotationDegrees(degrees);
    }

    /// <summary>
    /// Updates the horizontal and vertical shape flips.
    /// </summary>
    /// <param name="horizontal"><c>true</c> to flip horizontally.</param>
    /// <param name="vertical"><c>true</c> to flip vertically.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFlips(PptxShape target, bool horizontal, bool vertical)
    {
        target.SetFlips(horizontal, vertical);
    }

    /// <summary>
    /// Reports whether the shape is auto shape.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsAutoShape(PptxShape target)
    {
        return target.IsAutoShape();
    }

    /// <summary>
    /// Reports whether the shape is custom geometry.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsCustomGeometry(PptxShape target)
    {
        return target.IsCustomGeometry();
    }

    /// <summary>
    /// Reports whether the shape is freeform.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsFreeform(PptxShape target)
    {
        return target.IsFreeform();
    }

    /// <summary>
    /// Returns the preset geometry without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetPresetGeometry(PptxShape target)
    {
        return target.GetPresetGeometry();
    }

    /// <summary>
    /// Returns shape-level custom data names in XML order.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<string> GetCustomDataNames(PptxShape target)
    {
        return target.GetCustomDataNames();
    }

    /// <summary>
    /// Returns a shape-level custom data value, or an empty value when absent.
    /// </summary>
    /// <param name="name">The custom data name.</param>
    /// <returns>The stored custom data value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetCustomData(PptxShape target, string name)
    {
        return target.GetCustomData(name);
    }

    /// <summary>
    /// Adds or updates shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="name">The custom data name.</param>
    /// <param name="value">The custom data value.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetCustomData(PptxShape target, string name, string value)
    {
        target.SetCustomData(name, value);
    }

    /// <summary>
    /// Removes shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="name">The custom data name.</param>
    /// <returns><c>true</c> when an entry was removed; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool RemoveCustomData(PptxShape target, string name)
    {
        return target.RemoveCustomData(name);
    }

    /// <summary>
    /// Returns preserved non-visual shape extension XML in document order.
    /// </summary>
    /// <returns>The preserved extension XML values.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<string> GetExtensionXml(PptxShape target)
    {
        return target.GetExtensionXml();
    }

    /// <summary>
    /// Reports whether the shape is marked as a background shape.
    /// </summary>
    /// <returns><c>true</c> when the shape is a background shape; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsBackgroundShape(PptxShape target)
    {
        return target.IsBackgroundShape();
    }

    /// <summary>
    /// Updates the background-shape marker.
    /// </summary>
    /// <param name="background"><c>true</c> to mark the shape as background.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBackgroundShape(PptxShape target, bool background)
    {
        target.SetBackgroundShape(background);
    }

    /// <summary>
    /// Returns constant adjustment values from the preset geometry.
    /// </summary>
    /// <returns>The adjustment values in XML order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxGeometryAdjustment> GetPresetAdjustments(PptxShape target)
    {
        return target.GetPresetAdjustments();
    }

    /// <summary>
    /// Adds or updates a named constant preset adjustment.
    /// </summary>
    /// <param name="name">The DrawingML adjustment name.</param>
    /// <param name="value">The constant adjustment value.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetPresetAdjustment(PptxShape target, string name, int value)
    {
        target.SetPresetAdjustment(name, value);
    }

    /// <summary>
    /// Removes a named preset adjustment.
    /// </summary>
    /// <param name="name">The DrawingML adjustment name.</param>
    /// <returns><c>true</c> when an adjustment was removed; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool RemovePresetAdjustment(PptxShape target, string name)
    {
        return target.RemovePresetAdjustment(name);
    }

    /// <summary>
    /// Returns the fill without changing the shape.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxFill GetFill(PptxShape target)
    {
        return target.GetFill();
    }

    /// <summary>
    /// Returns the line without changing the shape.
    /// </summary>
    /// <returns>The resulting line value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxLine GetLine(PptxShape target)
    {
        return target.GetLine();
    }

    /// <summary>
    /// Returns the effect without changing the shape.
    /// </summary>
    /// <returns>The resulting effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxEffect GetEffect(PptxShape target)
    {
        return target.GetEffect();
    }

    /// <summary>
    /// Returns the text frame without changing the shape.
    /// </summary>
    /// <returns>The resulting text frame value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTextFrame GetTextFrame(PptxShape target)
    {
        return target.GetTextFrame();
    }

    /// <summary>
    /// Returns the text without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetText(PptxShape target)
    {
        return target.GetText();
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxShape target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Adds the text paragraph and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting paragraph value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxParagraph AddTextParagraph(PptxShape target, string text)
    {
        return target.AddTextParagraph(text);
    }

    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ReplaceText(PptxShape target, string search, string replacement)
    {
        return target.ReplaceText(search, replacement);
    }

    /// <summary>
    /// Reports whether the shape is picture.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsPicture(PptxShape target)
    {
        return target.IsPicture();
    }

    /// <summary>
    /// Reports whether the shape is table.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsTable(PptxShape target)
    {
        return target.IsTable();
    }

    /// <summary>
    /// Returns the table view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting table value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTable AsTable(PptxShape target)
    {
        return target.AsTable();
    }

    /// <summary>
    /// Reports whether the shape is chart.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsChart(PptxShape target)
    {
        return target.IsChart();
    }

    /// <summary>
    /// Returns the chart relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetChartRelationshipId(PptxShape target)
    {
        return target.GetChartRelationshipId();
    }

    /// <summary>
    /// Returns the chart view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting chart value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChart AsChart(PptxShape target)
    {
        return target.AsChart();
    }

    /// <summary>
    /// Reports whether the shape is diagram.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsDiagram(PptxShape target)
    {
        return target.IsDiagram();
    }

    /// <summary>
    /// Returns the diagram relationship id without changing the shape.
    /// </summary>
    /// <param name="relationshipName">The relationship name value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetDiagramRelationshipId(PptxShape target, string relationshipName)
    {
        return target.GetDiagramRelationshipId(relationshipName);
    }

    /// <summary>
    /// Returns the diagram view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting diagram value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxDiagram AsDiagram(PptxShape target)
    {
        return target.AsDiagram();
    }

    /// <summary>
    /// Reports whether the shape is embedded object.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsEmbeddedObject(PptxShape target)
    {
        return target.IsEmbeddedObject();
    }

    /// <summary>
    /// Returns the embedded object relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetEmbeddedObjectRelationshipId(PptxShape target)
    {
        return target.GetEmbeddedObjectRelationshipId();
    }

    /// <summary>
    /// Returns the embedded object program id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetEmbeddedObjectProgramId(PptxShape target)
    {
        return target.GetEmbeddedObjectProgramId();
    }

    /// <summary>
    /// Returns the embedded object name without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetEmbeddedObjectName(PptxShape target)
    {
        return target.GetEmbeddedObjectName();
    }

    /// <summary>
    /// Returns the embedded object show as icon without changing the shape.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetEmbeddedObjectShowAsIcon(PptxShape target)
    {
        return target.GetEmbeddedObjectShowAsIcon();
    }

    /// <summary>
    /// Returns the embedded object view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting embedded object value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxEmbeddedObject AsEmbeddedObject(PptxShape target)
    {
        return target.AsEmbeddedObject();
    }

    /// <summary>
    /// Returns the image relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetImageRelationshipId(PptxShape target)
    {
        return target.GetImageRelationshipId();
    }

    /// <summary>
    /// Extracts image from the shape without removing it.
    /// </summary>
    /// <returns>The resulting image file value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxImageFile ExtractImage(PptxShape target)
    {
        return target.ExtractImage();
    }

    /// <summary>
    /// Replaces image while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ReplaceImage(PptxShape target, byte[] bytes)
    {
        target.ReplaceImage(bytes);
    }

    /// <summary>
    /// Reports whether the shape is media shape.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsMediaShape(PptxShape target)
    {
        return target.IsMediaShape();
    }

    /// <summary>
    /// Returns the media relationship id without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetMediaRelationshipId(PptxShape target)
    {
        return target.GetMediaRelationshipId();
    }

    /// <summary>
    /// Reports whether the shape contains or enables poster frame.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasPosterFrame(PptxShape target)
    {
        return target.HasPosterFrame();
    }

    /// <summary>
    /// Extracts poster frame from the shape without removing it.
    /// </summary>
    /// <returns>The resulting image file value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxImageFile ExtractPosterFrame(PptxShape target)
    {
        return target.ExtractPosterFrame();
    }

    /// <summary>
    /// Replaces poster frame while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ReplacePosterFrame(PptxShape target, byte[] bytes)
    {
        target.ReplacePosterFrame(bytes);
    }

    /// <summary>
    /// Updates the image crop and the underlying presentation data.
    /// </summary>
    /// <param name="left">The left value to use.</param>
    /// <param name="top">The top value to use.</param>
    /// <param name="right">The right value to use.</param>
    /// <param name="bottom">The bottom value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetImageCrop(PptxShape target, int left, int top, int right, int bottom)
    {
        target.SetImageCrop(left, top, right, bottom);
    }

    /// <summary>
    /// Updates the picture crop from a typed percentage rectangle.
    /// </summary>
    /// <param name="crop">The crop rectangle value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetImageCropRectangle(PptxShape target, PptxImageCrop crop)
    {
        target.SetImageCropRectangle(crop);
    }

    /// <summary>
    /// Returns the image crop without changing the shape.
    /// </summary>
    /// <returns>The resulting image crop value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxImageCrop GetImageCrop(PptxShape target)
    {
        return target.GetImageCrop();
    }

    /// <summary>
    /// Updates the image stretch and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void SetImageStretch(PptxShape target)
    {
        target.SetImageStretch();
    }

    /// <summary>
    /// Updates the image tile and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void SetImageTile(PptxShape target)
    {
        target.SetImageTile();
    }

    /// <summary>
    /// Reports whether the shape is image tiled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsImageTiled(PptxShape target)
    {
        return target.IsImageTiled();
    }

    /// <summary>
    /// Reports whether the shape is image stretched.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsImageStretched(PptxShape target)
    {
        return target.IsImageStretched();
    }

    /// <summary>
    /// Reports whether the shape is text box.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsTextBox(PptxShape target)
    {
        return target.IsTextBox();
    }

    /// <summary>
    /// Updates the text box and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTextBox(PptxShape target, bool enabled)
    {
        target.SetTextBox(enabled);
    }

    /// <summary>
    /// Updates the preset geometry and the underlying presentation data.
    /// </summary>
    /// <param name="geometry">The geometry value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetPresetGeometry(PptxShape target, string geometry)
    {
        target.SetPresetGeometry(geometry);
    }

    /// <summary>
    /// Updates the freeform geometry and the underlying presentation data.
    /// </summary>
    /// <param name="points">The points value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFreeformGeometry(PptxShape target, List<PptxPoint> points)
    {
        target.SetFreeformGeometry(points);
    }

    /// <summary>
    /// Returns the supported commands from every custom-geometry path.
    /// </summary>
    /// <returns>The paths in XML order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxFreeformPath> GetFreeformPaths(PptxShape target)
    {
        return target.GetFreeformPaths();
    }

    /// <summary>
    /// Replaces custom geometry with the supplied paths and commands.
    /// </summary>
    /// <param name="paths">The freeform paths to write.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFreeformPaths(PptxShape target, List<PptxFreeformPath> paths)
    {
        target.SetFreeformPaths(paths);
    }

    /// <summary>
    /// Updates the fill and the underlying presentation data.
    /// </summary>
    /// <param name="fill">The fill value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFill(PptxShape target, PptxFill fill)
    {
        target.SetFill(fill);
    }

    /// <summary>
    /// Updates the line and the underlying presentation data.
    /// </summary>
    /// <param name="line">The line value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLine(PptxShape target, PptxLine line)
    {
        target.SetLine(line);
    }

    /// <summary>
    /// Updates the effect and the underlying presentation data.
    /// </summary>
    /// <param name="effect">The effect value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetEffect(PptxShape target, PptxEffect effect)
    {
        target.SetEffect(effect);
    }

    /// <summary>
    /// Updates the bounds and the underlying presentation data.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBounds(PptxShape target, PptxBounds bounds)
    {
        target.SetBounds(bounds);
    }

    /// <summary>
    /// Reports whether the shape is group.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsGroup(PptxShape target)
    {
        return target.IsGroup();
    }

    /// <summary>
    /// Returns the group view of this shape, or an empty value when the underlying shape has another kind.
    /// </summary>
    /// <returns>The resulting group shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxGroupShape AsGroup(PptxShape target)
    {
        return target.AsGroup();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ChildCount(PptxShape target)
    {
        return target.ChildCount();
    }

    /// <summary>
    /// Returns the child without changing the shape.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape GetChild(PptxShape target, int index)
    {
        return target.GetChild(index);
    }

    /// <summary>
    /// Finds the descendant by id; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape? FindDescendantById(PptxShape target, int id)
    {
        return target.FindDescendantById(id);
    }

    /// <summary>
    /// Finds the descendant by name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape? FindDescendantByName(PptxShape target, string name)
    {
        return target.FindDescendantByName(name);
    }

    /// <summary>
    /// Reports whether the shape is connector.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsConnector(PptxShape target)
    {
        return target.IsConnector();
    }

    /// <summary>
    /// Returns the connector type without changing the shape.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetConnectorType(PptxShape target)
    {
        return target.GetConnectorType();
    }

    /// <summary>
    /// Updates the connector type and the underlying presentation data.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetConnectorType(PptxShape target, string connectorType)
    {
        target.SetConnectorType(connectorType);
    }

    /// <summary>
    /// Updates the connector ends and the underlying presentation data.
    /// </summary>
    /// <param name="start">The start value to use.</param>
    /// <param name="end">The end value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetConnectorEnds(PptxShape target, PptxPoint start, PptxPoint end)
    {
        target.SetConnectorEnds(start, end);
    }

    /// <summary>
    /// Updates the connector ends and the underlying presentation data.
    /// </summary>
    /// <param name="startX">The start x value to use.</param>
    /// <param name="startY">The start y value to use.</param>
    /// <param name="endX">The end x value to use.</param>
    /// <param name="endY">The end y value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetConnectorEndCoordinates(PptxShape target, long startX, long startY, long endX, long endY)
    {
        target.SetConnectorEndCoordinates(startX, startY, endX, endY);
    }

    /// <summary>
    /// Returns the connector start position and optional shape attachment.
    /// </summary>
    /// <returns>The resulting connection point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxConnectionPoint GetConnectorStartConnection(PptxShape target)
    {
        return target.GetConnectorStartConnection();
    }

    /// <summary>
    /// Returns the connector end position and optional shape attachment.
    /// </summary>
    /// <returns>The resulting connection point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxConnectionPoint GetConnectorEndConnection(PptxShape target)
    {
        return target.GetConnectorEndConnection();
    }

    /// <summary>
    /// Updates connector endpoint positions and optional shape attachments.
    /// </summary>
    /// <param name="start">The start connection point.</param>
    /// <param name="end">The end connection point.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetConnectorConnections(PptxShape target, PptxConnectionPoint start, PptxConnectionPoint end)
    {
        target.SetConnectorConnections(start, end);
    }

    /// <summary>
    /// Updates the name and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetName(PptxShape target, string name)
    {
        target.SetName(name);
    }

    /// <summary>
    /// Reports whether the shape is visible.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsVisible(PptxShape target)
    {
        return target.IsVisible();
    }

    /// <summary>
    /// Updates the visible and the underlying presentation data.
    /// </summary>
    /// <param name="visible">The visible value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetVisible(PptxShape target, bool visible)
    {
        target.SetVisible(visible);
    }

    /// <summary>
    /// Updates the hidden and the underlying presentation data.
    /// </summary>
    /// <param name="hidden">The hidden value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetHidden(PptxShape target, bool hidden)
    {
        target.SetHidden(hidden);
    }

    /// <summary>
    /// Updates the alternative text and the underlying presentation data.
    /// </summary>
    /// <param name="alternativeText">The alternative text value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetAlternativeText(PptxShape target, string alternativeText)
    {
        target.SetAlternativeText(alternativeText);
    }

    /// <summary>
    /// Updates the title and the underlying presentation data.
    /// </summary>
    /// <param name="title">The title value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTitle(PptxShape target, string title)
    {
        target.SetTitle(title);
    }

    /// <summary>
    /// Updates the lock and the underlying presentation data.
    /// </summary>
    /// <param name="lockName">The lock name value to use.</param>
    /// <param name="locked">The locked value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLock(PptxShape target, string lockName, bool locked)
    {
        target.SetLock(lockName, locked);
    }

    /// <summary>
    /// Returns the lock without changing the shape.
    /// </summary>
    /// <param name="lockName">The lock name value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool GetLock(PptxShape target, string lockName)
    {
        return target.GetLock(lockName);
    }

    /// <summary>
    /// Updates the locks and the underlying presentation data.
    /// </summary>
    /// <param name="noMove">The no move value to use.</param>
    /// <param name="noResize">The no resize value to use.</param>
    /// <param name="noRotate">The no rotate value to use.</param>
    /// <param name="noSelect">The no select value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLocks(PptxShape target, bool noMove, bool noResize, bool noRotate, bool noSelect)
    {
        target.SetLocks(noMove, noResize, noRotate, noSelect);
    }

    /// <summary>
    /// Updates the hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="url">The url value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetHyperlink(PptxShape target, string url)
    {
        target.SetHyperlink(url);
    }

    /// <summary>
    /// Returns the hyperlink without changing the shape.
    /// </summary>
    /// <returns>The resulting hyperlink value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxHyperlink GetHyperlink(PptxShape target)
    {
        return target.GetHyperlink();
    }
    /// <summary>
    /// Returns the shape-level hyperlink followed by all run-level hyperlinks in text order.
    /// </summary>
    /// <returns>The hyperlinks owned by the shape and its text runs.</returns>
    /// <param name="target">The shape to inspect.</param>
    public List<PptxHyperlink> GetHyperlinks(PptxShape target)
    {
        return target.GetHyperlinks();
    }

    /// <summary>
    /// Updates the external hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="url">The url value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetExternalHyperlink(PptxShape target, string url)
    {
        target.SetExternalHyperlink(url);
    }

    /// <summary>
    /// Updates the file hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="fileUri">The file uri value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFileHyperlink(PptxShape target, string fileUri)
    {
        target.SetFileHyperlink(fileUri);
    }

    /// <summary>
    /// Updates the slide hyperlink and the underlying presentation data.
    /// </summary>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSlideHyperlink(PptxShape target, int slideIndex)
    {
        target.SetSlideHyperlink(slideIndex);
    }

    /// <summary>
    /// Updates the next slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void SetNextSlideAction(PptxShape target)
    {
        target.SetNextSlideAction();
    }

    /// <summary>
    /// Updates the previous slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void SetPreviousSlideAction(PptxShape target)
    {
        target.SetPreviousSlideAction();
    }

    /// <summary>
    /// Updates the first slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void SetFirstSlideAction(PptxShape target)
    {
        target.SetFirstSlideAction();
    }

    /// <summary>
    /// Updates the last slide action and the underlying presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void SetLastSlideAction(PptxShape target)
    {
        target.SetLastSlideAction();
    }

    /// <summary>
    /// Updates the custom show action and the underlying presentation data.
    /// </summary>
    /// <param name="customShowName">The custom show name value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetCustomShowAction(PptxShape target, string customShowName)
    {
        target.SetCustomShowAction(customShowName);
    }

    /// <summary>
    /// Updates the action and the underlying presentation data.
    /// </summary>
    /// <param name="action">The action value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetAction(PptxShape target, string action)
    {
        target.SetAction(action);
    }

    /// <summary>
    /// Removes the hyperlink and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveHyperlink(PptxShape target)
    {
        target.RemoveHyperlink();
    }

    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Remove(PptxShape target)
    {
        target.Remove();
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original shape.
    /// </summary>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape Clone(PptxShape target)
    {
        return target.Clone();
    }
}
