using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet view data used by the public workbook API.
/// </summary>
public class XlsxWorksheetView
{
    /// <summary>
    /// Stores the workbook view id value used by this model.
    /// </summary>
    public uint WorkbookViewId;
    /// <summary>
    /// Indicates whether workbook view id is present and should be written.
    /// </summary>
    public bool HasWorkbookViewId;
    /// <summary>
    /// Stores the view value used by this model.
    /// </summary>
    public string? View;
    /// <summary>
    /// Contains show grid lines.
    /// </summary>
    public bool ShowGridLines;
    /// <summary>
    /// Indicates whether show grid lines is present and should be written.
    /// </summary>
    public bool HasShowGridLines;
    /// <summary>
    /// Contains show row col headers.
    /// </summary>
    public bool ShowRowColHeaders;
    /// <summary>
    /// Indicates whether show row col headers is present and should be written.
    /// </summary>
    public bool HasShowRowColHeaders;
    /// <summary>
    /// Stores the right to left value used by this model.
    /// </summary>
    public bool RightToLeft;
    /// <summary>
    /// Indicates whether right to left is present and should be written.
    /// </summary>
    public bool HasRightToLeft;
    /// <summary>
    /// Stores the tab selected value used by this model.
    /// </summary>
    public bool TabSelected;
    /// <summary>
    /// Indicates whether tab selected is present and should be written.
    /// </summary>
    public bool HasTabSelected;
    /// <summary>
    /// Stores the zoom scale value used by this model.
    /// </summary>
    public uint ZoomScale;
    /// <summary>
    /// Indicates whether zoom scale is present and should be written.
    /// </summary>
    public bool HasZoomScale;
    /// <summary>
    /// Stores the pane value used by this model.
    /// </summary>
    public XlsxWorksheetPane Pane;
    /// <summary>
    /// Indicates whether pane is present and should be written.
    /// </summary>
    public bool HasPane;
    /// <summary>
    /// Contains selections.
    /// </summary>
    public List<XlsxWorksheetSelection> Selections;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet view with its default state.
    /// </summary>
    public XlsxWorksheetView()
    {
        WorkbookViewId = 0;
        HasWorkbookViewId = false;
        View = "";
        ShowGridLines = false;
        HasShowGridLines = false;
        ShowRowColHeaders = false;
        HasShowRowColHeaders = false;
        RightToLeft = false;
        HasRightToLeft = false;
        TabSelected = false;
        HasTabSelected = false;
        ZoomScale = 0;
        HasZoomScale = false;
        Pane = new XlsxWorksheetPane();
        HasPane = false;
        Selections = new List<XlsxWorksheetSelection>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
