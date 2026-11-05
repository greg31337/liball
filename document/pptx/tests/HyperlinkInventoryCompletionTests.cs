using System.Collections.Generic;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class HyperlinkInventoryCompletionTests
{
    [Test]
    public void ShapeAndRunHyperlinkInventoriesRoundTripEverySupportedActionKind()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide firstSlide = presentation.GetSlide(0);
        presentation.AddSlide();
        presentation.AddCustomShow("Demo Show");
        PptxShape shape = firstSlide.AddTextBox("URL run", new PptxBounds(0L, 0L, 600L, 300L));
        PptxParagraph paragraph = shape.GetTextFrame().Paragraphs[0];
        PptxRun urlRun = paragraph.Runs[0];
        PptxRun fileRun = paragraph.AddRun("File run");
        PptxRun slideRun = paragraph.AddRun("Slide run");
        PptxRun navigationRun = paragraph.AddRun("Navigation run");
        PptxRun customShowRun = paragraph.AddRun("Custom show run");
        PptxRun macroRun = paragraph.AddRun("Macro run");
        PptxRunOperations runOperations = new PptxRunOperations();

        shape.SetHyperlink("https://example.com/shape");
        runOperations.SetExternalHyperlink(urlRun, "https://example.com/run");
        runOperations.SetFileHyperlink(fileRun, "file:///tmp/report.pdf");
        runOperations.SetSlideHyperlink(slideRun, 1);
        runOperations.SetNavigationAction(navigationRun, "last-slide");
        runOperations.SetCustomShowAction(customShowRun, "Demo Show");
        runOperations.SetAction(macroRun, "ppaction://macro?name=Project.Module.RunReport");

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxPresentationOperations presentationOperations = new PptxPresentationOperations();
        PptxSlideOperations slideOperations = new PptxSlideOperations();
        PptxShapeOperations shapeOperations = new PptxShapeOperations();
        List<PptxHyperlink> presentationLinks = presentationOperations.GetHyperlinks(reopened);
        List<PptxHyperlink> slideLinks = slideOperations.GetHyperlinks(reopened.GetSlide(0));
        List<PptxHyperlink> shapeLinks = shapeOperations.GetHyperlinks(reopened.GetSlide(0).GetShapeTree().Get(0));

        Assert.That(presentationLinks.Count, Is.EqualTo(7));
        Assert.That(slideLinks.Count, Is.EqualTo(7));
        Assert.That(shapeLinks.Count, Is.EqualTo(7));
        Assert.That(shapeLinks[0].OwnerType, Is.EqualTo("shape"));
        Assert.That(shapeLinks[0].Type, Is.EqualTo("url"));
        Assert.That(shapeLinks[1].OwnerType, Is.EqualTo("run"));
        Assert.That(shapeLinks[1].Type, Is.EqualTo("url"));
        Assert.That(shapeLinks[2].Type, Is.EqualTo("file"));
        Assert.That(shapeLinks[3].Type, Is.EqualTo("slide"));
        Assert.That(shapeLinks[3].SlideIndex, Is.EqualTo(1));
        Assert.That(shapeLinks[4].Type, Is.EqualTo("last-slide"));
        Assert.That(shapeLinks[5].Type, Is.EqualTo("custom-show"));
        Assert.That(shapeLinks[5].CustomShowName, Is.EqualTo("Demo Show"));
        Assert.That(shapeLinks[6].Type, Is.EqualTo("macro"));
        Assert.That(shapeLinks[6].MacroName, Is.EqualTo("Project.Module.RunReport"));
        Assert.That(shapeLinks[6].Action, Is.EqualTo("ppaction://macro?name=Project.Module.RunReport"));
        string audit = presentationOperations.AuditHyperlinks(reopened).FormatDiagnostics();
        Assert.That(audit, Does.Contain("HLK002"));
        Assert.That(audit, Does.Contain("HLK004"));
        Assert.That(CountRelationships(reopened.GetSlide(0).Part.Relationships, PptxRelationshipTypes.Hyperlink), Is.EqualTo(3));
        Assert.That(CountRelationships(reopened.GetSlide(0).Part.Relationships, PptxRelationshipTypes.Slide), Is.EqualTo(1));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void HyperlinkOperationsEditEitherOwnerAndReturnSafetyAuditResults()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();
        PptxShape shape = presentation.GetSlide(0).AddTextBox("Linked run", new PptxBounds(0L, 0L, 300L, 100L));
        PptxRun run = shape.GetTextFrame().Paragraphs[0].Runs[0];
        PptxHyperlinkOperations operations = new PptxHyperlinkOperations();
        PptxHyperlink hyperlink = new PptxRunOperations().GetHyperlink(run);

        operations.SetUrl(hyperlink, "https://example.com");
        Assert.That(operations.AuditSafety(hyperlink).Count(), Is.EqualTo(0));
        operations.SetFile(hyperlink, "file:///tmp/report.pdf");
        Assert.That(operations.AuditSafety(hyperlink).FormatDiagnostics(), Does.Contain("HLK002"));
        operations.SetSlide(hyperlink, 1);
        Assert.That(hyperlink.Type, Is.EqualTo("slide"));
        operations.SetNavigation(hyperlink, "next-slide");
        Assert.That(hyperlink.Type, Is.EqualTo("next-slide"));
        operations.SetCustomShow(hyperlink, "Demo Show");
        Assert.That(hyperlink.Type, Is.EqualTo("custom-show"));
        operations.SetAction(hyperlink, "ppaction://macro?name=Project.Module.RunReport");
        Assert.That(operations.AuditSafety(hyperlink).FormatDiagnostics(), Does.Contain("HLK004"));
        operations.SetUrl(hyperlink, "javascript:alert(1)");
        Assert.That(operations.AuditSafety(hyperlink).FormatDiagnostics(), Does.Contain("HLK003"));
        Assert.That(() => operations.SetNavigation(hyperlink, "middle-slide"), Throws.ArgumentException);

        operations.Remove(hyperlink);
        Assert.That(operations.HasLink(hyperlink), Is.False);
        Assert.That(presentation.GetSlide(0).GetHyperlinks().Count, Is.EqualTo(0));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void EditingRunActionPreservesRelationshipSharedByShapeHyperlink()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddTextBox("Linked run", new PptxBounds(0L, 0L, 300L, 100L));
        PptxRun run = shape.GetTextFrame().Paragraphs[0].Runs[0];
        PptxRunOperations runOperations = new PptxRunOperations();
        shape.SetHyperlink("https://example.com/shared");
        runOperations.SetExternalHyperlink(run, "https://example.com/run");
        string sharedRelationshipId = shape.GetHyperlink().RelationshipId;
        string oldRunRelationshipId = run.GetHyperlink().RelationshipId;
        System.Xml.XmlElement? runHyperlink = run.GetHyperlinkClickElement();
        Assert.That(runHyperlink, Is.Not.Null);
        PptxXml.SetAttributeValue(runHyperlink!, "r", PptxNamespaces.Relationships, "id", sharedRelationshipId);
        shape.Tree.RemoveRelationship(oldRunRelationshipId);

        runOperations.SetAction(run, "ppaction://macro?name=Project.Module.RunReport");

        Assert.That(shape.Tree.FindRelationshipById(sharedRelationshipId), Is.Not.Null);
        Assert.That(shape.GetHyperlink().Target, Is.EqualTo("https://example.com/shared"));
        Assert.That(run.GetHyperlink().Type, Is.EqualTo("macro"));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        PptxDocument reopened = PptxDocument.OpenBytes(document.SaveBytes());
        return reopened.LoadPresentation();
    }

    static int CountRelationships(PptxRelationshipCollection relationships, string type)
    {
        int count = 0;
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                count = count + 1;
            }
        }

        return count;
    }
}
