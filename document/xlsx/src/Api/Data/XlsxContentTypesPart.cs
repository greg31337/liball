using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML content types part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxContentTypesPart
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
    /// Contains defaults.
    /// </summary>
    public List<XlsxContentTypeDefault> Defaults;
    /// <summary>
    /// Contains overrides.
    /// </summary>
    public List<XlsxContentTypeOverride> Overrides;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Initializes a new content types part with its default state.
    /// </summary>
    public XlsxContentTypesPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Defaults = new List<XlsxContentTypeDefault>();
        Overrides = new List<XlsxContentTypeOverride>();
        UnknownXml = new List<string>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns default from the current model.
    /// </summary>
    /// <param name = "extension">The extension value used by the operation.</param>
    /// <returns>The content type default produced by the operation.</returns>
    internal XlsxContentTypeDefault? GetDefault(string extension)
    {
        string normalizedExtension = extension;
        if (normalizedExtension != null && normalizedExtension.StartsWith("."))
        {
            normalizedExtension = normalizedExtension.Substring(1);
        }

        foreach (XlsxContentTypeDefault item in Defaults)
        {
            if (string.Equals(item.Extension, normalizedExtension, System.StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns override from the current model.
    /// </summary>
    /// <param name = "partName">The part name value used by the operation.</param>
    /// <returns>The content type override produced by the operation.</returns>
    internal XlsxContentTypeOverride? GetOverride(string partName)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partName);
        if (!normalized.Validation.IsValid)
        {
            return null;
        }

        foreach (XlsxContentTypeOverride item in Overrides)
        {
            if (item.PartName == normalized.Path)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns content type from the current model.
    /// </summary>
    /// <param name = "partName">The part name value used by the operation.</param>
    /// <returns>The text produced by the operation.</returns>
    internal string? GetContentType(string partName)
    {
        XlsxContentTypeOverride? overrideItem = GetOverride(partName);
        if (overrideItem != null)
        {
            return overrideItem.ContentType;
        }

        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partName);
        if (!normalized.Validation.IsValid)
        {
            return "";
        }

        int dotIndex = normalized.Path.LastIndexOf('.');
        if (dotIndex < 0 || dotIndex == normalized.Path.Length - 1)
        {
            return "";
        }

        string extension = normalized.Path.Substring(dotIndex + 1);
        XlsxContentTypeDefault? defaultItem = GetDefault(extension);
        if (defaultItem == null)
        {
            return "";
        }

        return defaultItem.ContentType;
    }
}
