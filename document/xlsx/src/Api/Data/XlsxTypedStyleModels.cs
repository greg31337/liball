using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>Represents a typed spreadsheet font.</summary>
public class XlsxStyleFont
{
    /// <summary>Stores the font name.</summary>
    public string Name;
    /// <summary>Stores the font size in points.</summary>
    public double Size;
    /// <summary>Indicates whether the font size is present.</summary>
    public bool HasSize;
    /// <summary>Stores the bold setting.</summary>
    public bool Bold;
    /// <summary>Stores the italic setting.</summary>
    public bool Italic;
    /// <summary>Stores the strike setting.</summary>
    public bool Strike;
    /// <summary>Stores the outline setting.</summary>
    public bool Outline;
    /// <summary>Stores the shadow setting.</summary>
    public bool Shadow;
    /// <summary>Stores the condense setting.</summary>
    public bool Condense;
    /// <summary>Stores the extend setting.</summary>
    public bool Extend;
    /// <summary>Stores the underline style.</summary>
    public string Underline;
    /// <summary>Stores the vertical alignment.</summary>
    public string VerticalAlignment;
    /// <summary>Stores the font family.</summary>
    public uint Family;
    /// <summary>Indicates whether the font family is present.</summary>
    public bool HasFamily;
    /// <summary>Stores the character set.</summary>
    public uint CharacterSet;
    /// <summary>Indicates whether the character set is present.</summary>
    public bool HasCharacterSet;
    /// <summary>Stores the font scheme.</summary>
    public string Scheme;
    /// <summary>Stores the font color.</summary>
    public XlsxSpreadsheetColor Color;
    /// <summary>Indicates whether the font color is present.</summary>
    public bool HasColor;
    /// <summary>Stores preserved XML.</summary>
    public string RawXml;
    /// <summary>Contains unknown child XML.</summary>
    public List<string> UnknownXml;
    /// <summary>Initializes a typed font.</summary>
    public XlsxStyleFont()
    {
        Name = ""; Size = 0; HasSize = false; Bold = false; Italic = false; Strike = false; Outline = false; Shadow = false; Condense = false; Extend = false;
        Underline = ""; VerticalAlignment = ""; Family = 0; HasFamily = false; CharacterSet = 0; HasCharacterSet = false; Scheme = "";
        Color = new XlsxSpreadsheetColor(); HasColor = false; RawXml = ""; UnknownXml = new List<string>();
    }
}

/// <summary>Represents a gradient fill stop.</summary>
public class XlsxStyleGradientStop
{
    /// <summary>Stores the stop position.</summary>
    public double Position;
    /// <summary>Stores the stop color.</summary>
    public XlsxSpreadsheetColor Color;
    /// <summary>Initializes a gradient stop.</summary>
    public XlsxStyleGradientStop() { Position = 0; Color = new XlsxSpreadsheetColor(); }
}

/// <summary>Represents a typed pattern or gradient fill.</summary>
public class XlsxStyleFill
{
    /// <summary>Stores the pattern type.</summary>
    public string PatternType;
    /// <summary>Stores the foreground color.</summary>
    public XlsxSpreadsheetColor ForegroundColor;
    /// <summary>Indicates whether the foreground color is present.</summary>
    public bool HasForegroundColor;
    /// <summary>Stores the background color.</summary>
    public XlsxSpreadsheetColor BackgroundColor;
    /// <summary>Indicates whether the background color is present.</summary>
    public bool HasBackgroundColor;
    /// <summary>Indicates whether this is a gradient fill.</summary>
    public bool IsGradient;
    /// <summary>Stores the gradient type.</summary>
    public string GradientType;
    /// <summary>Stores the gradient degree.</summary>
    public double Degree;
    /// <summary>Indicates whether gradient degree is present.</summary>
    public bool HasDegree;
    /// <summary>Contains gradient stops.</summary>
    public List<XlsxStyleGradientStop> GradientStops;
    /// <summary>Stores preserved XML.</summary>
    public string RawXml;
    /// <summary>Contains unknown child XML.</summary>
    public List<string> UnknownXml;
    /// <summary>Initializes a typed fill.</summary>
    public XlsxStyleFill()
    {
        PatternType = ""; ForegroundColor = new XlsxSpreadsheetColor(); HasForegroundColor = false; BackgroundColor = new XlsxSpreadsheetColor(); HasBackgroundColor = false;
        IsGradient = false; GradientType = ""; Degree = 0; HasDegree = false; GradientStops = new List<XlsxStyleGradientStop>(); RawXml = ""; UnknownXml = new List<string>();
    }
}

