using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SlideCloneTests
{
    [Test]
    public void CloneSlide_ClonesSlideAfterSourceAndReusesSharedLayout()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        PptxSlide clone = presentation.CloneSlide(0);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(clone.SlideId, Is.EqualTo(257));
        Assert.That(clone.PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(clone.RelationshipId, Is.EqualTo("rId6"));
        Assert.That(clone.Layout, Is.Not.Null);
        Assert.That(clone.Layout, Is.SameAs(presentation.GetSlide(0).Layout));
        Assert.That(presentation.Package.GetPart(clone.PartName).Relationships.Count(), Is.EqualTo(1));
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(256));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(257));
        Assert.That(reopened.GetSlide(1).Layout, Is.Not.Null);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CloneSlide_CanInsertCloneAtRequestedPosition()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();

        PptxSlide clone = presentation.CloneSlideToIndex(1, 0);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(clone.SlideId, Is.EqualTo(258));
        Assert.That(reopened.SlideCount(), Is.EqualTo(3));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(258));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(256));
        Assert.That(reopened.GetSlide(2).SlideId, Is.EqualTo(257));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CloneSlide_CopiesDependentInternalPartsAndRemapsRelationships()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        AddChartDependency(presentation);

        PptxSlide clone = presentation.CloneSlide(0);
        PptxPackage package = presentation.Package;
        PptxRelationship clonedChartRelationship = FindRelationship(package.GetPart(clone.PartName).Relationships, PptxRelationshipTypes.Chart);
        PptxRelationship clonedWorkbookRelationship = FindRelationship(package.GetPart("/ppt/charts/chart2.xml").Relationships, PptxRelationshipTypes.Package);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(package.HasPart("/ppt/charts/chart1.xml"), Is.True);
        Assert.That(package.HasPart("/ppt/charts/chart2.xml"), Is.True);
        Assert.That(package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"), Is.True);
        Assert.That(package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx"), Is.True);
        Assert.That(clonedChartRelationship.TargetPartName, Is.EqualTo("/ppt/charts/chart2.xml"));
        Assert.That(clonedChartRelationship.Target, Is.EqualTo("../charts/chart2.xml"));
        Assert.That(clonedWorkbookRelationship.TargetPartName, Is.EqualTo("/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx"));
        Assert.That(clonedWorkbookRelationship.Target, Is.EqualTo("../embeddings/Microsoft_Excel_Worksheet2.xlsx"));
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.Package.HasPart("/ppt/charts/chart2.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx"), Is.True);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CloneSlide_PreservesExternalRelationships()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.Package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.Hyperlink, "https://example.com", PptxRelationshipTargetMode.External);

        PptxSlide clone = presentation.CloneSlide(0);
        PptxRelationship hyperlink = FindRelationship(presentation.Package.GetPart(clone.PartName).Relationships, PptxRelationshipTypes.Hyperlink);

        Assert.That(hyperlink.TargetMode, Is.EqualTo(PptxRelationshipTargetMode.External));
        Assert.That(hyperlink.Target, Is.EqualTo("https://example.com"));
    }

    static void AddChartDependency(PptxPresentation presentation)
    {
        presentation.Package.AddPart(
            "/ppt/charts/chart1.xml",
            PptxContentTypes.Chart,
            Encoding.UTF8.GetBytes("<c:chartSpace xmlns:c=\"" + PptxNamespaces.Chart + "\"><c:chart/></c:chartSpace>"));
        presentation.Package.AddPart(
            "/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx",
            PptxContentTypes.Binary,
            new byte[] { 1, 2, 3 });
        presentation.Package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        presentation.Package.AddRelationship("/ppt/charts/chart1.xml", PptxRelationshipTypes.Package, "../embeddings/Microsoft_Excel_Worksheet1.xlsx", PptxRelationshipTargetMode.Internal);
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return relationships.Relationships[index];
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
