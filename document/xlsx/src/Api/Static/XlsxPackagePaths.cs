using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Provides the canonical OOXML package path identifiers used when inspecting or composing spreadsheet packages.
/// </summary>
public static class XlsxPackagePaths
{
    /// <summary>
    /// Identifies the content types OOXML value.
    /// </summary>
    public const string ContentTypes = "/[Content_Types].xml";
    /// <summary>
    /// Identifies the package relationships OOXML value.
    /// </summary>
    public const string PackageRelationships = "/_rels/.rels";
    /// <summary>
    /// Identifies the digital signatures directory OOXML value.
    /// </summary>
    public const string DigitalSignaturesDirectory = "/_xmlsignatures/";
    /// <summary>
    /// Identifies the digital signature origin OOXML value.
    /// </summary>
    public const string DigitalSignatureOrigin = "/_xmlsignatures/origin.sigs";
    /// <summary>
    /// Identifies the core properties OOXML value.
    /// </summary>
    public const string CoreProperties = "/docProps/core.xml";
    /// <summary>
    /// Identifies the extended properties OOXML value.
    /// </summary>
    public const string ExtendedProperties = "/docProps/app.xml";
    /// <summary>
    /// Identifies the custom properties OOXML value.
    /// </summary>
    public const string CustomProperties = "/docProps/custom.xml";
    /// <summary>
    /// Identifies the workbook OOXML value.
    /// </summary>
    public const string Workbook = "/xl/workbook.xml";
    /// <summary>
    /// Identifies the workbook relationships OOXML value.
    /// </summary>
    public const string WorkbookRelationships = "/xl/_rels/workbook.xml.rels";
    /// <summary>
    /// Identifies the styles OOXML value.
    /// </summary>
    public const string Styles = "/xl/styles.xml";
    /// <summary>
    /// Identifies the shared strings OOXML value.
    /// </summary>
    public const string SharedStrings = "/xl/sharedStrings.xml";
    /// <summary>
    /// Identifies the calculation chain OOXML value.
    /// </summary>
    public const string CalculationChain = "/xl/calcChain.xml";
    /// <summary>
    /// Identifies the theme OOXML value.
    /// </summary>
    public const string Theme = "/xl/theme/theme1.xml";
    /// <summary>
    /// Identifies the sheet metadata OOXML value.
    /// </summary>
    public const string SheetMetadata = "/xl/metadata.xml";
    /// <summary>
    /// Identifies the rich data directory OOXML value.
    /// </summary>
    public const string RichDataDirectory = "/xl/richData/";
    /// <summary>
    /// Identifies the worksheets directory OOXML value.
    /// </summary>
    public const string WorksheetsDirectory = "/xl/worksheets/";
    /// <summary>
    /// Identifies the worksheet1 OOXML value.
    /// </summary>
    public const string Worksheet1 = "/xl/worksheets/sheet1.xml";
    /// <summary>
    /// Identifies the chartsheets directory OOXML value.
    /// </summary>
    public const string ChartsheetsDirectory = "/xl/chartsheets/";
    /// <summary>
    /// Identifies the drawings directory OOXML value.
    /// </summary>
    public const string DrawingsDirectory = "/xl/drawings/";
    /// <summary>
    /// Identifies the media directory OOXML value.
    /// </summary>
    public const string MediaDirectory = "/xl/media/";
    /// <summary>
    /// Identifies the embeddings directory OOXML value.
    /// </summary>
    public const string EmbeddingsDirectory = "/xl/embeddings/";
    /// <summary>
    /// Identifies the tables directory OOXML value.
    /// </summary>
    public const string TablesDirectory = "/xl/tables/";
    /// <summary>
    /// Identifies the pivot tables directory OOXML value.
    /// </summary>
    public const string PivotTablesDirectory = "/xl/pivotTables/";
    /// <summary>
    /// Identifies the slicer caches directory OOXML value.
    /// </summary>
    public const string SlicerCachesDirectory = "/xl/slicerCaches/";
    /// <summary>
    /// Identifies the slicers directory OOXML value.
    /// </summary>
    public const string SlicersDirectory = "/xl/slicers/";
    /// <summary>
    /// Identifies the timeline caches directory OOXML value.
    /// </summary>
    public const string TimelineCachesDirectory = "/xl/timelineCaches/";
    /// <summary>
    /// Identifies the timelines directory OOXML value.
    /// </summary>
    public const string TimelinesDirectory = "/xl/timelines/";
    /// <summary>
    /// Identifies the external links directory OOXML value.
    /// </summary>
    public const string ExternalLinksDirectory = "/xl/externalLinks/";
    /// <summary>
    /// Identifies the connections OOXML value.
    /// </summary>
    public const string Connections = "/xl/connections.xml";
    /// <summary>
    /// Identifies the query tables directory OOXML value.
    /// </summary>
    public const string QueryTablesDirectory = "/xl/queryTables/";
    /// <summary>
    /// Identifies the custom xml directory OOXML value.
    /// </summary>
    public const string CustomXmlDirectory = "/customXml/";
    /// <summary>
    /// Identifies the comments directory OOXML value.
    /// </summary>
    public const string CommentsDirectory = "/xl/comments/";
    /// <summary>
    /// Identifies the printer settings directory OOXML value.
    /// </summary>
    public const string PrinterSettingsDirectory = "/xl/printerSettings/";
    /// <summary>
    /// Identifies the vba project OOXML value.
    /// </summary>
    public const string VbaProject = "/xl/vbaProject.bin";
    /// <summary>Identifies the package directory containing form-control property parts.</summary>
    public const string ControlPropertiesDirectory = "/xl/ctrlProps/";
}
