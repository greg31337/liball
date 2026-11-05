using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxStylesTests
{
    [Test]
    public void Styles_ParseCapturesDirectStyleElements()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:docDefaults />",
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\" w:default=\"1\" w:customStyle=\"0\"><w:name w:val=\"heading 1\" /><w:basedOn w:val=\"Normal\" /><w:next w:val=\"Normal\" /><w:link w:val=\"Heading1Char\" /></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"Heading1Char\"><w:name w:val=\"Heading 1 Char\" /></w:style>"
        }));

        Assert.That(styles.HasXml(), Is.True);
        Assert.That(styles.ChildCount(), Is.EqualTo(3));
        Assert.That(styles.StyleCount(), Is.EqualTo(2));
        Assert.That(styles.HasDocumentDefaults(), Is.True);
        Assert.That(styles.DocumentDefaults.HasXml(), Is.True);
        Assert.That(styles.CountChildElements("style", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(2));
        Assert.That(styles.CountChildElements("docDefaults", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));

        global::docx.DocxStyle style = styles.GetStyle(0);

        Assert.That(style.HasXml(), Is.True);
        Assert.That(style.HasIdentity(), Is.True);
        Assert.That(style.Type, Is.EqualTo("paragraph"));
        Assert.That(style.StyleId, Is.EqualTo("Heading1"));
        Assert.That(style.HasDefault, Is.True);
        Assert.That(style.Default, Is.True);
        Assert.That(style.HasCustomStyle, Is.True);
        Assert.That(style.CustomStyle, Is.False);
        Assert.That(style.Name, Is.EqualTo("heading 1"));
        Assert.That(style.BasedOn, Is.EqualTo("Normal"));
        Assert.That(style.Next, Is.EqualTo("Normal"));
        Assert.That(style.Link, Is.EqualTo("Heading1Char"));
    }

    [Test]
    public void Styles_ParseHandlesEmptyAndMissingRoots()
    {
        global::docx.DocxStyles emptyStyles = ParseStyles("<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxStyles missingStyles = new global::docx.DocxStyles("<root />");
        global::docx.DocxStyle missingStyle = new global::docx.DocxStyle("<root />");

        Assert.That(emptyStyles.StyleCount(), Is.EqualTo(0));
        Assert.That(emptyStyles.ChildCount(), Is.EqualTo(0));
        Assert.That(emptyStyles.HasDocumentDefaults(), Is.False);
        Assert.That(emptyStyles.HasLatentStyles(), Is.False);
        Assert.That(missingStyles.StyleCount(), Is.EqualTo(0));
        Assert.That(missingStyle.HasIdentity(), Is.False);
    }

    [Test]
    public void DocumentDefaults_ParseRunAndParagraphDefaults()
    {
        global::docx.DocxDocumentDefaults defaults = ParseDocumentDefaults("<w:docDefaults xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:rPrDefault><w:rPr><w:b /><w:rFonts w:ascii=\"Aptos\" /><w:color w:val=\"336699\" /></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:jc w:val=\"both\" /><w:spacing w:before=\"120\" w:after=\"240\" /><w:pBdr><w:bottom w:val=\"single\" /></w:pBdr></w:pPr></w:pPrDefault></w:docDefaults>");

        Assert.That(defaults.HasAnyDefaults(), Is.True);
        Assert.That(defaults.HasXml(), Is.True);
        Assert.That(defaults.HasRunFormat(), Is.True);
        Assert.That(defaults.HasParagraphFormat(), Is.True);
        Assert.That(defaults.RunPropertiesXml, Does.Contain("<w:rPr"));
        Assert.That(defaults.ParagraphPropertiesXml, Does.Contain("<w:pPr"));
        Assert.That(defaults.RunFormat.HasBold, Is.True);
        Assert.That(defaults.RunFormat.Bold, Is.True);
        Assert.That(defaults.RunFormat.FontAscii, Is.EqualTo("Aptos"));
        Assert.That(defaults.RunFormat.Color, Is.EqualTo("336699"));
        Assert.That(defaults.ParagraphFormat.Alignment, Is.EqualTo("both"));
        Assert.That(defaults.ParagraphFormat.SpacingBefore, Is.EqualTo("120"));
        Assert.That(defaults.ParagraphFormat.SpacingAfter, Is.EqualTo("240"));
        Assert.That(defaults.ParagraphFormat.Borders.BottomVal, Is.EqualTo("single"));
    }

    [Test]
    public void PublicDocumentDefaultsService_ReplacesDefaultsAndPreservesOtherMarkup()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:docDefaults><w:rPrDefault><w:rPr><w:b /><w:unknownRun w:val=\"retained-until-replaced\" /></w:rPr></w:rPrDefault><w:customDefault w:val=\"preserved\" /><w:pPrDefault><w:pPr><w:jc w:val=\"left\" /></w:pPr></w:pPrDefault></w:docDefaults>",
            "<w:latentStyles w:count=\"0\" />"
        }));
        global::docx.DocxParagraphFormat paragraphFormat = new global::docx.DocxParagraphFormat();
        global::docx.DocxRunFormat runFormat = new global::docx.DocxRunFormat();
        global::docx.IDocxDocumentDefaultsFormatting formatting = new global::docx.DocxDocumentDefaultsFormatting();

        paragraphFormat.Alignment = "both";
        paragraphFormat.SpacingBefore = "120";
        paragraphFormat.HasKeepNext = true;
        paragraphFormat.KeepNext = true;
        runFormat.HasItalic = true;
        runFormat.Italic = true;
        runFormat.FontAscii = "Aptos";
        runFormat.Size = "24";

        formatting.SetParagraphDefaults(styles, paragraphFormat);
        formatting.SetRunDefaults(styles, runFormat);

        string xml = styles.WriteXml();
        global::docx.DocxStyles parsed = ParseStyles(xml);

        Assert.That(formatting.HasParagraphDefaults(styles), Is.True);
        Assert.That(formatting.HasRunDefaults(styles), Is.True);
        Assert.That(formatting.GetParagraphDefaults(styles), Is.SameAs(paragraphFormat));
        Assert.That(formatting.GetRunDefaults(styles), Is.SameAs(runFormat));
        Assert.That(xml, Does.Contain("customDefault"));
        Assert.That(xml, Does.Contain("val=\"preserved\""));
        Assert.That(parsed.DocumentDefaults.ParagraphFormat.Alignment, Is.EqualTo("both"));
        Assert.That(parsed.DocumentDefaults.ParagraphFormat.SpacingBefore, Is.EqualTo("120"));
        Assert.That(parsed.DocumentDefaults.ParagraphFormat.KeepNext, Is.True);
        Assert.That(parsed.DocumentDefaults.RunFormat.Italic, Is.True);
        Assert.That(parsed.DocumentDefaults.RunFormat.FontAscii, Is.EqualTo("Aptos"));
        Assert.That(parsed.DocumentDefaults.RunFormat.Size, Is.EqualTo("24"));
    }

    [Test]
    public void PublicDocumentDefaultsService_AddsAndClearsDefaults()
    {
        global::docx.DocxStyles styles = new global::docx.DocxStyles("");
        global::docx.DocxParagraphFormat paragraphFormat = new global::docx.DocxParagraphFormat();
        global::docx.DocxRunFormat runFormat = new global::docx.DocxRunFormat();
        global::docx.IDocxDocumentDefaultsFormatting formatting = new global::docx.DocxDocumentDefaultsFormatting();

        paragraphFormat.Alignment = "center";
        runFormat.Color = "336699";
        formatting.SetParagraphDefaults(styles, paragraphFormat);
        formatting.SetRunDefaults(styles, runFormat);

        Assert.That(styles.WriteXml(), Does.Contain("docDefaults"));

        formatting.ClearParagraphDefaults(styles);
        formatting.ClearRunDefaults(styles);

        global::docx.DocxStyles parsed = ParseStyles(styles.WriteXml());

        Assert.That(formatting.HasParagraphDefaults(styles), Is.False);
        Assert.That(formatting.HasRunDefaults(styles), Is.False);
        Assert.That(parsed.DocumentDefaults.HasAnyDefaults(), Is.False);
    }

    [Test]
    public void PublicDocumentDefaultsService_RejectsInvalidArguments()
    {
        global::docx.IDocxDocumentDefaultsFormatting formatting = new global::docx.DocxDocumentDefaultsFormatting();
        global::docx.DocxStyles styles = new global::docx.DocxStyles("");

        Assert.Throws<ArgumentNullException>(() => formatting.HasParagraphDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetParagraphDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.SetParagraphDefaults(null!, new global::docx.DocxParagraphFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.SetParagraphDefaults(styles, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.ClearParagraphDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.HasRunDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRunDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.SetRunDefaults(null!, new global::docx.DocxRunFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.SetRunDefaults(styles, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.ClearRunDefaults(null!));
    }

    [Test]
    public void LatentStyles_ParseDefaultsAndExceptions()
    {
        global::docx.DocxLatentStyles latentStyles = ParseLatentStyles("<w:latentStyles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:defLockedState=\"0\" w:defUIPriority=\"99\" w:defSemiHidden=\"1\" w:defUnhideWhenUsed=\"1\" w:defQFormat=\"0\" w:count=\"376\"><w:lsdException w:name=\"Normal\" w:locked=\"0\" w:uiPriority=\"0\" w:semiHidden=\"0\" w:unhideWhenUsed=\"1\" w:qFormat=\"1\" /><w:lsdException w:name=\"heading 1\" w:locked=\"1\" /></w:latentStyles>");

        Assert.That(latentStyles.HasAnySettings(), Is.True);
        Assert.That(latentStyles.HasXml(), Is.True);
        Assert.That(latentStyles.HasDefaultLockedState, Is.True);
        Assert.That(latentStyles.DefaultLockedState, Is.False);
        Assert.That(latentStyles.DefaultUiPriority, Is.EqualTo("99"));
        Assert.That(latentStyles.HasDefaultSemiHidden, Is.True);
        Assert.That(latentStyles.DefaultSemiHidden, Is.True);
        Assert.That(latentStyles.HasDefaultUnhideWhenUsed, Is.True);
        Assert.That(latentStyles.DefaultUnhideWhenUsed, Is.True);
        Assert.That(latentStyles.HasDefaultQuickFormat, Is.True);
        Assert.That(latentStyles.DefaultQuickFormat, Is.False);
        Assert.That(latentStyles.Count, Is.EqualTo("376"));
        Assert.That(latentStyles.ChildCount(), Is.EqualTo(2));
        Assert.That(latentStyles.ExceptionCount(), Is.EqualTo(2));
        Assert.That(latentStyles.GetChildXml(0), Does.Contain("lsdException"));

        global::docx.DocxLatentStyleException normal = latentStyles.GetException(0);
        global::docx.DocxLatentStyleException heading = latentStyles.GetException(1);

        Assert.That(normal.HasAnySettings(), Is.True);
        Assert.That(normal.HasXml(), Is.True);
        Assert.That(normal.Name, Is.EqualTo("Normal"));
        Assert.That(normal.HasLocked, Is.True);
        Assert.That(normal.Locked, Is.False);
        Assert.That(normal.UiPriority, Is.EqualTo("0"));
        Assert.That(normal.HasSemiHidden, Is.True);
        Assert.That(normal.SemiHidden, Is.False);
        Assert.That(normal.HasUnhideWhenUsed, Is.True);
        Assert.That(normal.UnhideWhenUsed, Is.True);
        Assert.That(normal.HasQuickFormat, Is.True);
        Assert.That(normal.QuickFormat, Is.True);
        Assert.That(heading.Name, Is.EqualTo("heading 1"));
        Assert.That(heading.Locked, Is.True);
    }

    [Test]
    public void StylesParsing_ExposesDocumentDefaultsAndLatentStyles()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:docDefaults><w:rPrDefault><w:rPr><w:i /><w:sz w:val=\"22\" /></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:keepNext /><w:jc w:val=\"center\" /></w:pPr></w:pPrDefault></w:docDefaults>",
            "<w:latentStyles w:defUIPriority=\"99\" w:count=\"1\"><w:lsdException w:name=\"Normal\" w:qFormat=\"1\" /></w:latentStyles>",
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>"
        }));

        Assert.That(styles.ChildCount(), Is.EqualTo(3));
        Assert.That(styles.StyleCount(), Is.EqualTo(1));
        Assert.That(styles.HasDocumentDefaults(), Is.True);
        Assert.That(styles.DocumentDefaults.RunFormat.Italic, Is.True);
        Assert.That(styles.DocumentDefaults.RunFormat.Size, Is.EqualTo("22"));
        Assert.That(styles.DocumentDefaults.ParagraphFormat.KeepNext, Is.True);
        Assert.That(styles.DocumentDefaults.ParagraphFormat.Alignment, Is.EqualTo("center"));
        Assert.That(styles.HasLatentStyles(), Is.True);
        Assert.That(styles.LatentStyles.DefaultUiPriority, Is.EqualTo("99"));
        Assert.That(styles.LatentStyles.Count, Is.EqualTo("1"));
        Assert.That(styles.LatentStyles.GetException(0).Name, Is.EqualTo("Normal"));
        Assert.That(styles.LatentStyles.GetException(0).QuickFormat, Is.True);
        Assert.That(styles.GetStyle(0).StyleId, Is.EqualTo("Normal"));
    }

    [Test]
    public void StyleModels_ParseTypedFormatting()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Body\" /><w:pPr><w:jc w:val=\"both\" /><w:numPr><w:ilvl w:val=\"1\" /><w:numId w:val=\"7\" /></w:numPr></w:pPr><w:rPr><w:b /><w:color w:val=\"FF0000\" /></w:rPr></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"Emphasis\"><w:name w:val=\"Emphasis\" /><w:rPr><w:i /><w:shd w:fill=\"EEEEEE\" /></w:rPr></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"Grid\"><w:name w:val=\"Grid\" /><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\" /><w:tblBorders><w:insideH w:val=\"single\" /></w:tblBorders></w:tblPr></w:style>",
            "<w:style w:type=\"numbering\" w:styleId=\"ListStyle\"><w:name w:val=\"List Style\" /><w:numPr><w:ilvl w:val=\"0\" /><w:numId w:val=\"9\" /></w:numPr></w:style>"
        }));

        Assert.That(styles.ParagraphStyleCount(), Is.EqualTo(1));
        Assert.That(styles.CharacterStyleCount(), Is.EqualTo(1));
        Assert.That(styles.TableStyleCount(), Is.EqualTo(1));
        Assert.That(styles.NumberingStyleCount(), Is.EqualTo(1));

        global::docx.DocxStyle paragraph = styles.GetStyle(0);
        global::docx.DocxStyle character = styles.GetStyle(1);
        global::docx.DocxStyle table = styles.GetStyle(2);
        global::docx.DocxStyle numbering = styles.GetStyle(3);

        Assert.That(paragraph.IsParagraphStyle(), Is.True);
        Assert.That(paragraph.HasParagraphFormat(), Is.True);
        Assert.That(paragraph.HasRunFormat(), Is.True);
        Assert.That(paragraph.ParagraphFormat.Alignment, Is.EqualTo("both"));
        Assert.That(paragraph.RunFormat.Bold, Is.True);
        Assert.That(paragraph.RunFormat.Color, Is.EqualTo("FF0000"));
        Assert.That(paragraph.HasNumbering(), Is.True);
        Assert.That(paragraph.NumberingLevel, Is.EqualTo("1"));
        Assert.That(paragraph.NumberingId, Is.EqualTo("7"));

        Assert.That(character.IsCharacterStyle(), Is.True);
        Assert.That(character.HasRunFormat(), Is.True);
        Assert.That(character.RunFormat.Italic, Is.True);
        Assert.That(character.RunFormat.Shading.Fill, Is.EqualTo("EEEEEE"));

        Assert.That(table.IsTableStyle(), Is.True);
        Assert.That(table.HasTableFormat(), Is.True);
        Assert.That(table.TableFormat.Width, Is.EqualTo("5000"));
        Assert.That(table.TableFormat.WidthType, Is.EqualTo("pct"));
        Assert.That(table.TableFormat.Borders.InsideHorizontalVal, Is.EqualTo("single"));

        Assert.That(numbering.IsNumberingStyle(), Is.True);
        Assert.That(numbering.HasNumbering(), Is.True);
        Assert.That(numbering.NumberingLevel, Is.EqualTo("0"));
        Assert.That(numbering.NumberingId, Is.EqualTo("9"));
        Assert.That(numbering.NumberingPropertiesXml, Does.Contain("<w:numPr"));
    }

    [Test]
    public void StyleModel_PreservesDirectChildren()
    {
        global::docx.DocxStyle style = ParseStyle("<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Body\" /><w:pPr><w:jc w:val=\"center\" /></w:pPr><w:unknown marker=\"kept\" /></w:style>");

        Assert.That(style.ChildCount(), Is.EqualTo(3));
        Assert.That(style.GetChildXml(0), Does.Contain("<w:name"));
        Assert.That(style.GetChildXml(1), Does.Contain("<w:pPr"));
        Assert.That(style.GetChildXml(2), Does.Contain("marker=\"kept\""));
    }

    [Test]
    public void StylesLookup_FindsStylesByIdAndName()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"Strong\"><w:name w:val=\"Strong\" /></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"Grid\"><w:name w:val=\"Table Grid\" /></w:style>"
        }));

        Assert.That(styles.FindById("Normal"), Is.SameAs(styles.GetStyle(0)));
        Assert.That(styles.FindByName("Strong"), Is.SameAs(styles.GetStyle(1)));
        Assert.That(styles.ContainsStyleId("Grid"), Is.True);
        Assert.That(styles.ContainsStyleName("Table Grid"), Is.True);
        Assert.That(styles.ContainsStyleId("Missing"), Is.False);
        Assert.That(styles.ContainsStyleName("Missing"), Is.False);
    }

    [Test]
    public void StylesLookup_FiltersByType()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Shared Name\" /></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"Shared\"><w:name w:val=\"Shared Name\" /></w:style>"
        }));

        Assert.That(styles.FindById("Shared"), Is.SameAs(styles.GetStyle(0)));
        Assert.That(styles.FindByIdAndType("Shared", global::docx.DocxStyle.TypeParagraph), Is.SameAs(styles.GetStyle(0)));
        Assert.That(styles.FindByIdAndType("Shared", global::docx.DocxStyle.TypeCharacter), Is.SameAs(styles.GetStyle(1)));
        Assert.That(styles.FindByNameAndType("Shared Name", global::docx.DocxStyle.TypeParagraph), Is.SameAs(styles.GetStyle(0)));
        Assert.That(styles.FindByNameAndType("Shared Name", global::docx.DocxStyle.TypeTable), Is.Null);
    }

    [Test]
    public void DocumentLookup_DelegatesToParsedStyles()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"Grid\"><w:name w:val=\"Table Grid\" /></w:style>"
        }));
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.FindStyleById("Grid"), Is.SameAs(document.GetStyles().GetStyle(1)));
        Assert.That(document.FindStyleByName("Normal"), Is.SameAs(document.GetStyles().GetStyle(0)));
        Assert.That(document.FindStyleById("Missing"), Is.Null);
        Assert.That(document.FindStyleByName("Missing"), Is.Null);
    }

    [Test]
    public void StyleCreation_CreatesTypedStyleWithIdentity()
    {
        global::docx.DocxStyle style = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeParagraph, "Body", "Body Text");

        Assert.That(style.IsParagraphStyle(), Is.True);
        Assert.That(style.StyleId, Is.EqualTo("Body"));
        Assert.That(style.Name, Is.EqualTo("Body Text"));
        Assert.That(style.HasXml(), Is.False);
        Assert.That(style.HasIdentity(), Is.True);
    }

    [Test]
    public void StyleUpdate_UpdatesMetadataAndFormatting()
    {
        global::docx.DocxStyle style = ParseStyle("<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:type=\"paragraph\" w:styleId=\"Old\"><w:name w:val=\"Old Name\" /></w:style>");
        global::docx.DocxParagraphFormat paragraphFormat = new global::docx.DocxParagraphFormat();
        global::docx.DocxRunFormat runFormat = new global::docx.DocxRunFormat();
        global::docx.DocxTableFormat tableFormat = new global::docx.DocxTableFormat();

        paragraphFormat.SetAlignment("center");
        paragraphFormat.SetNumbering("2", "8");
        runFormat.SetBold(true);
        tableFormat.SetWidth("5000", "pct");

        style.SetIdentity(global::docx.DocxStyle.TypeTable, "Grid", "Table Grid");
        style.SetBasedOn("BaseGrid");
        style.SetNext("NextGrid");
        style.SetLink("GridChar");
        style.SetDefault(true);
        style.SetCustomStyle(false);
        style.SetParagraphFormat(paragraphFormat);
        style.SetRunFormat(runFormat);
        style.SetTableFormat(tableFormat);
        style.SetNumbering("3", "9");

        Assert.That(style.HasXml(), Is.False);
        Assert.That(style.IsTableStyle(), Is.True);
        Assert.That(style.StyleId, Is.EqualTo("Grid"));
        Assert.That(style.Name, Is.EqualTo("Table Grid"));
        Assert.That(style.BasedOn, Is.EqualTo("BaseGrid"));
        Assert.That(style.Next, Is.EqualTo("NextGrid"));
        Assert.That(style.Link, Is.EqualTo("GridChar"));
        Assert.That(style.HasDefault, Is.True);
        Assert.That(style.Default, Is.True);
        Assert.That(style.HasCustomStyle, Is.True);
        Assert.That(style.CustomStyle, Is.False);
        Assert.That(style.ParagraphFormat.Alignment, Is.EqualTo("center"));
        Assert.That(style.RunFormat.Bold, Is.True);
        Assert.That(style.TableFormat.Width, Is.EqualTo("5000"));
        Assert.That(style.NumberingLevel, Is.EqualTo("3"));
        Assert.That(style.NumberingId, Is.EqualTo("9"));

        style.ClearDefault();
        style.ClearCustomStyle();

        Assert.That(style.HasDefault, Is.False);
        Assert.That(style.HasCustomStyle, Is.False);
    }

    [Test]
    public void StyleWriting_PreservesRawXmlUntilMutated()
    {
        string rawXml = "<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Body\" /><w:custom marker=\"kept\" /><w:pPr><w:jc w:val=\"left\" /></w:pPr></w:style>";
        global::docx.DocxStyle style = ParseStyle(rawXml);

        Assert.That(style.WriteXml(), Is.EqualTo(rawXml));

        style.SetName("Body Text");
        string xml = style.WriteXml();
        global::docx.DocxStyle written = ParseStyle(xml);

        Assert.That(xml, Does.Contain("marker=\"kept\""));
        Assert.That(xml, Does.Contain("Body Text"));
        Assert.That(xml, Does.Not.Contain("w:val=\"Body\""));
        Assert.That(written.Name, Is.EqualTo("Body Text"));
        Assert.That(written.ParagraphFormat.Alignment, Is.EqualTo("left"));
        Assert.That(written.ChildCount(), Is.EqualTo(3));
    }

    [Test]
    public void StyleWriting_WritesCreatedStyleWithMetadataAndFormatting()
    {
        global::docx.DocxStyle style = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeParagraph, "Body", "Body Text");
        global::docx.DocxParagraphFormat paragraphFormat = new global::docx.DocxParagraphFormat();
        global::docx.DocxRunFormat runFormat = new global::docx.DocxRunFormat();

        paragraphFormat.SetAlignment("both");
        paragraphFormat.SetSpacing("120", "240", "360", "auto");
        paragraphFormat.SetNumbering("1", "7");
        runFormat.SetBold(true);
        runFormat.SetColor("445566");
        runFormat.SetSize("24");
        style.SetBasedOn("Normal");
        style.SetNext("Body");
        style.SetLink("BodyChar");
        style.SetDefault(true);
        style.SetCustomStyle(false);
        style.SetParagraphFormat(paragraphFormat);
        style.SetRunFormat(runFormat);

        string xml = style.WriteXml();
        global::docx.DocxStyle written = ParseStyle(xml);

        Assert.That(xml, Does.Contain("<w:style"));
        Assert.That(xml, Does.Contain("w:type=\"paragraph\""));
        Assert.That(xml, Does.Contain("w:styleId=\"Body\""));
        Assert.That(xml, Does.Contain("w:default=\"1\""));
        Assert.That(xml, Does.Contain("w:customStyle=\"0\""));
        Assert.That(written.Name, Is.EqualTo("Body Text"));
        Assert.That(written.BasedOn, Is.EqualTo("Normal"));
        Assert.That(written.Next, Is.EqualTo("Body"));
        Assert.That(written.Link, Is.EqualTo("BodyChar"));
        Assert.That(written.ParagraphFormat.Alignment, Is.EqualTo("both"));
        Assert.That(written.ParagraphFormat.SpacingBefore, Is.EqualTo("120"));
        Assert.That(written.NumberingLevel, Is.EqualTo("1"));
        Assert.That(written.NumberingId, Is.EqualTo("7"));
        Assert.That(written.RunFormat.Bold, Is.True);
        Assert.That(written.RunFormat.Color, Is.EqualTo("445566"));
        Assert.That(written.RunFormat.Size, Is.EqualTo("24"));
    }

    [Test]
    public void StyleWriting_WritesTableAndNumberingStyleProperties()
    {
        global::docx.DocxStyle tableStyle = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeTable, "Grid", "Table Grid");
        global::docx.DocxStyle numberingStyle = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeNumbering, "List", "List Style");
        global::docx.DocxTableFormat tableFormat = new global::docx.DocxTableFormat();

        tableFormat.SetWidth("5000", "pct");
        tableFormat.SetAlignment("center");
        tableStyle.SetTableFormat(tableFormat);
        numberingStyle.SetNumbering("0", "9");

        global::docx.DocxStyle writtenTable = ParseStyle(tableStyle.WriteXml());
        global::docx.DocxStyle writtenNumbering = ParseStyle(numberingStyle.WriteXml());

        Assert.That(writtenTable.IsTableStyle(), Is.True);
        Assert.That(writtenTable.TableFormat.Width, Is.EqualTo("5000"));
        Assert.That(writtenTable.TableFormat.WidthType, Is.EqualTo("pct"));
        Assert.That(writtenTable.TableFormat.Alignment, Is.EqualTo("center"));
        Assert.That(writtenNumbering.IsNumberingStyle(), Is.True);
        Assert.That(writtenNumbering.NumberingLevel, Is.EqualTo("0"));
        Assert.That(writtenNumbering.NumberingId, Is.EqualTo("9"));
    }

    [Test]
    public void StylesCreationAndReplacement_UpdateCollection()
    {
        global::docx.DocxStyles styles = global::docx.DocxStyles.Create();

        global::docx.DocxStyle created = styles.CreateStyle(global::docx.DocxStyle.TypeParagraph, "Body", "Body Text");
        global::docx.DocxStyle replacement = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeCharacter, "Body", "Body Character");
        global::docx.DocxStyle added = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeTable, "Grid", "Table Grid");

        Assert.That(styles.StyleCount(), Is.EqualTo(1));
        Assert.That(styles.FindById("Body"), Is.SameAs(created));
        Assert.Throws<InvalidOperationException>(() => styles.CreateStyle(global::docx.DocxStyle.TypeParagraph, "Body", "Duplicate"));

        Assert.That(styles.ReplaceStyle(replacement), Is.True);
        Assert.That(styles.FindById("Body"), Is.SameAs(replacement));
        Assert.That(styles.FindById("Body")!.IsCharacterStyle(), Is.True);
        Assert.That(styles.AddOrReplaceStyle(added), Is.False);
        Assert.That(styles.FindById("Grid"), Is.SameAs(added));
        Assert.That(styles.AddOrReplaceStyle(created), Is.True);
        Assert.That(styles.FindById("Body"), Is.SameAs(created));
    }

    [Test]
    public void StylesWriting_WritesReplacementStyleOnceAfterAddOrReplace()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Old Body\" /></w:style>"
        }));
        global::docx.DocxStyle replacement = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeParagraph, "Body", "New Body");

        replacement.RunFormat.SetBold(true);

        Assert.That(styles.AddOrReplaceStyle(replacement), Is.True);

        string xml = styles.WriteXml();
        global::docx.DocxStyles written = ParseStyles(xml);

        Assert.That(written.StyleCount(), Is.EqualTo(1));
        Assert.That(written.FindById("Body"), Is.Not.Null);
        Assert.That(written.FindById("Body")!.Name, Is.EqualTo("New Body"));
        Assert.That(written.FindById("Body")!.RunFormat.Bold, Is.True);
        Assert.That(xml, Does.Not.Contain("Old Body"));
    }

    [Test]
    public void StyleImport_AddsMissingStyleAndClonesSource()
    {
        global::docx.DocxStyles source = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"Heading 1\" /><w:rPr><w:b /></w:rPr></w:style>"
        }));
        global::docx.DocxStyles target = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>"
        }));

        Assert.That(target.ImportStyle(source.GetStyle(0)), Is.True);

        global::docx.DocxStyle imported = target.FindById("Heading1")!;

        Assert.That(target.StyleCount(), Is.EqualTo(2));
        Assert.That(imported, Is.Not.SameAs(source.GetStyle(0)));
        Assert.That(imported.Name, Is.EqualTo("Heading 1"));
        Assert.That(imported.RunFormat.Bold, Is.True);

        source.GetStyle(0).SetName("Changed");

        Assert.That(imported.Name, Is.EqualTo("Heading 1"));
    }

    [Test]
    public void StyleImport_SkipsEquivalentExistingStyle()
    {
        global::docx.DocxStyles source = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Body Text\" /></w:style>"
        }));
        global::docx.DocxStyles target = global::docx.DocxStyles.Create();

        Assert.That(target.ImportStyle(source.GetStyle(0)), Is.True);
        Assert.That(target.GetImportDiagnostics(source.GetStyle(0)).Count(), Is.EqualTo(0));
        Assert.That(target.HasImportConflict(source.GetStyle(0)), Is.False);
        Assert.That(target.ImportStyle(source.GetStyle(0)), Is.False);
        Assert.That(target.StyleCount(), Is.EqualTo(1));
    }

    [Test]
    public void StyleImport_ReportsConflictingStyleIdWithoutReplacing()
    {
        global::docx.DocxStyles target = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Body Text\" /></w:style>"
        }));
        global::docx.DocxStyle conflicting = global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeParagraph, "Body", "Different Body");

        global::docx.DocxValidationReport diagnostics = target.GetImportDiagnostics(conflicting);

        Assert.That(diagnostics.Count(), Is.EqualTo(1));
        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.ImportConflictCode), Is.True);
        Assert.That(diagnostics.Errors[0].Location, Is.EqualTo("/word/styles.xml#styleId=Body"));
        Assert.That(target.HasImportConflict(conflicting), Is.True);
        Assert.Throws<InvalidOperationException>(() => target.ImportStyle(conflicting));
        Assert.That(target.FindById("Body")!.Name, Is.EqualTo("Body Text"));
        Assert.That(target.StyleCount(), Is.EqualTo(1));
    }

    [Test]
    public void StyleImport_ImportsCollectionUntilConflict()
    {
        global::docx.DocxStyles source = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading\"><w:name w:val=\"Heading\" /></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"Grid\"><w:name w:val=\"Table Grid\" /></w:style>"
        }));
        global::docx.DocxStyles target = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Body\"><w:name w:val=\"Body\" /></w:style>"
        }));

        Assert.That(target.ImportStyles(source), Is.EqualTo(2));
        Assert.That(target.FindById("Heading"), Is.Not.Null);
        Assert.That(target.FindById("Grid"), Is.Not.Null);
        Assert.That(target.StyleCount(), Is.EqualTo(3));
    }

    [Test]
    public void StyleImportDiagnostics_ReportMissingStyleId()
    {
        global::docx.DocxStyles target = global::docx.DocxStyles.Create();
        global::docx.DocxStyle style = new global::docx.DocxStyle("");

        global::docx.DocxValidationReport diagnostics = target.GetImportDiagnostics(style);

        Assert.That(diagnostics.Count(), Is.EqualTo(1));
        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.ImportMissingStyleIdCode), Is.True);
        Assert.Throws<ArgumentException>(() => target.ImportStyle(style));
    }

    [Test]
    public void StylesWriting_WritesCollectionAfterMutation()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:docDefaults><w:rPrDefault><w:rPr><w:b /></w:rPr></w:rPrDefault></w:docDefaults>",
            "<w:latentStyles w:count=\"1\"><w:lsdException w:name=\"Normal\" /></w:latentStyles>",
            "<w:custom marker=\"kept\" />",
            "<w:style w:type=\"paragraph\" w:styleId=\"Old\"><w:name w:val=\"Old\" /></w:style>"
        }));

        styles.DeleteStyle("Old");
        styles.CreateStyle(global::docx.DocxStyle.TypeParagraph, "New", "New Style");

        string xml = styles.WriteXml();
        global::docx.DocxStyles written = ParseStyles(xml);

        Assert.That(xml, Does.Contain("<w:styles"));
        Assert.That(xml, Does.Contain("docDefaults"));
        Assert.That(xml, Does.Contain("latentStyles"));
        Assert.That(xml, Does.Contain("marker=\"kept\""));
        Assert.That(xml, Does.Not.Contain("styleId=\"Old\""));
        Assert.That(written.HasDocumentDefaults(), Is.True);
        Assert.That(written.HasLatentStyles(), Is.True);
        Assert.That(written.StyleCount(), Is.EqualTo(1));
        Assert.That(written.GetStyle(0).StyleId, Is.EqualTo("New"));
        Assert.That(written.GetStyle(0).Name, Is.EqualTo("New Style"));
    }

    [Test]
    public void DocumentCreation_AddsStyleToParsedCollection()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        global::docx.DocxStyle style = document.CreateStyle(global::docx.DocxStyle.TypeParagraph, "Body", "Body Text");

        Assert.That(document.GetStyles().StyleCount(), Is.EqualTo(1));
        Assert.That(document.FindStyleById("Body"), Is.SameAs(style));
    }

    [Test]
    public void DocumentImport_DelegatesToParsedStyles()
    {
        global::docx.DocxPackage sourcePackage = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading\"><w:name w:val=\"Heading\" /></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"Grid\"><w:name w:val=\"Table Grid\" /></w:style>"
        }));
        global::docx.DocxPackage targetPackage = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>"
        }));
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(sourcePackage);
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(targetPackage);

        Assert.That(target.GetStyleImportDiagnostics(source.GetStyles().GetStyle(0)).Count(), Is.EqualTo(0));
        Assert.That(target.ImportStyles(source), Is.EqualTo(2));

        Assert.That(target.FindStyleById("Heading"), Is.Not.Null);
        Assert.That(target.FindStyleById("Grid"), Is.Not.Null);
        Assert.That(target.GetStyles().StyleCount(), Is.EqualTo(3));
    }

    [Test]
    public void StyleDeletionDiagnostics_ReportReferencesAndRemoveStyle()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Body\" w:default=\"1\"><w:name w:val=\"Body\" /></w:style>",
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading\"><w:name w:val=\"Heading\" /><w:basedOn w:val=\"Body\" /><w:next w:val=\"Body\" /></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"BodyChar\"><w:name w:val=\"Body Char\" /><w:link w:val=\"Body\" /></w:style>"
        }));

        global::docx.DocxValidationReport diagnostics = styles.GetDeleteDiagnostics("Body");

        Assert.That(diagnostics.Count(), Is.EqualTo(4));
        Assert.That(diagnostics.HasWarnings(), Is.True);
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.DeleteDefaultStyleCode), Is.True);
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.DeleteBasedOnReferenceCode), Is.True);
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.DeleteNextReferenceCode), Is.True);
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.DeleteLinkReferenceCode), Is.True);
        Assert.That(diagnostics.Errors[0].Location, Is.EqualTo("/word/styles.xml#styleId=Body"));

        diagnostics = styles.DeleteStyle("Body");

        Assert.That(diagnostics.Count(), Is.EqualTo(4));
        Assert.That(styles.FindById("Body"), Is.Null);
        Assert.That(styles.StyleCount(), Is.EqualTo(2));
        Assert.That(styles.CountChildElements("style", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(2));
    }

    [Test]
    public void StyleDeletionDiagnostics_ReportMissingStyleWithoutMutation()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>"
        }));

        global::docx.DocxValidationReport diagnostics = styles.DeleteStyle("Missing");

        Assert.That(diagnostics.Count(), Is.EqualTo(1));
        Assert.That(diagnostics.ContainsCode(global::docx.DocxStyles.DeleteMissingStyleCode), Is.True);
        Assert.That(diagnostics.Errors[0].Location, Is.EqualTo("/word/styles.xml#styleId=Missing"));
        Assert.That(styles.StyleCount(), Is.EqualTo(1));
        Assert.That(styles.FindById("Normal"), Is.Not.Null);
    }

    [Test]
    public void DocumentDeletion_DelegatesToParsedStyles()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"Grid\"><w:name w:val=\"Table Grid\" /></w:style>"
        }));
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        global::docx.DocxValidationReport diagnostics = document.DeleteStyle("Grid");

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(document.FindStyleById("Grid"), Is.Null);
        Assert.That(document.GetStyles().StyleCount(), Is.EqualTo(1));
    }

    [Test]
    public void PackageDiscovery_FindsStylesPartThroughMainDocumentRelationship()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles/styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>"
        }));

        Assert.That(package.FindRelationshipSet("/word/document.xml"), Is.Not.Null);
        Assert.That(package.FindStylesRelationship(), Is.Not.Null);
        Assert.That(package.GetStylesPartName(), Is.EqualTo("/word/styles/styles.xml"));
        Assert.That(package.GetStylesPart(), Is.Not.Null);
        Assert.That(package.HasStylesPart(), Is.True);
    }

    [Test]
    public void DocumentParsing_LoadsStylesWhenPartIsPresent()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\"><w:name w:val=\"Normal\" /></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"TableGrid\"><w:name w:val=\"Table Grid\" /></w:style>"
        }));

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.StylesPart, Is.SameAs(package.GetStylesPart()));
        Assert.That(document.HasStylesRoot, Is.True);
        Assert.That(document.HasStyles(), Is.True);
        Assert.That(document.GetStyles().StyleCount(), Is.EqualTo(2));
        Assert.That(document.GetStyles().GetStyle(1).StyleId, Is.EqualTo("TableGrid"));
    }

    [Test]
    public void DocumentParsing_ReturnsFalseWhenStylesPartIsMissing()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.ParseStyles(), Is.False);
        Assert.That(document.StylesPart, Is.Null);
        Assert.That(document.HasStylesRoot, Is.False);
        Assert.That(document.HasStyles(), Is.False);
    }

    [Test]
    public void StylesDiscovery_IgnoresExternalAndMissingRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithExternalStylesRelationship();

        Assert.That(package.FindStylesRelationship(), Is.Null);
        Assert.That(package.GetStylesPartName(), Is.EqualTo(""));
        Assert.That(package.GetStylesPart(), Is.Null);
        Assert.That(package.HasStylesPart(), Is.False);

        package = global::docx.DocxPackage.Create();

        Assert.That(package.FindRelationshipSet("/word/document.xml"), Is.Null);
        Assert.That(package.FindStylesRelationship(), Is.Null);
        Assert.That(package.GetStylesPartName(), Is.EqualTo(""));
    }

    [Test]
    public void StylesParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxStyles styles = global::docx.DocxStyles.Create();
        global::docx.DocxStyle style = new global::docx.DocxStyle("");
        global::docx.DocxPackage package = new global::docx.DocxPackage();

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxStyles(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => styles.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => styles.AddStyle(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.DocxStyleFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.DocxDocumentDefaultsFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.DocxLatentStylesFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => styles.CountStylesByType(null!));
        Assert.Throws<ArgumentNullException>(() => styles.FindById(null!));
        Assert.Throws<ArgumentNullException>(() => styles.FindByName(null!));
        Assert.Throws<ArgumentNullException>(() => styles.FindByIdAndType(null!, global::docx.DocxStyle.TypeParagraph));
        Assert.Throws<ArgumentNullException>(() => styles.FindByIdAndType("Normal", null!));
        Assert.Throws<ArgumentNullException>(() => styles.FindByNameAndType(null!, global::docx.DocxStyle.TypeParagraph));
        Assert.Throws<ArgumentNullException>(() => styles.FindByNameAndType("Normal", null!));
        Assert.Throws<ArgumentNullException>(() => styles.ContainsStyleId(null!));
        Assert.Throws<ArgumentNullException>(() => styles.ContainsStyleName(null!));
        Assert.Throws<ArgumentNullException>(() => styles.CountChildElements(null!, ""));
        Assert.Throws<ArgumentNullException>(() => styles.CountChildElements("style", null!));
        Assert.Throws<ArgumentException>(() => styles.CountChildElements("", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxStyle(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.Create(null!, "Body", "Body"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeParagraph, null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.Create(global::docx.DocxStyle.TypeParagraph, "Body", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadAttributes(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadChild(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadDefaultAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadCustomStyleAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadParagraphProperties(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadRunProperties(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadTableProperties(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadNumberingProperties(null!));
        Assert.Throws<ArgumentNullException>(() => style.ReadNumberingPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => style.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => style.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetIdentity(null!, "Body", "Body"));
        Assert.Throws<ArgumentNullException>(() => style.SetIdentity(global::docx.DocxStyle.TypeParagraph, null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => style.SetIdentity(global::docx.DocxStyle.TypeParagraph, "Body", null!));
        Assert.Throws<ArgumentNullException>(() => style.SetType(null!));
        Assert.Throws<ArgumentException>(() => style.SetType("unknown"));
        Assert.Throws<ArgumentNullException>(() => style.SetStyleId(null!));
        Assert.Throws<ArgumentException>(() => style.SetStyleId(""));
        Assert.Throws<ArgumentNullException>(() => style.SetName(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetBasedOn(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetNext(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetLink(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetParagraphFormat(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetRunFormat(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetTableFormat(null!));
        Assert.Throws<ArgumentNullException>(() => style.SetNumbering(null!, ""));
        Assert.Throws<ArgumentNullException>(() => style.SetNumbering("", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.IsSupportedStyleType(null!));
        Assert.Throws<ArgumentNullException>(() => styles.CreateStyle(null!, "Body", "Body"));
        Assert.Throws<ArgumentNullException>(() => styles.CreateStyle(global::docx.DocxStyle.TypeParagraph, null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => styles.CreateStyle(global::docx.DocxStyle.TypeParagraph, "Body", null!));
        Assert.Throws<ArgumentNullException>(() => styles.ReplaceStyle(null!));
        Assert.Throws<ArgumentException>(() => styles.ReplaceStyle(new global::docx.DocxStyle("")));
        Assert.Throws<ArgumentNullException>(() => styles.AddOrReplaceStyle(null!));
        Assert.Throws<ArgumentNullException>(() => styles.GetImportDiagnostics(null!));
        Assert.Throws<ArgumentNullException>(() => styles.HasImportConflict(null!));
        Assert.Throws<ArgumentNullException>(() => styles.ImportStyle(null!));
        Assert.Throws<ArgumentNullException>(() => styles.ImportStyles(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.StylesEqual(null!, new global::docx.DocxStyle("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.StylesEqual(new global::docx.DocxStyle(""), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.CloneStyle(null!));
        Assert.Throws<ArgumentNullException>(() => styles.GetDeleteDiagnostics(null!));
        Assert.Throws<ArgumentNullException>(() => styles.DeleteStyle(null!));
        Assert.Throws<ArgumentNullException>(() => styles.RemoveStyleById(null!));
        Assert.Throws<ArgumentNullException>(() => styles.RemoveStyleChildById(null!));
        Assert.Throws<ArgumentNullException>(() => styles.IsStyleChildWithId(null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => styles.IsStyleChildWithId(new global::docx.DocxXmlBuffer("<root />"), null!));
        Assert.Throws<ArgumentNullException>(() => styles.AddReferenceDiagnostics(null!, new global::docx.DocxStyle("")));
        Assert.Throws<ArgumentNullException>(() => styles.AddReferenceDiagnostics(new global::docx.DocxValidationReport(), null!));
        Assert.Throws<ArgumentNullException>(() => styles.AddReferenceDiagnostic(null!, "code", "message", new global::docx.DocxStyle("")));
        Assert.Throws<ArgumentNullException>(() => styles.AddReferenceDiagnostic(new global::docx.DocxValidationReport(), null!, "message", new global::docx.DocxStyle("")));
        Assert.Throws<ArgumentNullException>(() => styles.AddReferenceDiagnostic(new global::docx.DocxValidationReport(), "code", null!, new global::docx.DocxStyle("")));
        Assert.Throws<ArgumentNullException>(() => styles.AddReferenceDiagnostic(new global::docx.DocxValidationReport(), "code", "message", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.GetStyleLocation(null!));
        Assert.Throws<ArgumentNullException>(() => styles.AddParsedChild(null!));
        Assert.Throws<ArgumentNullException>(() => styles.AddParsedStyle(null!));
        Assert.Throws<ArgumentNullException>(() => styles.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => styles.WriteNonStyleChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => styles.WriteStyleChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyles.IsStyleChild(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteAttributesTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteMetadataTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteParagraphPropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteRunPropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteTablePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteDirectNumberingPropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteOptionalAttribute(null!, "type", "paragraph"));
        Assert.Throws<ArgumentNullException>(() => style.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "paragraph"));
        Assert.Throws<ArgumentNullException>(() => style.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "type", null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteValueElement(null!, "name", "Body"));
        Assert.Throws<ArgumentNullException>(() => style.WriteValueElement(global::docx.DocxXmlWriter.Create(), null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => style.WriteValueElement(global::docx.DocxXmlWriter.Create(), "name", null!));
        Assert.Throws<ArgumentNullException>(() => style.WriteNumberingValueElement(null!, "ilvl", "0"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.IsManagedStyleChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxStyle.IsManagedStyleChildName(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxDocumentDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDocumentDefaults.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDocumentDefaults.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxLatentStyles(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxLatentStyles.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxLatentStyles.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxLatentStyleException(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxLatentStyleException.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxLatentStyleException.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxLatentStyles.IsLatentStyleException(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxLatentStyles.DocxLatentStyleExceptionFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => package.FindRelationshipSet(null!));
        Assert.Throws<FormatException>(() => new global::docx.DocxLatentStyles("<w:latentStyles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:defLockedState=\"maybe\" />"));
        Assert.Throws<FormatException>(() => new global::docx.DocxLatentStyleException("<w:lsdException xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:qFormat=\"maybe\" />"));
        Assert.Throws<FormatException>(() => new global::docx.DocxStyle("<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:default=\"maybe\" />"));
        Assert.Throws<FormatException>(() => new global::docx.DocxStyle("<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:customStyle=\"maybe\" />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxDocument.OpenPackage(CreatePackageWithStyles("styles.xml", "<root />")));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxStyles.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxStyle.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxDocumentDefaults.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxLatentStyles.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxLatentStyleException.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => style.ReadAttributes(reader));
        Assert.Throws<InvalidOperationException>(() => style.ReadParagraphProperties(reader));
        Assert.Throws<InvalidOperationException>(() => style.ReadRunProperties(reader));
        Assert.Throws<InvalidOperationException>(() => style.ReadTableProperties(reader));
        Assert.Throws<InvalidOperationException>(() => style.ReadNumberingProperties(reader));

        global::docx.DocxDocumentDefaults defaults = global::docx.DocxDocumentDefaults.Create();
        global::docx.DocxLatentStyles latentStyles = global::docx.DocxLatentStyles.Create();
        global::docx.DocxLatentStyleException latentStyleException = new global::docx.DocxLatentStyleException("");

        Assert.Throws<ArgumentNullException>(() => defaults.ReadParagraphDefault(null!));
        Assert.Throws<ArgumentNullException>(() => defaults.ReadRunDefault(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.ReadAttributes(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.ReadDefaultLockedStateAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.ReadDefaultSemiHiddenAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.ReadDefaultUnhideWhenUsedAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.ReadDefaultQuickFormatAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyles.AddException(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyleException.ReadAttributes(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyleException.ReadLockedAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyleException.ReadSemiHiddenAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyleException.ReadUnhideWhenUsedAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => latentStyleException.ReadQuickFormatAttribute(null!));
        Assert.Throws<InvalidOperationException>(() => defaults.ReadParagraphDefault(reader));
        Assert.Throws<InvalidOperationException>(() => defaults.ReadRunDefault(reader));
        Assert.Throws<InvalidOperationException>(() => latentStyles.ReadAttributes(reader));
        Assert.Throws<InvalidOperationException>(() => latentStyleException.ReadAttributes(reader));
    }

    [Test]
    public void DocumentLookup_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.Throws<ArgumentNullException>(() => document.FindStyleById(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindStyleByName(null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateStyle(null!, "Body", "Body"));
        Assert.Throws<ArgumentNullException>(() => document.CreateStyle(global::docx.DocxStyle.TypeParagraph, null!, "Body"));
        Assert.Throws<ArgumentNullException>(() => document.CreateStyle(global::docx.DocxStyle.TypeParagraph, "Body", null!));
        Assert.Throws<ArgumentNullException>(() => document.GetStyleImportDiagnostics(null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportStyle(null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportStyles((global::docx.DocxDocument)null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportStyleDefinitions((global::docx.DocxStyles)null!));
        Assert.Throws<ArgumentNullException>(() => document.DeleteStyle(null!));
    }

    [Test]
    public void EffectiveFormatting_SeparatesRunAndParagraphLayersAndResolvesThemeValues()
    {
        global::docx.DocxDocument document = CreateEffectiveFormattingDocument();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:pStyle w:val=\"Body\" /><w:jc w:val=\"right\" /></w:pPr>"
            + "<w:r><w:rPr><w:rStyle w:val=\"Emphasis\" /><w:b w:val=\"0\" /><w:sz w:val=\"28\" /></w:rPr><w:t>Theme</w:t></w:r>"
            + "<w:r><w:rPr><w:rStyle w:val=\"Emphasis\" /><w:rFonts w:ascii=\"Courier New\" /><w:color w:val=\"ABCDEF\" /></w:rPr><w:t>Direct</w:t></w:r></w:p>");
        global::docx.IDocxEffectiveFormatting formatting = new global::docx.DocxEffectiveFormatting();

        global::docx.DocxEffectiveParagraphFormatting paragraphResult = formatting.GetParagraphFormatting(document, paragraph);
        global::docx.DocxEffectiveRunFormatting runResult = formatting.GetRunFormatting(document, paragraph, paragraph.GetRun(0));
        global::docx.DocxEffectiveRunFormatting directResult = formatting.GetRunFormatting(document, paragraph, paragraph.GetRun(1));

        Assert.That(paragraphResult.Direct.Alignment, Is.EqualTo("right"));
        Assert.That(paragraphResult.Inherited.Alignment, Is.EqualTo("center"));
        Assert.That(paragraphResult.Inherited.SpacingBefore, Is.EqualTo("300"));
        Assert.That(paragraphResult.Inherited.SpacingAfter, Is.EqualTo("200"));
        Assert.That(paragraphResult.Effective.Alignment, Is.EqualTo("right"));
        Assert.That(runResult.Direct.Bold, Is.False);
        Assert.That(runResult.Inherited.Bold, Is.True);
        Assert.That(runResult.Inherited.Italic, Is.True);
        Assert.That(runResult.Inherited.FontAscii, Is.EqualTo("Cambria"));
        Assert.That(runResult.Inherited.Color, Is.EqualTo("99B2CC"));
        Assert.That(runResult.Inherited.UnderlineColor, Is.EqualTo("CC3300"));
        Assert.That(runResult.Effective.Bold, Is.False);
        Assert.That(runResult.Effective.Size, Is.EqualTo("28"));
        Assert.That(directResult.Direct.Color, Is.EqualTo("ABCDEF"));
        Assert.That(directResult.Effective.FontAsciiTheme, Is.EqualTo(""));
        Assert.That(directResult.Effective.FontAscii, Is.EqualTo("Courier New"));
        Assert.That(directResult.Effective.ColorTheme, Is.EqualTo(""));
        Assert.That(directResult.Effective.Color, Is.EqualTo("ABCDEF"));
    }

    [Test]
    public void EffectiveFormatting_ComposesCharacterStyleTogglesAndAppliesDirectValuesAbsolutely()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"StrongParagraph\"><w:rPr><w:b/><w:i/><w:caps/></w:rPr></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"BaseCharacter\"><w:rPr><w:b/><w:i w:val=\"0\"/><w:caps w:val=\"0\"/></w:rPr></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"DerivedCharacter\"><w:basedOn w:val=\"BaseCharacter\"/><w:rPr><w:b/><w:i/><w:caps w:val=\"0\"/></w:rPr></w:style>"
        }));
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:pPr><w:pStyle w:val=\"StrongParagraph\"/></w:pPr><w:r><w:rPr><w:rStyle w:val=\"DerivedCharacter\"/><w:b w:val=\"0\"/><w:i/></w:rPr><w:t>Text</w:t></w:r></w:p>");
        global::docx.IDocxEffectiveFormatting formatting = new global::docx.DocxEffectiveFormatting();

        global::docx.DocxEffectiveRunFormatting result = formatting.GetRunFormatting(document, paragraph, paragraph.GetRun(0));

        Assert.That(result.Inherited.HasBold && result.Inherited.Bold, Is.True);
        Assert.That(result.Inherited.HasItalic && !result.Inherited.Italic, Is.True);
        Assert.That(result.Inherited.HasCaps && result.Inherited.Caps, Is.True);
        Assert.That(result.Direct.HasBold && !result.Direct.Bold, Is.True);
        Assert.That(result.Direct.HasItalic && result.Direct.Italic, Is.True);
        Assert.That(result.Effective.HasBold && !result.Effective.Bold, Is.True);
        Assert.That(result.Effective.HasItalic && result.Effective.Italic, Is.True);
        Assert.That(result.Effective.HasCaps && result.Effective.Caps, Is.True);
    }

    [Test]
    public void EffectiveFormatting_CalculatesTableRowAndCellInheritance()
    {
        global::docx.DocxDocument document = CreateEffectiveFormattingDocument();
        global::docx.DocxTable table = new global::docx.DocxTable(
            "<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblStyle w:val=\"DerivedTable\" /><w:tblW w:w=\"6000\" w:type=\"dxa\" /></w:tblPr>"
            + "<w:tr><w:trPr><w:trHeight w:val=\"400\" w:hRule=\"exact\" /></w:trPr><w:tc><w:tcPr><w:vAlign w:val=\"bottom\" /><w:tcMar><w:left w:w=\"90\" w:type=\"dxa\" /></w:tcMar><w:shd w:fill=\"EEEEEE\" /></w:tcPr><w:p /></w:tc><w:tc><w:p /></w:tc></w:tr>"
            + "<w:tr><w:tc><w:p /></w:tc><w:tc><w:p /></w:tc></w:tr></w:tbl>");
        global::docx.IDocxEffectiveFormatting formatting = new global::docx.DocxEffectiveFormatting();

        global::docx.DocxEffectiveTableFormatting tableResult = formatting.GetTableFormatting(document, table);
        global::docx.DocxEffectiveTableRowFormatting rowResult = formatting.GetRowFormatting(document, table, table.GetRow(0));
        global::docx.DocxEffectiveTableCellFormatting cellResult = formatting.GetCellFormatting(document, table, 0, 0);

        Assert.That(tableResult.Direct.Width, Is.EqualTo("6000"));
        Assert.That(tableResult.Inherited.Alignment, Is.EqualTo("center"));
        Assert.That(tableResult.Inherited.CellMarginTopWidth, Is.EqualTo("120"));
        Assert.That(tableResult.Inherited.Shading.Fill, Is.EqualTo("CC3300"));
        Assert.That(tableResult.Effective.Width, Is.EqualTo("6000"));
        Assert.That(rowResult.Direct.Height, Is.EqualTo("400"));
        Assert.That(rowResult.Inherited.Height, Is.EqualTo("300"));
        Assert.That(rowResult.Inherited.CantSplit, Is.True);
        Assert.That(rowResult.Inherited.RepeatHeader, Is.True);
        Assert.That(rowResult.Effective.HeightRule, Is.EqualTo("exact"));
        Assert.That(cellResult.Direct.VerticalAlignment, Is.EqualTo("bottom"));
        Assert.That(cellResult.Inherited.VerticalAlignment, Is.EqualTo("center"));
        Assert.That(cellResult.Inherited.TextDirection, Is.EqualTo("tbRl"));
        Assert.That(cellResult.Inherited.MarginTopWidth, Is.EqualTo("120"));
        Assert.That(cellResult.Inherited.Shading.Fill, Is.EqualTo("336699"));
        Assert.That(cellResult.Inherited.Borders.TopColor, Is.EqualTo("AA0000"));
        Assert.That(cellResult.Inherited.Borders.LeftVal, Is.EqualTo("double"));
        Assert.That(cellResult.Effective.MarginLeftWidth, Is.EqualTo("90"));
        Assert.That(cellResult.Effective.Shading.ThemeFill, Is.EqualTo(""));
        Assert.That(cellResult.Effective.Shading.Fill, Is.EqualTo("EEEEEE"));
    }

    [Test]
    public void EffectiveFormatting_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableRow row = new global::docx.DocxTableRow("");
        global::docx.IDocxEffectiveFormatting formatting = new global::docx.DocxEffectiveFormatting();

        Assert.Throws<ArgumentNullException>(() => formatting.GetRunFormatting(null!, paragraph, run));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRunFormatting(document, null!, run));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRunFormatting(document, paragraph, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetParagraphFormatting(null!, paragraph));
        Assert.Throws<ArgumentNullException>(() => formatting.GetParagraphFormatting(document, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetTableFormatting(null!, table));
        Assert.Throws<ArgumentNullException>(() => formatting.GetTableFormatting(document, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRowFormatting(null!, table, row));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRowFormatting(document, null!, row));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRowFormatting(document, table, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetCellFormatting(null!, table, 0, 0));
        Assert.Throws<ArgumentNullException>(() => formatting.GetCellFormatting(document, null!, 0, 0));
    }

    [Test]
    public void PublicStyleManagement_EnumeratesMetadataAndResolvesRelationships()
    {
        global::docx.DocxStyles styles = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:latentStyles w:defUIPriority=\"99\"><w:lsdException w:name=\"heading 1\" w:qFormat=\"1\" /></w:latentStyles>",
            "<w:style w:type=\"paragraph\" w:styleId=\"Normal\" w:default=\"1\"><w:name w:val=\"Normal\" /></w:style>",
            "<w:style w:type=\"character\" w:styleId=\"Heading1Char\"><w:name w:val=\"Heading 1 Char\" /><w:link w:val=\"Heading1\" /></w:style>",
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\" /><w:aliases w:val=\"Heading One, H1\" /><w:basedOn w:val=\"Normal\" /><w:next w:val=\"Normal\" /><w:link w:val=\"Heading1Char\" /><w:uiPriority w:val=\"9\" /><w:hidden w:val=\"0\" /><w:semiHidden w:val=\"0\" /><w:unhideWhenUsed /><w:qFormat /><w:locked w:val=\"1\" /></w:style>"
        }));
        global::docx.IDocxStyleManagement management = new global::docx.DocxStyleManagement();
        global::docx.DocxStyle heading = management.FindStyleByIdAndType(styles, "Heading1", global::docx.DocxStyle.TypeParagraph)!;

        Assert.That(management.GetStyles(styles), Has.Count.EqualTo(3));
        Assert.That(management.GetStylesByType(styles, global::docx.DocxStyle.TypeParagraph), Has.Count.EqualTo(2));
        Assert.That(management.FindStyleByNameAndType(styles, "Heading 1 Char", global::docx.DocxStyle.TypeCharacter), Is.Not.Null);
        Assert.That(management.GetDefaultStyle(styles, global::docx.DocxStyle.TypeParagraph)!.StyleId, Is.EqualTo("Normal"));
        Assert.That(heading.Aliases, Is.EqualTo(new string[] { "Heading One", "H1" }));
        Assert.That(heading.UiPriority, Is.EqualTo("9"));
        Assert.That(heading.HasHidden && !heading.Hidden, Is.True);
        Assert.That(heading.HasSemiHidden && !heading.SemiHidden, Is.True);
        Assert.That(heading.HasUnhideWhenUsed && heading.UnhideWhenUsed, Is.True);
        Assert.That(heading.HasQuickFormat && heading.QuickFormat, Is.True);
        Assert.That(heading.HasLocked && heading.Locked, Is.True);
        Assert.That(management.GetBasedOnChain(styles, heading).Select(style => style.StyleId), Is.EqualTo(new string[] { "Normal" }));
        Assert.That(management.ResolveLinkedStyle(styles, heading)!.StyleId, Is.EqualTo("Heading1Char"));
        Assert.That(management.ResolveNextStyle(styles, heading)!.StyleId, Is.EqualTo("Normal"));
        Assert.That(management.GetLatentStyleExceptions(styles), Has.Count.EqualTo(1));
        Assert.That(management.FindLatentStyleException(styles, "heading 1")!.QuickFormat, Is.True);
        Assert.That(management.GetBuiltInStyleName("Heading1"), Is.EqualTo("heading 1"));
        Assert.That(management.GetBuiltInStyleId("List Paragraph"), Is.EqualTo("ListParagraph"));
        Assert.That(management.FindStyleByName(styles, "H1"), Is.SameAs(heading));
    }

    [Test]
    public void PublicStyleManagement_CreatesUpdatesAndPersistsLatentStyles()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.IDocxStyleManagement management = new global::docx.DocxStyleManagement();
        global::docx.DocxStyle style = management.CreateStyle(document, global::docx.DocxStyle.TypeParagraph, "Body", "Body Text");

        style.Aliases.Add("Body Copy");
        style.UiPriority = "20";
        style.HasQuickFormat = true;
        style.QuickFormat = true;
        style.BasedOn = "Normal";
        Assert.That(management.UpdateStyle(document, style), Is.True);

        document.GetStyles().LatentStyles.DefaultUiPriority = "99";
        document.GetStyles().LatentStyles.HasDefaultSemiHidden = true;
        document.GetStyles().LatentStyles.DefaultSemiHidden = true;
        management.UpdateLatentStyles(document);

        global::docx.DocxLatentStyleException exception = new global::docx.DocxLatentStyleException("");
        exception.Name = "Body Text";
        exception.UiPriority = "20";
        exception.HasQuickFormat = true;
        exception.QuickFormat = true;
        management.UpdateLatentStyleException(document, exception);

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);
        global::docx.DocxStyle persisted = management.FindStyleById(reopened.GetStyles(), "Body")!;

        Assert.That(persisted.Aliases, Is.EqualTo(new string[] { "Body Copy" }));
        Assert.That(persisted.UiPriority, Is.EqualTo("20"));
        Assert.That(persisted.QuickFormat, Is.True);
        Assert.That(persisted.BasedOn, Is.EqualTo("Normal"));
        Assert.That(reopened.GetStyles().LatentStyles.DefaultSemiHidden, Is.True);
        Assert.That(management.FindLatentStyleException(reopened.GetStyles(), "Body Text")!.QuickFormat, Is.True);
        Assert.That(management.DeleteLatentStyleException(reopened, "Body Text"), Is.True);
        Assert.That(management.FindLatentStyleException(global::docx.DocxDocument.OpenPackage(reopened.Package).GetStyles(), "Body Text"), Is.Null);
    }

    [Test]
    public void PublicStyleManagement_ReportsContentUsageBeforeDelete()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Used\"><w:name w:val=\"Used\" /></w:style>"
        }));
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument,
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:pPr><w:pStyle w:val=\"Used\" /></w:pPr><w:r><w:rPr><w:rStyle w:val=\"Used\" /></w:rPr><w:t>Used</w:t></w:r></w:p><w:tbl><w:tblPr><w:tblStyle w:val=\"Used\" /></w:tblPr></w:tbl><w:sectPr /></w:body></w:document>");
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.IDocxStyleManagement management = new global::docx.DocxStyleManagement();

        global::docx.DocxValidationReport diagnostics = management.GetStyleDeleteDiagnostics(document, "Used");

        Assert.That(diagnostics.Errors.Select(error => error.Code), Does.Contain(global::docx.DocxStyles.DeleteParagraphUsageCode));
        Assert.That(diagnostics.Errors.Select(error => error.Code), Does.Contain(global::docx.DocxStyles.DeleteRunUsageCode));
        Assert.That(diagnostics.Errors.Select(error => error.Code), Does.Contain(global::docx.DocxStyles.DeleteTableUsageCode));
        Assert.That(management.DeleteStyle(document, "Used").Errors, Has.Count.EqualTo(3));
        Assert.That(management.FindStyleById(global::docx.DocxDocument.OpenPackage(package).GetStyles(), "Used"), Is.Null);
    }

    [Test]
    public void PublicStyleManagement_ImportsIdenticalStylesAndReportsConflicts()
    {
        string originalXml = "<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Shared\" /></w:style>";
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.IDocxStyleManagement management = new global::docx.DocxStyleManagement();
        global::docx.DocxStyle original = new global::docx.DocxStyle(originalXml);

        Assert.That(management.ImportStyle(document, original), Is.True);
        Assert.That(management.ImportStyle(document, new global::docx.DocxStyle(originalXml)), Is.False);

        global::docx.DocxStyle conflict = new global::docx.DocxStyle("<w:style xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Different\" /></w:style>");
        global::docx.DocxValidationReport diagnostics = management.GetStyleImportDiagnostics(document, conflict);

        Assert.That(diagnostics.Errors.Select(error => error.Code), Does.Contain(global::docx.DocxStyles.ImportConflictCode));
        Assert.Throws<InvalidOperationException>(() => management.ImportStyle(document, conflict));
        Assert.That(management.FindStyleById(global::docx.DocxDocument.OpenPackage(document.Package).GetStyles(), "Shared")!.Name, Is.EqualTo("Shared"));
    }

    [Test]
    public void PublicStyleManagement_BulkImportSkipsConflictsAndPersistsEveryValidStyle()
    {
        global::docx.DocxPackage package = CreatePackageWithStyles("styles.xml", CreateStylesXml(new string[]
        {
            "<w:style w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Target Shared\"/></w:style>"
        }));
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxStyles source = ParseStyles(CreateStylesXml(new string[]
        {
            "<w:style w:type=\"character\" w:styleId=\"Before\"><w:name w:val=\"Before\"/></w:style>",
            "<w:style w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Source Conflict\"/></w:style>",
            "<w:style w:type=\"table\" w:styleId=\"After\"><w:name w:val=\"After\"/></w:style>"
        }));
        global::docx.IDocxStyleManagement management = new global::docx.DocxStyleManagement();

        int imported = management.ImportStyles(document, source);
        global::docx.DocxStyles persisted = global::docx.DocxDocument.OpenPackage(document.Package).GetStyles();

        Assert.That(imported, Is.EqualTo(2));
        Assert.That(management.FindStyleById(persisted, "Before")!.Type, Is.EqualTo(global::docx.DocxStyle.TypeCharacter));
        Assert.That(management.FindStyleById(persisted, "After")!.Type, Is.EqualTo(global::docx.DocxStyle.TypeTable));
        Assert.That(management.FindStyleById(persisted, "Shared")!.Name, Is.EqualTo("Target Shared"));
        Assert.That(persisted.StyleCount(), Is.EqualTo(3));
    }

    static global::docx.DocxDocument CreateEffectiveFormattingDocument()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        relationships.CreateRelationshipWithId("rIdStyles", global::docx.DocxConstants.RelationshipTypeStyles, "styles.xml", false);
        relationships.CreateRelationshipWithId("rIdTheme", global::docx.DocxConstants.RelationshipTypeTheme, "theme/theme1.xml", false);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, relationships.WriteXml());
        package.AddXmlPartWithContent("/word/styles.xml", global::docx.DocxConstants.ContentTypeStyles, CreateEffectiveFormattingStylesXml());
        package.AddXmlPartWithContent("/word/theme/theme1.xml", global::docx.DocxConstants.ContentTypeTheme, CreateEffectiveFormattingThemeXml());
        return global::docx.DocxDocument.OpenPackage(package);
    }

    static string CreateEffectiveFormattingStylesXml()
    {
        return "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:docDefaults><w:pPrDefault><w:pPr><w:jc w:val=\"left\" /><w:spacing w:before=\"100\" /></w:pPr></w:pPrDefault><w:rPrDefault><w:rPr><w:rFonts w:asciiTheme=\"minorAscii\" /><w:sz w:val=\"20\" /></w:rPr></w:rPrDefault></w:docDefaults>"
            + "<w:style w:type=\"paragraph\" w:styleId=\"Normal\" w:default=\"1\"><w:pPr><w:jc w:val=\"center\" /><w:spacing w:after=\"200\" /></w:pPr><w:rPr><w:b /></w:rPr></w:style>"
            + "<w:style w:type=\"paragraph\" w:styleId=\"Body\"><w:basedOn w:val=\"Normal\" /><w:pPr><w:spacing w:before=\"300\" /></w:pPr><w:rPr><w:i /><w:rFonts w:asciiTheme=\"majorAscii\" /></w:rPr></w:style>"
            + "<w:style w:type=\"character\" w:styleId=\"BaseChar\"><w:rPr><w:color w:themeColor=\"accent1\" w:themeTint=\"80\" /></w:rPr></w:style><w:style w:type=\"character\" w:styleId=\"Emphasis\"><w:basedOn w:val=\"BaseChar\" /><w:rPr><w:u w:val=\"single\" w:themeColor=\"accent2\" /></w:rPr></w:style>"
            + "<w:style w:type=\"table\" w:styleId=\"BaseTable\" w:default=\"1\"><w:tblPr><w:tblCellMar><w:top w:w=\"120\" w:type=\"dxa\" /><w:left w:w=\"130\" w:type=\"dxa\" /><w:bottom w:w=\"140\" w:type=\"dxa\" /><w:right w:w=\"150\" w:type=\"dxa\" /></w:tblCellMar><w:tblBorders><w:top w:val=\"single\" w:color=\"AA0000\" /><w:left w:val=\"double\" w:color=\"0000AA\" /><w:insideH w:val=\"dashed\" /><w:insideV w:val=\"dotted\" /></w:tblBorders><w:shd w:themeFill=\"accent2\" /></w:tblPr><w:trPr><w:trHeight w:val=\"300\" w:hRule=\"atLeast\" /><w:cantSplit /></w:trPr><w:tcPr><w:vAlign w:val=\"center\" /><w:shd w:themeFill=\"accent1\" /></w:tcPr></w:style>"
            + "<w:style w:type=\"table\" w:styleId=\"DerivedTable\"><w:basedOn w:val=\"BaseTable\" /><w:tblPr><w:jc w:val=\"center\" /></w:tblPr><w:trPr><w:tblHeader /></w:trPr><w:tcPr><w:textDirection w:val=\"tbRl\" /></w:tcPr></w:style></w:styles>";
    }

    static string CreateEffectiveFormattingThemeXml()
    {
        return "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><a:themeElements><a:clrScheme name=\"Test\"><a:accent1><a:srgbClr val=\"336699\" /></a:accent1><a:accent2><a:srgbClr val=\"CC3300\" /></a:accent2></a:clrScheme><a:fontScheme name=\"Test\"><a:majorFont><a:latin typeface=\"Cambria\" /></a:majorFont><a:minorFont><a:latin typeface=\"Aptos\" /></a:minorFont></a:fontScheme></a:themeElements></a:theme>";
    }

    static global::docx.DocxStyles ParseStyles(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("styles", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxStyles.Parse(reader);
    }

    static global::docx.DocxPackage CreatePackageWithStyles(string target, string stylesXml)
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdStyles", global::docx.DocxConstants.RelationshipTypeStyles, target, false);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent(package.GetStylesPartName(), global::docx.DocxConstants.ContentTypeStyles, stylesXml);

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithExternalStylesRelationship()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdStyles", global::docx.DocxConstants.RelationshipTypeStyles, "https://example.com/styles.xml", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static string CreateStylesXml(string[] children)
    {
        return "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:styles>";
    }

    static global::docx.DocxStyle ParseStyle(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("style", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxStyle.Parse(reader);
    }

    static global::docx.DocxDocumentDefaults ParseDocumentDefaults(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("docDefaults", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxDocumentDefaults.Parse(reader);
    }

    static global::docx.DocxLatentStyles ParseLatentStyles(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("latentStyles", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxLatentStyles.Parse(reader);
    }
}
