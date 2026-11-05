using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet pivot table reference data used by the public workbook API.
/// </summary>
public class XlsxWorksheetPivotTableReference
{
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the relationship value used by this model.
    /// </summary>
    public XlsxRelationship? Relationship;
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
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet pivot table reference with its default state.
    /// </summary>
    public XlsxWorksheetPivotTableReference()
    {
        RelationshipId = "";
        Relationship = null;
        HasRelationship = false;
        Target = "";
        TargetPath = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
