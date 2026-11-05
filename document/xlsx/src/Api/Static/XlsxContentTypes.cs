using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Provides the canonical OOXML content type identifiers used when inspecting or composing spreadsheet packages.
/// </summary>
public static class XlsxContentTypes
{
    /// <summary>
    /// Identifies the relationships OOXML value.
    /// </summary>
    public const string Relationships = "application/vnd.openxmlformats-package.relationships+xml";
    /// <summary>
    /// Identifies the xml OOXML value.
    /// </summary>
    public const string Xml = "application/xml";
    /// <summary>
    /// Identifies the workbook OOXML value.
    /// </summary>
    public const string Workbook = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
    /// <summary>
    /// Identifies the macro enabled workbook OOXML value.
    /// </summary>
    public const string MacroEnabledWorkbook = "application/vnd.ms-excel.sheet.macroEnabled.main+xml";
    /// <summary>
    /// Identifies the template workbook OOXML value.
    /// </summary>
    public const string TemplateWorkbook = "application/vnd.openxmlformats-officedocument.spreadsheetml.template.main+xml";
    /// <summary>
    /// Identifies the macro enabled template workbook OOXML value.
    /// </summary>
    public const string MacroEnabledTemplateWorkbook = "application/vnd.ms-excel.template.macroEnabled.main+xml";
    /// <summary>
    /// Identifies the worksheet OOXML value.
    /// </summary>
    public const string Worksheet = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml";
    /// <summary>
    /// Identifies the chartsheet OOXML value.
    /// </summary>
    public const string Chartsheet = "application/vnd.openxmlformats-officedocument.spreadsheetml.chartsheet+xml";
    /// <summary>
    /// Identifies the dialogsheet OOXML value.
    /// </summary>
    public const string Dialogsheet = "application/vnd.openxmlformats-officedocument.spreadsheetml.dialogsheet+xml";
    /// <summary>
    /// Identifies the macrosheet OOXML value.
    /// </summary>
    public const string Macrosheet = "application/vnd.ms-excel.macrosheet+xml";
    /// <summary>
    /// Identifies the styles OOXML value.
    /// </summary>
    public const string Styles = "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml";
    /// <summary>
    /// Identifies the shared strings OOXML value.
    /// </summary>
    public const string SharedStrings = "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml";
    /// <summary>
    /// Identifies the calculation chain OOXML value.
    /// </summary>
    public const string CalculationChain = "application/vnd.openxmlformats-officedocument.spreadsheetml.calcChain+xml";
    /// <summary>
    /// Identifies the theme OOXML value.
    /// </summary>
    public const string Theme = "application/vnd.openxmlformats-officedocument.theme+xml";
    /// <summary>
    /// Identifies the core properties OOXML value.
    /// </summary>
    public const string CoreProperties = "application/vnd.openxmlformats-package.core-properties+xml";
    /// <summary>
    /// Identifies the extended properties OOXML value.
    /// </summary>
    public const string ExtendedProperties = "application/vnd.openxmlformats-officedocument.extended-properties+xml";
    /// <summary>
    /// Identifies the custom properties OOXML value.
    /// </summary>
    public const string CustomProperties = "application/vnd.openxmlformats-officedocument.custom-properties+xml";
    /// <summary>
    /// Identifies the custom xml properties OOXML value.
    /// </summary>
    public const string CustomXmlProperties = "application/vnd.openxmlformats-officedocument.customXmlProperties+xml";
    /// <summary>
    /// Identifies the digital signature origin OOXML value.
    /// </summary>
    public const string DigitalSignatureOrigin = "application/vnd.openxmlformats-package.digital-signature-origin";
    /// <summary>
    /// Identifies the digital signature xml OOXML value.
    /// </summary>
    public const string DigitalSignatureXml = "application/vnd.openxmlformats-package.digital-signature-xmlsignature+xml";
    /// <summary>
    /// Identifies the table OOXML value.
    /// </summary>
    public const string Table = "application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml";
    /// <summary>
    /// Identifies the comments OOXML value.
    /// </summary>
    public const string Comments = "application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml";
    /// <summary>
    /// Identifies the threaded comments OOXML value.
    /// </summary>
    public const string ThreadedComments = "application/vnd.ms-excel.threadedcomments+xml";
    /// <summary>
    /// Identifies the vml drawing OOXML value.
    /// </summary>
    public const string VmlDrawing = "application/vnd.openxmlformats-officedocument.vmlDrawing";
    /// <summary>
    /// Identifies the drawing OOXML value.
    /// </summary>
    public const string Drawing = "application/vnd.openxmlformats-officedocument.drawing+xml";
    /// <summary>
    /// Identifies the chart OOXML value.
    /// </summary>
    public const string Chart = "application/vnd.openxmlformats-officedocument.drawingml.chart+xml";
    /// <summary>
    /// Identifies the ole object OOXML value.
    /// </summary>
    public const string OleObject = "application/vnd.openxmlformats-officedocument.oleObject";
    /// <summary>
    /// Identifies the embedded spreadsheet package OOXML value.
    /// </summary>
    public const string EmbeddedSpreadsheetPackage = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    /// <summary>
    /// Identifies the sheet metadata OOXML value.
    /// </summary>
    public const string SheetMetadata = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheetMetadata+xml";
    /// <summary>
    /// Identifies the rich value OOXML value.
    /// </summary>
    public const string RichValue = "application/vnd.ms-excel.rdrichvalue+xml";
    /// <summary>
    /// Identifies the rich value types OOXML value.
    /// </summary>
    public const string RichValueTypes = "application/vnd.ms-excel.rdrichvaluetypes+xml";
    /// <summary>
    /// Identifies the rich value relationship OOXML value.
    /// </summary>
    public const string RichValueRelationship = "application/vnd.ms-excel.richvaluerel+xml";
    /// <summary>
    /// Identifies the pivot cache definition OOXML value.
    /// </summary>
    public const string PivotCacheDefinition = "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheDefinition+xml";
    /// <summary>
    /// Identifies the pivot cache records OOXML value.
    /// </summary>
    public const string PivotCacheRecords = "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheRecords+xml";
    /// <summary>
    /// Identifies the pivot table OOXML value.
    /// </summary>
    public const string PivotTable = "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotTable+xml";
    /// <summary>
    /// Identifies the external link OOXML value.
    /// </summary>
    public const string ExternalLink = "application/vnd.openxmlformats-officedocument.spreadsheetml.externalLink+xml";
    /// <summary>
    /// Identifies the connections OOXML value.
    /// </summary>
    public const string Connections = "application/vnd.openxmlformats-officedocument.spreadsheetml.connections+xml";
    /// <summary>
    /// Identifies the query table OOXML value.
    /// </summary>
    public const string QueryTable = "application/vnd.openxmlformats-officedocument.spreadsheetml.queryTable+xml";
    /// <summary>
    /// Identifies the slicer OOXML value.
    /// </summary>
    public const string Slicer = "application/vnd.ms-excel.slicer+xml";
    /// <summary>
    /// Identifies the slicer cache OOXML value.
    /// </summary>
    public const string SlicerCache = "application/vnd.ms-excel.slicerCache+xml";
    /// <summary>
    /// Identifies the timeline OOXML value.
    /// </summary>
    public const string Timeline = "application/vnd.ms-excel.timeline+xml";
    /// <summary>
    /// Identifies the timeline cache OOXML value.
    /// </summary>
    public const string TimelineCache = "application/vnd.ms-excel.timelineCache+xml";
    /// <summary>
    /// Identifies the vba project OOXML value.
    /// </summary>
    public const string VbaProject = "application/vnd.ms-office.vbaProject";
    /// <summary>Identifies a form-control properties part.</summary>
    public const string ControlProperties = "application/vnd.ms-excel.controlproperties+xml";
}
