using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents a hyperlink attached to drawing non-visual properties.
/// </summary>
public class XlsxDrawingHyperlink
{
    /// <summary>Stores the hyperlink relationship identifier.</summary>
    public string RelationshipId;
    /// <summary>Stores the hyperlink tooltip.</summary>
    public string Tooltip;
    /// <summary>Stores the hyperlink action.</summary>
    public string Action;
    /// <summary>Stores the target frame.</summary>
    public string TargetFrame;
    /// <summary>Stores an invalid URL preserved by the producer.</summary>
    public string InvalidUrl;
    /// <summary>Stores whether hyperlink history is retained.</summary>
    public bool History;
    /// <summary>Indicates whether history is present.</summary>
    public bool HasHistory;
    /// <summary>Stores whether the hyperlink is highlighted after activation.</summary>
    public bool HighlightClick;
    /// <summary>Indicates whether highlight click is present.</summary>
    public bool HasHighlightClick;
    /// <summary>Stores whether sound ends when the hyperlink is activated.</summary>
    public bool EndSound;
    /// <summary>Indicates whether end sound is present.</summary>
    public bool HasEndSound;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes a drawing hyperlink.</summary>
    public XlsxDrawingHyperlink()
    {
        RelationshipId = "";
        Tooltip = "";
        Action = "";
        TargetFrame = "";
        InvalidUrl = "";
        History = false;
        HasHistory = false;
        HighlightClick = false;
        HasHighlightClick = false;
        EndSound = false;
        HasEndSound = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
