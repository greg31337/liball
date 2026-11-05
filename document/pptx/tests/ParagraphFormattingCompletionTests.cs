using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ParagraphFormattingCompletionTests
{
    [Test]
    public void ParagraphFormatting_RoundTripsAlignmentIndentationSpacingTabsDefaultsAndCharacterBullet()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 500L, 200L));
        shape.SetText("Formatted paragraph");
        PptxParagraph paragraph = shape.GetTextFrame().GetParagraph(0);
        IPptxParagraphOperations operations = new PptxParagraphOperations();
        PptxTextSpacing line = CreateSpacing(PptxTextSpacing.Percent, 125000);
        PptxTextSpacing before = CreateSpacing(PptxTextSpacing.Points, 600);
        PptxTextSpacing after = CreateSpacing(PptxTextSpacing.Points, 900);
        List<PptxTextTab> tabs = new List<PptxTextTab>();
        tabs.Add(CreateTab(914400L, "left"));
        tabs.Add(CreateTab(1828800L, "decimal"));
        PptxFont font = CreateDefaultFont();

        operations.SetAlignment(paragraph, "center");
        operations.SetIndentation(paragraph, 2, 457200L, 228600L, -228600L);
        operations.SetSpacing(paragraph, line, before, after);
        operations.SetTabs(paragraph, tabs);
        operations.SetDefaultTabSize(paragraph, 914400L);
        operations.SetDefaultRunProperties(paragraph, font);
        operations.SetCharacterBullet(paragraph, "•");

        PptxPresentation reopened = Reopen(presentation);
        PptxParagraph reopenedParagraph = reopened.GetSlide(0).GetShapeTree().Get(0).GetTextFrame().GetParagraph(0);
        PptxParagraphFormat format = operations.GetFormat(reopenedParagraph);

        Assert.That(format.Alignment, Is.EqualTo("ctr"));
        Assert.That(format.Level, Is.EqualTo(2));
        Assert.That(format.MarginLeft, Is.EqualTo(457200L));
        Assert.That(format.MarginRight, Is.EqualTo(228600L));
        Assert.That(format.Indent, Is.EqualTo(-228600L));
        Assert.That(format.DefaultTabSize, Is.EqualTo(914400L));
        Assert.That(format.LineSpacing.Unit, Is.EqualTo(PptxTextSpacing.Percent));
        Assert.That(format.LineSpacing.Value, Is.EqualTo(125000));
        Assert.That(format.SpaceBefore.Unit, Is.EqualTo(PptxTextSpacing.Points));
        Assert.That(format.SpaceBefore.Value, Is.EqualTo(600));
        Assert.That(format.SpaceAfter.Value, Is.EqualTo(900));
        Assert.That(format.Tabs, Has.Count.EqualTo(2));
        Assert.That(format.Tabs[0].Position, Is.EqualTo(914400L));
        Assert.That(format.Tabs[0].Alignment, Is.EqualTo("l"));
        Assert.That(format.Tabs[1].Alignment, Is.EqualTo("dec"));
        Assert.That(format.DefaultFont.Family, Is.EqualTo("Aptos"));
        Assert.That(format.DefaultFont.EastAsianFamily, Is.EqualTo("Yu Gothic"));
        Assert.That(format.DefaultFont.ComplexScriptFamily, Is.EqualTo("Arial"));
        Assert.That(format.DefaultFont.Language, Is.EqualTo("en-US"));
        Assert.That(format.DefaultFont.SizePoints, Is.EqualTo(22.0));
        Assert.That(format.DefaultFont.Bold, Is.True);
        Assert.That(format.DefaultFont.Italic, Is.True);
        Assert.That(format.DefaultFont.Underline, Is.True);
        Assert.That(format.DefaultFont.Strike, Is.True);
        Assert.That(format.DefaultFont.Baseline, Is.EqualTo(12000));
        Assert.That(format.DefaultFont.FillColor.Type, Is.EqualTo(PptxColor.Scheme));
        Assert.That(format.DefaultFont.FillColor.Value, Is.EqualTo("accent4"));
        Assert.That(format.BulletType, Is.EqualTo(PptxBullet.Character));
        Assert.That(format.BulletCharacter, Is.EqualTo("•"));
        Assert.That(reopenedParagraph.Alignment, Is.EqualTo("ctr"));
        Assert.That(reopenedParagraph.Level, Is.EqualTo(2));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ParagraphFormatting_PreservesPictureBulletAndExtensionsUntilBulletIsChanged()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 500L, 200L));
        shape.SetText("Picture bullet");
        PptxParagraph paragraph = shape.GetTextFrame().GetParagraph(0);
        AppendPictureBulletFormatting(paragraph);
        PptxParagraphOperations operations = new PptxParagraphOperations();

        PptxParagraphFormat original = operations.GetFormat(paragraph);
        Assert.That(original.BulletType, Is.EqualTo(PptxBullet.Picture));
        Assert.That(original.PictureBulletRelationshipId, Is.EqualTo("rIdBullet"));

        operations.SetAlignment(paragraph, "right");
        operations.SetSpacing(paragraph, CreateSpacing(PptxTextSpacing.Percent, 100000), new PptxTextSpacing(), new PptxTextSpacing());

        PptxParagraphFormat preserved = operations.GetFormat(paragraph);
        Assert.That(preserved.BulletType, Is.EqualTo(PptxBullet.Picture));
        Assert.That(preserved.PictureBulletRelationshipId, Is.EqualTo("rIdBullet"));
        Assert.That(paragraph.PropertiesXml, Does.Contain("future-paragraph"));
        Assert.That(paragraph.PropertiesXml, Does.Contain("buClr"));
        Assert.That(paragraph.PropertiesXml, Does.Contain("buFont"));

        operations.SetAutomaticNumbering(paragraph, "arabicPeriod", 4);
        PptxParagraphFormat numbered = operations.GetFormat(paragraph);
        Assert.That(numbered.BulletType, Is.EqualTo(PptxBullet.Number));
        Assert.That(numbered.NumberingType, Is.EqualTo("arabicPeriod"));
        Assert.That(numbered.NumberingStartAt, Is.EqualTo(4));
        Assert.That(paragraph.PropertiesXml, Does.Not.Contain("buBlip"));
        Assert.That(paragraph.PropertiesXml, Does.Contain("future-paragraph"));
        Assert.That(paragraph.PropertiesXml, Does.Contain("buClr"));
        Assert.That(paragraph.PropertiesXml, Does.Contain("buFont"));

        operations.ClearBullet(paragraph);
        Assert.That(operations.GetFormat(paragraph).BulletType, Is.EqualTo(PptxBullet.None));
        Assert.That(paragraph.HasBullet, Is.False);
        Assert.That(paragraph.PropertiesXml, Does.Contain("buNone"));
    }

    [Test]
    public void DefaultRunProperties_PreserveUnmodeledChildrenAndCanClearDirectValues()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 500L, 200L));
        shape.SetText("Defaults");
        PptxParagraph paragraph = shape.GetTextFrame().GetParagraph(0);
        XmlElement properties = PptxParagraphFormatCore.EnsureProperties(paragraph);
        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement defaultProperties = document.CreateElement("a", "defRPr", PptxNamespaces.Drawing);
        XmlElement highlight = document.CreateElement("a", "highlight", PptxNamespaces.Drawing);
        XmlElement highlightColor = document.CreateElement("a", "srgbClr", PptxNamespaces.Drawing);
        highlightColor.SetAttribute("val", "FFFF00");
        highlight.AppendChild(highlightColor);
        defaultProperties.AppendChild(highlight);
        properties.AppendChild(defaultProperties);
        PptxParagraphOperations operations = new PptxParagraphOperations();

        operations.SetDefaultRunProperties(paragraph, CreateDefaultFont());
        Assert.That(paragraph.PropertiesXml, Does.Contain("highlight"));
        Assert.That(paragraph.PropertiesXml.IndexOf("solidFill"), Is.LessThan(paragraph.PropertiesXml.IndexOf("highlight")));
        Assert.That(paragraph.PropertiesXml.IndexOf("highlight"), Is.LessThan(paragraph.PropertiesXml.IndexOf("latin")));

        operations.SetDefaultRunProperties(paragraph, new PptxFont());
        PptxParagraphFormat cleared = operations.GetFormat(paragraph);
        Assert.That(cleared.DefaultFont.Family, Is.Empty);
        Assert.That(cleared.DefaultFont.SizePoints, Is.EqualTo(0.0));
        Assert.That(cleared.DefaultFont.FillColor.Type, Is.EqualTo(PptxColor.Empty));
        Assert.That(paragraph.PropertiesXml, Does.Contain("highlight"));
    }

    [Test]
    public void ParagraphFormatting_RejectsInvalidValuesBeforeCreatingProperties()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 500L, 200L));
        shape.SetText("Validation");
        PptxParagraph paragraph = shape.GetTextFrame().GetParagraph(0);
        PptxParagraphOperations operations = new PptxParagraphOperations();
        PptxTextSpacing negative = CreateSpacing(PptxTextSpacing.Points, -1);
        PptxTextSpacing unknown = CreateSpacing("lines", 100);
        List<PptxTextTab> badTabs = new List<PptxTextTab>();
        badTabs.Add(CreateTab(0L, "bar"));
        PptxFont negativeFont = new PptxFont();
        negativeFont.SizePoints = -1.0;

        Assert.Throws<System.ArgumentException>(() => operations.SetAlignment(paragraph, "diagonal"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetIndentation(paragraph, 9, 0L, 0L, 0L));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetIndentation(paragraph, 0, -1L, 0L, 0L));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetSpacing(paragraph, negative, new PptxTextSpacing(), new PptxTextSpacing()));
        Assert.Throws<System.ArgumentException>(() => operations.SetSpacing(paragraph, unknown, new PptxTextSpacing(), new PptxTextSpacing()));
        Assert.Throws<System.ArgumentNullException>(() => operations.SetTabs(paragraph, null!));
        Assert.Throws<System.ArgumentException>(() => operations.SetTabs(paragraph, badTabs));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetDefaultTabSize(paragraph, -1L));
        Assert.Throws<System.ArgumentNullException>(() => operations.SetDefaultRunProperties(paragraph, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetDefaultRunProperties(paragraph, negativeFont));
        Assert.Throws<System.ArgumentException>(() => operations.SetCharacterBullet(paragraph, ""));
        Assert.Throws<System.ArgumentException>(() => operations.SetAutomaticNumbering(paragraph, "", 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetAutomaticNumbering(paragraph, "arabicPeriod", 0));
        Assert.That(paragraph.PropertiesXml, Is.Empty);
    }

    static PptxTextSpacing CreateSpacing(string unit, int value)
    {
        PptxTextSpacing spacing = new PptxTextSpacing();
        spacing.Unit = unit;
        spacing.Value = value;
        return spacing;
    }

    static PptxTextTab CreateTab(long position, string alignment)
    {
        PptxTextTab tab = new PptxTextTab();
        tab.Position = position;
        tab.Alignment = alignment;
        return tab;
    }

    static PptxFont CreateDefaultFont()
    {
        PptxFont font = new PptxFont();
        font.Family = "Aptos";
        font.EastAsianFamily = "Yu Gothic";
        font.ComplexScriptFamily = "Arial";
        font.Language = "en-US";
        font.SizePoints = 22.0;
        font.Bold = true;
        font.Italic = true;
        font.Underline = true;
        font.Strike = true;
        font.Baseline = 12000;
        font.FillColor = PptxColor.CreateScheme("accent4");
        return font;
    }

    static void AppendPictureBulletFormatting(PptxParagraph paragraph)
    {
        XmlElement properties = PptxParagraphFormatCore.EnsureProperties(paragraph);
        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement bulletColor = document.CreateElement("a", "buClr", PptxNamespaces.Drawing);
        XmlElement color = document.CreateElement("a", "schemeClr", PptxNamespaces.Drawing);
        color.SetAttribute("val", "accent2");
        bulletColor.AppendChild(color);
        properties.AppendChild(bulletColor);
        XmlElement bulletFont = document.CreateElement("a", "buFont", PptxNamespaces.Drawing);
        bulletFont.SetAttribute("typeface", "Aptos");
        properties.AppendChild(bulletFont);
        XmlElement picture = document.CreateElement("a", "buBlip", PptxNamespaces.Drawing);
        XmlElement blip = document.CreateElement("a", "blip", PptxNamespaces.Drawing);
        XmlAttribute relationship = document.CreateAttribute("r", "embed", PptxNamespaces.Relationships);
        relationship.Value = "rIdBullet";
        blip.Attributes.Append(relationship);
        picture.AppendChild(blip);
        properties.AppendChild(picture);
        XmlElement extensions = document.CreateElement("a", "extLst", PptxNamespaces.Drawing);
        XmlElement extension = document.CreateElement("a", "ext", PptxNamespaces.Drawing);
        extension.SetAttribute("uri", "future-paragraph");
        extensions.AppendChild(extension);
        properties.AppendChild(extensions);
    }

    static PptxPresentation Reopen(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
