using System.Diagnostics;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PerformanceSmokeTests
{
    public const long LargeDeckBudgetMilliseconds = 30000L;
    public const long ManySlidesBudgetMilliseconds = 30000L;
    public const long ManyImagesBudgetMilliseconds = 30000L;
    public const long LargeChartBudgetMilliseconds = 30000L;

    [Test]
    public void LargeDeck_MixedContentGeneratesSavesReopensAndValidatesWithinBudget()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        byte[] png = CreatePngBytes(160, 90);

        PopulateMixedSlide(presentation.GetSlide(0), 1, png);
        for (int index = 2; index <= 25; index++)
        {
            PopulateMixedSlide(presentation.AddSlide(), index, png);
        }

        byte[] bytes = document.SaveBytes();
        PptxDocument reopenedDocument = PptxDocument.OpenBytes(bytes);
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        stopwatch.Stop();

        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
        Assert.That(reopened.SlideCount(), Is.EqualTo(25));
        Assert.That(reopened.ExtractText(), Does.Contain("Mixed slide 25"));
        AssertWithinBudget(stopwatch, LargeDeckBudgetMilliseconds);
    }

    [Test]
    public void ManySlides_GeneratesSavesReopensAndValidatesWithinBudget()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        presentation.GetSlide(0).AddTextBox("Slide 1", new PptxBounds(914400L, 914400L, 5486400L, 685800L));

        for (int index = 2; index <= 100; index++)
        {
            PptxSlide slide = presentation.AddSlide();
            slide.AddTextBox("Slide " + index.ToString(), new PptxBounds(914400L, 914400L, 5486400L, 685800L));
        }

        byte[] bytes = document.SaveBytes();
        PptxDocument reopenedDocument = PptxDocument.OpenBytes(bytes);
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        stopwatch.Stop();

        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
        Assert.That(reopened.SlideCount(), Is.EqualTo(100));
        Assert.That(reopened.ExtractText(), Does.Contain("Slide 100"));
        AssertWithinBudget(stopwatch, ManySlidesBudgetMilliseconds);
    }

    [Test]
    public void ManyImages_GeneratesDeduplicatesReopensAndValidatesWithinBudget()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        byte[] png = CreatePngBytes(64, 64);

        for (int index = 0; index < 120; index++)
        {
            long x = 914400L + (long)(index % 12) * 274320L;
            long y = 914400L + (long)(index / 12) * 274320L;
            slide.AddImage(png, new PptxBounds(x, y, 182880L, 182880L));
        }

        byte[] bytes = document.SaveBytes();
        PptxDocument reopenedDocument = PptxDocument.OpenBytes(bytes);
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        stopwatch.Stop();

        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
        Assert.That(reopened.GetSlide(0).ExtractImages().Count, Is.EqualTo(120));
        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(1));
        AssertWithinBudget(stopwatch, ManyImagesBudgetMilliseconds);
    }

    [Test]
    public void LargeChartData_GeneratesSavesReopensAndValidatesWithinBudget()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        presentation.GetSlide(0).AddChart(PptxChartType.Line, CreateLargeChartData(250, 4), new PptxBounds(914400L, 914400L, 7315200L, 4114800L));

        byte[] bytes = document.SaveBytes();
        PptxDocument reopenedDocument = PptxDocument.OpenBytes(bytes);
        PptxPresentation reopened = reopenedDocument.LoadPresentation();
        PptxChartData data = reopened.GetSlide(0).GetChart(0).GetData();
        stopwatch.Stop();

        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
        Assert.That(data.CategoryCount(), Is.EqualTo(250));
        Assert.That(data.SeriesCount(), Is.EqualTo(4));
        Assert.That(data.GetSeries(3).GetValue(249), Is.EqualTo("1000"));
        AssertWithinBudget(stopwatch, LargeChartBudgetMilliseconds);
    }

    static void PopulateMixedSlide(PptxSlide slide, int slideNumber, byte[] png)
    {
        slide.AddTextBox("Mixed slide " + slideNumber.ToString(), new PptxBounds(914400L, 685800L, 5486400L, 685800L));
        slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(914400L, 1600200L, 914400L, 457200L));
        slide.AddConnector(PptxConnector.Straight, new PptxPoint(914400L, 2514600L), new PptxPoint(2743200L, 2514600L));
        slide.AddImage(png, new PptxBounds(3200400L, 1600200L, 1463040L, 822960L));
        PptxTable table = slide.AddTable(1, 2, new PptxBounds(914400L, 3200400L, 2743200L, 685800L));
        table.SetText(0, 0, "S" + slideNumber.ToString());
        table.SetText(0, 1, "Value");
    }

    static PptxChartData CreateLargeChartData(int categoryCount, int seriesCount)
    {
        PptxChartData data = new PptxChartData();
        for (int categoryIndex = 1; categoryIndex <= categoryCount; categoryIndex++)
        {
            data.Categories.Add("C" + categoryIndex.ToString());
        }

        for (int seriesIndex = 1; seriesIndex <= seriesCount; seriesIndex++)
        {
            PptxChartSeries series = new PptxChartSeries();
            series.Name = "Series " + seriesIndex.ToString();
            for (int valueIndex = 1; valueIndex <= categoryCount; valueIndex++)
            {
                series.Values.Add((seriesIndex * valueIndex).ToString());
            }

            data.Series.Add(series);
        }

        return data;
    }

    static void AssertWithinBudget(Stopwatch stopwatch, long budgetMilliseconds)
    {
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(budgetMilliseconds));
    }

    static int CountMediaParts(PptxPackage package)
    {
        int count = 0;
        for (int index = 0; index < package.Parts.Count; index++)
        {
            if (package.Parts[index].PartName.StartsWith("/ppt/media/", System.StringComparison.Ordinal))
            {
                count = count + 1;
            }
        }

        return count;
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
        bytes[offset] = (byte)((value / 16777216) & 255);
        bytes[offset + 1] = (byte)((value / 65536) & 255);
        bytes[offset + 2] = (byte)((value / 256) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
