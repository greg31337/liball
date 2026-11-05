using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxDrawingPart"/> data.
/// </summary>
public class XlsxDrawingPartOperations : IXlsxDrawingPartOperations
{
    /// <summary>
    /// Initializes a new xlsx drawing part operations instance.
    /// </summary>
    public XlsxDrawingPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.ResolveChartRelationships through the ResolveChartRelationships operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDrawingChartRelationshipResult ResolveChartRelationships(XlsxDrawingPart data, XlsxRelationshipsPart relationships)
    {
        return data.ResolveChartRelationships(relationships);
    }

    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.AddTwoCellImage through the AddTwoCellImage operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <param name="relationshipId">The relationship id value used by the operation.</param>
    /// <param name="fromCell">The from cell value used by the operation.</param>
    /// <param name="toCell">The to cell value used by the operation.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="description">The description value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDrawingImagePlacementResult AddTwoCellImage(XlsxDrawingPart data, string relationshipId, string fromCell, string toCell, string name, string description)
    {
        return data.AddTwoCellImage(relationshipId, fromCell, toCell, name, description);
    }

    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.AddOneCellImage through the AddOneCellImage operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <param name="relationshipId">The relationship id value used by the operation.</param>
    /// <param name="fromCell">The from cell value used by the operation.</param>
    /// <param name="extentCxEmu">The extent cx emu value used by the operation.</param>
    /// <param name="extentCyEmu">The extent cy emu value used by the operation.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="description">The description value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDrawingImagePlacementResult AddOneCellImage(XlsxDrawingPart data, string relationshipId, string fromCell, string extentCxEmu, string extentCyEmu, string name, string description)
    {
        return data.AddOneCellImage(relationshipId, fromCell, extentCxEmu, extentCyEmu, name, description);
    }

    /// <summary>Adds an absolutely positioned image anchor.</summary>
    /// <param name="data">The drawing part to update.</param><param name="relationshipId">The image relationship identifier.</param><param name="positionXEmu">The horizontal position.</param><param name="positionYEmu">The vertical position.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The name.</param><param name="description">The description.</param>
    /// <returns>The authored image anchor.</returns>
    public XlsxDrawingImagePlacementResult AddAbsoluteImage(XlsxDrawingPart data, string relationshipId, string positionXEmu, string positionYEmu, string extentCxEmu, string extentCyEmu, string name, string description)
    {
        return data.AddAbsoluteImage(relationshipId, positionXEmu, positionYEmu, extentCxEmu, extentCyEmu, name, description);
    }

    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.AddImageAnchorCore through the AddImageAnchorCore operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <param name="anchorType">The anchor type value used by the operation.</param>
    /// <param name="relationshipId">The relationship id value used by the operation.</param>
    /// <param name="fromCell">The from cell value used by the operation.</param>
    /// <param name="toCell">The to cell value used by the operation.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="description">The description value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDrawingImagePlacementResult AddImageAnchorCore(XlsxDrawingPart data, string anchorType, string relationshipId, string fromCell, string toCell, string name, string description)
    {
        return data.AddImageAnchorCore(anchorType, relationshipId, fromCell, toCell, name, description);
    }

    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.GetNextPictureId through the GetNextPictureId operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public uint GetNextPictureId(XlsxDrawingPart data)
    {
        return data.GetNextPictureId();
    }

    /// <summary>Sets a click hyperlink on an image anchor.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="anchorIndex">The zero-based image anchor index.</param>
    /// <param name="relationshipId">The hyperlink relationship identifier.</param>
    /// <param name="tooltip">The hyperlink tooltip.</param>
    /// <returns>Validation diagnostics for the mutation.</returns>
    public XlsxValidationResult SetImageHyperlink(XlsxDrawingPart data, int anchorIndex, string relationshipId, string tooltip)
    {
        return data.SetImageHyperlink(anchorIndex, relationshipId, tooltip);
    }

    /// <summary>Clears a click hyperlink from an image anchor.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="anchorIndex">The zero-based image anchor index.</param>
    /// <returns>Validation diagnostics for the mutation.</returns>
    public XlsxValidationResult ClearImageHyperlink(XlsxDrawingPart data, int anchorIndex)
    {
        return data.ClearImageHyperlink(anchorIndex);
    }

