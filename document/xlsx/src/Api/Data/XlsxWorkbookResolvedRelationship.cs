using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook resolved relationship data used by the public workbook API.
/// </summary>
public class XlsxWorkbookResolvedRelationship
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
    /// Stores the kind value used by this model.
    /// </summary>
    public XlsxWorkbookRelationshipKind Kind;
    /// <summary>
    /// Stores the target value used by this model.
    /// </summary>
    public string? Target;
    /// <summary>
    /// Stores the target path value used by this model.
    /// </summary>
    public string TargetPath;
    /// <summary>
    /// Stores the target fragment value used by this model.
    /// </summary>
    public string TargetFragment;
    /// <summary>
    /// Indicates whether external.
    /// </summary>
    public bool IsExternal;
    /// <summary>
    /// Stores the external target value used by this model.
    /// </summary>
    public string ExternalTarget;
    /// <summary>
    /// Stores the relationship value used by this model.
    /// </summary>
    public XlsxRelationship? Relationship;
    /// <summary>
    /// Stores the sheet value used by this model.
    /// </summary>
    public XlsxWorkbookSheet? Sheet;
    /// <summary>
    /// Indicates whether sheet is present and should be written.
    /// </summary>
    public bool HasSheet;
    /// <summary>
    /// Initializes a new workbook resolved relationship with its default state.
    /// </summary>
    public XlsxWorkbookResolvedRelationship()
    {
        Id = "";
        Type = "";
        Kind = XlsxWorkbookRelationshipKind.Unknown;
        Target = "";
        TargetPath = "";
        TargetFragment = "";
        IsExternal = false;
        ExternalTarget = "";
        Relationship = null;
        Sheet = null;
        HasSheet = false;
    }
}
