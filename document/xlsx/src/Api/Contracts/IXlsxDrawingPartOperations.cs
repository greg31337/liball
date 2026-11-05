using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxDrawingPart"/> data.
/// </summary>
public interface IXlsxDrawingPartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.ResolveChartRelationships through the ResolveChartRelationships operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxDrawingChartRelationshipResult ResolveChartRelationships(XlsxDrawingPart data, XlsxRelationshipsPart relationships);
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
    XlsxDrawingImagePlacementResult AddTwoCellImage(XlsxDrawingPart data, string relationshipId, string fromCell, string toCell, string name, string description);
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
    XlsxDrawingImagePlacementResult AddOneCellImage(XlsxDrawingPart data, string relationshipId, string fromCell, string extentCxEmu, string extentCyEmu, string name, string description);
    /// <summary>Adds an absolutely positioned image anchor.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <param name="positionXEmu">The horizontal position in English metric units.</param>
    /// <param name="positionYEmu">The vertical position in English metric units.</param>
    /// <param name="extentCxEmu">The horizontal extent in English metric units.</param>
    /// <param name="extentCyEmu">The vertical extent in English metric units.</param>
    /// <param name="name">The picture name.</param>
    /// <param name="description">The picture description.</param>
    /// <returns>The authored image anchor and validation diagnostics.</returns>
    XlsxDrawingImagePlacementResult AddAbsoluteImage(XlsxDrawingPart data, string relationshipId, string positionXEmu, string positionYEmu, string extentCxEmu, string extentCyEmu, string name, string description);
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
    XlsxDrawingImagePlacementResult AddImageAnchorCore(XlsxDrawingPart data, string anchorType, string relationshipId, string fromCell, string toCell, string name, string description);
    /// <summary>
    /// Invokes <see cref="XlsxDrawingPart"/>.GetNextPictureId through the GetNextPictureId operation.
    /// </summary>
    /// <param name="data">The xlsx drawing part data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    uint GetNextPictureId(XlsxDrawingPart data);
    /// <summary>Sets a click hyperlink on an image anchor.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="anchorIndex">The zero-based image anchor index.</param>
    /// <param name="relationshipId">The hyperlink relationship identifier.</param>
    /// <param name="tooltip">The hyperlink tooltip.</param>
    /// <returns>Validation diagnostics for the mutation.</returns>
    XlsxValidationResult SetImageHyperlink(XlsxDrawingPart data, int anchorIndex, string relationshipId, string tooltip);
    /// <summary>Clears a click hyperlink from an image anchor.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="anchorIndex">The zero-based image anchor index.</param>
    /// <returns>Validation diagnostics for the mutation.</returns>
    XlsxValidationResult ClearImageHyperlink(XlsxDrawingPart data, int anchorIndex);
    /// <summary>Adds a drawing object bounded by two cell markers.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="objectKind">The drawing payload kind.</param>
    /// <param name="fromCell">The first cell reference.</param>
    /// <param name="toCell">The second cell reference.</param>
    /// <param name="name">The object name.</param>
    /// <param name="text">The basic shape text.</param>
    /// <param name="relationshipId">The relationship identifier for relationship-backed objects.</param>
    /// <returns>The authored object anchor and validation diagnostics.</returns>
    XlsxDrawingObjectOperationResult AddTwoCellDrawingObject(XlsxDrawingPart data, XlsxDrawingObjectKind objectKind, string fromCell, string toCell, string name, string text, string relationshipId);
    /// <summary>Adds a drawing object starting at one cell marker.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="objectKind">The drawing payload kind.</param>
    /// <param name="fromCell">The first cell reference.</param>
    /// <param name="extentCxEmu">The horizontal extent in English metric units.</param>
    /// <param name="extentCyEmu">The vertical extent in English metric units.</param>
    /// <param name="name">The object name.</param>
    /// <param name="text">The basic shape text.</param>
    /// <param name="relationshipId">The relationship identifier for relationship-backed objects.</param>
    /// <returns>The authored object anchor and validation diagnostics.</returns>
    XlsxDrawingObjectOperationResult AddOneCellDrawingObject(XlsxDrawingPart data, XlsxDrawingObjectKind objectKind, string fromCell, string extentCxEmu, string extentCyEmu, string name, string text, string relationshipId);
    /// <summary>Adds an absolutely positioned drawing object.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="objectKind">The drawing payload kind.</param>
    /// <param name="positionXEmu">The horizontal position in English metric units.</param>
    /// <param name="positionYEmu">The vertical position in English metric units.</param>
    /// <param name="extentCxEmu">The horizontal extent in English metric units.</param>
    /// <param name="extentCyEmu">The vertical extent in English metric units.</param>
    /// <param name="name">The object name.</param>
    /// <param name="text">The basic shape text.</param>
    /// <param name="relationshipId">The relationship identifier for relationship-backed objects.</param>
    /// <returns>The authored object anchor and validation diagnostics.</returns>
    XlsxDrawingObjectOperationResult AddAbsoluteDrawingObject(XlsxDrawingPart data, XlsxDrawingObjectKind objectKind, string positionXEmu, string positionYEmu, string extentCxEmu, string extentCyEmu, string name, string text, string relationshipId);
    /// <summary>Sets a click hyperlink on a non-image drawing object.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="anchorIndex">The zero-based object anchor index.</param>
    /// <param name="relationshipId">The hyperlink relationship identifier.</param>
    /// <param name="tooltip">The hyperlink tooltip.</param>
    /// <returns>Validation diagnostics for the mutation.</returns>
    XlsxValidationResult SetDrawingObjectHyperlink(XlsxDrawingPart data, int anchorIndex, string relationshipId, string tooltip);
    /// <summary>Sets basic text on a shape or text box.</summary>
    /// <param name="data">The drawing part to update.</param>
    /// <param name="anchorIndex">The zero-based object anchor index.</param>
    /// <param name="text">The replacement text.</param>
    /// <returns>Validation diagnostics for the mutation.</returns>
    XlsxValidationResult SetDrawingObjectText(XlsxDrawingPart data, int anchorIndex, string text);
    /// <summary>Adds a named one-cell form control anchor.</summary>
    /// <param name="data">The drawing part to update.</param><param name="relationshipId">The control-properties relationship identifier.</param><param name="fromCell">The starting cell.</param><param name="extentCxEmu">The horizontal extent.</param><param name="extentCyEmu">The vertical extent.</param><param name="name">The control name.</param>
    /// <returns>The authored form control.</returns>
    XlsxDrawingObjectOperationResult AddOneCellFormControl(XlsxDrawingPart data, string relationshipId, string fromCell, string extentCxEmu, string extentCyEmu, string name);
    /// <summary>Removes a form control anchor.</summary>
    /// <param name="data">The drawing part to update.</param><param name="controlIndex">The zero-based form-control index.</param>
    /// <returns>The removed form control and validation diagnostics.</returns>
    XlsxDrawingObjectOperationResult RemoveFormControl(XlsxDrawingPart data, int controlIndex);
}
