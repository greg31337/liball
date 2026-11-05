using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML custom properties part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxCustomPropertiesPart
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
    /// Contains properties.
    /// </summary>
    public List<XlsxCustomProperty> Properties;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new custom properties part with its default state.
    /// </summary>
    public XlsxCustomPropertiesPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Properties = new List<XlsxCustomProperty>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns property from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>The custom property produced by the operation.</returns>
    internal XlsxCustomProperty? GetProperty(string name)
    {
        foreach (XlsxCustomProperty property in Properties)
        {
            if (property.Name == name)
            {
                return property;
            }
        }

        return null;
    }
}
