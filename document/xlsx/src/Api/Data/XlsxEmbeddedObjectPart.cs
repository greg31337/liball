using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML embedded object part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxEmbeddedObjectPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the content type value used by this model.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the data value used by this model.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Indicates whether ole object.
    /// </summary>
    public bool IsOleObject;
    /// <summary>
    /// Indicates whether embedded package.
    /// </summary>
    public bool IsEmbeddedPackage;
    /// <summary>Stores the source relationship identifier.</summary>
    public string RelationshipId;
    /// <summary>Stores the source part path that owns the relationship.</summary>
    public string SourcePartPath;
    /// <summary>Stores the OLE program identifier.</summary>
    public string ProgramId;
    /// <summary>Stores the worksheet shape identifier.</summary>
    public uint ShapeId;
    /// <summary>
    /// Initializes a new embedded object part with its default state.
    /// </summary>
    public XlsxEmbeddedObjectPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "";
        ContentType = "";
        Data = new byte[0];
        IsOleObject = false;
        IsEmbeddedPackage = false;
        RelationshipId = "";
        SourcePartPath = "";
        ProgramId = "";
        ShapeId = 0;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
