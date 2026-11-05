using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML styles part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxStylesPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores the number of number formats items processed by the operation.
    /// </summary>
    public uint NumberFormatsCount;
    /// <summary>
    /// Indicates whether number formats count is present and should be written.
    /// </summary>
    public bool HasNumberFormatsCount;
    /// <summary>
    /// Contains number formats.
    /// </summary>
    public List<XlsxNumberFormatDefinition> NumberFormats;
    /// <summary>
    /// Stores the number of fonts items processed by the operation.
    /// </summary>
    public uint FontsCount;
    /// <summary>
    /// Indicates whether fonts count is present and should be written.
    /// </summary>
    public bool HasFontsCount;
    /// <summary>
    /// Contains fonts.
    /// </summary>
    public List<XlsxRawStyleElement> Fonts;
    /// <summary>Contains typed fonts aligned with the raw font collection.</summary>
    public List<XlsxStyleFont> TypedFonts;
    /// <summary>
    /// Stores the number of fills items processed by the operation.
    /// </summary>
    public uint FillsCount;
    /// <summary>
    /// Indicates whether fills count is present and should be written.
    /// </summary>
    public bool HasFillsCount;
    /// <summary>
    /// Contains fills.
    /// </summary>
    public List<XlsxRawStyleElement> Fills;
    /// <summary>Contains typed pattern and gradient fills.</summary>
    public List<XlsxStyleFill> TypedFills;
    /// <summary>
    /// Stores the number of borders items processed by the operation.
    /// </summary>
    public uint BordersCount;
    /// <summary>
    /// Indicates whether borders count is present and should be written.
    /// </summary>
    public bool HasBordersCount;
    /// <summary>
    /// Contains borders.
    /// </summary>
    public List<XlsxRawStyleElement> Borders;
    /// <summary>Contains typed borders.</summary>
    public List<XlsxStyleBorder> TypedBorders;
    /// <summary>
    /// Stores the number of cell style formats items processed by the operation.
    /// </summary>
    public uint CellStyleFormatsCount;
    /// <summary>
    /// Indicates whether cell style formats count is present and should be written.
    /// </summary>
    public bool HasCellStyleFormatsCount;
    /// <summary>
    /// Contains cell style formats.
    /// </summary>
    public List<XlsxCellFormatDefinition> CellStyleFormats;
    /// <summary>
    /// Stores the number of cell formats items processed by the operation.
    /// </summary>
    public uint CellFormatsCount;
    /// <summary>
    /// Indicates whether cell formats count is present and should be written.
    /// </summary>
    public bool HasCellFormatsCount;
    /// <summary>
    /// Contains cell formats.
    /// </summary>
    public List<XlsxCellFormatDefinition> CellFormats;
    /// <summary>
    /// Stores the number of differential formats items processed by the operation.
    /// </summary>
    public uint DifferentialFormatsCount;
    /// <summary>
    /// Indicates whether differential formats count is present and should be written.
    /// </summary>
    public bool HasDifferentialFormatsCount;
    /// <summary>
    /// Contains differential formats.
    /// </summary>
    public List<XlsxRawStyleElement> DifferentialFormats;
    /// <summary>Contains typed differential styles.</summary>
    public List<XlsxDifferentialStyle> TypedDifferentialFormats;
    /// <summary>Contains named cell styles.</summary>
    public List<XlsxNamedStyle> NamedStyles;
    /// <summary>
    /// Contains table styles.
    /// </summary>
    public XlsxStylesTableStyles TableStyles;
    /// <summary>
    /// Indicates whether table styles is present and should be written.
    /// </summary>
    public bool HasTableStyles;
    /// <summary>
    /// Contains colors.
    /// </summary>
    public XlsxRawStyleElement Colors;
    /// <summary>
    /// Indicates whether colors is present and should be written.
    /// </summary>
    public bool HasColors;
    /// <summary>Contains indexed colors.</summary>
    public List<XlsxIndexedStyleColor> IndexedColors;
    /// <summary>Contains most-recently-used colors.</summary>
    public List<XlsxIndexedStyleColor> MostRecentlyUsedColors;
    /// <summary>
    /// Stores the extension list value used by this model.
    /// </summary>
    public XlsxExtensionList ExtensionList;
    /// <summary>
    /// Indicates whether extension list is present and should be written.
    /// </summary>
    public bool HasExtensionList;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new styles part with its default state.
    /// </summary>
    public XlsxStylesPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.Styles;
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        NumberFormatsCount = 0;
        HasNumberFormatsCount = false;
        NumberFormats = new List<XlsxNumberFormatDefinition>();
        FontsCount = 0;
        HasFontsCount = false;
        Fonts = new List<XlsxRawStyleElement>();
        TypedFonts = new List<XlsxStyleFont>();
        FillsCount = 0;
        HasFillsCount = false;
        Fills = new List<XlsxRawStyleElement>();
        TypedFills = new List<XlsxStyleFill>();
        BordersCount = 0;
        HasBordersCount = false;
        Borders = new List<XlsxRawStyleElement>();
        TypedBorders = new List<XlsxStyleBorder>();
        CellStyleFormatsCount = 0;
        HasCellStyleFormatsCount = false;
        CellStyleFormats = new List<XlsxCellFormatDefinition>();
        CellFormatsCount = 0;
        HasCellFormatsCount = false;
        CellFormats = new List<XlsxCellFormatDefinition>();
        DifferentialFormatsCount = 0;
        HasDifferentialFormatsCount = false;
        DifferentialFormats = new List<XlsxRawStyleElement>();
        TypedDifferentialFormats = new List<XlsxDifferentialStyle>();
        NamedStyles = new List<XlsxNamedStyle>();
        TableStyles = new XlsxStylesTableStyles();
        HasTableStyles = false;
        Colors = new XlsxRawStyleElement();
        HasColors = false;
        IndexedColors = new List<XlsxIndexedStyleColor>();
        MostRecentlyUsedColors = new List<XlsxIndexedStyleColor>();
        ExtensionList = new XlsxExtensionList();
        HasExtensionList = false;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
