using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook sheet data used by the public workbook API.
/// </summary>
public class XlsxWorkbookSheet
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string? Name;
    /// <summary>
    /// Stores the sheet id value used by this model.
    /// </summary>
    public uint SheetId;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string? RelationshipId;
    /// <summary>
    /// Stores the state value used by this model.
    /// </summary>
    public string? State;
    /// <summary>
    /// Stores the sheet type value used by this model.
    /// </summary>
    public XlsxSheetType SheetType;
    /// <summary>
    /// Stores the relationship type value used by this model.
    /// </summary>
    public string? RelationshipType;
    /// <summary>
    /// Stores the target path value used by this model.
    /// </summary>
    public string TargetPath;
    /// <summary>
    /// Stores the content type value used by this model.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Indicates whether relationship metadata is present and should be written.
    /// </summary>
    public bool HasRelationshipMetadata;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook sheet with its default state.
    /// </summary>
    public XlsxWorkbookSheet()
    {
        Name = "";
        SheetId = 0;
        RelationshipId = "";
        State = "";
        SheetType = XlsxSheetType.Worksheet;
        RelationshipType = "";
        TargetPath = "";
        ContentType = "";
        HasRelationshipMetadata = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
