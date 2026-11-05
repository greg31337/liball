using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents drawing raw anchor data used by the public workbook API.
/// </summary>
public class XlsxDrawingRawAnchor
{
    /// <summary>
    /// Stores the typed anchor kind.
    /// </summary>
    public XlsxDrawingAnchorKind AnchorKind;
    /// <summary>
    /// Stores the typed drawing payload kind.
    /// </summary>
    public XlsxDrawingObjectKind ObjectKind;
    /// <summary>
    /// Stores the anchor type value used by this model.
    /// </summary>
    public string AnchorType;
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public string Kind;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>Stores the object identifier.</summary>
    public uint ObjectId;
    /// <summary>Indicates whether an object identifier is present.</summary>
    public bool HasObjectId;
    /// <summary>Stores the object description.</summary>
    public string Description;
    /// <summary>Stores the first cell marker.</summary>
    public XlsxDrawingMarker From;
    /// <summary>Stores the second cell marker.</summary>
    public XlsxDrawingMarker To;
    /// <summary>Stores the absolute horizontal position in English metric units.</summary>
    public string PositionXEmu;
    /// <summary>Stores the absolute vertical position in English metric units.</summary>
    public string PositionYEmu;
    /// <summary>Stores the horizontal extent in English metric units.</summary>
    public string ExtentCxEmu;
    /// <summary>Stores the vertical extent in English metric units.</summary>
    public string ExtentCyEmu;
    /// <summary>Indicates whether an explicit extent is present.</summary>
    public bool HasExtent;
    /// <summary>Stores the two-cell edit behavior.</summary>
    public string EditAs;
    /// <summary>Stores the preset geometry name for a shape or connector.</summary>
    public string ShapeType;
    /// <summary>Stores basic unformatted shape or text-box text.</summary>
    public string Text;
    /// <summary>
    /// Stores the drawing hyperlink.
    /// </summary>
    public XlsxDrawingHyperlink Hyperlink;
    /// <summary>
    /// Indicates whether a drawing hyperlink is present.
    /// </summary>
    public bool HasHyperlink;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new drawing raw anchor with its default state.
    /// </summary>
    public XlsxDrawingRawAnchor()
    {
        AnchorKind = XlsxDrawingAnchorKind.TwoCell;
        ObjectKind = XlsxDrawingObjectKind.Unknown;
        AnchorType = "twoCell";
        Kind = "unknown";
        Name = "";
        RelationshipId = "";
        ObjectId = 0;
        HasObjectId = false;
        Description = "";
        From = new XlsxDrawingMarker();
        To = new XlsxDrawingMarker();
        PositionXEmu = "0";
        PositionYEmu = "0";
        ExtentCxEmu = "";
        ExtentCyEmu = "";
        HasExtent = false;
        EditAs = "";
        ShapeType = "rect";
        Text = "";
        Hyperlink = new XlsxDrawingHyperlink();
        HasHyperlink = false;
        RawXml = "";
    }
}
