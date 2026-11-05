using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML image part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxImagePart
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
    /// Stores the extension value used by this model.
    /// </summary>
    public string Extension;
    /// <summary>
    /// Stores the format value used by this model.
    /// </summary>
    public string Format;
    /// <summary>
    /// Stores the data value used by this model.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Initializes a new image part with its default state.
    /// </summary>
    public XlsxImagePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "";
        ContentType = "";
        Extension = "";
        Format = "";
        Data = new byte[0];
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
