using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ProcessingWorkflowCompletionTests
{
    [Test]
    public void ComparisonWorkflow_CombinesManifestAndEditableModelDifferences()
    {
        PptxDocumentFactory documents = new PptxDocumentFactory();
        PptxDocument left = documents.CreatePresentation();
        PptxDocument right = left.CloneDocument();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSlideOperations slides = new PptxSlideOperations();
        PptxPresentation rightPresentation = right.LoadPresentation();
        PptxSlide added = presentations.AddSlide(rightPresentation);
        slides.AddTextBox(added, "Compared content", new PptxBounds(0L, 0L, 914400L, 914400L));
        PptxComparisonOperations comparisons = new PptxComparisonOperations();

        PptxPresentationComparison changed = comparisons.CompareDocuments(left, right);
        PptxPresentationComparison same = comparisons.CompareSources(left, left.CloneDocument());

        Assert.That(changed.Equal, Is.False);
        Assert.That(changed.Manifest.Equal, Is.False);
        Assert.That(changed.EditableModel.Equal, Is.False);
        Assert.That(changed.Manifest.AddedEntryNames, Does.Contain("ppt/slides/slide2.xml"));
        Assert.That(FindDifference(changed.EditableModel, "SlideCount"), Is.Not.Null);
        Assert.That(FindDifference(changed.EditableModel, "ShapeCount"), Is.Not.Null);
        Assert.That(changed.EditableModel.Left.SlideCount, Is.EqualTo(1));
        Assert.That(changed.EditableModel.Right.SlideCount, Is.EqualTo(2));
        Assert.That(same.Equal, Is.True);
        Assert.That(same.Manifest.Equal, Is.True);
        Assert.That(same.EditableModel.Equal, Is.True);
        Assert.That(presentations.SlideCount(left.LoadPresentation()), Is.EqualTo(1));
        Assert.That(presentations.SlideCount(right.LoadPresentation()), Is.EqualTo(2));
    }

    [Test]
    public void ReportGenerator_CreatesTitleAndOrderedSectionSlidesFromData()
    {
        PptxReportData data = CreateReportData();
        PptxDocument document = new PptxReportGenerator().Generate(data);
        PptxDocument reopenedDocument = new PptxDocumentFactory().OpenBytes(document.SaveBytes());
        PptxPresentation presentation = reopenedDocument.LoadPresentation();
        PptxPresentationOperations presentations = new PptxPresentationOperations();

        Assert.That(presentations.SlideCount(presentation), Is.EqualTo(3));
        Assert.That(presentations.ExtractSlideText(presentation, 0), Does.Contain("Quarterly report"));
        Assert.That(presentations.ExtractSlideText(presentation, 0), Does.Contain("FY 2026"));
        Assert.That(presentations.ExtractSlideText(presentation, 1), Does.Contain("Revenue"));
        Assert.That(presentations.ExtractSlideText(presentation, 1), Does.Contain("North: 12"));
        Assert.That(presentations.ExtractSlideText(presentation, 1), Does.Contain("South: 9"));
        Assert.That(presentations.ExtractSlideText(presentation, 2), Does.Contain("Risks"));
        Assert.That(presentations.ExtractSlideText(presentation, 2), Does.Contain("Supply constraints"));
        Assert.That(presentations.GetSlide(presentation, 0).GetShapeTree().Count(), Is.EqualTo(2));
        Assert.That(presentations.GetSlide(presentation, 1).GetShapeTree().Count(), Is.EqualTo(2));
        Assert.That(reopenedDocument.Validate().Succeeded(), Is.True, reopenedDocument.Validate().FormatDiagnostics());
    }

    [Test]
    public void ReportGenerator_ProducesDeterministicBytesAndTextFormatting()
    {
        PptxReportGenerator generator = new PptxReportGenerator();
        byte[] first = generator.Generate(CreateReportData()).SaveBytes();
        byte[] second = generator.Generate(CreateReportData()).SaveBytes();
        PptxPresentation presentation = new PptxDocumentFactory().OpenBytes(first).LoadPresentation();
        PptxShape title = presentation.GetSlide(0).GetShapeTree().Get(0);
        PptxShape body = presentation.GetSlide(1).GetShapeTree().Get(1);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(title.GetTextFrame().GetParagraph(0).GetRun(0).Font.SizePoints, Is.EqualTo(32.0));
        Assert.That(title.GetTextFrame().GetParagraph(0).GetRun(0).Font.Bold, Is.True);
        Assert.That(body.GetTextFrame().ParagraphCount(), Is.EqualTo(2));
        Assert.That(body.GetTextFrame().GetParagraph(0).GetRun(0).Font.SizePoints, Is.EqualTo(20.0));
        Assert.That(body.GetTextFrame().GetParagraph(1).GetRun(0).Font.SizePoints, Is.EqualTo(20.0));
    }

    static PptxReportData CreateReportData()
    {
        PptxReportData data = new PptxReportData();
        data.Title = "Quarterly report";
        data.Subtitle = "FY 2026";
        PptxReportSection revenue = new PptxReportSection();
        revenue.Title = "Revenue";
        revenue.Lines.Add("North: 12");
        revenue.Lines.Add("South: 9");
        data.Sections.Add(revenue);
        PptxReportSection risks = new PptxReportSection();
        risks.Title = "Risks";
        risks.Lines.Add("Supply constraints");
        data.Sections.Add(risks);
        return data;
    }

    static PptxEditableModelDifference? FindDifference(PptxEditableModelComparison comparison, string fieldName)
    {
        for (int index = 0; index < comparison.Differences.Count; index++)
        {
            if (comparison.Differences[index].FieldName == fieldName)
            {
                return comparison.Differences[index];
            }
        }

        return null;
    }
}
