using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents markup compatibility attributes data used by the public workbook API.
/// </summary>
public class XlsxMarkupCompatibilityAttributes
{
    /// <summary>
    /// Stores the ignorable value used by this model.
    /// </summary>
    public string Ignorable;
    /// <summary>
    /// Stores the process content value used by this model.
    /// </summary>
    public string ProcessContent;
    /// <summary>
    /// Contains preserve elements.
    /// </summary>
    public string PreserveElements;
    /// <summary>
    /// Contains preserve attributes.
    /// </summary>
    public string PreserveAttributes;
    /// <summary>
    /// Stores the must understand value used by this model.
    /// </summary>
    public string MustUnderstand;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    internal List<XlsxXmlNamespace> NamespaceDeclarations;
    /// <summary>
    /// Initializes a new markup compatibility attributes with its default state.
    /// </summary>
    public XlsxMarkupCompatibilityAttributes()
    {
        Ignorable = "";
        ProcessContent = "";
        PreserveElements = "";
        PreserveAttributes = "";
        MustUnderstand = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        NamespaceDeclarations = new List<XlsxXmlNamespace>();
    }

    /// <summary>
    /// Performs the has values operation using the current model state.
    /// </summary>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasValues()
    {
        if (Ignorable.Length > 0 || ProcessContent.Length > 0 || PreserveElements.Length > 0 || PreserveAttributes.Length > 0 || MustUnderstand.Length > 0)
        {
            return true;
        }

        if (UnknownAttributes.Count > 0 || NamespaceDeclarations.Count > 0)
        {
            return true;
        }

        return false;
    }
}
