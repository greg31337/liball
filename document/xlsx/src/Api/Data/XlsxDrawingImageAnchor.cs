using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents drawing image anchor data used by the public workbook API.
/// </summary>
public class XlsxDrawingImageAnchor
{
    /// <summary>
    /// Stores the typed anchor kind.
    /// </summary>
    public XlsxDrawingAnchorKind AnchorKind;
    /// <summary>
    /// Stores the anchor type value used by this model.
    /// </summary>
    public string AnchorType;
    /// <summary>
    /// Contains edit as.
    /// </summary>
    public string EditAs;
    /// <summary>
    /// Stores the from value used by this model.
    /// </summary>
    public XlsxDrawingMarker From;
    /// <summary>
    /// Stores the to value used by this model.
    /// </summary>
    public XlsxDrawingMarker To;
    /// <summary>
    /// Stores the extent cx emu value used by this model.
    /// </summary>
    public string ExtentCxEmu;
    /// <summary>
    /// Stores the extent cy emu value used by this model.
    /// </summary>
    public string ExtentCyEmu;
    /// <summary>
    /// Indicates whether extent is present and should be written.
    /// </summary>
    public bool HasExtent;
    /// <summary>Stores the absolute horizontal position in English metric units.</summary>
    public string PositionXEmu;
    /// <summary>Stores the absolute vertical position in English metric units.</summary>
    public string PositionYEmu;
    /// <summary>
    /// Stores the picture id value used by this model.
    /// </summary>
    public uint PictureId;
    /// <summary>
    /// Indicates whether picture id is present and should be written.
    /// </summary>
    public bool HasPictureId;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the description value used by this model.
    /// </summary>
    public string Description;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the drawing hyperlink.
    /// </summary>
    public XlsxDrawingHyperlink Hyperlink;
    /// <summary>
    /// Indicates whether a drawing hyperlink is present and should be written.
    /// </summary>
    public bool HasHyperlink;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new drawing image anchor with its default state.
    /// </summary>
    public XlsxDrawingImageAnchor()
    {
        AnchorKind = XlsxDrawingAnchorKind.TwoCell;
        AnchorType = "twoCell";
        EditAs = "";
        From = new XlsxDrawingMarker();
        To = new XlsxDrawingMarker();
        ExtentCxEmu = "";
        ExtentCyEmu = "";
        HasExtent = false;
        PositionXEmu = "0";
        PositionYEmu = "0";
        PictureId = 0;
        HasPictureId = false;
        Name = "";
        Description = "";
        RelationshipId = "";
        Hyperlink = new XlsxDrawingHyperlink();
        HasHyperlink = false;
        RawXml = "";
        UnknownXml = new List<string>();
    }
}
