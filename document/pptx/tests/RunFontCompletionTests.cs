using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class RunFontCompletionTests
{
    [Test]
    public void RunFormatting_RoundTripsFamiliesEmphasisDecorationsMetricsFillHighlightAndSymbol()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 3000000L, 1000000L));
        shape.SetText("Formatted run");
        PptxRun run = shape.GetTextFrame().GetParagraph(0).GetRun(0);
        IPptxRunOperations operations = new PptxRunOperations();
        PptxFill fill = new PptxFillOperations().CreateGradient(PptxColor.CreateScheme("accent1"), PptxColor.CreateRgb("102030"), 45.0);

        operations.SetFamilies(run, "Aptos", "Yu Gothic", "Arial");
        operations.SetSize(run, 26.5);
        operations.SetEmphasis(run, true, true);
        operations.SetDecorations(run, "wavy-double", "double");
        operations.SetBaseline(run, 18000);
        operations.SetCapitalization(run, "small-caps");
        operations.SetKerning(run, 1200);
        operations.SetCharacterSpacing(run, 250);
        operations.SetLanguage(run, "en-US", "fr-FR");
        operations.SetFill(run, fill);
        operations.SetHighlight(run, PptxColor.CreatePreset("yellow"));
        operations.SetSymbol(run, "Wingdings");

        PptxPresentation reopened = Reopen(presentation);
        PptxRun reopenedRun = reopened.GetSlide(0).GetShapeTree().Get(0).GetTextFrame().GetParagraph(0).GetRun(0);
        PptxFont font = operations.GetFont(reopenedRun);

        Assert.That(font.Family, Is.EqualTo("Aptos"));
        Assert.That(font.EastAsianFamily, Is.EqualTo("Yu Gothic"));
        Assert.That(font.ComplexScriptFamily, Is.EqualTo("Arial"));
        Assert.That(font.SymbolFamily, Is.EqualTo("Wingdings"));
        Assert.That(font.SizePoints, Is.EqualTo(26.5));
        Assert.That(font.Bold, Is.True);
        Assert.That(font.Italic, Is.True);
        Assert.That(font.Underline, Is.True);
        Assert.That(font.UnderlineStyle, Is.EqualTo("wavyDbl"));
        Assert.That(font.Strike, Is.True);
        Assert.That(font.StrikeStyle, Is.EqualTo("dblStrike"));
        Assert.That(font.Baseline, Is.EqualTo(18000));
        Assert.That(font.Capitalization, Is.EqualTo("small"));
        Assert.That(font.Kerning, Is.EqualTo(1200));
        Assert.That(font.Spacing, Is.EqualTo(250));
        Assert.That(font.Language, Is.EqualTo("en-US"));
        Assert.That(font.AlternativeLanguage, Is.EqualTo("fr-FR"));
        Assert.That(font.Fill.Type, Is.EqualTo(PptxFill.Gradient));
        Assert.That(font.Fill.GradientStops, Has.Count.EqualTo(2));
        Assert.That(font.Fill.GradientStops[0].Color.Value, Is.EqualTo("accent1"));
        Assert.That(font.Fill.GradientStops[1].Color.Value, Is.EqualTo("102030"));
        Assert.That(font.HighlightColor.Type, Is.EqualTo(PptxColor.Preset));
        Assert.That(font.HighlightColor.Value, Is.EqualTo("yellow"));
        Assert.That(reopenedRun.PropertiesXml, Does.Contain("gradFill"));
        Assert.That(reopenedRun.PropertiesXml, Does.Contain("highlight"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void RunEditing_AddsFieldsSymbolsAndFormattedBreaksAndNormalizesText()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 3000000L, 1000000L));
        shape.SetText("Initial");
        PptxParagraph paragraph = shape.GetTextFrame().GetParagraph(0);
        IPptxParagraphOperations paragraphs = new PptxParagraphOperations();
        IPptxRunOperations runs = new PptxRunOperations();
        PptxRun text = paragraph.GetRun(0);

        Assert.That(runs.NormalizeText("Cafe\u0301\rBad\u0001"), Is.EqualTo("Café\nBad"));
        text.SetText("Cafe\u0301\r\nBad\u0001");
        runs.NormalizeRun(text);
        PptxRun field = paragraphs.AddField(paragraph, "field-1", "datetime1", "August 26");
        runs.SetField(field, "field-2", "datetime2");
        PptxRun symbol = paragraphs.AddSymbol(paragraph, "✓", "Wingdings");
        PptxRun lineBreak = paragraphs.AddLineBreak(paragraph);
        runs.SetSize(lineBreak, 14.0);

        PptxPresentation reopened = Reopen(presentation);
        PptxParagraph actual = reopened.GetSlide(0).GetShapeTree().Get(0).GetTextFrame().GetParagraph(0);

        Assert.That(actual.GetRun(0).Text, Is.EqualTo("Café\nBad"));
        Assert.That(actual.GetRun(1).Type, Is.EqualTo(PptxRun.Field));
        Assert.That(actual.GetRun(1).FieldId, Is.EqualTo("field-2"));
        Assert.That(actual.GetRun(1).FieldType, Is.EqualTo("datetime2"));
        Assert.That(actual.GetRun(1).Text, Is.EqualTo("August 26"));
        Assert.That(actual.GetRun(2).Font.SymbolFamily, Is.EqualTo("Wingdings"));
        Assert.That(actual.GetRun(2).Text, Is.EqualTo("✓"));
        Assert.That(actual.GetRun(3).Type, Is.EqualTo(PptxRun.LineBreak));
        Assert.That(actual.GetRun(3).Font.SizePoints, Is.EqualTo(14.0));
    }

    [Test]
    public void RunHyperlink_UpdatesAndRemovesExternalRelationshipWithoutLosingFormatting()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 3000000L, 1000000L));
        shape.SetText("Linked run");
        PptxRun run = shape.GetTextFrame().GetParagraph(0).GetRun(0);
        PptxRunOperations operations = new PptxRunOperations();

        operations.SetEmphasis(run, true, false);
        operations.SetExternalHyperlink(run, "https://example.com/one");
        string relationshipId = run.HyperlinkRelationshipId;
        operations.SetExternalHyperlink(run, "https://example.com/two");

        Assert.That(run.HyperlinkRelationshipId, Is.EqualTo(relationshipId));
        Assert.That(run.HyperlinkTarget, Is.EqualTo("https://example.com/two"));
        Assert.That(CountHyperlinks(presentation), Is.EqualTo(1));

        PptxPresentation reopened = Reopen(presentation);
        PptxRun reopenedRun = reopened.GetSlide(0).GetShapeTree().Get(0).GetTextFrame().GetParagraph(0).GetRun(0);
        Assert.That(reopenedRun.HyperlinkTarget, Is.EqualTo("https://example.com/two"));
        Assert.That(reopenedRun.Font.Bold, Is.True);

        operations.RemoveHyperlink(reopenedRun);
        PptxPresentation removed = Reopen(reopened);
        PptxRun removedRun = removed.GetSlide(0).GetShapeTree().Get(0).GetTextFrame().GetParagraph(0).GetRun(0);
        Assert.That(removedRun.HyperlinkRelationshipId, Is.Empty);
        Assert.That(removedRun.HyperlinkTarget, Is.Empty);
        Assert.That(removedRun.Font.Bold, Is.True);
        Assert.That(CountHyperlinks(removed), Is.EqualTo(0));
    }

    [Test]
    public void InspectFit_ReportsAvailableBoundsWrappingOverflowAndAutofitResponse()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape wideShape = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 5000000L, 1000000L));
        PptxShape narrowShape = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 1200000L, 300000L, 150000L));
        wideShape.SetText("A short line");
        narrowShape.SetText("A much longer line that must overflow or wrap");
        PptxTextFrameOperations frames = new PptxTextFrameOperations();
        PptxRunOperations runs = new PptxRunOperations();
        frames.SetMargins(wideShape.GetTextFrame(), new PptxMargins(0L, 0L, 0L, 0L));
        frames.SetMargins(narrowShape.GetTextFrame(), new PptxMargins(0L, 0L, 0L, 0L));
        runs.SetSize(wideShape.GetTextFrame().GetParagraph(0).GetRun(0), 18.0);
        runs.SetSize(narrowShape.GetTextFrame().GetParagraph(0).GetRun(0), 24.0);
        frames.SetWrapping(narrowShape.GetTextFrame(), "none");

        PptxTextFit wide = frames.InspectFit(wideShape.GetTextFrame());
        PptxTextFit narrow = frames.InspectFit(narrowShape.GetTextFrame());

        Assert.That(wide.Fits, Is.True);
        Assert.That(wide.CharacterCount, Is.EqualTo(12));
        Assert.That(wide.LineCount, Is.EqualTo(1));
        Assert.That(narrow.HorizontalOverflow, Is.True);
        Assert.That(narrow.Fits, Is.False);
        Assert.That(narrow.CanAutofit, Is.False);

        frames.SetWrapping(narrowShape.GetTextFrame(), "square");
        frames.SetAutofit(narrowShape.GetTextFrame(), "normal", 50000, 0);
        PptxTextFit autofit = frames.InspectFit(narrowShape.GetTextFrame());
        Assert.That(autofit.LineCount, Is.GreaterThan(1));
        Assert.That(autofit.VerticalOverflow, Is.True);
        Assert.That(autofit.CanAutofit, Is.True);
        Assert.That(autofit.Autofit, Is.EqualTo(PptxTextFrameBody.AutofitNormal));
    }

    [Test]
    public void RunOperations_RejectInvalidValuesBeforeCreatingRunProperties()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 3000000L, 1000000L));
        shape.SetText("Validation");
        PptxParagraph paragraph = shape.GetTextFrame().GetParagraph(0);
        PptxRun run = paragraph.GetRun(0);
        PptxRunOperations operations = new PptxRunOperations();
        string originalProperties = run.PropertiesXml;

        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetSize(run, -1.0));
        Assert.Throws<System.ArgumentException>(() => operations.SetDecorations(run, "triple", "none"));
        Assert.Throws<System.ArgumentException>(() => operations.SetDecorations(run, "single", "triple"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetBaseline(run, 100001));
        Assert.Throws<System.ArgumentException>(() => operations.SetCapitalization(run, "title"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetKerning(run, -1));
        Assert.Throws<System.ArgumentNullException>(() => operations.SetFill(run, null!));
        Assert.Throws<System.ArgumentNullException>(() => operations.SetHighlight(run, null!));
        Assert.Throws<System.ArgumentException>(() => operations.SetField(run, "id", "type"));
        Assert.Throws<System.ArgumentException>(() => operations.SetExternalHyperlink(run, "/relative/path"));
        Assert.Throws<System.ArgumentException>(() => new PptxParagraphOperations().AddField(paragraph, "", "type", "text"));
        Assert.Throws<System.ArgumentException>(() => new PptxParagraphOperations().AddSymbol(paragraph, "", "Wingdings"));
        Assert.That(run.PropertiesXml, Is.EqualTo(originalProperties));
    }

    static int CountHyperlinks(PptxPresentation presentation)
    {
        int count = 0;
        for (int index = 0; index < presentation.GetSlide(0).Part.Relationships.Relationships.Count; index++)
        {
            if (presentation.GetSlide(0).Part.Relationships.Relationships[index].Type == PptxRelationshipTypes.Hyperlink)
            {
                count = count + 1;
            }
        }

        return count;
    }

    static PptxPresentation Reopen(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
