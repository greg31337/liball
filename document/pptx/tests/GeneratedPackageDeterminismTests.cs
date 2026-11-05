using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class GeneratedPackageDeterminismTests
{
    [Test]
    public void MinimumPresentation_IndependentGenerationProducesIdenticalPackageBytes()
    {
        byte[] first = PptxDocument.CreateMinimumPresentation().SaveBytes();
        byte[] second = PptxDocument.CreateMinimumPresentation().SaveBytes();

        AssertSamePackageBytes(first, second);
        Assert.That(ReadEntryNames(first), Is.EqualTo(ReadEntryNames(second)));
    }

    [Test]
    public void RichPresentation_IndependentGenerationProducesIdenticalPackageBytes()
    {
        byte[] first = CreateRichGeneratedPackageBytes();
        byte[] second = CreateRichGeneratedPackageBytes();

        AssertSamePackageBytes(first, second);
        Assert.That(ReadEntryNames(first), Is.EqualTo(ReadEntryNames(second)));
    }

    [Test]
    public void RichPresentation_ReopenSaveProducesDeterministicPackageBytes()
    {
        byte[] generated = CreateRichGeneratedPackageBytes();

        byte[] first = PptxDocument.OpenBytes(generated).SaveBytes();
        byte[] second = PptxDocument.OpenBytes(generated).SaveBytes();

        AssertSamePackageBytes(first, second);
    }

    static byte[] CreateRichGeneratedPackageBytes()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();

        ApplyDocumentProperties(presentation);
        PopulateFirstSlide(presentation.GetSlide(0));
        PptxSlide second = presentation.AddSlide();
        second.AddTextBox("Second deterministic slide", new PptxBounds(914400L, 914400L, 5486400L, 685800L));
        second.AddImage(CreatePngBytes(160, 90), new PptxBounds(914400L, 1828800L, 1463040L, 822960L));

        return document.SaveBytes();
    }

    static void ApplyDocumentProperties(PptxPresentation presentation)
    {
        PptxCoreProperties core = presentation.GetCoreProperties();
        core.Title = "Deterministic generated deck";
        core.Creator = "pptx-tests";
        core.LastModifiedBy = "pptx-tests";
        core.Category = "determinism";
        core.Save();

        PptxCustomProperties custom = presentation.GetCustomProperties();
        custom.SetString("Deterministic", "true");
        custom.SetInt("Revision", 1);
        custom.Save();
    }

    static void PopulateFirstSlide(PptxSlide slide)
    {
        slide.AddTextBox("Deterministic title", new PptxBounds(914400L, 685800L, 5486400L, 685800L));
        PptxShape rectangle = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(914400L, 1600200L, 1371600L, 685800L));
        rectangle.SetName("Deterministic Rectangle");
        rectangle.SetText("Box");
        slide.AddAutoShape(PptxShapeType.Diamond, new PptxBounds(2514600L, 1600200L, 914400L, 914400L));
        slide.AddConnector(PptxConnector.Straight, new PptxPoint(914400L, 2971800L), new PptxPoint(3657600L, 2971800L));
        slide.AddImage(CreatePngBytes(320, 200), new PptxBounds(4114800L, 1600200L, 1828800L, 1143000L));

        PptxTable table = slide.AddTable(2, 2, new PptxBounds(914400L, 3429000L, 2743200L, 914400L));
        table.SetText(0, 0, "A1");
        table.SetText(0, 1, "B1");
        table.SetText(1, 0, "A2");
        table.SetText(1, 1, "B2");

        slide.AddChart(PptxChartType.Column, CreateChartData(), new PptxBounds(4114800L, 3200400L, 2743200L, 1828800L));
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

    static void AssertSamePackageBytes(byte[] first, byte[] second)
    {
        Assert.That(second, Is.EqualTo(first));
        AssertPackageValid(first);
        AssertPackageValid(second);
    }

    static void AssertPackageValid(byte[] bytes)
    {
        PptxDocument document = PptxDocument.OpenBytes(bytes);
        PptxResult validation = document.Validate();

        Assert.That(document.GetPackage().Succeeded(), Is.True, document.GetPackage().Diagnostics.FormatDiagnostics());
        Assert.That(validation.Succeeded(), Is.True, validation.FormatDiagnostics());
    }

    static string[] ReadEntryNames(byte[] bytes)
    {
        using MemoryStream stream = new MemoryStream(bytes);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
        string[] names = new string[archive.Entries.Count];

        for (int index = 0; index < archive.Entries.Count; index++)
        {
            names[index] = archive.Entries[index].FullName;
        }

        return names;
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
