using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents slicer timeline item data used by the public workbook API.
/// </summary>
public class XlsxSlicerTimelineItem
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the cache name value used by this model.
    /// </summary>
    public string CacheName;
    /// <summary>Stores the displayed caption.</summary>
    public string Caption;
    /// <summary>Stores the timeline or slicer style name.</summary>
    public string StyleName;
    /// <summary>Stores the timeline display level.</summary>
    public uint Level;
    /// <summary>Indicates whether the display level is present.</summary>
    public bool HasLevel;
    /// <summary>Stores the timeline selection level.</summary>
    public uint SelectionLevel;
    /// <summary>Indicates whether the selection level is present.</summary>
    public bool HasSelectionLevel;
    /// <summary>Stores the timeline scroll position.</summary>
    public uint ScrollPosition;
    /// <summary>Indicates whether the scroll position is present.</summary>
    public bool HasScrollPosition;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new slicer timeline item with its default state.
    /// </summary>
    public XlsxSlicerTimelineItem()
    {
        Name = "";
        CacheName = "";
        Caption = "";
        StyleName = "";
        Level = 0;
        HasLevel = false;
        SelectionLevel = 0;
        HasSelectionLevel = false;
        ScrollPosition = 0;
        HasScrollPosition = false;
        RawXml = "";
    }
}
