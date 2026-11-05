using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML core properties part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxCorePropertiesPart
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
    /// Stores the title value used by this model.
    /// </summary>
    public string Title;
    /// <summary>
    /// Stores the subject value used by this model.
    /// </summary>
    public string Subject;
    /// <summary>
    /// Stores the creator value used by this model.
    /// </summary>
    public string Creator;
    /// <summary>
    /// Contains keywords.
    /// </summary>
    public string Keywords;
    /// <summary>
    /// Stores the description value used by this model.
    /// </summary>
    public string Description;
    /// <summary>
    /// Stores the last modified by value used by this model.
    /// </summary>
    public string LastModifiedBy;
    /// <summary>
    /// Stores the revision value used by this model.
    /// </summary>
    public string Revision;
    /// <summary>
    /// Stores the category value used by this model.
    /// </summary>
    public string Category;
    /// <summary>
    /// Contains content status.
    /// </summary>
    public string ContentStatus;
    /// <summary>
    /// Stores the created value used by this model.
    /// </summary>
    public System.DateTime Created;
    /// <summary>
    /// Indicates whether created is present and should be written.
    /// </summary>
    public bool HasCreated;
    /// <summary>
    /// Stores the modified value used by this model.
    /// </summary>
    public System.DateTime Modified;
    /// <summary>
    /// Indicates whether modified is present and should be written.
    /// </summary>
    public bool HasModified;
    /// <summary>
    /// Stores the language value used by this model.
    /// </summary>
    public string Language;
    /// <summary>
    /// Stores the identifier value used by this model.
    /// </summary>
    public string Identifier;
    /// <summary>
    /// Stores the version value used by this model.
    /// </summary>
    public string Version;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new core properties part with its default state.
    /// </summary>
    public XlsxCorePropertiesPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Title = "";
        Subject = "";
        Creator = "";
        Keywords = "";
        Description = "";
        LastModifiedBy = "";
        Revision = "";
        Category = "";
        ContentStatus = "";
        Created = default(System.DateTime);
        HasCreated = false;
        Modified = default(System.DateTime);
        HasModified = false;
        Language = "";
        Identifier = "";
        Version = "";
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
