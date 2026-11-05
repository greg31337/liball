using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML vba project part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxVbaProjectPart
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
    /// Initializes a new vba project part with its default state.
    /// </summary>
    public XlsxVbaProjectPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.VbaProject;
        ContentType = XlsxContentTypes.VbaProject;
        Data = new byte[0];
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
