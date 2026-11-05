using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PublicApiSmokeTests
{
    [Test]
    public void CreateOpenEditSaveReopenAndValidate_WithPublicApis()
    {
        PptxDocument created = PptxDocument.CreatePresentation();
        PptxPresentation presentation = created.LoadPresentation();
        PptxSlide firstSlide = presentation.GetSlide(0);

        firstSlide.AddTextBox("{{Title}}", new PptxBounds(914400L, 685800L, 5486400L, 685800L));
        firstSlide.AddImage(CreatePngBytes(320, 200), new PptxBounds(914400L, 1600200L, 1828800L, 1143000L));
        PptxTable table = firstSlide.AddTable(1, 2, new PptxBounds(914400L, 3200400L, 2743200L, 685800L));
        table.SetText(0, 0, "Left");
        table.SetText(0, 1, "Right");
        firstSlide.AddChart(PptxChartType.Column, CreateChartData(), new PptxBounds(4114800L, 1600200L, 2743200L, 1828800L));

        PptxSlide secondSlide = presentation.AddSlide();
        secondSlide.AddTextBox("Second slide text", new PptxBounds(914400L, 914400L, 5486400L, 685800L));
        int replacements = presentation.ReplaceText("{{Title}}", "Public API smoke deck");

        byte[] saved = created.SaveBytes();
        PptxDocument opened = PptxDocument.OpenBytes(saved);
        PptxResult validation = opened.Validate();
        PptxPresentation reopened = opened.LoadPresentation();

        Assert.That(replacements, Is.EqualTo(1));
        Assert.That(opened.GetPackage().Succeeded(), Is.True, opened.GetPackage().Diagnostics.FormatDiagnostics());
        Assert.That(validation.Succeeded(), Is.True, validation.FormatDiagnostics());
        Assert.That(reopened.Succeeded(), Is.True, reopened.Diagnostics.FormatDiagnostics());
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.ExtractText(), Does.Contain("Public API smoke deck"));
        Assert.That(reopened.ExtractText(), Does.Contain("Second slide text"));
        Assert.That(reopened.GetSlide(0).ExtractImages().Count, Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).GetTables().Count, Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).GetCharts().Count, Is.EqualTo(1));

        byte[] savedAgain = opened.SaveBytes();
        PptxDocument reopenedAgain = PptxDocument.OpenBytes(savedAgain);

        Assert.That(reopenedAgain.Validate().Succeeded(), Is.True, reopenedAgain.Validate().FormatDiagnostics());
        Assert.That(reopenedAgain.LoadPresentation().ExtractText(), Does.Contain("Public API smoke deck"));
    }

    static PptxChartData CreateChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("Q1");
        data.Categories.Add("Q2");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Revenue";
        series.Values.Add("10");
        series.Values.Add("20");
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
        bytes[offset] = (byte)((value / 16777216) & 255);
        bytes[offset + 1] = (byte)((value / 65536) & 255);
        bytes[offset + 2] = (byte)((value / 256) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
