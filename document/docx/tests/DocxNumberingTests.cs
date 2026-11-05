using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxNumberingTests
{
    [Test]
    public void NumberingParsing_CapturesAbstractNumberingAndInstances()
    {
        global::docx.DocxNumbering numbering = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"10\"><w:multiLevelType w:val=\"hybridMultilevel\" /><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"5\"><w:abstractNumId w:val=\"10\" /></w:num>",
            "<w:custom marker=\"kept\" />"
        }));

        Assert.That(numbering.HasXml(), Is.True);
        Assert.That(numbering.ChildCount(), Is.EqualTo(3));
        Assert.That(numbering.AbstractNumberingCount(), Is.EqualTo(1));
        Assert.That(numbering.NumberingInstanceCount(), Is.EqualTo(1));
        Assert.That(numbering.CountChildElements("abstractNum", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(numbering.CountChildElements("num", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(numbering.GetChildXml(2), Does.Contain("marker=\"kept\""));

        global::docx.DocxAbstractNumbering abstractNumbering = numbering.GetAbstractNumbering(0);
        global::docx.DocxNumberingInstance instance = numbering.GetNumberingInstance(0);

        Assert.That(abstractNumbering.HasXml(), Is.True);
        Assert.That(abstractNumbering.AbstractNumberingId, Is.EqualTo("10"));
        Assert.That(abstractNumbering.MultiLevelType, Is.EqualTo("hybridMultilevel"));
        Assert.That(abstractNumbering.ChildCount(), Is.EqualTo(2));
        Assert.That(abstractNumbering.LevelCount(), Is.EqualTo(1));
        Assert.That(abstractNumbering.GetChildXml(1), Does.Contain("<w:lvl"));
        Assert.That(instance.HasXml(), Is.True);
        Assert.That(instance.NumberingId, Is.EqualTo("5"));
        Assert.That(instance.AbstractNumberingId, Is.EqualTo("10"));
        Assert.That(instance.ChildCount(), Is.EqualTo(1));
        Assert.That(instance.LevelOverrideCount(), Is.EqualTo(0));
        Assert.That(numbering.FindAbstractNumberingById("10"), Is.SameAs(abstractNumbering));
        Assert.That(numbering.FindNumberingInstanceById("5"), Is.SameAs(instance));
        Assert.That(numbering.FindAbstractNumberingById("missing"), Is.Null);
        Assert.That(numbering.FindNumberingInstanceById("missing"), Is.Null);
    }

    [Test]
    public void AbstractNumberingParsing_ReadsDefinitionMetadataAndLevels()
    {
        global::docx.DocxAbstractNumbering abstractNumbering = ParseAbstractNumbering("<w:abstractNum xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:abstractNumId=\"20\"><w:nsid w:val=\"12AB34CD\" /><w:multiLevelType w:val=\"multilevel\" /><w:tmpl w:val=\"0409001D\" /><w:styleLink w:val=\"ListParagraph\" /><w:numStyleLink w:val=\"ListStyle\" /><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\" /><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1.\" /><w:lvlJc w:val=\"left\" /><w:suff w:val=\"tab\" /><w:pPr><w:ind w:left=\"720\" w:hanging=\"360\" /></w:pPr><w:rPr><w:b /><w:color w:val=\"112233\" /></w:rPr></w:lvl><w:lvl w:ilvl=\"1\"><w:start w:val=\"1\" /><w:numFmt w:val=\"bullet\" /><w:lvlText w:val=\"-\" /></w:lvl></w:abstractNum>");

        Assert.That(abstractNumbering.HasAnyDefinition(), Is.True);
        Assert.That(abstractNumbering.AbstractNumberingId, Is.EqualTo("20"));
        Assert.That(abstractNumbering.Nsid, Is.EqualTo("12AB34CD"));
        Assert.That(abstractNumbering.MultiLevelType, Is.EqualTo("multilevel"));
        Assert.That(abstractNumbering.TemplateCode, Is.EqualTo("0409001D"));
        Assert.That(abstractNumbering.StyleLink, Is.EqualTo("ListParagraph"));
        Assert.That(abstractNumbering.NumberingStyleLink, Is.EqualTo("ListStyle"));
        Assert.That(abstractNumbering.LevelCount(), Is.EqualTo(2));

        global::docx.DocxNumberingLevel level = abstractNumbering.GetLevel(0);
        global::docx.DocxNumberingLevel bulletLevel = abstractNumbering.FindLevelByIndex("1")!;

        Assert.That(level.HasXml(), Is.True);
        Assert.That(level.HasAnyDefinition(), Is.True);
        Assert.That(level.LevelIndex, Is.EqualTo("0"));
        Assert.That(level.Start, Is.EqualTo("1"));
        Assert.That(level.NumberFormat, Is.EqualTo("decimal"));
        Assert.That(level.LevelText, Is.EqualTo("%1."));
        Assert.That(level.LevelJustification, Is.EqualTo("left"));
        Assert.That(level.Suffix, Is.EqualTo("tab"));
        Assert.That(level.ChildCount(), Is.EqualTo(7));
        Assert.That(level.HasParagraphFormat(), Is.True);
        Assert.That(level.ParagraphFormat.IndentLeft, Is.EqualTo("720"));
        Assert.That(level.ParagraphFormat.Hanging, Is.EqualTo("360"));
        Assert.That(level.HasRunFormat(), Is.True);
        Assert.That(level.RunFormat.Bold, Is.True);
        Assert.That(level.RunFormat.Color, Is.EqualTo("112233"));
        Assert.That(bulletLevel.NumberFormat, Is.EqualTo("bullet"));
        Assert.That(bulletLevel.LevelText, Is.EqualTo("-"));
        Assert.That(abstractNumbering.FindLevelByIndex("missing"), Is.Null);
    }

    [Test]
    public void NumberingInstanceParsing_ReadsLevelOverrides()
    {
        global::docx.DocxNumberingInstance instance = ParseNumberingInstance("<w:num xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:numId=\"7\"><w:abstractNumId w:val=\"2\" /><w:lvlOverride w:ilvl=\"0\"><w:startOverride w:val=\"5\" /><w:lvl w:ilvl=\"0\"><w:start w:val=\"5\" /><w:numFmt w:val=\"lowerLetter\" /><w:lvlText w:val=\"%1)\" /><w:pPr><w:ind w:left=\"360\" /></w:pPr></w:lvl></w:lvlOverride><w:lvlOverride w:ilvl=\"2\"><w:startOverride w:val=\"3\" /></w:lvlOverride></w:num>");

        Assert.That(instance.HasAnyDefinition(), Is.True);
        Assert.That(instance.NumberingId, Is.EqualTo("7"));
        Assert.That(instance.AbstractNumberingId, Is.EqualTo("2"));
        Assert.That(instance.ChildCount(), Is.EqualTo(3));
        Assert.That(instance.LevelOverrideCount(), Is.EqualTo(2));

        global::docx.DocxNumberingLevelOverride firstOverride = instance.GetLevelOverride(0);
        global::docx.DocxNumberingLevelOverride secondOverride = instance.FindLevelOverrideByIndex("2")!;

        Assert.That(firstOverride.HasXml(), Is.True);
        Assert.That(firstOverride.HasAnyOverride(), Is.True);
        Assert.That(firstOverride.LevelIndex, Is.EqualTo("0"));
        Assert.That(firstOverride.StartOverride, Is.EqualTo("5"));
        Assert.That(firstOverride.ChildCount(), Is.EqualTo(2));
        Assert.That(firstOverride.GetChildXml(1), Does.Contain("<w:lvl"));
        Assert.That(firstOverride.HasLevel(), Is.True);
        Assert.That(firstOverride.Level.LevelIndex, Is.EqualTo("0"));
        Assert.That(firstOverride.Level.Start, Is.EqualTo("5"));
        Assert.That(firstOverride.Level.NumberFormat, Is.EqualTo("lowerLetter"));
        Assert.That(firstOverride.Level.LevelText, Is.EqualTo("%1)"));
        Assert.That(firstOverride.Level.ParagraphFormat.IndentLeft, Is.EqualTo("360"));
        Assert.That(secondOverride.LevelIndex, Is.EqualTo("2"));
        Assert.That(secondOverride.StartOverride, Is.EqualTo("3"));
        Assert.That(secondOverride.HasLevel(), Is.False);
        Assert.That(instance.FindLevelOverrideByIndex("missing"), Is.Null);
    }

    [Test]
    public void NumberingCreation_CreatesDecimalAndBulletLists()
    {
        global::docx.DocxNumbering numbering = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"3\" />",
            "<w:num w:numId=\"8\"><w:abstractNumId w:val=\"3\" /></w:num>"
        }));

        global::docx.DocxNumberingInstance decimalInstance = numbering.CreateDecimalList();
        global::docx.DocxNumberingInstance bulletInstance = numbering.CreateBulletListWithIds("10", "12");

        Assert.That(decimalInstance.NumberingId, Is.EqualTo("9"));
        Assert.That(decimalInstance.AbstractNumberingId, Is.EqualTo("4"));
        Assert.That(bulletInstance.NumberingId, Is.EqualTo("12"));
        Assert.That(bulletInstance.AbstractNumberingId, Is.EqualTo("10"));
        Assert.That(numbering.AbstractNumberingCount(), Is.EqualTo(3));
        Assert.That(numbering.NumberingInstanceCount(), Is.EqualTo(3));
        Assert.That(numbering.ContainsAbstractNumberingId("4"), Is.True);
        Assert.That(numbering.ContainsNumberingId("9"), Is.True);

        global::docx.DocxAbstractNumbering decimalAbstract = numbering.FindAbstractNumberingById("4")!;
        global::docx.DocxNumberingLevel decimalLevel = decimalAbstract.GetLevel(0);

        Assert.That(decimalAbstract.MultiLevelType, Is.EqualTo("singleLevel"));
        Assert.That(decimalLevel.LevelIndex, Is.EqualTo("0"));
        Assert.That(decimalLevel.Start, Is.EqualTo("1"));
        Assert.That(decimalLevel.NumberFormat, Is.EqualTo("decimal"));
        Assert.That(decimalLevel.LevelText, Is.EqualTo("%1."));
        Assert.That(decimalLevel.LevelJustification, Is.EqualTo("left"));
        Assert.That(decimalLevel.ParagraphFormat.IndentLeft, Is.EqualTo("720"));
        Assert.That(decimalLevel.ParagraphFormat.Hanging, Is.EqualTo("360"));

        global::docx.DocxAbstractNumbering bulletAbstract = numbering.FindAbstractNumberingById("10")!;
        global::docx.DocxNumberingLevel bulletLevel = bulletAbstract.GetLevel(0);

        Assert.That(bulletLevel.NumberFormat, Is.EqualTo("bullet"));
        Assert.That(bulletLevel.LevelText, Is.EqualTo("\u2022"));
        Assert.That(bulletLevel.RunFormat.FontAscii, Is.EqualTo("Symbol"));
        Assert.That(bulletLevel.RunFormat.FontHighAnsi, Is.EqualTo("Symbol"));
    }

    [Test]
    public void NumberingCreation_CreatesExplicitSingleLevelList()
    {
        global::docx.DocxNumbering numbering = global::docx.DocxNumbering.Create();

        global::docx.DocxNumberingInstance instance = numbering.CreateSingleLevelList("0", "1", "lowerRoman", "%1)");

        Assert.That(instance.NumberingId, Is.EqualTo("1"));
        Assert.That(instance.AbstractNumberingId, Is.EqualTo("0"));
        Assert.That(numbering.AllocateAbstractNumberingId(), Is.EqualTo("1"));
        Assert.That(numbering.AllocateNumberingId(), Is.EqualTo("2"));
        Assert.That(numbering.GetAbstractNumbering(0).GetLevel(0).NumberFormat, Is.EqualTo("lowerRoman"));
        Assert.That(numbering.GetAbstractNumbering(0).GetLevel(0).LevelText, Is.EqualTo("%1)"));
        Assert.Throws<InvalidOperationException>(() => numbering.CreateDecimalListWithIds("0", "2"));
        Assert.Throws<InvalidOperationException>(() => numbering.CreateDecimalListWithIds("2", "1"));
    }

    [Test]
    public void NumberingImport_RemapsCollidingIdsAndReferences()
    {
        global::docx.DocxNumbering target = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\" /><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1.\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));
        global::docx.DocxNumbering source = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"3\" /><w:numFmt w:val=\"lowerLetter\" /><w:lvlText w:val=\"%1)\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));

        global::docx.DocxNumberingImportMap map = target.ImportNumbering(source);

        Assert.That(map.AbstractNumberingCount(), Is.EqualTo(1));
        Assert.That(map.NumberingCount(), Is.EqualTo(1));
        Assert.That(map.GetAbstractNumberingId("0"), Is.EqualTo("1"));
        Assert.That(map.GetNumberingId("1"), Is.EqualTo("2"));
        Assert.That(target.AbstractNumberingCount(), Is.EqualTo(2));
        Assert.That(target.NumberingInstanceCount(), Is.EqualTo(2));

        global::docx.DocxNumberingInstance importedInstance = target.FindNumberingInstanceById("2")!;
        global::docx.DocxNumberingLevel importedLevel = target.FindEffectiveLevel("2", "0")!;

        Assert.That(importedInstance.AbstractNumberingId, Is.EqualTo("1"));
        Assert.That(importedLevel.Start, Is.EqualTo("3"));
        Assert.That(importedLevel.NumberFormat, Is.EqualTo("lowerLetter"));

        source.GetAbstractNumbering(0).GetLevel(0).NumberFormat = "upperRoman";
        source.GetAbstractNumbering(0).GetLevel(0).Xml = "";

        Assert.That(importedLevel.NumberFormat, Is.EqualTo("lowerLetter"));
    }

    [Test]
    public void NumberingImport_PreservesAvailableIdsAndMapsIdentity()
    {
        global::docx.DocxNumbering target = global::docx.DocxNumbering.Create();
        global::docx.DocxNumbering source = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"4\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"bullet\" /><w:lvlText w:val=\"-\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"9\"><w:abstractNumId w:val=\"4\" /></w:num>"
        }));

        global::docx.DocxNumberingImportMap map = target.ImportNumbering(source);

        Assert.That(map.ContainsAbstractNumberingId("4"), Is.True);
        Assert.That(map.ContainsNumberingId("9"), Is.True);
        Assert.That(map.GetAbstractNumberingId("4"), Is.EqualTo("4"));
        Assert.That(map.GetNumberingId("9"), Is.EqualTo("9"));
        Assert.That(target.FindAbstractNumberingById("4"), Is.Not.Null);
        Assert.That(target.FindNumberingInstanceById("9")!.AbstractNumberingId, Is.EqualTo("4"));
    }

    [Test]
    public void NumberingImport_RemapsMultipleInstancesSharingCollidingAbstractNumbering()
    {
        global::docx.DocxNumbering target = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\" />",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));
        global::docx.DocxNumbering source = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>",
            "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));

        global::docx.DocxNumberingImportMap map = target.ImportNumbering(source);

        Assert.That(map.GetAbstractNumberingId("0"), Is.EqualTo("1"));
        Assert.That(map.GetNumberingId("1"), Is.EqualTo("2"));
        Assert.That(map.GetNumberingId("2"), Is.EqualTo("3"));
        Assert.That(target.FindNumberingInstanceById("2")!.AbstractNumberingId, Is.EqualTo("1"));
        Assert.That(target.FindNumberingInstanceById("3")!.AbstractNumberingId, Is.EqualTo("1"));
        Assert.That(target.AbstractNumberingCount(), Is.EqualTo(2));
        Assert.That(target.NumberingInstanceCount(), Is.EqualTo(3));
    }

    [Test]
    public void NumberingImport_AllocatesIdsForMissingSourceIds()
    {
        global::docx.DocxNumbering target = global::docx.DocxNumbering.Create();
        global::docx.DocxNumbering source = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /></w:lvl></w:abstractNum>",
            "<w:num><w:abstractNumId w:val=\"\" /></w:num>"
        }));

        global::docx.DocxNumberingImportMap map = target.ImportNumbering(source);

        Assert.That(map.GetAbstractNumberingId(""), Is.EqualTo("0"));
        Assert.That(map.GetNumberingId(""), Is.EqualTo("1"));
        Assert.That(target.GetAbstractNumbering(0).AbstractNumberingId, Is.EqualTo("0"));
        Assert.That(target.GetNumberingInstance(0).NumberingId, Is.EqualTo("1"));
        Assert.That(target.GetNumberingInstance(0).AbstractNumberingId, Is.EqualTo("0"));
    }

    [Test]
    public void NumberingApplication_AppliesInstancesToParagraphsAndResolvesLevels()
    {
        global::docx.DocxNumbering numbering = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"2\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\" /><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1.\" /></w:lvl><w:lvl w:ilvl=\"1\"><w:numFmt w:val=\"lowerLetter\" /><w:lvlText w:val=\"%2)\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"7\"><w:abstractNumId w:val=\"2\" /><w:lvlOverride w:ilvl=\"1\"><w:lvl w:ilvl=\"1\"><w:start w:val=\"4\" /><w:numFmt w:val=\"upperRoman\" /><w:lvlText w:val=\"%2.\" /></w:lvl></w:lvlOverride></w:num>"
        }));
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        numbering.ApplyToParagraph(paragraph, "7", "1");

        Assert.That(paragraph.HasNumbering(), Is.True);
        Assert.That(paragraph.Format.NumberingId, Is.EqualTo("7"));
        Assert.That(paragraph.Format.NumberingLevel, Is.EqualTo("1"));

        global::docx.DocxParagraph parsed = ParseParagraph(paragraph.WriteXml());
        global::docx.DocxNumberingLevel effectiveLevel = numbering.FindEffectiveLevel("7", "1")!;
        global::docx.DocxNumberingLevel baseLevel = numbering.FindEffectiveLevel("7", "0")!;

        Assert.That(parsed.Format.NumberingId, Is.EqualTo("7"));
        Assert.That(parsed.Format.NumberingLevel, Is.EqualTo("1"));
        Assert.That(effectiveLevel.NumberFormat, Is.EqualTo("upperRoman"));
        Assert.That(effectiveLevel.Start, Is.EqualTo("4"));
        Assert.That(baseLevel.NumberFormat, Is.EqualTo("decimal"));
        Assert.That(numbering.FindEffectiveLevel("missing", "0"), Is.Null);
        Assert.That(numbering.FindEffectiveLevel("7", "missing"), Is.Null);
    }

    [Test]
    public void NumberingApplication_AppliesCreatedListInstance()
    {
        global::docx.DocxNumbering numbering = global::docx.DocxNumbering.Create();
        global::docx.DocxNumberingInstance instance = numbering.CreateDecimalList();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        numbering.ApplyInstanceToParagraph(paragraph, instance, "0");

        Assert.That(paragraph.Format.NumberingId, Is.EqualTo(instance.NumberingId));
        Assert.That(paragraph.Format.NumberingLevel, Is.EqualTo("0"));
        Assert.That(numbering.FindEffectiveLevel(instance.NumberingId, "0")!.NumberFormat, Is.EqualTo("decimal"));
    }

    [Test]
    public void NumberingLabels_CalculatesCommonFormatsAndStartOverrides()
    {
        global::docx.DocxNumbering numbering = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"2\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\" /><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1.\" /></w:lvl><w:lvl w:ilvl=\"1\"><w:numFmt w:val=\"lowerLetter\" /><w:lvlText w:val=\"%2)\" /></w:lvl><w:lvl w:ilvl=\"2\"><w:numFmt w:val=\"upperRoman\" /><w:lvlText w:val=\"Article %3\" /></w:lvl><w:lvl w:ilvl=\"3\"><w:numFmt w:val=\"decimalZero\" /><w:lvlText w:val=\"%4\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"7\"><w:abstractNumId w:val=\"2\" /><w:lvlOverride w:ilvl=\"0\"><w:startOverride w:val=\"5\" /></w:lvlOverride></w:num>"
        }));

        Assert.That(numbering.CalculateLabel("7", "0", 3), Is.EqualTo("3."));
        Assert.That(numbering.CalculateLabelForItem("7", "0", 0), Is.EqualTo("5."));
        Assert.That(numbering.CalculateLabelForItem("7", "0", 2), Is.EqualTo("7."));
        Assert.That(numbering.CalculateLabel("7", "1", 28), Is.EqualTo("ab)"));
        Assert.That(numbering.CalculateLabel("7", "2", 4), Is.EqualTo("Article IV"));
        Assert.That(numbering.CalculateLabel("7", "3", 4), Is.EqualTo("04"));
        Assert.That(global::docx.DocxNumbering.FormatNumber("upperLetter", 28), Is.EqualTo("AB"));
        Assert.That(global::docx.DocxNumbering.FormatNumber("lowerRoman", 9), Is.EqualTo("ix"));
    }

    [Test]
    public void NumberingLabels_CalculatesBulletLabels()
    {
        global::docx.DocxNumbering numbering = global::docx.DocxNumbering.Create();
        global::docx.DocxNumberingInstance bullet = numbering.CreateBulletList();

        Assert.That(numbering.CalculateLabelForItem(bullet.NumberingId, "0", 0), Is.EqualTo("\u2022"));
    }

    [Test]
    public void NumberingArithmetic_HandlesIntegerBoundaries()
    {
        global::docx.DocxNumbering exhausted = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"2147483647\" />",
            "<w:num w:numId=\"2147483647\"><w:abstractNumId w:val=\"2147483647\" /></w:num>"
        }));
        global::docx.DocxNumbering labels = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"2147483647\" /><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));

        Assert.Throws<InvalidOperationException>(() => exhausted.AllocateAbstractNumberingId());
        Assert.Throws<InvalidOperationException>(() => exhausted.AllocateNumberingId());
        Assert.Throws<ArgumentOutOfRangeException>(() => labels.CalculateLabelForItem("1", "0", 1));
        Assert.That(global::docx.DocxNumbering.GetLevelTextPlaceholder("2147483647"), Is.EqualTo("%2147483648"));
    }

    [Test]
    public void NumberingWriting_WritesCreatedDecimalAndBulletLists()
    {
        global::docx.DocxNumbering numbering = global::docx.DocxNumbering.Create();

        numbering.CreateDecimalList();
        numbering.CreateBulletList();

        string xml = numbering.WriteXml();
        global::docx.DocxNumbering written = ParseNumbering(xml);

        Assert.That(xml, Does.Contain("<w:numbering"));
        Assert.That(written.AbstractNumberingCount(), Is.EqualTo(2));
        Assert.That(written.NumberingInstanceCount(), Is.EqualTo(2));
        Assert.That(written.GetAbstractNumbering(0).GetLevel(0).NumberFormat, Is.EqualTo("decimal"));
        Assert.That(written.GetAbstractNumbering(0).GetLevel(0).LevelText, Is.EqualTo("%1."));
        Assert.That(written.GetAbstractNumbering(1).GetLevel(0).NumberFormat, Is.EqualTo("bullet"));
        Assert.That(written.GetAbstractNumbering(1).GetLevel(0).LevelText, Is.EqualTo("\u2022"));
        Assert.That(written.GetAbstractNumbering(1).GetLevel(0).RunFormat.FontAscii, Is.EqualTo("Symbol"));
        Assert.That(written.GetNumberingInstance(0).AbstractNumberingId, Is.EqualTo(written.GetAbstractNumbering(0).AbstractNumberingId));
    }

    [Test]
    public void NumberingWriting_PreservesRawXmlUntilMutated()
    {
        string rawXml = CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"1\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /></w:lvl></w:abstractNum>",
            "<w:custom marker=\"kept\" />",
            "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"1\" /></w:num>"
        });
        global::docx.DocxNumbering numbering = ParseNumbering(rawXml);

        Assert.That(numbering.WriteXml(), Is.EqualTo(rawXml));

        numbering.CreateDecimalList();

        string xml = numbering.WriteXml();
        global::docx.DocxNumbering written = ParseNumbering(xml);

        Assert.That(xml, Does.Contain("marker=\"kept\""));
        Assert.That(written.AbstractNumberingCount(), Is.EqualTo(2));
        Assert.That(written.NumberingInstanceCount(), Is.EqualTo(2));
        Assert.That(written.FindAbstractNumberingById("1"), Is.Not.Null);
        Assert.That(written.FindNumberingInstanceById("2"), Is.Not.Null);
        Assert.That(written.FindNumberingInstanceById("3"), Is.Not.Null);
    }

    [Test]
    public void NumberingWriting_WritesLevelOverrides()
    {
        global::docx.DocxNumbering numbering = global::docx.DocxNumbering.Create();
        global::docx.DocxNumberingInstance instance = numbering.CreateDecimalListWithIds("0", "1");
        global::docx.DocxNumberingLevelOverride levelOverride = new global::docx.DocxNumberingLevelOverride("");

        levelOverride.LevelIndex = "0";
        levelOverride.StartOverride = "4";
        levelOverride.Level = global::docx.DocxNumberingLevel.Create("0", "4", "upperRoman", "%1.");
        instance.AddLevelOverride(levelOverride);

        global::docx.DocxNumbering written = ParseNumbering(numbering.WriteXml());
        global::docx.DocxNumberingLevelOverride writtenOverride = written.GetNumberingInstance(0).GetLevelOverride(0);

        Assert.That(writtenOverride.LevelIndex, Is.EqualTo("0"));
        Assert.That(writtenOverride.StartOverride, Is.EqualTo("4"));
        Assert.That(writtenOverride.HasLevel(), Is.True);
        Assert.That(writtenOverride.Level.NumberFormat, Is.EqualTo("upperRoman"));
        Assert.That(written.CalculateLabelForItem("1", "0", 0), Is.EqualTo("IV."));
    }

    [Test]
    public void NumberingParsing_HandlesEmptyAndMissingRoots()
    {
        global::docx.DocxNumbering emptyNumbering = ParseNumbering("<w:numbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxNumbering missingNumbering = new global::docx.DocxNumbering("<root />");
        global::docx.DocxAbstractNumbering missingAbstract = new global::docx.DocxAbstractNumbering("<root />");
        global::docx.DocxNumberingInstance missingInstance = new global::docx.DocxNumberingInstance("<root />");

        Assert.That(emptyNumbering.HasXml(), Is.True);
        Assert.That(emptyNumbering.ChildCount(), Is.EqualTo(0));
        Assert.That(emptyNumbering.AbstractNumberingCount(), Is.EqualTo(0));
        Assert.That(emptyNumbering.NumberingInstanceCount(), Is.EqualTo(0));
        Assert.That(missingNumbering.HasXml(), Is.True);
        Assert.That(missingNumbering.ChildCount(), Is.EqualTo(0));
        Assert.That(missingAbstract.AbstractNumberingId, Is.EqualTo(""));
        Assert.That(missingInstance.NumberingId, Is.EqualTo(""));
    }

    [Test]
    public void PackageDiscovery_FindsNumberingPartThroughMainDocumentRelationship()
    {
        global::docx.DocxPackage package = CreatePackageWithNumbering("numbering/numbering.xml", CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"1\" />"
        }));

        Assert.That(package.FindRelationshipSet("/word/document.xml"), Is.Not.Null);
        Assert.That(package.FindNumberingRelationship(), Is.Not.Null);
        Assert.That(package.GetNumberingPartName(), Is.EqualTo("/word/numbering/numbering.xml"));
        Assert.That(package.GetNumberingPart(), Is.Not.Null);
        Assert.That(package.HasNumberingPart(), Is.True);
    }

    [Test]
    public void DocumentParsing_LoadsNumberingWhenPartIsPresent()
    {
        global::docx.DocxPackage package = CreatePackageWithNumbering("numbering.xml", CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"4\" />",
            "<w:num w:numId=\"9\"><w:abstractNumId w:val=\"4\" /></w:num>"
        }));

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.NumberingPart, Is.SameAs(package.GetNumberingPart()));
        Assert.That(document.HasNumberingRoot, Is.True);
        Assert.That(document.HasNumbering(), Is.True);
        Assert.That(document.GetNumbering().AbstractNumberingCount(), Is.EqualTo(1));
        Assert.That(document.GetNumbering().NumberingInstanceCount(), Is.EqualTo(1));
        Assert.That(document.GetNumbering().GetNumberingInstance(0).NumberingId, Is.EqualTo("9"));
    }

    [Test]
    public void DocumentImport_DelegatesToParsedNumbering()
    {
        global::docx.DocxPackage targetPackage = CreatePackageWithNumbering("numbering.xml", CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\" />",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));
        global::docx.DocxPackage sourcePackage = CreatePackageWithNumbering("numbering.xml", CreateNumberingXml(new string[]
        {
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(targetPackage);
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(sourcePackage);

        global::docx.DocxNumberingImportMap map = target.ImportNumbering(source);

        Assert.That(map.GetAbstractNumberingId("0"), Is.EqualTo("1"));
        Assert.That(map.GetNumberingId("1"), Is.EqualTo("2"));
        Assert.That(target.GetNumbering().FindNumberingInstanceById("2")!.AbstractNumberingId, Is.EqualTo("1"));
    }

    [Test]
    public void DocumentParsing_ReturnsFalseWhenNumberingPartIsMissing()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.ParseNumbering(), Is.False);
        Assert.That(document.NumberingPart, Is.Null);
        Assert.That(document.HasNumberingRoot, Is.False);
        Assert.That(document.HasNumbering(), Is.False);
    }

    [Test]
    public void NumberingDiscovery_IgnoresExternalAndMissingRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithExternalNumberingRelationship();

        Assert.That(package.FindNumberingRelationship(), Is.Null);
        Assert.That(package.GetNumberingPartName(), Is.EqualTo(""));
        Assert.That(package.GetNumberingPart(), Is.Null);
        Assert.That(package.HasNumberingPart(), Is.False);

        package = global::docx.DocxPackage.Create();

        Assert.That(package.FindNumberingRelationship(), Is.Null);
        Assert.That(package.GetNumberingPartName(), Is.EqualTo(""));
    }

    [Test]
    public void PublicNumberingManagement_EnumeratesDefinitionsLinksOverridesAndPictureBullets()
    {
        global::docx.DocxNumbering numbering = ParseNumbering(CreateNumberingXml(new string[]
        {
            "<w:numPicBullet w:numPicBulletId=\"7\"><w:pict><w:shape xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" /></w:pict></w:numPicBullet>",
            "<w:abstractNum w:abstractNumId=\"2\"><w:styleLink w:val=\"ListParagraph\" /><w:numStyleLink w:val=\"ListStyle\" /><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1.\" /></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"7\"><w:abstractNumId w:val=\"2\" /><w:lvlOverride w:ilvl=\"0\"><w:startOverride w:val=\"3\" /></w:lvlOverride></w:num>"
        }));
        global::docx.IDocxNumberingManagement management = new global::docx.DocxNumberingManagement();
        global::docx.DocxAbstractNumbering definition = management.FindAbstractNumbering(numbering, "2")!;
        global::docx.DocxNumberingInstance instance = management.FindNumberingInstance(numbering, "7")!;

        Assert.That(management.GetAbstractNumberings(numbering), Has.Count.EqualTo(1));
        Assert.That(management.GetNumberingInstances(numbering), Has.Count.EqualTo(1));
        Assert.That(management.GetLevels(definition), Has.Count.EqualTo(1));
        Assert.That(management.GetLevelOverrides(instance), Has.Count.EqualTo(1));
        Assert.That(management.FindLevel(definition, "0")!.NumberFormat, Is.EqualTo("decimal"));
        Assert.That(management.FindLevelOverride(instance, "0")!.StartOverride, Is.EqualTo("3"));
        Assert.That(management.FindEffectiveLevel(numbering, "7", "0"), Is.SameAs(management.FindLevel(definition, "0")));
        Assert.That(management.GetStyleLinkedNumberings(numbering, "ListParagraph"), Is.EqualTo(new global::docx.DocxAbstractNumbering[] { definition }));
        Assert.That(management.GetStyleLinkedNumberings(numbering, "ListStyle"), Is.EqualTo(new global::docx.DocxAbstractNumbering[] { definition }));
        Assert.That(management.GetPictureBulletXml(numbering), Has.Count.EqualTo(1));
        Assert.That(management.GetPictureBulletXml(numbering)[0], Does.Contain("numPicBulletId=\"7\""));
    }

    [Test]
    public void PublicNumberingManagement_CreatesAndPersistsCommonListFormats()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.IDocxNumberingManagement management = new global::docx.DocxNumberingManagement();

        global::docx.DocxNumberingInstance bullet = management.CreateBulletList(document);
        global::docx.DocxNumberingInstance decimalList = management.CreateDecimalList(document);
        global::docx.DocxNumberingInstance lowerLetter = management.CreateLowerLetterList(document);
        global::docx.DocxNumberingInstance upperLetter = management.CreateUpperLetterList(document);
        global::docx.DocxNumberingInstance lowerRoman = management.CreateLowerRomanList(document);
        global::docx.DocxNumberingInstance upperRoman = management.CreateUpperRomanList(document);
        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);
        global::docx.DocxNumbering numbering = reopened.GetNumbering();

        Assert.That(reopened.NumberingPart, Is.Not.Null);
        Assert.That(management.GetAbstractNumberings(numbering), Has.Count.EqualTo(6));
        Assert.That(management.GetNumberingInstances(numbering), Has.Count.EqualTo(6));
        Assert.That(management.CalculateListLabel(numbering, bullet.NumberingId, "0", 0), Is.EqualTo("\u2022"));
        Assert.That(management.CalculateListLabel(numbering, decimalList.NumberingId, "0", 2), Is.EqualTo("3."));
        Assert.That(management.CalculateListLabel(numbering, lowerLetter.NumberingId, "0", 27), Is.EqualTo("ab."));
        Assert.That(management.CalculateListLabel(numbering, upperLetter.NumberingId, "0", 27), Is.EqualTo("AB."));
        Assert.That(management.CalculateListLabel(numbering, lowerRoman.NumberingId, "0", 8), Is.EqualTo("ix."));
        Assert.That(management.CalculateListLabel(numbering, upperRoman.NumberingId, "0", 8), Is.EqualTo("IX."));
    }

    [Test]
    public void PublicNumberingManagement_CreatesMultilevelListsAppliesContinuationAndRestart()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.IDocxNumberingManagement management = new global::docx.DocxNumberingManagement();
        global::docx.DocxNumberingLevel first = new global::docx.DocxNumberingLevel("");
        first.LevelIndex = "0";
        first.Start = "1";
        first.NumberFormat = "decimal";
        first.LevelText = "%1.";
        global::docx.DocxNumberingLevel second = new global::docx.DocxNumberingLevel("");
        second.LevelIndex = "1";
        second.Start = "1";
        second.NumberFormat = "lowerLetter";
        second.LevelText = "%1.%2.";
        global::docx.DocxNumberingLevel third = new global::docx.DocxNumberingLevel("");
        third.LevelIndex = "2";
        third.Start = "1";
        third.NumberFormat = "upperRoman";
        third.LevelText = "%1.%2.%3.";

        global::docx.DocxNumberingInstance instance = management.CreateMultiLevelList(document, new List<global::docx.DocxNumberingLevel> { first, second, third });
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        management.ApplyList(document, paragraph, instance.NumberingId, "2");

        Assert.That(paragraph.Format.NumberingId, Is.EqualTo(instance.NumberingId));
        Assert.That(paragraph.Format.NumberingLevel, Is.EqualTo("2"));
        Assert.That(management.CalculateMultiLevelLabel(document.GetNumbering(), instance.NumberingId, "2", new List<int> { 2, 28, 4 }), Is.EqualTo("2.ab.IV."));
        Assert.That(management.ContinueList(document, instance.NumberingId), Is.SameAs(instance));

        global::docx.DocxNumberingInstance restarted = management.RestartList(document, instance.NumberingId, "0", 5);

        Assert.That(restarted.NumberingId, Is.Not.EqualTo(instance.NumberingId));
        Assert.That(restarted.AbstractNumberingId, Is.EqualTo(instance.AbstractNumberingId));
        Assert.That(management.FindLevelOverride(restarted, "0")!.StartOverride, Is.EqualTo("5"));
        Assert.That(management.CalculateListLabel(document.GetNumbering(), restarted.NumberingId, "0", 0), Is.EqualTo("5."));

        management.FindLevelOverride(restarted, "0")!.StartOverride = "6";
        Assert.That(management.UpdateNumberingInstance(document, restarted), Is.True);
        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);
        Assert.That(management.CalculateListLabel(reopened.GetNumbering(), restarted.NumberingId, "0", 0), Is.EqualTo("6."));

        management.ClearList(paragraph);
        Assert.That(paragraph.HasNumbering(), Is.False);
    }

    [Test]
    public void PublicNumberingManagement_CalculatesEmptyTextBulletLevelInMultilevelList()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.IDocxNumberingManagement management = new global::docx.DocxNumberingManagement();
        global::docx.DocxNumberingLevel bulletLevel = new global::docx.DocxNumberingLevel("");
        bulletLevel.LevelIndex = "0";
        bulletLevel.Start = "1";
        bulletLevel.NumberFormat = "bullet";
        bulletLevel.LevelText = "";
        global::docx.DocxNumberingLevel decimalLevel = new global::docx.DocxNumberingLevel("");
        decimalLevel.LevelIndex = "1";
        decimalLevel.Start = "1";
        decimalLevel.NumberFormat = "decimal";
        decimalLevel.LevelText = "%2.";

        global::docx.DocxNumberingInstance instance = management.CreateMultiLevelList(
            document,
            new List<global::docx.DocxNumberingLevel> { bulletLevel, decimalLevel });
        global::docx.DocxNumbering persisted = global::docx.DocxDocument.OpenPackage(document.Package).GetNumbering();

        Assert.That(management.CalculateListLabel(persisted, instance.NumberingId, "0", 0), Is.EqualTo("\u2022"));
        Assert.That(management.CalculateMultiLevelLabel(persisted, instance.NumberingId, "0", new List<int> { 1 }), Is.EqualTo("\u2022"));
        Assert.That(management.CalculateMultiLevelLabel(persisted, instance.NumberingId, "1", new List<int> { 1, 2 }), Is.EqualTo("2."));
    }

    [Test]
    public void PublicNumberingManagement_PersistsNestedEditsAndPreservesPictureBullets()
    {
        global::docx.DocxPackage package = CreatePackageWithNumbering("numbering.xml", CreateNumberingXml(new string[]
        {
            "<w:numPicBullet w:numPicBulletId=\"3\"><w:pict /></w:numPicBullet>",
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\" /><w:numFmt w:val=\"decimal\" /><w:lvlText w:val=\"%1.\" /><w:pPr><w:ind w:left=\"720\" /><w:customIndent w:val=\"kept\" /></w:pPr></w:lvl></w:abstractNum>",
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num>"
        }));
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.IDocxNumberingManagement management = new global::docx.DocxNumberingManagement();
        global::docx.DocxAbstractNumbering definition = management.FindAbstractNumbering(document.GetNumbering(), "0")!;

        definition.StyleLink = "ListParagraph";
        definition.GetLevel(0).NumberFormat = "upperRoman";
        definition.GetLevel(0).LevelText = "Article %1";

        Assert.That(management.UpdateAbstractNumbering(document, definition), Is.True);

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxAbstractNumbering persisted = management.FindAbstractNumbering(reopened.GetNumbering(), "0")!;

        Assert.That(persisted.StyleLink, Is.EqualTo("ListParagraph"));
        Assert.That(persisted.GetLevel(0).NumberFormat, Is.EqualTo("upperRoman"));
        Assert.That(persisted.GetLevel(0).LevelText, Is.EqualTo("Article %1"));
        Assert.That(persisted.GetLevel(0).ParagraphFormat.IndentLeft, Is.EqualTo("720"));
        Assert.That(management.CalculateListLabel(reopened.GetNumbering(), "1", "0", 3), Is.EqualTo("Article IV"));
        Assert.That(management.GetPictureBulletXml(reopened.GetNumbering()), Has.Count.EqualTo(1));
        Assert.That(package.GetNumberingPart()!.GetText(), Does.Contain("customIndent"));
    }

    [Test]
    public void NumberingParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxNumbering numbering = global::docx.DocxNumbering.Create();
        global::docx.DocxAbstractNumbering abstractNumbering = new global::docx.DocxAbstractNumbering("");
        global::docx.DocxNumberingInstance instance = new global::docx.DocxNumberingInstance("");
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxAbstractNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxNumberingInstance(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxNumberingLevel(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxNumberingLevelOverride(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingInstance.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevelOverride.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.CreateSingleLevel(null!, "singleLevel", new global::docx.DocxNumberingLevel("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.CreateSingleLevel("0", null!, new global::docx.DocxNumberingLevel("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.CreateSingleLevel("0", "singleLevel", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxAbstractNumbering.CreateSingleLevel("", "singleLevel", new global::docx.DocxNumberingLevel("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingInstance.Create(null!, "0"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingInstance.Create("1", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxNumberingInstance.Create("", "0"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxNumberingInstance.Create("1", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.Create(null!, "1", "decimal", "%1."));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.Create("0", null!, "decimal", "%1."));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.Create("0", "1", null!, "%1."));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.Create("0", "1", "decimal", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxNumberingLevel.Create("", "1", "decimal", "%1."));
        Assert.Throws<ArgumentException>(() => global::docx.DocxNumberingLevel.Create("0", "1", "", "%1."));
        Assert.Throws<ArgumentNullException>(() => numbering.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.AddParsedChild(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.AddAbstractNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.AddParsedAbstractNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.AddNumberingInstance(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.AddParsedNumberingInstance(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.ImportNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.GetImportedAbstractNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.GetImportedNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.CloneAbstractNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.CloneNumberingInstance(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.DocxAbstractNumberingFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.DocxNumberingInstanceFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.WriteNonNumberingChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.WriteAbstractNumberingsTo(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.WriteNumberingInstancesTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.IsNumberingDefinitionChild(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.FindAbstractNumberingById(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.FindNumberingInstanceById(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.ContainsAbstractNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.ContainsNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => numbering.ApplyToParagraph(null!, "1", "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.ApplyToParagraph(new global::docx.DocxParagraph(""), (string)null!, "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.ApplyToParagraph(new global::docx.DocxParagraph(""), "1", null!));
        Assert.Throws<ArgumentNullException>(() => numbering.ApplyInstanceToParagraph(null!, new global::docx.DocxNumberingInstance(""), "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.ApplyInstanceToParagraph(new global::docx.DocxParagraph(""), (global::docx.DocxNumberingInstance)null!, "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.ApplyInstanceToParagraph(new global::docx.DocxParagraph(""), new global::docx.DocxNumberingInstance(""), null!));
        Assert.Throws<InvalidOperationException>(() => numbering.ApplyToParagraph(new global::docx.DocxParagraph(""), "missing", "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.FindEffectiveLevel(null!, "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.FindEffectiveLevel("1", null!));
        Assert.Throws<ArgumentNullException>(() => numbering.CalculateLabelForItem(null!, "0", 0));
        Assert.Throws<ArgumentNullException>(() => numbering.CalculateLabelForItem("1", null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => numbering.CalculateLabelForItem("1", "0", -1));
        Assert.Throws<ArgumentNullException>(() => numbering.CalculateLabel(null!, "0", 1));
        Assert.Throws<ArgumentNullException>(() => numbering.CalculateLabel("1", null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => numbering.CalculateLabel("1", "0", 0));
        Assert.Throws<InvalidOperationException>(() => numbering.CalculateLabel("missing", "0", 1));
        Assert.Throws<ArgumentNullException>(() => numbering.GetLevelStartValue(null!, "0"));
        Assert.Throws<ArgumentNullException>(() => numbering.GetLevelStartValue("1", null!));
        Assert.Throws<InvalidOperationException>(() => numbering.GetLevelStartValue("missing", "0"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.FormatNumber(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxNumbering.FormatNumber("decimal", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxNumbering.FormatAlphabetic(0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxNumbering.FormatRoman(0, false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.AppendRomanPart(null!, 1, 1, "I"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.AppendRomanPart(new System.Text.StringBuilder(), 1, 1, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.ApplyLevelText(null!, "%1", "0", "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.ApplyLevelText("decimal", null!, "0", "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.ApplyLevelText("decimal", "%1", null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.ApplyLevelText("decimal", "%1", "0", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.GetLevelTextPlaceholder(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.ReadPositiveInt(null!, "start"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumbering.ReadPositiveInt("1", null!));
        Assert.Throws<FormatException>(() => global::docx.DocxNumbering.ReadPositiveInt("abc", "start"));
        Assert.Throws<FormatException>(() => global::docx.DocxNumbering.ReadPositiveInt("0", "start"));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateDecimalListWithIds(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateDecimalListWithIds("0", null!));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateBulletListWithIds(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateBulletListWithIds("0", null!));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateSingleLevelList(null!, "1", "decimal", "%1."));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateSingleLevelList("0", null!, "decimal", "%1."));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateSingleLevelList("0", "1", null!, "%1."));
        Assert.Throws<ArgumentNullException>(() => numbering.CreateSingleLevelList("0", "1", "decimal", null!));
        Assert.Throws<ArgumentException>(() => numbering.CreateSingleLevelList("", "1", "decimal", "%1."));
        Assert.Throws<ArgumentException>(() => numbering.CreateSingleLevelList("0", "", "decimal", "%1."));
        Assert.Throws<ArgumentException>(() => numbering.CreateSingleLevelList("0", "1", "", "%1."));
        Assert.Throws<ArgumentNullException>(() => numbering.CountChildElements(null!, ""));
        Assert.Throws<ArgumentNullException>(() => numbering.CountChildElements("num", null!));
        Assert.Throws<ArgumentException>(() => numbering.CountChildElements("", ""));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.ReadChild(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.AddLevel(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.AddParsedLevel(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.DocxNumberingLevelFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteLevelsTo(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteOptionalAttribute(null!, "abstractNumId", "0"));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "0"));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "abstractNumId", null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteValueElement(null!, "nsid", "1"));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteValueElement(global::docx.DocxXmlWriter.Create(), null!, "1"));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.WriteValueElement(global::docx.DocxXmlWriter.Create(), "nsid", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.IsManagedAbstractNumberingChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxAbstractNumbering.IsManagedAbstractNumberingChildName(null!));
        Assert.Throws<ArgumentNullException>(() => abstractNumbering.FindLevelByIndex(null!));
        Assert.Throws<ArgumentNullException>(() => instance.ReadChild(null!));
        Assert.Throws<ArgumentNullException>(() => instance.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => instance.AddLevelOverride(null!));
        Assert.Throws<ArgumentNullException>(() => instance.AddParsedLevelOverride(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingInstance.DocxNumberingLevelOverrideFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => instance.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => instance.WriteLevelOverridesTo(null!));
        Assert.Throws<ArgumentNullException>(() => instance.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => instance.WriteOptionalAttribute(null!, "numId", "1"));
        Assert.Throws<ArgumentNullException>(() => instance.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "1"));
        Assert.Throws<ArgumentNullException>(() => instance.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "numId", null!));
        Assert.Throws<ArgumentNullException>(() => instance.WriteValueElement(null!, "abstractNumId", "0"));
        Assert.Throws<ArgumentNullException>(() => instance.WriteValueElement(global::docx.DocxXmlWriter.Create(), null!, "0"));
        Assert.Throws<ArgumentNullException>(() => instance.WriteValueElement(global::docx.DocxXmlWriter.Create(), "abstractNumId", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingInstance.IsManagedNumberingInstanceChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingInstance.IsManagedNumberingInstanceChildName(null!));
        Assert.Throws<ArgumentNullException>(() => instance.FindLevelOverrideByIndex(null!));
        global::docx.DocxNumberingLevel level = new global::docx.DocxNumberingLevel("");
        Assert.Throws<ArgumentNullException>(() => level.ReadChild(null!));
        Assert.Throws<ArgumentNullException>(() => level.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => level.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => level.WriteParagraphPropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => level.WriteRunPropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => level.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => level.WriteOptionalAttribute(null!, "ilvl", "0"));
        Assert.Throws<ArgumentNullException>(() => level.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "0"));
        Assert.Throws<ArgumentNullException>(() => level.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "ilvl", null!));
        Assert.Throws<ArgumentNullException>(() => level.WriteValueElement(null!, "start", "1"));
        Assert.Throws<ArgumentNullException>(() => level.WriteValueElement(global::docx.DocxXmlWriter.Create(), null!, "1"));
        Assert.Throws<ArgumentNullException>(() => level.WriteValueElement(global::docx.DocxXmlWriter.Create(), "start", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.IsManagedLevelChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevel.IsManagedLevelChildName(null!));
        Assert.Throws<ArgumentNullException>(() => level.SetParagraphIndent(null!, "360"));
        Assert.Throws<ArgumentNullException>(() => level.SetParagraphIndent("720", null!));
        Assert.Throws<ArgumentNullException>(() => level.SetRunFonts(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => level.SetRunFonts("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => level.SetRunFonts("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => level.SetRunFonts("", "", "", null!));
        global::docx.DocxNumberingLevelOverride levelOverride = new global::docx.DocxNumberingLevelOverride("");
        Assert.Throws<ArgumentNullException>(() => levelOverride.ReadChild(null!));
        Assert.Throws<ArgumentNullException>(() => levelOverride.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevelOverride.DocxNumberingLevelFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteLevelTo(null!));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteOptionalAttribute(null!, "ilvl", "0"));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "0"));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "ilvl", null!));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteValueElement(null!, "startOverride", "1"));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteValueElement(global::docx.DocxXmlWriter.Create(), null!, "1"));
        Assert.Throws<ArgumentNullException>(() => levelOverride.WriteValueElement(global::docx.DocxXmlWriter.Create(), "startOverride", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevelOverride.IsManagedLevelOverrideChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxNumberingLevelOverride.IsManagedLevelOverrideChildName(null!));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxDocument.OpenPackage(CreatePackageWithNumbering("numbering.xml", "<root />")));

        global::docx.DocxNumberingImportMap map = new global::docx.DocxNumberingImportMap();

        Assert.Throws<ArgumentNullException>(() => map.AddAbstractNumberingId(null!, "0"));
        Assert.Throws<ArgumentNullException>(() => map.AddAbstractNumberingId("0", null!));
        Assert.Throws<ArgumentNullException>(() => map.AddNumberingId(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => map.AddNumberingId("1", null!));
        Assert.Throws<ArgumentNullException>(() => map.ContainsAbstractNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => map.ContainsNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => map.GetAbstractNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => map.GetNumberingId(null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportNumbering((global::docx.DocxDocument)null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportNumberingDefinitions((global::docx.DocxNumbering)null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxNumbering.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxAbstractNumbering.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxNumberingInstance.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxNumberingLevel.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxNumberingLevelOverride.Parse(reader));
    }

    static global::docx.DocxNumbering ParseNumbering(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("numbering", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxNumbering.Parse(reader);
    }

    static global::docx.DocxAbstractNumbering ParseAbstractNumbering(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("abstractNum", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxAbstractNumbering.Parse(reader);
    }

    static global::docx.DocxNumberingInstance ParseNumberingInstance(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("num", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxNumberingInstance.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static global::docx.DocxPackage CreatePackageWithNumbering(string target, string numberingXml)
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdNumbering", global::docx.DocxConstants.RelationshipTypeNumbering, target, false);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent(package.GetNumberingPartName(), global::docx.DocxConstants.ContentTypeNumbering, numberingXml);

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithExternalNumberingRelationship()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rIdNumbering", global::docx.DocxConstants.RelationshipTypeNumbering, "https://example.com/numbering.xml", true);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static string CreateNumberingXml(string[] children)
    {
        return "<w:numbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:numbering>";
    }
}