/// <summary>Represents one side of a typed border.</summary>
public class XlsxStyleBorderSide
{
    /// <summary>Stores the border style.</summary>
    public string Style;
    /// <summary>Stores the border color.</summary>
    public XlsxSpreadsheetColor Color;
    /// <summary>Indicates whether the border color is present.</summary>
    public bool HasColor;
    /// <summary>Initializes a border side.</summary>
    public XlsxStyleBorderSide() { Style = ""; Color = new XlsxSpreadsheetColor(); HasColor = false; }
}

/// <summary>Represents a typed spreadsheet border.</summary>
public class XlsxStyleBorder
{
    /// <summary>Stores the left side.</summary>
    public XlsxStyleBorderSide Left;
    /// <summary>Stores the right side.</summary>
    public XlsxStyleBorderSide Right;
    /// <summary>Stores the top side.</summary>
    public XlsxStyleBorderSide Top;
    /// <summary>Stores the bottom side.</summary>
    public XlsxStyleBorderSide Bottom;
    /// <summary>Stores the diagonal side.</summary>
    public XlsxStyleBorderSide Diagonal;
    /// <summary>Stores the vertical inner side.</summary>
    public XlsxStyleBorderSide Vertical;
    /// <summary>Stores the horizontal inner side.</summary>
    public XlsxStyleBorderSide Horizontal;
    /// <summary>Stores the diagonal-up setting.</summary>
    public bool DiagonalUp;
    /// <summary>Stores the diagonal-down setting.</summary>
    public bool DiagonalDown;
    /// <summary>Stores the outline setting.</summary>
    public bool Outline;
    /// <summary>Stores preserved XML.</summary>
    public string RawXml;
    /// <summary>Contains unknown child XML.</summary>
    public List<string> UnknownXml;
    /// <summary>Initializes a typed border.</summary>
    public XlsxStyleBorder()
    {
        Left = new XlsxStyleBorderSide(); Right = new XlsxStyleBorderSide(); Top = new XlsxStyleBorderSide(); Bottom = new XlsxStyleBorderSide();
        Diagonal = new XlsxStyleBorderSide(); Vertical = new XlsxStyleBorderSide(); Horizontal = new XlsxStyleBorderSide(); DiagonalUp = false; DiagonalDown = false; Outline = false;
        RawXml = ""; UnknownXml = new List<string>();
    }
}

/// <summary>Represents typed cell alignment settings.</summary>
public class XlsxStyleAlignment
{
    /// <summary>Stores horizontal alignment.</summary>
    public string Horizontal;
    /// <summary>Stores vertical alignment.</summary>
    public string Vertical;
    /// <summary>Stores text rotation.</summary>
    public int TextRotation;
    /// <summary>Indicates whether text rotation is present.</summary>
    public bool HasTextRotation;
    /// <summary>Stores wrap text.</summary>
    public bool WrapText;
    /// <summary>Indicates whether wrap text is present.</summary>
    public bool HasWrapText;
    /// <summary>Stores shrink to fit.</summary>
    public bool ShrinkToFit;
    /// <summary>Indicates whether shrink to fit is present.</summary>
    public bool HasShrinkToFit;
    /// <summary>Stores indentation.</summary>
    public uint Indent;
    /// <summary>Indicates whether indentation is present.</summary>
    public bool HasIndent;
    /// <summary>Stores reading order.</summary>
    public uint ReadingOrder;
    /// <summary>Indicates whether reading order is present.</summary>
    public bool HasReadingOrder;
    /// <summary>Initializes typed alignment.</summary>
    public XlsxStyleAlignment()
    {
        Horizontal = ""; Vertical = ""; TextRotation = 0; HasTextRotation = false; WrapText = false; HasWrapText = false; ShrinkToFit = false; HasShrinkToFit = false;
        Indent = 0; HasIndent = false; ReadingOrder = 0; HasReadingOrder = false;
    }
}