    /// <summary>Adds a two-cell drawing object.</summary>
    /// <param name="data">The drawing part.</param><param name="objectKind">The object kind.</param><param name="fromCell">The first cell.</param><param name="toCell">The second cell.</param><param name="name">The name.</param><param name="text">The text.</param><param name="relationshipId">The relationship identifier.</param>
    /// <returns>The authored object.</returns>
    public XlsxDrawingObjectOperationResult AddTwoCellDrawingObject(XlsxDrawingPart data, XlsxDrawingObjectKind objectKind, string fromCell, string toCell, string name, string text, string relationshipId)
    {
        return data.AddTwoCellDrawingObject(objectKind, fromCell, toCell, name, text, relationshipId);
    }

    /// <summary>Adds a one-cell drawing object.</summary>
    /// <param name="data">The drawing part.</param><param name="objectKind">The object kind.</param><param name="fromCell">The first cell.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The name.</param><param name="text">The text.</param><param name="relationshipId">The relationship identifier.</param>
    /// <returns>The authored object.</returns>
    public XlsxDrawingObjectOperationResult AddOneCellDrawingObject(XlsxDrawingPart data, XlsxDrawingObjectKind objectKind, string fromCell, string extentCxEmu, string extentCyEmu, string name, string text, string relationshipId)
    {
        return data.AddOneCellDrawingObject(objectKind, fromCell, extentCxEmu, extentCyEmu, name, text, relationshipId);
    }

    /// <summary>Adds an absolutely positioned drawing object.</summary>
    /// <param name="data">The drawing part.</param><param name="objectKind">The object kind.</param><param name="positionXEmu">The horizontal position.</param><param name="positionYEmu">The vertical position.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The name.</param><param name="text">The text.</param><param name="relationshipId">The relationship identifier.</param>
    /// <returns>The authored object.</returns>
    public XlsxDrawingObjectOperationResult AddAbsoluteDrawingObject(XlsxDrawingPart data, XlsxDrawingObjectKind objectKind, string positionXEmu, string positionYEmu, string extentCxEmu, string extentCyEmu, string name, string text, string relationshipId)
    {
        return data.AddAbsoluteDrawingObject(objectKind, positionXEmu, positionYEmu, extentCxEmu, extentCyEmu, name, text, relationshipId);
    }

    /// <summary>Sets a drawing object hyperlink.</summary>
    /// <param name="data">The drawing part.</param><param name="anchorIndex">The object index.</param><param name="relationshipId">The relationship identifier.</param><param name="tooltip">The tooltip.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult SetDrawingObjectHyperlink(XlsxDrawingPart data, int anchorIndex, string relationshipId, string tooltip)
    {
        return data.SetDrawingObjectHyperlink(anchorIndex, relationshipId, tooltip);
    }

    /// <summary>Sets basic shape text.</summary>
    /// <param name="data">The drawing part.</param><param name="anchorIndex">The object index.</param><param name="text">The replacement text.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult SetDrawingObjectText(XlsxDrawingPart data, int anchorIndex, string text)
    {
        return data.SetDrawingObjectText(anchorIndex, text);
    }

    /// <summary>Adds a one-cell form control.</summary>
    /// <param name="data">The drawing part.</param><param name="relationshipId">The relationship identifier.</param><param name="fromCell">The starting cell.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The control name.</param>
    /// <returns>The authored form control.</returns>
    public XlsxDrawingObjectOperationResult AddOneCellFormControl(XlsxDrawingPart data, string relationshipId, string fromCell, string extentCxEmu, string extentCyEmu, string name)
    {
        return data.AddOneCellFormControl(relationshipId, fromCell, extentCxEmu, extentCyEmu, name);
    }

    /// <summary>Removes a form control.</summary>
    /// <param name="data">The drawing part.</param><param name="controlIndex">The control index.</param>
    /// <returns>The removed control.</returns>
    public XlsxDrawingObjectOperationResult RemoveFormControl(XlsxDrawingPart data, int controlIndex)
    {
        return data.RemoveFormControl(controlIndex);
    }
}
