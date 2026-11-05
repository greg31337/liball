using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRunFormatTests
{
    [Test]
    public void Parse_ReadsCommonRunProperties()
    {
        global::docx.DocxRunFormat format = ParseFormat("<w:rPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:rStyle w:val=\"Strong\" /><w:rFonts w:ascii=\"Aptos\" w:hAnsi=\"Aptos\" w:eastAsia=\"Yu Gothic\" w:cs=\"Arial\" /><w:b /><w:i w:val=\"0\" /><w:strike w:val=\"true\" /><w:u w:val=\"single\" /><w:color w:val=\"FF00AA\" /><w:highlight w:val=\"yellow\" /><w:sz w:val=\"24\" /></w:rPr>");

        Assert.That(format.HasAnyFormatting(), Is.True);
        Assert.That(format.StyleId, Is.EqualTo("Strong"));
        Assert.That(format.FontAscii, Is.EqualTo("Aptos"));
        Assert.That(format.FontHighAnsi, Is.EqualTo("Aptos"));
        Assert.That(format.FontEastAsia, Is.EqualTo("Yu Gothic"));
        Assert.That(format.FontComplexScript, Is.EqualTo("Arial"));
        Assert.That(format.HasBold, Is.True);
        Assert.That(format.Bold, Is.True);
        Assert.That(format.HasItalic, Is.True);
        Assert.That(format.Italic, Is.False);
        Assert.That(format.HasStrike, Is.True);
        Assert.That(format.Strike, Is.True);
        Assert.That(format.Underline, Is.EqualTo("single"));
        Assert.That(format.Color, Is.EqualTo("FF00AA"));
        Assert.That(format.Highlight, Is.EqualTo("yellow"));
        Assert.That(format.Size, Is.EqualTo("24"));
    }

    [Test]
    public void RunParsing_ExposesFormatFromRunProperties()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:rPr><w:b /><w:color w:val=\"336699\" /></w:rPr><w:t>Text</w:t></w:r>");

        Assert.That(run.HasProperties(), Is.True);
        Assert.That(run.HasFormat(), Is.True);
        Assert.That(run.Format.HasBold, Is.True);
        Assert.That(run.Format.Bold, Is.True);
        Assert.That(run.Format.Color, Is.EqualTo("336699"));
        Assert.That(run.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void Parse_HandlesEmptyAndMissingRunProperties()
    {
        global::docx.DocxRun noProperties = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Text</w:t></w:r>");
        global::docx.DocxRunFormat emptyProperties = global::docx.DocxRunFormat.ParseXml("<w:rPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxRunFormat missingProperties = global::docx.DocxRunFormat.ParseXml("<root />");

        Assert.That(noProperties.HasFormat(), Is.False);
        Assert.That(emptyProperties.HasAnyFormatting(), Is.False);
        Assert.That(missingProperties.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void SetPropertiesXml_UpdatesParsedRunFormat()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");

        run.SetPropertiesXml("<w:rPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:i /><w:sz w:val=\"28\" /></w:rPr>");

        Assert.That(run.HasFormat(), Is.True);
        Assert.That(run.Format.HasItalic, Is.True);
        Assert.That(run.Format.Italic, Is.True);
        Assert.That(run.Format.Size, Is.EqualTo("28"));
    }

    [Test]
    public void WriteXml_WritesCommonRunProperties()
    {
        global::docx.DocxRunFormat format = new global::docx.DocxRunFormat();

        format.SetStyleId("Strong");
        format.SetFonts("Aptos", "Aptos", "Yu Gothic", "Arial");
        format.SetBold(true);
        format.SetItalic(false);
        format.SetStrike(true);
        format.SetUnderline("single");
        format.SetColor("#ff00aa");
        format.SetHighlight("yellow");
        format.SetSize("24");

        global::docx.DocxRunFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.StyleId, Is.EqualTo("Strong"));
        Assert.That(parsed.FontAscii, Is.EqualTo("Aptos"));
        Assert.That(parsed.FontHighAnsi, Is.EqualTo("Aptos"));
        Assert.That(parsed.FontEastAsia, Is.EqualTo("Yu Gothic"));
        Assert.That(parsed.FontComplexScript, Is.EqualTo("Arial"));
        Assert.That(parsed.HasBold, Is.True);
        Assert.That(parsed.Bold, Is.True);
        Assert.That(parsed.HasItalic, Is.True);
        Assert.That(parsed.Italic, Is.False);
        Assert.That(parsed.HasStrike, Is.True);
        Assert.That(parsed.Strike, Is.True);
        Assert.That(parsed.Underline, Is.EqualTo("single"));
        Assert.That(parsed.Color, Is.EqualTo("FF00AA"));
        Assert.That(parsed.Highlight, Is.EqualTo("yellow"));
        Assert.That(parsed.Size, Is.EqualTo("24"));
    }

    [Test]
    public void WriteXml_WritesPartialFontsAndOmitsEmptyScalarProperties()
    {
        global::docx.DocxRunFormat format = new global::docx.DocxRunFormat();

        format.SetFonts("Aptos", "", "Yu Gothic", "");
        format.SetUnderline("");
        format.SetColor("#abcdef");
        format.SetHighlight("");
        format.SetSize("");

        string xml = format.WriteXml();
        global::docx.DocxRunFormat parsed = ParseFormat(xml);

        Assert.That(parsed.FontAscii, Is.EqualTo("Aptos"));
        Assert.That(parsed.FontHighAnsi, Is.EqualTo(""));
        Assert.That(parsed.FontEastAsia, Is.EqualTo("Yu Gothic"));
        Assert.That(parsed.FontComplexScript, Is.EqualTo(""));
        Assert.That(parsed.Color, Is.EqualTo("ABCDEF"));
        Assert.That(parsed.Underline, Is.EqualTo(""));
        Assert.That(parsed.Highlight, Is.EqualTo(""));
        Assert.That(parsed.Size, Is.EqualTo(""));
        Assert.That(xml, Does.Not.Contain("w:hAnsi"));
        Assert.That(xml, Does.Not.Contain("<w:u"));
        Assert.That(xml, Does.Not.Contain("<w:highlight"));
        Assert.That(xml, Does.Not.Contain("<w:sz"));
    }

    [Test]
    public void RunWriting_WritesStructuredFormatWhenNoRawProperties()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");

        run.Format.SetBold(true);
        run.Format.SetColor("336699");
        run.AddText(global::docx.DocxText.Create("Text"));

        string xml = run.WriteXml();
        global::docx.DocxRun parsed = ParseRun(xml);

        Assert.That(xml.IndexOf("<w:rPr", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<w:t", StringComparison.Ordinal)));
        Assert.That(parsed.HasFormat(), Is.True);
        Assert.That(parsed.Format.HasBold, Is.True);
        Assert.That(parsed.Format.Color, Is.EqualTo("336699"));
        Assert.That(parsed.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void SetFormat_ReplacesRawPropertiesForWriting()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:rPr><w:u w:val=\"single\" /></w:rPr><w:t>Text</w:t></w:r>");
        global::docx.DocxRunFormat format = new global::docx.DocxRunFormat();

        format.SetBold(true);
        run.SetFormat(format);

        string xml = run.WriteXml();
        global::docx.DocxRun parsed = ParseRun(xml);

        Assert.That(xml, Does.Contain("<w:b"));
        Assert.That(xml, Does.Not.Contain("<w:u"));
        Assert.That(parsed.Format.HasBold, Is.True);
        Assert.That(parsed.Format.Underline, Is.EqualTo(""));
    }

    [Test]
    public void ClearToggleFormatting_RemovesToggleFromWrittenProperties()
    {
        global::docx.DocxRunFormat format = new global::docx.DocxRunFormat();

        format.SetBold(true);
        format.ClearBold();
        format.SetItalic(true);
        format.ClearItalic();
        format.SetStrike(true);
        format.ClearStrike();

        global::docx.DocxRunFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void Parse_ReadsCompleteRunFormattingModel()
    {
        global::docx.DocxRunFormat format = ParseFormat(
            "<w:rPr xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\">"
            + "<w:rFonts w:ascii=\"Aptos\" w:hAnsi=\"Arial\" w:eastAsia=\"Yu Gothic\" w:cs=\"Traditional Arabic\" w:asciiTheme=\"majorAscii\" w:hAnsiTheme=\"majorHAnsi\" w:eastAsiaTheme=\"majorEastAsia\" w:cstheme=\"majorBidi\" w:hint=\"eastAsia\"/>"
            + "<w:b/><w:bCs w:val=\"0\"/><w:i w:val=\"false\"/><w:iCs/><w:caps/><w:smallCaps w:val=\"0\"/><w:strike/><w:dstrike w:val=\"false\"/>"
            + "<w:outline/><w:shadow w:val=\"0\"/><w:emboss/><w:imprint w:val=\"false\"/><w:vanish/><w:webHidden w:val=\"0\"/>"
            + "<w:color w:val=\"123456\" w:themeColor=\"accent1\" w:themeTint=\"80\" w:themeShade=\"40\"/>"
            + "<w:spacing w:val=\"20\"/><w:kern w:val=\"24\"/><w:position w:val=\"4\"/><w:sz w:val=\"28\"/><w:szCs w:val=\"30\"/><w:highlight w:val=\"green\"/>"
            + "<w:u w:val=\"double\" w:color=\"654321\" w:themeColor=\"accent2\" w:themeTint=\"70\" w:themeShade=\"30\"/>"
            + "<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"EEEEEE\"/><w:rtl/><w:cs w:val=\"0\"/><w:lang w:val=\"en-US\" w:eastAsia=\"ja-JP\" w:bidi=\"ar-SA\"/>"
            + "</w:rPr>");

        Assert.That(format.FontAscii, Is.EqualTo("Aptos"));
        Assert.That(format.FontHighAnsi, Is.EqualTo("Arial"));
        Assert.That(format.FontEastAsia, Is.EqualTo("Yu Gothic"));
        Assert.That(format.FontComplexScript, Is.EqualTo("Traditional Arabic"));
        Assert.That(format.FontAsciiTheme, Is.EqualTo("majorAscii"));
        Assert.That(format.FontHighAnsiTheme, Is.EqualTo("majorHAnsi"));
        Assert.That(format.FontEastAsiaTheme, Is.EqualTo("majorEastAsia"));
        Assert.That(format.FontComplexScriptTheme, Is.EqualTo("majorBidi"));
        Assert.That(format.FontHint, Is.EqualTo("eastAsia"));
        Assert.That(format.HasBold && format.Bold, Is.True);
        Assert.That(format.HasComplexScriptBold && !format.ComplexScriptBold, Is.True);
        Assert.That(format.HasItalic && !format.Italic, Is.True);
        Assert.That(format.HasComplexScriptItalic && format.ComplexScriptItalic, Is.True);
        Assert.That(format.HasCaps && format.Caps, Is.True);
        Assert.That(format.HasSmallCaps && !format.SmallCaps, Is.True);
        Assert.That(format.HasStrike && format.Strike, Is.True);
        Assert.That(format.HasDoubleStrike && !format.DoubleStrike, Is.True);
        Assert.That(format.HasOutline && format.Outline, Is.True);
        Assert.That(format.HasShadow && !format.Shadow, Is.True);
        Assert.That(format.HasEmboss && format.Emboss, Is.True);
        Assert.That(format.HasImprint && !format.Imprint, Is.True);
        Assert.That(format.HasVanish && format.Vanish, Is.True);
        Assert.That(format.HasHidden && !format.Hidden, Is.True);
        Assert.That(format.Color, Is.EqualTo("123456"));
        Assert.That(format.ColorTheme, Is.EqualTo("accent1"));
        Assert.That(format.ColorThemeTint, Is.EqualTo("80"));
        Assert.That(format.ColorThemeShade, Is.EqualTo("40"));
        Assert.That(format.CharacterSpacing, Is.EqualTo("20"));
        Assert.That(format.Kerning, Is.EqualTo("24"));
        Assert.That(format.Position, Is.EqualTo("4"));
        Assert.That(format.Size, Is.EqualTo("28"));
        Assert.That(format.SizeComplexScript, Is.EqualTo("30"));
        Assert.That(format.Highlight, Is.EqualTo("green"));
        Assert.That(format.Underline, Is.EqualTo("double"));
        Assert.That(format.UnderlineColor, Is.EqualTo("654321"));
        Assert.That(format.UnderlineThemeColor, Is.EqualTo("accent2"));
        Assert.That(format.UnderlineThemeTint, Is.EqualTo("70"));
        Assert.That(format.UnderlineThemeShade, Is.EqualTo("30"));
        Assert.That(format.Shading.Val, Is.EqualTo("clear"));
        Assert.That(format.Shading.Color, Is.EqualTo("auto"));
        Assert.That(format.Shading.Fill, Is.EqualTo("EEEEEE"));
        Assert.That(format.HasRightToLeft && format.RightToLeft, Is.True);
        Assert.That(format.HasComplexScript && !format.ComplexScript, Is.True);
        Assert.That(format.Language, Is.EqualTo("en-US"));
        Assert.That(format.LanguageEastAsia, Is.EqualTo("ja-JP"));
        Assert.That(format.LanguageBidirectional, Is.EqualTo("ar-SA"));
    }

    [Test]
    public void PublicRunFormatPath_AppliesAndRoundTripsCompleteFormatting()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\"><w:rPr><w:b/></w:rPr><w:t>Text</w:t></w:r>");
        global::docx.IDocxRunModel model = run;
        global::docx.DocxRunFormat format = model.GetFormat();
        format.HasDoubleStrike = true;
        format.DoubleStrike = true;
        format.HasCaps = true;
        format.Caps = false;
        format.HasSmallCaps = true;
        format.SmallCaps = true;
        format.HasHidden = true;
        format.Hidden = true;
        format.HasVanish = true;
        format.Vanish = false;
        format.HasEmboss = true;
        format.Emboss = true;
        format.HasImprint = true;
        format.Imprint = false;
        format.HasOutline = true;
        format.Outline = true;
        format.HasShadow = true;
        format.Shadow = false;
        format.HasRightToLeft = true;
        format.RightToLeft = true;
        format.HasComplexScript = true;
        format.ComplexScript = true;
        format.HasComplexScriptBold = true;
        format.ComplexScriptBold = false;
        format.HasComplexScriptItalic = true;
        format.ComplexScriptItalic = true;
        format.FontAscii = "Aptos";
        format.FontHighAnsi = "Arial";
        format.FontEastAsia = "Yu Gothic";
        format.FontComplexScript = "Traditional Arabic";
        format.FontAsciiTheme = "minorAscii";
        format.FontHighAnsiTheme = "minorHAnsi";
        format.FontEastAsiaTheme = "minorEastAsia";
        format.FontComplexScriptTheme = "minorBidi";
        format.FontHint = "cs";
        format.Color = "112233";
        format.ColorTheme = "accent3";
        format.ColorThemeTint = "66";
        format.ColorThemeShade = "22";
        format.Highlight = "yellow";
        format.Underline = "single";
        format.UnderlineColor = "445566";
        format.UnderlineThemeColor = "accent4";
        format.UnderlineThemeTint = "55";
        format.UnderlineThemeShade = "11";
        format.Size = "24";
        format.SizeComplexScript = "26";
        format.CharacterSpacing = "15";
        format.Position = "-2";
        format.Kerning = "20";
        format.Language = "fr-FR";
        format.LanguageEastAsia = "ko-KR";
        format.LanguageBidirectional = "he-IL";
        format.Shading.Val = "clear";
        format.Shading.Color = "auto";
        format.Shading.Fill = "ABCDEF";

        model.SetFormat(format);

        global::docx.DocxRunFormat parsed = ParseRun(run.WriteXml()).GetFormat();
        Assert.That(model.HasFormat(), Is.True);
        Assert.That(parsed.HasDoubleStrike && parsed.DoubleStrike, Is.True);
        Assert.That(parsed.HasCaps && !parsed.Caps, Is.True);
        Assert.That(parsed.HasSmallCaps && parsed.SmallCaps, Is.True);
        Assert.That(parsed.HasHidden && parsed.Hidden, Is.True);
        Assert.That(parsed.HasVanish && !parsed.Vanish, Is.True);
        Assert.That(parsed.HasEmboss && parsed.Emboss, Is.True);
        Assert.That(parsed.HasImprint && !parsed.Imprint, Is.True);
        Assert.That(parsed.HasOutline && parsed.Outline, Is.True);
        Assert.That(parsed.HasShadow && !parsed.Shadow, Is.True);
        Assert.That(parsed.HasRightToLeft && parsed.RightToLeft, Is.True);
        Assert.That(parsed.HasComplexScript && parsed.ComplexScript, Is.True);
        Assert.That(parsed.HasComplexScriptBold && !parsed.ComplexScriptBold, Is.True);
        Assert.That(parsed.HasComplexScriptItalic && parsed.ComplexScriptItalic, Is.True);
        Assert.That(parsed.FontAsciiTheme, Is.EqualTo("minorAscii"));
        Assert.That(parsed.FontComplexScriptTheme, Is.EqualTo("minorBidi"));
        Assert.That(parsed.ColorTheme, Is.EqualTo("accent3"));
        Assert.That(parsed.UnderlineThemeColor, Is.EqualTo("accent4"));
        Assert.That(parsed.SizeComplexScript, Is.EqualTo("26"));
        Assert.That(parsed.CharacterSpacing, Is.EqualTo("15"));
        Assert.That(parsed.Position, Is.EqualTo("-2"));
        Assert.That(parsed.Kerning, Is.EqualTo("20"));
        Assert.That(parsed.Language, Is.EqualTo("fr-FR"));
        Assert.That(parsed.LanguageEastAsia, Is.EqualTo("ko-KR"));
        Assert.That(parsed.LanguageBidirectional, Is.EqualTo("he-IL"));
        Assert.That(parsed.Shading.Fill, Is.EqualTo("ABCDEF"));
        Assert.That(ParseRun(run.WriteXml()).GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void RunFormatParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxRunFormat format = new global::docx.DocxRunFormat();
        global::docx.DocxRun run = new global::docx.DocxRun("");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRunFormat.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRunFormat.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadProperty(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRunFormat.ReadToggleValue(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetStyleId(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetFonts(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetFonts("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetFonts("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetFonts("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetUnderline(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetColor(null!));
        Assert.Throws<ArgumentException>(() => format.SetColor("xyz"));
        Assert.Throws<ArgumentNullException>(() => format.SetHighlight(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetSize(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(null!, "b", "val", "1"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), null!, "val", "1"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "b", null!, "1"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "b", "val", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteToggleProperty(null!, "b", true, true));
        Assert.Throws<ArgumentNullException>(() => format.WriteToggleProperty(global::docx.DocxXmlWriter.Create(), null!, true, true));
        Assert.Throws<ArgumentNullException>(() => format.WriteFontsTo(null!));
        Assert.Throws<ArgumentNullException>(() => run.SetFormat(null!));
        Assert.Throws<FormatException>(() => global::docx.DocxRunFormat.ParseXml("<w:rPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:b w:val=\"maybe\" /></w:rPr>"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxRunFormat.Parse(reader));
    }

    static global::docx.DocxRunFormat ParseFormat(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("rPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRunFormat.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }
}
