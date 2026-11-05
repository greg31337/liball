using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet hyperlink data used by the public workbook API.
/// </summary>
public class XlsxWorksheetHyperlink
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string? Reference;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string? RelationshipId;
    /// <summary>
    /// Stores the location value used by this model.
    /// </summary>
    public string? Location;
    /// <summary>
    /// Stores the target value used by this model.
    /// </summary>
    public string? Target;
    /// <summary>
    /// Stores the target mode value used by this model.
    /// </summary>
    public string? TargetMode;
    /// <summary>
    /// Indicates whether external.
    /// </summary>
    public bool IsExternal;
    /// <summary>
    /// Stores the target path value used by this model.
    /// </summary>
    public string TargetPath;
    /// <summary>
    /// Stores the target fragment value used by this model.
    /// </summary>
    public string TargetFragment;
    /// <summary>
    /// Stores the external target value used by this model.
    /// </summary>
    public string ExternalTarget;
    /// <summary>
    /// Indicates whether relationship is present and should be written.
    /// </summary>
    public bool HasRelationship;
    /// <summary>
    /// Stores the display value used by this model.
    /// </summary>
    public string? Display;
    /// <summary>
    /// Stores the tooltip value used by this model.
    /// </summary>
    public string? Tooltip;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet hyperlink with its default state.
    /// </summary>
    public XlsxWorksheetHyperlink()
    {
        Reference = "";
        RelationshipId = "";
        Location = "";
        Target = "";
        TargetMode = "";
        IsExternal = false;
        TargetPath = "";
        TargetFragment = "";
        ExternalTarget = "";
        HasRelationship = false;
        Display = "";
        Tooltip = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