/// <summary>Represents typed cell protection settings.</summary>
public class XlsxStyleProtection
{
    /// <summary>Stores the locked setting.</summary>
    public bool Locked;
    /// <summary>Indicates whether locked is present.</summary>
    public bool HasLocked;
    /// <summary>Stores the hidden setting.</summary>
    public bool Hidden;
    /// <summary>Indicates whether hidden is present.</summary>
    public bool HasHidden;
    /// <summary>Initializes typed protection.</summary>
    public XlsxStyleProtection() { Locked = false; HasLocked = false; Hidden = false; HasHidden = false; }
}

/// <summary>Represents a typed differential style.</summary>
public class XlsxDifferentialStyle
{
    /// <summary>Stores the number format.</summary>
    public XlsxNumberFormatDefinition NumberFormat;
    /// <summary>Indicates whether the number format is present.</summary>
    public bool HasNumberFormat;
    /// <summary>Stores the font.</summary>
    public XlsxStyleFont Font;
    /// <summary>Indicates whether the font is present.</summary>
    public bool HasFont;
    /// <summary>Stores the fill.</summary>
    public XlsxStyleFill Fill;
    /// <summary>Indicates whether the fill is present.</summary>
    public bool HasFill;
    /// <summary>Stores the border.</summary>
    public XlsxStyleBorder Border;
    /// <summary>Indicates whether the border is present.</summary>
    public bool HasBorder;
    /// <summary>Stores alignment.</summary>
    public XlsxStyleAlignment Alignment;
    /// <summary>Indicates whether alignment is present.</summary>
    public bool HasAlignment;
    /// <summary>Stores protection.</summary>
    public XlsxStyleProtection Protection;
    /// <summary>Indicates whether protection is present.</summary>
    public bool HasProtection;
    /// <summary>Stores preserved XML.</summary>
    public string RawXml;
    /// <summary>Initializes a differential style.</summary>
    public XlsxDifferentialStyle()
    {
        NumberFormat = new XlsxNumberFormatDefinition(); HasNumberFormat = false; Font = new XlsxStyleFont(); HasFont = false; Fill = new XlsxStyleFill(); HasFill = false;
        Border = new XlsxStyleBorder(); HasBorder = false; Alignment = new XlsxStyleAlignment(); HasAlignment = false; Protection = new XlsxStyleProtection(); HasProtection = false; RawXml = "";
    }
}

/// <summary>Represents a named cell style.</summary>
public class XlsxNamedStyle
{
    /// <summary>Stores the style name.</summary>
    public string Name;
    /// <summary>Stores the base format identifier.</summary>
    public uint FormatId;
    /// <summary>Stores the built-in style identifier.</summary>
    public uint BuiltInId;
    /// <summary>Indicates whether the built-in identifier is present.</summary>
    public bool HasBuiltInId;
    /// <summary>Stores the hidden setting.</summary>
    public bool Hidden;
    /// <summary>Indicates whether hidden is present.</summary>
    public bool HasHidden;
    /// <summary>Initializes a named style.</summary>
    public XlsxNamedStyle() { Name = ""; FormatId = 0; BuiltInId = 0; HasBuiltInId = false; Hidden = false; HasHidden = false; }
}

