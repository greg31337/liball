using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of workbook sanitize operations.
/// </summary>
public class XlsxWorkbookSanitizeOptions
{
    /// <summary>
    /// Contains remove macros.
    /// </summary>
    public bool RemoveMacros;
    /// <summary>
    /// Contains remove external links.
    /// </summary>
    public bool RemoveExternalLinks;
    /// <summary>
    /// Contains remove comments.
    /// </summary>
    public bool RemoveComments;
    /// <summary>
    /// Contains remove hidden sheets.
    /// </summary>
    public bool RemoveHiddenSheets;
    /// <summary>
    /// Contains remove custom xml parts.
    /// </summary>
    public bool RemoveCustomXmlParts;
    /// <summary>
    /// Stores the remove personal metadata value used by this model.
    /// </summary>
    public bool RemovePersonalMetadata;
    /// <summary>
    /// Contains remove ole objects.
    /// </summary>
    public bool RemoveOleObjects;
    /// <summary>
    /// Contains remove unknown parts.
    /// </summary>
    public bool RemoveUnknownParts;
    /// <summary>
    /// Initializes a new workbook sanitize options with its default state.
    /// </summary>
    public XlsxWorkbookSanitizeOptions()
    {
        RemoveMacros = true;
        RemoveExternalLinks = true;
        RemoveComments = true;
        RemoveHiddenSheets = true;
        RemoveCustomXmlParts = true;
        RemovePersonalMetadata = true;
        RemoveOleObjects = true;
        RemoveUnknownParts = true;
    }
}
