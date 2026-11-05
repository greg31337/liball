using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

public class TypedStyleCompletionTests
{
    [Test]
    public void Reader_ExposesTypedStylesPreviouslyAvailableOnlyAsRawXml()
    {
        string xml =
            "<styleSheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\">" +
            "<fonts count=\"1\"><font><b/><i/><sz val=\"12.5\"/><color theme=\"4\" tint=\"0.25\"/><name val=\"Aptos\"/><family val=\"2\"/><charset val=\"1\"/><scheme val=\"minor\"/><u val=\"double\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFF0000\"/><bgColor indexed=\"64\"/></patternFill></fill><fill><gradientFill type=\"linear\" degree=\"45\"><stop position=\"0\"><color rgb=\"FF000000\"/></stop><stop position=\"1\"><color theme=\"5\"/></stop></gradientFill></fill></fills>" +
            "<borders count=\"1\"><border diagonalUp=\"1\"><left style=\"thin\"><color rgb=\"FF112233\"/></left><bottom style=\"double\"/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"1\"><xf fontId=\"0\" fillId=\"1\" borderId=\"0\"><alignment horizontal=\"center\" wrapText=\"1\" indent=\"2\"/><protection locked=\"0\" hidden=\"1\"/></xf></cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Accent\" xfId=\"0\" builtinId=\"5\" hidden=\"0\"/></cellStyles>" +
            "<dxfs count=\"1\"><dxf><font><strike/><color rgb=\"FF00FF00\"/></font><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFFF00\"/></patternFill></fill><border><top style=\"thin\"/></border><alignment horizontal=\"right\"/><protection hidden=\"1\"/><numFmt numFmtId=\"165\" formatCode=\"0.000\"/></dxf></dxfs>" +
            "<tableStyles count=\"1\" defaultTableStyle=\"TableStyleMedium2\" defaultPivotStyle=\"PivotStyleLight16\"><tableStyle name=\"CustomBoth\" count=\"1\" table=\"1\" pivot=\"1\"><tableStyleElement type=\"wholeTable\" dxfId=\"0\"/></tableStyle></tableStyles>" +
            "<colors><indexedColors><rgbColor rgb=\"FF010203\"/></indexedColors><mruColors><color theme=\"6\" tint=\"-0.2\"/></mruColors></colors>" +
            "</styleSheet>";

        XlsxStylesPart part = new XlsxStylesPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True, Diagnostics(part.Validation));
        Assert.That(part.TypedFonts.Count, Is.EqualTo(1));
        Assert.That(part.TypedFonts[0].Name, Is.EqualTo("Aptos"));
        Assert.That(part.TypedFonts[0].Bold, Is.True);
        Assert.That(part.TypedFonts[0].Underline, Is.EqualTo("double"));
        Assert.That(part.TypedFonts[0].Color.Theme, Is.EqualTo(4));
        Assert.That(part.TypedFills.Count, Is.EqualTo(2));
        Assert.That(part.TypedFills[0].PatternType, Is.EqualTo("solid"));
        Assert.That(part.TypedFills[1].IsGradient, Is.True);
        Assert.That(part.TypedFills[1].GradientStops.Count, Is.EqualTo(2));
        Assert.That(part.TypedBorders[0].Left.Style, Is.EqualTo("thin"));
        Assert.That(part.TypedBorders[0].DiagonalUp, Is.True);
        Assert.That(part.CellFormats[0].HasAlignment, Is.True);
        Assert.That(part.CellFormats[0].Alignment.Horizontal, Is.EqualTo("center"));
        Assert.That(part.CellFormats[0].Protection.Hidden, Is.True);
        Assert.That(part.NamedStyles[0].Name, Is.EqualTo("Accent"));
        Assert.That(part.TypedDifferentialFormats[0].HasNumberFormat, Is.True);
        Assert.That(part.TypedDifferentialFormats[0].HasFont, Is.True);
        Assert.That(part.TableStyles.Styles[0].AppliesToTables, Is.True);
        Assert.That(part.TableStyles.Styles[0].AppliesToPivots, Is.True);
        Assert.That(part.IndexedColors[0].Color.Rgb, Is.EqualTo("FF010203"));
        Assert.That(part.MostRecentlyUsedColors[0].Color.Theme, Is.EqualTo(6));
    }

    [Test]
    public void Operations_InternMutateResolveAndRoundTripTypedStyles()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        IXlsxStylesPartOperations operations = new XlsxStylesPartOperations();
        XlsxStyleFont font = CreateFont("Aptos", "FF336699");
        XlsxStyleFill fill = CreateGradientFill();
        XlsxStyleBorder border = CreateBorder();
        XlsxDifferentialStyle differential = new XlsxDifferentialStyle();
        differential.Font = CreateFont("Aptos", "FFFF0000");
        differential.HasFont = true;

        int fontId = operations.InternFont(styles, font);
        int sameFontId = operations.InternFont(styles, CreateFont("Aptos", "FF336699"));
        int fillId = operations.InternFill(styles, fill);
        int borderId = operations.InternBorder(styles, border);
        int differentialId = operations.InternDifferentialStyle(styles, differential);
        AddNumberFormat(styles, 165, "0.000");
        XlsxCellFormatDefinition format = CreateCellFormat(fontId, fillId, borderId);
        int formatId = operations.InternCellFormat(styles, format);
        int sameFormatId = operations.InternCellFormat(styles, CreateCellFormat(fontId, fillId, borderId));

        Assert.That(operations.AddNamedStyle(styles, CreateNamedStyle("Custom", formatId)), Is.EqualTo(0));
        Assert.That(operations.AddTableStyle(styles, CreateTableStyle("CustomTable", differentialId)), Is.EqualTo(0));
        Assert.That(operations.AddPivotStyle(styles, CreateTableStyle("CustomPivot", differentialId)), Is.EqualTo(1));
        Assert.That(operations.AddSlicerStyle(styles, CreateTableStyle("CustomSlicer", differentialId)), Is.EqualTo(0));
        Assert.That(operations.AddIndexedColor(styles, CreateRgb("FF010203")), Is.EqualTo(0));
        Assert.That(operations.AddThemeColor(styles, 5, -0.25), Is.EqualTo(0));
        XlsxResolvedCellStyle resolved = operations.ResolveCellFormat(styles, formatId);

        Assert.That(fontId, Is.EqualTo(0));
        Assert.That(sameFontId, Is.EqualTo(fontId));
        Assert.That(fillId, Is.EqualTo(0));
        Assert.That(borderId, Is.EqualTo(0));
        Assert.That(differentialId, Is.EqualTo(0));
        Assert.That(sameFormatId, Is.EqualTo(formatId));
        Assert.That(resolved.IsValid, Is.True, Diagnostics(resolved.Validation));
        Assert.That(resolved.Font.Name, Is.EqualTo("Aptos"));
        Assert.That(resolved.Fill.IsGradient, Is.True);
        Assert.That(resolved.Border.Bottom.Style, Is.EqualTo("double"));
        Assert.That(resolved.NumberFormatCode, Is.EqualTo("0.000"));
        Assert.That(resolved.Alignment.Horizontal, Is.EqualTo("center"));
        Assert.That(resolved.Protection.Locked, Is.False);

        XlsxStyleFont replacement = CreateFont("Consolas", "FF000000");
        XlsxValidationResult update = operations.UpdateFont(styles, fontId, replacement);
        XlsxStylesPartWriteResult write = new XlsxStylesPartWriter().WriteString(styles);
        XlsxStylesPart roundTrip = new XlsxStylesPartReader().ReadBytes(write.Bytes);

        Assert.That(update.IsValid, Is.True, Diagnostics(update));
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        Assert.That(roundTrip.TypedFonts[0].Name, Is.EqualTo("Consolas"));
        Assert.That(roundTrip.TypedFills[0].GradientStops.Count, Is.EqualTo(2));
        Assert.That(roundTrip.TypedBorders[0].Bottom.Style, Is.EqualTo("double"));
        Assert.That(roundTrip.TypedDifferentialFormats[0].HasFont, Is.True);
        Assert.That(roundTrip.NamedStyles[0].Name, Is.EqualTo("Custom"));
        Assert.That(roundTrip.TableStyles.Styles.Count, Is.EqualTo(2));
        Assert.That(roundTrip.TableStyles.SlicerStyles.Count, Is.EqualTo(1));
        Assert.That(roundTrip.IndexedColors.Count, Is.EqualTo(1));
        Assert.That(roundTrip.MostRecentlyUsedColors[0].Color.Theme, Is.EqualTo(5));
        Assert.That(operations.UpdateFont(styles, 20, replacement).IsValid, Is.False);
        Assert.That(operations.AddThemeColor(styles, 1, 2), Is.EqualTo(-1));
    }

    [Test]
    public void StyleBuilder_UsesTypedComponentsAndInternsEquivalentStyles()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        XlsxStyleBuilder builder = new XlsxStyleBuilder();
        IXlsxStyleBuilderOperations operations = new XlsxStyleBuilderOperations();
        operations.SetNumberFormat(builder, "#,##0.00");
        operations.SetTypedFont(builder, CreateFont("Aptos Display", "FF224466"));
        operations.SetTypedFill(builder, CreateGradientFill());
        operations.SetTypedBorder(builder, CreateBorder());
        XlsxStyleAlignment alignment = new XlsxStyleAlignment();
        alignment.Horizontal = "center";
        alignment.WrapText = true;
        alignment.HasWrapText = true;
        operations.SetTypedAlignment(builder, alignment);
        XlsxStyleProtection protection = new XlsxStyleProtection();
        protection.Locked = false;
        protection.HasLocked = true;
        operations.SetTypedProtection(builder, protection);

        int first = operations.AddToStylesPart(builder, styles);
        int second = operations.AddToStylesPart(builder, styles);
        XlsxResolvedCellStyle resolved = new XlsxStylesPartOperations().ResolveCellFormat(styles, first);

        Assert.That(first, Is.EqualTo(0));
        Assert.That(second, Is.EqualTo(first));
        Assert.That(styles.TypedFonts.Count, Is.EqualTo(1));
        Assert.That(styles.TypedFills.Count, Is.EqualTo(1));
        Assert.That(styles.TypedBorders.Count, Is.EqualTo(1));
        Assert.That(styles.CellFormats.Count, Is.EqualTo(1));
        Assert.That(resolved.HasAlignment, Is.True);
        Assert.That(resolved.Alignment.WrapText, Is.True);
        Assert.That(resolved.HasProtection, Is.True);
        Assert.That(resolved.Protection.Locked, Is.False);
    }

    static XlsxStyleFont CreateFont(string name, string rgb)
    {
        XlsxStyleFont font = new XlsxStyleFont();
        font.Name = name;
        font.Size = 11;
        font.HasSize = true;
        font.Bold = true;
        font.Color = CreateRgb(rgb);
        font.HasColor = true;
        return font;
    }

    static XlsxStyleFill CreateGradientFill()
    {
        XlsxStyleFill fill = new XlsxStyleFill();
        fill.IsGradient = true;
        fill.GradientType = "linear";
        fill.Degree = 90;
        fill.HasDegree = true;
        fill.GradientStops.Add(CreateStop(0, "FF000000"));
        fill.GradientStops.Add(CreateStop(1, "FFFFFFFF"));
        return fill;
    }

    static XlsxStyleGradientStop CreateStop(double position, string rgb)
    {
        XlsxStyleGradientStop stop = new XlsxStyleGradientStop();
        stop.Position = position;
        stop.Color = CreateRgb(rgb);
        return stop;
    }

    static XlsxStyleBorder CreateBorder()
    {
        XlsxStyleBorder border = new XlsxStyleBorder();
        border.Left.Style = "thin";
        border.Left.Color = CreateRgb("FF445566");
        border.Left.HasColor = true;
        border.Bottom.Style = "double";
        return border;
    }

    static XlsxSpreadsheetColor CreateRgb(string rgb)
    {
        XlsxSpreadsheetColor color = new XlsxSpreadsheetColor();
        color.Rgb = rgb;
        color.HasRgb = true;
        return color;
    }

    static void AddNumberFormat(XlsxStylesPart styles, uint id, string code)
    {
        XlsxNumberFormatDefinition format = new XlsxNumberFormatDefinition();
        format.NumberFormatId = id;
        format.HasNumberFormatId = true;
        format.FormatCode = code;
        styles.NumberFormats.Add(format);
    }

    static XlsxCellFormatDefinition CreateCellFormat(int fontId, int fillId, int borderId)
    {
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        format.NumberFormatId = 165;
        format.HasNumberFormatId = true;
        format.FontId = (uint)fontId;
        format.HasFontId = true;
        format.FillId = (uint)fillId;
        format.HasFillId = true;
        format.BorderId = (uint)borderId;
        format.HasBorderId = true;
        format.Alignment.Horizontal = "center";
        format.Alignment.WrapText = true;
        format.Alignment.HasWrapText = true;
        format.HasAlignment = true;
        format.Protection.Locked = false;
        format.Protection.HasLocked = true;
        format.HasProtection = true;
        return format;
    }

    static XlsxNamedStyle CreateNamedStyle(string name, int formatId)
    {
        XlsxNamedStyle style = new XlsxNamedStyle();
        style.Name = name;
        style.FormatId = (uint)formatId;
        return style;
    }

    static XlsxTableStyleDefinition CreateTableStyle(string name, int differentialId)
    {
        XlsxTableStyleDefinition style = new XlsxTableStyleDefinition();
        style.Name = name;
        XlsxTableStyleElement element = new XlsxTableStyleElement();
        element.Type = "wholeTable";
        element.DifferentialFormatId = (uint)differentialId;
        style.Elements.Add(element);
        return style;
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        List<string> messages = new List<string>();
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            messages.Add(diagnostic.Code + ": " + diagnostic.Message);
        }
        return string.Join(" | ", messages);
    }
}
