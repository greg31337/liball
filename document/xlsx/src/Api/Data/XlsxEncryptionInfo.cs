using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents encryption info data used by the public workbook API.
/// </summary>
public class XlsxEncryptionInfo
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
    /// Stores the major version value used by this model.
    /// </summary>
    public ushort MajorVersion;
    /// <summary>
    /// Stores the minor version value used by this model.
    /// </summary>
    public ushort MinorVersion;
    /// <summary>
    /// Stores the encryption kind value used by this model.
    /// </summary>
    public string EncryptionKind;
    /// <summary>
    /// Indicates whether agile.
    /// </summary>
    public bool IsAgile;
    /// <summary>
    /// Indicates whether standard.
    /// </summary>
    public bool IsStandard;
    /// <summary>
    /// Identifies the parsed encryption format.
    /// </summary>
    public XlsxPackageEncryptionKind Kind;
    /// <summary>
    /// Identifies the parsed AES algorithm and key size, or unknown when unsupported.
    /// </summary>
    public XlsxPackageEncryptionAlgorithm Algorithm;
    /// <summary>
    /// Stores the encryption key size in bits.
    /// </summary>
    public int KeyBits;
    /// <summary>
    /// Stores the password-hash iteration count.
    /// </summary>
    public int SpinCount;
    /// <summary>
    /// Indicates whether the declared cipher, chaining, hash, and key size are supported.
    /// </summary>
    public bool IsSupported;
    /// <summary>
    /// Initializes a new encryption info with its default state.
    /// </summary>
    public XlsxEncryptionInfo()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        MajorVersion = 0;
        MinorVersion = 0;
        EncryptionKind = "";
        IsAgile = false;
        IsStandard = false;
        Kind = XlsxPackageEncryptionKind.Unknown;
        Algorithm = XlsxPackageEncryptionAlgorithm.Unknown;
        KeyBits = 0;
        SpinCount = 0;
        IsSupported = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
