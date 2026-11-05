using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents relationship data used by the public workbook API.
/// </summary>
public class XlsxRelationship
{
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public string? Id;
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string? Type;
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
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new relationship with its default state.
    /// </summary>
    public XlsxRelationship()
    {
        Id = "";
        Type = "";
        Target = "";
        TargetMode = "";
        IsExternal = false;
        TargetPath = "";
        TargetFragment = "";
        ExternalTarget = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
