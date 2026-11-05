using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML drawing part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxDrawingPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains image anchors.
    /// </summary>
    public List<XlsxDrawingImageAnchor> ImageAnchors;
    /// <summary>
    /// Contains chart anchors.
    /// </summary>
    public List<XlsxDrawingChartAnchor> ChartAnchors;
    /// <summary>
    /// Contains raw anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> RawAnchors;
    /// <summary>
    /// Contains text box anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> TextBoxAnchors;
    /// <summary>
    /// Contains shape anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> ShapeAnchors;
    /// <summary>
    /// Contains connector anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> ConnectorAnchors;
    /// <summary>
    /// Contains form control anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> FormControlAnchors;
    /// <summary>
    /// Contains preserved diagram anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> DiagramAnchors;
    /// <summary>
    /// Contains other anchors.
    /// </summary>
    public List<XlsxDrawingRawAnchor> OtherAnchors;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Initializes a new drawing part with its default state.
    /// </summary>
    public XlsxDrawingPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/drawings/drawing1.xml";
        RawXml = "";
        ImageAnchors = new List<XlsxDrawingImageAnchor>();
        ChartAnchors = new List<XlsxDrawingChartAnchor>();
        RawAnchors = new List<XlsxDrawingRawAnchor>();
        TextBoxAnchors = new List<XlsxDrawingRawAnchor>();
        ShapeAnchors = new List<XlsxDrawingRawAnchor>();
        ConnectorAnchors = new List<XlsxDrawingRawAnchor>();
        FormControlAnchors = new List<XlsxDrawingRawAnchor>();
        DiagramAnchors = new List<XlsxDrawingRawAnchor>();
        OtherAnchors = new List<XlsxDrawingRawAnchor>();
        UnknownXml = new List<string>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Performs the resolve chart relationships operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDrawingChartRelationshipResult ResolveChartRelationships(XlsxRelationshipsPart relationships)
    {
        XlsxDrawingChartRelationshipResult result = new XlsxDrawingChartRelationshipResult();
        if (ChartAnchors == null)
        {
            result.AddError("drawing.chartAnchorsNull", "Drawing chart anchor list must not be null.");
            return result;
        }

        if (ChartAnchors.Count == 0)
        {
            return result;
        }

        if (relationships == null)
        {
            result.AddError("drawing.chartRelationshipsMissing", "Drawing relationships part is required when drawing chart anchors are present.");
            return result;
        }

        foreach (XlsxDrawingChartAnchor anchor in ChartAnchors)
        {
            ResolveChartRelationship(result, anchor, relationships);
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static void ResolveChartRelationship(XlsxDrawingChartRelationshipResult result, XlsxDrawingChartAnchor anchor, XlsxRelationshipsPart relationships)
    {
        if (anchor == null)
        {
            result.AddError("drawing.chartAnchorNull", "Drawing chart anchor list must not contain null entries.");
            return;
        }

        result.ChartAnchors.Add(anchor);
        if (anchor.RelationshipId == null || anchor.RelationshipId.Length == 0)
        {
            result.AddError("drawing.chartRelationshipIdMissing", "Drawing chart anchor must have an r:id relationship id.");
            return;
        }

        XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(anchor.RelationshipId);
        foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
        {
            diagnostic.SetLocation("", "chart", "r:id");
            result.AddDiagnostic(diagnostic);
        }

        if (!relationshipId.IsValid)
        {
            return;
        }

        XlsxRelationship? relationship = relationships.GetRelationship(anchor.RelationshipId);
        if (relationship == null)
        {
            XlsxDiagnostic diagnostic = result.AddError("drawing.chartRelationshipMissing", "Drawing chart anchor references a missing relationship.");
            diagnostic.SetRelationshipId(anchor.RelationshipId);
            return;
        }

        if (relationship.Type != XlsxRelationshipTypes.Chart)
        {
            XlsxDiagnostic diagnostic = result.AddError("drawing.chartRelationshipType", "Drawing chart anchor relationship must target a chart part.");
            diagnostic.SetRelationshipId(anchor.RelationshipId);
            return;
        }

        anchor.Relationship = relationship;
        anchor.HasRelationship = true;
        anchor.Target = relationship.Target;
        anchor.TargetPath = relationship.TargetPath;
    }

    /// <summary>
    /// Adds two cell image to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "relationshipId">The relationship id value used by the operation.</param>
    /// <param name = "fromCell">The from cell value used by the operation.</param>
    /// <param name = "toCell">The to cell value used by the operation.</param>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "description">The description value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDrawingImagePlacementResult AddTwoCellImage(string relationshipId, string fromCell, string toCell, string name, string description)
    {
        XlsxDrawingImagePlacementResult result = AddImageAnchorCore("twoCell", relationshipId, fromCell, toCell, name, description);
        return result;
    }

    /// <summary>
    /// Adds one cell image to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "relationshipId">The relationship id value used by the operation.</param>
    /// <param name = "fromCell">The from cell value used by the operation.</param>
    /// <param name = "extentCxEmu">The extent cx emu value used by the operation.</param>
    /// <param name = "extentCyEmu">The extent cy emu value used by the operation.</param>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "description">The description value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDrawingImagePlacementResult AddOneCellImage(string relationshipId, string fromCell, string extentCxEmu, string extentCyEmu, string name, string description)
    {
        XlsxDrawingImagePlacementResult result = AddImageAnchorCore("oneCell", relationshipId, fromCell, fromCell, name, description);
        if (!result.IsValid || result.Anchor == null)
        {
            return result;
        }

        result.Anchor.HasExtent = true;
        result.Anchor.AnchorKind = XlsxDrawingAnchorKind.OneCell;
        result.Anchor.ExtentCxEmu = XlsxCellValueConverter.NormalizeText(extentCxEmu);
        result.Anchor.ExtentCyEmu = XlsxCellValueConverter.NormalizeText(extentCyEmu);
        return result;
    }

    internal XlsxDrawingImagePlacementResult AddAbsoluteImage(string relationshipId, string positionXEmu, string positionYEmu, string extentCxEmu, string extentCyEmu, string name, string description)
    {
        XlsxDrawingImagePlacementResult result = new XlsxDrawingImagePlacementResult();
        if (ImageAnchors == null)
        {
            result.AddError("drawing.imageAnchorsNull", "Drawing image anchor list must not be null.");
            return result;
        }

        XlsxDrawingImageAnchor anchor = new XlsxDrawingImageAnchor();
        anchor.AnchorType = "absolute";
        anchor.AnchorKind = XlsxDrawingAnchorKind.Absolute;
        anchor.RelationshipId = XlsxCellValueConverter.NormalizeText(relationshipId);
        anchor.PictureId = GetNextPictureId();
        anchor.HasPictureId = true;
        anchor.Name = XlsxCellValueConverter.NormalizeText(name);
        anchor.Description = XlsxCellValueConverter.NormalizeText(description);
        anchor.PositionXEmu = XlsxCellValueConverter.NormalizeText(positionXEmu);
        anchor.PositionYEmu = XlsxCellValueConverter.NormalizeText(positionYEmu);
        anchor.ExtentCxEmu = XlsxCellValueConverter.NormalizeText(extentCxEmu);
        anchor.ExtentCyEmu = XlsxCellValueConverter.NormalizeText(extentCyEmu);
        anchor.HasExtent = true;
        ImageAnchors.Add(anchor);
        RawXml = "";
        result.Anchor = anchor;
        result.HasAnchor = true;
        return result;
    }

    /// <summary>
    /// Adds image anchor core to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "anchorType">The anchor type value used by the operation.</param>
    /// <param name = "relationshipId">The relationship id value used by the operation.</param>
    /// <param name = "fromCell">The from cell value used by the operation.</param>
    /// <param name = "toCell">The to cell value used by the operation.</param>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "description">The description value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDrawingImagePlacementResult AddImageAnchorCore(string anchorType, string relationshipId, string fromCell, string toCell, string name, string description)
    {
        XlsxDrawingImagePlacementResult result = new XlsxDrawingImagePlacementResult();
        if (ImageAnchors == null)
        {
            result.AddError("drawing.imageAnchorsNull", "Drawing image anchor list must not be null.");
            return result;
        }

        XlsxA1CellReferenceResult from = XlsxA1CellReferenceConverter.Parse(fromCell);
        if (!from.IsValid)
        {
            AddCellReferenceDiagnostics(result, from);
            return result;
        }

        XlsxA1CellReferenceResult to = XlsxA1CellReferenceConverter.Parse(toCell);
        if (!to.IsValid)
        {
            AddCellReferenceDiagnostics(result, to);
            return result;
        }

        XlsxDrawingImageAnchor anchor = new XlsxDrawingImageAnchor();
        anchor.AnchorType = anchorType;
        anchor.AnchorKind = XlsxDrawingPartReader.GetAnchorKind(anchorType);
        anchor.RelationshipId = XlsxCellValueConverter.NormalizeText(relationshipId);
        anchor.PictureId = GetNextPictureId();
        anchor.HasPictureId = true;
        anchor.Name = XlsxCellValueConverter.NormalizeText(name);
        if (anchor.Name.Length == 0)
        {
            anchor.Name = "Picture " + anchor.PictureId.ToString(CultureInfo.InvariantCulture);
        }

        anchor.Description = XlsxCellValueConverter.NormalizeText(description);
        SetMarker(anchor.From, from.Row, from.Column);
        SetMarker(anchor.To, to.Row, to.Column);
        ImageAnchors.Add(anchor);
        RawXml = "";
        result.DrawingPart = this;
        result.HasDrawingPart = true;
        result.Anchor = anchor;
        result.HasAnchor = true;
        return result;
    }

    internal XlsxDrawingObjectOperationResult AddTwoCellDrawingObject(XlsxDrawingObjectKind objectKind, string fromCell, string toCell, string name, string text, string relationshipId)
    {
        XlsxDrawingObjectOperationResult result = CreateDrawingObject(objectKind, name, text, relationshipId);
        if (!result.IsValid || result.Anchor == null)
        {
            return result;
        }
        XlsxA1CellReferenceResult from = XlsxA1CellReferenceConverter.Parse(fromCell);
        XlsxA1CellReferenceResult to = XlsxA1CellReferenceConverter.Parse(toCell);
        AddObjectCellReferenceDiagnostics(result, from);
        AddObjectCellReferenceDiagnostics(result, to);
        if (!result.IsValid)
        {
            return result;
        }
        result.Anchor.AnchorType = "twoCell";
        result.Anchor.AnchorKind = XlsxDrawingAnchorKind.TwoCell;
        SetMarker(result.Anchor.From, from.Row, from.Column);
        SetMarker(result.Anchor.To, to.Row, to.Column);
        AddDrawingObject(result.Anchor);
        result.HasAnchor = true;
        return result;
    }

    internal XlsxDrawingObjectOperationResult AddOneCellDrawingObject(XlsxDrawingObjectKind objectKind, string fromCell, string extentCxEmu, string extentCyEmu, string name, string text, string relationshipId)
    {
        XlsxDrawingObjectOperationResult result = CreateDrawingObject(objectKind, name, text, relationshipId);
        if (!result.IsValid || result.Anchor == null)
        {
            return result;
        }
        XlsxA1CellReferenceResult from = XlsxA1CellReferenceConverter.Parse(fromCell);
        AddObjectCellReferenceDiagnostics(result, from);
        if (!result.IsValid)
        {
            return result;
        }
        result.Anchor.AnchorType = "oneCell";
        result.Anchor.AnchorKind = XlsxDrawingAnchorKind.OneCell;
        SetMarker(result.Anchor.From, from.Row, from.Column);
        result.Anchor.ExtentCxEmu = XlsxCellValueConverter.NormalizeText(extentCxEmu);
        result.Anchor.ExtentCyEmu = XlsxCellValueConverter.NormalizeText(extentCyEmu);
        result.Anchor.HasExtent = true;
        AddDrawingObject(result.Anchor);
        result.HasAnchor = true;
        return result;
    }

    internal XlsxDrawingObjectOperationResult AddAbsoluteDrawingObject(XlsxDrawingObjectKind objectKind, string positionXEmu, string positionYEmu, string extentCxEmu, string extentCyEmu, string name, string text, string relationshipId)
    {
        XlsxDrawingObjectOperationResult result = CreateDrawingObject(objectKind, name, text, relationshipId);
        if (!result.IsValid || result.Anchor == null)
        {
            return result;
        }
        result.Anchor.AnchorType = "absolute";
        result.Anchor.AnchorKind = XlsxDrawingAnchorKind.Absolute;
        result.Anchor.PositionXEmu = XlsxCellValueConverter.NormalizeText(positionXEmu);
        result.Anchor.PositionYEmu = XlsxCellValueConverter.NormalizeText(positionYEmu);
        result.Anchor.ExtentCxEmu = XlsxCellValueConverter.NormalizeText(extentCxEmu);
        result.Anchor.ExtentCyEmu = XlsxCellValueConverter.NormalizeText(extentCyEmu);
        result.Anchor.HasExtent = true;
        AddDrawingObject(result.Anchor);
        result.HasAnchor = true;
        return result;
    }

    internal XlsxDrawingObjectOperationResult AddOneCellFormControl(string relationshipId, string fromCell, string extentCxEmu, string extentCyEmu, string name)
    {
        return AddOneCellDrawingObject(XlsxDrawingObjectKind.FormControl, fromCell, extentCxEmu, extentCyEmu, name, "", relationshipId);
    }

    internal XlsxDrawingObjectOperationResult RemoveFormControl(int controlIndex)
    {
        XlsxDrawingObjectOperationResult result = new XlsxDrawingObjectOperationResult();
        if (FormControlAnchors == null || controlIndex < 0 || controlIndex >= FormControlAnchors.Count)
        {
            result.AddError("drawing.formControlIndexRange", "Form control index is outside the form control anchor list.");
            return result;
        }
        XlsxDrawingRawAnchor anchor = FormControlAnchors[controlIndex];
        FormControlAnchors.RemoveAt(controlIndex);
        int index = 0;
        while (index < RawAnchors.Count)
        {
            if (RawAnchors[index] == anchor) { RawAnchors.RemoveAt(index); continue; }
            index = index + 1;
        }
        RawXml = "";
        result.Anchor = anchor;
        result.HasAnchor = true;
        return result;
    }

    internal XlsxDrawingObjectOperationResult CreateDrawingObject(XlsxDrawingObjectKind objectKind, string name, string text, string relationshipId)
    {
        XlsxDrawingObjectOperationResult result = new XlsxDrawingObjectOperationResult();
        if (RawAnchors == null)
        {
            result.AddError("drawing.rawAnchorsNull", "Drawing object anchor list must not be null.");
            return result;
        }
        if (objectKind == XlsxDrawingObjectKind.Unknown || objectKind == XlsxDrawingObjectKind.Diagram)
        {
            result.AddError("drawing.objectKindUnsupported", "Typed authoring requires a text box, shape, connector, or form control kind.");
            return result;
        }
        if (objectKind == XlsxDrawingObjectKind.FormControl && (relationshipId == null || relationshipId.Length == 0))
        {
            result.AddError("drawing.controlRelationshipMissing", "A form control requires a relationship identifier.");
            return result;
        }
        XlsxDrawingRawAnchor anchor = new XlsxDrawingRawAnchor();
        anchor.ObjectKind = objectKind;
        anchor.Kind = GetDrawingObjectKindText(objectKind);
        anchor.ObjectId = GetNextDrawingObjectId();
        anchor.HasObjectId = true;
        anchor.Name = XlsxCellValueConverter.NormalizeText(name);
        if (anchor.Name.Length == 0)
        {
            anchor.Name = "Drawing " + anchor.ObjectId.ToString(CultureInfo.InvariantCulture);
        }
        anchor.Text = XlsxCellValueConverter.NormalizeText(text);
        anchor.RelationshipId = XlsxCellValueConverter.NormalizeText(relationshipId);
        result.Anchor = anchor;
        return result;
    }

    internal void AddDrawingObject(XlsxDrawingRawAnchor anchor)
    {
        RawAnchors.Add(anchor);
        if (anchor.ObjectKind == XlsxDrawingObjectKind.TextBox) { TextBoxAnchors.Add(anchor); }
        else if (anchor.ObjectKind == XlsxDrawingObjectKind.Shape) { ShapeAnchors.Add(anchor); }
        else if (anchor.ObjectKind == XlsxDrawingObjectKind.Connector) { ConnectorAnchors.Add(anchor); }
        else if (anchor.ObjectKind == XlsxDrawingObjectKind.FormControl) { FormControlAnchors.Add(anchor); }
        RawXml = "";
    }

    internal uint GetNextDrawingObjectId()
    {
        uint next = GetNextPictureId();
        foreach (XlsxDrawingRawAnchor anchor in RawAnchors)
        {
            if (anchor != null && anchor.HasObjectId && anchor.ObjectId >= next)
            {
                next = anchor.ObjectId + 1;
            }
        }
        return next;
    }

    internal static string GetDrawingObjectKindText(XlsxDrawingObjectKind objectKind)
    {
        if (objectKind == XlsxDrawingObjectKind.TextBox) { return "textBox"; }
        if (objectKind == XlsxDrawingObjectKind.Shape) { return "shape"; }
        if (objectKind == XlsxDrawingObjectKind.Connector) { return "connector"; }
        if (objectKind == XlsxDrawingObjectKind.FormControl) { return "formControl"; }
        if (objectKind == XlsxDrawingObjectKind.Diagram) { return "diagram"; }
        return "unknown";
    }

    internal static void AddObjectCellReferenceDiagnostics(XlsxDrawingObjectOperationResult result, XlsxA1CellReferenceResult reference)
    {
        foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal XlsxValidationResult SetDrawingObjectHyperlink(int anchorIndex, string relationshipId, string tooltip)
    {
        XlsxValidationResult result = ValidateDrawingObjectIndex(anchorIndex);
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseRelationshipId(relationshipId);
        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return result; }
        XlsxDrawingRawAnchor anchor = RawAnchors[anchorIndex];
        anchor.Hyperlink = new XlsxDrawingHyperlink();
        anchor.Hyperlink.RelationshipId = relationshipId;
        anchor.Hyperlink.Tooltip = XlsxCellValueConverter.NormalizeText(tooltip);
        anchor.HasHyperlink = true;
        anchor.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult SetDrawingObjectText(int anchorIndex, string text)
    {
        XlsxValidationResult result = ValidateDrawingObjectIndex(anchorIndex);
        if (!result.IsValid) { return result; }
        XlsxDrawingRawAnchor anchor = RawAnchors[anchorIndex];
        if (anchor.ObjectKind != XlsxDrawingObjectKind.Shape && anchor.ObjectKind != XlsxDrawingObjectKind.TextBox)
        {
            result.AddError("drawing.objectTextUnsupported", "Basic drawing text is supported only for shapes and text boxes.");
            return result;
        }
        anchor.Text = XlsxCellValueConverter.NormalizeText(text);
        anchor.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult ValidateDrawingObjectIndex(int anchorIndex)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (RawAnchors == null || anchorIndex < 0 || anchorIndex >= RawAnchors.Count)
        {
            result.AddError("drawing.objectAnchorRange", "Drawing object anchor index is outside the object anchor list.");
        }
        return result;
    }

    /// <summary>
    /// Returns next picture id from the current model.
    /// </summary>
    /// <returns>The uint value produced by the operation.</returns>
    internal uint GetNextPictureId()
    {
        uint next = 1;
        if (ImageAnchors == null)
        {
            return next;
        }

        foreach (XlsxDrawingImageAnchor anchor in ImageAnchors)
        {
            if (anchor != null && anchor.HasPictureId && anchor.PictureId >= next)
            {
                next = anchor.PictureId + 1;
            }
        }

        return next;
    }

    internal static void SetMarker(XlsxDrawingMarker marker, int row, int column)
    {
        marker.Row = (uint)(row - 1);
        marker.Column = (uint)(column - 1);
        marker.RowOffsetEmu = "0";
        marker.ColumnOffsetEmu = "0";
    }

    internal static void AddCellReferenceDiagnostics(XlsxDrawingImagePlacementResult result, XlsxA1CellReferenceResult reference)
    {
        foreach (XlsxDiagnostic diagnostic in reference.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal XlsxValidationResult SetImageHyperlink(int anchorIndex, string relationshipId, string tooltip)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (ImageAnchors == null || anchorIndex < 0 || anchorIndex >= ImageAnchors.Count)
        {
            result.AddError("drawing.hyperlinkAnchorRange", "Drawing image hyperlink anchor index is outside the image anchor list.");
            return result;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseRelationshipId(relationshipId);
        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (!result.IsValid)
        {
            return result;
        }
        XlsxDrawingImageAnchor anchor = ImageAnchors[anchorIndex];
        XlsxDrawingHyperlink hyperlink = new XlsxDrawingHyperlink();
        hyperlink.RelationshipId = relationshipId;
        hyperlink.Tooltip = XlsxCellValueConverter.NormalizeText(tooltip);
        anchor.Hyperlink = hyperlink;
        anchor.HasHyperlink = true;
        anchor.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult ClearImageHyperlink(int anchorIndex)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (ImageAnchors == null || anchorIndex < 0 || anchorIndex >= ImageAnchors.Count)
        {
            result.AddError("drawing.hyperlinkAnchorRange", "Drawing image hyperlink anchor index is outside the image anchor list.");
            return result;
        }
        XlsxDrawingImageAnchor anchor = ImageAnchors[anchorIndex];
        anchor.Hyperlink = new XlsxDrawingHyperlink();
        anchor.HasHyperlink = false;
        anchor.RawXml = "";
        RawXml = "";
        return result;
    }
}
