using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ValueStyleFunctionalCompletionTests
{
    [Test]
    public void TypedStyleInternAndUpdate_IsolateCallerOwnedNestedModels()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        IXlsxStylesPartOperations operations = new XlsxStylesPartOperations();
        XlsxStyleFont font = CreateFont("Aptos", "FF112233");
        font.UnknownXml.Add("<future xmlns=\"urn:test-style\" value=\"kept\" />");
        XlsxStyleFill fill = CreateFill("FF445566");
        XlsxStyleBorder border = CreateBorder("FF778899");
        XlsxDifferentialStyle differential = new XlsxDifferentialStyle();
        differential.Font = CreateFont("Differential", "FFABCDEF");
        differential.HasFont = true;

        int fontId = operations.InternFont(styles, font);
        int fillId = operations.InternFill(styles, fill);
        int borderId = operations.InternBorder(styles, border);
        int differentialId = operations.InternDifferentialStyle(styles, differential);
        XlsxCellFormatDefinition format = CreateFormat(fontId, fillId, borderId, "center");
        int formatId = operations.InternCellFormat(styles, format);

        font.Name = "Mutated";
        font.Color.Rgb = "FFFFFFFF";
        font.UnknownXml.Clear();
        fill.GradientStops[0].Position = 0.75d;
        fill.GradientStops[0].Color.Rgb = "FFFFFFFF";
        border.Left.Style = "double";
        border.Left.Color.Rgb = "FFFFFFFF";
        differential.Font.Name = "Mutated differential";
        format.FontId = 99;
        format.Alignment.Horizontal = "right";

        Assert.That(styles.TypedFonts[fontId].Name, Is.EqualTo("Aptos"));
        Assert.That(styles.TypedFonts[fontId].Color.Rgb, Is.EqualTo("FF112233"));
        Assert.That(styles.TypedFonts[fontId].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(styles.TypedFills[fillId].GradientStops[0].Position, Is.EqualTo(0d));
        Assert.That(styles.TypedFills[fillId].GradientStops[0].Color.Rgb, Is.EqualTo("FF445566"));
        Assert.That(styles.TypedBorders[borderId].Left.Style, Is.EqualTo("thin"));
        Assert.That(styles.TypedBorders[borderId].Left.Color.Rgb, Is.EqualTo("FF778899"));
        Assert.That(styles.TypedDifferentialFormats[differentialId].Font.Name, Is.EqualTo("Differential"));
        Assert.That(styles.CellFormats[formatId].FontId, Is.EqualTo((uint)fontId));
        Assert.That(styles.CellFormats[formatId].Alignment.Horizontal, Is.EqualTo("center"));

        XlsxStyleFont replacementFont = CreateFont("Consolas", "FF010203");
        replacementFont.UnknownXml.Add("<future xmlns=\"urn:test-style\" value=\"updated\" />");
        XlsxStyleFill replacementFill = CreateFill("FF040506");
        XlsxStyleBorder replacementBorder = CreateBorder("FF070809");
        XlsxCellFormatDefinition replacementFormat = CreateFormat(fontId, fillId, borderId, "left");
        Assert.That(operations.UpdateFont(styles, fontId, replacementFont).IsValid, Is.True);
        Assert.That(operations.UpdateFill(styles, fillId, replacementFill).IsValid, Is.True);
        Assert.That(operations.UpdateBorder(styles, borderId, replacementBorder).IsValid, Is.True);
        Assert.That(operations.UpdateCellFormat(styles, formatId, replacementFormat).IsValid, Is.True);

        replacementFont.Name = "Changed after update";
        replacementFont.UnknownXml.Clear();
        replacementFill.GradientStops[0].Color.Rgb = "FFFFFFFF";
        replacementBorder.Left.Color.Rgb = "FFFFFFFF";
        replacementFormat.Alignment.Horizontal = "right";

        Assert.That(styles.TypedFonts[fontId].Name, Is.EqualTo("Consolas"));
        Assert.That(styles.TypedFonts[fontId].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(styles.TypedFills[fillId].GradientStops[0].Color.Rgb, Is.EqualTo("FF040506"));
        Assert.That(styles.TypedBorders[borderId].Left.Color.Rgb, Is.EqualTo("FF070809"));
        Assert.That(styles.CellFormats[formatId].Alignment.Horizontal, Is.EqualTo("left"));

        XlsxStylesPartWriteResult write = new XlsxStylesPartWriter().WriteString(styles);
        XlsxStylesPart roundTrip = new XlsxStylesPartReader().ReadBytes(write.Bytes);
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        Assert.That(write.Xml, Does.Contain("urn:test-style"));
        Assert.That(roundTrip.TypedFonts[fontId].Name, Is.EqualTo("Consolas"));
        Assert.That(roundTrip.TypedFills[fillId].GradientStops[0].Color.Rgb, Is.EqualTo("FF040506"));
    }

    [Test]
    public void ResolveAndAddStyleOperations_ExposeMutationIsolatedModels()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        IXlsxStylesPartOperations operations = new XlsxStylesPartOperations();
        int fontId = operations.InternFont(styles, CreateFont("Aptos", "FF112233"));
        int fillId = operations.InternFill(styles, CreateFill("FF445566"));
        int borderId = operations.InternBorder(styles, CreateBorder("FF778899"));
        int formatId = operations.InternCellFormat(styles, CreateFormat(fontId, fillId, borderId, "center"));

        XlsxResolvedCellStyle resolved = operations.ResolveCellFormat(styles, formatId);
        resolved.Font.Name = "Resolved mutation";
        resolved.Fill.GradientStops[0].Color.Rgb = "FFFFFFFF";
        resolved.Border.Left.Style = "double";
        resolved.CellFormat.FontId = 99;
        resolved.Alignment.Horizontal = "right";

        XlsxResolvedCellStyle resolvedAgain = operations.ResolveCellFormat(styles, formatId);
        Assert.That(resolvedAgain.IsValid, Is.True, Diagnostics(resolvedAgain.Validation));
        Assert.That(resolvedAgain.Font.Name, Is.EqualTo("Aptos"));
        Assert.That(resolvedAgain.Fill.GradientStops[0].Color.Rgb, Is.EqualTo("FF445566"));
        Assert.That(resolvedAgain.Border.Left.Style, Is.EqualTo("thin"));
        Assert.That(resolvedAgain.CellFormat.FontId, Is.EqualTo((uint)fontId));
        Assert.That(resolvedAgain.Alignment.Horizontal, Is.EqualTo("center"));

        XlsxNamedStyle named = new XlsxNamedStyle();
        named.Name = "Input";
        named.FormatId = (uint)formatId;
        XlsxTableStyleDefinition table = CreateTableStyle("TableInput");
        XlsxTableStyleDefinition pivot = CreateTableStyle("PivotInput");
        XlsxTableStyleDefinition slicer = CreateTableStyle("SlicerInput");
        XlsxSpreadsheetColor color = CreateColor("FF0A0B0C");
        operations.AddNamedStyle(styles, named);
        operations.AddTableStyle(styles, table);
        operations.AddPivotStyle(styles, pivot);
        operations.AddSlicerStyle(styles, slicer);
        operations.AddIndexedColor(styles, color);

        named.Name = "Changed";
        table.Elements[0].Type = "changed";
        pivot.Name = "Changed";
        slicer.Elements.Clear();
        color.Rgb = "FFFFFFFF";

        Assert.That(styles.NamedStyles[0].Name, Is.EqualTo("Input"));
        Assert.That(styles.TableStyles.Styles[0].Elements[0].Type, Is.EqualTo("wholeTable"));
        Assert.That(styles.TableStyles.Styles[1].Name, Is.EqualTo("PivotInput"));
        Assert.That(styles.TableStyles.SlicerStyles[0].Elements.Count, Is.EqualTo(1));
        Assert.That(styles.IndexedColors[0].Color.Rgb, Is.EqualTo("FF0A0B0C"));
    }

    [Test]
    public void TypedBorderReader_DoesNotEnableAbsentBooleanAttributes()
    {
        string xml = "<styleSheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><borders count=\"2\"><border><left style=\"thin\" /></border><border diagonalUp=\"0\" outline=\"1\"><right style=\"double\" /></border></borders></styleSheet>";

        XlsxStylesPart styles = new XlsxStylesPartReader().ReadString(xml);
        XlsxStylesPartWriteResult write = new XlsxStylesPartWriter().WriteString(styles);
        XlsxStylesPart roundTrip = new XlsxStylesPartReader().ReadBytes(write.Bytes);

        Assert.That(styles.IsValid, Is.True, Diagnostics(styles.Validation));
        Assert.That(styles.TypedBorders[0].DiagonalUp, Is.False);
        Assert.That(styles.TypedBorders[0].DiagonalDown, Is.False);
        Assert.That(styles.TypedBorders[0].Outline, Is.False);
        Assert.That(styles.TypedBorders[1].DiagonalUp, Is.False);
        Assert.That(styles.TypedBorders[1].DiagonalDown, Is.False);
        Assert.That(styles.TypedBorders[1].Outline, Is.True);
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        Assert.That(roundTrip.TypedBorders[0].Outline, Is.False);
        Assert.That(roundTrip.TypedBorders[1].Outline, Is.True);
    }

    static XlsxStyleFont CreateFont(string name, string rgb)
    {
        XlsxStyleFont font = new XlsxStyleFont();
        font.Name = name;
        font.Color = CreateColor(rgb);
        font.HasColor = true;
        return font;
    }

    static XlsxStyleFill CreateFill(string rgb)
    {
        XlsxStyleFill fill = new XlsxStyleFill();
        fill.IsGradient = true;
        fill.GradientType = "linear";
        XlsxStyleGradientStop first = new XlsxStyleGradientStop();
        first.Position = 0d;
        first.Color = CreateColor(rgb);
        fill.GradientStops.Add(first);
        XlsxStyleGradientStop second = new XlsxStyleGradientStop();
        second.Position = 1d;
        second.Color = CreateColor("FFFFFFFF");
        fill.GradientStops.Add(second);
        return fill;
    }

    static XlsxStyleBorder CreateBorder(string rgb)
    {
        XlsxStyleBorder border = new XlsxStyleBorder();
        border.Left.Style = "thin";
        border.Left.Color = CreateColor(rgb);
        border.Left.HasColor = true;
        return border;
    }

    static XlsxCellFormatDefinition CreateFormat(int fontId, int fillId, int borderId, string horizontal)
    {
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        format.FontId = (uint)fontId;
        format.HasFontId = true;
        format.FillId = (uint)fillId;
        format.HasFillId = true;
        format.BorderId = (uint)borderId;
        format.HasBorderId = true;
        format.Alignment.Horizontal = horizontal;
        format.HasAlignment = true;
        format.Protection.Locked = true;
        format.Protection.HasLocked = true;
        format.HasProtection = true;
        return format;
    }

    static XlsxTableStyleDefinition CreateTableStyle(string name)
    {
        XlsxTableStyleDefinition style = new XlsxTableStyleDefinition();
        style.Name = name;
        XlsxTableStyleElement element = new XlsxTableStyleElement();
        element.Type = "wholeTable";
        element.DifferentialFormatId = 0;
        style.Elements.Add(element);
        return style;
    }

    static XlsxSpreadsheetColor CreateColor(string rgb)
    {
        XlsxSpreadsheetColor color = new XlsxSpreadsheetColor();
        color.Rgb = rgb;
        color.HasRgb = true;
        return color;
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        string text = "";
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            text = text + diagnostic.Code + ": " + diagnostic.Message + "\n";
        }
        return text;
    }
}
