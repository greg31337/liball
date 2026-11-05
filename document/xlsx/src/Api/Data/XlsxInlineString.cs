using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents inline string data used by the public workbook API.
/// </summary>
public class XlsxInlineString
{
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Indicates whether rich text.
    /// </summary>
    public bool IsRichText;
    /// <summary>
    /// Contains rich text runs.
    /// </summary>
    public List<XlsxSharedStringRichTextRun> RichTextRuns;
    /// <summary>
    /// Contains phonetic texts.
    /// </summary>
    public List<XlsxPhoneticText> PhoneticTexts;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string PhoneticPropertiesRawXml;
    /// <summary>
    /// Stores typed phonetic display properties.
    /// </summary>
    public XlsxPhoneticProperties PhoneticProperties;
    /// <summary>
    /// Indicates whether phonetic properties is present and should be written.
    /// </summary>
    public bool HasPhoneticProperties;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new inline string with its default state.
    /// </summary>
    public XlsxInlineString()
    {
        Text = "";
        IsRichText = false;
        RichTextRuns = new List<XlsxSharedStringRichTextRun>();
        PhoneticTexts = new List<XlsxPhoneticText>();
        PhoneticPropertiesRawXml = "";
        PhoneticProperties = new XlsxPhoneticProperties();
        HasPhoneticProperties = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    /// <summary>
    /// Creates text and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>The inline string produced by the operation.</returns>
    internal static XlsxInlineString CreateText(string text)
    {
        XlsxInlineString value = new XlsxInlineString();
        value.Text = XlsxCellValueConverter.NormalizeText(text);
        return value;
    }
}
