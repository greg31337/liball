using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxParagraphFormatTests
{
    [Test]
    public void Parse_ReadsCommonParagraphProperties()
    {
        global::docx.DocxParagraphFormat format = ParseFormat("<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pStyle w:val=\"Heading1\" /><w:jc w:val=\"center\" /><w:spacing w:before=\"120\" w:after=\"240\" w:line=\"360\" w:lineRule=\"auto\" /><w:ind w:left=\"720\" w:right=\"120\" w:firstLine=\"360\" w:hanging=\"180\" /><w:keepNext /><w:keepLines w:val=\"0\" /><w:pageBreakBefore w:val=\"true\" /><w:widowControl w:val=\"off\" /><w:numPr><w:ilvl w:val=\"2\" /><w:numId w:val=\"9\" /></w:numPr><w:outlineLvl w:val=\"1\" /></w:pPr>");

        Assert.That(format.HasAnyFormatting(), Is.True);
        Assert.That(format.StyleId, Is.EqualTo("Heading1"));
        Assert.That(format.Alignment, Is.EqualTo("center"));
        Assert.That(format.SpacingBefore, Is.EqualTo("120"));
        Assert.That(format.SpacingAfter, Is.EqualTo("240"));
        Assert.That(format.SpacingLine, Is.EqualTo("360"));
        Assert.That(format.SpacingLineRule, Is.EqualTo("auto"));
        Assert.That(format.IndentLeft, Is.EqualTo("720"));
        Assert.That(format.IndentRight, Is.EqualTo("120"));
        Assert.That(format.FirstLine, Is.EqualTo("360"));
        Assert.That(format.Hanging, Is.EqualTo("180"));
        Assert.That(format.HasKeepNext, Is.True);
        Assert.That(format.KeepNext, Is.True);
        Assert.That(format.HasKeepLines, Is.True);
        Assert.That(format.KeepLines, Is.False);
        Assert.That(format.HasPageBreakBefore, Is.True);
        Assert.That(format.PageBreakBefore, Is.True);
        Assert.That(format.HasWidowControl, Is.True);
        Assert.That(format.WidowControl, Is.False);
        Assert.That(format.NumberingLevel, Is.EqualTo("2"));
        Assert.That(format.NumberingId, Is.EqualTo("9"));
        Assert.That(format.OutlineLevel, Is.EqualTo("1"));
    }

    [Test]
    public void ParagraphParsing_ExposesFormatFromProperties()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:pStyle w:val=\"Quote\" /><w:jc w:val=\"both\" /></w:pPr><w:r><w:t>Text</w:t></w:r></w:p>");

        Assert.That(paragraph.HasProperties(), Is.True);
        Assert.That(paragraph.HasFormat(), Is.True);
        Assert.That(paragraph.Format.StyleId, Is.EqualTo("Quote"));
        Assert.That(paragraph.Format.Alignment, Is.EqualTo("both"));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
    }

    [Test]
    public void Parse_HandlesEmptyAndMissingParagraphProperties()
    {
        global::docx.DocxParagraph noProperties = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Text</w:t></w:r></w:p>");
        global::docx.DocxParagraphFormat emptyProperties = global::docx.DocxParagraphFormat.ParseXml("<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxParagraphFormat missingProperties = global::docx.DocxParagraphFormat.ParseXml("<root />");

        Assert.That(noProperties.HasFormat(), Is.False);
        Assert.That(emptyProperties.HasAnyFormatting(), Is.False);
        Assert.That(missingProperties.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void SetPropertiesXml_UpdatesParsedParagraphFormat()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.SetPropertiesXml("<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:spacing w:before=\"100\" /><w:numPr><w:numId w:val=\"3\" /></w:numPr></w:pPr>");

        Assert.That(paragraph.HasFormat(), Is.True);
        Assert.That(paragraph.Format.SpacingBefore, Is.EqualTo("100"));
        Assert.That(paragraph.Format.NumberingId, Is.EqualTo("3"));
    }

    [Test]
    public void WriteXml_WritesCommonParagraphProperties()
    {
        global::docx.DocxParagraphFormat format = new global::docx.DocxParagraphFormat();

        format.SetStyleId("Heading1");
        format.SetAlignment("center");
        format.SetSpacing("120", "240", "360", "auto");
        format.SetIndent("720", "120", "360", "180");
        format.SetKeepNext(true);
        format.SetKeepLines(false);
        format.SetPageBreakBefore(true);
        format.SetWidowControl(false);
        format.SetNumbering("2", "9");
        format.SetOutlineLevel("1");

        global::docx.DocxParagraphFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.StyleId, Is.EqualTo("Heading1"));
        Assert.That(parsed.Alignment, Is.EqualTo("center"));
        Assert.That(parsed.SpacingBefore, Is.EqualTo("120"));
        Assert.That(parsed.SpacingAfter, Is.EqualTo("240"));
        Assert.That(parsed.SpacingLine, Is.EqualTo("360"));
        Assert.That(parsed.SpacingLineRule, Is.EqualTo("auto"));
        Assert.That(parsed.IndentLeft, Is.EqualTo("720"));
        Assert.That(parsed.IndentRight, Is.EqualTo("120"));
        Assert.That(parsed.FirstLine, Is.EqualTo("360"));
        Assert.That(parsed.Hanging, Is.EqualTo("180"));
        Assert.That(parsed.KeepNext, Is.True);
        Assert.That(parsed.KeepLines, Is.False);
        Assert.That(parsed.PageBreakBefore, Is.True);
        Assert.That(parsed.WidowControl, Is.False);
        Assert.That(parsed.NumberingLevel, Is.EqualTo("2"));
        Assert.That(parsed.NumberingId, Is.EqualTo("9"));
        Assert.That(parsed.OutlineLevel, Is.EqualTo("1"));
    }

    [Test]
    public void WriteXml_WritesPartialSpacingAndIndentAttributesOnly()
    {
        global::docx.DocxParagraphFormat format = new global::docx.DocxParagraphFormat();

        format.SetSpacing("", "240", "", "");
        format.SetIndent("720", "", "", "360");

        string xml = format.WriteXml();
        global::docx.DocxParagraphFormat parsed = ParseFormat(xml);

        Assert.That(parsed.SpacingBefore, Is.EqualTo(""));
        Assert.That(parsed.SpacingAfter, Is.EqualTo("240"));
        Assert.That(parsed.SpacingLine, Is.EqualTo(""));
        Assert.That(parsed.SpacingLineRule, Is.EqualTo(""));
        Assert.That(parsed.IndentLeft, Is.EqualTo("720"));
        Assert.That(parsed.IndentRight, Is.EqualTo(""));
        Assert.That(parsed.FirstLine, Is.EqualTo(""));
        Assert.That(parsed.Hanging, Is.EqualTo("360"));
        Assert.That(xml, Does.Not.Contain("w:before"));
        Assert.That(xml, Does.Not.Contain("w:line=\""));
        Assert.That(xml, Does.Not.Contain("w:right"));
        Assert.That(xml, Does.Not.Contain("w:firstLine"));
    }

    [Test]
    public void ParagraphWriting_WritesStructuredFormatWhenNoRawProperties()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.Format.SetStyleId("Quote");
        paragraph.Format.SetAlignment("both");
        paragraph.AddChild(new global::docx.DocxXmlBuffer("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Text</w:t></w:r>"));

        string xml = paragraph.WriteXml();
        global::docx.DocxParagraph parsed = ParseParagraph(xml);

        Assert.That(xml.IndexOf("<w:pPr", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<w:r", StringComparison.Ordinal)));
        Assert.That(parsed.HasFormat(), Is.True);
        Assert.That(parsed.Format.StyleId, Is.EqualTo("Quote"));
        Assert.That(parsed.Format.Alignment, Is.EqualTo("both"));
    }

    [Test]
    public void SetFormat_ReplacesRawPropertiesForWriting()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:jc w:val=\"center\" /></w:pPr><w:r><w:t>Text</w:t></w:r></w:p>");
        global::docx.DocxParagraphFormat format = new global::docx.DocxParagraphFormat();

        format.SetStyleId("BodyText");
        paragraph.SetFormat(format);

        string xml = paragraph.WriteXml();
        global::docx.DocxParagraph parsed = ParseParagraph(xml);

        Assert.That(xml, Does.Contain("<w:pStyle"));
        Assert.That(xml, Does.Not.Contain("<w:jc"));
        Assert.That(parsed.Format.StyleId, Is.EqualTo("BodyText"));
        Assert.That(parsed.Format.Alignment, Is.EqualTo(""));
    }

    [Test]
    public void ParagraphNumbering_AppliesClearsAndWritesNumberingProperties()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:jc w:val=\"center\" /></w:pPr><w:r><w:t>Item</w:t></w:r></w:p>");

        paragraph.ApplyNumbering("9", "2");

        Assert.That(paragraph.HasProperties(), Is.False);
        Assert.That(paragraph.HasNumbering(), Is.True);
        Assert.That(paragraph.Format.NumberingId, Is.EqualTo("9"));
        Assert.That(paragraph.Format.NumberingLevel, Is.EqualTo("2"));

        global::docx.DocxParagraph parsed = ParseParagraph(paragraph.WriteXml());

        Assert.That(parsed.Format.Alignment, Is.EqualTo("center"));
        Assert.That(parsed.Format.NumberingId, Is.EqualTo("9"));
        Assert.That(parsed.Format.NumberingLevel, Is.EqualTo("2"));

        paragraph.ClearNumbering();

        Assert.That(paragraph.HasNumbering(), Is.False);
        Assert.That(paragraph.Format.NumberingId, Is.EqualTo(""));
        Assert.That(paragraph.Format.NumberingLevel, Is.EqualTo(""));
    }

    [Test]
    public void ClearToggleFormatting_RemovesTogglesFromWrittenProperties()
    {
        global::docx.DocxParagraphFormat format = new global::docx.DocxParagraphFormat();

        format.SetKeepNext(true);
        format.ClearKeepNext();
        format.SetKeepLines(true);
        format.ClearKeepLines();
        format.SetPageBreakBefore(true);
        format.ClearPageBreakBefore();
        format.SetWidowControl(true);
        format.ClearWidowControl();

        global::docx.DocxParagraphFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void Parse_ReadsCompleteParagraphFormattingModel()
    {
        global::docx.DocxParagraphFormat format = ParseFormat(
            "<w:pPr xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\">"
            + "<w:pStyle w:val=\"BodyText\"/><w:keepNext/><w:keepLines w:val=\"0\"/><w:pageBreakBefore/><w:widowControl w:val=\"false\"/>"
            + "<w:pBdr><w:top w:val=\"single\" w:color=\"112233\" w:sz=\"8\" w:space=\"2\"/></w:pBdr>"
            + "<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"EEEEEE\"/>"
            + "<w:tabs><w:tab w:val=\"left\" w:pos=\"720\"/><w:tab w:val=\"decimal\" w:pos=\"1440\" w:leader=\"dot\"/><w:tab w:val=\"clear\" w:pos=\"2160\"/></w:tabs>"
            + "<w:bidi w:val=\"0\"/><w:spacing w:before=\"120\" w:beforeLines=\"50\" w:beforeAutospacing=\"1\" w:after=\"240\" w:afterLines=\"75\" w:afterAutospacing=\"0\" w:line=\"360\" w:lineRule=\"auto\"/>"
            + "<w:ind w:left=\"720\" w:right=\"240\" w:start=\"800\" w:end=\"320\" w:leftChars=\"100\" w:rightChars=\"50\" w:startChars=\"110\" w:endChars=\"60\" w:firstLine=\"360\" w:firstLineChars=\"75\" w:hanging=\"180\" w:hangingChars=\"25\"/>"
            + "<w:contextualSpacing/><w:jc w:val=\"both\"/><w:textDirection w:val=\"tbRl\"/><w:outlineLvl w:val=\"2\"/>"
            + "</w:pPr>");

        Assert.That(format.StyleId, Is.EqualTo("BodyText"));
        Assert.That(format.HasKeepNext && format.KeepNext, Is.True);
        Assert.That(format.HasKeepLines && !format.KeepLines, Is.True);
        Assert.That(format.HasPageBreakBefore && format.PageBreakBefore, Is.True);
        Assert.That(format.HasWidowControl && !format.WidowControl, Is.True);
        Assert.That(format.Borders.TopVal, Is.EqualTo("single"));
        Assert.That(format.Borders.TopColor, Is.EqualTo("112233"));
        Assert.That(format.Shading.Fill, Is.EqualTo("EEEEEE"));
        Assert.That(format.Tabs.Select(tab => tab.Alignment), Is.EqualTo(new string[] { "left", "decimal", "clear" }));
        Assert.That(format.Tabs.Select(tab => tab.Position), Is.EqualTo(new string[] { "720", "1440", "2160" }));
        Assert.That(format.Tabs[1].Leader, Is.EqualTo("dot"));
        Assert.That(format.HasBidirectional && !format.Bidirectional, Is.True);
        Assert.That(format.SpacingBefore, Is.EqualTo("120"));
        Assert.That(format.SpacingBeforeLines, Is.EqualTo("50"));
        Assert.That(format.SpacingBeforeAuto, Is.EqualTo("1"));
        Assert.That(format.SpacingAfter, Is.EqualTo("240"));
        Assert.That(format.SpacingAfterLines, Is.EqualTo("75"));
        Assert.That(format.SpacingAfterAuto, Is.EqualTo("0"));
        Assert.That(format.SpacingLine, Is.EqualTo("360"));
        Assert.That(format.SpacingLineRule, Is.EqualTo("auto"));
        Assert.That(format.IndentLeft, Is.EqualTo("720"));
        Assert.That(format.IndentRight, Is.EqualTo("240"));
        Assert.That(format.IndentStart, Is.EqualTo("800"));
        Assert.That(format.IndentEnd, Is.EqualTo("320"));
        Assert.That(format.IndentLeftCharacters, Is.EqualTo("100"));
        Assert.That(format.IndentRightCharacters, Is.EqualTo("50"));
        Assert.That(format.IndentStartCharacters, Is.EqualTo("110"));
        Assert.That(format.IndentEndCharacters, Is.EqualTo("60"));
        Assert.That(format.FirstLine, Is.EqualTo("360"));
        Assert.That(format.FirstLineCharacters, Is.EqualTo("75"));
        Assert.That(format.Hanging, Is.EqualTo("180"));
        Assert.That(format.HangingCharacters, Is.EqualTo("25"));
        Assert.That(format.HasContextualSpacing && format.ContextualSpacing, Is.True);
        Assert.That(format.Alignment, Is.EqualTo("both"));
        Assert.That(format.TextDirection, Is.EqualTo("tbRl"));
        Assert.That(format.OutlineLevel, Is.EqualTo("2"));
    }

    [Test]
    public void PublicParagraphFormatPath_AppliesAndRoundTripsCompleteFormatting()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\"><w:pPr><w:jc w:val=\"left\"/></w:pPr><w:r><w:t>Text</w:t></w:r></w:p>");
        global::docx.IDocxParagraphModel model = paragraph;
        global::docx.DocxParagraphFormat format = model.GetFormat();
        format.StyleId = "Quote";
        format.Alignment = "center";
        format.SpacingBefore = "100";
        format.SpacingBeforeLines = "40";
        format.SpacingBeforeAuto = "0";
        format.SpacingAfter = "200";
        format.SpacingAfterLines = "60";
        format.SpacingAfterAuto = "1";
        format.SpacingLine = "300";
        format.SpacingLineRule = "exact";
        format.IndentLeft = "600";
        format.IndentRight = "180";
        format.IndentStart = "640";
        format.IndentEnd = "220";
        format.IndentLeftCharacters = "90";
        format.IndentRightCharacters = "40";
        format.IndentStartCharacters = "95";
        format.IndentEndCharacters = "45";
        format.FirstLine = "300";
        format.FirstLineCharacters = "65";
        format.Hanging = "150";
        format.HangingCharacters = "20";
        format.OutlineLevel = "3";
        format.HasKeepNext = true;
        format.KeepNext = false;
        format.HasKeepLines = true;
        format.KeepLines = true;
        format.HasPageBreakBefore = true;
        format.PageBreakBefore = false;
        format.HasWidowControl = true;
        format.WidowControl = true;
        format.HasContextualSpacing = true;
        format.ContextualSpacing = false;
        format.HasBidirectional = true;
        format.Bidirectional = true;
        format.TextDirection = "btLr";
        format.Borders.BottomVal = "double";
        format.Borders.BottomColor = "445566";
        format.Borders.BottomSize = "12";
        format.Shading.Val = "clear";
        format.Shading.Fill = "ABCDEF";
        format.Tabs.Add(new global::docx.DocxParagraphTab("right", "1080", "hyphen"));
        format.Tabs.Add(new global::docx.DocxParagraphTab("center", "1800", "underscore"));

        model.SetFormat(format);

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());
        global::docx.DocxParagraphFormat parsed = written.GetFormat();
        Assert.That(model.HasFormat(), Is.True);
        Assert.That(parsed.StyleId, Is.EqualTo("Quote"));
        Assert.That(parsed.Alignment, Is.EqualTo("center"));
        Assert.That(parsed.SpacingBeforeLines, Is.EqualTo("40"));
        Assert.That(parsed.SpacingAfterAuto, Is.EqualTo("1"));
        Assert.That(parsed.SpacingLineRule, Is.EqualTo("exact"));
        Assert.That(parsed.IndentStart, Is.EqualTo("640"));
        Assert.That(parsed.IndentEndCharacters, Is.EqualTo("45"));
        Assert.That(parsed.FirstLineCharacters, Is.EqualTo("65"));
        Assert.That(parsed.HangingCharacters, Is.EqualTo("20"));
        Assert.That(parsed.HasKeepNext && !parsed.KeepNext, Is.True);
        Assert.That(parsed.HasKeepLines && parsed.KeepLines, Is.True);
        Assert.That(parsed.HasPageBreakBefore && !parsed.PageBreakBefore, Is.True);
        Assert.That(parsed.HasWidowControl && parsed.WidowControl, Is.True);
        Assert.That(parsed.HasContextualSpacing && !parsed.ContextualSpacing, Is.True);
        Assert.That(parsed.HasBidirectional && parsed.Bidirectional, Is.True);
        Assert.That(parsed.TextDirection, Is.EqualTo("btLr"));
        Assert.That(parsed.Borders.BottomVal, Is.EqualTo("double"));
        Assert.That(parsed.Shading.Fill, Is.EqualTo("ABCDEF"));
        Assert.That(parsed.Tabs.Select(tab => tab.Alignment), Is.EqualTo(new string[] { "right", "center" }));
        Assert.That(parsed.Tabs.Select(tab => tab.Leader), Is.EqualTo(new string[] { "hyphen", "underscore" }));
        Assert.That(written.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void ParagraphFormatParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxParagraphFormat format = new global::docx.DocxParagraphFormat();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraphFormat.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraphFormat.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadProperty(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraphFormat.ReadToggleValue(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetStyleId(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetAlignment(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetSpacing(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetSpacing("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetSpacing("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetSpacing("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetIndent(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetIndent("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetIndent("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetIndent("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetNumbering(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetNumbering("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetOutlineLevel(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ApplyNumbering((string)null!, "0"));
        Assert.Throws<ArgumentNullException>(() => paragraph.ApplyNumbering("1", null!));
        Assert.Throws<ArgumentException>(() => paragraph.ApplyNumbering("", "0"));
        Assert.Throws<ArgumentException>(() => paragraph.ApplyNumbering("1", ""));
        Assert.Throws<ArgumentNullException>(() => paragraph.ApplyNumberingInstance((global::docx.DocxNumberingInstance)null!, "0"));
        Assert.Throws<ArgumentNullException>(() => paragraph.ApplyNumberingInstance(new global::docx.DocxNumberingInstance(""), null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(null!, "pStyle", "val", "Body"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), null!, "val", "Body"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "pStyle", null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "pStyle", "val", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteToggleProperty(null!, "keepNext", true, true));
        Assert.Throws<ArgumentNullException>(() => format.WriteToggleProperty(global::docx.DocxXmlWriter.Create(), null!, true, true));
        Assert.Throws<ArgumentNullException>(() => format.WriteSpacingTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteIndentTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteNumberingTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(null!, "left", "720"));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "720"));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "left", null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.SetFormat(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxParagraphTab(null!, "720", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxParagraphTab("left", null!, ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxParagraphTab("left", "720", null!));
        Assert.Throws<FormatException>(() => global::docx.DocxParagraphFormat.ParseXml("<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:keepNext w:val=\"maybe\" /></w:pPr>"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxParagraphFormat.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadNumbering(reader));
    }

    static global::docx.DocxParagraphFormat ParseFormat(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("pPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraphFormat.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}
