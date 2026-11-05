using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML theme part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxThemePart
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
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string? Name;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the color scheme value used by this model.
    /// </summary>
    public XlsxThemeColorScheme ColorScheme;
    /// <summary>
    /// Indicates whether color scheme is present and should be written.
    /// </summary>
    public bool HasColorScheme;
    /// <summary>
    /// Stores the font scheme value used by this model.
    /// </summary>
    public XlsxThemeFontScheme FontScheme;
    /// <summary>
    /// Indicates whether font scheme is present and should be written.
    /// </summary>
    public bool HasFontScheme;
    /// <summary>
    /// Stores the format scheme value used by this model.
    /// </summary>
    public XlsxThemeFormatScheme FormatScheme;
    /// <summary>
    /// Indicates whether format scheme is present and should be written.
    /// </summary>
    public bool HasFormatScheme;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new theme part with its default state.
    /// </summary>
    public XlsxThemePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.Theme;
        NamespaceUri = XlsxNamespaces.Drawing;
        Name = "";
        RawXml = "";
        ColorScheme = new XlsxThemeColorScheme();
        HasColorScheme = false;
        FontScheme = new XlsxThemeFontScheme();
        HasFontScheme = false;
        FormatScheme = new XlsxThemeFormatScheme();
        HasFormatScheme = false;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
