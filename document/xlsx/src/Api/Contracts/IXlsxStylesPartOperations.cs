using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>Defines typed stylesheet interning, mutation, and resolution operations.</summary>
public interface IXlsxStylesPartOperations
{
    /// <summary>Interns a font and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="font">The typed font.</param><returns>The font index, or -1 on invalid input.</returns>
    int InternFont(XlsxStylesPart data, XlsxStyleFont font);
    /// <summary>Interns a fill and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="fill">The typed fill.</param><returns>The fill index, or -1 on invalid input.</returns>
    int InternFill(XlsxStylesPart data, XlsxStyleFill fill);
    /// <summary>Interns a border and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="border">The typed border.</param><returns>The border index, or -1 on invalid input.</returns>
    int InternBorder(XlsxStylesPart data, XlsxStyleBorder border);
    /// <summary>Interns a differential style and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="style">The differential style.</param><returns>The differential style index, or -1 on invalid input.</returns>
    int InternDifferentialStyle(XlsxStylesPart data, XlsxDifferentialStyle style);
    /// <summary>Interns a cell format and returns its zero-based index.</summary>
    /// <param name="data">The styles part.</param><param name="format">The cell format.</param><returns>The cell format index, or -1 on invalid input.</returns>
    int InternCellFormat(XlsxStylesPart data, XlsxCellFormatDefinition format);
    /// <summary>Replaces a font at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="font">The replacement font.</param><returns>Validation diagnostics.</returns>
    XlsxValidationResult UpdateFont(XlsxStylesPart data, int index, XlsxStyleFont font);
    /// <summary>Replaces a fill at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="fill">The replacement fill.</param><returns>Validation diagnostics.</returns>
    XlsxValidationResult UpdateFill(XlsxStylesPart data, int index, XlsxStyleFill fill);
    /// <summary>Replaces a border at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="border">The replacement border.</param><returns>Validation diagnostics.</returns>
    XlsxValidationResult UpdateBorder(XlsxStylesPart data, int index, XlsxStyleBorder border);
    /// <summary>Replaces a cell format at an existing index.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based index.</param><param name="format">The replacement format.</param><returns>Validation diagnostics.</returns>
    XlsxValidationResult UpdateCellFormat(XlsxStylesPart data, int index, XlsxCellFormatDefinition format);
    /// <summary>Adds a named cell style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The named style.</param><returns>The added index, or -1 on invalid input.</returns>
    int AddNamedStyle(XlsxStylesPart data, XlsxNamedStyle style);
    /// <summary>Adds a custom table style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The table style.</param><returns>The added index, or -1 on invalid input.</returns>
    int AddTableStyle(XlsxStylesPart data, XlsxTableStyleDefinition style);
    /// <summary>Adds a custom pivot style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The pivot style.</param><returns>The added index, or -1 on invalid input.</returns>
    int AddPivotStyle(XlsxStylesPart data, XlsxTableStyleDefinition style);
    /// <summary>Adds a custom slicer style.</summary>
    /// <param name="data">The styles part.</param><param name="style">The slicer style.</param><returns>The added index, or -1 on invalid input.</returns>
    int AddSlicerStyle(XlsxStylesPart data, XlsxTableStyleDefinition style);
    /// <summary>Adds an indexed color.</summary>
    /// <param name="data">The styles part.</param><param name="color">The color.</param><returns>The added index, or -1 on invalid input.</returns>
    int AddIndexedColor(XlsxStylesPart data, XlsxSpreadsheetColor color);
    /// <summary>Adds a theme-based color to the recent color collection.</summary>
    /// <param name="data">The styles part.</param><param name="theme">The theme color index.</param><param name="tint">The tint adjustment.</param><returns>The added index, or -1 on invalid input.</returns>
    int AddThemeColor(XlsxStylesPart data, uint theme, double tint);
    /// <summary>Resolves a cell format and its referenced components.</summary>
    /// <param name="data">The styles part.</param><param name="index">The zero-based cell format index.</param><returns>The resolved style and diagnostics.</returns>
    XlsxResolvedCellStyle ResolveCellFormat(XlsxStylesPart data, int index);
}