/// <summary>Represents one custom table or pivot style element.</summary>
public class XlsxTableStyleElement
{
    /// <summary>Stores the element type.</summary>
    public string Type;
    /// <summary>Stores the differential format identifier.</summary>
    public uint DifferentialFormatId;
    /// <summary>Stores the stripe size.</summary>
    public uint Size;
    /// <summary>Indicates whether stripe size is present.</summary>
    public bool HasSize;
    /// <summary>Initializes a table style element.</summary>
    public XlsxTableStyleElement() { Type = ""; DifferentialFormatId = 0; Size = 0; HasSize = false; }
}

/// <summary>Represents a custom table, pivot, or slicer style.</summary>
public class XlsxTableStyleDefinition
{
    /// <summary>Stores the style name.</summary>
    public string Name;
    /// <summary>Indicates whether the style applies to tables.</summary>
    public bool AppliesToTables;
    /// <summary>Indicates whether the style applies to pivot tables.</summary>
    public bool AppliesToPivots;
    /// <summary>Indicates whether the style applies to slicers.</summary>
    public bool AppliesToSlicers;
    /// <summary>Contains style elements.</summary>
    public List<XlsxTableStyleElement> Elements;
    /// <summary>Stores preserved XML.</summary>
    public string RawXml;
    /// <summary>Initializes a table style definition.</summary>
    public XlsxTableStyleDefinition() { Name = ""; AppliesToTables = false; AppliesToPivots = false; AppliesToSlicers = false; Elements = new List<XlsxTableStyleElement>(); RawXml = ""; }
}

/// <summary>Represents one indexed or recent style color.</summary>
public class XlsxIndexedStyleColor
{
    /// <summary>Stores the color value.</summary>
    public XlsxSpreadsheetColor Color;
    /// <summary>Initializes an indexed style color.</summary>
    public XlsxIndexedStyleColor() { Color = new XlsxSpreadsheetColor(); }
}

/// <summary>Represents a resolved cell style and its referenced components.</summary>
public class XlsxResolvedCellStyle
{
    /// <summary>Indicates whether resolution succeeded.</summary>
    public bool IsValid;
    /// <summary>Contains resolution diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the resolved cell format.</summary>
    public XlsxCellFormatDefinition CellFormat;
    /// <summary>Stores the resolved font.</summary>
    public XlsxStyleFont Font;
    /// <summary>Indicates whether a font was resolved.</summary>
    public bool HasFont;
    /// <summary>Stores the resolved fill.</summary>
    public XlsxStyleFill Fill;
    /// <summary>Indicates whether a fill was resolved.</summary>
    public bool HasFill;
    /// <summary>Stores the resolved border.</summary>
    public XlsxStyleBorder Border;
    /// <summary>Indicates whether a border was resolved.</summary>
    public bool HasBorder;
    /// <summary>Stores the resolved number format code.</summary>
    public string NumberFormatCode;
    /// <summary>Stores resolved alignment.</summary>
    public XlsxStyleAlignment Alignment;
    /// <summary>Indicates whether alignment was resolved.</summary>
    public bool HasAlignment;
    /// <summary>Stores resolved protection.</summary>
    public XlsxStyleProtection Protection;
    /// <summary>Indicates whether protection was resolved.</summary>
    public bool HasProtection;
    /// <summary>Initializes a resolved cell style.</summary>
    public XlsxResolvedCellStyle()
    {
        IsValid = true; Validation = new XlsxValidationResult(); CellFormat = new XlsxCellFormatDefinition(); Font = new XlsxStyleFont(); HasFont = false;
        Fill = new XlsxStyleFill(); HasFill = false; Border = new XlsxStyleBorder(); HasBorder = false; NumberFormatCode = "";
        Alignment = new XlsxStyleAlignment(); HasAlignment = false; Protection = new XlsxStyleProtection(); HasProtection = false;
    }

    internal void AddError(string code, string message)
    {
        Validation.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message)); IsValid = Validation.IsValid;
    }
}
