using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents the typed source of a pivot cache.
/// </summary>
public class XlsxPivotCacheSource
{
    /// <summary>Stores the OOXML cache source type token.</summary>
    public string Type;
    /// <summary>Stores the external connection identifier.</summary>
    public uint ConnectionId;
    /// <summary>Indicates whether the connection identifier is present.</summary>
    public bool HasConnectionId;
    /// <summary>Stores worksheet source details.</summary>
    public XlsxPivotCacheWorksheetSource WorksheetSource;
    /// <summary>Indicates whether worksheet source details are present.</summary>
    public bool HasWorksheetSource;
    /// <summary>Stores preserved consolidation or external source XML.</summary>
    public string SourceRawXml;
    /// <summary>Contains unknown source attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;

    /// <summary>Initializes an empty pivot-cache source.</summary>
    public XlsxPivotCacheSource()
    {
        Type = "";
        ConnectionId = 0;
        HasConnectionId = false;
        WorksheetSource = new XlsxPivotCacheWorksheetSource();
        HasWorksheetSource = false;
        SourceRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
