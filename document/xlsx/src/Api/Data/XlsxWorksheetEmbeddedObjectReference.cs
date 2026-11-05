using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Represents a worksheet reference to an OLE object or embedded package.
/// </summary>
public class XlsxWorksheetEmbeddedObjectReference
{
    /// <summary>Stores the relationship identifier.</summary>
    public string RelationshipId;
    /// <summary>Stores the OLE program identifier.</summary>
    public string ProgramId;
    /// <summary>Stores the worksheet shape identifier.</summary>
    public uint ShapeId;
    /// <summary>Indicates whether a shape identifier is present.</summary>
    public bool HasShapeId;
    /// <summary>Indicates whether the target is an OLE compound object.</summary>
    public bool IsOleObject;
    /// <summary>Preserves the complete source element when it contains unsupported attributes or children.</summary>
    public string RawXml;

    /// <summary>Initializes an empty embedded-object reference.</summary>
    public XlsxWorksheetEmbeddedObjectReference()
    {
        RelationshipId = "";
        ProgramId = "Package";
        ShapeId = 0;
        HasShapeId = false;
        IsOleObject = false;
        RawXml = "";
    }
}
