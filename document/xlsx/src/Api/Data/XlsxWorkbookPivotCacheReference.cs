using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook pivot cache reference data used by the public workbook API.
/// </summary>
public class XlsxWorkbookPivotCacheReference
{
    /// <summary>
    /// Stores the cache id value used by this model.
    /// </summary>
    public uint CacheId;
    /// <summary>
    /// Indicates whether cache id is present and should be written.
    /// </summary>
    public bool HasCacheId;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the target value used by this model.
    /// </summary>
    public string? Target;
    /// <summary>
    /// Stores the target path value used by this model.
    /// </summary>
    public string TargetPath;
    /// <summary>
    /// Indicates whether relationship is present and should be written.
    /// </summary>
    public bool HasRelationship;
    /// <summary>
    /// Stores the relationship value used by this model.
    /// </summary>
    public XlsxRelationship? Relationship;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook pivot cache reference with its default state.
    /// </summary>
    public XlsxWorkbookPivotCacheReference()
    {
        CacheId = 0;
        HasCacheId = false;
        RelationshipId = "";
        Target = "";
        TargetPath = "";
        HasRelationship = false;
        Relationship = null;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
