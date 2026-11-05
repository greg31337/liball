using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxBordersShadingTests
{
    [Test]
    public void Borders_ParseReadsParagraphBorderCollection()
    {
        global::docx.DocxBorders borders = ParseBorders("<w:pBdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:top w:val=\"single\" w:color=\"FF0000\" w:sz=\"8\" w:space=\"1\" /><w:left w:val=\"dashed\" w:color=\"00FF00\" w:sz=\"6\" w:space=\"2\" /><w:bottom w:val=\"double\" w:color=\"0000FF\" w:sz=\"12\" w:space=\"3\" /><w:right w:val=\"nil\" /><w:between w:val=\"single\" w:color=\"auto\" /><w:bar w:val=\"single\" w:sz=\"4\" /></w:pBdr>");

        Assert.That(borders.HasAnyBorder(), Is.True);
        Assert.That(borders.TopVal, Is.EqualTo("single"));
        Assert.That(borders.TopColor, Is.EqualTo("FF0000"));
        Assert.That(borders.TopSize, Is.EqualTo("8"));
        Assert.That(borders.TopSpace, Is.EqualTo("1"));
        Assert.That(borders.LeftVal, Is.EqualTo("dashed"));
        Assert.That(borders.LeftColor, Is.EqualTo("00FF00"));
        Assert.That(borders.LeftSize, Is.EqualTo("6"));
        Assert.That(borders.LeftSpace, Is.EqualTo("2"));
        Assert.That(borders.BottomVal, Is.EqualTo("double"));
        Assert.That(borders.BottomColor, Is.EqualTo("0000FF"));
        Assert.That(borders.BottomSize, Is.EqualTo("12"));
        Assert.That(borders.BottomSpace, Is.EqualTo("3"));
        Assert.That(borders.RightVal, Is.EqualTo("nil"));
        Assert.That(borders.BetweenVal, Is.EqualTo("single"));
        Assert.That(borders.BetweenColor, Is.EqualTo("auto"));
        Assert.That(borders.BarVal, Is.EqualTo("single"));
        Assert.That(borders.BarSize, Is.EqualTo("4"));
    }

    [Test]
    public void Borders_ParseReadsTableInsideAndSingleBorder()
    {
        global::docx.DocxBorders tableBorders = ParseBorders("<w:tblBorders xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:insideH w:val=\"single\" w:color=\"CCCCCC\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"dotted\" w:color=\"999999\" w:sz=\"2\" w:space=\"0\" /></w:tblBorders>");
        global::docx.DocxBorders singleBorder = ParseBorders("<w:bdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:val=\"single\" w:color=\"111111\" w:sz=\"8\" w:space=\"1\" />");

        Assert.That(tableBorders.InsideHorizontalVal, Is.EqualTo("single"));
        Assert.That(tableBorders.InsideHorizontalColor, Is.EqualTo("CCCCCC"));
        Assert.That(tableBorders.InsideHorizontalSize, Is.EqualTo("4"));
        Assert.That(tableBorders.InsideHorizontalSpace, Is.EqualTo("0"));
        Assert.That(tableBorders.InsideVerticalVal, Is.EqualTo("dotted"));
        Assert.That(tableBorders.InsideVerticalColor, Is.EqualTo("999999"));
        Assert.That(singleBorder.SingleVal, Is.EqualTo("single"));
        Assert.That(singleBorder.SingleColor, Is.EqualTo("111111"));
        Assert.That(singleBorder.SingleSize, Is.EqualTo("8"));
        Assert.That(singleBorder.SingleSpace, Is.EqualTo("1"));
    }

    [Test]
    public void Shading_ParseReadsAttributes()
    {
        global::docx.DocxShading shading = ParseShading("<w:shd xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:val=\"clear\" w:color=\"auto\" w:fill=\"FFFF00\" w:themeColor=\"accent1\" w:themeFill=\"accent2\" w:themeTint=\"80\" w:themeFillTint=\"40\" w:themeShade=\"20\" w:themeFillShade=\"10\" />");

        Assert.That(shading.HasAnyShading(), Is.True);
        Assert.That(shading.Val, Is.EqualTo("clear"));
        Assert.That(shading.Color, Is.EqualTo("auto"));
        Assert.That(shading.Fill, Is.EqualTo("FFFF00"));
        Assert.That(shading.ThemeColor, Is.EqualTo("accent1"));
        Assert.That(shading.ThemeFill, Is.EqualTo("accent2"));
        Assert.That(shading.ThemeTint, Is.EqualTo("80"));
        Assert.That(shading.ThemeFillTint, Is.EqualTo("40"));
        Assert.That(shading.ThemeShade, Is.EqualTo("20"));
        Assert.That(shading.ThemeFillShade, Is.EqualTo("10"));
    }

    [Test]
    public void FormattingParsing_ExposesBordersAndShading()
    {
        global::docx.DocxParagraphFormat paragraph = global::docx.DocxParagraphFormat.ParseXml("<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pBdr><w:top w:val=\"single\" w:color=\"FF0000\" /></w:pBdr><w:shd w:fill=\"EEEEEE\" /></w:pPr>");
        global::docx.DocxRunFormat run = global::docx.DocxRunFormat.ParseXml("<w:rPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:bdr w:val=\"single\" w:sz=\"8\" /><w:shd w:val=\"clear\" w:fill=\"DDDDDD\" /></w:rPr>");
        global::docx.DocxTableFormat table = global::docx.DocxTableFormat.ParseXml("<w:tblPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblBorders><w:insideH w:val=\"single\" /></w:tblBorders><w:shd w:fill=\"CCCCCC\" /></w:tblPr>");
        global::docx.DocxSectionFormat section = global::docx.DocxSectionFormat.ParseXml("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pgBorders><w:bottom w:val=\"double\" /></w:pgBorders></w:sectPr>");

        Assert.That(paragraph.HasAnyFormatting(), Is.True);
        Assert.That(paragraph.Borders.TopVal, Is.EqualTo("single"));
        Assert.That(paragraph.Shading.Fill, Is.EqualTo("EEEEEE"));
        Assert.That(run.HasAnyFormatting(), Is.True);
        Assert.That(run.Borders.SingleVal, Is.EqualTo("single"));
        Assert.That(run.Shading.Fill, Is.EqualTo("DDDDDD"));
        Assert.That(table.HasAnyFormatting(), Is.True);
        Assert.That(table.Borders.InsideHorizontalVal, Is.EqualTo("single"));
        Assert.That(table.Shading.Fill, Is.EqualTo("CCCCCC"));
        Assert.That(section.HasAnyFormatting(), Is.True);
        Assert.That(section.PageBorders.BottomVal, Is.EqualTo("double"));
    }

    [Test]
    public void Parse_HandlesEmptyAndMissingBorderAndShadingProperties()
    {
        global::docx.DocxBorders emptyBorders = global::docx.DocxBorders.ParseXml("<w:pBdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxBorders missingBorders = global::docx.DocxBorders.ParseXml("<root />");
        global::docx.DocxShading emptyShading = global::docx.DocxShading.ParseXml("<w:shd xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxShading missingShading = global::docx.DocxShading.ParseXml("<root />");

        Assert.That(emptyBorders.HasAnyBorder(), Is.False);
        Assert.That(missingBorders.HasAnyBorder(), Is.False);
        Assert.That(emptyShading.HasAnyShading(), Is.False);
        Assert.That(missingShading.HasAnyShading(), Is.False);
    }

    [Test]
    public void Borders_WriteXml_RoundTripsCollectionAndSingleBorder()
    {
        global::docx.DocxBorders borders = new global::docx.DocxBorders();

        borders.SetTop("single", "FF0000", "8", "1");
        borders.SetLeft("dashed", "00FF00", "6", "2");
        borders.SetBottom("double", "0000FF", "12", "3");
        borders.SetRight("nil", "", "", "");
        borders.SetInsideHorizontal("single", "CCCCCC", "4", "0");
        borders.SetInsideVertical("dotted", "999999", "2", "0");
        borders.SetBetween("single", "auto", "", "");
        borders.SetBar("single", "", "4", "");

        global::docx.DocxBorders parsed = ParseBorders(borders.WriteXml("pBdr"));

        Assert.That(parsed.TopVal, Is.EqualTo("single"));
        Assert.That(parsed.LeftVal, Is.EqualTo("dashed"));
        Assert.That(parsed.BottomVal, Is.EqualTo("double"));
        Assert.That(parsed.RightVal, Is.EqualTo("nil"));
        Assert.That(parsed.InsideHorizontalVal, Is.EqualTo("single"));
        Assert.That(parsed.InsideVerticalVal, Is.EqualTo("dotted"));
        Assert.That(parsed.BetweenVal, Is.EqualTo("single"));
        Assert.That(parsed.BarVal, Is.EqualTo("single"));

        borders = new global::docx.DocxBorders();
        borders.SetSingle("single", "111111", "8", "1");

        parsed = ParseBorders(borders.WriteXml("bdr"));

        Assert.That(parsed.SingleVal, Is.EqualTo("single"));
        Assert.That(parsed.SingleColor, Is.EqualTo("111111"));
        Assert.That(parsed.SingleSize, Is.EqualTo("8"));
        Assert.That(parsed.SingleSpace, Is.EqualTo("1"));
    }

    [Test]
    public void Shading_WriteXml_RoundTripsAttributes()
    {
        global::docx.DocxShading shading = new global::docx.DocxShading();

        shading.SetShading("clear", "auto", "FFFF00");
        shading.SetTheme("accent1", "accent2", "80", "40", "20", "10");

        global::docx.DocxShading parsed = ParseShading(shading.WriteXml());

        Assert.That(parsed.Val, Is.EqualTo("clear"));
        Assert.That(parsed.Color, Is.EqualTo("auto"));
        Assert.That(parsed.Fill, Is.EqualTo("FFFF00"));
        Assert.That(parsed.ThemeColor, Is.EqualTo("accent1"));
        Assert.That(parsed.ThemeFill, Is.EqualTo("accent2"));
        Assert.That(parsed.ThemeTint, Is.EqualTo("80"));
        Assert.That(parsed.ThemeFillTint, Is.EqualTo("40"));
        Assert.That(parsed.ThemeShade, Is.EqualTo("20"));
        Assert.That(parsed.ThemeFillShade, Is.EqualTo("10"));
    }

    [Test]
    public void FormattingWriting_IncludesBordersAndShading()
    {
        global::docx.DocxParagraphFormat paragraph = new global::docx.DocxParagraphFormat();
        global::docx.DocxRunFormat run = new global::docx.DocxRunFormat();
        global::docx.DocxTableFormat table = new global::docx.DocxTableFormat();
        global::docx.DocxSectionFormat section = new global::docx.DocxSectionFormat();

        paragraph.Borders.SetTop("single", "FF0000", "8", "1");
        paragraph.Shading.SetShading("clear", "auto", "EEEEEE");
        run.Borders.SetSingle("single", "111111", "8", "1");
        run.Shading.SetShading("clear", "auto", "DDDDDD");
        table.Borders.SetInsideHorizontal("single", "CCCCCC", "4", "0");
        table.Shading.SetShading("clear", "auto", "CCCCCC");
        section.PageBorders.SetBottom("double", "0000FF", "12", "3");

        global::docx.DocxParagraphFormat parsedParagraph = global::docx.DocxParagraphFormat.ParseXml(paragraph.WriteXml());
        global::docx.DocxRunFormat parsedRun = global::docx.DocxRunFormat.ParseXml(run.WriteXml());
        global::docx.DocxTableFormat parsedTable = global::docx.DocxTableFormat.ParseXml(table.WriteXml());
        global::docx.DocxSectionFormat parsedSection = global::docx.DocxSectionFormat.ParseXml(section.WriteXml());

        Assert.That(parsedParagraph.Borders.TopVal, Is.EqualTo("single"));
        Assert.That(parsedParagraph.Shading.Fill, Is.EqualTo("EEEEEE"));
        Assert.That(parsedRun.Borders.SingleVal, Is.EqualTo("single"));
        Assert.That(parsedRun.Shading.Fill, Is.EqualTo("DDDDDD"));
        Assert.That(parsedTable.Borders.InsideHorizontalVal, Is.EqualTo("single"));
        Assert.That(parsedTable.Shading.Fill, Is.EqualTo("CCCCCC"));
        Assert.That(parsedSection.PageBorders.BottomVal, Is.EqualTo("double"));
    }

    [Test]
    public void BorderAndShadingParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxBorders borders = new global::docx.DocxBorders();
        global::docx.DocxShading shading = new global::docx.DocxShading();
        global::docx.DocxParagraphFormat paragraph = new global::docx.DocxParagraphFormat();
        global::docx.DocxRunFormat run = new global::docx.DocxRunFormat();
        global::docx.DocxTableFormat table = new global::docx.DocxTableFormat();
        global::docx.DocxSectionFormat section = new global::docx.DocxSectionFormat();

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ReadToSupportedRoot(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.IsSupportedRoot(null!));
        Assert.Throws<ArgumentNullException>(() => borders.ReadBorderElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ReadVal(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ReadColor(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ReadSize(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ReadSpace(null!));
        Assert.Throws<ArgumentNullException>(() => borders.SetSingle(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => borders.SetTop("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => borders.SetLeft("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => borders.SetBottom("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ValidateBorderValues(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.IsSupportedRootName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBorders.ValidateRootName(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxBorders.ValidateRootName("unknown"));
        Assert.Throws<ArgumentNullException>(() => borders.WriteXml(null!));
        Assert.Throws<ArgumentNullException>(() => borders.WriteTo(null!, "pBdr"));
        Assert.Throws<ArgumentException>(() => borders.WriteTo(global::docx.DocxXmlWriter.Create(), "unknown"));
        Assert.Throws<ArgumentNullException>(() => borders.WriteBorderElement(null!, "top", "single", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => borders.WriteBorderElement(global::docx.DocxXmlWriter.Create(), null!, "single", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => borders.WriteBorderElement(global::docx.DocxXmlWriter.Create(), "top", null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => borders.WriteOptionalAttribute(null!, "val", "single"));
        Assert.Throws<ArgumentNullException>(() => borders.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "single"));
        Assert.Throws<ArgumentNullException>(() => borders.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "val", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxShading.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxShading.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => shading.ReadAttributes(null!));
        Assert.Throws<ArgumentNullException>(() => shading.SetShading(null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetShading("", null!, ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetShading("", "", null!));
        Assert.Throws<ArgumentNullException>(() => shading.SetTheme(null!, "", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetTheme("", null!, "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetTheme("", "", null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetTheme("", "", "", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetTheme("", "", "", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => shading.SetTheme("", "", "", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => shading.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => shading.WriteOptionalAttribute(null!, "fill", "FFFF00"));
        Assert.Throws<ArgumentNullException>(() => shading.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "FFFF00"));
        Assert.Throws<ArgumentNullException>(() => shading.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "fill", null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.WriteBordersTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.WriteShadingTo(null!));
        Assert.Throws<ArgumentNullException>(() => run.WriteBordersTo(null!));
        Assert.Throws<ArgumentNullException>(() => run.WriteShadingTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WriteBordersTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WriteShadingTo(null!));
        Assert.Throws<ArgumentNullException>(() => section.WritePageBordersTo(null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.That(global::docx.DocxBorders.ReadToSupportedRoot(reader), Is.False);

        reader = global::docx.DocxXmlReader.Create("<root />");
        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxBorders.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxShading.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => shading.ReadAttributes(reader));
    }

    static global::docx.DocxBorders ParseBorders(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(global::docx.DocxBorders.ReadToSupportedRoot(reader), Is.True);

        return global::docx.DocxBorders.Parse(reader);
    }

    static global::docx.DocxShading ParseShading(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("shd", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxShading.Parse(reader);
    }
}
