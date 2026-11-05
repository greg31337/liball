using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Provides the canonical OOXML relationship type identifiers used when inspecting or composing spreadsheet packages.
/// </summary>
public static class XlsxRelationshipTypes
{
    /// <summary>
    /// Identifies the package thumbnail used for document preview metadata.
    /// </summary>
    public const string Thumbnail = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail";
    /// <summary>
    /// Identifies the office document OOXML value.
    /// </summary>
    public const string OfficeDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    /// <summary>
    /// Identifies the office document OOXML value.
    /// </summary>
    public const string Workbook = OfficeDocument;
    /// <summary>
    /// Identifies the worksheet OOXML value.
    /// </summary>
    public const string Worksheet = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
    /// <summary>
    /// Identifies the chartsheet OOXML value.
    /// </summary>
    public const string Chartsheet = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chartsheet";
    /// <summary>
    /// Identifies the dialogsheet OOXML value.
    /// </summary>
    public const string Dialogsheet = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/dialogsheet";
    /// <summary>
    /// Identifies the macrosheet OOXML value.
    /// </summary>
    public const string Macrosheet = "http://schemas.microsoft.com/office/2006/relationships/xlMacrosheet";
    /// <summary>
    /// Identifies the styles OOXML value.
    /// </summary>
    public const string Styles = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";
    /// <summary>
    /// Identifies the shared strings OOXML value.
    /// </summary>
    public const string SharedStrings = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings";
    /// <summary>
    /// Identifies the calculation chain OOXML value.
    /// </summary>
    public const string CalculationChain = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/calcChain";
    /// <summary>
    /// Identifies the theme OOXML value.
    /// </summary>
    public const string Theme = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme";
    /// <summary>
    /// Identifies the external link OOXML value.
    /// </summary>
    public const string ExternalLink = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLink";
    /// <summary>
    /// Identifies the external link path OOXML value.
    /// </summary>
    public const string ExternalLinkPath = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLinkPath";
    /// <summary>
    /// Identifies the connections OOXML value.
    /// </summary>
    public const string Connections = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/connections";
    /// <summary>
    /// Identifies the query table OOXML value.
    /// </summary>
    public const string QueryTable = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/queryTable";
    /// <summary>
    /// Identifies the pivot cache definition OOXML value.
    /// </summary>
    public const string PivotCacheDefinition = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/pivotCacheDefinition";
    /// <summary>
    /// Identifies the pivot cache records OOXML value.
    /// </summary>
    public const string PivotCacheRecords = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/pivotCacheRecords";
    /// <summary>
    /// Identifies the pivot table OOXML value.
    /// </summary>
    public const string PivotTable = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/pivotTable";
    /// <summary>
    /// Identifies the slicer OOXML value.
    /// </summary>
    public const string Slicer = "http://schemas.microsoft.com/office/2007/relationships/slicer";
    /// <summary>
    /// Identifies the slicer cache OOXML value.
    /// </summary>
    public const string SlicerCache = "http://schemas.microsoft.com/office/2007/relationships/slicerCache";
    /// <summary>
    /// Identifies the timeline OOXML value.
    /// </summary>
    public const string Timeline = "http://schemas.microsoft.com/office/2011/relationships/timeline";
    /// <summary>
    /// Identifies the timeline cache OOXML value.
    /// </summary>
    public const string TimelineCache = "http://schemas.microsoft.com/office/2011/relationships/timelineCache";
    /// <summary>
    /// Identifies the core properties OOXML value.
    /// </summary>
    public const string CoreProperties = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
    /// <summary>
    /// Identifies the digital signature origin OOXML value.
    /// </summary>
    public const string DigitalSignatureOrigin = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin";
    /// <summary>
    /// Identifies the digital signature OOXML value.
    /// </summary>
    public const string DigitalSignature = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/signature";
    /// <summary>
    /// Identifies the extended properties OOXML value.
    /// </summary>
    public const string ExtendedProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties";
    /// <summary>
    /// Identifies the custom properties OOXML value.
    /// </summary>
    public const string CustomProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/custom-properties";
    /// <summary>
    /// Identifies the hyperlink OOXML value.
    /// </summary>
    public const string Hyperlink = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";
    /// <summary>
    /// Identifies the drawing OOXML value.
    /// </summary>
    public const string Drawing = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing";
    /// <summary>
    /// Identifies the image OOXML value.
    /// </summary>
    public const string Image = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
    /// <summary>
    /// Identifies the chart OOXML value.
    /// </summary>
    public const string Chart = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart";
    /// <summary>
    /// Identifies the ole object OOXML value.
    /// </summary>
    public const string OleObject = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/oleObject";
    /// <summary>
    /// Identifies the embedded package OOXML value.
    /// </summary>
    public const string EmbeddedPackage = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/package";
    /// <summary>
    /// Identifies the table OOXML value.
    /// </summary>
    public const string Table = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/table";
    /// <summary>
    /// Identifies the comments OOXML value.
    /// </summary>
    public const string Comments = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments";
    /// <summary>
    /// Identifies the threaded comments OOXML value.
    /// </summary>
    public const string ThreadedComments = "http://schemas.microsoft.com/office/2017/10/relationships/threadedComment";
    /// <summary>
    /// Identifies the vml drawing OOXML value.
    /// </summary>
    public const string VmlDrawing = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/vmlDrawing";
    /// <summary>
    /// Identifies the printer settings OOXML value.
    /// </summary>
    public const string PrinterSettings = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/printerSettings";
    /// <summary>
    /// Identifies the custom xml OOXML value.
    /// </summary>
    public const string CustomXml = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml";
    /// <summary>
    /// Identifies the custom xml properties OOXML value.
    /// </summary>
    public const string CustomXmlProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXmlProps";
    /// <summary>
    /// Identifies the vba project OOXML value.
    /// </summary>
    public const string VbaProject = "http://schemas.microsoft.com/office/2006/relationships/vbaProject";
    /// <summary>Identifies a form-control properties relationship.</summary>
    public const string ControlProperties = "http://schemas.microsoft.com/office/2006/relationships/ctrlProp";
}
