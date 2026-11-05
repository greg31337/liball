using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents phonetic text data used by the public workbook API.
/// </summary>
public class XlsxPhoneticText
{
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the start base index value used by this model.
    /// </summary>
    public uint StartBaseIndex;
    /// <summary>
    /// Indicates whether start base index is present and should be written.
    /// </summary>
    public bool HasStartBaseIndex;
    /// <summary>
    /// Stores the end base index value used by this model.
    /// </summary>
    public uint EndBaseIndex;
    /// <summary>
    /// Indicates whether end base index is present and should be written.
    /// </summary>
    public bool HasEndBaseIndex;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new phonetic text with its default state.
    /// </summary>
    public XlsxPhoneticText()
    {
        Text = "";
        StartBaseIndex = 0;
        HasStartBaseIndex = false;
        EndBaseIndex = 0;
        HasEndBaseIndex = false;
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
