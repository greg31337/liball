using System.Text.Json;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class JsonSummaryExportTests
{
    [Test]
    public void Package_ToSummaryJsonExportsPackagePartsAndRelationships()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();

        using (JsonDocument document = JsonDocument.Parse(package.ToSummaryJson()))
        {
            JsonElement root = document.RootElement;
            JsonElement packageElement = root.GetProperty("package");
            JsonElement parts = packageElement.GetProperty("parts");

            Assert.That(root.GetProperty("summaryType").GetString(), Is.EqualTo("package"));
            Assert.That(packageElement.GetProperty("partCount").GetInt32(), Is.EqualTo(package.PartCount()));
            Assert.That(packageElement.GetProperty("rootRelationshipCount").GetInt32(), Is.EqualTo(package.RootRelationships.Count()));
            Assert.That(packageElement.GetProperty("contentTypeOverrideCount").GetInt32(), Is.EqualTo(package.ContentTypes.Overrides.Count));
            Assert.That(ContainsPart(parts, PptxPartNames.Presentation, PptxContentTypes.Presentation), Is.True);
            Assert.That(ContainsPart(parts, PptxPartNames.FirstSlide, PptxContentTypes.Slide), Is.True);
        }
    }

    [Test]
    public void Presentation_ToSummaryJsonExportsPackageAndModelCounts()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        slide.AddImage(CreatePngBytes(320, 200), new PptxBounds(0L, 0L, 320L, 200L));
        slide.AddTable(1, 1, new PptxBounds(0L, 220L, 320L, 120L));
        presentation.SetSlideNotesText(0, "Speaker Notes");
        presentation.AddSection("Intro");
        presentation.AddSlideToSection(0, slide.SlideId);
        presentation.AddCustomShow("Main");
        presentation.AddSlideToCustomShow(0, slide.SlideId);

        using (JsonDocument document = JsonDocument.Parse(presentation.ToSummaryJson()))
        {
            JsonElement root = document.RootElement;
            JsonElement packageElement = root.GetProperty("package");
            JsonElement model = root.GetProperty("model");
            JsonElement slideSummary = model.GetProperty("slides")[0];

            Assert.That(root.GetProperty("summaryType").GetString(), Is.EqualTo("presentation"));
            Assert.That(packageElement.GetProperty("partCount").GetInt32(), Is.EqualTo(presentation.Package.PartCount()));
            Assert.That(model.GetProperty("slideCount").GetInt32(), Is.EqualTo(1));
            Assert.That(model.GetProperty("slideWidth").GetInt64(), Is.EqualTo(12192000L));
            Assert.That(model.GetProperty("slideHeight").GetInt64(), Is.EqualTo(6858000L));
            Assert.That(model.GetProperty("sectionCount").GetInt32(), Is.EqualTo(1));
            Assert.That(model.GetProperty("customShowCount").GetInt32(), Is.EqualTo(1));
            Assert.That(model.GetProperty("mediaCount").GetInt32(), Is.EqualTo(0));
            Assert.That(model.GetProperty("hasMacros").GetBoolean(), Is.False);
            Assert.That(slideSummary.GetProperty("partName").GetString(), Is.EqualTo(PptxPartNames.FirstSlide));
            Assert.That(slideSummary.GetProperty("imageCount").GetInt32(), Is.EqualTo(1));
            Assert.That(slideSummary.GetProperty("tableCount").GetInt32(), Is.EqualTo(1));
            Assert.That(slideSummary.GetProperty("shapeCount").GetInt32(), Is.EqualTo(2));
            Assert.That(slideSummary.GetProperty("hasNotes").GetBoolean(), Is.True);
            Assert.That(slideSummary.GetProperty("hidden").GetBoolean(), Is.False);
        }
    }

    static bool ContainsPart(JsonElement parts, string partName, string contentType)
    {
        foreach (JsonElement part in parts.EnumerateArray())
        {
            if (part.GetProperty("partName").GetString() == partName && part.GetProperty("contentType").GetString() == contentType)
            {
                return true;
            }
        }

        return false;
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
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
