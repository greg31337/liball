using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook view data used by the public workbook API.
/// </summary>
public class XlsxWorkbookView
{
    /// <summary>
    /// Stores the visibility value used by this model.
    /// </summary>
    public string? Visibility;
    /// <summary>
    /// Stores the active tab value used by this model.
    /// </summary>
    public uint ActiveTab;
    /// <summary>
    /// Indicates whether active tab is present and should be written.
    /// </summary>
    public bool HasActiveTab;
    /// <summary>
    /// Stores the first sheet value used by this model.
    /// </summary>
    public uint FirstSheet;
    /// <summary>
    /// Indicates whether first sheet is present and should be written.
    /// </summary>
    public bool HasFirstSheet;
    /// <summary>
    /// Stores the tab ratio value used by this model.
    /// </summary>
    public uint TabRatio;
    /// <summary>
    /// Indicates whether tab ratio is present and should be written.
    /// </summary>
    public bool HasTabRatio;
    /// <summary>
    /// Stores the window x value used by this model.
    /// </summary>
    public int WindowX;
    /// <summary>
    /// Indicates whether window x is present and should be written.
    /// </summary>
    public bool HasWindowX;
    /// <summary>
    /// Stores the window y value used by this model.
    /// </summary>
    public int WindowY;
    /// <summary>
    /// Indicates whether window y is present and should be written.
    /// </summary>
    public bool HasWindowY;
    /// <summary>
    /// Stores the window width value used by this model.
    /// </summary>
    public int WindowWidth;
    /// <summary>
    /// Indicates whether window width is present and should be written.
    /// </summary>
    public bool HasWindowWidth;
    /// <summary>
    /// Stores the window height value used by this model.
    /// </summary>
    public int WindowHeight;
    /// <summary>
    /// Indicates whether window height is present and should be written.
    /// </summary>
    public bool HasWindowHeight;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook view with its default state.
    /// </summary>
    public XlsxWorkbookView()
    {
        Visibility = "";
        ActiveTab = 0;
        HasActiveTab = false;
        FirstSheet = 0;
        HasFirstSheet = false;
        TabRatio = 0;
        HasTabRatio = false;
        WindowX = 0;
        HasWindowX = false;
        WindowY = 0;
        HasWindowY = false;
        WindowWidth = 0;
        HasWindowWidth = false;
        WindowHeight = 0;
        HasWindowHeight = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
