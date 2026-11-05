using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SlideImportTests
{
    [Test]
    public void ImportSlide_CopiesSlideLayoutMasterAndThemeDependencies()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxPresentation source = LoadGeneratedPresentation();

        PptxSlide imported = destination.ImportSlide(source, 0);
        PptxPresentation reopened = SaveReopenAndLoad(destination);

        Assert.That(imported.SlideId, Is.EqualTo(257));
        Assert.That(imported.PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(imported.RelationshipId, Is.EqualTo("rId6"));
        Assert.That(destination.Package.HasPart("/ppt/slideLayouts/slideLayout2.xml"), Is.True);
        Assert.That(destination.Package.HasPart("/ppt/slideMasters/slideMaster2.xml"), Is.True);
        Assert.That(destination.Package.HasPart("/ppt/theme/theme2.xml"), Is.True);
        Assert.That(FindRelationship(destination.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.SlideMaster, "/ppt/slideMasters/slideMaster2.xml").Id, Is.EqualTo("rId7"));
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.SlideMasterCount(), Is.EqualTo(2));
        Assert.That(reopened.SlideLayoutCount(), Is.EqualTo(2));
        Assert.That(reopened.ThemeCount(), Is.EqualTo(2));
        Assert.That(reopened.GetSlide(1).Layout, Is.Not.Null);
        Assert.That(reopened.GetSlide(1).Layout!.PartName, Is.EqualTo("/ppt/slideLayouts/slideLayout2.xml"));
        Assert.That(reopened.GetSlide(1).Layout!.Master, Is.Not.Null);
        Assert.That(reopened.GetSlide(1).Layout!.Master!.PartName, Is.EqualTo("/ppt/slideMasters/slideMaster2.xml"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ImportSlide_CopiesDependentPartsAndRemapsRelationships()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxPresentation source = LoadGeneratedPresentation();
        AddChartDependency(source);
        AddExistingChartNameCollisions(destination);

        PptxSlide imported = destination.ImportSlide(source, 0);
        PptxRelationship importedChart = FindRelationship(destination.Package.GetPart(imported.PartName).Relationships, PptxRelationshipTypes.Chart, "/ppt/charts/chart2.xml");
        PptxRelationship importedWorkbook = FindRelationship(destination.Package.GetPart("/ppt/charts/chart2.xml").Relationships, PptxRelationshipTypes.Package, "/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx");
        PptxPresentation reopened = SaveReopenAndLoad(destination);

        Assert.That(destination.Package.HasPart("/ppt/charts/chart1.xml"), Is.True);
        Assert.That(destination.Package.HasPart("/ppt/charts/chart2.xml"), Is.True);
        Assert.That(destination.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"), Is.True);
        Assert.That(destination.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx"), Is.True);
        Assert.That(importedChart.Target, Is.EqualTo("../charts/chart2.xml"));
        Assert.That(importedWorkbook.Target, Is.EqualTo("../embeddings/Microsoft_Excel_Worksheet2.xlsx"));
        Assert.That(reopened.Package.HasPart("/ppt/charts/chart2.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx"), Is.True);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ImportSlide_PreservesExternalRelationships()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxPresentation source = LoadGeneratedPresentation();
        source.Package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.Hyperlink, "https://example.com", PptxRelationshipTargetMode.External);

        PptxSlide imported = destination.ImportSlide(source, 0);
        PptxRelationship hyperlink = FindRelationship(destination.Package.GetPart(imported.PartName).Relationships, PptxRelationshipTypes.Hyperlink, "");

        Assert.That(hyperlink.TargetMode, Is.EqualTo(PptxRelationshipTargetMode.External));
        Assert.That(hyperlink.Target, Is.EqualTo("https://example.com"));
    }

    [Test]
    public void ImportSlide_AcceptsSourceDocument()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxDocument sourceDocument = PptxDocument.CreateMinimumPresentation();

        PptxSlide imported = destination.ImportDocumentSlide(sourceDocument, 0);

        Assert.That(imported.SlideId, Is.EqualTo(257));
        Assert.That(destination.SlideCount(), Is.EqualTo(2));
    }

    static void AddChartDependency(PptxPresentation presentation)
    {
        AddExistingChartNameCollisions(presentation);
        presentation.Package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        presentation.Package.AddRelationship("/ppt/charts/chart1.xml", PptxRelationshipTypes.Package, "../embeddings/Microsoft_Excel_Worksheet1.xlsx", PptxRelationshipTargetMode.Internal);
    }

    static void AddExistingChartNameCollisions(PptxPresentation presentation)
    {
        if (!presentation.Package.HasPart("/ppt/charts/chart1.xml"))
        {
            presentation.Package.AddPart(
                "/ppt/charts/chart1.xml",
                PptxContentTypes.Chart,
                Encoding.UTF8.GetBytes("<c:chartSpace xmlns:c=\"" + PptxNamespaces.Chart + "\"><c:chart/></c:chartSpace>"));
        }

        if (!presentation.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"))
        {
            presentation.Package.AddPart(
                "/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx",
                PptxContentTypes.Binary,
                new byte[] { 1, 2, 3 });
        }
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string type, string targetPartName)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.Type != type)
            {
                continue;
            }

            if (targetPartName.Length == 0 || relationship.TargetPartName == targetPartName)
            {
                return relationship;
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }
}
