using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents drawing chart anchor data used by the public workbook API.
/// </summary>
public class XlsxDrawingChartAnchor
{
    /// <summary>
    /// Stores the anchor type value used by this model.
    /// </summary>
    public string AnchorType;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Indicates whether relationship is present and should be written.
    /// </summary>
    public bool HasRelationship;
    /// <summary>
    /// Stores the target value used by this model.
    /// </summary>
    public string? Target;
    /// <summary>
    /// Stores the target path value used by this model.
    /// </summary>
    public string TargetPath;
    /// <summary>
    /// Stores the relationship value used by this model.
    /// </summary>
    public XlsxRelationship? Relationship;
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
    /// Initializes a new drawing chart anchor with its default state.
    /// </summary>
    public XlsxDrawingChartAnchor()
    {
        AnchorType = "twoCell";
        Name = "";
        RelationshipId = "";
        HasRelationship = false;
        Target = "";
        TargetPath = "";
        Relationship = null;
        Hyperlink = new XlsxDrawingHyperlink();
        HasHyperlink = false;
        RawXml = "";
    }
}
