using System.Collections.Generic;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class DeckGenerationCompletionTests
{
    [Test]
    public void CreateDefaultDeck_ProvidesDeterministicThemeMasterAndBuiltInLayouts()
    {
        PptxDeckGenerator generator = new PptxDeckGenerator();
        PptxDocument document = generator.CreateDefaultDeck();
        byte[] first = document.SaveBytes();
        byte[] second = generator.CreateDefaultDeck().SaveBytes();
        PptxDocument reopenedDocument = new PptxDocumentFactory().OpenBytes(first);
        PptxPresentation presentation = reopenedDocument.LoadPresentation();

        Assert.That(presentation.SlideCount(), Is.EqualTo(0));
        Assert.That(presentation.SlideMasterCount(), Is.EqualTo(1));
        Assert.That(presentation.SlideLayoutCount(), Is.EqualTo(6));
        Assert.That(presentation.ThemeCount(), Is.EqualTo(1));
        AssertLayout(presentation.GetSlideLayout(0), PptxBuiltInLayout.Blank, "blank", "rId2");
        AssertLayout(presentation.GetSlideLayout(1), PptxBuiltInLayout.TitleSlide, "title", "rId3");
        AssertLayout(presentation.GetSlideLayout(2), PptxBuiltInLayout.TitleAndContent, "obj", "rId4");
        AssertLayout(presentation.GetSlideLayout(3), PptxBuiltInLayout.SectionHeader, "secHead", "rId5");
        AssertLayout(presentation.GetSlideLayout(4), PptxBuiltInLayout.TwoContent, "twoObj", "rId6");
        AssertLayout(presentation.GetSlideLayout(5), PptxBuiltInLayout.TitleOnly, "titleOnly", "rId7");
        Assert.That(presentation.GetSlideMaster(0).Layouts.Count, Is.EqualTo(6));
        Assert.That(first, Is.EqualTo(second));
        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
    }

    [Test]
    public void Generator_AddsTitleContentBulletAndImageSlidesWithStableIdentifiers()
    {
        PptxDeckGenerator generator = new PptxDeckGenerator();
        PptxDocument document = generator.CreateDefaultDeck();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide title = generator.AddTitleSlide(presentation, "Product launch", "Autumn 2026");
        PptxSlide content = generator.AddContentSlide(presentation, "Overview", "Launch readiness");
        List<string> bullets = new List<string>();
        bullets.Add("Design complete");
        bullets.Add("Pilot scheduled");
        PptxSlide bullet = generator.AddBulletSlide(presentation, "Milestones", bullets);
        PptxSlide image = generator.AddImageSlide(presentation, "Architecture", CreatePngBytes(320, 180), PptxContentTypes.Png);
        PptxPresentation reopened = Reopen(document).LoadPresentation();

        AssertSlideIdentity(title, 256, "/ppt/slides/slide1.xml", "rId2");
        AssertSlideIdentity(content, 257, "/ppt/slides/slide2.xml", "rId6");
        AssertSlideIdentity(bullet, 258, "/ppt/slides/slide3.xml", "rId7");
        AssertSlideIdentity(image, 259, "/ppt/slides/slide4.xml", "rId8");
        Assert.That(reopened.GetSlide(0).Layout!.Name, Is.EqualTo(PptxBuiltInLayout.TitleSlide));
        Assert.That(reopened.GetSlide(1).Layout!.Name, Is.EqualTo(PptxBuiltInLayout.TitleAndContent));
        Assert.That(reopened.GetSlide(0).GetPlaceholderTextByIndex("title", 0), Is.EqualTo("Product launch"));
        Assert.That(reopened.GetSlide(0).GetPlaceholderTextByIndex("subTitle", 1), Is.EqualTo("Autumn 2026"));
        Assert.That(reopened.GetSlide(1).GetPlaceholderTextByIndex("content", 1), Is.EqualTo("Launch readiness"));
        PptxTextFrame bulletFrame = reopened.GetSlide(2).GetShapeTree().Get(1).GetTextFrame();
        Assert.That(bulletFrame.ParagraphCount(), Is.EqualTo(2));
        Assert.That(bulletFrame.GetParagraph(0).HasBullet, Is.True);
        Assert.That(bulletFrame.GetParagraph(1).HasBullet, Is.True);
        Assert.That(bulletFrame.GetParagraph(0).BulletCharacter, Is.EqualTo("•"));
        Assert.That(reopened.GetSlide(3).ExtractImages().Count, Is.EqualTo(1));
        Assert.That(reopened.GetSlide(3).ExtractImages()[0].Info.WidthPixels, Is.EqualTo(320));
        Assert.That(reopened.GetSlide(3).ExtractImages()[0].Info.HeightPixels, Is.EqualTo(180));
        Assert.That(new PptxDocument(reopened.Package).Validate().Succeeded(), Is.True);
    }

    [Test]
    public void Generator_AddsPopulatedTableAndChartSlidesAndRejectsRaggedTables()
    {
        PptxDeckGenerator generator = new PptxDeckGenerator();
        PptxDocument document = generator.CreateDefaultDeck();
        PptxPresentation presentation = document.LoadPresentation();
        List<List<string>> tableData = CreateTableData();
        PptxChartData chartData = CreateChartData();

        generator.AddTableSlide(presentation, "Regional totals", tableData);
        generator.AddChartSlide(presentation, "Regional chart", PptxChartType.Column, chartData);
        PptxDocument reopenedDocument = Reopen(document);
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        List<List<string>> actualTable = reopened.GetSlide(0).GetTable(0).ExtractData();
        PptxChartData actualChart = reopened.GetSlide(1).GetChart(0).GetData();
        List<List<string>> ragged = new List<List<string>>();
        ragged.Add(new List<string> { "A", "B" });
        ragged.Add(new List<string> { "C" });

        Assert.That(actualTable, Is.EqualTo(tableData));
        Assert.That(actualChart.Categories, Is.EqualTo(chartData.Categories));
        Assert.That(actualChart.Series.Count, Is.EqualTo(1));
        Assert.That(actualChart.Series[0].Name, Is.EqualTo("Revenue"));
        Assert.That(actualChart.Series[0].Values, Is.EqualTo(new List<string> { "12", "9" }));
        Assert.That(reopened.GetSlide(0).GetShapeTree().Get(1).IsTable(), Is.True);
        Assert.That(reopened.GetSlide(1).GetShapeTree().Get(1).IsChart(), Is.True);
        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
        Assert.That(() => generator.AddTableSlide(presentation, "Invalid", ragged), Throws.ArgumentException);
    }

    [Test]
    public void Generator_AllCommonSlideKindsProduceDeterministicPackageBytes()
    {
        byte[] first = CreateCommonDeckBytes();
        byte[] second = CreateCommonDeckBytes();
        PptxDocument reopenedDocument = new PptxDocumentFactory().OpenBytes(first);
        PptxPresentation presentation = reopenedDocument.LoadPresentation();

        Assert.That(first, Is.EqualTo(second));
        Assert.That(presentation.SlideCount(), Is.EqualTo(6));
        Assert.That(presentation.GetSlide(0).RelationshipId, Is.EqualTo("rId2"));
        Assert.That(presentation.GetSlide(1).RelationshipId, Is.EqualTo("rId6"));
        Assert.That(presentation.GetSlide(5).RelationshipId, Is.EqualTo("rId10"));
        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
    }

    static void AssertLayout(PptxSlideLayout layout, string name, string type, string relationshipId)
    {
        Assert.That(layout.Name, Is.EqualTo(name));
        Assert.That(layout.Type, Is.EqualTo(type));
        Assert.That(layout.RelationshipId, Is.EqualTo(relationshipId));
        Assert.That(layout.Master, Is.Not.Null);
    }

    static void AssertSlideIdentity(PptxSlide slide, int slideId, string partName, string relationshipId)
    {
        Assert.That(slide.SlideId, Is.EqualTo(slideId));
        Assert.That(slide.PartName, Is.EqualTo(partName));
        Assert.That(slide.RelationshipId, Is.EqualTo(relationshipId));
    }

    static PptxDocument Reopen(PptxDocument document)
    {
        return new PptxDocumentFactory().OpenBytes(document.SaveBytes());
    }

    static byte[] CreateCommonDeckBytes()
    {
        PptxDeckGenerator generator = new PptxDeckGenerator();
        PptxDocument document = generator.CreateDefaultDeck();
        PptxPresentation presentation = document.LoadPresentation();
        List<string> bullets = new List<string>();
        bullets.Add("First");
        bullets.Add("Second");
        generator.AddTitleSlide(presentation, "Title", "Subtitle");
        generator.AddContentSlide(presentation, "Content", "Body");
        generator.AddBulletSlide(presentation, "Bullets", bullets);
        generator.AddImageSlide(presentation, "Image", CreatePngBytes(160, 90), PptxContentTypes.Png);
        generator.AddTableSlide(presentation, "Table", CreateTableData());
        generator.AddChartSlide(presentation, "Chart", PptxChartType.Column, CreateChartData());
        return document.SaveBytes();
    }

    static List<List<string>> CreateTableData()
    {
        List<List<string>> data = new List<List<string>>();
        data.Add(new List<string> { "Region", "Revenue" });
        data.Add(new List<string> { "North", "12" });
        data.Add(new List<string> { "South", "9" });
        return data;
    }

    static PptxChartData CreateChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("North");
        data.Categories.Add("South");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Revenue";
        series.Values.Add("12");
        series.Values.Add("9");
        data.Series.Add(series);
        return data;
    }

    static byte[] CreatePngBytes(int width, int height)
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        WriteInt32Big(bytes, 16, width);
        WriteInt32Big(bytes, 20, height);
        return bytes;
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
