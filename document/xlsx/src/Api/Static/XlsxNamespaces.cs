using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Provides the canonical OOXML namespace identifiers used when inspecting or composing spreadsheet packages.
/// </summary>
public static class XlsxNamespaces
{
    /// <summary>
    /// Identifies the content types OOXML value.
    /// </summary>
    public const string ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    /// <summary>
    /// Identifies the package relationships OOXML value.
    /// </summary>
    public const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    /// <summary>
    /// Identifies the package core properties relationships OOXML value.
    /// </summary>
    public const string PackageCorePropertiesRelationships = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
    /// <summary>
    /// Identifies the markup compatibility OOXML value.
    /// </summary>
    public const string MarkupCompatibility = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    /// <summary>
    /// Identifies the spreadsheet OOXML value.
    /// </summary>
    public const string Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    /// <summary>
    /// Identifies the strict spreadsheet OOXML value.
    /// </summary>
    public const string StrictSpreadsheet = "http://purl.oclc.org/ooxml/spreadsheetml/main";
    /// <summary>
    /// Identifies the office document relationships OOXML value.
    /// </summary>
    public const string OfficeDocumentRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    /// <summary>
    /// Identifies the Strict office document relationships OOXML value.
    /// </summary>
    public const string StrictOfficeDocumentRelationships = "http://purl.oclc.org/ooxml/officeDocument/relationships";
    /// <summary>
    /// Identifies the drawing OOXML value.
    /// </summary>
    public const string Drawing = "http://schemas.openxmlformats.org/drawingml/2006/main";
    /// <summary>
    /// Identifies the spreadsheet drawing OOXML value.
    /// </summary>
    public const string SpreadsheetDrawing = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    /// <summary>
    /// Identifies the chart OOXML value.
    /// </summary>
    public const string Chart = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    /// <summary>
    /// Identifies the office2010 spreadsheet OOXML value.
    /// </summary>
    public const string Office2010Spreadsheet = "http://schemas.microsoft.com/office/spreadsheetml/2009/9/main";
    /// <summary>
    /// Identifies the office2013 spreadsheet OOXML value.
    /// </summary>
    public const string Office2013Spreadsheet = "http://schemas.microsoft.com/office/spreadsheetml/2010/11/main";
    /// <summary>
    /// Identifies the office excel OOXML value.
    /// </summary>
    public const string OfficeExcel = "http://schemas.microsoft.com/office/excel/2006/main";
    /// <summary>
    /// Identifies the core properties OOXML value.
    /// </summary>
    public const string CoreProperties = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    /// <summary>
    /// Identifies the dublin core OOXML value.
    /// </summary>
    public const string DublinCore = "http://purl.org/dc/elements/1.1/";
    /// <summary>
    /// Identifies the dublin core terms OOXML value.
    /// </summary>
    public const string DublinCoreTerms = "http://purl.org/dc/terms/";
    /// <summary>
    /// Identifies the xml schema instance OOXML value.
    /// </summary>
    public const string XmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
    /// <summary>
    /// Identifies the extended properties OOXML value.
    /// </summary>
    public const string ExtendedProperties = "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
    /// <summary>
    /// Identifies the custom properties OOXML value.
    /// </summary>
    public const string CustomProperties = "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties";
    /// <summary>
    /// Identifies the variant types OOXML value.
    /// </summary>
    public const string VariantTypes = "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes";
}
